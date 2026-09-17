using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Seondong.IntegrationGame
{
    [InitializeOnLoad]
    public static class IntegrationGameEditor
    {
        public const string Root = "Assets/Seondong/IntegrationGame";
        public const string Title = Root + "/Scenes/TitleScene.unity";
        public const string Game = Root + "/Scenes/SampleScene.unity";
        public const string Output = "Builds/SeondongIntegrationFinal/Magic-Survive.exe";
        private const string BackupKey = "Seondong.IntegrationGame.BuildScenesBackup";
        private const string Request = "Logs/SeondongIntegration/editor-request.txt";
        [Serializable] private class Backup { public string[] paths; public bool[] enabled; public string startScene; }

        static IntegrationGameEditor()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.EraseString("Seondong.IntegrationGame.AutoScenario");
                    RestoreBuildScenes();
                }
            };
            EditorApplication.update += PollRequest;
        }

        [MenuItem("Tools/Seondong Integration/1. Create Missing Scenes")]
        public static void CreateScenes()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            try
            {
                if (!File.Exists(Game))
                {
                    if (!AssetDatabase.CopyAsset("Assets/00.Scenes/SampleScene.unity", Game)) throw new Exception("Scene copy failed.");
                    var scene = EditorSceneManager.OpenScene(Game);
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        // Only the personal scene instance changes. Shared prefab assets stay intact.
                        if (root.GetComponentInChildren<PopupUi>(true) != null && PrefabUtility.IsAnyPrefabInstanceRoot(root))
                            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        foreach (var popup in root.GetComponentsInChildren<PopupUi>(true))
                        {
                            if (popup.GetComponentInChildren<HudDynamicUi>(true) != null) throw new Exception("Popup contains HUD; cannot remove safely.");
                            UnityEngine.Object.DestroyImmediate(popup.gameObject);
                        }
                        foreach (var result in root.GetComponentsInChildren<ResultUi>(true)) result.gameObject.SetActive(false);
                        foreach (var scaler in root.GetComponentsInChildren<CanvasScaler>(true))
                        {
                            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                            scaler.referenceResolution = new Vector2(1920, 1080);
                            scaler.matchWidthOrHeight = .5f;
                        }
                    }
                    var flow = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameFlowController>(true)).Single();
                    flow.gameObject.AddComponent<IntegrationRunEvidence>();
                    EditorSceneManager.SaveScene(scene, Game);
                }
                if (!File.Exists(Title))
                {
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
                    camera.tag = "MainCamera";
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(.045f, .065f, .1f);
                    new GameObject("IntegrationTitle", typeof(TitleSceneController), typeof(IntegrationTitle));
                    EditorSceneManager.SaveScene(scene, Title);
                }
                ConfigureEncounters();
                AssetDatabase.SaveAssets();
                Debug.Log("[Seondong Integration] Personal scenes ready.");
            }
            finally { if (setup.Any(s => s.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void ConfigureEncounters()
        {
            Directory.CreateDirectory(Root + "/Data");
            AssetDatabase.Refresh();
            var charger = TemporaryEnemy("TemporaryCharger", "Enemy_Fast", 180, 1.8f, 20, 15);
            var summoner = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/03.Data/Enemy/Enemy_Summoner.asset")
                ?? TemporaryEnemy("TemporarySummoner", "Enemy_Tank", 260, 1f, 12, 25);
            var boss = TemporaryEnemy("TemporaryBoss", "Enemy_Boss", 2000, 1.3f, 20, 0);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(Game);
            // Opening a scene unloads unused assets; reload the persistent objects afterwards.
            charger = AssetDatabase.LoadAssetAtPath<EnemyData>(Root + "/Data/TemporaryCharger.asset");
            summoner = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/03.Data/Enemy/Enemy_Summoner.asset")
                ?? AssetDatabase.LoadAssetAtPath<EnemyData>(Root + "/Data/TemporarySummoner.asset");
            boss = AssetDatabase.LoadAssetAtPath<EnemyData>(Root + "/Data/TemporaryBoss.asset");
            var roots = scene.GetRootGameObjects();
            var run = roots.SelectMany(r => r.GetComponentsInChildren<RunDirector>(true)).Single();
            var manager = roots.SelectMany(r => r.GetComponentsInChildren<EnemyManager>(true)).Single();
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).First(c => c.CompareTag("MainCamera"));
            var elite = run.GetComponent<IntegrationEliteSpawner>();
            if (elite == null) elite = run.gameObject.AddComponent<IntegrationEliteSpawner>();
            var serialized = new SerializedObject(elite);
            serialized.FindProperty("runDirector").objectReferenceValue = run;
            serialized.FindProperty("enemyManager").objectReferenceValue = manager;
            serialized.FindProperty("spawnCamera").objectReferenceValue = camera;
            // Keep later inspector assignments: the placeholders are only the initial defaults.
            if (serialized.FindProperty("charger").objectReferenceValue == null) serialized.FindProperty("charger").objectReferenceValue = charger;
            var configuredSummoner = serialized.FindProperty("summoner");
            if (configuredSummoner.objectReferenceValue == null ||
                AssetDatabase.GetAssetPath(configuredSummoner.objectReferenceValue) == Root + "/Data/TemporarySummoner.asset")
                configuredSummoner.objectReferenceValue = summoner;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EnsureReactionEffectManager(scene);
            var bossSpawner = roots.SelectMany(r => r.GetComponentsInChildren<BossSpawner>(true)).Single();
            var bossSerialized = new SerializedObject(bossSpawner);
            var assigned = bossSerialized.FindProperty("bossData").objectReferenceValue;
            if (assigned == null || AssetDatabase.GetAssetPath(assigned) == "Assets/03.Data/Enemy/Enemy_Boss.asset")
                bossSerialized.FindProperty("bossData").objectReferenceValue = boss;
            bossSerialized.ApplyModifiedPropertiesWithoutUndo();
            var bossHud = run.GetComponent<IntegrationBossHud>();
            if (bossHud == null) bossHud = run.gameObject.AddComponent<IntegrationBossHud>();
            var hudSerialized = new SerializedObject(bossHud);
            hudSerialized.FindProperty("spawner").objectReferenceValue = bossSpawner;
            hudSerialized.FindProperty("hud").objectReferenceValue = roots.SelectMany(r => r.GetComponentsInChildren<HudDynamicUi>(true)).Single();
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();
            var hud = (HudDynamicUi)hudSerialized.FindProperty("hud").objectReferenceValue;
            var existingHud = new SerializedObject(hud);
            hudSerialized.FindProperty("runDirector").objectReferenceValue = run;
            hudSerialized.FindProperty("remainingTime").objectReferenceValue = existingHud.FindProperty("bossTimer").objectReferenceValue;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();
            var bossGroup = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Boss_Timer&Hp").gameObject;
            hudSerialized.FindProperty("bossGroup").objectReferenceValue = bossGroup;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();
            existingHud.FindProperty("bossHpBarGroup").objectReferenceValue = bossGroup;
            existingHud.ApplyModifiedPropertiesWithoutUndo();
            bossGroup.SetActive(false);
            if (bossSerialized.FindProperty("bossData").objectReferenceValue == null ||
                serialized.FindProperty("charger").objectReferenceValue == null || serialized.FindProperty("summoner").objectReferenceValue == null)
                throw new Exception("Encounter data did not survive scene loading.");
            EditorSceneManager.SaveScene(scene, Game);
            var finalRoots = scene.GetRootGameObjects();
            foreach (var root in finalRoots)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) throw new Exception("Missing script: " + t.name);
            if (finalRoots.Any(r => r.GetComponentInChildren<PopupUi>(true) != null)) throw new Exception("Conflicting PopupUi remains.");
        }

        private static void EnsureReactionEffectManager(Scene scene)
        {
            bool exists = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ElementReactionEffectPlayer>(true))
                .Any();
            if (exists) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/02.Prefabs/UI/Effect_Manager/Stact_Sound_Effect_Manager.prefab");
            if (prefab == null) throw new Exception("Missing element reaction effect manager prefab.");
            PrefabUtility.InstantiatePrefab(prefab, scene);
        }

        private static EnemyData TemporaryEnemy(string name, string source, float hp, float speed, float damage, int xp)
        {
            string path = Root + "/Data/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (existing != null) return existing;
            var original = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/03.Data/Enemy/" + source + ".asset");
            if (original == null || original.Prefab == null) throw new Exception("Missing source enemy: " + source);
            var data = UnityEngine.Object.Instantiate(original); data.name = name;
            AssetDatabase.CreateAsset(data, path);
            var serialized = new SerializedObject(data);
            serialized.FindProperty("maxHealth").floatValue = hp;
            serialized.FindProperty("moveSpeed").floatValue = speed;
            serialized.FindProperty("contactDamage").floatValue = damage;
            serialized.FindProperty("experienceReward").intValue = xp;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        [MenuItem("Tools/Seondong Integration/4. Auto Check Play Mode (Natural Death)")]
        public static void AutoPlay()
        {
            SessionState.SetString("Seondong.IntegrationGame.AutoScenario", "death");
            Play();
        }

        [MenuItem("Tools/Seondong Integration/5. Validate Team Handoff")]
        public static void ValidateTeamHandoff()
        {
            Scene scene = EditorSceneManager.OpenScene(Game, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var spawners = roots.SelectMany(root => root.GetComponentsInChildren<IntegrationEliteSpawner>(true)).ToArray();
            Require(spawners.Length == 1, "Expected one integration elite spawner.");
            var spawner = new SerializedObject(spawners[0]);
            var summoner = spawner.FindProperty("summoner").objectReferenceValue as EnemyData;
            Require(summoner != null, "Summoner data missing.");
            Require(AssetDatabase.GetAssetPath(summoner) == "Assets/03.Data/Enemy/Enemy_Summoner.asset",
                "Integration spawner must use Yushin's summoner data.");
            Require(summoner.Prefab != null && Mathf.Approximately(summoner.MaxHealth, 260f) &&
                Mathf.Approximately(summoner.MoveSpeed, 1f) && Mathf.Approximately(summoner.ContactDamage, 12f) &&
                summoner.ExperienceReward == 25, "Summoner data values changed.");
            Require(RunTimelineRules.Reached(360f, RunTimelineRules.SecondEliteTime), "Summoner timeline missing.");
            Require(ElementReactionValues.ReactionUnlockLevel == 1, "Reaction unlock level changed.");
            Require(Mathf.Approximately(ElementReactionValues.KnockbackDistance, .5f), "Earth knockback value changed.");

            var reactionPlayers = roots.SelectMany(root => root.GetComponentsInChildren<ElementReactionEffectPlayer>(true)).ToArray();
            Require(reactionPlayers.Length == 1, "Expected one element reaction effect manager.");
            var effects = new SerializedObject(reactionPlayers[0]).FindProperty("effects");
            bool fireReady = false;
            bool frostReady = false;
            for (int index = 0; index < effects.arraySize; index++)
            {
                var entry = effects.GetArrayElementAtIndex(index);
                var element = (MagicElement)entry.FindPropertyRelative("element").enumValueIndex;
                bool hasPrefab = entry.FindPropertyRelative("effectPrefab").objectReferenceValue != null;
                if (element == MagicElement.Fire) fireReady = hasPrefab;
                if (element == MagicElement.Frost) frostReady = hasPrefab;
            }
            Require(fireReady && frostReady, "Fire and frost reaction effects are not wired.");
            Require(!roots.SelectMany(root => root.GetComponentsInChildren<LightningChainEffectPlayer>(true)).Any(),
                "Incomplete lightning chain effect manager must not be wired.");

            IReadOnlyList<Vector2> receivedTargets = null;
            Action<MagicElement, Vector2, IReadOnlyList<Vector2>> handler =
                (element, origin, targets) => { if (element == MagicElement.Lightning) receivedTargets = targets; };
            GameEvents.ChainReactionTriggered += handler;
            try
            {
                GameEvents.RaiseChainReaction(MagicElement.Lightning, Vector2.zero,
                    new[] { new Vector2(1f, 2f), new Vector2(3f, 4f) });
            }
            finally
            {
                GameEvents.ChainReactionTriggered -= handler;
            }
            Require(receivedTargets != null && receivedTargets.Count == 2, "Chain reaction event did not publish targets.");
            Debug.Log("[Seondong Integration] Team handoff validation passed.");
        }

        [MenuItem("Tools/Seondong Integration/2. Play From Title")]
        public static void Play()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            CreateScenes();
            RequireScenes();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RestoreBuildScenes();
            var old = EditorBuildSettings.scenes;
            SessionState.SetString(BackupKey, JsonUtility.ToJson(new Backup
            {
                paths = old.Select(s => s.path).ToArray(), enabled = old.Select(s => s.enabled).ToArray(),
                startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)
            }));
            EditorBuildSettings.scenes = PersonalScenes();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Title);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Tools/Seondong Integration/3. Build Windows")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            CreateScenes();
            RequireScenes();
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Title, Game }, locationPathName = Output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("[Seondong Integration] Windows build succeeded: " + Path.GetFullPath(Output));
        }

        [MenuItem("Tools/Seondong Integration/Restore Previous Scene List")]
        public static void RestoreBuildScenes()
        {
            string json = SessionState.GetString(BackupKey, "");
            if (string.IsNullOrEmpty(json)) return;
            var backup = JsonUtility.FromJson<Backup>(json);
            EditorBuildSettings.scenes = backup.paths.Select((p, i) => new EditorBuildSettingsScene(p, backup.enabled[i])).ToArray();
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(backup.startScene);
            SessionState.EraseString(BackupKey);
        }

        private static EditorBuildSettingsScene[] PersonalScenes() => new[] { new EditorBuildSettingsScene(Title, true), new EditorBuildSettingsScene(Game, true) };
        private static void RequireScenes()
        {
            if (!File.Exists(Title) || !File.Exists(Game)) throw new Exception("Personal scenes are missing.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        // A local, explicit request lets CLI and the already-open editor share the project safely.
        private static void PollRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            string command = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            try
            {
                if (command == "build") Build();
                else if (command == "graphics-install") IntegrationGraphicsEditor.Install();
                else if (command == "create") CreateScenes();
                else if (command == "play") Play();
                else if (command == "auto-death") AutoPlay();
                else throw new Exception("Unknown integration request: " + command);
                File.WriteAllText("Logs/SeondongIntegration/editor-result.txt", "OK " + command);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                File.WriteAllText("Logs/SeondongIntegration/editor-result.txt", error.ToString());
            }
        }
    }
}
