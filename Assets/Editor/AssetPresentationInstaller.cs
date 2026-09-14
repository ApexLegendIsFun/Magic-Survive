using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class AssetPresentationInstaller
{
    public const string Output = "Assets/02.Prefabs/Presentation";
    private const string ThirdParty = "Assets/Untracked Asset/";
    private const string LocalFont = "Assets/04.ThirdParty/GeneratedPresentation/DNPPixel SDF.asset";
    private const string KoreanFont = "Assets/02.Prefabs/UI/nanum-gothic/NanumGothic SDF.asset";
    private const string Gui = ThirdParty + "CasualFantasyGUIPack/Art/Textures/UI/";
    private const string PlayerModel = ThirdParty + "SPUM/Resources/Addons/RetroHeroes/2_Prefab/0_TeamOriginal/SPUM_20240910232736382.prefab";
    private static readonly string[] EnemyModels =
    {
        ThirdParty + "SPUM/Resources/Addons/Undead/2_Prefab/1_NormalZombie/SPUM_20241203205635802.prefab",
        ThirdParty + "SPUM/Resources/Addons/MS_Orc/2_Prefab/2_BlueOrc/SPUM_20240910225933543.prefab",
        ThirdParty + "SPUM/Resources/Addons/Undead/2_Prefab/2_EliteZomibes/SPUM_20241203205638738.prefab",
        ThirdParty + "FantasyMonsters/Bosses/Titans/Demon/Demon01.prefab"
    };
    private static readonly string[] Roles = { "Basic", "Fast", "Tank", "Boss" };
    private static readonly string[] EnemyBases = { "Enemy", "Enemy 2", "Enemy_Tank", "Enemy_Boss" };
    private static readonly string[] Effects =
    {
        ThirdParty + "2D_PFX/Prefabs/Fire/Fire_05.prefab",
        ThirdParty + "2D_PFX/Prefabs/Electric/Electric_07.prefab",
        ThirdParty + "2D_PFX/Prefabs/IceWater/IceWater_01.prefab",
        ThirdParty + "PixelAttackFx/Prefebs/Skill_18.prefab",
        ThirdParty + "PixelAttackFx/Prefebs/Skill_19.prefab"
    };
    private const string Click = ThirdParty + "Casual Game UI Sound/USER_INTERFACE/USER_INTERFACE_Button_Click_01.wav";
    private const string LevelUp = ThirdParty + "Casual Game UI Sound/USER_INTERFACE/USER_INTERFACE_Bubble_Zoom_01.wav";
    private static readonly List<string> sources = new List<string>();
    private static TMP_FontAsset font;
    private static TMP_FontAsset pixelFont;
    private static Sprite button;
    private static Sprite panel;
    private static Sprite frame;
    private static Sprite selectedFrame;
    private static SoundManager sound;

    [MenuItem("Tools/Magic Survive/Apply Existing Feature Assets")]
    public static void Apply()
    {
        Preflight();
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory(Path.GetDirectoryName(LocalFont));
        AssetDatabase.Refresh();
        font = Load<TMP_FontAsset>(KoreanFont);
        pixelFont = CreatePixelFont();
        button = LoadSprite(Gui + "Common/Common_Btn_Blue.png");
        panel = LoadSprite(Gui + "SubScreen/Levelup_Reward_Level_Frame.png");
        frame = LoadSprite(Gui + "SubScreen/Levelup_Reward_Goods_Frame.png");
        selectedFrame = LoadSprite(Gui + "SubScreen/Levelup_Reward_Goods_Frame_On.png");
        // Work in a temporary scene; never save source prefab instances into a work scene.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        for (int i = 0; i < Roles.Length; i++) BuildEnemy(i);
        for (int i = 0; i < Effects.Length; i++) BuildProjectile(i);
        var playerVisual = new GameObject("PlayerVisual");
        AttachVisual(playerVisual, PlayerModel, 1.25f, true, Color.white);
        var playerAsset = Save(playerVisual, "PlayerVisual");
        var ui = Instantiate("Assets/02.Prefabs/UI/GameplayUI.prefab");
        StyleUi(ui);
        var uiAsset = Save(ui, "GameplayUI");
        var damage = Instantiate("Assets/01.Scripts/UI/Prefabs_Ui/DamageText.prefab");
        StyleDamage(damage);
        var damageAsset = Save(damage, "DamageText");
        sound = BuildSound();
        ConfigureMainScene(playerAsset, uiAsset, damageAsset);
        ConfigureTitleScene();
        WriteManifest();
        AssetDatabase.SaveAssets();
        Debug.Log("[Asset Presentation] APPLIED: visual variants, five projectiles, UI/font, damage pool and sound bindings.");
    }

    private static void Preflight()
    {
        sources.Clear();
        sources.Add(PlayerModel);
        sources.AddRange(EnemyModels);
        sources.AddRange(Effects);
        sources.AddRange(new[] { Click, LevelUp, KoreanFont,
            ThirdParty + "DamageNumbersPro/Fonts/DNPPixel.ttf",
            Gui + "Common/Common_Btn_Blue.png", Gui + "Common/Common_Icon_HP.png",
            Gui + "SubScreen/Levelup_Reward_Level_Frame.png",
            Gui + "SubScreen/Levelup_Reward_Goods_Frame.png",
            Gui + "SubScreen/Levelup_Reward_Goods_Frame_On.png",
            "Assets/02.Prefabs/UI/GameplayUI.prefab", "Assets/02.Prefabs/Sound/Sound.prefab",
            "Assets/01.Scripts/UI/Prefabs_Ui/DamageText.prefab", "Assets/02.Prefabs/Projectile/Projectile.prefab" });
        foreach (var name in EnemyBases) sources.Add($"Assets/02.Prefabs/Enemy/{name}.prefab");
        var missing = sources.Where(path => AssetDatabase.LoadMainAssetAtPath(path) == null).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException("Import the team packages first. Missing assets:\n" + string.Join("\n", missing));
    }
    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        var result = AssetDatabase.LoadAssetAtPath<T>(path);
        if (result == null) throw new InvalidOperationException($"Missing {typeof(T).Name}: {path}");
        return result;
    }
    private static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite == null) throw new InvalidOperationException("No imported sprite: " + path);
        return sprite;
    }
    private static GameObject Instantiate(string path) => (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path));
    private static GameObject Save(GameObject root, string name)
    {
        root.name = name;
        var asset = PrefabUtility.SaveAsPrefabAsset(root, $"{Output}/{name}.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        if (asset == null) throw new InvalidOperationException("Could not save " + name);
        return asset;
    }
    private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException($"Missing field: {target.name}.{field}");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
    private static void BuildEnemy(int index)
    {
        var root = Instantiate($"Assets/02.Prefabs/Enemy/{EnemyBases[index]}.prefab");
        foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true)) sprite.enabled = false;
        float radius = root.GetComponent<Enemy>().HitRadius;
        AttachVisual(root, EnemyModels[index], radius * (index == 3 ? 3f : 2.4f), true, Color.white);
        var variant = Save(root, "Enemy_" + Roles[index]);
        SetReference(Load<EnemyData>($"Assets/03.Data/Enemy/Enemy_{Roles[index]}.asset"), "prefab", variant.GetComponent<Enemy>());
    }
    private static void BuildProjectile(int index)
    {
        var element = MagicContentCatalog.PentagonElements[index];
        var root = Instantiate("Assets/02.Prefabs/Projectile/Projectile.prefab");
        foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true)) sprite.enabled = false;
        Color tint = element == MagicElement.Earth ? new Color(0.65f, 0.4f, 0.18f) :
            element == MagicElement.Dark ? new Color(0.65f, 0.24f, 0.95f) : Color.white;
        AttachVisual(root, Effects[index], 0.55f, false, tint);
        var variant = Save(root, "Projectile_" + element);
        SetReference(Load<ProjectileMagicDefinition>($"Assets/03.Data/Magic/{MagicContentCatalog.GetMagicId(element)}.asset"),
            "projectilePrefab", variant.GetComponent<Projectile>());
    }
    private static void AttachVisual(GameObject root, string source, float height, bool character, Color tint)
    {
        var container = new GameObject("AssetVisual");
        container.transform.SetParent(root.transform, false);
        var model = Instantiate(source);
        model.transform.SetParent(container.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        foreach (var body in model.GetComponentsInChildren<Rigidbody2D>(true)) UnityEngine.Object.DestroyImmediate(body);
        foreach (var collider in model.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        // Selected VFX use Animator only. Character package components are data/callback helpers.
        foreach (var particle in model.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particle.main;
            main.stopAction = ParticleSystemStopAction.None;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }
        foreach (var animator in model.GetComponentsInChildren<Animator>(true))
        {
            animator.applyRootMotion = false;
            animator.keepAnimatorStateOnDisable = false;
            if (!character) animator.fireEvents = false;
        }
        var renderers = model.GetComponentsInChildren<SpriteRenderer>().Where(r => r.enabled && r.sprite != null).ToArray();
        if (renderers.Length == 0) throw new InvalidOperationException("No visible sprites: " + source);
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float dimension = character ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.y);
        model.transform.localScale *= height / Mathf.Max(0.01f, dimension);
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        Vector3 offset = root.transform.position - bounds.center;
        model.transform.position += new Vector3(offset.x, offset.y, 0f);
        var sorting = container.AddComponent<SortingGroup>();
        sorting.sortingOrder = character ? 1000 : 5000;
        var lifecycle = root.AddComponent<AssetVisualLifecycle>();
        lifecycle.visual = container.transform;
        lifecycle.faceMovement = character;
        lifecycle.repeatAnimation = !character;
        lifecycle.tint = tint;
    }
    private static void StyleUi(GameObject root)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        { text.font = font; EditorUtility.SetDirty(text); }
        foreach (var b in root.GetComponentsInChildren<Button>(true))
            if (b.image != null) { b.image.sprite = button; b.image.type = Image.Type.Sliced; }
        foreach (var popup in root.GetComponentsInChildren<PopupUi>(true))
        {
            var serialized = new SerializedObject(popup);
            var slots = serialized.FindProperty("elementSlots");
            for (int i = 0; i < slots.arraySize; i++)
            {
                var slot = slots.GetArrayElementAtIndex(i);
                var b = slot.FindPropertyRelative("button").objectReferenceValue as Button;
                if (b != null && b.image != null)
                {
                    b.image.sprite = frame;
                    var colors = b.colors;
                    colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
                    colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
                    b.colors = colors;
                }
                var element = (MagicElement)slot.FindPropertyRelative("element").intValue;
                int elementIndex = MagicContentCatalog.PentagonElements.ToList().IndexOf(element);
                var icon = slot.FindPropertyRelative("icon").objectReferenceValue as Image;
                if (icon != null && elementIndex >= 0)
                {
                    var effect = Load<GameObject>(Effects[elementIndex]);
                    icon.sprite = effect.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.sprite != null).sprite;
                    var animator = effect.GetComponentInChildren<Animator>();
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        var clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault();
                        if (clip != null)
                        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                        {
                            if (binding.propertyName != "m_Sprite") continue;
                            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                            if (keys.Length > 0 && keys[keys.Length / 2].value is Sprite sprite) icon.sprite = sprite;
                            break;
                        }
                    }
                    icon.preserveAspect = true;
                    icon.color = element == MagicElement.Earth ? new Color(0.65f, 0.4f, 0.18f) :
                        element == MagicElement.Dark ? new Color(0.65f, 0.24f, 0.95f) : Color.white;
                }
                var highlight = slot.FindPropertyRelative("pendingHighlight").objectReferenceValue as GameObject;
                if (highlight != null && highlight.TryGetComponent<Image>(out var image))
                { image.sprite = selectedFrame; image.type = Image.Type.Sliced; }
            }
            var description = serialized.FindProperty("previewDescriptionText").objectReferenceValue as TMP_Text;
            if (description != null)
            {
                description.enableAutoSizing = true;
                description.fontSizeMin = 18;
                description.fontSizeMax = 30;
                description.color = Color.white;
                var backing = description.GetComponentInParent<Image>();
                if (backing != null)
                {
                    backing.sprite = button;
                    backing.type = Image.Type.Sliced;
                    backing.color = new Color(0.18f, 0.23f, 0.32f);
                    foreach (var label in backing.GetComponentsInChildren<TMP_Text>(true)) label.color = Color.white;
                }
            }
        }
        foreach (var image in root.GetComponentsInChildren<Image>(true))
            if (image.name == "Common_Icon_HP" || image.name == "Hp_Icon") image.sprite = LoadSprite(Gui + "Common/Common_Icon_HP.png");
        // Explicitly record all variant overrides; never apply to the source asset.
        foreach (var component in root.GetComponentsInChildren<Component>(true))
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }
    private static TMP_FontAsset CreatePixelFont()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LocalFont);
        if (existing != null && existing.material != null && existing.atlasTextures.Length > 0 && existing.atlasTextures[0] != null) return existing;
        if (existing != null) AssetDatabase.DeleteAsset(LocalFont);
        var result = TMP_FontAsset.CreateFontAsset(Load<Font>(ThirdParty + "DamageNumbersPro/Fonts/DNPPixel.ttf"));
        result.name = "DNPPixel SDF";
        result.TryAddCharacters("0123456789.,+-");
        result.atlasPopulationMode = AtlasPopulationMode.Static;
        var material = result.material;
        var textures = result.atlasTextures;
        AssetDatabase.CreateAsset(result, LocalFont);
        AssetDatabase.AddObjectToAsset(material, result);
        foreach (var atlas in textures) AssetDatabase.AddObjectToAsset(atlas, result);
        result.material = material;
        result.atlasTextures = textures;
        material.mainTexture = textures[0];
        EditorUtility.SetDirty(material);
        foreach (var atlas in textures) EditorUtility.SetDirty(atlas);
        EditorUtility.SetDirty(result);
        AssetDatabase.SaveAssets();
        return result;
    }
    private static void StyleDamage(GameObject damage)
    {
        var root = (RectTransform)damage.transform;
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(150, 60);
        root.localScale = Vector3.one;
        foreach (var text in damage.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = pixelFont;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        }
        foreach (var graphic in damage.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        var serialized = new SerializedObject(damage.GetComponent<DamageUi>());
        serialized.FindProperty("moveSpeed").floatValue = 45;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static SoundManager BuildSound()
    {
        var root = Instantiate("Assets/02.Prefabs/Sound/Sound.prefab");
        var inheritedManager = root.GetComponentInChildren<SoundManager>(true);
        var manager = root.AddComponent<SoundManager>();
        EditorUtility.CopySerialized(inheritedManager, manager);
        UnityEngine.Object.DestroyImmediate(inheritedManager);
        // The existing SoundManager requires volume Sliders even in direct SampleScene play.
        // Inactive configuration controls satisfy that contract without adding visible UI.
        foreach (string field in new[] { "bgmSlider", "sfxSlider" })
        {
            var control = new GameObject(field, typeof(RectTransform), typeof(Slider));
            control.transform.SetParent(root.transform, false);
            control.SetActive(false);
            SetReference(manager, field, control.GetComponent<Slider>());
        }
        var audio = root.GetComponentsInChildren<AudioSource>(true);
        while (audio.Length < 3)
        {
            var child = new GameObject("SfxUiAudio", typeof(AudioSource));
            child.transform.SetParent(root.transform, false);
            audio = root.GetComponentsInChildren<AudioSource>(true);
        }
        SetReference(manager, "sfxUiAudioSource", audio[2]);
        foreach (var source in audio) { source.playOnAwake = false; source.spatialBlend = 0f; }
        manager.soundClip = new[] { Load<AudioClip>(Click), Load<AudioClip>(LevelUp) };
        return Save(root, "SoundPresentation").GetComponent<SoundManager>();
    }
    private static void ConfigureMainScene(GameObject playerVisual, GameObject uiAsset, GameObject damageAsset)
    {
        var scene = EditorSceneManager.OpenScene("Assets/00.Scenes/SampleScene.unity");
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player == null) throw new InvalidOperationException("Player missing.");
        var oldVisual = player.transform.Find("PlayerVisual");
        if (oldVisual != null) UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(playerVisual, player.transform);
        visual.transform.localPosition = Vector3.zero;
        var oldSprite = player.GetComponent<SpriteRenderer>();
        if (oldSprite != null) oldSprite.enabled = false;
        var ui = scene.GetRootGameObjects().First(go => go.name == "GameplayUI");
        PrefabUtility.ReplacePrefabAssetOfPrefabInstance(ui, uiAsset,
            new PrefabReplacingSettings { objectMatchMode = ObjectMatchMode.ByHierarchy,
                prefabOverridesOptions = PrefabOverridesOptions.KeepAllPossibleOverrides,
                changeRootNameToAssetName = false }, InteractionMode.AutomatedAction);
        StyleUi(ui);
        var bossSpawner = UnityEngine.Object.FindFirstObjectByType<BossSpawner>(FindObjectsInactive.Include);
        if (bossSpawner == null) bossSpawner = new GameObject("BossSpawner").AddComponent<BossSpawner>();
        var bossBindings = new SerializedObject(bossSpawner);
        bossBindings.FindProperty("runDirector").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<RunDirector>();
        bossBindings.FindProperty("enemyManager").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<EnemyManager>();
        bossBindings.FindProperty("bossData").objectReferenceValue = Load<EnemyData>("Assets/03.Data/Enemy/Enemy_Boss.asset");
        bossBindings.FindProperty("spawnCamera").objectReferenceValue = Camera.main;
        bossBindings.ApplyModifiedPropertiesWithoutUndo();
        var oldPresentation = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "AssetPresentation");
        if (oldPresentation != null) UnityEngine.Object.DestroyImmediate(oldPresentation);
        var root = new GameObject("AssetPresentation");
        var canvasObject = new GameObject("DamageCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(root.transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var pool = UnityEngine.Object.FindFirstObjectByType<UiObjectPool>(FindObjectsInactive.Include);
        if (pool == null) pool = root.AddComponent<UiObjectPool>();
        var serialized = new SerializedObject(pool);
        var list = serialized.FindProperty("objList");
        list.arraySize = 1;
        list.GetArrayElementAtIndex(0).objectReferenceValue = damageAsset;
        serialized.FindProperty("poolsize").intValue = 30;
        serialized.FindProperty("uiParent").objectReferenceValue = canvasObject.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(pool)) PrefabUtility.RecordPrefabInstancePropertyModifications(pool);
        AddFeedback(root, canvas);
        EditorSceneManager.SaveScene(scene);
    }
    private static void ConfigureTitleScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/01.Scripts/UI/TitleScene.unity");
        foreach (var root in scene.GetRootGameObjects()) StyleUi(root);
        var presentation = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "AssetPresentation");
        if (presentation == null) presentation = new GameObject("AssetPresentation");
        AddFeedback(presentation, null);
        EditorSceneManager.SaveScene(scene);
    }
    private static void AddFeedback(GameObject root, Canvas canvas)
    {
        var feedback = root.GetComponent<AssetPresentationFeedback>() ?? root.AddComponent<AssetPresentationFeedback>();
        feedback.soundPrefab = sound;
        feedback.clickClip = Load<AudioClip>(Click);
        feedback.levelUpClip = Load<AudioClip>(LevelUp);
        feedback.font = font;
        feedback.buttonSprite = button;
        feedback.panelSprite = panel;
        feedback.damageCanvas = canvas;
    }
    [Serializable] private class Manifest { public string note; public Entry[] dependencies; }
    [Serializable] private class Entry { public string path; public string guid; }
    private static void WriteManifest()
    {
        var paths = AssetDatabase.GetDependencies(sources.ToArray(), true).Concat(new[] { LocalFont }).Distinct().OrderBy(p => p);
        var manifest = new Manifest
        {
            note = "Team-owned packages and generated DNPPixel atlas stay local/ignored. Import packages, then run Apply Existing Feature Assets. Do not publish package content.",
            dependencies = paths.Select(path => new Entry { path = path, guid = AssetDatabase.AssetPathToGUID(path) }).ToArray()
        };
        File.WriteAllText("Docs/AssetPresentationDependencies.json", JsonUtility.ToJson(manifest, true));
    }
}
