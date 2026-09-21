using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

// 현재 살아있는 적 관리
// 적 이동, 타겟 검색, 적 생성 관련
public class EnemyManager : MonoBehaviour
{

    [SerializeField] private Transform playerTransform;

    private readonly List<Enemy> activeEnemies = new List<Enemy>();

    private readonly Dictionary<Enemy, ObjectPool<Enemy>>
        pools = new Dictionary<Enemy, ObjectPool<Enemy>>();

    // 냉기 5레벨 파괴 전용 버퍼
    private readonly List<Enemy> frostShatterBuffer = new List<Enemy>(16);

    // 대지 8레벨 낙석 전용 버퍼와 위치 배열
    // 낙석 피해가 파괴를 유발할 수 있어 파괴 버퍼와 섞으면 x
    private readonly List<Enemy> rockfallBuffer = new List<Enemy>(16);
    private readonly Vector2[] rockfallPositions =
        new Vector2[ElementReactionValues.RockfallCount];

    // 화염 8레벨 불장판 피해 버퍼
    // GroundAreaState 가 이번 프레임 분을 채우고 ApplyGroundDamage 가 적에게 적용
    private readonly List<GroundAreaState.DamagePulse> groundDamagePulses =
        new List<GroundAreaState.DamagePulse>(8);

    // 파괴 피해가 다른 빙결 적을 또 터뜨리는 재귀를 막음.
    // 냉기 전용.
    private bool isResolvingFrostShatter;

    public bool IsResolvingFrostShatter => isResolvingFrostShatter;

    // 화염,암흑 5레벨 사망 전염 대기열
    // Enemy 참조를 담지 않음. LateUpdate 전에 풀로 반환되어 재사용될 수 있음
    private struct PendingDeathSpread
    {
        public MagicElement Element;
        public Vector2 Center;
        public float Radius;
        public int MaxTargets;
    }

    private readonly List<PendingDeathSpread> pendingDeathSpreads =
        new List<PendingDeathSpread>(16);

    // 사망 전염 전용 버퍼. 냉기 파괴와 공유 x
    private readonly List<Enemy> deathSpreadBuffer = new List<Enemy>(16);
    private readonly List<Vector2> deathSpreadPositions = new List<Vector2>(4);

    // [연동:Combat] 검증용. 이번 프레임에 대기 중인 전염 수
    public int PendingDeathSpreadCount => pendingDeathSpreads.Count;

    // [연동:UI] HUD 적 수 표시, 디버그
    public int ActiveCount => activeEnemies.Count;

    // 적 행동 컴포넌트에 넘겨줄 투사체 런처
    // 직렬화 참조를 새로 만들지 않으려고 ProjectileLauncher.Awake가 스스로 등록
    private ProjectileLauncher projectileLauncher;

    public void SetProjectileLauncher(ProjectileLauncher launcher)
    {
        projectileLauncher = launcher;
    }

    // [연동:Combat] 지속 장판을 추가한다. 대지 5레벨, 냉기 8레벨이 사용
    // slowPercent 0.3 이면 이동속도 30% 감소
    public void AddGroundArea(
        MagicElement element, Vector2 center, float radius,
        float durationSeconds, float slowPercent)
    {
        AddGroundArea(element, center, radius, durationSeconds, slowPercent, 0f, 0f);
    }

    // [연동:Combat] 피해를 주는 장판. 화염 8레벨 불장판이 사용
    // damagePerTick 을 damageIntervalSeconds 마다 장판 안의 적에게 준다
    public void AddGroundArea(
        MagicElement element, Vector2 center, float radius,
        float durationSeconds, float slowPercent,
        float damagePerTick, float damageIntervalSeconds)
    {
        // 만들어지지 않았으면 알리지 않음 연출만 나오고 판정이 없는 상태를 막음
        if (!groundAreas.Add(
            element, center, radius, durationSeconds, slowPercent,
            damagePerTick, damageIntervalSeconds))
        {
            return;
        }

        GameEvents.RaiseGroundAreaCreated(element, center, radius, durationSeconds);
    }

    // [연동:UI] 디버그와 검증용
    public int GroundAreaCount => groundAreas.ActiveCount;

    private void Register(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        activeEnemies.Add(enemy);
    }


    // 지정 위치 기준 가장 가까운 적 찾기
    // 자동공격 타겟 검색용
    public Enemy FindNearest(Vector2 from, float maxRange)
    {

        // 제곱근 생략 위해 제곱 거리로 비교. 매 프레임 X 마법 수만큼 돌아감
        float bestSqrDistance = maxRange * maxRange;

        Enemy nearest = null;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];

            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            Vector2 enemyPosition = enemy.transform.position;

            float sqrDistance = (enemyPosition - from).sqrMagnitude;

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;

                nearest = enemy;
            }
        }

        return nearest;
    }

    public void FindOverlappingEnemies(Vector2 center, float radius, List<Enemy> results)
    {
        results.Clear();

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];

            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            Vector2 enemyPosition = enemy.transform.position;

            float combined = radius + enemy.HitRadius;

            if ((enemyPosition - center).sqrMagnitude < combined * combined)
            {
                results.Add(enemy);
            }
            {

            }

        }

    }

    // 냉기 5레벨 파괴. 
    public void ResolveFrostShatter(Vector2 center)
    {
        isResolvingFrostShatter = true;

        try
        {
            FindOverlappingEnemies(
                center, ElementReactionValues.ShatterRadius, frostShatterBuffer);

            for (int i = 0; i < frostShatterBuffer.Count; i++)
            {
                Enemy target = frostShatterBuffer[i];

                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                target.TakeDamage(ElementReactionValues.ShatterDamage);
            }

            // [연동:UI] 빙결(반경 0)과 파괴(반경 1.2)가 같은 Frost 이벤트로 나감
            // 반경으로 구분
            GameEvents.RaiseElementReaction(
                MagicElement.Frost, center, ElementReactionValues.ShatterRadius);
        }
        finally
        {
            isResolvingFrostShatter = false;
        }
    }

    // [연동:Combat] 대지 8레벨 낙석. ProjectileLauncher 가 5번째 대지 발사에서 호출
    // 여기서 처리하는 이유: 낙석은 적중 지점이 아니라 플레이어 주변에 떨어지고,
    // 플레이어 위치와 적 목록을 둘 다 아는 곳이 여기뿐. 냉기 파괴와 같은 자리
    // 판정은 충격파/점화와 같은 FindOverlappingEnemies (반경 + 적 HitRadius).
    // 장판이 아니라 순간 타격
    public void ResolveRockfall()
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector2 playerPosition = playerTransform.position;

        float stepDegrees = 360f / rockfallPositions.Length;

        for (int i = 0; i < rockfallPositions.Length; i++)
        {
            float radians =
                (ElementReactionValues.RockfallFirstAngleDegrees + i * stepDegrees)
                * Mathf.Deg2Rad;

            Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians))
                * ElementReactionValues.RockfallSpawnDistance;

            Vector2 position = playerPosition + offset;

            rockfallPositions[i] = position;

            FindOverlappingEnemies(
                position, ElementReactionValues.RockfallRadius, rockfallBuffer);

            // 낙석마다 따로 판정. 세 반경에 모두 든 적은 세 번 맞음
            for (int target = 0; target < rockfallBuffer.Count; target++)
            {
                Enemy enemy = rockfallBuffer[target];

                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                enemy.TakeDamage(ElementReactionValues.RockfallDamage);
            }
        }

        // [연동:UI] 중심은 플레이어, 대상은 낙석 3곳
        // 화염 전염·연쇄와 같은 이벤트라 원소로 구분할 것
        GameEvents.RaiseChainReaction(MagicElement.Earth, playerPosition, (Vector2[])rockfallPositions.Clone());
    }

    // 화염·암흑 5레벨 사망 전염 등록. Enemy 가 죽는 순간 호출
    // 조건 판정은 Enemy 가 끝냈으므로 여기서는 저장만
    public void QueueDeathSpread(
        MagicElement element, Vector2 center, float radius, int maxTargets)
    {
        pendingDeathSpreads.Add(new PendingDeathSpread
        {
            Element = element,
            Center = center,
            Radius = radius,
            MaxTargets = maxTargets,
        });
    }

    // LateUpdate 에서 대기열을 한 번에 처리

    // 이 프레임의 모든 피해 순회(투사체, 광역 반응, 화염 도트)가 끝난 뒤라
    // 표식 증가가 같은 프레임의 피해 배율에 섞이지 않음

    // 표식만 옮기고 피해는 주지 않으므로 처리 중에 새 항목이 생기지 않음 -> 재진입 가드 x
    // 전염이 피해를 주게 바뀌면 이 순회 도중 대기열이 늘어날 수 있으니 다시 볼 것
    private void ProcessPendingDeathSpreads()
    {
        if (pendingDeathSpreads.Count == 0)
        {
            return;
        }

        for (int i = 0; i < pendingDeathSpreads.Count; i++)
        {
            PendingDeathSpread pending = pendingDeathSpreads[i];

            ResolveDeathSpread(
                pending.Element, pending.Center, pending.Radius, pending.MaxTargets);
        }

        pendingDeathSpreads.Clear();
    }

    // 전염 1건. 죽은 적은 이미 반환됐거나 IsAlive 가 false 라 대상에 없음
    // 대상에게 전염 권한은 넘기지 않음. 재전염 여부 미확정
    private void ResolveDeathSpread(
        MagicElement element, Vector2 center, float radius, int maxTargets)
    {
        FindOverlappingEnemies(center, radius, deathSpreadBuffer);

        deathSpreadPositions.Clear();

        for (int i = 0; i < deathSpreadBuffer.Count; i++)
        {
            if (deathSpreadPositions.Count >= maxTargets)
            {
                break;
            }

            Enemy target = deathSpreadBuffer[i];

            if (target == null || !target.IsAlive)
            {
                continue;
            }

            // 다른 반응과 같이 위치를 먼저 기록
            deathSpreadPositions.Add(target.transform.position);

            target.ApplyElementMark(element, 1, ElementMarkRules.Duration);
        }

        // [연동:UI] 점화 전염과 같은 이벤트. 중심은 죽은 적의 위치
        // 대상은 관리 목록 순서. 이웃이 상한보다 많으면 누가 받는지는 순서에 따름
        if (deathSpreadPositions.Count > 0)
        {
            GameEvents.RaiseChainReaction(
                element, center, deathSpreadPositions.ToArray());
        }
    }

    // 적 행동 컴포넌트가 플레이어를 직접 때릴 때 쓰는 체력

    // 적 행동 컴포넌트가 플레이어를 직접 때릴 때 쓰는 체력
    // 현재 소비자는 BossShockwave 하나. 원거리탄은 ProjectileLauncher 쪽을 사용
    private Health playerHealth;

    // 지속 장판. 씬 배선을 늘리지 않으려고 여기서 소유
    private readonly GroundAreaState groundAreas = new GroundAreaState();

    private void Awake()
    {
        if (playerTransform == null)
        {
            Debug.LogError("[EnemyManager] Player Transform 미연결. 적이 움직이지 않습니다.", this);

            return;
        }

        // 씬에 [SerializeField] 배선을 하나 더 늘리지 않으려고 Player 에서 직접 찾음
        playerHealth = playerTransform.GetComponent<Health>();

        if (playerHealth == null)
        {
            Debug.LogWarning(
                "[EnemyManager] Player 오브젝트에 Health 가 없습니다. 보스 충격파가 플레이어를 때리지 않습니다.",
                this);
        }
    }


    // TODO: 풀링 적용 후 반납이 이 루프에만 있음
    // 플레이어가 사라지는 구조가 생기면 정리 루프는 조건 밖으로 빼야 함
    private void Update()
    {
        if (playerTransform == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        Vector2 playerPosition = playerTransform.position;

        // 적보다 먼저. 만료된 장판이 이번 프레임에 영향을 주지 않게
        groundAreas.Tick(deltaTime, groundDamagePulses);

        // 이동 전에 장판 피해. 적이 이번 프레임에 서 있던 자리로 판정
        ApplyGroundDamage();

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeEnemies[i];

            // 죽거나 삭제된 적을 목록에서 제거
            if (enemy == null || !enemy.IsAlive)
            {
                RemoveAtSwapBack(i);

                if (enemy != null)
                {
                    pools[enemy.SourcePrefab].Release(enemy);
                }

                continue;
            }

            // 장판 둔화는 위치로 정해지므로 이동 직전에 갱신
            // 보스 면역이 원소마다 달라 IsBoss 를 같이 넘긴다
            enemy.SetGroundSlowMultiplier(
                groundAreas.GetSlowMultiplier(enemy.transform.position, enemy.IsBoss));

            // Manager에서 이동 처리
            enemy.Tick(deltaTime, playerPosition);
        }

    }

    // 모든 Update 가 끝난 뒤. 투사체와 적 순회 양쪽의 피해가 확정된 시점
    // playerTransform 과 무관하게 돌아야 대기열이 남지 않음
    private void LateUpdate()
    {
        ProcessPendingDeathSpreads();
    }

    // 화염 8레벨 불장판 피해. 
    // 판정은 장판 둔화와 같은 규칙으로 중심 거리만 확인
    // FindOverlappingEnemies 와 달리 적의 HitRadius 더하기 x.
    // 화면에 그려지는 원과 피해 범위를 맞추기 위해서
    // 보스에게도 적용
    private void ApplyGroundDamage()
    {
        for (int pulseIndex = 0; pulseIndex < groundDamagePulses.Count; pulseIndex++)
        {
            GroundAreaState.DamagePulse pulse = groundDamagePulses[pulseIndex];

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                Enemy enemy = activeEnemies[i];

                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Vector2 position = enemy.transform.position;

                if ((position - pulse.Center).sqrMagnitude >= pulse.Radius * pulse.Radius)
                {
                    continue;
                }

                enemy.TakeDamage(pulse.Damage);
            }
        }

        groundDamagePulses.Clear();
    }

    // 역순 순회 중 List.Remove로 앞쪽 지우면 뒤 항목이 당겨져 하나 건너 뜀
    // 그래서 지금 보고 있는 인덱스에만 마지막 항목을 덮어쓰는 방식으로 지움
    private void RemoveAtSwapBack(int index)
    {
        int lastIndex = activeEnemies.Count - 1;

        activeEnemies[index] = activeEnemies[lastIndex];

        activeEnemies.RemoveAt(lastIndex);
    }



    // [연동:스폰] 스폰 타이밍 결정 후 함수 호출
    // 외부에서 Enemy 직접 Instantiate 시 풀링 우회하게 됨
    // 생성, EnemyData 적용, 관리목록 등록을 처리
    public Enemy Spawn(EnemyData data, Vector2 position)
    {
        if (data == null || data.Prefab == null)
        {
            return null;
        }

        Enemy enemy = GetPool(data.Prefab).Get();

        enemy.transform.position = position;

        enemy.Initialize(data);

        // 반응이 주변 적을 찾을 수 있게.
        enemy.SetEnemyManager(this);

        // 원거리 공격을 가진 적(소환술사, 보스)에만 붙음
        // 행동 컴포넌트 종류가 늘어나면 공통 인터페이스로 묶을 것. 지금은 1종
        EnemyRangedAttack ranged = enemy.GetComponent<EnemyRangedAttack>();

        if (ranged != null)
        {
            ranged.Configure(projectileLauncher, playerTransform);
        }

        EnemySummon summon = enemy.GetComponent<EnemySummon>();

        if (summon != null)
        {
            summon.Configure(this, playerTransform);
        }

        // 보스에만 붙음. 2페이즈 전에는 꺼져 있지만 GetComponent 는 꺼진 것도 찾음
        BossShockwave shockwave = enemy.GetComponent<BossShockwave>();

        if (shockwave != null)
        {
            shockwave.Configure(playerHealth);
        }

        enemy.gameObject.SetActive(true);

        Register(enemy);

        return enemy;

    }

    private ObjectPool<Enemy> GetPool(Enemy prefab)
    {
        if (pools.TryGetValue(prefab, out ObjectPool<Enemy> existing))
        {
            return existing;
        }

        Enemy Create()
        {
            return CreateEnemy(prefab);
        }

        ObjectPool<Enemy> pool = new ObjectPool<Enemy>(
            createFunc: Create,
            actionOnRelease: DeactivateEnemy,
            actionOnDestroy: DestroyEnemy);

        pools.Add(prefab, pool);

        return pool;

    }


    // 풀이 비어 있을 때 호출
    private Enemy CreateEnemy(Enemy prefab)
    {
        Enemy created = Instantiate(prefab);

        // 활성 상태로 생성해 Awake를 돌린 뒤 즉시 비활성화
        created.SetSourcePrefab(prefab);
        created.gameObject.SetActive(false);

        return created;
    }

    private void DeactivateEnemy(Enemy enemy)
    {
        enemy.gameObject.SetActive(false);
    }


    private void DestroyEnemy(Enemy enemy)
    {
        Destroy(enemy.gameObject);
    }

    // [연동:스폰] 종료, 재시작 시 호출
    public void DespawnAll()
    {

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeEnemies[i];

            if (enemy != null)
            {
                pools[enemy.SourcePrefab].Release(enemy);
            }
        }

        activeEnemies.Clear();
        groundAreas.Clear();

        // 보스 등장 정리나 재시작 직후 남은 전염이 새 적에게 걸리지 않게
        pendingDeathSpreads.Clear();


    }
}

