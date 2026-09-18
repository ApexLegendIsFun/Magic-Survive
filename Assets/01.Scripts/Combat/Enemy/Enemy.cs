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

    // 지속 장판 둔화. EnemyManager 가 Tick 직전에 매 프레임 세팅
    // 1 이면 장판 밖.
    private float groundSlowMultiplier = 1f;

    // 암흑 3레벨 해금 여부
    private bool darkAmplificationUnlocked;

    // EnemyManager.Spawn 이 주입.
    // 반응이 주변 적을 찾아야 할 때 사용. 프리팹이라 [SerializeField] 로는 못 받음
    private EnemyManager enemyManager;

    // 냉기 5레벨. 빙결을 건 얼음창이 5레벨이었는지만 기억.
    // 적은 스킬 레벨을 모르므로, 레벨을 아는 투사체가 권한만 남기고 감
    private bool frostShatterArmed;


    // 마지막으로 DarkAmplifiedChanged 로 알린 상태
    // 증폭은 해금과 스택 수에서 파생되는 값이라 바뀌는 순간을 직접 잡아야 함
    private bool darkAmplifiedNotified;

    private float baseMaxHealth;
    private float baseContactDamage;

    // EnemyData 가 지정하는 제어 면역. Initialize 가 매 Spawn 마다 덮는다
    private bool isBoss;

    private Health health;

    // 거리 유지 이동. 소환술사에만 붙어 있고 없으면 null
    private EnemyKeepDistance keepDistance;

    // 돌진 이동. 돌진자에만 붙어 있고 없으면 null
    private EnemyDash dash;

    private Enemy sourcePrefab;

    public Enemy SourcePrefab => sourcePrefab;


    // 0f 를 돌려주면 ApplyFreeze 가 applied <= 0f 가드에서 빠져나가므로
    // 호출부를 바꾸지 않고 빙결 면역이 됨
    public float CrowdControlDurationMultiplier => isBoss ? 0f : 1f;

    public bool IsKnockbackImmune => isBoss;

    // [연동:Combat] 아직 안 만든 보스 예외가 조회.
    // 기획의 "보스에게는 연쇄 대상이 보스 1명으로 제한"과 암흑 8레벨 처형 면역
    public bool IsBoss => isBoss;

    // [연동:UI] 빙결 상태 변화. true=시작, false=해제
    // 구독은 활성화 뒤에, 해제는 비활성화될 때
    public event Action<bool> FrozenChanged;

    // [연동:Combat] 개체 단위 사망. 현재 소비자는 EnemySummon 하나
    // 풀 반환 시 null 로 비우지 않을 것.
    public event Action<Enemy> Killed;

    public bool IsFrozen => freezeRemainingSeconds > 0f;

    // [연동:UI] 암흑 3레벨 증폭 상태 변화. true=시작, false=해제
    // 기획과 발송본의 "저주" 부분
    //
    // 적은 풀에서 재사용되므로 구독부는 OnEnable 에서 IsDarkAmplified 를 한 번 읽어
    // 현재 상태를 맞추고, OnDisable 에서 자기 연출을 꺼야 함
    // 해제 이벤트가 반드시 먼저 온다고 가정 x
    public event Action<bool> DarkAmplifiedChanged;

    // [연동:UI] 지금 증폭이 걸려 있는가
    //
    // 해금과 3중첩이 둘 다 참이어야 함
    // 스택만 보면 3중첩이어도 스킬 3레벨 전이면 증폭이 안 걸린 상태를 놓침
    public bool IsDarkAmplified =>
        darkAmplificationUnlocked
        && markState.Get(MagicElement.Dark).Stacks >= ElementMarkRules.MaximumStacks;

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

        RefreshDarkAmplified();
    }

    public void ConsumeElementMarks(MagicElement element, int amount)
    {
        markState.Consume(element, amount);

        RefreshDarkAmplified();
    }

    // 암흑 증폭이 켜지고 꺼지는 순간에만 알림
    //
    // markState.Changed 를 구독하지 않는 이유: Reset() 은 Changed 를 발행하지 않아서
    // 풀 반환과 재사용을 놓치므로, 스택이 바뀌는 지점에서 직접 부르는 편이 확실
    //
    // darkAmplificationUnlocked 가 false 면 단락 평가로 Get 까지 가지 안흥ㅁ
    // 일반 적은 거의 항상 false 라 Tick 비용이 사실상 0.
    private void RefreshDarkAmplified()
    {
        bool current = IsDarkAmplified;

        if (current == darkAmplifiedNotified)
        {
            return;
        }

        darkAmplifiedNotified = current;

        DarkAmplifiedChanged?.Invoke(current);
    }


    // 냉기 표식 3중첩 반응. Projectile이 호출
    // 지속시간에 CrowdControlDurationMultiplier를 곱하기.
    // 보스는 이 값이 0f 라 applied 가 0 이 되고 빙결이 걸리지 않음
    //
    // armShatter 는 5레벨 파괴 권한. 보스는 위 가드에서 먼저 빠져나가
    // 권한도 받지 않는다
    public void ApplyFreeze(float durationSeconds, bool armShatter)
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

        // 지속시간이 갱신되지 않아도 권한은 줌
        // 긴 빙결이 남아 있을 때 5레벨 얼음창이 또 맞으면 파괴는 열려야 함
        if (armShatter)
        {
            frostShatterArmed = true;
        }
    }


    // 암흑 표식 3중첩 반응
    public void SetDarkAmplificationUnlocked()
    {
        darkAmplificationUnlocked = true;

        // 투사체가 부르는 시점은 이미 3중첩 전이라 여기서 바로 켜짐
        RefreshDarkAmplified();
    }

    // EnemyManager.Spawn 이 풀에서 꺼낸 직후 1회 호출
    public void SetEnemyManager(EnemyManager manager)
    {
        enemyManager = manager;
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

    // [연동:Combat] 돌진 예고 선 길이 계산에 필요
    // 냉기 둔화가 반영되지 않은 원본 값.
    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        health = GetComponent<Health>();

        // 이동 방식을 가진 적에만 붙어 있음. 없으면 기존 추적 그대로
        keepDistance = GetComponent<EnemyKeepDistance>();

        dash = GetComponent<EnemyDash>();
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
        groundSlowMultiplier = 1f;

        // 빙결 중 반환되면 해제를 알림
        if (freezeRemainingSeconds > 0f)
        {
            freezeRemainingSeconds = 0f;
            FrozenChanged?.Invoke(false);
        }

        frostShatterArmed = false;

        // 상태를 먼저 확정하고 알림
        // 구독부가 false 를 받은 순간 IsDarkAmplified 를 다시 읽어도 false 여야 함
        bool wasDarkAmplified = darkAmplifiedNotified;

        darkAmplificationUnlocked = false;
        darkAmplifiedNotified = false;

        if (wasDarkAmplified)
        {
            DarkAmplifiedChanged?.Invoke(false);
        }
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

        TryFrostShatter(appliedDamage);
    }

    // 냉기 5레벨. 빙결 중에 피해를 받으면 그 자리에서 파괴가 터짐
    //
    // 어떤 피해든 상관없음. 직격, 화염 도트, 충격파, 연쇄 전부 여기를 통과
    // 실제로 깎인 양이 0 이면 터지지 않음. 오버킬 뒤 중복 호출 막기.
    //
    //  IsResolvingFrostShatter 를 권한 소비보다 먼저 본다
    private void TryFrostShatter(float appliedDamage)
    {
        if (!frostShatterArmed
            || appliedDamage <= 0f
            || freezeRemainingSeconds <= 0f
            || enemyManager == null
            || enemyManager.IsResolvingFrostShatter)
        {
            return;
        }

        // 한 번의 빙결에 한 번만. 0.6초 동안 맞을 때마다 터지면 과하다
        frostShatterArmed = false;

        enemyManager.ResolveFrostShatter(transform.position);
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

        isBoss = data.IsBoss;

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

    // 보스 2페이즈 진입 시 BossPhaseController 가 1회 호출
    public void MultiplyMoveSpeed(float multiplier)
    {
        if (multiplier <= 0f)
        {
            return;
        }

        moveSpeed *= multiplier;
    }

    // EnemyManager 가 Tick 직전에 부름
    // 보스 면역은 GroundAreaState 가 원소별로 이미 걸렀으므로 여기서는 보지 않음
    public void SetGroundSlowMultiplier(float multiplier)
    {
        groundSlowMultiplier = Mathf.Clamp01(multiplier);
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

        // 표식 만료로 3중첩이 깨지는 순간을 여기서 잡기
        // 빙결 early return 보다 앞이어야 빙결 중에도 해제가 알려짐
        RefreshDarkAmplified();

        // 빙결 중에는 이동만 멈추기
        // TickFireDot과 markState.Tick보다 뒤에 있어야 도트와 표식 만료가 계속 돌아감
        if (freezeRemainingSeconds > 0f)
        {
            freezeRemainingSeconds -= deltaTime;

            // 해제되는 프레임에 알린다. 이 프레임의 이동은 그대로 막는다
            if (freezeRemainingSeconds <= 0f)
            {
                freezeRemainingSeconds = 0f;
                frostShatterArmed = false;
                FrozenChanged?.Invoke(false);
            }

            return;
        }


        Vector2 currentPosition = transform.position;
        Vector2 toPlayer = playerPosition - currentPosition;

        // 이동 방식을 가진 적은 방향만 위임받음.
        // 위치 대입은 아래에서 한 번만
        
        // 돌진은 방향뿐 아니라 속도도 바꾸므로 배율을 함께 받기.
        // 이동 방식이 3종째가 되면 공통 인터페이스로 묶을 것. 지금은 2종.
        Vector2 direction;
        float speedMultiplier = 1f;

        if (dash != null && dash.isActiveAndEnabled)
        {
            direction = dash.Tick(deltaTime, currentPosition, playerPosition);

            speedMultiplier = dash.SpeedMultiplier;
        }
        else if (keepDistance != null && keepDistance.isActiveAndEnabled)
        {
            direction = keepDistance.GetMoveDirection(currentPosition, playerPosition);
        }
        else
        {
            direction = toPlayer.normalized;
        }

        // 겹쳐서 방향을 못 정했거나, 유지 구간이라 멈추는 경우
        // 예고 중과 전환 프레임도 여기로 빠짐.
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // 냉기 표식 중첩당 이동속도 감소. moveSpeed 원본은 그대로 두어 만료 시 복원
        // 기획에서 보스에게 둔화 면역을 지정했으므로. 표식은 그대로 쌓이고 속도만 안 깎이게
        int frostStacks = isBoss ? 0 : markState.Get(MagicElement.Frost).Stacks;

        // 장판 둔화는 원소별로 보스 면역이 다름
        // 대지 지진은 보스에게도 걸리므로 여기서 isBoss 로 일괄 면제 x
        // 냉기 서리 장판만 GroundAreaState 가 걸러서 1 을 돌려줌
        //
        // 표식 둔화와는 곱하기. 서로 다른 계열
        float currentSpeed = moveSpeed * (1f - frostStacks * FrostMovementSpeedReductionPerStack) * groundSlowMultiplier * speedMultiplier;

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


