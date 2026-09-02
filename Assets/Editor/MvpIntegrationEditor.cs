using System;
using System.Collections.Generic;
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
    private const string TitleScenePath = "Assets/01.Scripts/UI/TitleScene.unity";
    private const string UiSourceScenePath = "Assets/01.Scripts/UI/UI_SampleScene.unity";
    private const string GameplayUiPrefabPath = "Assets/02.Prefabs/UI/GameplayUI.prefab";
    private const string ProjectilePrefabPath = "Assets/02.Prefabs/Projectile/Projectile.prefab";
    private const string MagicDataFolder = "Assets/03.Data/Magic";
    private const string BasicEnemyPath = "Assets/03.Data/Enemy/Enemy_A.asset";
    private const string FastEnemyPath = "Assets/03.Data/Enemy/Enemy_B.asset";

    [MenuItem("Tools/Magic Survive/Build MVP Integration Scene")]
    public static void Run()
    {
        CreateFolderRecursive("Assets/02.Prefabs/UI");
        CreateFolderRecursive(MagicDataFolder);

        ConfigureTitleScene();
        EnsureTargetedMagics();
        Scene mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        GameObject gameplayUi = FindRoot(mainScene, "GameplayUI");
        if (gameplayUi == null)
        {
            gameplayUi = ImportGameplayUi(mainScene);
        }

        GameObject gameSystems = FindRoot(mainScene, "GameSystems");
        if (gameSystems == null)
        {
            gameSystems = CreateGameSystems(mainScene, gameplayUi, LoadTargetedMagics());
        }

        EditorSceneManager.MarkSceneDirty(mainScene);
        EditorSceneManager.SaveScene(mainScene, MainScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TitleScenePath, true),
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
        Scene titleScene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        ValidateTitleScene(titleScene);
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        GameObject gameplayUi = FindRoot(scene, "GameplayUI");
        GameObject gameSystems = FindRoot(scene, "GameSystems");
        ValidateScene(scene, gameplayUi, gameSystems);
        Debug.Log("[MVP Integration] Validation passed.");
    }

    private static ProjectileMagicDefinition[] EnsureTargetedMagics()
    {
        GameObject projectileObject = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        Projectile projectile = projectileObject != null ? projectileObject.GetComponent<Projectile>() : null;
        if (projectile == null)
        {
            throw new InvalidOperationException($"Projectile prefab missing: {ProjectilePrefabPath}");
        }

        MagicId[] magicIds =
        {
            MagicId.FireBolt,
            MagicId.ChainLightning,
            MagicId.IceSpear,
            MagicId.RockSpear,
            MagicId.ShadowOrb
        };

        ProjectileMagicDefinition[] definitions =
        {
            EnsureTargetedMagic(projectile, MagicId.FireBolt, MagicElement.Fire, 6f, 0.8f, 0),
            EnsureTargetedMagic(projectile, MagicId.ChainLightning, MagicElement.Lightning, 4f, 0.7f, 0),
            EnsureTargetedMagic(projectile, MagicId.IceSpear, MagicElement.Frost, 5f, 0.9f, 1),
            EnsureTargetedMagic(projectile, MagicId.RockSpear, MagicElement.Earth, 8f, 1.1f, 0),
            EnsureTargetedMagic(projectile, MagicId.ShadowOrb, MagicElement.Dark, 6f, 1f, 1)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        for (int index = 0; index < definitions.Length; index++)
        {
            string path = $"{MagicDataFolder}/{magicIds[index]}.asset";
            definitions[index] = AssetDatabase.LoadAssetAtPath<ProjectileMagicDefinition>(path);
            if (definitions[index] == null)
            {
                throw new InvalidOperationException($"Targeted magic asset failed to load: {path}");
            }
        }

        return definitions;
    }

    private static ProjectileMagicDefinition EnsureTargetedMagic(
        Projectile projectile,
        MagicId magicId,
        MagicElement element,
        float damage,
        float cooldown,
        int pierceCount)
    {
        string path = $"{MagicDataFolder}/{magicId}.asset";
        ProjectileMagicDefinition definition =
            AssetDatabase.LoadAssetAtPath<ProjectileMagicDefinition>(path);

        if (definition != null)
        {
            if (definition.MagicId != magicId)
            {
                throw new InvalidOperationException(
                    $"Existing targeted magic identity mismatch: {path}");
            }

            return definition;
        }

        definition = ScriptableObject.CreateInstance<ProjectileMagicDefinition>();
        definition.name = magicId.ToString();
        AssetDatabase.CreateAsset(definition, path);

        SerializedObject serialized = new SerializedObject(definition);
        serialized.FindProperty("magicId").enumValueIndex = (int)magicId;
        serialized.FindProperty("element").enumValueIndex = (int)element;
        serialized.FindProperty("projectilePrefab").objectReferenceValue = projectile;
        serialized.FindProperty("cooldown").floatValue = cooldown;
        serialized.FindProperty("range").floatValue = 8f;
        serialized.FindProperty("damage").floatValue = damage;
        serialized.FindProperty("speed").floatValue = 12f;
        serialized.FindProperty("maxDistance").floatValue = 10f;
        serialized.FindProperty("hitRadius").floatValue = 0.25f;
        serialized.FindProperty("pierceCount").intValue = pierceCount;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static ProjectileMagicDefinition[] LoadTargetedMagics()
    {
        MagicId[] magicIds =
        {
            MagicId.FireBolt,
            MagicId.ChainLightning,
            MagicId.IceSpear,
            MagicId.RockSpear,
            MagicId.ShadowOrb
        };

        ProjectileMagicDefinition[] definitions =
            new ProjectileMagicDefinition[magicIds.Length];
        for (int index = 0; index < magicIds.Length; index++)
        {
            string path = $"{MagicDataFolder}/{magicIds[index]}.asset";
            definitions[index] = AssetDatabase.LoadAssetAtPath<ProjectileMagicDefinition>(path);
            if (definitions[index] == null)
            {
                throw new InvalidOperationException($"Targeted magic asset failed to load: {path}");
            }
        }

        return definitions;
    }

    private static void ConfigureTitleScene()
    {
        Scene titleScene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        GameObject[] roots = titleScene.GetRootGameObjects();
        Transform startTransform = roots
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(candidate => candidate.name == "GameStart");

        if (startTransform == null)
        {
            throw new InvalidOperationException("Seungbum TitleScene GameStart object is missing.");
        }

        foreach (GameObject root in roots)
        {
            FixZeroCanvasScales(root);
        }

        Image targetGraphic = startTransform.GetComponent<Image>();
        if (targetGraphic == null)
        {
            throw new InvalidOperationException("TitleScene GameStart image is missing.");
        }

        Button startButton = startTransform.GetComponent<Button>();
        if (startButton == null)
        {
            startButton = startTransform.gameObject.AddComponent<Button>();
        }

        startButton.targetGraphic = targetGraphic;

        GameObject flowObject = FindRoot(titleScene, "TitleFlow");
        if (flowObject == null)
        {
            flowObject = new GameObject("TitleFlow");
            SceneManager.MoveGameObjectToScene(flowObject, titleScene);
        }

        TitleSceneController controller = flowObject.GetComponent<TitleSceneController>();
        if (controller == null)
        {
            controller = flowObject.AddComponent<TitleSceneController>();
        }

        SetObjectReference(controller, "gameStartButton", startButton);
        EditorSceneManager.MarkSceneDirty(titleScene);
        EditorSceneManager.SaveScene(titleScene, TitleScenePath);
        ValidateTitleScene(titleScene);
    }

    private static void ValidateTitleScene(Scene titleScene)
    {
        GameObject flowObject = FindRoot(titleScene, "TitleFlow");
        TitleSceneController controller = flowObject != null
            ? flowObject.GetComponent<TitleSceneController>()
            : null;
        if (controller == null)
        {
            throw new InvalidOperationException("TitleScene missing TitleSceneController.");
        }

        ValidateObjectReferences(controller, "gameStartButton");

        Button startButton = titleScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Button>(true))
            .FirstOrDefault(button => button.name == "GameStart");
        if (startButton == null)
        {
            throw new InvalidOperationException("TitleScene GameStart button is missing.");
        }
    }

    private static GameObject ImportGameplayUi(Scene mainScene)
    {
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayUiPrefabPath);
        if (existingPrefab != null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(existingPrefab, mainScene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("GameplayUI prefab could not be instantiated.");
            }

            instance.name = "GameplayUI";
            FixZeroCanvasScales(instance);

            GameObject canvas = instance.transform.Find("Canvas_InGame")?.gameObject;
            GameObject manager = instance.transform.Find("UiManager")?.gameObject;
            if (canvas == null || manager == null)
            {
                throw new InvalidOperationException("GameplayUI prefab roots are incomplete.");
            }

            ConfigureHud(canvas, manager);
            return instance;
        }

        Behaviour[] mainSceneLights = mainScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Behaviour>(true))
            .Where(component =>
                component.enabled &&
                component.GetType().FullName == "UnityEngine.Rendering.Universal.Light2D")
            .ToArray();

        foreach (Behaviour light in mainSceneLights)
        {
            light.enabled = false;
        }

        Scene uiScene = default;

        try
        {
            uiScene = EditorSceneManager.OpenScene(UiSourceScenePath, OpenSceneMode.Additive);
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

            foreach (Behaviour light in mainSceneLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                }
            }
        }
    }

    private static GameObject CreateGameSystems(
        Scene mainScene,
        GameObject gameplayUi,
        ProjectileMagicDefinition[] targetedMagics)
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
        RunDirector runDirector = systems.AddComponent<RunDirector>();
        SpawnDirector spawnDirector = systems.AddComponent<SpawnDirector>();
        LevelUpController levelUp = systems.AddComponent<LevelUpController>();
        GameplayHudBinder hudBinder = systems.AddComponent<GameplayHudBinder>();

        SetObjectReference(skills, "weaponRunner", weaponRunner);
        SetObjectReferences(skills, "targetedMagicDefinitions", targetedMagics);

        SetObjectReference(progression, "playerHealth", health);

        SetObjectReference(levelUp, "playerProgression", progression);
        SetObjectReference(levelUp, "playerSkillSystem", skills);
        SetObjectReference(levelUp, "gameFlowController", flow);
        SetObjectReference(levelUp, "runDirector", runDirector);
        SetObjectReference(levelUp, "view", graybox);

        EnemyManager enemyManager = UnityEngine.Object.FindFirstObjectByType<EnemyManager>();
        if (enemyManager == null)
        {
            throw new InvalidOperationException("EnemyManager missing from SampleScene.");
        }

        SetObjectReference(runDirector, "gameFlowController", flow);
        SetObjectReference(runDirector, "playerProgression", progression);
        SetObjectReference(runDirector, "playerSkillSystem", skills);
        SetObjectReference(runDirector, "enemyManager", enemyManager);

        SetObjectReference(spawnDirector, "enemyManager", enemyManager);
        SetObjectReference(spawnDirector, "gameFlowController", flow);
        SetObjectReference(spawnDirector, "runDirector", runDirector);
        SetObjectReference(spawnDirector, "spawnCamera", Camera.main);
        SetObjectReferences(
            spawnDirector,
            "normalEnemies",
            new UnityEngine.Object[]
            {
                AssetDatabase.LoadAssetAtPath<EnemyData>(BasicEnemyPath),
                AssetDatabase.LoadAssetAtPath<EnemyData>(FastEnemyPath),
                null,
                null
            });

        TestEnemySpawner legacySpawner = UnityEngine.Object.FindFirstObjectByType<TestEnemySpawner>();
        if (legacySpawner != null)
        {
            legacySpawner.enabled = false;
        }

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

        Image healthBar = FindNamedComponent<Image>(canvasRoot, "HpBar");
        if (healthBar == null)
        {
            throw new InvalidOperationException("HpBar image missing in Seungbum UI hierarchy.");
        }

        Image experienceBar = FindNamedComponent<Image>(canvasRoot, "Level");
        if (experienceBar == null)
        {
            throw new InvalidOperationException("Level image missing in Seungbum UI hierarchy.");
        }

        ConfigureFillBar(healthBar, 1f);
        ConfigureFillBar(experienceBar, 0f);

        TextMeshProUGUI levelText = FindTextBelowNamedObject(canvasRoot, "Level");
        TextMeshProUGUI killText = FindTextBelowNamedObject(canvasRoot, "KillCountText");
        if (levelText == null || killText == null)
        {
            throw new InvalidOperationException("Level or kill-count text missing in Seungbum UI hierarchy.");
        }

        SerializedObject hudSerialized = new SerializedObject(hud);
        hudSerialized.FindProperty("gameTime").floatValue = 600f;
        hudSerialized.FindProperty("hpBar").objectReferenceValue = healthBar;
        hudSerialized.FindProperty("levelupBar").objectReferenceValue = experienceBar;
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
        if (EditorBuildSettings.scenes.Length != 2 ||
            EditorBuildSettings.scenes[0].path != TitleScenePath ||
            !EditorBuildSettings.scenes[0].enabled ||
            EditorBuildSettings.scenes[1].path != MainScenePath ||
            !EditorBuildSettings.scenes[1].enabled)
        {
            throw new InvalidOperationException("Build Settings must contain TitleScene then SampleScene.");
        }

        GameObject[] sceneRoots = scene.GetRootGameObjects();
        int gameplayUiCount = sceneRoots.Count(root => root.name == "GameplayUI");
        int gameSystemsCount = sceneRoots.Count(root => root.name == "GameSystems");
        if (gameplayUiCount != 1 || gameSystemsCount != 1 ||
            gameplayUi == null || gameSystems == null)
        {
            throw new InvalidOperationException(
                $"Scene must contain exactly one GameplayUI and GameSystems root " +
                $"(found {gameplayUiCount} and {gameSystemsCount}).");
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
            typeof(RunDirector),
            typeof(SpawnDirector),
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

        PlayerSkillSystem skillSystem = gameSystems.GetComponent<PlayerSkillSystem>();
        ValidateObjectReferences(skillSystem, "weaponRunner");
        ValidateObjectReferenceArray(skillSystem, "targetedMagicDefinitions", 5);
        ValidateObjectReferences(
            gameSystems.GetComponent<PlayerProgression>(),
            "playerHealth");
        ValidateObjectReferences(
            gameSystems.GetComponent<LevelUpController>(),
            "playerProgression",
            "playerSkillSystem",
            "gameFlowController",
            "runDirector",
            "view");
        ValidateObjectReferences(
            gameSystems.GetComponent<RunDirector>(),
            "gameFlowController",
            "playerProgression",
            "playerSkillSystem",
            "enemyManager");
        ValidateObjectReferences(
            gameSystems.GetComponent<SpawnDirector>(),
            "enemyManager",
            "gameFlowController",
            "runDirector",
            "spawnCamera");
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

        string[] requiredHudFields = { "timerText", "killCount", "hpBar", "levelupBar", "lvText" };
        ValidateObjectReferences(hud, requiredHudFields);

        SerializedObject hudSerialized = new SerializedObject(hud);
        ValidateFillBar(
            hudSerialized.FindProperty("hpBar").objectReferenceValue as Image,
            "HudDynamicUi.hpBar");
        ValidateFillBar(
            hudSerialized.FindProperty("levelupBar").objectReferenceValue as Image,
            "HudDynamicUi.levelupBar");

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

    private static void ValidateObjectReferenceArray(
        UnityEngine.Object target,
        string propertyName,
        int expectedCount)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray || property.arraySize != expectedCount)
        {
            throw new InvalidOperationException(
                $"{target.GetType().Name}.{propertyName} must contain {expectedCount} entries.");
        }

        for (int index = 0; index < property.arraySize; index++)
        {
            if (property.GetArrayElementAtIndex(index).objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name}.{propertyName}[{index}] is unassigned.");
            }
        }
    }

    private static void ConfigureFillBar(Image bar, float fillAmount)
    {
        bar.material = null;
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = (int)Image.OriginHorizontal.Left;
        bar.fillAmount = fillAmount;
    }

    private static void ValidateFillBar(Image bar, string label)
    {
        if (bar == null ||
            bar.type != Image.Type.Filled ||
            bar.fillMethod != Image.FillMethod.Horizontal ||
            bar.fillOrigin != (int)Image.OriginHorizontal.Left)
        {
            throw new InvalidOperationException(
                $"{label} must be a left-to-right Filled image.");
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
                EditorUtility.SetDirty(canvas.transform);
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

    private static void SetObjectReferences(
        UnityEngine.Object target,
        string propertyName,
        IReadOnlyList<UnityEngine.Object> values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} array not found.");
        }

        property.arraySize = values != null ? values.Count : 0;
        for (int index = 0; index < property.arraySize; index++)
        {
            UnityEngine.Object value = values[index];
            if (value == null && propertyName == "targetedMagicDefinitions")
            {
                throw new InvalidOperationException($"{propertyName}[{index}] source is null.");
            }

            property.GetArrayElementAtIndex(index).objectReferenceValue = value;
        }

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
