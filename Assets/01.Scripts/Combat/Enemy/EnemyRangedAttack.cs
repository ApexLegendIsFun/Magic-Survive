using UnityEngine;

// 적이 플레이어에게 일정 주기로 투사체를 쏘는 행동
// 소환술사가 사용하고, 보스 부채꼴도 이 발사 경로를 씀
[RequireComponent(typeof(Enemy))]

public class EnemyRangedAttack : MonoBehaviour
{

    [Header("기획서 §5 소환술사")]
    [SerializeField] private float attackInterval = 2.5f;
    [SerializeField] private float damage = 8f;


    [Header("기획에 수치가 없어 정한 임시값")]

    // "느린 원거리탄"이라 보스 투사체 6보다 느리게 잡음
    [SerializeField] private float projectileSpeed = 3f;
    [SerializeField] private float maxDistance = 12f;
    [SerializeField] private float hitRadius = 0.25f;


    // 이 거리 안에 플레이어가 있을 때만 쏘기
    [SerializeField] private float attackRange = 8f;


    [Header("부채꼴. 1발 0도면 기존 단발과 동일")]
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float angleStepDegrees = 0f;


    // 비워두면 ProjectileLauncher의 Default Projectile Prefab이 쓰임
    [SerializeField] private Projectile projectilePrefab;


    private Enemy enemy;

    private ProjectileLauncher launcher;
    private Transform playerTransform;

    private float attackTimer;

    // 2페이즈가 주기와 발수를 바꾸므로 인스펙터 원본을 따로 보관
    private float baseAttackInterval;
    private int baseProjectileCount;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        baseAttackInterval = attackInterval;
        baseProjectileCount = projectileCount;
    }

    // EnemyManager.Spawn이 풀에서 꺼낸 직후 1회 호출
    // 프리팹이라 씬 오브젝트를 [SerializeField]로 받을 수 없어 주입받음
    public void Configure(ProjectileLauncher projectileLauncher, Transform player)
    {
        launcher = projectileLauncher;
        playerTransform = player;

        // 2페이즈에서 바뀐 값이 풀 재사용으로 다음 개체에 남지 않게 원본으로 되돌림
        attackInterval = baseAttackInterval;
        projectileCount = baseProjectileCount;

        // 풀에서 재사용될 때 이전 개체의 남은 주기를 이어받지 않도록 초기화
        attackTimer = attackInterval;
    }

    // 보스 2페이즈 진입 시 BossPhaseController 가 1회 호출
    // 진행 중인 주기는 그대로 두고 다음 주기부터 새 값이 적용
    public void SetSpread(int count, float interval)
    {
        projectileCount = Mathf.Max(1, count);
        attackInterval = Mathf.Max(0.1f, interval);
    }

    // 이 컴포넌트가 붙는 적은 소환술사와 보스뿐이라 개별 Update를 둠

    // 이 컴포넌트가 붙는 적은 소환술사와 보스뿐이라 개별 Update를 둠
    // 100마리가 쓰는 Enemy.Tick과 달리 동시에 1~2개라
    // EnemyManager에 행동 Tick 목록을 만드는 쪽이 오히려 복잡해질 수 있으므로
    private void Update()
    {
        if (launcher == null || playerTransform == null)
        {
            return;
        }

        if (!enemy.IsAlive)
        {
            return;
        }

        // 빙결 중에는 쏘지 않음
        // 기획에 언급이 없어 "멈춘다"에 공격 중지를 포함하는 쪽으로 정했음. 확인 요청 대상
        if (enemy.IsFrozen)
        {
            return;
        }

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
        {
            return;
        }

        // 남은 시간을 다음 주기로 보존
        attackTimer += attackInterval;

        Fire();
    }

    private void Fire()
    {
        Vector2 origin = transform.position;
        Vector2 toPlayer = (Vector2)playerTransform.position - origin;

        float sqrDistance = toPlayer.sqrMagnitude;

        // 사거리 밖이면 이번 주기는 건너뜀. 주기는 이미 소비
        if (sqrDistance > attackRange * attackRange || sqrDistance < 0.0001f)
        {
            return;
        }

        // SkillLevel 0. 적 투사체는 원소 표식을 걸지 않음
        // readonly struct 라 한 번 만들어 모든 탄이 공유해도 안전하도록.
        ProjectileSpec spec = new ProjectileSpec(
            projectilePrefab, damage, projectileSpeed, maxDistance, hitRadius, 0);

        Vector2 baseDirection = toPlayer.normalized;

        int count = Mathf.Max(1, projectileCount);

        // 가운데 탄이 플레이어를 향하도록 좌우 대칭으로 벌림
        // 3발 20도면 -20 / 0 / +20, 5발이면 -40 / -20 / 0 / +20 / +40
        float startAngle = -angleStepDegrees * (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            launcher.FireAtPlayer(
                spec, origin, Rotate(baseDirection, startAngle + angleStepDegrees * i));
        }
    }

    // 2D 한 축 회전이라 Quaternion 보다 이쪽을 채택
    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;

        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos);
    }
}

