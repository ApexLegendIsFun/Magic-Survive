using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 28개 노드와 오각형 연결 관계의 단일 정적 카탈로그입니다.
/// </summary>
public static class SkillTreeCatalog
{
    // 기존 MagicElement 값(Fire=0, Frost=1, Lightning=2, Dark=3)을 바꾸지 않고
    // 새 원소를 끝에 추가하는 계약입니다.
    public static readonly MagicElement EarthElement = MagicElement.Earth;

    private static readonly MagicElement[] PentagonElementsInternal =
    {
        MagicElement.Fire,
        MagicElement.Lightning,
        MagicElement.Frost,
        EarthElement,
        MagicElement.Dark
    };

    private static readonly Dictionary<SkillTreeNodeId, SkillTreeNodeDefinition> NodesById;
    private static readonly Dictionary<FusionKind, FusionDefinition> FusionsByKind;
    private static readonly ReadOnlyCollection<SkillTreeNodeDefinition> NodesInternal;
    private static readonly ReadOnlyCollection<FusionDefinition> FusionsInternal;
    private static readonly ReadOnlyCollection<MagicElement> PentagonInternal;

    static SkillTreeCatalog()
    {
        List<SkillTreeNodeDefinition> nodes = BuildNodes();
        List<FusionDefinition> fusions = BuildFusions();
        ValidateCatalog(nodes, fusions);

        NodesById = new Dictionary<SkillTreeNodeId, SkillTreeNodeDefinition>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            NodesById.Add(nodes[i].Id, nodes[i]);
        }

        FusionsByKind = new Dictionary<FusionKind, FusionDefinition>(fusions.Count);
        for (int i = 0; i < fusions.Count; i++)
        {
            FusionsByKind.Add(fusions[i].Kind, fusions[i]);
        }

        NodesInternal = nodes.AsReadOnly();
        FusionsInternal = fusions.AsReadOnly();
        PentagonInternal = Array.AsReadOnly((MagicElement[])PentagonElementsInternal.Clone());
    }

    public static IReadOnlyList<SkillTreeNodeDefinition> Nodes => NodesInternal;
    public static IReadOnlyList<FusionDefinition> Fusions => FusionsInternal;
    public static IReadOnlyList<MagicElement> PentagonElements => PentagonInternal;

    public static SkillTreeNodeDefinition GetNode(SkillTreeNodeId id)
    {
        SkillTreeNodeDefinition definition;
        if (!NodesById.TryGetValue(id, out definition))
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "등록되지 않은 스킬트리 노드입니다.");
        }

        return definition;
    }

    public static bool TryGetNode(SkillTreeNodeId id, out SkillTreeNodeDefinition definition)
    {
        return NodesById.TryGetValue(id, out definition);
    }

    public static FusionDefinition GetFusion(FusionKind kind)
    {
        FusionDefinition definition;
        if (!FusionsByKind.TryGetValue(kind, out definition))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "등록되지 않은 융합입니다.");
        }

        return definition;
    }

    public static bool TryGetFusion(FusionKind kind, out FusionDefinition definition)
    {
        return FusionsByKind.TryGetValue(kind, out definition);
    }

    public static bool IsElement(MagicElement element)
    {
        for (int i = 0; i < PentagonElementsInternal.Length; i++)
        {
            if (PentagonElementsInternal[i].Equals(element))
            {
                return true;
            }
        }

        return false;
    }

    public static bool AreAdjacent(MagicElement first, MagicElement second)
    {
        int firstIndex = GetPentagonIndex(first);
        int secondIndex = GetPentagonIndex(second);
        if (firstIndex < 0 || secondIndex < 0 || firstIndex == secondIndex)
        {
            return false;
        }

        int distance = Math.Abs(firstIndex - secondIndex);
        return distance == 1 || distance == PentagonElementsInternal.Length - 1;
    }

    public static SkillTreeNodeId GetTargetNode(MagicElement element)
    {
        if (element.Equals(MagicElement.Fire)) return SkillTreeNodeId.FireTarget;
        if (element.Equals(MagicElement.Lightning)) return SkillTreeNodeId.LightningTarget;
        if (element.Equals(MagicElement.Frost)) return SkillTreeNodeId.FrostTarget;
        if (element.Equals(EarthElement)) return SkillTreeNodeId.EarthTarget;
        if (element.Equals(MagicElement.Dark)) return SkillTreeNodeId.DarkTarget;
        throw new ArgumentOutOfRangeException(nameof(element), element, "오각형에 없는 원소입니다.");
    }

    public static SkillTreeNodeId GetAreaNode(MagicElement element)
    {
        if (element.Equals(MagicElement.Fire)) return SkillTreeNodeId.FireArea;
        if (element.Equals(MagicElement.Lightning)) return SkillTreeNodeId.LightningArea;
        if (element.Equals(MagicElement.Frost)) return SkillTreeNodeId.FrostArea;
        if (element.Equals(EarthElement)) return SkillTreeNodeId.EarthArea;
        if (element.Equals(MagicElement.Dark)) return SkillTreeNodeId.DarkArea;
        throw new ArgumentOutOfRangeException(nameof(element), element, "오각형에 없는 원소입니다.");
    }

    public static SkillTreeNodeId GetMasteryNode(MagicElement element)
    {
        if (element.Equals(MagicElement.Fire)) return SkillTreeNodeId.FireMastery;
        if (element.Equals(MagicElement.Lightning)) return SkillTreeNodeId.LightningMastery;
        if (element.Equals(MagicElement.Frost)) return SkillTreeNodeId.FrostMastery;
        if (element.Equals(EarthElement)) return SkillTreeNodeId.EarthMastery;
        if (element.Equals(MagicElement.Dark)) return SkillTreeNodeId.DarkMastery;
        throw new ArgumentOutOfRangeException(nameof(element), element, "오각형에 없는 원소입니다.");
    }

    private static int GetPentagonIndex(MagicElement element)
    {
        for (int i = 0; i < PentagonElementsInternal.Length; i++)
        {
            if (PentagonElementsInternal[i].Equals(element))
            {
                return i;
            }
        }

        return -1;
    }

    private static void ValidateCatalog(
        IReadOnlyCollection<SkillTreeNodeDefinition> nodes,
        IReadOnlyCollection<FusionDefinition> fusions)
    {
        int nodeIdCount = Enum.GetValues(typeof(SkillTreeNodeId)).Length;
        int magicIdCount = Enum.GetValues(typeof(MagicId)).Length;
        int fusionKindCount = Enum.GetValues(typeof(FusionKind)).Length;

        if (nodeIdCount != 28 || nodes.Count != nodeIdCount)
        {
            throw new InvalidOperationException("스킬트리 카탈로그는 정확히 28개 노드를 가져야 합니다.");
        }

        if (magicIdCount != 15)
        {
            throw new InvalidOperationException("마법 카탈로그 계약은 정확히 15개 MagicId를 가져야 합니다.");
        }

        if (fusionKindCount != 5 || fusions.Count != fusionKindCount)
        {
            throw new InvalidOperationException("융합 카탈로그는 정확히 5개 융합을 가져야 합니다.");
        }
    }

    private static List<SkillTreeNodeDefinition> BuildNodes()
    {
        List<SkillTreeNodeDefinition> nodes = new List<SkillTreeNodeDefinition>(28)
        {
            ElementTarget(SkillTreeNodeId.FireTarget, "화염탄", "피해 6, 쿨다운 0.8초", MagicElement.Fire, MagicId.FireBolt),
            ElementArea(SkillTreeNodeId.FireArea, "불꽃 고리", "피해 4, 쿨다운 2.6초", MagicElement.Fire, MagicId.FlameRing, SkillTreeNodeId.FireTarget),
            ElementMastery(SkillTreeNodeId.FireMastery, "화염 숙련", "화염 피해·범위 +20%. 3중첩 적 사망 시 폭발", MagicElement.Fire, SkillTreeNodeId.FireArea),

            ElementTarget(SkillTreeNodeId.LightningTarget, "연쇄 전격", "피해 4, 쿨다운 0.7초, 1회 연쇄", MagicElement.Lightning, MagicId.ChainLightning),
            ElementArea(SkillTreeNodeId.LightningArea, "낙뢰", "피해 8, 쿨다운 2.2초, 반경 1.4", MagicElement.Lightning, MagicId.LightningStrike, SkillTreeNodeId.LightningTarget),
            ElementMastery(SkillTreeNodeId.LightningMastery, "번개 숙련", "번개 피해·범위 +20%. 3중첩 시 주변 3명 방전", MagicElement.Lightning, SkillTreeNodeId.LightningArea),

            ElementTarget(SkillTreeNodeId.FrostTarget, "얼음창", "피해 5, 쿨다운 0.9초, 관통 1", MagicElement.Frost, MagicId.IceSpear),
            ElementArea(SkillTreeNodeId.FrostArea, "서리 폭발", "피해 3, 쿨다운 3초, 반경 2.5", MagicElement.Frost, MagicId.FrostBurst, SkillTreeNodeId.FrostTarget),
            ElementMastery(SkillTreeNodeId.FrostMastery, "냉기 숙련", "냉기 피해·범위 +20%. 3중첩 시 0.6초 빙결", MagicElement.Frost, SkillTreeNodeId.FrostArea),

            ElementTarget(SkillTreeNodeId.EarthTarget, "바위창", "피해 8, 쿨다운 1.1초", EarthElement, MagicId.RockSpear),
            ElementArea(SkillTreeNodeId.EarthArea, "지진", "피해 6, 쿨다운 3.2초, 반경 2.2", EarthElement, MagicId.Earthquake, SkillTreeNodeId.EarthTarget),
            ElementMastery(SkillTreeNodeId.EarthMastery, "대지 숙련", "대지 피해·범위 +20%. 3중첩 시 기절과 충격파", EarthElement, SkillTreeNodeId.EarthArea),

            ElementTarget(SkillTreeNodeId.DarkTarget, "그림자 구체", "피해 6, 쿨다운 1초, 관통 1", MagicElement.Dark, MagicId.ShadowOrb),
            ElementArea(SkillTreeNodeId.DarkArea, "암흑 파동", "피해 4, 쿨다운 2.5초, 반경 2", MagicElement.Dark, MagicId.DarkWave, SkillTreeNodeId.DarkTarget),
            ElementMastery(SkillTreeNodeId.DarkMastery, "암흑 숙련", "암흑 피해·범위 +20%. 3중첩 적 사망 시 표식 전파", MagicElement.Dark, SkillTreeNodeId.DarkArea),

            FusionMagic(SkillTreeNodeId.PlasmaMagic, "플라즈마 창", "화염+번개 융합 공격과 3+3 반응 활성화", FusionKind.Plasma, MagicId.PlasmaLance, SkillTreeNodeId.FireMastery, SkillTreeNodeId.LightningMastery),
            FusionMastery(SkillTreeNodeId.PlasmaMastery, "플라즈마 숙련", "융합 공격·반응 피해 +20%, 반경 2.4, 연쇄 대상 5명", FusionKind.Plasma, SkillTreeNodeId.PlasmaMagic),

            FusionMagic(SkillTreeNodeId.StormMagic, "폭풍 낙뢰", "번개+냉기 융합 공격과 3+3 반응 활성화", FusionKind.Storm, MagicId.StormLightning, SkillTreeNodeId.LightningMastery, SkillTreeNodeId.FrostMastery),
            FusionMastery(SkillTreeNodeId.StormMastery, "폭풍 숙련", "융합 공격·반응 피해 +20%, 반경 3, 지속시간 3.5초", FusionKind.Storm, SkillTreeNodeId.StormMagic),

            FusionMagic(SkillTreeNodeId.PermafrostMagic, "얼음 기둥", "냉기+대지 융합 공격과 3+3 반응 활성화", FusionKind.Permafrost, MagicId.IcePillar, SkillTreeNodeId.FrostMastery, SkillTreeNodeId.EarthMastery),
            FusionMastery(SkillTreeNodeId.PermafrostMastery, "동토 숙련", "융합 공격·반응 피해 +20%, 0.6초 후 두 번째 파동", FusionKind.Permafrost, SkillTreeNodeId.PermafrostMagic),

            FusionMagic(SkillTreeNodeId.GraveyardMagic, "묘지 가시", "대지+암흑 융합 공격과 3+3 반응 활성화", FusionKind.Graveyard, MagicId.GraveyardSpikes, SkillTreeNodeId.EarthMastery, SkillTreeNodeId.DarkMastery),
            FusionMastery(SkillTreeNodeId.GraveyardMastery, "묘지 숙련", "융합 공격·반응 피해 +20%, 가시 8개와 발생 범위 증가", FusionKind.Graveyard, SkillTreeNodeId.GraveyardMagic),

            FusionMagic(SkillTreeNodeId.HellfireMagic, "지옥불 구체", "암흑+화염 융합 공격과 3+3 반응 활성화", FusionKind.Hellfire, MagicId.HellfireOrb, SkillTreeNodeId.DarkMastery, SkillTreeNodeId.FireMastery),
            FusionMastery(SkillTreeNodeId.HellfireMastery, "지옥불 숙련", "융합 공격·반응 피해 +20%, 반경 2.8, 지속시간 4초", FusionKind.Hellfire, SkillTreeNodeId.HellfireMagic),

            Common(SkillTreeNodeId.CommonPower, "위력 강화", "모든 마법·상태·반응 피해 +15%", CommonUpgradeKind.Power),
            Common(SkillTreeNodeId.CommonRapidFire, "속사 강화", "모든 자동공격 쿨다운 ×0.9", CommonUpgradeKind.RapidFire),
            Common(SkillTreeNodeId.CommonPierce, "관통 강화", "투사체 관통 +1, 연쇄 공격 대상 +1", CommonUpgradeKind.Pierce)
        };

        return nodes;
    }

    private static List<FusionDefinition> BuildFusions()
    {
        return new List<FusionDefinition>(5)
        {
            new FusionDefinition(FusionKind.Plasma, "플라즈마", MagicElement.Fire, MagicElement.Lightning, MagicId.PlasmaLance, SkillTreeNodeId.PlasmaMagic, SkillTreeNodeId.PlasmaMastery),
            new FusionDefinition(FusionKind.Storm, "폭풍", MagicElement.Lightning, MagicElement.Frost, MagicId.StormLightning, SkillTreeNodeId.StormMagic, SkillTreeNodeId.StormMastery),
            new FusionDefinition(FusionKind.Permafrost, "동토", MagicElement.Frost, EarthElement, MagicId.IcePillar, SkillTreeNodeId.PermafrostMagic, SkillTreeNodeId.PermafrostMastery),
            new FusionDefinition(FusionKind.Graveyard, "묘지", EarthElement, MagicElement.Dark, MagicId.GraveyardSpikes, SkillTreeNodeId.GraveyardMagic, SkillTreeNodeId.GraveyardMastery),
            new FusionDefinition(FusionKind.Hellfire, "지옥불", MagicElement.Dark, MagicElement.Fire, MagicId.HellfireOrb, SkillTreeNodeId.HellfireMagic, SkillTreeNodeId.HellfireMastery)
        };
    }

    private static SkillTreeNodeDefinition ElementTarget(SkillTreeNodeId id, string name, string description, MagicElement element, MagicId magic)
    {
        return new SkillTreeNodeDefinition(id, SkillTreeNodeType.ElementTargetMagic, name, description, element, null, magic, null);
    }

    private static SkillTreeNodeDefinition ElementArea(SkillTreeNodeId id, string name, string description, MagicElement element, MagicId magic, SkillTreeNodeId prerequisite)
    {
        return new SkillTreeNodeDefinition(id, SkillTreeNodeType.ElementAreaMagic, name, description, element, null, magic, null, prerequisite);
    }

    private static SkillTreeNodeDefinition ElementMastery(SkillTreeNodeId id, string name, string description, MagicElement element, SkillTreeNodeId prerequisite)
    {
        return new SkillTreeNodeDefinition(id, SkillTreeNodeType.ElementMastery, name, description, element, null, null, null, prerequisite);
    }

    private static SkillTreeNodeDefinition FusionMagic(SkillTreeNodeId id, string name, string description, FusionKind fusion, MagicId magic, params SkillTreeNodeId[] prerequisites)
    {
        return new SkillTreeNodeDefinition(id, SkillTreeNodeType.FusionMagic, name, description, null, fusion, magic, null, prerequisites);
    }

    private static SkillTreeNodeDefinition FusionMastery(SkillTreeNodeId id, string name, string description, FusionKind fusion, SkillTreeNodeId prerequisite)
    {
        return new SkillTreeNodeDefinition(id, SkillTreeNodeType.FusionMastery, name, description, null, fusion, null, null, prerequisite);
    }

    private static SkillTreeNodeDefinition Common(SkillTreeNodeId id, string name, string description, CommonUpgradeKind upgrade)
    {
        return new SkillTreeNodeDefinition(id, SkillTreeNodeType.CommonUpgrade, name, description, null, null, null, upgrade);
    }
}
