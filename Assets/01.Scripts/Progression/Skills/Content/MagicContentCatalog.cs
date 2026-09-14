using System;
using System.Collections.Generic;

public static class MagicContentCatalog
{
    public const int MaxSkillLevel = 8;
    public const int MaxMarkStacks = 3;
    public const float MarkDurationSeconds = 5f;
    public static readonly IReadOnlyList<MagicElement> PentagonElements = Array.AsReadOnly(new[]
    { MagicElement.Fire, MagicElement.Lightning, MagicElement.Frost, MagicElement.Earth, MagicElement.Dark });

    public static bool IsElement(MagicElement element) => (int)element >= 0 && (int)element < 5;
    public static bool AreAdjacent(MagicElement a, MagicElement b)
    {
        for (int i = 0; i < PentagonElements.Count; i++)
            if (PentagonElements[i] == a)
                return PentagonElements[(i + 1) % 5] == b || PentagonElements[(i + 4) % 5] == b;
        return false;
    }
    public static MagicId GetMagicId(MagicElement element)
    {
        switch (element)
        {
            case MagicElement.Fire: return MagicId.FireBolt;
            case MagicElement.Lightning: return MagicId.ChainLightning;
            case MagicElement.Frost: return MagicId.IceSpear;
            case MagicElement.Earth: return MagicId.RockSpear;
            case MagicElement.Dark: return MagicId.ShadowOrb;
            default: throw new ArgumentOutOfRangeException(nameof(element));
        }
    }
    public static string GetDisplayName(MagicElement element)
    {
        switch (element)
        {
            case MagicElement.Fire: return "화염탄";
            case MagicElement.Lightning: return "번개탄";
            case MagicElement.Frost: return "얼음창";
            case MagicElement.Earth: return "바위창";
            case MagicElement.Dark: return "그림자 구체";
            default: throw new ArgumentOutOfRangeException(nameof(element));
        }
    }
    public static ElementSkillStats GetStats(MagicElement element, int level)
    {
        if (!IsElement(element)) throw new ArgumentOutOfRangeException(nameof(element));
        if (level < 1 || level > MaxSkillLevel) throw new ArgumentOutOfRangeException(nameof(level));
        float damage = element == MagicElement.Earth ? 8f :
            element == MagicElement.Fire || element == MagicElement.Dark ? 6f : 5f;
        float cooldown;
        switch (element)
        {
            case MagicElement.Fire: cooldown = 0.8f; break;
            case MagicElement.Lightning: cooldown = 0.7f; break;
            case MagicElement.Frost: cooldown = 0.9f; break;
            case MagicElement.Earth: cooldown = 1.1f; break;
            default: cooldown = 1f; break;
        }
        int pierce = element == MagicElement.Frost || element == MagicElement.Dark ? 1 : 0;
        if (level >= 2) damage *= 1.15f;
        if (level >= 4) cooldown *= 0.9f;
        if (level >= 6 && element != MagicElement.Lightning) pierce++;
        return new ElementSkillStats(damage, cooldown, pierce);
    }
    public static string GetLevelDescription(MagicElement element, int level)
    {
        ElementSkillStats stats = GetStats(element, level);
        return $"Lv.{level} / {MaxSkillLevel}\n피해 {stats.Damage:0.##} · 쿨다운 {stats.Cooldown:0.##}초 · 관통 {stats.PierceCount}";
    }
}
