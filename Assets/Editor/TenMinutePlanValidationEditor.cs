using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TenMinutePlanValidationEditor
{
    [MenuItem("Tools/Magic Survive/Validate 10 Minute Plan Rules")]
    public static void Run()
    {
        ValidateCatalogs();
        ValidatePentagonForEveryStart();
        ValidateFusionPath();
        ValidateSimultaneousFusionReactions();
        ValidateDifficulty();
        ValidateTimeline();
        ValidateProgressionRules();
        Debug.Log("[10 Minute Rules] PASS: catalogs, pentagon, fusion path, difficulty, progression.");
    }

    private static void ValidateCatalogs()
    {
        Require(SkillTreeCatalog.Nodes.Count == 28, "Skill tree must contain 28 nodes.");
        Require(SkillTreeCatalog.Fusions.Count == 5, "Skill tree must contain 5 fusions.");
        Require(MagicContentCatalog.AllBaseMagics.Count == 10,
            "Magic catalog must contain 10 base magics.");
        Require(MagicContentCatalog.AllFusions.Count == 5,
            "Magic catalog must contain 5 fusion magics.");
        Require(MagicContentCatalog.AllElementRules.Count == 5,
            "Magic catalog must contain 5 element rules.");
        Require(MagicContentCatalog.MaxMarkStacks == 3, "Marks must cap at 3.");
        RequireApproximately(MagicContentCatalog.MarkDurationSeconds, 5f, "Mark duration");

        MagicDefinition fireBolt = MagicContentCatalog.GetMagic(MagicId.FireBolt);
        RequireApproximately(fireBolt.Attack.Damage, 6f, "FireBolt damage");
        RequireApproximately(fireBolt.Attack.CooldownSeconds, 0.8f, "FireBolt cooldown");

        FusionContentDefinition plasma = MagicContentCatalog.GetFusion(FusionKind.Plasma);
        RequireApproximately(plasma.Reaction.Damage, 24f, "Plasma reaction damage");
        Require(plasma.Reaction.ChainTargetCount == 3, "Plasma chain target count must be 3.");
        Require(plasma.RequiredMarkStacksPerParent == 3 &&
                plasma.ConsumedMarkStacksPerParent == 3,
            "Fusion reaction must require and consume 3+3 marks.");
    }

    private static void ValidatePentagonForEveryStart()
    {
        MagicElement[] expectedOrder =
        {
            MagicElement.Fire,
            MagicElement.Lightning,
            MagicElement.Frost,
            MagicElement.Earth,
            MagicElement.Dark
        };

        Require(SkillTreeCatalog.PentagonElements.Count == expectedOrder.Length,
            "Pentagon must contain five elements.");

        for (int index = 0; index < expectedOrder.Length; index++)
        {
            MagicElement start = expectedOrder[index];
            Require(SkillTreeCatalog.PentagonElements[index] == start,
                "Pentagon order does not match the design.");

            PlayerSkillTree tree = new PlayerSkillTree();
            Require(tree.TryChooseStartingElement(start), $"Could not choose {start}.");
            Require(tree.OwnedElements.Count == 1 && tree.OwnedElements[0] == start,
                $"{start} must be the only free element.");

            for (int candidateIndex = 0; candidateIndex < expectedOrder.Length; candidateIndex++)
            {
                MagicElement candidate = expectedOrder[candidateIndex];
                if (candidate == start)
                {
                    continue;
                }

                SkillTreeNodeState state = tree.GetNodeState(
                    SkillTreeCatalog.GetTargetNode(candidate));
                SkillTreeNodeState expected = SkillTreeCatalog.AreAdjacent(start, candidate)
                    ? SkillTreeNodeState.Available
                    : SkillTreeNodeState.Locked;
                Require(state == expected,
                    $"{start} -> {candidate} adjacency state expected {expected}, got {state}.");
            }
        }
    }

    private static void ValidateFusionPath()
    {
        PlayerSkillTree tree = new PlayerSkillTree();
        Require(tree.TryChooseStartingElement(MagicElement.Fire), "Fire start failed.");
        Require(tree.GetNodeState(SkillTreeNodeId.PlasmaMagic) == SkillTreeNodeState.Hidden,
            "Plasma branch must start hidden.");

        Own(tree, SkillTreeNodeId.LightningTarget);
        Require(tree.GetNodeState(SkillTreeNodeId.PlasmaMagic) == SkillTreeNodeState.Locked,
            "Plasma branch must become visible but locked after two roots.");
        Own(tree, SkillTreeNodeId.FireArea);
        Own(tree, SkillTreeNodeId.FireMastery);
        Own(tree, SkillTreeNodeId.LightningArea);
        Own(tree, SkillTreeNodeId.LightningMastery);
        Require(tree.GetNodeState(SkillTreeNodeId.PlasmaMagic) == SkillTreeNodeState.Available,
            "Plasma must unlock after both masteries.");
        Own(tree, SkillTreeNodeId.PlasmaMagic);
        Require(tree.HasFusion(FusionKind.Plasma), "Plasma fusion ownership missing.");
        Require(tree.GetNodeState(SkillTreeNodeId.PlasmaMastery) == SkillTreeNodeState.Available,
            "Plasma mastery must follow plasma magic.");
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

    private static void ValidateSimultaneousFusionReactions()
    {
        PlayerSkillTree tree = new PlayerSkillTree();
        Require(tree.TryChooseStartingElement(MagicElement.Lightning), "Lightning start failed.");
        Own(tree, SkillTreeNodeId.LightningArea);
        Own(tree, SkillTreeNodeId.LightningMastery);

        Own(tree, SkillTreeNodeId.FireTarget);
        Own(tree, SkillTreeNodeId.FireArea);
        Own(tree, SkillTreeNodeId.FireMastery);
        Own(tree, SkillTreeNodeId.PlasmaMagic);

        Own(tree, SkillTreeNodeId.FrostTarget);
        Own(tree, SkillTreeNodeId.FrostArea);
        Own(tree, SkillTreeNodeId.FrostMastery);
        Own(tree, SkillTreeNodeId.StormMagic);

        FakeElementMarkTarget target = new FakeElementMarkTarget();
        target.ApplyElementMark(MagicElement.Fire, 3, 5f);
        target.ApplyElementMark(MagicElement.Lightning, 3, 5f);
        target.ApplyElementMark(MagicElement.Frost, 3, 5f);

        FusionKind[] buffer = new FusionKind[5];
        int count = FusionReactionRules.CollectEligibleReactions(tree, target, buffer);
        Require(count == 2, "Two simultaneously eligible fusion reactions must both be collected.");
        Require(Contains(buffer, count, FusionKind.Plasma), "Plasma reaction missing.");
        Require(Contains(buffer, count, FusionKind.Storm), "Storm reaction missing.");
    }

    private static void ValidateProgressionRules()
    {
        Require(PlayerProgression.GetRequiredExperience(1) == 5,
            "Level 1 requirement must be 5.");
        Require(PlayerProgression.GetRequiredExperience(13) == 65,
            "Level 13 requirement must be 65.");
        Require(ElementMarkRules.ShouldTriggerMastery(2, 3),
            "Mastery must trigger on 2 to 3 stacks.");
        Require(!ElementMarkRules.ShouldTriggerMastery(3, 3),
            "Mastery must not retrigger while staying at 3 stacks.");

        RunResult result = new RunResult(
            RunOutcome.Victory,
            480f,
            100,
            13,
            new[] { MagicElement.Fire, MagicElement.Lightning },
            new[] { FusionKind.Plasma });
        IList<MagicElement> elements = result.Elements as IList<MagicElement>;
        IList<FusionKind> fusions = result.Fusions as IList<FusionKind>;
        Require(elements != null && elements.IsReadOnly,
            "RunResult element snapshot must be read-only.");
        Require(fusions != null && fusions.IsReadOnly,
            "RunResult fusion snapshot must be read-only.");
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

    private static void Own(PlayerSkillTree tree, SkillTreeNodeId node)
    {
        Require(tree.TrySelectNode(node), $"Could not select {node}.");
        Require(tree.Confirm(), $"Could not confirm {node}.");
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

    private static bool Contains(FusionKind[] values, int count, FusionKind expected)
    {
        for (int index = 0; index < count; index++)
        {
            if (values[index] == expected)
            {
                return true;
            }
        }

        return false;
    }

    private sealed class FakeElementMarkTarget : IElementMarkTarget
    {
        private readonly Dictionary<MagicElement, ElementMarkSnapshot> marks =
            new Dictionary<MagicElement, ElementMarkSnapshot>();

        public float CrowdControlDurationMultiplier => 1f;
        public bool IsKnockbackImmune => false;
        public event Action<ElementMarkChange> ElementMarkChanged;

        public ElementMarkSnapshot GetElementMark(MagicElement element)
        {
            return marks.TryGetValue(element, out ElementMarkSnapshot mark)
                ? mark
                : new ElementMarkSnapshot(element, 0, 0f);
        }

        public void ApplyElementMark(MagicElement element, int amount, float duration)
        {
            ElementMarkSnapshot previous = GetElementMark(element);
            ElementMarkSnapshot current = new ElementMarkSnapshot(
                element,
                previous.Stacks + amount,
                duration);
            marks[element] = current;
            ElementMarkChanged?.Invoke(new ElementMarkChange(previous, current));
        }

        public void ConsumeElementMarks(MagicElement element, int amount)
        {
            ElementMarkSnapshot previous = GetElementMark(element);
            ElementMarkSnapshot current = new ElementMarkSnapshot(
                element,
                Math.Max(0, previous.Stacks - amount),
                previous.RemainingDuration);
            marks[element] = current;
            ElementMarkChanged?.Invoke(new ElementMarkChange(previous, current));
        }
    }
}
