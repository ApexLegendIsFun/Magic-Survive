using System;
using System.Collections.Generic;

public interface IReadOnlyPlayerSkillTree
{
    bool HasStartingElement { get; }
    MagicElement StartingElement { get; }
    MagicElement? PendingSelection { get; }
    IReadOnlyList<MagicElement> OwnedElements { get; }
    bool IsMaxed { get; }
    int GetSkillLevel(MagicElement element);
    bool CanUpgrade(MagicElement element);
}

// 한 판의 원소 레벨 상태. Unity 객체와 독립적으로 검증한다.
public sealed class PlayerSkillTree : IReadOnlyPlayerSkillTree
{
    private readonly int[] levels = new int[5];
    private readonly List<MagicElement> ownedElements = new List<MagicElement>(5);
    public PlayerSkillTree() { OwnedElements = ownedElements.AsReadOnly(); }
    public bool HasStartingElement { get; private set; }
    public MagicElement StartingElement { get; private set; }
    public MagicElement? PendingSelection { get; private set; }
    public IReadOnlyList<MagicElement> OwnedElements { get; }
    public bool IsMaxed
    {
        get
        {
            foreach (int level in levels)
                if (level < MagicContentCatalog.MaxSkillLevel) return false;
            return true;
        }
    }
    public event Action TreeChanged;
    public event Action<MagicElement, int> SkillLevelChanged;
    public event Action<MagicElement> ElementUnlocked;

    public int GetSkillLevel(MagicElement element) =>
        MagicContentCatalog.IsElement(element) ? levels[(int)element] : 0;

    public bool CanUpgrade(MagicElement element)
    {
        if (!HasStartingElement || !MagicContentCatalog.IsElement(element) ||
            GetSkillLevel(element) >= MagicContentCatalog.MaxSkillLevel) return false;
        if (GetSkillLevel(element) > 0) return true;
        foreach (MagicElement owned in ownedElements)
            if (MagicContentCatalog.AreAdjacent(owned, element)) return true;
        return false;
    }

    public bool TryChooseStartingElement(MagicElement element)
    {
        if (HasStartingElement || !MagicContentCatalog.IsElement(element)) return false;
        HasStartingElement = true;
        StartingElement = element;
        Upgrade(element);
        return true;
    }

    public bool TrySelectSkill(MagicElement element)
    {
        if (!CanUpgrade(element)) return false;
        PendingSelection = element;
        TreeChanged?.Invoke();
        return true;
    }

    public bool Cancel()
    {
        if (!PendingSelection.HasValue) return false;
        PendingSelection = null;
        TreeChanged?.Invoke();
        return true;
    }

    public bool Confirm()
    {
        if (!PendingSelection.HasValue || !CanUpgrade(PendingSelection.Value)) return false;
        MagicElement element = PendingSelection.Value;
        PendingSelection = null;
        Upgrade(element);
        return true;
    }

    private void Upgrade(MagicElement element)
    {
        int level = ++levels[(int)element];
        if (level == 1) ownedElements.Add(element);
        SkillLevelChanged?.Invoke(element, level);
        if (level == 1) ElementUnlocked?.Invoke(element);
        TreeChanged?.Invoke();
    }
}
