using UnityEngine;

// 주기마다 예고 뒤 정해진 방향으로 돌진하는 이동. 돌진자가 사용
//
// 자기 Update 에서 transform 을 직접 옮기면 냉기 둔화와 빙결 처리를
// Enemy.Tick 과 여기 두 곳에 복제해야 함
[RequireComponent(typeof(Enemy))]
public class EnemyDash : MonoBehaviour
{
    private enum DashState
    {
        Chase,       // 평소 추적
        Telegraph,   // 방향을 정하고 멈춰서 예고
        Dashing      // 고정 방향으로 돌진
    }

    [Header("기획의 돌진자")]
    [SerializeField] private float dashInterval = 8f;
    [SerializeField] private float telegraphSeconds = 1.2f;
    [SerializeField] private float dashSeconds = 0.8f;
    [SerializeField] private float dashSpeedMultiplier = 4f;

    private Enemy enemy;

    private DashState state = DashState.Chase;

    private float dashTimer;
    private float stateTimer;

    // 예고 때 정하고 돌진이 끝날 때까지 바꾸지 않음
    private Vector2 dashDirection;

    // Enemy.Tick 이 이번 프레임에 쓸 속도 배율
    // Tick 을 부른 직후에 읽으므로 이번 프레임의 상태 전이가 이미 반영.
    public float SpeedMultiplier => state == DashState.Dashing ? dashSpeedMultiplier : 1f;

    // [연동:Combat] 겹침 분리가 이 적을 밀면 안 되는 구간
    // 예고를 포함
    public bool IsTrajectoryLocked => state == DashState.Telegraph || state == DashState.Dashing;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        // 주기도 다시 셈
        // 빙결 중에는 Enemy.Tick 이 아래 Tick 을 부르지 않아 타이머가 멈추므로
        // 실제로는 해제 시점부터 8초가 됨
        ResetToChase();

        // 빙결 중에는 Enemy.Tick 이 아래 Tick 을 부르지 않음
        // 그래서 빙결을 Tick 안에서 볼 수 없고 이벤트로 받아야 함
        //
        // 같은 오브젝트의 이벤트를 구독.
        // OnEnable 에서 상태를 리셋하고 OnDisable 에서 해제.
        enemy.FrozenChanged += HandleFrozenChanged;
    }

    private void OnDisable()
    {
        enemy.FrozenChanged -= HandleFrozenChanged;
    }

    private void HandleFrozenChanged(bool frozen)
    {
        if (!frozen)
        {
            return;
        }

        // 빙결되면 진행 중인 예고와 돌진을 버림.
        //
        // 이어서 하면 예고선이 먼저 사라진 뒤에 돌진이 시작.
        // 구독부의 예고선은 받은 예고 시간이 지나면 지워지는데
        // 빙결이 그보다 길면 화면에 아무 표시도 없이 돌진하게 됨.
        //
        // 주기도 다시 세게 됨. 해제 직후 바로 튀어나가도 예고를 놓친 것과 같음
        ResetToChase();
    }

    private void ResetToChase()
    {
        state = DashState.Chase;

        dashTimer = dashInterval;
        stateTimer = 0f;

        dashDirection = Vector2.zero;
    }

    /// <summary>
    /// 이번 프레임에 갈 방향. Vector2.zero 면 멈춤.
    /// Enemy.Tick 이 호출하고 실제 이동은 그쪽에서.
    /// 속도 배율은 이 호출 직후 SpeedMultiplier 로 읽음
    /// </summary>

    public Vector2 Tick(float deltaTime, Vector2 selfPosition, Vector2 playerPosition)
    {
        // 주기는 예고와 돌진 중에도 흐름
        // 소환술사, 보스 충격파와 같은 방침.
        // 뒤에 두면 실제 주기가 8 이 아니라 8 + 1.2 + 0.8 = 10초.
        //
        // dashInterval 이 예고 + 돌진(2.0초)보다 짧으면 돌진이 끝나자마자
        // 다음 예고가 시작. 8 과 2.0 이라 지금은 이상무
        dashTimer -= deltaTime;

        if (state == DashState.Telegraph)
        {
            stateTimer -= deltaTime;

            if (stateTimer <= 0f)
            {
                state = DashState.Dashing;
                stateTimer = dashSeconds;
            }

            // 예고 중에는 멈추기
            //
            // 돌진으로 바뀌는 전환 프레임도 여기서 0 을 돌려줌
            // 아래 종료 전환과 대칭이고, 프레임 경계 오차를 양 끝에서 똑같이 만듦
            return Vector2.zero;
        }

        if (state == DashState.Dashing)
        {
            stateTimer -= deltaTime;

            if (stateTimer <= 0f)
            {
                // 기획: 돌진이 끝나면 플레이어 추적을 다시 시작.
                // 다만 전환 프레임은 움직이지 않음
                state = DashState.Chase;

                return Vector2.zero;
            }

            // 기획: 돌진 중에는 방향을 바꾸지 않음
            return dashDirection;
        }

        if (dashTimer > 0f)
        {
            return ToPlayer(selfPosition, playerPosition);
        }

        // 남은 시간을 살려 주기를 유지
        dashTimer += dashInterval;

        return BeginTelegraph(selfPosition, playerPosition);
    }

    private Vector2 BeginTelegraph(Vector2 selfPosition, Vector2 playerPosition)
    {
        Vector2 toPlayer = playerPosition - selfPosition;

        // 완전히 겹치면 돌진 방향을 정할 수 없음
        // 이번 주기는 건너뛴다. 겹친 상태라 어차피 이동이 0.
        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return Vector2.zero;
        }

        state = DashState.Telegraph;
        stateTimer = telegraphSeconds;

        dashDirection = toPlayer.normalized;

        // [연동:UI] 붉은 방향선. (시작 위치, 방향, 최대 거리, 예고 시간)
        //
        // 예고가 끝나면 이 방향으로 돌진. 시작 위치는 예고 동안 움직이지 않음
        // 거리는 둔화가 없을 때의 값이라 실제 도달 거리는 이보다 짧을 수 있음
        //
        // 사망이나 빙결로 돌진이 취소될 수 있음
        // 별도 취소 이벤트는 열지 않음. 예고 시간이 지나면 선이 지워질 것.
        GameEvents.RaiseDashTelegraph(selfPosition,dashDirection, enemy.MoveSpeed * dashSpeedMultiplier * dashSeconds, telegraphSeconds);

        return Vector2.zero;
    }

    private static Vector2 ToPlayer(Vector2 selfPosition, Vector2 playerPosition)
    {
        Vector2 toPlayer = playerPosition - selfPosition;

        // 겹치면 Enemy.Tick 의 기존 추적과 같이 zero 를 돌려 멈추기.
        return toPlayer.sqrMagnitude < 0.0001f ? Vector2.zero : toPlayer.normalized;
    }
}