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
        // 만들어지지 않았으면 알리지 않음 연출만 나오고 판정이 없는 상태를 막음
        if (!groundAreas.Add(element, center, radius, durationSeconds, slowPercent))
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
        groundAreas.Tick(deltaTime);

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


    }
}

