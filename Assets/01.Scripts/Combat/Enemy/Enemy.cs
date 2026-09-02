using UnityEngine;

[RequireComponent(typeof(Health))]

// 적 이동, 사망 처리
public class Enemy : MonoBehaviour
{

    [Header("EnemyData 안 쓰고 직접 배치 했을 시 값(씬 직접 배치는 미지원)")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float contactDamage = 5f;
    [SerializeField] private int experienceReward = 1;

    private Health health;

    private Enemy sourcePrefab;

    public Enemy SourcePrefab => sourcePrefab;



    public void SetSourcePrefab(Enemy prefab)
    {
        sourcePrefab = prefab;
    }

    // EnemyManager에서 생존 여부 확인용
    public bool IsAlive => health != null && health.IsAlive;

    public float ContactDamage => contactDamage;

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

    // EnemyData의 수치를 실제 적에게 적용
    // EnemyManager.Spawn()에서 호출
    public void Initialize(EnemyData data)
    {
        if (data == null)
        {
            return;
        }

        moveSpeed = data.MoveSpeed;
        contactDamage = data.ContactDamage;
        experienceReward = data.ExperienceReward;

        health.ResetHealth(data.MaxHealth);
    }

    // EnemyManager에서 매 프레임 호출합니다
    // 현재는 플레이어 방향으로 직선적으로 추적함
    public void Tick(float deltaTime, Vector2 playerPosition)
    {
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