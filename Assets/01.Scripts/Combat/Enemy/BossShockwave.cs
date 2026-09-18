using UnityEngine;

// 보스 2페이즈의 원형 충격파
// 예고를 띄우고 그 자리에서 터져 반경 안의 플레이어에게 데미지
//
// BossPhaseController 가 2페이즈 진입 때 enabled = true 로 발동
// EnemySummon 과 완전히 같은 방식
[RequireComponent(typeof(Enemy))]
public class BossShockwave : MonoBehaviour
{
    [Header("기획의 보스 2페이즈")]
    [SerializeField] private float shockwaveInterval = 5f;
    [SerializeField] private float telegraphSeconds = 0.8f;
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float damage = 18f;

    private Enemy enemy;

    // EnemyManager.Spawn 이 주입.
    // 때릴 대상이 플레이어 한 명뿐이라 Transform 은 별도 받지 않고 이것 사용
    private Health playerHealth;

    private float shockwaveTimer;

    // 0보다 크면 예고 중
    private float telegraphTimer;

    // 예고 시점의 보스 위치. 발동까지 고정
    //
    private Vector2 telegraphCenter;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    // EnemyManager.Spawn 이 풀에서 꺼낸 직후 1회 호출
    // 프리팹이라 씬의 플레이어를 [SerializeField] 로 받을 수 없음
    //
    // 2페이즈 전까지 이 컴포넌트는 꺼져 있지만 GetComponent 는 꺼진 것도 찾으므로
    // 주입은 스폰 시점에 종료
    public void Configure(Health player)
    {
        playerHealth = player;

        shockwaveTimer = shockwaveInterval;
        telegraphTimer = 0f;
    }

    private void OnDisable()
    {
        // 예고 중에 죽거나 풀로 돌아가면 예고를 버림
        // 남겨 두면 다음 보스가 스폰되자마자 남은 예고가 터짐
        telegraphTimer = 0f;
    }

    private void Update()
    {
        if (playerHealth == null)
        {
            return;
        }

        // 빙결 중에는 멈춤. 원거리 공격, 소환과 같은 방침
        // 보스는 빙결 면역이라 지금은 걸리지 않지만 방침을 컴포넌트마다 다르게 두지 않음
        if (!enemy.IsAlive || enemy.IsFrozen)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        // 주기는 예고 중에도 줄임
        shockwaveTimer -= deltaTime;

        if (telegraphTimer > 0f)
        {
            telegraphTimer -= deltaTime;

            if (telegraphTimer <= 0f)
            {
                Detonate();
            }

            return;
        }

        if (shockwaveTimer > 0f)
        {
            return;
        }

        // 남은 시간을 살려 주기를 유지
        shockwaveTimer += shockwaveInterval;

        BeginTelegraph();
    }

    private void BeginTelegraph()
    {
        telegraphCenter = transform.position;

        telegraphTimer = telegraphSeconds;

        // [연동:UI] 예고 원. 이 중심과 반경에서 telegraphSeconds 뒤에 터짐
        //
        // 예고 중에 보스가 죽으면 Triggered 가 오지 않음
        // 구독부는 받은 예고 시간이 지나면 스스로 연출을 지울 것
        GameEvents.RaiseBossShockwaveTelegraph(telegraphCenter, radius, telegraphSeconds);
    }

    private void Detonate()
    {
        // 피해보다 먼저 알림
        // 플레이어가 이 피해로 죽으면 게임 오버 전환이 먼저 일어날 수 있음
        // 빗나가도 폭발 연출은 나와야 하므로 판정 밖에 둠
        GameEvents.RaiseBossShockwaveTriggered(telegraphCenter, radius);

        if (!playerHealth.IsAlive)
        {
            return;
        }

        Vector2 toPlayer = (Vector2)playerHealth.transform.position - telegraphCenter;

        // 기획의 반경 2.5. 중심점 거리 2.5 "이하"가 적중.
        // 아래 조건에서 빠져나가지 않으므로 거리 == 2.5 는 맞는다
        // float 경계라 정확히 2.5 에 서는 경우의 결과는 위치에 따라 흔들림
        // 피해 단위가 크고 플레이 중 그 거리에 정확히 서는 일이 없어 그대로.
        if (toPlayer.sqrMagnitude > radius * radius)
        {
            return;
        }

        // 적 투사체와 같이 PlayerContactDamage 의 무적 시간을 거치지 않음
        // 접촉 20 + 원거리 8 + 충격파 18 이 같은 프레임에 겹칠 수 있음. 확인 요청 대상
        playerHealth.TakeDamage(damage);
    }
}