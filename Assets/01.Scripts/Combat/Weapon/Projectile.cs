using UnityEngine;
using System.Collections.Generic;


// 투사체 1개 이동, 피격, 관통 처리
public class Projectile : MonoBehaviour
{
    // 반경 질의 결과 버퍼. 인스턴스 필드
    private readonly List<Enemy> hitBuffer = new List<Enemy>(16);

    // 3중첩 반응(점화 폭발)용 별도 버퍼
    // hitBuffer를 재사용하면 CheckHits가 for로 순회하는 중에 목록이 덮여
    // 명중 처리가 깨짐. 반드시 따로 두기
    private readonly List<Enemy> reactionBuffer = new List<Enemy>(16);

    // 관통 중 같은 적을 매 프레임 다시 때리기 방지
    private readonly List<Enemy> alreadyHit = new List<Enemy>(4);

    // 연쇄 대상 위치 수집용. 
    private readonly List<Vector2> chainTargetPositions = new List<Vector2>(4);

    // 적이 플레이어에게 쏜 투사체일 때만 채워짐
    // null이면 지금까지처럼 적을 찾아 때리는 플레이어 투사체로 동작
    private Health playerTarget;
    private float playerTargetRadius;

    // 이 투사체를 만든 프리팹. 어느 풀로 반납할지 찾는 데 쓰임
    private Projectile sourcePrefab;

    public Projectile SourcePrefab => sourcePrefab;

    private ProjectileSpec spec;
    private Vector2 direction;

    private float traveledDistance;

    private int remainingPierce;

    private bool isActive;
    public bool IsActive => isActive;


    public void SetSourcePrefab(Projectile prefab)
    {
        sourcePrefab = prefab;
    }

    // 투사체 발사 전 상태 초기화
    // 풀에서 재사용될 때도 매번 호출
    public void Launch(in ProjectileSpec launchSpec, Vector2 origin, Vector2 launchDirection)
    {
        Prepare(launchSpec, origin, launchDirection);

        // 플레이어 투사체. 적을 찾아 때림
        playerTarget = null;
        playerTargetRadius = 0f;
    }


    // 적이 플레이어에게 쏘는 투사체. 소환술사 원거리탄, 보스 부채꼴이 사용
    //
    // 대상이 플레이어 한 명뿐이라 EnemyManager 검색x
    // 발사 시점에 받은 Health 하나만 거리로 검사
    //
    // spec.SkillLevel은 0으로 둘 것. 표식은 적에게만 붙는 개념
    public void LaunchAtPlayer(in ProjectileSpec launchSpec, Vector2 origin, Vector2 launchDirection,
        Health target, float targetRadius)
    {
        Prepare(launchSpec, origin, launchDirection);

        playerTarget = target;
        playerTargetRadius = targetRadius;
    }

    // 두 발사 경로가 공유하는 상태 초기화
    private void Prepare(in ProjectileSpec launchSpec, Vector2 origin, Vector2 launchDirection)
    {
        spec = launchSpec;
        direction = launchDirection.normalized;
        traveledDistance = 0f;
        remainingPierce = launchSpec.PierceCount;

        // 이전 발사에 맞은 적 기록 초기화
        alreadyHit.Clear();

        isActive = true;

        transform.position = origin;

        // 스프라이트 오른쪽 방향을 진행방향으로 맞춤
        transform.right = direction;
    }

    // 기존 2인자 호출부 호환용.
    // 호출: Assets/Editor/AssetPresentationValidation.cs (선동님 파일, 3곳)
    // 그쪽이 3인자로 바뀌면 이 오버로드는 제거
    public void Tick(float deltaTime, EnemyManager enemyManager)
    {
        Tick(deltaTime, enemyManager, null);
    }

    // ProjectileLauncher에서 매 프레임 호출
    public void Tick(float deltaTime, EnemyManager enemyManager, ElementHitCounter hitCounter)
    {
        if (!isActive)
        {
            return;
        }

        // TODO: step이 HitRadius*2보다 커지면 적 통과함. 속도 향상 시에 재검토
        float step = spec.Speed * deltaTime;

        Vector2 nextPosition = (Vector2)transform.position + direction * step;

        transform.position = nextPosition;

        traveledDistance += step;

        // 적 명중으로 소멸시 이번 Tick 종료
        if (CheckHits(nextPosition, enemyManager, hitCounter))
        {
            return;
        }

        // 최대 이동 거리 도달 시 종료
        if (traveledDistance >= spec.MaxDistance)
        {
            isActive = false;
        }
    }


    // 표식 3중첩 도달 시의 원소별 반응
    //
    // 실행 위치가 Enemy가 아니라 투사체인 이유는
    // 1. 반응 해금 판정에 필요한 스킬 레벨은 적이 아니라 이 투사체가 들고 있음
    // 2. 점화 폭발은 EnemyManager의 활성 적 목록이 필요한데 Enemy는 Manager를 모름
    private void TriggerMarkReaction(Enemy origin, EnemyManager enemyManager)
    {
        if (spec.SkillLevel < ElementReactionValues.ReactionUnlockLevel)
        {
            return;
        }

        Vector2 center = origin.transform.position;

        switch (spec.Element)
        {
            case MagicElement.Fire:

                // 7레벨이면 피해,반경이 커짐. 판정과 이벤트가 같은 반경을 쓰도록 한 번만 계산
                float igniteRadius = ElementReactionValues.GetIgniteRadius(spec.SkillLevel);
                float igniteDamage = ElementReactionValues.GetIgniteDamage(spec.SkillLevel);

                // 점화. 중심 적 자신도 반경 안에 들어가므로 함께 피해를 받음
                enemyManager.FindOverlappingEnemies(
                    center, igniteRadius, reactionBuffer);

                for (int i = 0; i < reactionBuffer.Count; i++)
                {
                    // 폭발 피해는 표식을 걸지 않는다. 연쇄 점화 x
                    reactionBuffer[i].TakeDamage(igniteDamage);
                }

                GameEvents.RaiseElementReaction(
                    MagicElement.Fire, center, igniteRadius);

                // 5레벨 전염
                // 점화가 중심 적을 죽였다면 사망 전염이 대기열에 들어갔으므로 건너뜀
                // 둘 다 돌면 같은 주변 적 2명이 표식 2개, 이벤트 2회를 받음
                // 두 경로 모두 5레벨 게이트라 레벨 조건은 어긋나지 않음
                if (origin.IsAlive)
                {
                    SpreadFireMark(origin, center, enemyManager);
                }

                break;

            case MagicElement.Lightning:

                // 방전. 위쪽 ReactionUnlockLevel 게이트와 별개로 5레벨에서 해금.
                if (spec.SkillLevel < ElementReactionValues.ExpansionUnlockLevel)
                {
                    break;
                }

                // 점화와 같이 중심 적도 반경 안이라 함께 맞음
                // 이 피해는 표식도 적중 카운트도 만들지 않음
                enemyManager.FindOverlappingEnemies(center, ElementReactionValues.DischargeRadius, reactionBuffer);

                // 7레벨 방전 피해 강화. 반경은 언급 없어 그대로
                float dischargeDamage = ElementReactionValues.GetDischargeDamage(spec.SkillLevel);

                for (int i = 0; i < reactionBuffer.Count; i++)
                {
                    reactionBuffer[i].TakeDamage(dischargeDamage);
                }

                // 번개가 ElementReactionTriggered 를 발행하는 첫 경로.
                //
                // 3레벨 연쇄는 선이라 ChainReactionTriggered 를 쓰지만 방전은 원형.
                // 3중첩 전이와 3번째 적중이 같은 타격이라 둘이 함께 터질 수 있음
                // 방전이 먼저 실행되므로 방전이 죽인 적은 연쇄 대상에서 빠짐
                GameEvents.RaiseElementReaction(
                    MagicElement.Lightning, center, ElementReactionValues.DischargeRadius);

                break;

            case MagicElement.Frost:

                // 빙결. 
                // 5레벨이면 파괴 권한을 같이 넘김
                // 파괴는 이 자리가 아니라 다음에 맞을 때 터지므로
                // 레벨을 아는 여기서 적에게 권한만 남김
                //
                // 7레벨이면 빙결 시간이 늘어남. 파괴 권한은 빙결 동안 유지되므로 함께 길어짐
                origin.ApplyFreeze(
                    ElementReactionValues.GetFreezeDurationSeconds(spec.SkillLevel),
                    spec.SkillLevel >= ElementReactionValues.ExpansionUnlockLevel);

                GameEvents.RaiseElementReaction(MagicElement.Frost, center, 0f);

                break;


            case MagicElement.Dark:

                // 점화, 빙결은 이 자리에서 끝나는 1회성이지만 암흑은 지속 상태.
                origin.SetDarkAmplificationUnlocked();

                break;
        }
    }

    // 화염 5레벨. 점화가 터진 자리에서 주변 적에게 화염 표식 1 을 옮김
    // 점화가 쓴 reactionBuffer 를 재사용하지 않고 전염 반경으로 다시 찾음
    // 점화 루프는 이미 끝났으므로 같은 버퍼를 덮어써도 ok
    private void SpreadFireMark(Enemy origin, Vector2 center, EnemyManager enemyManager)
    {
        if (spec.SkillLevel < ElementReactionValues.ExpansionUnlockLevel)
        {
            return;
        }

        enemyManager.FindOverlappingEnemies(
            center, ElementReactionValues.FireSpreadRadius, reactionBuffer);

        // 화염 투사체가 부르는 경로라 같은 프레임에 번개 연쇄가 이 목록을 쓰지 않음
        // TriggerHitCountReaction 의 switch 는 대지와 번개만 처리
        chainTargetPositions.Clear();

        for (int i = 0; i < reactionBuffer.Count; i++)
        {
            Enemy target = reactionBuffer[i];

            // origin 은 방금 3중첩이 된 적. 전염해도 지속시간만 갱신되고
            // "주변 적 최대 2명" 자리를 먹음
            if (target == origin || !target.IsAlive)
            {
                continue;
            }

            // 위치를 피해보다 먼저 기록하는 다른 반응들과 순서를 맞추기
            chainTargetPositions.Add(target.transform.position);

            // 전염 표식은 반응 판정을 거치지 않음. 미확정 부분.
            // reactionBuffer 가 인스턴스 필드라 전염 도중 반응이 다시 돌면
            // 지금 순회 중인 이 목록이 덮어써진다. 같은 적 재처리 금지도 있어야 함
            //
            // 구조를 합의하기 전까지는 표식만 걸기. 커밋 본문에 확인 요청.
            target.ApplyElementMark(MagicElement.Fire, 1, ElementMarkRules.Duration);

            if (chainTargetPositions.Count >= ElementReactionValues.FireSpreadMaxTargets)
            {
                break;
            }
        }

        // 중심에서 개별 대상으로 이어지는 반응이라 연쇄와 같은 이벤트를 사용
        // 같은 순간에 ElementReactionTriggered(Fire, 원형) 도 나가게.
        // 폭발이 터지고 표식이 퍼지는 그림.
        // [연동:UI] 별도 연결이 필요
        if (chainTargetPositions.Count > 0)
        {
            GameEvents.RaiseChainReaction(
                MagicElement.Fire, center, chainTargetPositions.ToArray());
        }
    }


    // 적중마다의 원소별 반응. 3중첩 반응과 발동 조건만 다르고 실행 위치는 동일
    // 카운터는 원소를 가리지 않고 셈
    private void TriggerHitCountReaction(Enemy origin, EnemyManager enemyManager)
    {
        Vector2 center = origin.transform.position;

        switch (spec.Element)
        {
            case MagicElement.Earth:

                // 6레벨이면 충격파 반경이 커짐. 판정과 이벤트가 같은 반경을 쓰도록 한 번만 계산
                // 아래 지진 구역은 EarthquakeRadius 를 그대로 사용
                float shockwaveRadius = ElementReactionValues.GetShockwaveRadius(spec.SkillLevel);

                // 충격파. 중심 적도 반경 안이라 함께 맞고,
                // 이 피해는 표식도 적중 카운트도 만들지 않음
                enemyManager.FindOverlappingEnemies(center, shockwaveRadius, reactionBuffer);

                for (int i = 0; i < reactionBuffer.Count; i++)
                {
                    reactionBuffer[i].TakeDamage(ElementReactionValues.ShockwaveDamage);
                }

                GameEvents.RaiseElementReaction(MagicElement.Earth, center, shockwaveRadius);

                // 5레벨 지진 구역. 충격파가 터진 자리에 남는다
                if (spec.SkillLevel >= ElementReactionValues.ExpansionUnlockLevel)
                {
                    enemyManager.AddGroundArea(
                        MagicElement.Earth,
                        center,
                        ElementReactionValues.EarthquakeRadius,
                        ElementReactionValues.EarthquakeDurationSeconds,
                        ElementReactionValues.EarthquakeSlowPercent);
                }

                break;

            case MagicElement.Lightning:

                // 연쇄. 기획이 주변 적이라 직격을 맞은 적을 제외
                // 충격파와 반대. 충격파는 중심 적도 함께 맞음
                enemyManager.FindOverlappingEnemies(
                    center, ElementReactionValues.ChainRadius, reactionBuffer);

                float chainDamage = spec.Damage * ElementReactionValues.ChainDamageRatio;

                // 6레벨이면 대상 +1. 보스 분기는 이 값을 쓰지 않아 보스는 계속 1명
                int chainMaxTargets = ElementReactionValues.GetChainMaxTargets(spec.SkillLevel);

                chainTargetPositions.Clear();

                // 기획: 보스에게는 연쇄 대상이 보스 1명으로 제한
                //
                // 아래 루프 안에서 IsBoss 를 보면 x.
                // 보스가 목록 뒤쪽에 있으면 앞의 일반 적들을 이미 때린 뒤에 만나게 됨.
                // 목록 순서와 무관하도록 때리기 전에 먼저 찾음
                Enemy chainBoss = FindChainBoss(origin);

                if (chainBoss != null)
                {
                    chainTargetPositions.Add(chainBoss.transform.position);

                    chainBoss.TakeDamage(chainDamage);
                }
                else
                {
                    // 대상 우선순위가 기획에 없어 관리 목록 순서를 그대로 사용.
                    // 거리순x,  적이 죽으면 목록 순서가 바뀌므로
                    // 같은 배치에서도 대상이 달라질 수 있음
                    // 기획에서 우선순위가 추가로 정해지면 여길 수정
                    for (int i = 0; i < reactionBuffer.Count; i++)
                    {
                        if (reactionBuffer[i] == origin)
                        {
                            continue;
                        }

                        // 위치를 피해보다 먼저 기록.
                        // 피해가 위치를 바꾸는 경우가 이미 있음 (대지 밀치기)
                        chainTargetPositions.Add(reactionBuffer[i].transform.position);

                        reactionBuffer[i].TakeDamage(chainDamage);

                        if (chainTargetPositions.Count >= chainMaxTargets)
                        {
                            break;
                        }
                    }
                }

                // 연쇄 대상이 0명이면 알리지 않음. 보여줄 연출 x
                // 이 경우에도 적중 카운트는 이미 소비.
                // 발동을 보류하지 않으므로 다음 기회는 3번째 적중 뒤


                if (chainTargetPositions.Count > 0)
                {
                    GameEvents.RaiseChainReaction(
                        MagicElement.Lightning, center, chainTargetPositions.ToArray());
                }

                break;
        }

    }

    // 적 투사체의 플레이어 명중 판정
    //
    // 관통도 표식도 없으므로 맞으면 그 자리에서 끝
    // 플레이어는 풀링 대상이 아니라 중복 방지 필요 없음
    private bool CheckPlayerHit(Vector2 position)
    {
        // 이미 죽은 플레이어는 때리지 않음.
        if (!playerTarget.IsAlive)
        {
            return false;
        }

        float combined = spec.HitRadius + playerTargetRadius;

        Vector2 toPlayer = (Vector2)playerTarget.transform.position - position;

        if (toPlayer.sqrMagnitude >= combined * combined)
        {
            return false;
        }

        // 접촉 피해와 달리 무적 시간을 거치지 않음
        // PlayerContactDamage의 무적은 그 컴포넌트 내부 타이머이고, 공유하면
        // 접촉 중에 맞은 원거리탄이 먹혀 기획의 접촉 12 + 원거리 8이 안 나옴
        // 기획에 규정이 없어 정한 것이라 확인 요청 대상
        playerTarget.TakeDamage(spec.Damage);

        isActive = false;

        return true;
    }


    private bool CheckHits(Vector2 position, EnemyManager enemyManager, ElementHitCounter hitCounter)
    {
        // 적이 쏜 투사체는 대상이 플레이어 한 명뿐이라 적 검색으로 내려가지 않음
        if (playerTarget != null)
        {
            return CheckPlayerHit(position);
        }

        enemyManager.FindOverlappingEnemies(position, spec.HitRadius, hitBuffer);

        for (int i = 0; i < hitBuffer.Count; i++)
        {

            Enemy enemy = hitBuffer[i];

            if (alreadyHit.Contains(enemy))
            {
                continue;
            }

            alreadyHit.Add(enemy);

            // 화염·암흑 5레벨 사망 전염 권한. 반드시 피해보다 먼저
            if (spec.SkillLevel >= ElementReactionValues.ExpansionUnlockLevel)
            {
                enemy.ArmDeathSpread(spec.Element);
            }

            enemy.TakeDamage(spec.Damage);

            // 원소 공격 적중 시 해당 원소 표식 1중첩
            //
            // SkillLevel 0 = 표식을 걸지 않는 투사체
            //   (보스 투사체, _Test/SimpleProjectileAttack, MagicRuntime 미반영 상태)
            if (spec.SkillLevel > 0 && enemy.IsAlive)
            {
                int stacksBefore = enemy.GetElementMark(spec.Element).Stacks;

                enemy.ApplyElementMark(spec.Element, 1, ElementMarkRules.Duration);

                int stacksAfter = enemy.GetElementMark(spec.Element).Stacks;

                // 2 -> 3 전이에서만 반응.
                if (ElementMarkRules.ShouldTriggerMastery(stacksBefore, stacksAfter))
                {
                    TriggerMarkReaction(enemy, enemyManager);
                }

                // N번째 적중마다 발동하는 효과.
                // 표식과 같은 IsAlive 가드 안에 두어 막타는 세지 않음
                // 밖에 두면 죽은 적 위치에서 반응이 터져 승리 전환 뒤에 이벤트가 발행
                if (spec.SkillLevel >= ElementReactionValues.ReactionUnlockLevel
                    && hitCounter != null
                    && hitCounter.RegisterHit(spec.Element))
                {
                    TriggerHitCountReaction(enemy, enemyManager);
                }



                // 대지 1레벨 밀치기. 카운터가 아니라 적중할 때마다라 여기에 두기
                //
                // 충격파보다 뒤여야 함. 앞에 두면 TriggerHitCountReaction이
                // origin.transform.position을 읽을 때 이미 밀려난 위치가 되므로
                //
                // 7레벨이면 거리 +30%. 보스 면역은 Enemy.ApplyKnockback 의 IsKnockbackImmune 이 처리
                if (spec.Element == MagicElement.Earth)
                {
                    enemy.ApplyKnockback(direction, ElementReactionValues.GetKnockbackDistance(spec.SkillLevel));
                }

            }

            // PierceCount 0이며 첫 명중 시 소멸
            if (remainingPierce <= 0)
            {
                isActive = false;
                return true;
            }

            remainingPierce--;

        }


        return false;
    }

    // 연쇄 대상 후보 중 보스를 찾기
    // 기획의 "보스에게는 연쇄 대상이 보스 1명" 을
    // 보스가 연쇄를 받는 쪽일 때로 해석.
    // 이 해석이면 단독 보스전 구간에는 연쇄 대상이 0명. 커밋 본문에 확인 요청
    private Enemy FindChainBoss(Enemy origin)
    {
        for (int i = 0; i < reactionBuffer.Count; i++)
        {
            Enemy candidate = reactionBuffer[i];

            if (candidate == origin)
            {
                continue;
            }

            if (candidate.IsBoss)
            {
                return candidate;
            }
        }

        return null;
    }

}


