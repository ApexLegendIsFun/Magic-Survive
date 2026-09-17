using UnityEngine;

// 보스 체력이 일정 비율 이하로 내려가면 2페이즈로 한 번 전환
[RequireComponent(typeof(Enemy))]
public class BossPhaseController : MonoBehaviour
{
    [Header("기획서 보스 2페이즈")]

    // 최대 체력의 이 비율 이하가 되면 전환
    [Range(0.05f, 0.95f)]
    [SerializeField] private float phaseTwoHealthRatio = 0.7f;

    [SerializeField] private int phaseTwoProjectileCount = 5;
    [SerializeField] private float phaseTwoAttackInterval = 4f;
    [SerializeField] private float moveSpeedMultiplier = 1.25f;

    private Enemy enemy;
    private Health health;

    // 보스 프리팹에는 둘 다 있지만 강제하지는 않음
    // RequireComponent 로 묶으면 소환이 없는 보스를 만들 수 없게 되므로
    private EnemyRangedAttack rangedAttack;
    private EnemySummon summon;
    private BossShockwave shockwave;

    private bool isPhaseTwo;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        // Enemy 가 [RequireComponent(typeof(Health))] 라 반드시 존재
        health = GetComponent<Health>();

        rangedAttack = GetComponent<EnemyRangedAttack>();

        summon = GetComponent<EnemySummon>();

        shockwave = GetComponent<BossShockwave>();

        // 프리팹 설정을 빠뜨리면 전환이 절반만 일어나 정상처럼 보임
        if (rangedAttack == null || summon == null || shockwave == null)
        {
            Debug.LogWarning(
                "[BossPhaseController] EnemyRangedAttack, EnemySummon, BossShockwave 중 " +
                "없는 것이 있습니다. 2페이즈에서 해당 변화는 일어나지 않습니다.", this);
        }
    }

    // EnemyManager.Spawn 은 Initialize 로 체력을 되돌린 뒤에 SetActive(true).
    // 이벤트를 기다리지 말고 지금 체력을 직접 읽어 상태를 맞춤
    private void OnEnable()
    {
        isPhaseTwo = false;

        // 소환은 2페이즈 전용이다
        // 프리팹에서 꺼둬도 풀 재사용 때 다시 보장해야 함
        if (summon != null)
        {
            summon.enabled = false;
        }

        if (shockwave != null)
        {
            shockwave.enabled = false;
        }

        health.HealthChanged += HandleHealthChanged;

        // 풀에서 꺼낸 직후라 보통 만피지만 가정하지 않음
        HandleHealthChanged(health.CurrentHealth, health.MaxHealth);
    }

    private void OnDisable()
    {
        health.HealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        // Health.TakeDamage 는 HealthChanged 를 먼저 발행하고 그 뒤에 Died 를 부름
        // 만피에서 즉사하면 체력 0 으로 이 핸들러가 먼저 오므로
        // 사망 체력을 걸러내지 않으면 죽는 프레임에 2페이즈로 들어감
        if (isPhaseTwo || currentHealth <= 0f || maxHealth <= 0f)
        {
            return;
        }

        // 나눗셈 대신 곱셈. "최대 체력의 70% 이하"라는 의도에 더 가깝다
        //
        // float 비교라 경계가 기획 수치와 정확히 일치하지 않음
        // 최대 2000 기준으로 1400 에서는 통과하고 1399 에서 전환되는 것을 실측 완료
        if (currentHealth > maxHealth * phaseTwoHealthRatio)
        {
            return;
        }

        EnterPhaseTwo();
    }

    private void EnterPhaseTwo()
    {
        isPhaseTwo = true;

        enemy.MultiplyMoveSpeed(moveSpeedMultiplier);

        if (rangedAttack != null)
        {
            rangedAttack.SetSpread(phaseTwoProjectileCount, phaseTwoAttackInterval);
        }

        if (summon != null)
        {
            summon.enabled = true;
        }

        if (shockwave != null)
        {
            shockwave.enabled = true;
        }

    }
}
