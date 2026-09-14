using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TenMinutePlanValidationEditor
{
    [MenuItem("Tools/Magic Survive/Validate Element Level Rules")]
    public static void Run()
    {
        ValidateGrowth();
        ValidateSerializedReferences();
        ValidateMarksAndResult();
        ValidateDifficulty();
        ValidateTimeline();
        Debug.Log("[Element Level Rules] PASS: five starts, adjacency, levels, caps, marks, result, timeline.");
    }
    private static void ValidateSerializedReferences()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/02.Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                    $"Missing script: {path}/{child.name}");
        }
        foreach (string path in new[] { "Assets/00.Scenes/SampleScene.unity", "Assets/01.Scripts/UI/TitleScene.unity" })
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,
                UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                            $"Missing script: {path}/{child.name}");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
        }
        Debug.Log("[Element Assets] PASS: main/title scenes and gameplay prefabs have no Missing Script.");
    }

    private static void ValidateGrowth()
    {
        var order = MagicContentCatalog.PentagonElements;
        Require(order.Count == 5, "Exactly five elements required.");
        foreach (MagicElement start in order)
        {
            var tree = new PlayerSkillTree();
            int changes = 0;
            tree.SkillLevelChanged += (element, level) => changes++;
            Require(!tree.TrySelectSkill(start), "Cannot upgrade before starting.");
            Require(!tree.TryChooseStartingElement((MagicElement)99), "Invalid element accepted.");
            Require(tree.TryChooseStartingElement(start), "Start failed.");
            Require(tree.GetSkillLevel(start) == 1 && changes == 1, "Start must grant one level.");
            Require(!tree.TryChooseStartingElement(start), "Start cannot repeat.");
            foreach (MagicElement candidate in order)
            {
                bool expected = candidate == start || MagicContentCatalog.AreAdjacent(start, candidate);
                Require(tree.CanUpgrade(candidate) == expected, "Adjacency mismatch.");
            }
            Require(tree.TrySelectSkill(start) && tree.Cancel(), "Cancel selection failed.");
            Require(!tree.Confirm() && tree.GetSkillLevel(start) == 1, "Cancel spent a level.");
            Require(tree.TrySelectSkill(start) && tree.Confirm(), "Upgrade failed.");
            Require(tree.GetSkillLevel(start) == 2 && changes == 2, "Upgrade must increment once.");
            Require(!tree.Confirm(), "Double confirm must not spend twice.");
            // Follow the ring from every starting element, then cap each skill.
            while (!tree.IsMaxed)
            {
                bool advanced = false;
                foreach (MagicElement element in order)
                {
                    if (!tree.CanUpgrade(element)) continue;
                    Require(tree.TrySelectSkill(element) && tree.Confirm(), "Growth failed.");
                    advanced = true;
                }
                Require(advanced, "Growth deadlocked.");
            }
            Require(changes == 40, "All skills need exactly 40 levels including the free start.");
            foreach (MagicElement element in order)
                Require(tree.GetSkillLevel(element) == 8 && !tree.TrySelectSkill(element), "Max level failed.");
            Require(!new PlayerSkillTree().HasStartingElement, "Fresh run leaked skill state.");
        }
        var fire2 = MagicContentCatalog.GetStats(MagicElement.Fire, 2);
        RequireApproximately(fire2.Damage, 6.9f, "Level 2 damage");
        RequireApproximately(MagicContentCatalog.GetStats(MagicElement.Fire, 4).Cooldown, 0.72f, "Level 4 cooldown");
        Require(MagicContentCatalog.GetStats(MagicElement.Frost, 6).PierceCount == 2, "Frost level 6 pierce");
        RequireApproximately(MagicContentCatalog.GetStats(MagicElement.Lightning, 1).Damage, 5f, "Lightning base damage");
    }
    private static void ValidateMarksAndResult()
    {
        var marks = new ElementMarkState();
        marks.Apply(MagicElement.Fire, 4, 5f);
        marks.Apply(MagicElement.Frost, 1, 5f);
        Require(marks.Get(MagicElement.Fire).Stacks == 3, "Mark cap failed.");
        marks.Tick(4f);
        marks.Apply(MagicElement.Fire, 1, 5f);
        marks.Tick(1f);
        Require(marks.Get(MagicElement.Fire).Stacks == 3 && marks.Get(MagicElement.Frost).Stacks == 0,
            "Independent mark expiry/refresh failed.");
        marks.Reset();
        Require(marks.TotalStacks == 0, "Pool mark reset failed.");
        Require(PlayerProgression.GetRequiredExperience(1) == 5, "Initial EXP requirement changed.");
        var source = new List<MagicElement> { MagicElement.Fire };
        var result = new RunResult(RunOutcome.Victory, 480f, 100, 13, source);
        source.Clear();
        Require(result.Elements.Count == 1 && ((IList<MagicElement>)result.Elements).IsReadOnly,
            "Result must own an immutable snapshot.");
    }
    private static void ValidateDifficulty()
    {
        DifficultySnapshot start = DifficultyRules.Evaluate(0f);
        DifficultySnapshot end = DifficultyRules.Evaluate(480f);
        RequireApproximately(start.SpawnInterval, 1.4f, "Start spawn interval");
        Require(start.EnemyCap == 25, "Start enemy cap must be 25.");
        RequireApproximately(start.HealthMultiplier, 1f, "Start HP multiplier");
        RequireApproximately(start.DamageMultiplier, 1f, "Start damage multiplier");
        RequireApproximately(end.SpawnInterval, 0.35f, "End spawn interval");
        Require(end.EnemyCap == 100, "End enemy cap must be 100.");
        RequireApproximately(end.HealthMultiplier, 1.6f, "End HP multiplier");
        RequireApproximately(end.DamageMultiplier, 1.3f, "End damage multiplier");

        RequireApproximately(
            DifficultyRules.GetNormalizedSpawnWeight(NormalEnemyRole.Fast, 90f),
            0f,
            "Fast weight at unlock");
        RequireApproximately(
            DifficultyRules.GetNormalizedSpawnWeight(NormalEnemyRole.Basic, 480f),
            0.35f,
            "Final basic weight");
        RequireApproximately(
            DifficultyRules.GetNormalizedSpawnWeight(NormalEnemyRole.Ranged, 480f),
            0.15f,
            "Final ranged weight");
    }

    private static void ValidateTimeline()
    {
        Require(!RunTimelineRules.Reached(179.99f, RunTimelineRules.FirstEliteTime),
            "First elite must not appear before 3 minutes.");
        Require(RunTimelineRules.Reached(180f, RunTimelineRules.FirstEliteTime),
            "First elite must appear at 3 minutes.");
        Require(RunTimelineRules.Reached(360f, RunTimelineRules.SecondEliteTime),
            "Second elite must appear at 6 minutes.");
        Require(RunTimelineRules.Reached(480f, RunTimelineRules.BossTime),
            "Boss phase must begin at 8 minutes.");
        Require(RunTimelineRules.Reached(600f, RunTimelineRules.TimeLimit),
            "Timeout must occur at 10 minutes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void RequireApproximately(float actual, float expected, string label)
    {
        if (!Mathf.Approximately(actual, expected))
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}.");
        }
    }

}
