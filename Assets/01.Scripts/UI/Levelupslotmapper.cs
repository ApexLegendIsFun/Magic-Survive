using System.Collections.Generic;
using System.Linq;

//해당 스크립트는 오각형 계산기라고 생각하면 편함. 
public static class LevelUpSlotMapper
{
    private static readonly Dictionary<SkillTreeNodeType, int> StepOrder = new Dictionary<SkillTreeNodeType, int>
    {
        { SkillTreeNodeType.ElementTargetMagic, 0 },
        { SkillTreeNodeType.ElementAreaMagic, 1 },
        { SkillTreeNodeType.ElementMastery, 2 },
        { SkillTreeNodeType.FusionMagic, 0 },
        { SkillTreeNodeType.FusionMastery, 1 },
    };

    /// 화염 슬롯이 대표하는 3단계 노드
    public static List<SkillTreeNodeDefinition> GetElementChain(MagicElement element)
    {
        return SkillTreeCatalog.Nodes
            .Where(n => n.Element.HasValue && n.Element.Value.Equals(element))
            .OrderBy(n => StepOrder[n.Type])
            .ToList();
    }

    /// 변 슬롯이 대표하는 2단계 노드
    public static List<SkillTreeNodeDefinition> GetFusionChain(FusionKind fusion)
    {
        return SkillTreeCatalog.Nodes
            .Where(n => n.Fusion.HasValue && n.Fusion.Value.Equals(fusion))
            .OrderBy(n => StepOrder[n.Type])
            .ToList();
    }

    public static (SkillTreeNodeDefinition node, bool maxed) GetRepresentativeNode(
        IReadOnlyList<SkillTreeNodeDefinition> chain,
        IReadOnlyPlayerSkillTree tree)
    {
        for (int i = 0; i < chain.Count; i++)
        {
            if (!tree.HasNode(chain[i].Id))
            {
                return (chain[i], false);
            }
        }
        return (chain[chain.Count - 1], true); // 전부 보유 → 마지막 노드를 "완료" 상태로 표시
    }
}