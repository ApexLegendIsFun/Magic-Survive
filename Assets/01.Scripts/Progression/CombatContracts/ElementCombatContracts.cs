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
