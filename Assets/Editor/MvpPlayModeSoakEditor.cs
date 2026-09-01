using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MvpPlayModeSoakEditor
{
    private const string RunningKey = "MagicSurvive.MvpSoak.Running";
    private const string RequestedAtKey = "MagicSurvive.MvpSoak.RequestedAt";
    private const string MainScenePath = "Assets/00.Scenes/SampleScene.unity";
    private const double SoakSeconds = 120d;

    private static double startedAt;
    private static bool initialized;
    private static bool finishing;
    private static PlayerProgression progression;
    private static GameplayHudBinder hudBinder;
    private static Health playerHealth;

    static MvpPlayModeSoakEditor()
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            Hook();
        }
    }

    [MenuItem("Tools/Magic Survive/Run 2 Minute Combat Soak")]
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
            throw new InvalidOperationException("MVP soak test is already running.");
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
        if (finishing || !SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            float requestedAt = SessionState.GetFloat(RequestedAtKey, (float)EditorApplication.timeSinceStartup);
            if (EditorApplication.timeSinceStartup - requestedAt > 30d)
            {
                Finish(1, "[MVP Soak] FAIL: Play Mode did not start within 30 seconds.");
            }

            return;
        }

        try
        {
            if (!initialized)
            {
                progression = Require<PlayerProgression>();
                hudBinder = Require<GameplayHudBinder>();
                PlayerController player = Require<PlayerController>();
                playerHealth = player.GetComponent<Health>();
                Assert(playerHealth != null, "Player Health missing.");

                playerHealth.SetMaxHealth(1000000f, true);
                progression.enabled = false;
                startedAt = EditorApplication.timeSinceStartup;
                initialized = true;
                return;
            }

            if (EditorApplication.timeSinceStartup - startedAt < SoakSeconds)
            {
                return;
            }

            EnemyManager enemyManager = Require<EnemyManager>();
            Projectile[] projectiles = UnityEngine.Object.FindObjectsByType<Projectile>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert(playerHealth.IsAlive, "Player died during combat soak.");
            Assert(hudBinder.KillCount >= 20, $"Expected at least 20 kills, got {hudBinder.KillCount}.");
            Assert(enemyManager.ActiveCount > 0, "No active enemies after combat soak.");
            Assert(projectiles.Length > 0, "Projectile pool was not populated.");
            Assert(Mathf.Approximately(Time.timeScale, 1f), "Time scale changed during combat soak.");

            Finish(0, $"[MVP Soak] PASS: {SoakSeconds:0}s, kills={hudBinder.KillCount}, " +
                $"activeEnemies={enemyManager.ActiveCount}, pooledProjectiles={projectiles.Length}.");
        }
        catch (Exception exception)
        {
            Finish(1, $"[MVP Soak] FAIL: {exception}");
        }
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

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (finishing || type == LogType.Log || type == LogType.Warning ||
            stackTrace.Contains("UnityEditor.Search."))
        {
            return;
        }

        Finish(1, $"[MVP Soak] Runtime {type}: {condition}\n{stackTrace}");
    }

    private static void ResetRunState()
    {
        startedAt = 0d;
        initialized = false;
        finishing = false;
        progression = null;
        hudBinder = null;
        playerHealth = null;
    }

    private static void Finish(int exitCode, string message)
    {
        if (finishing)
        {
            return;
        }

        finishing = true;
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
        }
        else
        {
            EditorApplication.ExitPlaymode();
        }
    }
}
