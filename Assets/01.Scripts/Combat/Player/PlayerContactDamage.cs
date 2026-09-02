using UnityEngine;


// 플레이어가 적과 겹쳤는지 주기적으로 검사하여 피해 적용

[RequireComponent(typeof(Health))]
public class PlayerContactDamage : MonoBehaviour
{


    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float checkRadius = 0.4f;
    [SerializeField] private float checkInterval = 0.15f;

    // 피격 후 이 시간 동안 무적. 접촉 피해 빈도 설정
    [SerializeField] private float invincibilityDuration = 0.5f;

    // 적이 밀집하면 기존 32 버퍼가 부족할 수 있으므로 64로
    private const int OverlapBufferSize = 64;
    private static readonly Collider2D[] OverlapBuffer = new Collider2D[OverlapBufferSize];

    private Health health;
    private ContactFilter2D enemyFilter;
    private float checkTimer;


    // [임시] 인스펙터 확인용 필드화
    [SerializeField] private float invincibleTimer;


    // [연동:UI] 피격 깜빡임 연출 필요하면 사용 가능
    public bool IsInvincible => invincibleTimer > 0f;

    private void Awake()
    {
        health = GetComponent<Health>();

        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyLayers);
        enemyFilter.useTriggers = true;
    }


    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (invincibleTimer > 0f)
        {
            invincibleTimer -= deltaTime;
        }

        checkTimer -= deltaTime;

        if (checkTimer > 0f)
        {
            return;
        }

        checkTimer += Mathf.Max(0.02f, checkInterval);

        if (!health.IsAlive || invincibleTimer > 0f)
        {
            return;
        }

        ApplyHighestContactDamage();
    }


    // 동시에 겹친 적 중 ContactDamage가 가장 높은 하나만 적용.
    // 전부 합산 시 과도한 피해로 증발 방지
    private void ApplyHighestContactDamage()
    {
        int hitCount = Physics2D.OverlapCircle(transform.position, checkRadius, enemyFilter, OverlapBuffer);



        float highestDamage = 0f;

        for (int i = 0; i < hitCount; i++)
        {
            if (!OverlapBuffer[i].TryGetComponent(out Enemy enemy))
            {
                continue;
            }

            if (!enemy.IsAlive)
            {
                continue;
            }

            if (enemy.ContactDamage > highestDamage)
            {
                highestDamage = enemy.ContactDamage;
            }


        }

        if (highestDamage <= 0f)
        {
            return;
        }

        health.TakeDamage(highestDamage);

        invincibleTimer = invincibilityDuration;

    }

}
