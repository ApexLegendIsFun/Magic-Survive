using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AssetPresentationValidation
{
    private const string Key = "MagicSurvive.AssetVisualTest";
    private static int stage;
    private static int elementIndex;
    private static double nextAt;
    private static RenderTexture target;
    private static PlayerSkillSystem skills;
    private static LevelUpController levels;
    private static GameFlowController flow;
    private static EnemyManager enemies;
    private static ProjectileLauncher launcher;
    private static Enemy targetEnemy;
    private static EnemyData data;
    private static Projectile projectile;
    private static ProjectileMagicDefinition definition;
    static AssetPresentationValidation() { if (SessionState.GetBool(Key, false)) Hook(); }

    public static void FinalChecks()
    {
        TenMinutePlanValidationEditor.Run();
        TitlePlayModeSmokeEditor.RunBatch();
    }

    public static void RunBatch()
    {
        foreach (var path in Directory.GetFiles(AssetPresentationInstaller.Output, "*.prefab"))
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
            Require(root != null, "Prefab not imported: " + path);
            foreach (var component in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(component.gameObject) == 0,
                    "Missing Script: " + path);
        }
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/04.ThirdParty/GeneratedPresentation/DNPPixel SDF.asset");
        Require(font != null && font.material != null && font.atlasTexture != null, "Pixel font atlas missing.");
        SessionState.SetBool(Key, true);
        SessionState.SetFloat(Key + ".Start", (float)EditorApplication.timeSinceStartup);
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
        if (stack.Contains("UnityEditor.Search.")) return;
        if (type == LogType.Error || type == LogType.Exception ||
            type == LogType.Warning && (message.Contains("missing") || message.Contains("AnimationEvent"))) Finish(1, message + "\n" + stack);
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) return;
        try
        {
            double now = EditorApplication.timeSinceStartup;
            Require(now - SessionState.GetFloat(Key + ".Start", 0f) < 120, "Visual test timeout.");
            if (!EditorApplication.isPlaying || now < nextAt) return;
            var element = MagicContentCatalog.PentagonElements[elementIndex];
            switch (stage)
            {
                case 0:
                    levels = UnityEngine.Object.FindFirstObjectByType<LevelUpController>();
                    if (levels == null) return;
                    skills = UnityEngine.Object.FindFirstObjectByType<PlayerSkillSystem>();
                    flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
                    Require(flow.State == GameFlowState.ElementSelect, "Start selection missing.");
                    target = new RenderTexture(1920, 1080, 24);
                    target.Create();
                    Camera.main.targetTexture = target;
                    RouteCanvases();
                    Advance(1, 0.3);
                    break;
                case 1:
                    if (elementIndex == 0) Capture("01-element-selection");
                    Require(levels.TryChooseStartingElement(element), "Could not start " + element);
                    UnityEngine.Object.FindFirstObjectByType<WeaponRunner>().enabled = false;
                    UnityEngine.Object.FindFirstObjectByType<SpawnDirector>().enabled = false;
                    UnityEngine.Object.FindFirstObjectByType<PlayerProgression>().SetExperienceEnabled(false);
                    UnityEngine.Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>().SetMaxHealth(1000000, true);
                    enemies = UnityEngine.Object.FindFirstObjectByType<EnemyManager>();
                    launcher = UnityEngine.Object.FindFirstObjectByType<ProjectileLauncher>();
                    enemies.DespawnAll();
                    data = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/03.Data/Enemy/Enemy_Basic.asset"));
                    var serialized = new SerializedObject(data);
                    serialized.FindProperty("maxHealth").floatValue = 100000;
                    serialized.FindProperty("moveSpeed").floatValue = 0;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    targetEnemy = enemies.Spawn(data, new Vector2(5, 0));
                    definition = AssetDatabase.LoadAssetAtPath<ProjectileMagicDefinition>(
                        $"Assets/03.Data/Magic/{MagicContentCatalog.GetMagicId(element)}.asset");
                    Require(definition.ProjectilePrefab.GetComponent<AssetVisualLifecycle>() != null, "Variant missing " + element);
                    Require(skills.CurrentMagic.Execute(new AttackContext(Vector2.zero, targetEnemy, launcher)), "Fire failed.");
                    projectile = ActiveProjectile();
                    Require(projectile != null, "Projectile not spawned.");
                    projectile.Tick(0.12f, enemies);
                    foreach (var animator in projectile.GetComponentsInChildren<Animator>()) animator.Update(0.1f);
                    Time.timeScale = 0f;
                    Advance(2, 0.12);
                    break;
                case 2:
                    Capture("02-combat-" + element);
                    projectile.Tick(100f, enemies);
                    Advance(3, 0.1);
                    break;
                case 3:
                    Require(!projectile.gameObject.activeSelf, "Projectile not returned to pool.");
                    Require(skills.CurrentMagic.Execute(new AttackContext(Vector2.zero, targetEnemy, launcher)), "Refire failed.");
                    Require(ActiveProjectile() == projectile, "Projectile pool not reused.");
                    var visual = projectile.GetComponent<AssetVisualLifecycle>();
                    Require(visual.visual.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled), "VFX invisible.");
                    projectile.Tick(100f, enemies);
                    var enemyVisual = targetEnemy.GetComponent<AssetVisualLifecycle>();
                    Vector3 scale = enemyVisual.visual.localScale;
                    enemyVisual.visual.localScale = scale * 2;
                    enemies.DespawnAll();
                    Require(enemies.Spawn(data, new Vector2(5, 0)) == targetEnemy, "Enemy pool not reused.");
                    Require(enemyVisual.visual.localScale == scale, "Visual scale leaked across pool reuse.");
                    var progression = UnityEngine.Object.FindFirstObjectByType<PlayerProgression>();
                    progression.SetExperienceEnabled(true);
                    progression.AddExperience(progression.RequiredExperience);
                    Require(flow.State == GameFlowState.LevelUp, "Level-up did not open.");
                    var popup = UnityEngine.Object.FindFirstObjectByType<PopupUi>();
                    var slots = new SerializedObject(popup).FindProperty("elementSlots");
                    var choice = skills.Choices[0];
                    for (int i = 0; i < slots.arraySize; i++)
                    {
                        var slot = slots.GetArrayElementAtIndex(i);
                        if ((MagicElement)slot.FindPropertyRelative("element").intValue != choice) continue;
                        ((UnityEngine.UI.Button)slot.FindPropertyRelative("button").objectReferenceValue).onClick.Invoke();
                    }
                    Require(skills.Tree.PendingSelection.HasValue, "Skill selection failed.");
                    RouteCanvases();
                    Advance(4, 0.2);
                    break;
                case 4:
                    if (elementIndex == 0) Capture("03-level-up");
                    Require(levels.ConfirmSelectedSkill(), "Confirm failed.");
                    Require(UnityEngine.Object.FindObjectsByType<SoundManager>(FindObjectsSortMode.None).Length == 1,
                        "Sound manager count=" + UnityEngine.Object.FindObjectsByType<SoundManager>(FindObjectsSortMode.None).Length +
                        "; feedback=" + UnityEngine.Object.FindFirstObjectByType<AssetPresentationFeedback>() +
                        "; singleton=" + SoundManager.instance);
                    if (++elementIndex < 5)
                    {
                        Camera.main.targetTexture = null;
                        target.Release();
                        UnityEngine.Object.DestroyImmediate(target);
                        SceneManager.LoadScene("SampleScene");
                        Advance(0, 0.4);
                    }
                    else
                    {
                        elementIndex = 4;
                        var run = UnityEngine.Object.FindFirstObjectByType<RunDirector>();
                        var runSerialized = new SerializedObject(run);
                        runSerialized.FindProperty("bossTime").floatValue = 0.1f;
                        runSerialized.ApplyModifiedPropertiesWithoutUndo();
                        Advance(5, 0.3);
                    }
                    break;
                case 5:
                    Require(flow.State == GameFlowState.Boss, "Boss phase missing.");
                    var bossData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/03.Data/Enemy/Enemy_Boss.asset");
                    targetEnemy = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)
                        .FirstOrDefault(e => e.SourcePrefab == bossData.Prefab);
                    Require(targetEnemy != null && targetEnemy.GetComponent<AssetVisualLifecycle>() != null, "Boss visual missing.");
                    targetEnemy.transform.position = new Vector3(2, 1, 0);
                    Advance(6, 0.3);
                    break;
                case 6:
                    Capture("04-boss");
                    targetEnemy.GetComponent<Health>().TakeDamage(1000000);
                    Require(flow.State == GameFlowState.Victory, "Boss defeat did not produce victory.");
                    RouteCanvases();
                    Advance(7, 0.3);
                    break;
                case 7:
                    Capture("05-victory");
                    Finish(0, "five starts, VFX/pool reuse, level-up, audio singleton, boss and victory; screenshots saved.");
                    break;
            }
        }
        catch (Exception error) { Finish(1, error.ToString()); }
    }
    private static Projectile ActiveProjectile() => UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)
        .FirstOrDefault(p => p.SourcePrefab == definition.ProjectilePrefab && p.IsActive);
    private static void RouteCanvases()
    {
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
            if (canvas.sortingOrder < 10000) canvas.sortingOrder += 20000;
        }
    }
    private static void Capture(string name)
    {
        var old = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        Directory.CreateDirectory("Logs/AssetPresentation");
        File.WriteAllBytes("Logs/AssetPresentation/" + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
        RenderTexture.active = old;
    }
    private static void Advance(int value, double delay) { stage = value; nextAt = EditorApplication.timeSinceStartup + delay; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Finish(int code, string message)
    {
        SessionState.SetBool(Key, false);
        Application.logMessageReceived -= OnLog;
        if (code == 0) Debug.Log("[Asset Visual Test] PASS " + message);
        else Debug.LogError("[Asset Visual Test] FAIL " + message);
        EditorApplication.Exit(code);
    }
}
