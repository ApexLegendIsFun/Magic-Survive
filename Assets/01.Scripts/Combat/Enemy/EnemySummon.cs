using System.Collections.Generic;
using UnityEngine;

// 주기마다 플레이어 주변에 적을 소환하는 행동
// 소환술사가 쓰고, 보스 2페이즈의 "10초마다 빠른 적 2마리"도 같은 컴포넌트 사용
// 주기 마리수 상한만 다름

[RequireComponent(typeof(Enemy))]
public class EnemySummon : MonoBehaviour
{
    [Header("기획서 §5 소환술사")]
    [SerializeField] private float summonInterval = 8f;
    [SerializeField] private float telegraphSeconds = 0.8f;
    [SerializeField] private int summonCount = 3;
    [SerializeField] private int maxAliveSummons = 6;

    [Header("소환할 적")]
    [SerializeField] private EnemyData summonData;

    [Header("기획에 수치가 없어 정한 임시값")]

    // 기획은 "플레이어 주변"이라고 언급
    [SerializeField] private float summonRadius = 2.5f;

    private Enemy enemy;

    private EnemyManager enemyManager;
    private Transform playerTransform;

    private float summonTimer;

    // 0보다 크면 예고 중이다
    private float telegraphTimer;

    // 예고 시작 때 정해두고 예고가 끝나면 그대로 사용
    // 예고와 실제 소환 위치가 다르면 예고가 거짓이 됨
    private readonly List<Vector2> pendingPositions = new List<Vector2>(4);

    // 살아 있는 소환 적. Killed 에서 제거
    private readonly List<Enemy> aliveSummons = new List<Enemy>(8);

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    // EnemyManager.Spawn 이 풀에서 꺼낸 직후 1회 호출
    public void Configure(EnemyManager manager, Transform player)
    {
        enemyManager = manager;
        playerTransform = player;

        summonTimer = summonInterval;
        telegraphTimer = 0f;
    }

    private void OnDisable()
    {
        // 구독을 남기면 재사용된 적이 죽을 때 이 컴포넌트가 또 호출
        for (int i = 0; i < aliveSummons.Count; i++)
        {
            if (aliveSummons[i] != null)
            {
                aliveSummons[i].Killed -= HandleSummonKilled;
            }
        }

        aliveSummons.Clear();
        pendingPositions.Clear();

        telegraphTimer = 0f;
    }

    private void Update()
    {
        if (enemyManager == null || playerTransform == null || summonData == null)
        {
            return;
        }

        // 빙결 중에는 예고도 주기도 멈춘다. 원거리 공격과 같은 정책
        if (!enemy.IsAlive || enemy.IsFrozen)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        // 주기는 예고 중에도 진행
        // 기획이 "8초마다 소환 위치를 표시한다"이므로 예고 시작 간격이 8초여야 함
        // 예고 중에 멈추면 실제 주기가 8 + 0.8 = 8.8초.
        //
        // summonInterval 이 telegraphSeconds 보다 짧으면 예고가 끝나자마자
        // 다음 예고가 시작됨. 8 대 0.8 이라 지금은 x
        summonTimer -= deltaTime;

        if (telegraphTimer > 0f)
        {
            telegraphTimer -= deltaTime;

            if (telegraphTimer <= 0f)
            {
                SpawnPending();
            }

            return;
        }

        if (summonTimer > 0f)
        {
            return;
        }

        // 남은 시간을 다음 주기로 보존
        summonTimer += summonInterval;

        BeginTelegraph();
    }

    private void BeginTelegraph()
    {
        // Killed 를 못 받은 경우의 안전망
        RemoveDeadSummons();

        int room = maxAliveSummons - aliveSummons.Count;

        // 상한이 찼으면 이번 주기는 건너뛴다. 주기는 이미 소비
        if (room <= 0)
        {
            return;
        }

        int count = Mathf.Min(summonCount, room);

        pendingPositions.Clear();

        Vector2 center = playerTransform.position;

        // 배치가 기획에 없어 균등 분배로 정했다. 랜덤보다 검증이 용이
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;

            pendingPositions.Add(
                center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * summonRadius);
        }

        telegraphTimer = telegraphSeconds;

        // 재사용 버퍼를 그대로 넘기면 구독부가 여러 프레임에 걸쳐 쓸 때 내용이 덮임
        GameEvents.RaiseSummonTelegraph(pendingPositions.ToArray(), telegraphSeconds);
    }

    private void SpawnPending()
    {
        for (int i = 0; i < pendingPositions.Count; i++)
        {
            Enemy spawned = enemyManager.Spawn(summonData, pendingPositions[i]);

            if (spawned == null)
            {
                continue;
            }

            // 기획: 소환된 적은 EXP 를 주지 않음
            spawned.SuppressExperienceReward();

            spawned.Killed += HandleSummonKilled;

            aliveSummons.Add(spawned);
        }

        pendingPositions.Clear();
    }

    private void HandleSummonKilled(Enemy dead)
    {
        dead.Killed -= HandleSummonKilled;

        aliveSummons.Remove(dead);
    }

    private void RemoveDeadSummons()
    {
        for (int i = aliveSummons.Count - 1; i >= 0; i--)
        {
            Enemy summon = aliveSummons[i];

            if (summon == null || !summon.IsAlive)
            {
                if (summon != null)
                {
                    summon.Killed -= HandleSummonKilled;
                }

                aliveSummons.RemoveAt(i);
            }
        }
    }
}