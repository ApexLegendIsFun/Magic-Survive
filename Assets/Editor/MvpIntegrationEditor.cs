using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MvpIntegrationEditor
{
    private const string MainScenePath = "Assets/00.Scenes/SampleScene.unity";
    private const string UiSourceScenePath = "Assets/01.Scripts/UI/UI_SampleScene.unity";
    private const string GameplayUiPrefabPath = "Assets/02.Prefabs/UI/GameplayUI.prefab";
    private const string ProjectilePrefabPath = "Assets/02.Prefabs/Projectile/Projectile.prefab";
    private const string StartingMagicPath = "Assets/03.Data/Magic/FireBolt.asset";

    [MenuItem("Tools/Magic Survive/Build MVP Integration Scene")]
    public static void Run()
    {
        CreateFolderRecursive("Assets/02.Prefabs/UI");
        CreateFolderRecursive("Assets/03.Data/Magic");

        ProjectileMagicDefinition startingMagic = CreateOrUpdateStartingMagic();
        Scene mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        DestroyRootIfPresent(mainScene, "GameplayUI");
        DestroyRootIfPresent(mainScene, "GameSystems");

        GameObject gameplayUi = ImportGameplayUi(mainScene);
        GameObject gameSystems = CreateGameSystems(mainScene, gameplayUi, startingMagic);

        EditorSceneManager.MarkSceneDirty(mainScene);
        EditorSceneManager.SaveScene(mainScene, MainScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateScene(mainScene, gameplayUi, gameSystems);
        Debug.Log("[MVP Integration] SampleScene integration complete.");
    }

    [MenuItem("Tools/Magic Survive/Validate MVP Integration Scene")]
    public static void Validate()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        GameObject gameplayUi = FindRoot(scene, "GameplayUI");
        GameObject gameSystems = FindRoot(scene, "GameSystems");
        ValidateScene(scene, gameplayUi, gameSystems);
        Debug.Log("[MVP Integration] Validation passed.");
    }

    private static ProjectileMagicDefinition CreateOrUpdateStartingMagic()
    {
        GameObject projectileObject = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        Projectile projectile = projectileObject != null ? projectileObject.GetComponent<Projectile>() : null;
        if (projectile == null)
        {
            throw new InvalidOperationException($"Projectile prefab missing: {ProjectilePrefabPath}");
        }

        ProjectileMagicDefinition definition =
            AssetDatabase.LoadAssetAtPath<ProjectileMagicDefinition>(StartingMagicPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ProjectileMagicDefinition>();
            definition.name = "FireBolt";
            AssetDatabase.CreateAsset(definition, StartingMagicPath);
        }

        SerializedObject serialized = new SerializedObject(definition);
        serialized.FindProperty("element").enumValueIndex = (int)MagicElement.Fire;
        serialized.FindProperty("projectilePrefab").objectReferenceValue = projectile;
        serialized.FindProperty("cooldown").floatValue = 0.8f;
        serialized.FindProperty("range").floatValue = 8f;
        serialized.FindProperty("damage").floatValue = 3f;
        serialized.FindProperty("speed").floatValue = 12f;
        serialized.FindProperty("maxDistance").floatValue = 10f;
        serialized.FindProperty("hitRadius").floatValue = 0.25f;
        serialized.FindProperty("pierceCount").intValue = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static GameObject ImportGameplayUi(Scene mainScene)
    {
        Scene uiScene = EditorSceneManager.OpenScene(UiSourceScenePath, OpenSceneMode.Additive);

        try
        {
            GameObject sourceCanvas = FindRoot(uiScene, "Canvas_InGame");
            GameObject sourceManager = FindRoot(uiScene, "UiManager");
            GameObject sourceEventSystem = FindRoot(uiScene, "EventSystem");

            if (sourceCanvas == null || sourceManager == null || sourceEventSystem == null)
            {
                throw new InvalidOperationException("Seungbum UI roots are incomplete.");
            }

            SceneManager.SetActiveScene(mainScene);

            GameObject gameplayUi = new GameObject("GameplayUI");
            SceneManager.MoveGameObjectToScene(gameplayUi, mainScene);

            GameObject canvas = CloneIntoScene(sourceCanvas, gameplayUi.transform, mainScene);
            GameObject manager = CloneIntoScene(sourceManager, gameplayUi.transform, mainScene);
            CloneIntoScene(sourceEventSystem, gameplayUi.transform, mainScene);

            FixZeroCanvasScales(canvas);
            ConfigureHud(canvas, manager);

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                gameplayUi,
                GameplayUiPrefabPath,
                InteractionMode.AutomatedAction);

            return gameplayUi;
        }
        finally
        {
            if (uiScene.IsValid() && uiScene.isLoaded)
            {
                EditorSceneManager.CloseScene(uiScene, true);
            }
        }
    }

    private static GameObject CreateGameSystems(
        Scene mainScene,
        GameObject gameplayUi,
        ProjectileMagicDefinition startingMagic)
    {
        GameObject player = mainScene.GetRootGameObjects().FirstOrDefault(root => root.name == "Player");
        if (player == null)
        {
            throw new InvalidOperationException("Player root missing from SampleScene.");
        }

        WeaponRunner weaponRunner = player.GetComponent<WeaponRunner>();
        Health health = player.GetComponent<Health>();
        if (weaponRunner == null || health == null)
        {
            throw new InvalidOperationException("Player combat components are incomplete.");
        }

        SerializedObject weaponSerialized = new SerializedObject(weaponRunner);
        SerializedProperty startingAttacks = weaponSerialized.FindProperty("startingAttacks");
        startingAttacks.arraySize = 0;
        weaponSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject systems = new GameObject("GameSystems");
        SceneManager.MoveGameObjectToScene(systems, mainScene);

        PlayerProgression progression = systems.AddComponent<PlayerProgression>();
        GameFlowController flow = systems.AddComponent<GameFlowController>();
        PlayerSkillSystem skills = systems.AddComponent<PlayerSkillSystem>();
        GrayboxGameFlowView graybox = systems.AddComponent<GrayboxGameFlowView>();
        LevelUpController levelUp = systems.AddComponent<LevelUpController>();
        GameplayHudBinder hudBinder = systems.AddComponent<GameplayHudBinder>();

        SetObjectReference(skills, "weaponRunner", weaponRunner);
        SetObjectReference(skills, "startingMagic", startingMagic);

        SetObjectReference(levelUp, "playerProgression", progression);
        SetObjectReference(levelUp, "playerSkillSystem", skills);
        SetObjectReference(levelUp, "gameFlowController", flow);
        SetObjectReference(levelUp, "view", graybox);

        HudDynamicUi hud = gameplayUi.GetComponentInChildren<HudDynamicUi>(true);
        if (hud == null)
        {
            throw new InvalidOperationException("HudDynamicUi missing from GameplayUI prefab.");
        }

        SetObjectReference(hudBinder, "hud", hud);
        SetObjectReference(hudBinder, "playerHealth", health);
        SetObjectReference(hudBinder, "progression", progression);

        return systems;
    }

    private static void ConfigureHud(GameObject canvasRoot, GameObject managerRoot)
    {
        HudDynamicUi hud = canvasRoot.GetComponentInChildren<HudDynamicUi>(true);
        if (hud == null)
        {
            throw new InvalidOperationException("HudDynamicUi missing in Seungbum UI hierarchy.");
        }

        Slider healthBar = FindNamedComponent<Slider>(canvasRoot, "Hp_Bar");
        if (healthBar == null)
        {
            throw new InvalidOperationException("Hp_Bar slider missing in Seungbum UI hierarchy.");
        }

        Slider experienceBar = FindNamedComponent<Slider>(canvasRoot, "Exp_Bar");
        if (experienceBar == null)
        {
            GameObject experienceObject = UnityEngine.Object.Instantiate(
                healthBar.gameObject,
                healthBar.transform.parent);
            experienceObject.name = "Exp_Bar";

            RectTransform rect = experienceObject.GetComponent<RectTransform>();
            rect.anchoredPosition += new Vector2(0f, -28f);

            experienceBar = experienceObject.GetComponent<Slider>();
        }

        healthBar.interactable = false;
        healthBar.minValue = 0f;
        healthBar.maxValue = 1f;
        healthBar.value = 1f;

        experienceBar.interactable = false;
        experienceBar.minValue = 0f;
        experienceBar.maxValue = 1f;
        experienceBar.value = 0f;

        TextMeshProUGUI levelText = FindTextBelowNamedObject(canvasRoot, "Level");
        TextMeshProUGUI killText = FindTextBelowNamedObject(canvasRoot, "KillCountText");
        if (levelText == null || killText == null)
        {
            throw new InvalidOperationException("Level or kill-count text missing in Seungbum UI hierarchy.");
        }

        SerializedObject hudSerialized = new SerializedObject(hud);
        hudSerialized.FindProperty("hpbar").objectReferenceValue = healthBar;
        hudSerialized.FindProperty("expBar").objectReferenceValue = experienceBar;
        hudSerialized.FindProperty("lvText").objectReferenceValue = levelText;
        hudSerialized.FindProperty("killCount").objectReferenceValue = killText;
        hudSerialized.ApplyModifiedPropertiesWithoutUndo();

        UIManager manager = managerRoot.GetComponent<UIManager>();
        if (manager != null)
        {
            SerializedObject managerSerialized = new SerializedObject(manager);
            managerSerialized.FindProperty("hudDynamicUi").objectReferenceValue = hud;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ValidateScene(Scene scene, GameObject gameplayUi, GameObject gameSystems)
    {
        if (EditorBuildSettings.scenes.Length != 1 ||
            EditorBuildSettings.scenes[0].path != MainScenePath ||
            !EditorBuildSettings.scenes[0].enabled)
        {
            throw new InvalidOperationException("Build Settings must contain only SampleScene.");
        }

        if (gameplayUi == null || gameSystems == null)
        {
            throw new InvalidOperationException("GameplayUI or GameSystems root missing.");
        }

        int missingScriptCount = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Sum(transform => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject));
        if (missingScriptCount != 0)
        {
            throw new InvalidOperationException($"Scene contains {missingScriptCount} missing script(s).");
        }

        Type[] requiredSystems =
        {
            typeof(PlayerProgression),
            typeof(PlayerSkillSystem),
            typeof(LevelUpController),
            typeof(GameFlowController),
            typeof(GameplayHudBinder),
            typeof(GrayboxGameFlowView)
        };

        foreach (Type type in requiredSystems)
        {
            if (gameSystems.GetComponent(type) == null)
            {
                throw new InvalidOperationException($"GameSystems missing {type.Name}.");
            }
        }

        ValidateObjectReferences(
            gameSystems.GetComponent<PlayerSkillSystem>(),
            "weaponRunner",
            "startingMagic");
        ValidateObjectReferences(
            gameSystems.GetComponent<LevelUpController>(),
            "playerProgression",
            "playerSkillSystem",
            "gameFlowController",
            "view");
        ValidateObjectReferences(
            gameSystems.GetComponent<GameplayHudBinder>(),
            "hud",
            "playerHealth",
            "progression");

        HudDynamicUi hud = gameplayUi.GetComponentInChildren<HudDynamicUi>(true);
        if (hud == null)
        {
            throw new InvalidOperationException("GameplayUI missing HudDynamicUi.");
        }

        SerializedObject hudSerialized = new SerializedObject(hud);
        string[] requiredHudFields = { "timerText", "killCount", "expBar", "hpbar", "lvText" };
        foreach (string field in requiredHudFields)
        {
            if (hudSerialized.FindProperty(field).objectReferenceValue == null)
            {
                throw new InvalidOperationException($"HudDynamicUi.{field} is unassigned.");
            }
        }

        if (scene.GetRootGameObjects().Count(root => root.name == "EventSystem") > 0)
        {
            throw new InvalidOperationException("EventSystem must stay inside GameplayUI prefab.");
        }

        if (gameplayUi.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) == null)
        {
            throw new InvalidOperationException("GameplayUI prefab missing EventSystem.");
        }
    }

    private static void ValidateObjectReferences(UnityEngine.Object target, params string[] propertyNames)
    {
        SerializedObject serialized = new SerializedObject(target);
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName} is unassigned.");
            }
        }
    }

    private static GameObject CloneIntoScene(GameObject source, Transform parent, Scene targetScene)
    {
        GameObject clone = UnityEngine.Object.Instantiate(source);
        clone.name = source.name;
        SceneManager.MoveGameObjectToScene(clone, targetScene);
        clone.transform.SetParent(parent, false);
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = Quaternion.identity;
        clone.transform.localScale = Vector3.one;
        return clone;
    }

    private static void FixZeroCanvasScales(GameObject root)
    {
        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            Vector3 scale = canvas.transform.localScale;
            if (Mathf.Approximately(scale.x, 0f) &&
                Mathf.Approximately(scale.y, 0f) &&
                Mathf.Approximately(scale.z, 0f))
            {
                canvas.transform.localScale = Vector3.one;
            }
        }
    }

    private static T FindNamedComponent<T>(GameObject root, string objectName) where T : Component
    {
        return root.GetComponentsInChildren<T>(true)
            .FirstOrDefault(component => component.gameObject.name == objectName);
    }

    private static TextMeshProUGUI FindTextBelowNamedObject(GameObject root, string objectName)
    {
        Transform target = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.gameObject.name == objectName);
        return target != null ? target.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        return scene.IsValid()
            ? scene.GetRootGameObjects().FirstOrDefault(root => root.name == objectName)
            : null;
    }

    private static void DestroyRootIfPresent(Scene scene, string objectName)
    {
        GameObject existing = FindRoot(scene, objectName);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} not found.");
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateFolderRecursive(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int index = 1; index < parts.Length; index++)
        {
            string next = $"{current}/{parts[index]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[index]);
            }

            current = next;
        }
    }
}
