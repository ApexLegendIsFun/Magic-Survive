using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Seondong.IntegrationGame
{
    // Controlled integration fixture. Real WeaponRunner/Projectile combat is used,
    // but XP and durable target setup are artificial; this is NOT a natural run.
    [InitializeOnLoad]
    public static class FinalCombatValidation
    {
        const string Key = "FinalSubmission.CombatFixture";
        [Serializable] class Result
        {
            public string element;
            public int skillLevel;
            public List<string> events = new List<string>();
            public bool damaged, frozenVisual, darkVisual, execution, poolReset, markVisual;
        }
        [Serializable] class Report
        {
            public string status, method = "Controlled Play Mode targets and growth setup; real automatic attacks; not natural completion";
            public List<Result> elements = new List<Result>();
            public bool bossImmunities;
            public string failure;
        }
        static Report report;
        static Result current;
        static PlayerSkillSystem previous;
        static EnemyManager manager;
        static EnemyData data;
        static Enemy executionTarget;
        static readonly List<Enemy> targets = new List<Enemy>();
        static double started, deadline;
        static int index;
        static bool ready;
        static readonly MagicElement[] elements = { MagicElement.Fire, MagicElement.Lightning, MagicElement.Frost, MagicElement.Earth, MagicElement.Dark };
        static FinalCombatValidation() { EditorApplication.update += Tick; }
        public static void Run()
        {
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene(IntegrationGameEditor.Game);
            EditorSceneManager.playModeStartScene = null;
            EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (report == null)
                {
                    report = new Report { status = "RUNNING" }; deadline = EditorApplication.timeSinceStartup + 220;
                    GameEvents.ElementReactionTriggered += Reaction; GameEvents.ChainReactionTriggered += Chain; GameEvents.GroundAreaCreated += Ground;
                    Application.logMessageReceived += OnLog;
                    return;
                }
                Require(EditorApplication.timeSinceStartup < deadline, "Combat fixture timeout.");
                var skills = UnityEngine.Object.FindFirstObjectByType<PlayerSkillSystem>();
                if (skills == null || skills == previous) return;
                if (!ready)
                {
                    var progression = UnityEngine.Object.FindFirstObjectByType<PlayerProgression>();
                    var controller = UnityEngine.Object.FindFirstObjectByType<LevelUpController>();
                    Require(controller.TryChooseStartingElement(elements[index]), "Starting selection failed.");
                    var cards = SpecializationCatalog.All.Where(c => c.Element == elements[index]).ToArray();
                    for (int i = 0; i < 7; i++)
                    {
                        progression.AddExperience(progression.RequiredExperience - progression.CurrentExperience);
                        Require(controller.TryConfirmSpecialization(elements[index], cards[i % 3].Id), "Specialization setup failed.");
                    }
                    progression.SetExperienceEnabled(false);
                    UnityEngine.Object.FindFirstObjectByType<SpawnDirector>().enabled = false;
                    var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                    player.GetComponent<Health>().SetMaxHealth(100000, true);
                    manager = UnityEngine.Object.FindFirstObjectByType<EnemyManager>(); manager.DespawnAll();
                    data = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/03.Data/Enemy/Enemy_Basic.asset"));
                    var settings = new SerializedObject(data);
                    settings.FindProperty("maxHealth").floatValue = 5000;
                    settings.FindProperty("moveSpeed").floatValue = 0;
                    settings.FindProperty("contactDamage").floatValue = 0;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    targets.Clear();
                    for (int i = 0; i < 6; i++) targets.Add(manager.Spawn(data, (Vector2)player.transform.position + new Vector2(2.5f + .45f * (i % 3), .45f * (i / 3))));
                    executionTarget = null;
                    if (elements[index] == MagicElement.Dark)
                    {
                        executionTarget = targets[0];
                        executionTarget.ApplyElementMark(MagicElement.Dark, 3, 60);
                        executionTarget.TakeDamage(2600); // marks amplify damage; keep above zero.
                        var health = executionTarget.GetComponent<Health>();
                        // Establish a low-health target before the first real projectile.
                        while (health.CurrentHealth > health.MaxHealth * .09f) executionTarget.TakeDamage(10);
                    }
                    current = new Result { element = elements[index].ToString(), skillLevel = skills.GetSkillLevel(elements[index]) };
                    report.elements.Add(current); started = EditorApplication.timeSinceStartup; ready = true;
                    return;
                }
                foreach (var enemy in targets.Where(e => e != null && e.IsAlive))
                {
                    var health = enemy.GetComponent<Health>();
                    if (enemy != executionTarget && health.CurrentHealth < health.MaxHealth) current.damaged = true;
                    var mark = enemy.GetComponent<ElementMarkStackVisualController>();
                    Require(mark != null, "Mark display controller missing.");
                    var displays = new SerializedObject(mark).FindProperty("displays");
                    for (int i = 0; i < displays.arraySize; i++)
                    {
                        var entry = displays.GetArrayElementAtIndex(i);
                        int stacks = enemy.GetElementMark((MagicElement)entry.FindPropertyRelative("element").intValue).Stacks;
                        var root = entry.FindPropertyRelative("root").objectReferenceValue as GameObject;
                        var text = entry.FindPropertyRelative("stackText").objectReferenceValue as TMPro.TMP_Text;
                        Require(root != null && text != null && root.activeSelf == (stacks > 0), "Mark visibility mismatch.");
                        if (stacks > 0) { Require(text.text == stacks.ToString(), "Mark stack text mismatch."); current.markVisual = true; }
                    }
                    if (enemy.IsFrozen)
                    {
                        var controller = enemy.GetComponent<EnemyFrozenVisualController>();
                        var overlay = new SerializedObject(controller).FindProperty("frozenOverlay").objectReferenceValue as GameObject;
                        current.frozenVisual |= overlay != null && overlay.activeInHierarchy;
                    }
                    if (enemy.IsDarkAmplified)
                    {
                        var visual = enemy.GetComponent<StatusEffectVisual>();
                        var overlay = new SerializedObject(visual).FindProperty("overlay").objectReferenceValue as GameObject;
                        current.darkVisual |= overlay != null && overlay.activeInHierarchy;
                    }
                }
                if (executionTarget != null && !executionTarget.IsAlive) current.execution = true;
                if (EditorApplication.timeSinceStartup - started < 25) return;
                Require(current.skillLevel == 8 && current.damaged && current.markVisual, current.element + " actual projectile damage/mark display not observed.");
                switch (elements[index])
                {
                    case MagicElement.Fire: Require(current.events.Contains("Reaction") && current.events.Contains("Ground") && current.events.Contains("Chain"), "Fire reaction/ground/spread missing."); break;
                    case MagicElement.Lightning: Require(current.events.Contains("Reaction") && current.events.Contains("Chain"), "Lightning discharge/storm/chain missing."); break;
                    case MagicElement.Frost: Require(current.frozenVisual && current.events.Contains("Ground") && current.events.Contains("Reaction:Area"), "Frost freeze visual/shatter/ground missing."); break;
                    case MagicElement.Earth: Require(current.events.Contains("Reaction") && current.events.Contains("Ground") && current.events.Contains("Chain"), "Earth shock/ground/rockfall missing."); break;
                    case MagicElement.Dark: Require(current.darkVisual && current.execution && current.events.Contains("Chain"), "Dark visual/execution/spread missing."); break;
                }
                manager.DespawnAll();
                var reused = manager.Spawn(data, Vector2.zero);
                current.poolReset = !reused.IsFrozen && !reused.IsDarkAmplified && !reused.HasAnyElementMark;
                Require(current.poolReset, "Pooled status was retained.");
                Write();
                index++;
                if (index == elements.Length)
                {
                    manager.DespawnAll();
                    var boss = manager.Spawn(AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/03.Data/Enemy/Enemy_Boss.asset"), new Vector2(20, 0));
                    boss.ApplyFreeze(2, false, false); boss.ArmDarkExecute(); boss.TakeDamage(1900);
                    report.bossImmunities = boss.IsBoss && !boss.IsFrozen && boss.IsKnockbackImmune && boss.IsAlive;
                    Require(report.bossImmunities, "Boss control/execution immunity failed.");
                    Finish(true, null); return;
                }
                previous = skills; ready = false; SceneManager.LoadScene(IntegrationGameEditor.Game);
            }
            catch (Exception ex) { Finish(false, ex.ToString()); }
        }
        static void Event(MagicElement element, string value)
        { if (current != null && element == elements[index] && !current.events.Contains(value)) current.events.Add(value); }
        static void Reaction(MagicElement e, Vector2 p, float r) { Event(e, "Reaction"); if (r > 0) Event(e, "Reaction:Area"); }
        static void Chain(MagicElement e, Vector2 p, IReadOnlyList<Vector2> targets) => Event(e, "Chain");
        static void Ground(MagicElement e, Vector2 p, float r, float duration) => Event(e, "Ground");
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void OnLog(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, message + "\n" + stack); }
        static void Write() => File.WriteAllText(Path.Combine(FinalSubmissionAudit.Output, "combat-fixture.json"), JsonUtility.ToJson(report, true));
        static void Finish(bool pass, string failure)
        {
            if (!SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false); Application.logMessageReceived -= OnLog;
            GameEvents.ElementReactionTriggered -= Reaction; GameEvents.ChainReactionTriggered -= Chain; GameEvents.GroundAreaCreated -= Ground;
            report.status = pass ? "PASS" : "FAIL"; report.failure = failure; Write();
            Debug.Log("[Final Combat Fixture] " + report.status + " " + failure);
            EditorApplication.Exit(pass ? 0 : 1);
        }
    }
}
