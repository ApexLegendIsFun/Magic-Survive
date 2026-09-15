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

                // 점화. 중심 적 자신도 반경 안에 들어가므로 함께 피해를 받는다
                enemyManager.FindOverlappingEnemies(center, ElementReactionValues.IgniteRadius, reactionBuffer);

                for (int i = 0; i < reactionBuffer.Count; i++)
                {
                    // 폭발 피해는 표식을 걸지 않는다. 연쇄 점화가 생기지 않음
                    reactionBuffer[i].TakeDamage(ElementReactionValues.IgniteDamage);
                }

                GameEvents.RaiseElementReaction(
                    MagicElement.Fire, center, ElementReactionValues.IgniteRadius);

                break;

            case MagicElement.Frost:

                // 빙결. 단일 대상이라 반경 0으로 알린다
                origin.ApplyFreeze(ElementReactionValues.FreezeDurationSeconds);

                GameEvents.RaiseElementReaction(MagicElement.Frost, center, 0f);

                break;


            case MagicElement.Dark:

                // 점화, 빙결은 이 자리에서 끝나는 1회성이지만 암흑은 지속 상태.
                origin.SetDarkAmplificationUnlocked();

                break;
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

                // 충격파. 중심 적도 반경 안이라 함께 맞고,
                // 이 피해는 표식도 적중 카운트도 만들지 않음
                enemyManager.FindOverlappingEnemies(
                    center, ElementReactionValues.ShockwaveRadius, reactionBuffer);

                for (int i = 0; i < reactionBuffer.Count; i++)
                {
                    reactionBuffer[i].TakeDamage(ElementReactionValues.ShockwaveDamage);
                }

                GameEvents.RaiseElementReaction(
                    MagicElement.Earth, center, ElementReactionValues.ShockwaveRadius);

                break;
        }
    }


    private bool CheckHits(Vector2 position, EnemyManager enemyManager, ElementHitCounter hitCounter)
    {
        enemyManager.FindOverlappingEnemies(position, spec.HitRadius, hitBuffer);

        for (int i = 0; i < hitBuffer.Count; i++)
        {

            Enemy enemy = hitBuffer[i];

            if (alreadyHit.Contains(enemy))
            {
                continue;
            }

            alreadyHit.Add(enemy);

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

}


