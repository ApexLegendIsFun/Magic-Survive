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


    // 비워두면 ProjectileLauncher의 Default Projectile Prefab이 쓰임
    [SerializeField] private Projectile projectilePrefab;


    private Enemy enemy;

    private ProjectileLauncher launcher;
    private Transform playerTransform;

    private float attackTimer;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    // EnemyManager.Spawn이 풀에서 꺼낸 직후 1회 호출
    // 프리팹이라 씬 오브젝트를 [SerializeField]로 받을 수 없어 주입받음
    public void Configure(ProjectileLauncher projectileLauncher, Transform player)
    {
        launcher = projectileLauncher;
        playerTransform = player;

        // 풀에서 재사용될 때 이전 개체의 남은 주기를 이어받지 않도록 초기화
        attackTimer = attackInterval;
    }

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
        ProjectileSpec spec = new ProjectileSpec(
            projectilePrefab, damage, projectileSpeed, maxDistance, hitRadius, 0);

        launcher.FireAtPlayer(spec, origin, toPlayer.normalized);
    }
}

