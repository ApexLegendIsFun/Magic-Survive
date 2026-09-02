using System;
using UnityEngine;

public enum EliteKind
{
    Charger = 0,
    Summoner = 1
}

[DisallowMultipleComponent]
public sealed class RunDirector : MonoBehaviour
{
    public const float DefaultBossTime = RunTimelineRules.BossTime;
    public const float DefaultTimeLimit = RunTimelineRules.TimeLimit;

    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerSkillSystem playerSkillSystem;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField, Min(1f)] private float bossTime = DefaultBossTime;
    [SerializeField, Min(1f)] private float timeLimit = DefaultTimeLimit;

    private bool firstEliteRequested;
    private bool secondEliteRequested;
    private bool bossRequested;
    private bool resultPublished;
    private RunOutcome pendingOutcome;

    public float ElapsedCombatTime { get; private set; }
    public float RemainingTime => Mathf.Max(0f, timeLimit - ElapsedCombatTime);
    public float GrowthProgress => Mathf.Clamp01(ElapsedCombatTime / bossTime);
    public int KillCount { get; private set; }
    public RunResult Result { get; private set; }

    public event Action<float, float> TimeChanged;
    public event Action<int> KillCountChanged;
    public event Action<EliteKind> EliteSpawnRequested;
    public event Action BossSpawnRequested;
    public event Action<RunResult> ResultReady;

    private void Awake()
    {
        bossTime = Mathf.Max(1f, bossTime);
        timeLimit = Mathf.Max(bossTime, timeLimit);
    }

    private void OnEnable()
    {
        GameEvents.EnemyKilled += HandleEnemyKilled;

        if (gameFlowController != null)
        {
            gameFlowController.StateChanged += HandleStateChanged;
        }
    }

    private void Start()
    {
        TimeChanged?.Invoke(ElapsedCombatTime, RemainingTime);
        KillCountChanged?.Invoke(KillCount);
    }

    private void OnDisable()
    {
        GameEvents.EnemyKilled -= HandleEnemyKilled;

        if (gameFlowController != null)
        {
            gameFlowController.StateChanged -= HandleStateChanged;
        }
    }

    private void Update()
    {
        if (gameFlowController == null ||
            (gameFlowController.State != GameFlowState.Playing &&
             gameFlowController.State != GameFlowState.Boss))
        {
            return;
        }

        ElapsedCombatTime = Mathf.Min(timeLimit, ElapsedCombatTime + Time.deltaTime);
        TimeChanged?.Invoke(ElapsedCombatTime, RemainingTime);

        if (gameFlowController.State == GameFlowState.Playing)
        {
            RequestScheduledEncounters();

            if (!bossRequested && ElapsedCombatTime >= bossTime)
            {
                BeginBossPhase();
            }
        }

        if (gameFlowController.State == GameFlowState.Boss && ElapsedCombatTime >= timeLimit)
        {
            pendingOutcome = RunOutcome.Timeout;
            gameFlowController.EnterGameOver();
        }
    }

    public bool ReportBossDefeated()
    {
        if (gameFlowController == null || gameFlowController.State != GameFlowState.Boss)
        {
            return false;
        }

        pendingOutcome = RunOutcome.Victory;
        return gameFlowController.TryEnterVictory();
    }

    private void RequestScheduledEncounters()
    {
        if (!firstEliteRequested &&
            RunTimelineRules.Reached(ElapsedCombatTime, RunTimelineRules.FirstEliteTime))
        {
            firstEliteRequested = true;
            EliteSpawnRequested?.Invoke(EliteKind.Charger);
        }

        if (!secondEliteRequested &&
            RunTimelineRules.Reached(ElapsedCombatTime, RunTimelineRules.SecondEliteTime))
        {
            secondEliteRequested = true;
            EliteSpawnRequested?.Invoke(EliteKind.Summoner);
        }
    }

    private void BeginBossPhase()
    {
        if (gameFlowController == null || !gameFlowController.TryEnterBoss())
        {
            return;
        }

        bossRequested = true;
        playerProgression?.SetExperienceEnabled(false);
        enemyManager?.DespawnAll();
        BossSpawnRequested?.Invoke();
    }

    private void HandleEnemyKilled(Vector2 _, int __)
    {
        if (resultPublished)
        {
            return;
        }

        KillCount++;
        KillCountChanged?.Invoke(KillCount);
    }

    private void HandleStateChanged(GameFlowState state)
    {
        if (resultPublished || (state != GameFlowState.Victory && state != GameFlowState.GameOver))
        {
            return;
        }

        RunOutcome outcome = pendingOutcome;
        if (outcome == RunOutcome.None)
        {
            outcome = state == GameFlowState.Victory
                ? RunOutcome.Victory
                : RunOutcome.Defeat;
        }

        playerProgression?.SetExperienceEnabled(false);

        Result = new RunResult(
            outcome,
            ElapsedCombatTime,
            KillCount,
            playerProgression != null ? playerProgression.Level : 1,
            playerSkillSystem != null ? playerSkillSystem.GetOwnedElements() : null,
            playerSkillSystem != null ? playerSkillSystem.GetOwnedFusions() : null);

        resultPublished = true;
        ResultReady?.Invoke(Result);
    }
}
