using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// UI와 성장 규칙이 함께 조회하는 불변 노드 명세입니다.
/// </summary>
public sealed class SkillTreeNodeDefinition
{
    private readonly ReadOnlyCollection<SkillTreeNodeId> prerequisites;

    internal SkillTreeNodeDefinition(
        SkillTreeNodeId id,
        SkillTreeNodeType type,
        string displayName,
        string description,
        MagicElement? element,
        FusionKind? fusion,
        MagicId? magic,
        CommonUpgradeKind? commonUpgrade,
        params SkillTreeNodeId[] prerequisites)
    {
        Id = id;
        Type = type;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Description = description ?? string.Empty;
        Element = element;
        Fusion = fusion;
        Magic = magic;
        CommonUpgrade = commonUpgrade;

        SkillTreeNodeId[] prerequisiteCopy = prerequisites == null
            ? Array.Empty<SkillTreeNodeId>()
            : (SkillTreeNodeId[])prerequisites.Clone();
        this.prerequisites = Array.AsReadOnly(prerequisiteCopy);
    }

    public SkillTreeNodeId Id { get; }
    public SkillTreeNodeType Type { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public MagicElement? Element { get; }
    public FusionKind? Fusion { get; }
    public MagicId? Magic { get; }
    public CommonUpgradeKind? CommonUpgrade { get; }
    public IReadOnlyList<SkillTreeNodeId> Prerequisites => prerequisites;
}

/// <summary>
/// 오각형에서 서로 이웃한 두 원소와 융합 가지의 연결 정보입니다.
/// </summary>
public sealed class FusionDefinition
{
    internal FusionDefinition(
        FusionKind kind,
        string displayName,
        MagicElement firstElement,
        MagicElement secondElement,
        MagicId magic,
        SkillTreeNodeId magicNode,
        SkillTreeNodeId masteryNode)
    {
        Kind = kind;
        DisplayName = displayName;
        FirstElement = firstElement;
        SecondElement = secondElement;
        Magic = magic;
        MagicNode = magicNode;
        MasteryNode = masteryNode;
    }

    public FusionKind Kind { get; }
    public string DisplayName { get; }
    public MagicElement FirstElement { get; }
    public MagicElement SecondElement { get; }
    public MagicId Magic { get; }
    public SkillTreeNodeId MagicNode { get; }
    public SkillTreeNodeId MasteryNode { get; }
}
