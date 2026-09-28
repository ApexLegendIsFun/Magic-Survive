using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Seondong.IntegrationGame
{
    public static class FinalSubmissionWiring
    {
        const string UI = "Assets/02.Prefabs/UI/GameplayUI.prefab";
        const string Effects = "Assets/02.Prefabs/UI/Effect/";
        static T[] All<T>(Scene s) where T : Component => s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        static T Load<T>(string p) where T : Object => AssetDatabase.LoadAssetAtPath<T>(p) ?? throw new Exception("Missing asset: " + p);
        static GameObject Instantiate(string p, Scene s) => (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(p), s);
        static void Set(Object o, string field, Object value)
        { var s = new SerializedObject(o); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
        static T Get<T>(Object o, string field) where T : Object => new SerializedObject(o).FindProperty(field).objectReferenceValue as T;
        static Transform Named(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        static T Move<T>(T component, GameObject target) where T : Component
        {
            if (component.gameObject == target) return component;
            UnityEditorInternal.ComponentUtility.CopyComponent(component);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target);
            Object.DestroyImmediate(component);
            return target.GetComponent<T>();
        }
        static void RenameButton(Object o, string field, string name) { var b = Get<Button>(o, field); if (b != null) b.name = name; }

        public static void ConfigureGame(Scene scene)
        {
            var systems = All<GameFlowController>(scene).Single().gameObject;
            var run = systems.GetComponent<RunDirector>();
            var skills = systems.GetComponent<PlayerSkillSystem>();
            Set(skills, "gameFlowController", systems.GetComponent<GameFlowController>());
            Set(skills, "levelUpController", systems.GetComponent<LevelUpController>());
            var manager = All<EnemyManager>(scene).Single();
            var player = All<PlayerController>(scene).Single();
            var input = new SerializedObject(player.GetComponent<PlayerInput>());
            input.FindProperty("m_DefaultActionMap").stringValue = "Player";
            input.ApplyModifiedPropertiesWithoutUndo();
            var spawner = All<BossSpawner>(scene).Single();
            var camera = All<Camera>(scene).Single(c => c.CompareTag("MainCamera"));
            foreach (var t in All<TestEnemySpawner>(scene)) Object.DestroyImmediate(t.gameObject);
            foreach (var t in All<GrayboxGameFlowView>(scene)) Object.DestroyImmediate(t);
            Set(systems.GetComponent<LevelUpController>(), "view", null);
            foreach (var t in All<IntegrationBossHud>(scene)) Object.DestroyImmediate(t);

            GameObject ui = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "FinalTeamUI");
            if (ui == null)
            {
                foreach (var root in scene.GetRootGameObjects().Where(r => r.GetComponentInChildren<HudDynamicUi>(true) != null || r.GetComponent<SoundManager>() != null).ToArray())
                    Object.DestroyImmediate(root);
                ui = Instantiate(UI, scene);
                ui.name = "FinalTeamUI";
                PrefabUtility.UnpackPrefabInstance(ui, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var popup = ui.GetComponentInChildren<PopupUi>(true);
                var result = ui.GetComponentInChildren<ResultUi>(true);
                // The delivered components shared Canvas_Popup. Keep that canvas active;
                // each component controls only its own authored panel.
                popup.gameObject.SetActive(true);
                popup = Move(popup, Named(ui, "PopUp").gameObject);
                result = Move(result, Named(ui, "Result_Panel").gameObject);
                result.gameObject.SetActive(false);
                popup.gameObject.SetActive(false);
                var overlay = Named(ui, "PopupUi_Panel").GetComponent<Image>();
                if (overlay != null) { overlay.color = Color.clear; overlay.raycastTarget = false; }
            }
            ConfigureUi(scene, ui, systems, player);
            ConfigureSound(All<SoundManager>(scene).Single());
            ConfigureEffects(scene);
            ConfigureEnemies();
            var elite = systems.GetComponent<IntegrationEliteSpawner>() ?? systems.AddComponent<IntegrationEliteSpawner>();
            Set(elite, "runDirector", run); Set(elite, "enemyManager", manager); Set(elite, "spawnCamera", camera);
            Set(elite, "charger", Load<EnemyData>("Assets/03.Data/Enemy/Enemy_Dasher.asset"));
            Set(elite, "summoner", Load<EnemyData>("Assets/03.Data/Enemy/Enemy_Summoner.asset"));
            Set(spawner, "bossData", Load<EnemyData>("Assets/03.Data/Enemy/Enemy_Boss.asset"));
            Set(player.GetComponent<ProjectileLauncher>(), "playerHealth", player.GetComponent<Health>());
            AssetDatabase.SaveAssets();
            Validate(scene);
        }

        static void ConfigureUi(Scene scene, GameObject ui, GameObject systems, PlayerController player)
        {
            var popup = ui.GetComponentInChildren<PopupUi>(true);
            var result = ui.GetComponentInChildren<ResultUi>(true);
            Set(popup, "levelUpController", systems.GetComponent<LevelUpController>());
            Set(result, "gameFlowController", systems.GetComponent<GameFlowController>());
            RenameButton(popup, "startButton", "ConfirmStart");
            RenameButton(popup, "gainButton", "ConfirmButton");
            RenameButton(popup, "cancelButton", "CancelSelection");
            RenameButton(result, "title", "TitleButton"); RenameButton(result, "reStart", "RestartButton");
            var data = new SerializedObject(popup);
            var slots = data.FindProperty("elementSlots");
            var icons = new System.Collections.Generic.Dictionary<MagicElement, Sprite>();
            for (int i = 0; i < slots.arraySize; i++)
            {
                var slot = slots.GetArrayElementAtIndex(i);
                var element = (MagicElement)slot.FindPropertyRelative("element").intValue;
                var b = (Button)slot.FindPropertyRelative("button").objectReferenceValue;
                b.name = "Element_" + element;
                var icon = (Image)slot.FindPropertyRelative("icon").objectReferenceValue;
                if (icon.sprite == null) icon.sprite = ElementIcon(element);
                icon.preserveAspect = true; icons[element] = icon.sprite;
            }
            var cards = data.FindProperty("cardSlots");
            for (int i = 0; i < cards.arraySize; i++) ((Button)cards.GetArrayElementAtIndex(i).FindPropertyRelative("selectButton").objectReferenceValue).name = "Specialization_" + i;
            ((Button)data.FindProperty("unlockCardSlot.confirmButton").objectReferenceValue).name = "UnlockElement";
            data.ApplyModifiedPropertiesWithoutUndo();
            var stat = ui.GetComponentInChildren<HudStatcUi>(true);
            Set(stat, "playerSkillSystem", systems.GetComponent<PlayerSkillSystem>());
            foreach (var obj in new Object[] { stat, result })
            {
                var s = new SerializedObject(obj); var a = s.FindProperty("elementIcons"); a.arraySize = 5;
                int index = 0;
                foreach (var e in MagicContentCatalog.PentagonElements)
                { var item = a.GetArrayElementAtIndex(index++); item.FindPropertyRelative("element").intValue = (int)e; item.FindPropertyRelative("icon").objectReferenceValue = icons[e]; }
                if (obj == result)
                {
                    var rs = s.FindProperty("elementSlots");
                    for (int i = 0; i < rs.arraySize; i++)
                    { var item = rs.GetArrayElementAtIndex(i); var text = item.FindPropertyRelative("levelText").objectReferenceValue as TMP_Text; if (text != null) item.FindPropertyRelative("icon").objectReferenceValue = text.transform.parent.GetComponent<Image>(); }
                }
                s.ApplyModifiedPropertiesWithoutUndo();
            }
            var dynamicHud = ui.GetComponentInChildren<HudDynamicUi>(true);
            foreach (string field in new[] { "timerText", "lvText", "killCount" })
                Get<TMP_Text>(dynamicHud, field).color = new Color32(245, 244, 230, 255);
            var killGroup = (RectTransform)Get<TMP_Text>(dynamicHud, "killCount").transform.parent;
            killGroup.anchoredPosition = new Vector2(-160, -114);
            Get<TMP_Text>(dynamicHud, "killCount").rectTransform.sizeDelta = new Vector2(140, 46.5f);
            var hud = new SerializedObject(dynamicHud); hud.FindProperty("gameTime").floatValue = RunTimelineRules.TimeLimit; hud.ApplyModifiedPropertiesWithoutUndo();
            systems.GetComponent<GameplayHudBinder>().Configure(dynamicHud, player.GetComponent<Health>(), systems.GetComponent<PlayerProgression>());
            var bossHud = ui.GetComponentInChildren<BossHudUi>(true);
            Set(bossHud, "panelRoot", Named(ui, "Boss_Timer&Hp").gameObject);
            Set(bossHud, "runDirector", systems.GetComponent<RunDirector>()); Set(bossHud, "bossSpawner", All<BossSpawner>(scene).Single());
            Named(ui, "Boss_Timer&Hp").gameObject.SetActive(false);
            foreach (var announcement in ui.GetComponentsInChildren<EliteAnnouncementUi>(true))
            {
                Set(announcement, "runDirector", systems.GetComponent<RunDirector>());
                var ad = new SerializedObject(announcement); var entries = ad.FindProperty("eliteEntries");
                if (!Enumerable.Range(0, entries.arraySize).Any(i => entries.GetArrayElementAtIndex(i).FindPropertyRelative("kind").intValue == (int)EliteKind.Summoner))
                { var entry = entries.GetArrayElementAtIndex(entries.arraySize++); entry.FindPropertyRelative("kind").intValue = (int)EliteKind.Summoner; entry.FindPropertyRelative("title").stringValue = "소환술사 등장!"; entry.FindPropertyRelative("icon").objectReferenceValue = null; }
                ad.ApplyModifiedPropertiesWithoutUndo();
                foreach (var graphic in announcement.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            }
            var uiManager = ui.GetComponentInChildren<UIManager>(true);
            Set(uiManager, "popupUi", popup); Set(uiManager, "resultUi", result);
            foreach (var option in ui.GetComponentsInChildren<OptionsUi>(true)) { var panel = Get<GameObject>(option, "panelRoot"); if (panel != null) panel.SetActive(false); RenameButton(option, "exitButton", "GameCloseOptions"); }
            var pool = ui.GetComponentInChildren<UiObjectPool>(true);
            var damage = Named(ui, "Canvas_Damage(World_Space)").GetComponent<Canvas>();
            damage.renderMode = RenderMode.ScreenSpaceOverlay;
            Set(pool, "uiParent", damage.transform);
            var ps = new SerializedObject(pool); var list = ps.FindProperty("objList"); list.arraySize = 1; list.GetArrayElementAtIndex(0).objectReferenceValue = Load<GameObject>("Assets/02.Prefabs/Presentation/DamageText.prefab"); ps.FindProperty("poolsize").intValue = 48; ps.ApplyModifiedPropertiesWithoutUndo();
            foreach (var feedback in All<AssetPresentationFeedback>(scene)) feedback.damageCanvas = damage;
            NormalizeUi(ui);
        }
        static Sprite ElementIcon(MagicElement e)
        {
            var visual = Load<GameObject>(IntegrationGameEditor.Root + "/Graphics/Visuals/" + e + "_Tier1.prefab");
            var animator = visual.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.runtimeAnimatorController != null)
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    {
                        if (binding.propertyName != "m_Sprite") continue;
                        var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                        if (frames.Length > 0 && frames[frames.Length / 2].value is Sprite sprite) return sprite;
                    }
            return visual.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.sprite != null).sprite;
        }
        static void NormalizeUi(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<CanvasScaler>(true))
            { c.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; c.referenceResolution = new Vector2(1920, 1080); c.matchWidthOrHeight = .5f; }
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true)) t.raycastTarget = false;
        }
        static void ConfigureEffects(Scene s)
        {
            var types = new[] { typeof(ElementReactionEffectPlayer), typeof(ChainReactionEffectPlayer), typeof(LightningChainEffectPlayer), typeof(SummonEffectPlayer), typeof(BossShockwaveEffectPlayer), typeof(GroundAreaEffectPlayer), typeof(DashTelegraphEffectPlayer) };
            foreach (var root in s.GetRootGameObjects().Where(r => r.name != "FinalTeamEffects" && types.Any(t => r.GetComponentInChildren(t, true) != null)).ToArray()) Object.DestroyImmediate(root);
            var effects = s.GetRootGameObjects().FirstOrDefault(r => r.name == "FinalTeamEffects");
            if (effects == null)
            {
                effects = Instantiate("Assets/02.Prefabs/UI/Effect_Manager/Effect_All_Manager.prefab", s);
                effects.name = "FinalTeamEffects";
                PrefabUtility.UnpackPrefabInstance(effects, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var b in effects.GetComponentsInChildren<MonoBehaviour>(true).Where(b => b.GetType().Name.EndsWith("DebugTrigger")).ToArray()) Object.DestroyImmediate(b);
            }
            var dash = effects.GetComponentInChildren<DashTelegraphEffectPlayer>(true);
            Set(dash, "dashLinePrefab", Load<GameObject>(Effects + "Dahser_Effect.prefab").GetComponent<LineRenderer>());
            // Reuse the delivered ground renderer for the delivered fire Lv8 event.
            var ground = effects.GetComponentInChildren<GroundAreaEffectPlayer>(true);
            var d = new SerializedObject(ground); var entries = d.FindProperty("entries");
            if (!Enumerable.Range(0, entries.arraySize).Any(i => entries.GetArrayElementAtIndex(i).FindPropertyRelative("element").intValue == (int)MagicElement.Fire))
            { var e = entries.GetArrayElementAtIndex(entries.arraySize++); e.FindPropertyRelative("element").intValue = (int)MagicElement.Fire; e.FindPropertyRelative("areaPrefab").objectReferenceValue = Load<GameObject>(Effects + "Ignite.prefab"); }
            d.ApplyModifiedPropertiesWithoutUndo();
            var chain = effects.GetComponentInChildren<ChainReactionEffectPlayer>(true);
            foreach (var element in new[] { MagicElement.Earth, MagicElement.Dark })
            {
                var cd = new SerializedObject(chain); var list = cd.FindProperty("entries");
                if (!Enumerable.Range(0, list.arraySize).Any(i => list.GetArrayElementAtIndex(i).FindPropertyRelative("element").intValue == (int)element))
                {
                    var entry = list.GetArrayElementAtIndex(list.arraySize++);
                    entry.FindPropertyRelative("element").intValue = (int)element;
                    entry.FindPropertyRelative("boltPrefab").objectReferenceValue = null;
                    entry.FindPropertyRelative("burstEffectPrefab").objectReferenceValue = Load<GameObject>(Effects + (element == MagicElement.Earth ? "Earth_Shork.prefab" : "Dark_Effect.prefab"));
                    entry.FindPropertyRelative("burstAnimationClip").objectReferenceValue = null;
                    entry.FindPropertyRelative("burstFallbackLifetime").floatValue = .6f;
                    entry.FindPropertyRelative("sfxType").intValue = (int)(element == MagicElement.Earth ? SFXType.Eart_Shork : SFXType.Dark_Increased_damage);
                }
                cd.ApplyModifiedPropertiesWithoutUndo();
            }
            var reaction = effects.GetComponentInChildren<ElementReactionEffectPlayer>(true);
            var rd = new SerializedObject(reaction); var re = rd.FindProperty("effects");
            if (!Enumerable.Range(0, re.arraySize).Any(i => re.GetArrayElementAtIndex(i).FindPropertyRelative("element").intValue == (int)MagicElement.Dark))
            {
                var entry = re.GetArrayElementAtIndex(re.arraySize++);
                entry.FindPropertyRelative("element").intValue = (int)MagicElement.Dark;
                entry.FindPropertyRelative("radiusMatch").intValue = 0;
                entry.FindPropertyRelative("effectPrefab").objectReferenceValue = Load<GameObject>(Effects + "Dark_Effect.prefab");
                entry.FindPropertyRelative("animationClip").objectReferenceValue = null;
                entry.FindPropertyRelative("sfxType").intValue = (int)SFXType.Dark_Increased_damage;
            }
            rd.ApplyModifiedPropertiesWithoutUndo();
        }
        static void ConfigureEnemies()
        {
            foreach (string role in new[] { "Basic", "Fast", "Tank", "Dasher", "Summoner", "Boss" })
            {
                var data = Load<EnemyData>("Assets/03.Data/Enemy/Enemy_" + role + ".asset");
                string path = AssetDatabase.GetAssetPath(data.Prefab);
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (prefab.GetComponent<AssetVisualLifecycle>() == null)
                    {
                        string modelRole = role == "Dasher" ? "Fast" : role == "Summoner" ? "Tank" : role;
                        var source = Load<GameObject>("Assets/02.Prefabs/Presentation/Enemy_" + modelRole + ".prefab");
                        var lifecycle = source.GetComponent<AssetVisualLifecycle>();
                        var visual = Get<Transform>(lifecycle, "visual").gameObject;
                        foreach (var renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
                        var copy = Object.Instantiate(visual, prefab.transform, false); copy.name = "TeamArt"; copy.SetActive(true);
                        var installed = prefab.AddComponent<AssetVisualLifecycle>();
                        EditorUtility.CopySerialized(lifecycle, installed); Set(installed, "visual", copy.transform);
                    }
                    var frozen = prefab.GetComponent<EnemyFrozenVisualController>() ?? prefab.AddComponent<EnemyFrozenVisualController>();
                    if (Get<GameObject>(frozen, "frozenOverlay") == null)
                    { var overlay = Object.Instantiate(Load<GameObject>(Effects + "Freeze.prefab"), prefab.transform, false); overlay.name = "FrozenStatus"; overlay.SetActive(false); Set(frozen, "frozenOverlay", overlay); }
                    var status = prefab.GetComponent<StatusEffectVisual>() ?? prefab.AddComponent<StatusEffectVisual>();
                    if (Get<GameObject>(status, "overlay") == null)
                    { var overlay = Object.Instantiate(Load<GameObject>(Effects + "Dark_Effect.prefab"), prefab.transform, false); overlay.name = "DarkStatus"; overlay.SetActive(false); Set(status, "overlay", overlay); }
                    var dark = prefab.GetComponent<EnemyDarkVisualController>() ?? prefab.AddComponent<EnemyDarkVisualController>(); Set(dark, "visual", status);
                    ConfigureMarkDisplay(prefab);
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
        }
        static void ConfigureMarkDisplay(GameObject prefab)
        {
            if (prefab.GetComponent<ElementMarkStackVisualController>() != null) return;
            var controller = prefab.AddComponent<ElementMarkStackVisualController>();
            var root = new GameObject("ElementStacks", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(prefab.transform, false);
            root.transform.localPosition = new Vector3(0, .85f, 0);
            root.transform.localScale = Vector3.one * .008f;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 4500;
            ((RectTransform)root.transform).sizeDelta = new Vector2(170, 28);
            var font = Load<GameObject>("Assets/02.Prefabs/Presentation/DamageText.prefab").GetComponentInChildren<TMP_Text>().font;
            var data = new SerializedObject(controller); var displays = data.FindProperty("displays"); displays.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                var element = MagicContentCatalog.PentagonElements[i];
                var slot = new GameObject(element.ToString(), typeof(RectTransform)); slot.transform.SetParent(root.transform, false);
                var rect = (RectTransform)slot.transform; rect.sizeDelta = new Vector2(34, 28); rect.anchoredPosition = new Vector2((i - 2) * 34, 0);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)); icon.transform.SetParent(slot.transform, false);
                var image = icon.GetComponent<Image>(); image.sprite = ElementIcon(element); image.preserveAspect = true; image.raycastTarget = false;
                image.rectTransform.sizeDelta = new Vector2(22, 22); image.rectTransform.anchoredPosition = new Vector2(-5, 0);
                var number = new GameObject("Stacks", typeof(RectTransform), typeof(TextMeshProUGUI)); number.transform.SetParent(slot.transform, false);
                var text = number.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = 18; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
                text.rectTransform.sizeDelta = new Vector2(15, 26); text.rectTransform.anchoredPosition = new Vector2(10, 0);
                var entry = displays.GetArrayElementAtIndex(i); entry.FindPropertyRelative("element").intValue = (int)element;
                entry.FindPropertyRelative("root").objectReferenceValue = slot; entry.FindPropertyRelative("stackText").objectReferenceValue = text;
                slot.SetActive(false);
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        static void ConfigureSound(SoundManager sound)
        {
            const string music = "Assets/Untracked Asset/cplomedia/Casual Games SFX and Music Pack/";
            if (sound.bgmClip == null || sound.bgmClip.Length != 2 || sound.bgmClip.Any(c => c == null))
                sound.bgmClip = new[] { Load<AudioClip>(music + "Music_Curious_Piano.wav"), Load<AudioClip>(music + "Music_Run_Piano.wav") };
            if (sound.soundClip == null || sound.soundClip.Length < 10)
                sound.soundClip = Load<GameObject>(UI).GetComponentInChildren<SoundManager>(true).soundClip;
            var bgm = Get<AudioSource>(sound, "bgmAudioSurce");
            if (bgm == null || sound.soundClip.Length < 10 || sound.soundClip.Take(10).Any(c => c == null)) throw new Exception("Required audio source/clip missing.");
            bgm.loop = true; bgm.playOnAwake = false; bgm.spatialBlend = 0;
            EditorUtility.SetDirty(sound);
        }
        public static void ConfigureTitle()
        {
            var s = EditorSceneManager.OpenScene(IntegrationGameEditor.Title);
            ConfigureSound(All<SoundManager>(s).Single());
            foreach (var root in s.GetRootGameObjects()) NormalizeUi(root);
            var controller = All<TitleSceneController>(s).Single();
            var start = Get<Button>(controller, "gameStartButton"); start.name = "START GAME";
            var options = All<OptionsUi>(s).Single();
            var os = new SerializedObject(options); os.FindProperty("enableEscToggle").boolValue = false; os.ApplyModifiedPropertiesWithoutUndo();
            var panel = Get<GameObject>(options, "panelRoot"); panel.SetActive(false);
            var close = Get<Button>(options, "exitButton"); close.name = "CloseOptions";
            foreach (var b in All<Button>(s))
            {
                if (b.name == "Option") { b.name = "OPEN OPTIONS"; if (b.onClick.GetPersistentEventCount() == 0) UnityEventTools.AddPersistentListener(b.onClick, options.Open); }
                if (b.name == "Exit" && !b.transform.IsChildOf(panel.transform))
                {
                    b.name = "QUIT";
                    var exit = controller.GetComponent<FinalSubmissionExit>() ?? controller.gameObject.AddComponent<FinalSubmissionExit>();
                    if (b.onClick.GetPersistentEventCount() == 0) UnityEventTools.AddPersistentListener(b.onClick, exit.Quit);
                }
            }
            EditorSceneManager.SaveScene(s);
        }
        public static void Validate(Scene s)
        {
            var missing = new System.Collections.Generic.List<string>();
            foreach (var t in All<Transform>(s))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0 || PrefabUtility.IsPrefabAssetMissing(t.gameObject)) throw new Exception("Missing component/prefab: " + t.name);
                foreach (var c in t.GetComponents<Component>().Where(c => c != null))
                {
                    var p = new SerializedObject(c).GetIterator();
                    while (p.NextVisible(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0) missing.Add(t.name + " / " + c.GetType().Name + "." + p.propertyPath);
                }
            }
            if (missing.Count > 0) throw new Exception("Missing references:\n" + string.Join("\n", missing));
            if (All<PopupUi>(s).Length != 1 || !All<PopupUi>(s)[0].IsConfigured || All<ResultUi>(s).Length != 1) throw new Exception("Team flow UI missing.");
            if (All<SoundManager>(s).Length != 1 || All<EventSystem>(s).Length != 1 || All<ChainReactionEffectPlayer>(s).Length != 1 || All<GroundAreaEffectPlayer>(s).Length != 1) throw new Exception("Duplicate/missing team manager.");
            if (All<MonoBehaviour>(s).Any(b => b != null && b.GetType().Name.EndsWith("DebugTrigger"))) throw new Exception("Debug trigger in final scene.");
            if (ElementReactionValues.ReactionUnlockLevel != 3) throw new Exception("Test reaction level remains enabled.");
            IntegrationGraphicsCheck.ValidateAssets();
            FinalFunctionalCoverage.Validate(s);
            string output = FinalSubmissionAudit.Output;
            var dependencies = AssetDatabase.GetDependencies(new[] { IntegrationGameEditor.Title, IntegrationGameEditor.Game }, true).OrderBy(p => p).ToArray();
            File.WriteAllLines(Path.Combine(output, "final-asset-dependencies.txt"), dependencies);
            var components = All<MonoBehaviour>(s).Where(c => c != null).Select(c => c.GetType().Name).ToList();
            foreach (var path in dependencies.Where(p => p.EndsWith(".prefab")))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) components.AddRange(prefab.GetComponentsInChildren<MonoBehaviour>(true).Where(c => c != null).Select(c => c.GetType().Name));
            }
            File.WriteAllLines(Path.Combine(output, "final-component-types.txt"), components.Distinct().OrderBy(c => c));
            Debug.Log("[Final Submission] Team UI, effects, encounters and references passed.");
        }
    }
}
