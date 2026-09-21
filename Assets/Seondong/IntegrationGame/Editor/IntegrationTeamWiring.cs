using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Seondong.IntegrationGame
{
    // Scene-instance wiring only. Team-owned prefabs and scripts remain unchanged.
    public static class IntegrationTeamWiring
    {
        private const string Managers = "Assets/02.Prefabs/UI/Effect_Manager/";

        private static T[] All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static T Ensure<T>(Scene scene, string path) where T : Component
        {
            var existing = All<T>(scene);
            Require(existing.Length <= 1, "Duplicate " + typeof(T).Name);
            if (existing.Length == 1) return existing[0];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab != null, "Missing prefab: " + path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            return instance.GetComponentInChildren<T>(true);
        }

        public static void Configure(Scene scene)
        {
            Ensure<LightningChainEffectPlayer>(scene, Managers + "Staack_Sound_Effect_LightningManager.prefab");
            Ensure<SummonEffectPlayer>(scene, Managers + "Summon_Effect_Manager.prefab");
            Ensure<BossShockwaveEffectPlayer>(scene, Managers + "Boss_Effect_Manager.prefab");
            Ensure<GroundAreaEffectPlayer>(scene, Managers + "Ground_Effect_Manager.prefab");
            var dash = Ensure<DashTelegraphEffectPlayer>(scene, Managers + "Elite_Dasher_Manger.prefab");
            var dashData = new SerializedObject(dash);
            dashData.FindProperty("dashLinePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02.Prefabs/UI/Effect/Dahser_Effect.prefab")
                    .GetComponent<LineRenderer>();
            dashData.ApplyModifiedPropertiesWithoutUndo();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02.Prefabs/UI/GameplayUI.prefab");
            Require(source != null, "Missing team GameplayUI prefab.");
            var announcement = All<EliteAnnouncementUi>(scene).SingleOrDefault();
            if (announcement == null)
            {
                // Copy the delivered announcement subtree with all its internal references.
                // The integration HUD is intentionally unpacked and has no PopupUi.
                var template = source.GetComponentInChildren<EliteAnnouncementUi>(true);
                Require(template != null, "Team announcement missing.");
                var instance = UnityEngine.Object.Instantiate(template.gameObject);
                SceneManager.MoveGameObjectToScene(instance, scene);
                instance.transform.SetParent(All<HudDynamicUi>(scene).Single().transform.root, false);
                announcement = instance.GetComponent<EliteAnnouncementUi>();
            }
            var data = new SerializedObject(announcement);
            data.FindProperty("runDirector").objectReferenceValue = All<RunDirector>(scene).Single();
            var entries = data.FindProperty("eliteEntries");
            if (!Enumerable.Range(0, entries.arraySize).Any(i =>
                    entries.GetArrayElementAtIndex(i).FindPropertyRelative("kind").enumValueIndex == (int)EliteKind.Summoner))
            {
                int index = entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("kind").enumValueIndex = (int)EliteKind.Summoner;
                entry.FindPropertyRelative("title").stringValue = "소환술사 등장!";
                entry.FindPropertyRelative("icon").objectReferenceValue = null;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            foreach (var scaler in announcement.GetComponentsInChildren<CanvasScaler>(true))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
            }
            foreach (var graphic in announcement.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;

            // Preserve volume controls; refresh only the delivered clip-slot assignments.
            var templateSound = source.GetComponentInChildren<SoundManager>(true);
            Require(templateSound != null, "Team SoundManager missing.");
            Ensure<SoundManager>(scene, "Assets/02.Prefabs/Presentation/SoundPresentation.prefab");
            foreach (var sound in All<SoundManager>(scene))
            {
                var serialized = new SerializedObject(sound);
                var clips = serialized.FindProperty("soundClip");
                clips.arraySize = templateSound.soundClip.Length;
                for (int i = 0; i < clips.arraySize; i++)
                    clips.GetArrayElementAtIndex(i).objectReferenceValue = templateSound.soundClip[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void Validate(Scene scene)
        {
            foreach (var component in All<Transform>(scene))
            {
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(component.gameObject) == 0,
                    "Missing script: " + component.name);
                Require(!PrefabUtility.IsPrefabAssetMissing(component.gameObject), "Missing prefab: " + component.name);
            }
            Require(All<PopupUi>(scene).Length == 0, "Conflicting PopupUi.");
            Require(!All<MonoBehaviour>(scene).Any(b => b != null && b.GetType().Name.EndsWith("DebugTrigger")),
                "Test trigger must not be included in integration scene.");
            var elite = new SerializedObject(All<IntegrationEliteSpawner>(scene).Single());
            Require(AssetDatabase.GetAssetPath(elite.FindProperty("charger").objectReferenceValue) ==
                "Assets/03.Data/Enemy/Enemy_Dasher.asset", "Actual dasher data is not wired.");
            var boss = new SerializedObject(All<BossSpawner>(scene).Single());
            Require(AssetDatabase.GetAssetPath(boss.FindProperty("bossData").objectReferenceValue) ==
                "Assets/03.Data/Enemy/Enemy_Boss.asset", "Actual boss data is not wired.");
            References(All<DashTelegraphEffectPlayer>(scene).Single(), "dashLinePrefab");
            References(All<LightningChainEffectPlayer>(scene).Single(), "boltPrefab", "burstEffectPrefab");
            References(All<SummonEffectPlayer>(scene).Single(), "warningIndicatorPrefab", "summonBurstPrefab");
            References(All<BossShockwaveEffectPlayer>(scene).Single(), "telegraphRingPrefab", "shockwaveBurstPrefab");
            References(All<EliteAnnouncementUi>(scene).Single(), "runDirector", "panelRoot", "titleText");
            Require(All<GroundAreaEffectPlayer>(scene).Length == 1, "Expected one ground effect manager.");
            Require(All<GameplayHudBinder>(scene).Length == 1, "Expected one HUD binder.");
            Require(All<SoundManager>(scene).Length == 1, "Expected one SoundManager.");
            var sounds = All<SoundManager>(scene).Single().soundClip;
            Require(sounds.Length > (int)SFXType.Summon && sounds[(int)SFXType.Summon] != null,
                "Summon sound slot missing.");
            Debug.Log("[Seondong Integration] Actual encounters, effects and HUD wiring passed.");
        }

        private static void References(UnityEngine.Object component, params string[] fields)
        {
            var serialized = new SerializedObject(component);
            foreach (var field in fields)
                Require(serialized.FindProperty(field).objectReferenceValue != null,
                    component.GetType().Name + "." + field + " missing.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
