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

    private Health health;

    private Enemy sourcePrefab;

    public Enemy SourcePrefab => sourcePrefab;

    public float CrowdControlDurationMultiplier => 1f;

    private float baseMaxHealth;
    private float baseContactDamage;

    public bool IsKnockbackImmune => false;

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
    }

    private void OnEnable()
    {
        // Health에서 사망 이벤트 받음
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        health.Died -= HandleDied;
    }

    // [연동:전투] 투사체가 적을 때릴 때 호출
    // Health를 통째로 노출하지 않는 이유: 외부에서 ResetHealth/SetMaxHealth까지
    // 부를 수 있게 되면 적의 생명주기가 EnemyManager 밖에서 흔들림
    public void TakeDamage(float amount)
    {
        health.TakeDamage(amount);
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
    }

    // 스폰 직후 SpawnDirector가 1회 호출
    public void ApplyDifficulty(float healthMultiplier, float damageMultiplier)
    {
        contactDamage = baseContactDamage * damageMultiplier;

        health.SetMaxHealth(baseMaxHealth * healthMultiplier, true);
    }

    // EnemyManager에서 매 프레임 호출합니다
    // 현재는 플레이어 방향으로 직선적으로 추적함
    public void Tick(float deltaTime, Vector2 playerPosition)
    {

        markState.Tick(deltaTime);

        Vector2 currentPosition = transform.position;
        Vector2 toPlayer = playerPosition - currentPosition;

        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 direction = toPlayer.normalized;

        transform.position = currentPosition + direction * moveSpeed * deltaTime;
    }


    // [연동:성장] 사망 위치, 경험치 보상 전달
    private void HandleDied()
    {
        GameEvents.RaiseEnemyKilled(transform.position, experienceReward);
    }
}