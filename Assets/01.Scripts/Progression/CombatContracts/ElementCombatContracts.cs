using System;

public readonly struct ElementMarkSnapshot
{
    public ElementMarkSnapshot(MagicElement element, int stacks, float remainingDuration)
    {
        Element = element;
        Stacks = Math.Max(0, Math.Min(ElementMarkRules.MaximumStacks, stacks));
        RemainingDuration = Math.Max(0f, remainingDuration);
    }

    public MagicElement Element { get; }
    public int Stacks { get; }
    public float RemainingDuration { get; }
    public bool IsMasteredThreshold => Stacks == ElementMarkRules.MaximumStacks;
}

public readonly struct ElementMarkChange
{
    public ElementMarkChange(ElementMarkSnapshot previous, ElementMarkSnapshot current)
    {
        Previous = previous;
        Current = current;
    }

    public ElementMarkSnapshot Previous { get; }
    public ElementMarkSnapshot Current { get; }
    public bool TriggeredMastery => ElementMarkRules.ShouldTriggerMastery(
        Previous.Stacks,
        Current.Stacks);
}

public readonly struct FusionReactionOccurrence
{
    public FusionReactionOccurrence(FusionKind fusion, IElementMarkTarget target)
    {
        Fusion = fusion;
        Target = target;
    }

    public FusionKind Fusion { get; }
    public IElementMarkTarget Target { get; }
}

public interface IElementMarkTarget
{
    float CrowdControlDurationMultiplier { get; }
    bool IsKnockbackImmune { get; }

    event Action<ElementMarkChange> ElementMarkChanged;

    ElementMarkSnapshot GetElementMark(MagicElement element);
    void ApplyElementMark(MagicElement element, int amount, float duration);
    void ConsumeElementMarks(MagicElement element, int amount);
}

public static class ElementMarkRules
{
    public const int MaximumStacks = MagicContentCatalog.MaxMarkStacks;
    public const float Duration = MagicContentCatalog.MarkDurationSeconds;
    public const float BossCrowdControlMultiplier = 0.25f;

    public static bool ShouldTriggerMastery(int previousStacks, int currentStacks)
    {
        return previousStacks == 2 && currentStacks == MaximumStacks;
    }
}

public static class FusionReactionRules
{
    public static int CollectEligibleReactions(
        PlayerSkillTree tree,
        IElementMarkTarget target,
        FusionKind[] buffer)
    {
        if (tree == null || target == null || buffer == null)
        {
            return 0;
        }

        int count = 0;
        for (int index = 0; index < SkillTreeCatalog.Fusions.Count && count < buffer.Length; index++)
        {
            FusionDefinition fusion = SkillTreeCatalog.Fusions[index];
            if (!tree.HasFusion(fusion.Kind))
            {
                continue;
            }

            if (target.GetElementMark(fusion.FirstElement).Stacks <
                    MagicContentCatalog.FusionReactionRequiredStacksPerParent ||
                target.GetElementMark(fusion.SecondElement).Stacks <
                    MagicContentCatalog.FusionReactionRequiredStacksPerParent)
            {
                continue;
            }

            buffer[count++] = fusion.Kind;
        }

        return count;
    }
}

public static class ElementCombatEvents
{
    public static event Action<FusionReactionOccurrence> FusionReactionOccurred;

    public static void RaiseFusionReaction(FusionReactionOccurrence occurrence)
    {
        FusionReactionOccurred?.Invoke(occurrence);
    }

    public static void Clear()
    {
        FusionReactionOccurred = null;
    }
}
