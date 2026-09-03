using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Unity 오브젝트에 의존하지 않는 한 판용 오각형 스킬트리 상태입니다.
/// 선택과 확정을 분리하며 저장·초기화·건너뛰기를 제공하지 않습니다.
/// </summary>
public interface IReadOnlyPlayerSkillTree
{
    bool HasStartingElement { get; }
    MagicElement StartingElement { get; }
    SkillTreeNodeId? PendingSelection { get; }
    IReadOnlyList<SkillTreeNodeId> OwnedNodes { get; }
    IReadOnlyList<MagicElement> OwnedElements { get; }
    IReadOnlyList<FusionKind> OwnedFusions { get; }
    IReadOnlyList<MagicId> OwnedMagics { get; }

    SkillTreeNodeState GetNodeState(SkillTreeNodeId id);
    bool HasNode(SkillTreeNodeId id);
    bool HasElement(MagicElement element);
    bool HasFusion(FusionKind fusion);
    bool HasMagic(MagicId magic);
}

public sealed class PlayerSkillTree : IReadOnlyPlayerSkillTree
{
    private readonly HashSet<SkillTreeNodeId> ownedNodeLookup = new HashSet<SkillTreeNodeId>();
    private readonly HashSet<MagicElement> ownedElementLookup = new HashSet<MagicElement>();
    private readonly HashSet<FusionKind> ownedFusionLookup = new HashSet<FusionKind>();
    private readonly HashSet<MagicId> ownedMagicLookup = new HashSet<MagicId>();

    private readonly List<SkillTreeNodeId> ownedNodes = new List<SkillTreeNodeId>(28);
    private readonly List<MagicElement> ownedElements = new List<MagicElement>(5);
    private readonly List<FusionKind> ownedFusions = new List<FusionKind>(5);
    private readonly List<MagicId> ownedMagics = new List<MagicId>(15);

    private readonly ReadOnlyCollection<SkillTreeNodeId> ownedNodesView;
    private readonly ReadOnlyCollection<MagicElement> ownedElementsView;
    private readonly ReadOnlyCollection<FusionKind> ownedFusionsView;
    private readonly ReadOnlyCollection<MagicId> ownedMagicsView;

    private MagicElement startingElement;

    public PlayerSkillTree()
    {
        ownedNodesView = ownedNodes.AsReadOnly();
        ownedElementsView = ownedElements.AsReadOnly();
        ownedFusionsView = ownedFusions.AsReadOnly();
        ownedMagicsView = ownedMagics.AsReadOnly();
    }

    public bool HasStartingElement { get; private set; }

    public MagicElement StartingElement
    {
        get
        {
            if (!HasStartingElement)
            {
                throw new InvalidOperationException("시작 원소가 아직 선택되지 않았습니다.");
            }

            return startingElement;
        }
    }

    public SkillTreeNodeId? PendingSelection { get; private set; }
    public IReadOnlyList<SkillTreeNodeId> OwnedNodes => ownedNodesView;
    public IReadOnlyList<MagicElement> OwnedElements => ownedElementsView;
    public IReadOnlyList<FusionKind> OwnedFusions => ownedFusionsView;
    public IReadOnlyList<MagicId> OwnedMagics => ownedMagicsView;

    public event Action TreeChanged;
    public event Action<SkillTreeNodeId> NodeOwned;
    public event Action<MagicElement> ElementUnlocked;
    public event Action<FusionKind> FusionUnlocked;
    public event Action<MagicId> MagicUnlocked;

    /// <summary>
    /// 시작 원소의 조준 마법을 포인트 소모 없이 즉시 획득합니다.
    /// </summary>
    public bool TryChooseStartingElement(MagicElement element)
    {
        if (HasStartingElement || !SkillTreeCatalog.IsElement(element))
        {
            return false;
        }

        HasStartingElement = true;
        startingElement = element;

        SkillTreeNodeId targetNode = SkillTreeCatalog.GetTargetNode(element);
        OwnNode(targetNode);
        TreeChanged?.Invoke();
        return true;
    }

    public SkillTreeNodeState GetNodeState(SkillTreeNodeId id)
    {
        SkillTreeNodeDefinition definition;
        if (!SkillTreeCatalog.TryGetNode(id, out definition))
        {
            return SkillTreeNodeState.Hidden;
        }

        if (ownedNodeLookup.Contains(id))
        {
            return SkillTreeNodeState.Owned;
        }

        switch (definition.Type)
        {
            case SkillTreeNodeType.ElementTargetMagic:
                return GetElementTargetState(definition);

            case SkillTreeNodeType.ElementAreaMagic:
            case SkillTreeNodeType.ElementMastery:
                return PrerequisitesOwned(definition)
                    ? SkillTreeNodeState.Available
                    : SkillTreeNodeState.Locked;

            case SkillTreeNodeType.FusionMagic:
            case SkillTreeNodeType.FusionMastery:
                return GetFusionNodeState(definition);

            case SkillTreeNodeType.CommonUpgrade:
                return HasStartingElement
                    ? SkillTreeNodeState.Available
                    : SkillTreeNodeState.Locked;

            default:
                return SkillTreeNodeState.Hidden;
        }
    }

    /// <summary>
    /// 현재 Available 노드 하나를 확정 대기 상태로 둡니다. 다른 Available 노드로 변경 가능합니다.
    /// </summary>
    public bool TrySelectNode(SkillTreeNodeId id)
    {
        if (!HasStartingElement || GetNodeState(id) != SkillTreeNodeState.Available)
        {
            return false;
        }

        if (PendingSelection.HasValue && PendingSelection.Value == id)
        {
            return true;
        }

        PendingSelection = id;
        TreeChanged?.Invoke();
        return true;
    }

    public bool Cancel()
    {
        if (!PendingSelection.HasValue)
        {
            return false;
        }

        PendingSelection = null;
        TreeChanged?.Invoke();
        return true;
    }

    public bool Confirm()
    {
        if (!PendingSelection.HasValue)
        {
            return false;
        }

        SkillTreeNodeId selectedNode = PendingSelection.Value;
        if (GetNodeState(selectedNode) != SkillTreeNodeState.Available)
        {
            PendingSelection = null;
            TreeChanged?.Invoke();
            return false;
        }

        PendingSelection = null;
        OwnNode(selectedNode);
        TreeChanged?.Invoke();
        return true;
    }

    public bool HasNode(SkillTreeNodeId id)
    {
        return ownedNodeLookup.Contains(id);
    }

    public bool HasElement(MagicElement element)
    {
        return ownedElementLookup.Contains(element);
    }

    public bool HasFusion(FusionKind fusion)
    {
        return ownedFusionLookup.Contains(fusion);
    }

    public bool HasMagic(MagicId magic)
    {
        return ownedMagicLookup.Contains(magic);
    }

    private SkillTreeNodeState GetElementTargetState(SkillTreeNodeDefinition definition)
    {
        if (!HasStartingElement || !definition.Element.HasValue)
        {
            return SkillTreeNodeState.Locked;
        }

        MagicElement candidate = definition.Element.Value;
        for (int i = 0; i < ownedElements.Count; i++)
        {
            if (SkillTreeCatalog.AreAdjacent(ownedElements[i], candidate))
            {
                return SkillTreeNodeState.Available;
            }
        }

        return SkillTreeNodeState.Locked;
    }

    private SkillTreeNodeState GetFusionNodeState(SkillTreeNodeDefinition definition)
    {
        if (!definition.Fusion.HasValue)
        {
            return SkillTreeNodeState.Hidden;
        }

        FusionDefinition fusion = SkillTreeCatalog.GetFusion(definition.Fusion.Value);
        bool branchVisible = HasElement(fusion.FirstElement) && HasElement(fusion.SecondElement);
        if (!branchVisible)
        {
            return SkillTreeNodeState.Hidden;
        }

        return PrerequisitesOwned(definition)
            ? SkillTreeNodeState.Available
            : SkillTreeNodeState.Locked;
    }

    private bool PrerequisitesOwned(SkillTreeNodeDefinition definition)
    {
        IReadOnlyList<SkillTreeNodeId> prerequisites = definition.Prerequisites;
        for (int i = 0; i < prerequisites.Count; i++)
        {
            if (!ownedNodeLookup.Contains(prerequisites[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void OwnNode(SkillTreeNodeId id)
    {
        if (!ownedNodeLookup.Add(id))
        {
            return;
        }

        SkillTreeNodeDefinition definition = SkillTreeCatalog.GetNode(id);
        ownedNodes.Add(id);

        MagicElement? unlockedElement = null;
        FusionKind? unlockedFusion = null;
        MagicId? unlockedMagic = null;

        if (definition.Type == SkillTreeNodeType.ElementTargetMagic && definition.Element.HasValue)
        {
            MagicElement element = definition.Element.Value;
            if (ownedElementLookup.Add(element))
            {
                ownedElements.Add(element);
                unlockedElement = element;
            }
        }

        if (definition.Type == SkillTreeNodeType.FusionMagic && definition.Fusion.HasValue)
        {
            FusionKind fusion = definition.Fusion.Value;
            if (ownedFusionLookup.Add(fusion))
            {
                ownedFusions.Add(fusion);
                unlockedFusion = fusion;
            }
        }

        if (definition.Magic.HasValue)
        {
            MagicId magic = definition.Magic.Value;
            if (ownedMagicLookup.Add(magic))
            {
                ownedMagics.Add(magic);
                unlockedMagic = magic;
            }
        }

        // 모든 읽기 상태를 먼저 갱신합니다. 어느 이벤트에서 조회해도 같은 스냅샷을 봅니다.
        NodeOwned?.Invoke(id);

        if (unlockedElement.HasValue)
        {
            ElementUnlocked?.Invoke(unlockedElement.Value);
        }

        if (unlockedFusion.HasValue)
        {
            FusionUnlocked?.Invoke(unlockedFusion.Value);
        }

        if (unlockedMagic.HasValue)
        {
            MagicUnlocked?.Invoke(unlockedMagic.Value);
        }
    }
}
