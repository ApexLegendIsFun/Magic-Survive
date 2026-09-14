public static class LevelUpSlotMapper
{
    public static int GetPreviewLevel(MagicElement element, IReadOnlyPlayerSkillTree tree) =>
        System.Math.Min(MagicContentCatalog.MaxSkillLevel, tree.GetSkillLevel(element) + 1);
    public static bool IsMaxed(MagicElement element, IReadOnlyPlayerSkillTree tree) =>
        tree.GetSkillLevel(element) >= MagicContentCatalog.MaxSkillLevel;
}
