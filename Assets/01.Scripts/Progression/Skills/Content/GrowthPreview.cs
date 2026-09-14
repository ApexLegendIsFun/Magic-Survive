using System.Collections.Generic;

public enum ElementGrowthState { Invalid, Locked, Unlock, Upgrade, Max }

public sealed class SpecializationCardPreview
{
    internal SpecializationCardPreview(SpecializationDefinition definition, int count, bool canSelect)
    { Definition = definition; SelectionCount = count; CanSelect = canSelect; }
    public SpecializationDefinition Definition { get; }
    public SpecializationId Id => Definition.Id;
    public string Name => Definition.Name;
    public int SelectionCount { get; }
    public float AmountPerSelection => Definition.Amount;
    public SpecializationUnit Unit => Definition.Unit;
    public float CurrentBonus => SelectionCount * AmountPerSelection;
    public float AfterSelectionBonus => CurrentBonus + AmountPerSelection;
    public bool CanSelect { get; }
    public SpecializationCombatStatus CombatStatus => Definition.CombatStatus;
    public string Description => Definition.Description;
    public string AccumulationDescription => $"선택 {SelectionCount}회 · 누적 +{CurrentBonus:0.##}{Definition.UnitText}";
    public string AfterSelectionDescription => CanSelect
        ? $"선택 후 +{AfterSelectionBonus:0.##}{Definition.UnitText}" : "선택 불가";
}

// Immutable display snapshot. Null stats mean unowned / no next level, never fabricated Lv.0/9 values.
public sealed class GrowthPreview
{
    internal GrowthPreview(MagicElement element, int level, ElementGrowthState state, bool canSelect,
        ElementSkillStats? currentStats, ElementSkillStats? nextStats,
        IReadOnlyList<SpecializationCardPreview> cards)
    {
        Element = element; CurrentLevel = level; State = state; CanSelect = canSelect;
        CurrentStats = currentStats; NextStats = nextStats; Cards = cards;
    }
    public MagicElement Element { get; }
    public int CurrentLevel { get; }
    public int? NextLevel => NextStats.HasValue ? CurrentLevel + 1 : (int?)null;
    public ElementGrowthState State { get; }
    public bool CanSelect { get; }
    public ElementSkillStats? CurrentStats { get; }
    public ElementSkillStats? NextStats { get; }
    public IReadOnlyList<SpecializationCardPreview> Cards { get; }
}
