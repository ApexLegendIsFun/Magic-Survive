using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AssetPresentationBenchmark
{
    private const string Key = "MagicSurvive.AssetBenchmark";
    private static double start;
    private static double last;
    private static int frame = -1;
    private static readonly List<double> samples = new List<double>();
    private static RenderTexture target;
    private static bool ready;

    static AssetPresentationBenchmark()
    {
        if (SessionState.GetBool(Key, false)) Hook();
    }
    public static void Before() { Begin("before"); }
    public static void After() { Begin("after"); }
    private static void Begin(string label)
    {
        SessionState.SetBool(Key, true);
        SessionState.SetString(Key + ".Label", label);
        SessionState.SetFloat(Key + ".Requested", (float)EditorApplication.timeSinceStartup);
        EditorSceneManager.OpenScene("Assets/00.Scenes/SampleScene.unity");
        Hook();
        EditorApplication.EnterPlaymode();
    }
    private static void Hook()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }
    private static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception) return;
        if (stack.Contains("UnityEditor.Search.")) return;
        FinishError(message + "\n" + stack);
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) return;
        try
        {
            if (!EditorApplication.isPlaying)
            {
                if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Requested", 0f) > 90)
                    FinishError("Play Mode timeout");
                return;
            }
            if (!ready)
            {
                var flow = UnityEngine.Object.FindFirstObjectByType<LevelUpController>();
                if (flow == null || !flow.TryChooseStartingElement(MagicElement.Fire)) return;
                UnityEngine.Object.FindFirstObjectByType<PlayerProgression>().SetExperienceEnabled(false);
                UnityEngine.Object.FindFirstObjectByType<SpawnDirector>().enabled = false;
                var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                player.GetComponent<Health>().SetMaxHealth(10000000, true);
                var manager = UnityEngine.Object.FindFirstObjectByType<EnemyManager>();
                manager.DespawnAll();
                var data = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyData>(
                    "Assets/03.Data/Enemy/Enemy_Basic.asset"));
                var serialized = new SerializedObject(data);
                serialized.FindProperty("maxHealth").floatValue = 1000000;
                serialized.FindProperty("moveSpeed").floatValue = 0.05f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                for (int i = 0; i < 100; i++)
                {
                    float angle = i * 2.39996323f;
                    float radius = 2f + Mathf.Sqrt(i / 99f) * 4f;
                    manager.Spawn(data, (Vector2)player.transform.position +
                        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                target = new RenderTexture(1920, 1080, 24);
                target.Create();
                Camera.main.targetTexture = target;
                start = last = EditorApplication.timeSinceStartup;
                ready = true;
                return;
            }
            if (Time.frameCount == frame) return;
            frame = Time.frameCount;
            double now = EditorApplication.timeSinceStartup;
            if (now - start > 5) samples.Add((now - last) * 1000);
            last = now;
            if (now - start < 25) return;
            var enemies = UnityEngine.Object.FindFirstObjectByType<EnemyManager>();
            if (enemies.ActiveCount != 100) throw new Exception("Benchmark must keep 100 enemies.");
            string label = SessionState.GetString(Key + ".Label", "unknown");
            samples.Sort();
            double mean = samples.Average();
            string report = $"label={label}\nUnity={Application.unityVersion}\n" +
                $"GPU={SystemInfo.graphicsDeviceName}\nAPI={SystemInfo.graphicsDeviceType}\n" +
                $"resolution=1920x1080 RenderTexture; Editor Play Mode; 100 enemies + automatic fire\n" +
                $"frames={samples.Count}\nmeanMs={mean:F3}\np95Ms={samples[(int)(samples.Count * 0.95)]:F3}\n" +
                $"meanFPS={1000 / mean:F2}\n";
            Directory.CreateDirectory("Logs");
            File.WriteAllText($"Logs/AssetBenchmark-{label}.txt", report);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            File.WriteAllBytes($"Logs/AssetBenchmark-{label}.png", image.EncodeToPNG());
            RenderTexture.active = previous;
            Debug.Log("[Asset Benchmark] PASS\n" + report);
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(0);
        }
        catch (Exception error) { FinishError(error.ToString()); }
    }
    private static void FinishError(string error)
    {
        SessionState.SetBool(Key, false);
        Application.logMessageReceived -= OnLog;
        Debug.LogError("[Asset Benchmark] FAIL " + error);
        EditorApplication.Exit(1);
    }
}

