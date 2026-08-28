using UnityEngine;

[CreateAssetMenu(fileName = "SimpleProjectileAttack", menuName = "Combat/Test/Simple Projectile Attack")]


// [임시] IAttackSource 확인용 테스트 SO
// [연동:성장] 마법 SO 만들 때 참고 가능

public class SimpleProjectileAttack : ScriptableObject, IAttackSource
{

    // 사용할 투사체 프리팹
    [SerializeField] private Projectile projectilePrefab;

    // 공격 기본 수치
    [SerializeField] private float cooldown = 0.8f;
    [SerializeField] private float range = 8f;
    [SerializeField] private float damage = 5f;

    // 투사체 수치
    [SerializeField] private float speed = 12f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float hitRadius = 0.25f;

    // 0: 명중시 소멸
    // 1: 한명 관통 후 두번째 적에서 소멸
    [SerializeField] private int pierceCount = 0;

    public float Cooldown => cooldown;
    public float Range => range;

    public bool Execute(in AttackContext context)
    {

        // 타겟이나 Launcher 없으면 공격 X
        if (context.Target == null || context.Launcher == null)
        {
            return false;
        }

        Vector2 targetPosition = context.Target.transform.position;


        // 발사 위치 -> 타겟 방향
        Vector2 direction = targetPosition - context.Origin;

        // 타겟과 발사 위치 거의 같으면 공격 X(방향이 없음)
        if (direction.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        // 이번 발사에 사용할 수치 묶어 전달
        ProjectileSpec spec = new ProjectileSpec(projectilePrefab, damage, speed, maxDistance, hitRadius, pierceCount);

        context.Launcher.Fire(spec, context.Origin, direction);

        return true;
    }
}