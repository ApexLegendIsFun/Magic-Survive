using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Seondong.IntegrationGame
{
    // Verify the actual release bindings, including intentionally empty object fields.
    // A GUID dependency list alone cannot prove these connections.
    public static class FinalFunctionalCoverage
    {
        public static void Validate(Scene scene)
        {
            var rows = new List<string> { "owner,feature,target,result" };
            var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            T One<T>() where T : MonoBehaviour
            {
                var found = components.OfType<T>().ToArray();
                Require(found.Length == 1 && found[0].enabled, "Missing/disabled/duplicate " + typeof(T).Name);
                return found[0];
            }
            void References(Object obj, params string[] fields)
            {
                var data = new SerializedObject(obj);
                foreach (string field in fields)
                    Require(data.FindProperty(field)?.objectReferenceValue != null, obj.name + "." + field);
            }
            void Effect<T>(string array, MagicElement[] elements, params string[] visuals) where T : MonoBehaviour
            {
                var component = One<T>();
                Require(component.gameObject.activeInHierarchy, "Inactive " + typeof(T).Name);
                var entries = new SerializedObject(component).FindProperty(array);
                foreach (var element in elements)
                {
                    bool ready = Enumerable.Range(0, entries.arraySize).Any(i =>
                    {
                        var e = entries.GetArrayElementAtIndex(i);
                        return e.FindPropertyRelative("element").intValue == (int)element &&
                            visuals.Any(v => e.FindPropertyRelative(v).objectReferenceValue != null);
                    });
                    Require(ready, typeof(T).Name + ": " + element);
                    rows.Add("Seungbum," + element + " effect," + typeof(T).Name + ",PASS");
                }
            }
            References(One<PlayerSkillSystem>(), "weaponRunner", "gameFlowController", "levelUpController");
            var magic = new SerializedObject(One<PlayerSkillSystem>()).FindProperty("targetedMagicDefinitions");
            var definitions = Enumerable.Range(0, magic.arraySize).Select(i => magic.GetArrayElementAtIndex(i).objectReferenceValue as ProjectileMagicDefinition).ToArray();
            foreach (var e in MagicContentCatalog.PentagonElements)
            {
                var definition = definitions.Single(d => d != null && d.Element == e);
                Require(definition.ProjectilePrefab != null, "Projectile " + e);
                rows.Add("Yushin," + e + " combat and growth," + AssetDatabase.GetAssetPath(definition) + ",PASS");
            }
            References(One<SpawnDirector>(), "enemyManager", "gameFlowController", "runDirector", "spawnCamera");
            var normals = new SerializedObject(One<SpawnDirector>()).FindProperty("normalEnemies");
            // Delivered normal enemy data contains Basic/Fast/Tank only. The fourth
            // slot is a reserved ranged role; ranged combat is used by elites/boss.
            Require(normals.arraySize >= 3, "Normal enemy roles");
            for (int i = 0; i < 3; i++) Require(normals.GetArrayElementAtIndex(i).objectReferenceValue != null, "Normal enemy role " + i);
            References(One<IntegrationEliteSpawner>(), "enemyManager", "runDirector", "spawnCamera", "charger", "summoner");
            References(One<BossSpawner>(), "enemyManager", "runDirector", "spawnCamera", "bossData");
            foreach (string role in new[] { "Basic", "Fast", "Tank", "Dasher", "Summoner", "Boss" })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/03.Data/Enemy/Enemy_" + role + ".asset");
                Require(data != null && data.Prefab != null, role + " data/prefab");
                var prefab = data.Prefab;
                string[] types = role == "Boss" ? new[] { "EnemyRangedAttack", "EnemySummon", "BossPhaseController", "BossShockwave" } :
                    role == "Dasher" ? new[] { "EnemyDash" } : role == "Summoner" ? new[] { "EnemyRangedAttack", "EnemyKeepDistance", "EnemySummon" } : Array.Empty<string>();
                foreach (string type in types)
                {
                    bool phaseTwoOnly = role == "Boss" && (type == "EnemySummon" || type == "BossShockwave");
                    Require(prefab.GetComponent(type) is Behaviour b && (b.enabled || phaseTwoOnly), role + ": " + type);
                }
                if (role == "Boss" || role == "Summoner") References(prefab.GetComponent<EnemySummon>(), "summonData");
                References(prefab.GetComponent<EnemyFrozenVisualController>(), "frozenOverlay");
                References(prefab.GetComponent<EnemyDarkVisualController>(), "visual");
                References(prefab.GetComponent<StatusEffectVisual>(), "overlay");
                Require(prefab.GetComponent<ElementMarkStackVisualController>() != null, role + " marks");
                if (role == "Boss") Require(data.IsBoss, "Boss immunity flag");
                rows.Add("Yushin and Seungbum," + role + " combat and status," + AssetDatabase.GetAssetPath(prefab) + ",PASS");
            }
            Require(AssetDatabase.GetAssetPath(new SerializedObject(One<IntegrationEliteSpawner>()).FindProperty("charger").objectReferenceValue) == "Assets/03.Data/Enemy/Enemy_Dasher.asset", "Charger source");
            Require(AssetDatabase.GetAssetPath(new SerializedObject(One<BossSpawner>()).FindProperty("bossData").objectReferenceValue) == "Assets/03.Data/Enemy/Enemy_Boss.asset", "Boss source");
            References(One<PopupUi>(), "levelUpController", "startButton", "gainButton");
            References(One<ResultUi>(), "gameFlowController", "title", "reStart");
            References(One<BossHudUi>(), "runDirector", "bossSpawner", "panelRoot", "bossNameText", "bossTimeText", "bossHpBar");
            Require(One<BossHudUi>().gameObject.activeInHierarchy, "Boss listener inactive");
            References(One<HudDynamicUi>(), "timerText", "lvText", "hpBar", "levelupBar", "killCount");
            References(One<HudStatcUi>(), "playerSkillSystem");
            References(One<UIManager>(), "popupUi", "resultUi");
            References(One<EliteAnnouncementUi>(), "runDirector");
            Effect<ElementReactionEffectPlayer>("effects", MagicContentCatalog.PentagonElements.ToArray(), "effectPrefab");
            Effect<ChainReactionEffectPlayer>("entries", new[] { MagicElement.Fire, MagicElement.Lightning, MagicElement.Earth, MagicElement.Dark }, "boltPrefab", "burstEffectPrefab");
            Effect<GroundAreaEffectPlayer>("entries", new[] { MagicElement.Fire, MagicElement.Frost, MagicElement.Earth }, "areaPrefab");
            References(One<DashTelegraphEffectPlayer>(), "dashLinePrefab");
            References(One<SummonEffectPlayer>(), "warningIndicatorPrefab", "summonBurstPrefab");
            References(One<BossShockwaveEffectPlayer>(), "telegraphRingPrefab", "shockwaveBurstPrefab");
            var sound = One<SoundManager>();
            References(sound, "bgmAudioSurce", "sfxAudioSource", "sfxUiAudioSource");
            foreach (SFXType sfx in Enum.GetValues(typeof(SFXType))) Require(sound.soundClip.Length > (int)sfx && sound.soundClip[(int)sfx] != null, "SFX " + sfx);
            Require(sound.bgmClip.Length >= 2 && sound.bgmClip.All(c => c != null), "BGM");
            rows.Add("Seungbum,selection result HUD settings audio and encounter effects,FinalTeamUI and FinalTeamEffects,PASS");
            File.WriteAllLines(Path.Combine(FinalSubmissionAudit.Output, "functional-bindings.csv"), rows);
            Debug.Log("[Final Functional Coverage] Required combat/UI/effect/audio bindings passed.");
        }
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Functional coverage: " + message); }
    }
}
