using System;
using UnityEngine;

[RequireComponent(typeof(Health))]

// 적 이동, 사망 처리
public class Enemy : MonoBehaviour, IElementMarkTarget
{

    [Header("EnemyData 안 쓰고 직접 배치 했을 시 값(씬 직접 배치는 미지원)")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float contactDamage = 5f;
    [SerializeField] private int experienceReward = 1;

    // 물리 Collider 대신 사용하는 피격 반경, 기존 콜라이더 크기 참고해 0.5로 시작
    // 실제 캐릭터 크기가 확정되면 다시 조정
    [SerializeField] private float hitRadius = 0.5f;

    // Initialize가 비활성 상태에서 호출돼도 이미 살아 있음
    private readonly ElementMarkState markState = new ElementMarkState();

    // 표식 공통 효과. 개편 기획서 공통 규칙에는 없으므로 지시가 올 때까지 유지
    private const float DamageTakenPerTotalStack = 0.05f;

    // 원소별 고유 효과. 카탈로그 확정값 임시 미러링
    // 암흑은 중첩당이 아니라 3중첩 문턱 효과가 되어 ElementReactionValues로 옮김
    private const float FireDotDamagePerStack = 1f;
    private const float FrostMovementSpeedReductionPerStack = 0.10f;



    // 매 프레임 HealthChanged가 발행되는 것을 피하기 위해 1초 단위로 처리
    private const float FireDotIntervalSeconds = 1f;

    private float fireDotTimer = FireDotIntervalSeconds;

    // 냉기 3중첩 빙결. 남은 시간이 있으면 이동x
    private float freezeRemainingSeconds;

    // 암흑 3레벨 해금 여부
    private bool darkAmplificationUnlocked;

    private float baseMaxHealth;
    private float baseContactDamage;

    private Health health;

    // 거리 유지 이동. 소환술사에만 붙어 있고 없으면 null
    private EnemyKeepDistance keepDistance;

    private Enemy sourcePrefab;

    public Enemy SourcePrefab => sourcePrefab;

    public float CrowdControlDurationMultiplier => 1f;

    public bool IsKnockbackImmune => false;

    // [연동:UI] 빙결 상태 변화. true=시작, false=해제
    // 구독은 활성화 뒤에, 해제는 비활성화될 때
    public event Action<bool> FrozenChanged;

    // [연동:Combat] 개체 단위 사망. 현재 소비자는 EnemySummon 하나
    // 풀 반환 시 null 로 비우지 않을 것.
    public event Action<Enemy> Killed;

    public bool IsFrozen => freezeRemainingSeconds > 0f;

    // 구독을 markState로 그대로 넘김
    public event Action<ElementMarkChange> ElementMarkChanged
    {
        add { markState.Changed += value; }

        remove { markState.Changed -= value; }
    }

    public ElementMarkSnapshot GetElementMark(MagicElement element)
    {

        return markState.Get(element);

    }

    public void ApplyElementMark(MagicElement element, int amount, float duration)
    {
        markState.Apply(element, amount, duration);
    }

    public void ConsumeElementMarks(MagicElement element, int amount)
    {
        markState.Consume(element, amount);
    }


    // 냉기 표식 3중첩 반응. Projectile이 호출
    // 지속시간에 CrowdControlDurationMultiplier를 곱하는 이유는 보스 제어 면역.
    // 현재 이 값은 1f 고정이고, 보스 작업에서 보스만 0f를 반환하게 하면
    // 여기와 호출부는 바뀌지 않음
    public void ApplyFreeze(float durationSeconds)
    {
        float applied = durationSeconds * CrowdControlDurationMultiplier;

        if (applied <= 0f)
        {
            return;
        }

        // 짧은 빙결이 남아 있는 긴 빙결을 덮어쓰지 않게 최댓값 유지
        if (applied > freezeRemainingSeconds)
        {
            bool wasFrozen = freezeRemainingSeconds > 0f;

            freezeRemainingSeconds = applied;

            if (!wasFrozen)
            {
                FrozenChanged?.Invoke(true);
            }
        }
    }


    // 암흑 표식 3중첩 반응
    public void SetDarkAmplificationUnlocked()
    {
        darkAmplificationUnlocked = true;
    }


    // 대지 1레벨 밀치기. 투사체가 적중 시 호출
    //
    // 즉시 위치 이동이라 Tick의 이동 계산과 만나지 않음
    // 빙결 중에도 밀쳐짐.
    //
    public void ApplyKnockback(Vector2 direction, float distance)
    {
        // 충격파가 대상을 죽인 뒤 호출될 수 있음. 죽은 적은 밀치지 않음
        if (!IsAlive || IsKnockbackImmune || distance <= 0f)
        {
            return;
        }

        Vector2 currentPosition = transform.position;

        transform.position = currentPosition + direction.normalized * distance;
    }


    public void SetSourcePrefab(Enemy prefab)
    {
        sourcePrefab = prefab;
    }


    // EnemyManager에서 생존 여부 확인용
    public bool IsAlive => health != null && health.IsAlive;

    public float ContactDamage => contactDamage;

    public float HitRadius => hitRadius;

    private void Awake()
    {
        health = GetComponent<Health>();

        // 이동 방식을 가진 적에만 붙어 있음. 없으면 기존 추적 그대로
        keepDistance = GetComponent<EnemyKeepDistance>();
    }

    private void OnEnable()
    {
        // Health에서 사망 이벤트 받음
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        health.Died -= HandleDied;

        // 풀 반환 시 표식과 화염 도트 주기, 빙결을 초기화
        markState.Reset();
        fireDotTimer = FireDotIntervalSeconds;

        // 빙결 중 반환되면 해제를 알림
        if (freezeRemainingSeconds > 0f)
        {
            freezeRemainingSeconds = 0f;
            FrozenChanged?.Invoke(false);
        }

        darkAmplificationUnlocked = false;
    }


    /// <summary>
    /// 적 전용 받는 피해 보정을 적용한 뒤 Health에 전달
    /// amount는 보정 전 피해량
    /// </summary>
    // [연동:전투] 투사체가 적을 때릴 때 호출
    // Health를 통째로 노출하지 않는 이유: 외부에서 ResetHealth/SetMaxHealth까지
    // 부를 수 있게 되면 적의 생명주기가 EnemyManager 밖에서 흔들림
    public void TakeDamage(float amount)
    {
        // 공통 효과 원소별이 아니라 모든 원소 중첩의 합
        float multiplier = 1f + markState.TotalStacks * DamageTakenPerTotalStack;

        // 암흑 3레벨. 중첩당 가산이 아니라 3중첩 문턱에서 한 번만
        if (darkAmplificationUnlocked
            && markState.Get(MagicElement.Dark).Stacks >= ElementMarkRules.MaximumStacks)
        {
            multiplier += ElementReactionValues.DarkAmplificationBonus;
        }

        // [연동:UI] Damage Number는 요청량이 아니라 실제로 깎인 양을 받음
        // 이미 죽었거나 오버킬이면 요청량보다 작거나 0
        float healthBeforeDamage = health.CurrentHealth;

        health.TakeDamage(amount * multiplier);

        float appliedDamage = healthBeforeDamage - health.CurrentHealth;

        if (appliedDamage > 0f)
        {
            GameEvents.RaiseEnemyDamaged(transform.position, appliedDamage);
        }

    }


    // EnemyData의 수치를 실제 적에게 적용
    // EnemyManager.Spawn()에서 호출
    public void Initialize(EnemyData data)
    {
        if (data == null)
        {
            return;
        }

        moveSpeed = data.MoveSpeed;
        experienceReward = data.ExperienceReward;

        baseMaxHealth = data.MaxHealth;
        baseContactDamage = data.ContactDamage;

        contactDamage = baseContactDamage;
        health.ResetHealth(baseMaxHealth);

        markState.Reset();

        fireDotTimer = FireDotIntervalSeconds;

    }

    // 소환된 적의 EXP 미지급. 소환 주체가 Spawn 직후 1회 호출
    //
    // Initialize 가 매 Spawn 마다 EnemyData 값으로 덮으므로
    // 풀에서 재사용돼도 이 억제가 다음 개체에 남지 않음
    public void SuppressExperienceReward()
    {
        experienceReward = 0;
    }

    // 스폰 직후 SpawnDirector가 1회 호출
    public void ApplyDifficulty(float healthMultiplier, float damageMultiplier)
    {
        contactDamage = baseContactDamage * damageMultiplier;

        health.SetMaxHealth(baseMaxHealth * healthMultiplier, true);
    }

    // EnemyManager에서 매 프레임 호출
    // 기본은 플레이어 방향으로 직선 추적이고,
    // EnemyKeepDistance가 붙어 있고 켜져 있으면 방향만 그쪽에서 받음
    public void Tick(float deltaTime, Vector2 playerPosition)
    {

        // markState.Tick보다 앞이어야 함
        // 뒤에 두면 표식이 만료되는 프레임에 스택이 이미 0이라 마지막 도트 틱이 사라짐

        TickFireDot(deltaTime);

        markState.Tick(deltaTime);

        // 빙결 중에는 이동만 멈추기
        // TickFireDot과 markState.Tick보다 뒤에 있어야 도트와 표식 만료가 계속 돌아감
        if (freezeRemainingSeconds > 0f)
        {
            freezeRemainingSeconds -= deltaTime;

            // 해제되는 프레임에 알린다. 이 프레임의 이동은 그대로 막는다
            if (freezeRemainingSeconds <= 0f)
            {
                freezeRemainingSeconds = 0f;
                FrozenChanged?.Invoke(false);
            }

            return;
        }

        Vector2 currentPosition = transform.position;
        Vector2 toPlayer = playerPosition - currentPosition;

        // 이동 방식을 가진 적은 방향만 위임받음
        // 위치 대입은 아래에서 한 번만.
        Vector2 direction = keepDistance != null && keepDistance.isActiveAndEnabled
            ? keepDistance.GetMoveDirection(currentPosition, playerPosition)
            : toPlayer.normalized;

        // 겹쳐서 방향을 못 정했거나, 유지 구간이라 멈추는 경우
        // 기존 추적도 toPlayer가 0이면 normalized가 zero를 돌려주므로 동일한 동작
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // 냉기 표식 중첩당 이동속도 감소. moveSpeed 원본은 그대로 두어 만료 시 복원
        int frostStacks = markState.Get(MagicElement.Frost).Stacks;
        float currentSpeed = moveSpeed * (1f - frostStacks * FrostMovementSpeedReductionPerStack);

        transform.position = currentPosition + direction * currentSpeed * deltaTime;
    }

    // 화염표식 지속피해. Enemy.TakeDamage를 지나므로 공통 표식 효과가 함께 적용
    // 자기 화염 스택도 총 중첩에 포함되므로 도트가 스스로를 증폭
    private void TickFireDot(float deltaTime)
    {

        int fireStacks = markState.Get(MagicElement.Fire).Stacks;

        if (fireStacks <= 0)
        {
            // 표식이 사라졌을 때만 주기 되돌림
            fireDotTimer = FireDotIntervalSeconds;
            return;
        }

        fireDotTimer -= deltaTime;

        if (fireDotTimer > 0f)
        {
            return;
        }

        fireDotTimer += FireDotIntervalSeconds;

        TakeDamage(fireStacks * FireDotDamagePerStack);
    }


    // [연동:성장] 사망 위치, 경험치 보상 전달
    private void HandleDied()
    {
        GameEvents.RaiseEnemyKilled(transform.position, experienceReward);

        // 개체 단위 통지는 전역 이벤트 뒤에 발행
        // Health.TakeDamage 가 !isAlive 면 바로 빠져나가므로 사망당 한 번만
        Killed?.Invoke(this);
    }
}

