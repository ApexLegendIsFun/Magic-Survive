public readonly struct SkillTreeNodePreview
{
    public SkillTreeNodePreview(
        SkillTreeNodeDefinition definition,
        SkillTreeNodeState state,
        bool isSelected,
        string currentValue,
        string appliedValue)
    {
        Definition = definition;
        State = state;
        IsSelected = isSelected;
        CurrentValue = currentValue;
        AppliedValue = appliedValue;
    }

    public SkillTreeNodeDefinition Definition { get; }
    public SkillTreeNodeState State { get; }
    public bool IsSelected { get; }
    public string CurrentValue { get; }
    public string AppliedValue { get; }
}
