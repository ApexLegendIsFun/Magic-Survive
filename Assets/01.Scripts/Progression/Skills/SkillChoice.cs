/// <summary>
/// 레벨업 때 적용할 수 있는 런타임 강화 종류입니다.
/// </summary>
public enum SkillUpgradeKind
{
    Damage = 0,
    FireRate = 1,
    Pierce = 2
}

/// <summary>
/// UI가 표시하고 PlayerSkillSystem이 적용하는 고정 선택지입니다.
/// </summary>
public readonly struct SkillChoice
{
    public SkillChoice(SkillUpgradeKind kind, string title, string description)
    {
        Kind = kind;
        Title = title;
        Description = description;
    }

    public SkillUpgradeKind Kind { get; }
    public string Title { get; }
    public string DisplayName => Title;
    public string Description { get; }
}
