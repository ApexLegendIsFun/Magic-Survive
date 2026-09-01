using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MvpPlayModeSmokeEditor
{
    private const string RunningKey = "MagicSurvive.MvpSmoke.Running";
    private const string RequestedAtKey = "MagicSurvive.MvpSmoke.RequestedAt";
    private const string MainScenePath = "Assets/00.Scenes/SampleScene.unity";

    private static double startedAt;
    private static double phaseStartedAt;
    private static int phase;
    private static bool finishing;

    private static PlayerProgression progression;
    private static PlayerSkillSystem skills;
    private static LevelUpController levelUp;
    private static GameFlowController flow;
    private static GrayboxGameFlowView view;
    private static GameplayHudBinder hudBinder;
    private static Health health;
    private static PlayerController player;
    private static PlayerInput playerInput;
    private static CameraFollow cameraFollow;
    private static Vector3 playerStartPosition;
    private static Vector3 cameraStartPosition;
    private static int observedKillEvents;
    private static int observedExperienceRewards;

    private static readonly FieldInfo MoveInputField = typeof(PlayerController).GetField(
        "moveInput",
        BindingFlags.Instance | BindingFlags.NonPublic);

    static MvpPlayModeSmokeEditor()
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            Hook();
        }
    }

    [MenuItem("Tools/Magic Survive/Run MVP Play Mode Smoke")]
    public static void RunFromMenu()
    {
        Begin(false);
    }

    public static void RunBatch()
    {
        Begin(true);
    }

    private static void Begin(bool batchMode)
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            throw new InvalidOperationException("MVP smoke test is already running.");
        }

        ResetRunState();
        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool($"{RunningKey}.Batch", batchMode);
        SessionState.SetFloat(RequestedAtKey, (float)EditorApplication.timeSinceStartup);
        EditorSceneManager.OpenScene(MainScenePath);
        Hook();
        EditorApplication.EnterPlaymode();
    }

    private static void Hook()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= HandleLog;
        Application.logMessageReceived += HandleLog;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false) || finishing)
        {
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            float requestedAt = SessionState.GetFloat(RequestedAtKey, (float)EditorApplication.timeSinceStartup);
            if (EditorApplication.timeSinceStartup - requestedAt > 30d)
            {
                Fail("Play Mode did not start within 30 seconds.");
            }

            return;
        }

        if (startedAt <= 0d)
        {
            startedAt = EditorApplication.timeSinceStartup;
            phaseStartedAt = startedAt;
            phase = 0;
        }

        if (EditorApplication.timeSinceStartup - startedAt > 45d)
        {
            Fail("Smoke test timed out.");
            return;
        }

        try
        {
            RunPhase();
        }
        catch (Exception exception)
        {
            Fail(exception.ToString());
        }
    }

    private static void RunPhase()
    {
        switch (phase)
        {
            case 0:
                ResolveRuntimeReferences();
                health.SetMaxHealth(10000f, true);
                Assert(flow.State == GameFlowState.Playing, "Game must start in Playing state.");
                Assert(Mathf.Approximately(Time.timeScale, 1f), "Time scale must start at 1.");
                Assert(skills.CurrentMagic != null, "Starting magic runtime missing.");
                AssertApproximately(skills.CurrentMagic.Damage, 3f, "Starting damage");
                AssertApproximately(skills.CurrentMagic.Cooldown, 0.8f, "Starting cooldown");
                AssertApproximately(FindSlider("Hp_Bar").value, 1f, "Initial HP slider");

                Assert(MoveInputField != null, "Player movement input field missing.");
                Assert(playerInput.actions != null && playerInput.actions.FindAction("Move") != null,
                    "PlayerInput Move action missing.");
                GameEvents.EnemyKilled -= HandleObservedEnemyKilled;
                GameEvents.EnemyKilled += HandleObservedEnemyKilled;
                playerStartPosition = player.transform.position;
                cameraStartPosition = cameraFollow.transform.position;
                playerInput.enabled = false;
                SetMoveInput(Vector2.right);
                Advance(1);
                break;

            case 1:
                if (!Elapsed(0.25d))
                {
                    return;
                }

                SetMoveInput(Vector2.zero);
                playerInput.enabled = true;
                Assert(player.transform.position.x > playerStartPosition.x + 0.05f,
                    "Player did not move from input.");
                Advance(10);
                break;

            case 10:
                if (!Elapsed(0.5d))
                {
                    return;
                }

                Assert(cameraFollow.transform.position.x > cameraStartPosition.x + 0.01f,
                    "Camera did not follow player movement.");

                health.SetMaxHealth(10000f, true);
                health.TakeDamage(2500f);
                AssertApproximately(FindSlider("Hp_Bar").value, 0.75f, "Damaged HP slider");
                health.Heal(2500f);
                Advance(2);
                break;

            case 2:
                if (hudBinder.KillCount <= 0)
                {
                    Assert(!Elapsed(12d), "Automatic attack did not kill an enemy within 12 seconds.");
                    return;
                }

                EnemyManager enemyManager = UnityEngine.Object.FindFirstObjectByType<EnemyManager>();
                Projectile[] pooledProjectiles = UnityEngine.Object.FindObjectsByType<Projectile>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                Assert(enemyManager != null, "Enemy manager missing during Play Mode smoke.");
                Assert(pooledProjectiles.Length > 0,
                    "Automatic attack did not create a pooled projectile.");
                Assert(observedKillEvents > 0, "EnemyKilled event was not raised.");
                Assert(hudBinder.KillCount == observedKillEvents,
                    "HUD kill count must update exactly once per EnemyKilled event.");
                Assert(progression.Level == 1, "Natural combat unexpectedly reached level 2.");
                Assert(progression.CurrentExperience == observedExperienceRewards,
                    "Enemy death rewards must be applied to EXP exactly once.");
                Assert(health.IsAlive, "Player died before progression smoke began.");

                Time.timeScale = 0f;
                progression.ResetProgression();
                progression.AddExperience(20);
                Advance(3);
                break;

            case 3:
                Assert(progression.Level == 3, "20 EXP must raise level 1 to 3.");
                Assert(progression.CurrentExperience == 5, "Overflow EXP must remain 5.");
                Assert(levelUp.PendingLevelUps == 2, "Two level-up choices must be queued.");
                Assert(flow.State == GameFlowState.LevelUp, "Level-up must pause game flow.");
                Assert(Mathf.Approximately(Time.timeScale, 0f), "Level-up must set time scale to 0.");
                Assert(GameObject.Find("GrayboxGameFlowCanvas") != null, "Graybox canvas missing.");
                AssertApproximately(FindSlider("Exp_Bar").value, 1f / 3f, "EXP slider");
                Assert(FindLevelText().text == "3", "Level HUD must display 3.");
                Click("Choice1");
                Advance(4);
                break;

            case 4:
                Assert(levelUp.PendingLevelUps == 1, "One queued level-up must remain.");
                Assert(flow.State == GameFlowState.LevelUp, "Game must remain paused for queued level-up.");
                AssertApproximately(skills.CurrentMagic.Damage, 5f, "Damage upgrade");
                Click("Choice2");
                Advance(5);
                break;

            case 5:
                Assert(levelUp.PendingLevelUps == 0, "Level-up queue must be empty.");
                Assert(flow.State == GameFlowState.Playing, "Game must resume after final queued choice.");
                Assert(Mathf.Approximately(Time.timeScale, 1f), "Time scale must resume to 1.");
                AssertApproximately(skills.CurrentMagic.Cooldown, 0.72f, "Fire-rate upgrade");
                progression.AddExperience(10);
                Advance(6);
                break;

            case 6:
                Assert(progression.Level == 4, "10 overflow EXP must reach level 4.");
                Assert(flow.State == GameFlowState.LevelUp, "Third choice must pause game.");
                Click("Choice3");
                Advance(7);
                break;

            case 7:
                Assert(skills.CurrentMagic.PierceCount == 1, "Pierce upgrade must add one pierce.");
                Assert(flow.State == GameFlowState.Playing, "Game must resume after pierce choice.");
                progression.AddExperience(20);
                health.TakeDamage(health.CurrentHealth);
                Advance(8);
                break;

            case 8:
                Assert(!health.IsAlive, "Fatal damage must kill the player.");
                Assert(!player.enabled, "Player movement must be disabled on death.");
                Assert(flow.State == GameFlowState.GameOver, "PlayerDied must force GameOver.");
                Assert(levelUp.PendingLevelUps == 0, "GameOver must clear pending level-ups.");
                GameEvents.RaiseEnemyKilled(Vector2.zero, progression.RequiredExperience);
                Assert(flow.State == GameFlowState.GameOver && levelUp.PendingLevelUps == 0,
                    "EnemyKilled after death must not override GameOver or queue a level-up.");
                Assert(Mathf.Approximately(Time.timeScale, 0f), "GameOver must pause time.");
                Assert(IsActive("GameOverPanel"), "GameOver graybox panel missing.");
                Click("RestartButton");
                Advance(9);
                break;

            case 9:
                if (!Elapsed(1d))
                {
                    return;
                }

                ResolveRuntimeReferences();
                Assert(flow.State == GameFlowState.Playing, "Restart must return to Playing.");
                Assert(progression.Level == 1 && progression.CurrentExperience == 0,
                    "Restart must reset progression.");
                Assert(Mathf.Approximately(Time.timeScale, 1f), "Restart must restore time scale.");
                AssertApproximately(skills.CurrentMagic.Damage, 3f, "Restarted damage");
                AssertApproximately(skills.CurrentMagic.Cooldown, 0.8f, "Restarted cooldown");
                Assert(skills.CurrentMagic.PierceCount == 0, "Restart must reset pierce.");
                Succeed();
                break;
        }
    }

    private static void ResolveRuntimeReferences()
    {
        progression = Require<PlayerProgression>();
        skills = Require<PlayerSkillSystem>();
        levelUp = Require<LevelUpController>();
        flow = Require<GameFlowController>();
        view = Require<GrayboxGameFlowView>();
        hudBinder = Require<GameplayHudBinder>();

        player = Require<PlayerController>();
        playerInput = player.GetComponent<PlayerInput>();
        cameraFollow = Require<CameraFollow>();
        health = player.GetComponent<Health>();
        Assert(health != null, "Player Health missing.");
        Assert(playerInput != null, "PlayerInput missing.");
        Assert(view != null, "Graybox view missing.");
    }

    private static void SetMoveInput(Vector2 value)
    {
        MoveInputField.SetValue(player, value);
    }

    private static void ResetRunState()
    {
        startedAt = 0d;
        phaseStartedAt = 0d;
        phase = 0;
        finishing = false;
        progression = null;
        skills = null;
        levelUp = null;
        flow = null;
        view = null;
        hudBinder = null;
        health = null;
        player = null;
        playerInput = null;
        cameraFollow = null;
        observedKillEvents = 0;
        observedExperienceRewards = 0;
    }

    private static void HandleObservedEnemyKilled(Vector2 _, int experienceReward)
    {
        observedKillEvents++;
        observedExperienceRewards += experienceReward;
    }

    private static T Require<T>() where T : UnityEngine.Object
    {
        T value = UnityEngine.Object.FindFirstObjectByType<T>();
        if (value == null)
        {
            throw new InvalidOperationException($"Runtime object missing: {typeof(T).Name}");
        }

        return value;
    }

    private static Slider FindSlider(string objectName)
    {
        Slider slider = UnityEngine.Object.FindObjectsByType<Slider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.gameObject.name == objectName);
        if (slider == null)
        {
            throw new InvalidOperationException($"Slider missing: {objectName}");
        }

        return slider;
    }

    private static TextMeshProUGUI FindLevelText()
    {
        GameObject level = GameObject.Find("Level");
        TextMeshProUGUI text = level != null ? level.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (text == null)
        {
            throw new InvalidOperationException("Level text missing.");
        }

        return text;
    }

    private static void Click(string buttonName)
    {
        Button button = UnityEngine.Object.FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.gameObject.name == buttonName);
        if (button == null)
        {
            throw new InvalidOperationException($"Graybox button missing: {buttonName}");
        }

        Assert(EventSystem.current != null, "EventSystem missing.");
        ExecuteEvents.Execute(
            button.gameObject,
            new BaseEventData(EventSystem.current),
            ExecuteEvents.submitHandler);
    }

    private static bool IsActive(string objectName)
    {
        Transform transform = Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(candidate =>
                candidate.gameObject.scene.IsValid() && candidate.gameObject.name == objectName);
        return transform != null && transform.gameObject.activeInHierarchy;
    }

    private static bool Elapsed(double seconds)
    {
        return EditorApplication.timeSinceStartup - phaseStartedAt >= seconds;
    }

    private static void Advance(int nextPhase)
    {
        phase = nextPhase;
        phaseStartedAt = EditorApplication.timeSinceStartup;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertApproximately(float actual, float expected, string label)
    {
        if (!Mathf.Approximately(actual, expected))
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}.");
        }
    }

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (finishing || type == LogType.Log || type == LogType.Warning)
        {
            return;
        }

        // Unity 6000.3 may report a batch-mode Quick Search indexing exception while
        // entering Play Mode. It is editor infrastructure noise, not a game runtime error.
        if (stackTrace.Contains("UnityEditor.Search."))
        {
            return;
        }

        Fail($"Runtime {type}: {condition}\n{stackTrace}");
    }

    private static void Succeed()
    {
        Finish(0, "[MVP Smoke] PASS: combat, pooling, progression, upgrades, game-over, restart.");
    }

    private static void Fail(string message)
    {
        Finish(1, $"[MVP Smoke] FAIL: {message}");
    }

    private static void Finish(int exitCode, string message)
    {
        if (finishing)
        {
            return;
        }

        finishing = true;
        GameEvents.EnemyKilled -= HandleObservedEnemyKilled;
        SessionState.SetBool(RunningKey, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= HandleLog;

        if (exitCode == 0)
        {
            Debug.Log(message);
        }
        else
        {
            Debug.LogError(message);
        }

        if (SessionState.GetBool($"{RunningKey}.Batch", false))
        {
            EditorApplication.Exit(exitCode);
            return;
        }

        EditorApplication.ExitPlaymode();
    }
}
