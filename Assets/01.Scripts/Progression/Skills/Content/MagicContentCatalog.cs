using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 10개 기본 마법, 5개 융합 마법, 원소 표식 규칙의 단일 읽기 전용 원본입니다.
/// </summary>
public static class MagicContentCatalog
{
    public const int MaxMarkStacks = 3;
    public const float MarkDurationSeconds = 5f;
    public const int MasteryTriggerFromStacks = 2;
    public const int MasteryTriggerToStacks = 3;
    public const float BaseMasteryDamageMultiplier = 1.2f;
    public const float BaseMasteryRangeMultiplier = 1.2f;
    public const float FusionMasteryDamageMultiplier = 1.2f;
    public const int FusionAttackMarkStacksPerParent = 1;
    public const int FusionReactionRequiredStacksPerParent = 3;
    public const int FusionReactionConsumedStacksPerParent = 3;
    public const int MarkVisualStageCount = 3;
    public const bool MarksStoredIndependentlyByElement = true;
    public const bool ReapplyingSameElementRefreshesDuration = true;
    public const bool ThreeStackMarkUsesPulseVisual = true;
    public const bool MasteryProcOncePerTwoToThreeTransition = true;
    public const bool MasteryCanReactivateAfterMarksReset = true;
    public const bool FusionMustBeUnlockedForReaction = true;
    public const bool ResolveAllSimultaneouslyEligibleReactions = true;
    public const bool RemoveRelatedMarksAfterReactionEvaluation = true;

    private static readonly MagicDefinition FireBolt = BaseMagic(
        MagicId.FireBolt,
        "화염탄",
        MagicAttackKind.Targeted,
        MagicElement.Fire,
        damage: 6f,
        cooldown: 0.8f);

    private static readonly MagicDefinition FlameRing = BaseMagic(
        MagicId.FlameRing,
        "불꽃 고리",
        MagicAttackKind.Area,
        MagicElement.Fire,
        damage: 4f,
        cooldown: 2.6f);

    private static readonly MagicDefinition ChainLightning = BaseMagic(
        MagicId.ChainLightning,
        "연쇄 전격",
        MagicAttackKind.Targeted,
        MagicElement.Lightning,
        damage: 4f,
        cooldown: 0.7f,
        chainTargetCount: 1);

    private static readonly MagicDefinition LightningStrike = BaseMagic(
        MagicId.LightningStrike,
        "낙뢰",
        MagicAttackKind.Area,
        MagicElement.Lightning,
        damage: 8f,
        cooldown: 2.2f,
        radius: 1.4f);

    private static readonly MagicDefinition IceSpear = BaseMagic(
        MagicId.IceSpear,
        "얼음창",
        MagicAttackKind.Targeted,
        MagicElement.Frost,
        damage: 5f,
        cooldown: 0.9f,
        pierceCount: 1);

    private static readonly MagicDefinition FrostBurst = BaseMagic(
        MagicId.FrostBurst,
        "서리 폭발",
        MagicAttackKind.Area,
        MagicElement.Frost,
        damage: 3f,
        cooldown: 3f,
        radius: 2.5f);

    private static readonly MagicDefinition RockSpear = BaseMagic(
        MagicId.RockSpear,
        "바위창",
        MagicAttackKind.Targeted,
        MagicElement.Earth,
        damage: 8f,
        cooldown: 1.1f);

    private static readonly MagicDefinition Earthquake = BaseMagic(
        MagicId.Earthquake,
        "지진",
        MagicAttackKind.Area,
        MagicElement.Earth,
        damage: 6f,
        cooldown: 3.2f,
        radius: 2.2f);

    private static readonly MagicDefinition ShadowOrb = BaseMagic(
        MagicId.ShadowOrb,
        "그림자 구체",
        MagicAttackKind.Targeted,
        MagicElement.Dark,
        damage: 6f,
        cooldown: 1f,
        pierceCount: 1);

    private static readonly MagicDefinition DarkWave = BaseMagic(
        MagicId.DarkWave,
        "암흑 파동",
        MagicAttackKind.Area,
        MagicElement.Dark,
        damage: 4f,
        cooldown: 2.5f,
        radius: 2f);

    private static readonly MagicDefinition PlasmaLance = FusionMagic(
        MagicId.PlasmaLance,
        "플라즈마 창",
        MagicElement.Fire,
        MagicElement.Lightning,
        damage: 12f,
        cooldown: 1.4f,
        pierceCount: 3);

    private static readonly MagicDefinition StormLightning = FusionMagic(
        MagicId.StormLightning,
        "폭풍 낙뢰",
        MagicElement.Lightning,
        MagicElement.Frost,
        damage: 8f,
        cooldown: 2f);

    private static readonly MagicDefinition IcePillar = FusionMagic(
        MagicId.IcePillar,
        "얼음 기둥",
        MagicElement.Frost,
        MagicElement.Earth,
        damage: 14f,
        cooldown: 2.2f);

    private static readonly MagicDefinition GraveyardSpikes = FusionMagic(
        MagicId.GraveyardSpikes,
        "묘지 가시",
        MagicElement.Earth,
        MagicElement.Dark,
        damage: 12f,
        cooldown: 2f);

    private static readonly MagicDefinition HellfireOrb = FusionMagic(
        MagicId.HellfireOrb,
        "지옥불 구체",
        MagicElement.Dark,
        MagicElement.Fire,
        damage: 9f,
        cooldown: 1.8f);

    private static readonly ReadOnlyCollection<MagicDefinition> BaseMagics = Array.AsReadOnly(
        new[]
        {
            FireBolt,
            FlameRing,
            ChainLightning,
            LightningStrike,
            IceSpear,
            FrostBurst,
            RockSpear,
            Earthquake,
            ShadowOrb,
            DarkWave
        });

    private static readonly ReadOnlyCollection<FusionContentDefinition> Fusions = Array.AsReadOnly(
        new[]
        {
            new FusionContentDefinition(
                FusionKind.Plasma,
                "플라즈마",
                MagicElement.Fire,
                MagicElement.Lightning,
                PlasmaLance,
                new FusionReactionDefinition(
                    FusionReactionDamageMode.Instant,
                    "피해 24, 반경 1.8 폭발 후 3명에게 피해 8 연쇄",
                    damage: 24f,
                    radius: 1.8f,
                    chainDamage: 8f,
                    chainTargetCount: 3),
                new FusionMasteryDefinition(
                    "반경 2.4, 연쇄 대상 5명",
                    radius: 2.4f,
                    chainTargetCount: 5)),
            new FusionContentDefinition(
                FusionKind.Storm,
                "폭풍",
                MagicElement.Lightning,
                MagicElement.Frost,
                StormLightning,
                new FusionReactionDefinition(
                    FusionReactionDamageMode.PerTick,
                    "반경 2.5에 2.5초간 0.5초 주기 피해 3",
                    damage: 3f,
                    radius: 2.5f,
                    durationSeconds: 2.5f,
                    tickIntervalSeconds: 0.5f),
                new FusionMasteryDefinition(
                    "반경 3, 지속시간 3.5초",
                    radius: 3f,
                    durationSeconds: 3.5f)),
            new FusionContentDefinition(
                FusionKind.Permafrost,
                "동토",
                MagicElement.Frost,
                MagicElement.Earth,
                IcePillar,
                new FusionReactionDefinition(
                    FusionReactionDamageMode.Instant,
                    "1초 빙결과 피해 18 얼음 가시 파동",
                    damage: 18f,
                    freezeSeconds: 1f),
                new FusionMasteryDefinition(
                    "0.6초 후 더 큰 두 번째 파동",
                    secondaryWaveDelaySeconds: 0.6f,
                    addsLargerSecondaryWave: true)),
            new FusionContentDefinition(
                FusionKind.Graveyard,
                "묘지",
                MagicElement.Earth,
                MagicElement.Dark,
                GraveyardSpikes,
                new FusionReactionDefinition(
                    FusionReactionDamageMode.PerSpawn,
                    "주변에 피해 7 가시 4개와 0.3초 기절",
                    damage: 7f,
                    stunSeconds: 0.3f,
                    spawnCount: 4),
                new FusionMasteryDefinition(
                    "가시 8개, 발생 범위 증가",
                    spawnCount: 8,
                    increasesSpawnRange: true)),
            new FusionContentDefinition(
                FusionKind.Hellfire,
                "지옥불",
                MagicElement.Dark,
                MagicElement.Fire,
                HellfireOrb,
                new FusionReactionDefinition(
                    FusionReactionDamageMode.PerTick,
                    "반경 2.2에 3초간 0.5초 주기 피해 4",
                    damage: 4f,
                    radius: 2.2f,
                    durationSeconds: 3f,
                    tickIntervalSeconds: 0.5f),
                new FusionMasteryDefinition(
                    "반경 2.8, 지속시간 4초",
                    radius: 2.8f,
                    durationSeconds: 4f))
        });

    private static readonly ReadOnlyCollection<ElementRuleDefinition> ElementRules = Array.AsReadOnly(
        new[]
        {
            new ElementRuleDefinition(
                MagicElement.Fire,
                ElementMarkEffectKind.DamageOverTime,
                1f,
                "중첩당 초당 피해 1",
                new ElementMasteryProcDefinition(
                    ElementMasteryProcKind.DeathExplosion,
                    ElementMasteryProcTiming.OnArmedTargetDeath,
                    "3중첩 적 사망 시 피해 10, 반경 1.5 폭발",
                    damage: 10f,
                    radius: 1.5f)),
            new ElementRuleDefinition(
                MagicElement.Lightning,
                ElementMarkEffectKind.LightningDamageTaken,
                0.05f,
                "중첩당 번개 피해 +5%",
                new ElementMasteryProcDefinition(
                    ElementMasteryProcKind.Discharge,
                    ElementMasteryProcTiming.OnReachedThreeStacks,
                    "3중첩 시 주변 3명에게 피해 5 방전",
                    damage: 5f,
                    targetCount: 3)),
            new ElementRuleDefinition(
                MagicElement.Frost,
                ElementMarkEffectKind.MovementSpeedReduction,
                0.10f,
                "중첩당 이동속도 -10%",
                new ElementMasteryProcDefinition(
                    ElementMasteryProcKind.Freeze,
                    ElementMasteryProcTiming.OnReachedThreeStacks,
                    "3중첩 시 0.6초 빙결",
                    freezeSeconds: 0.6f)),
            new ElementRuleDefinition(
                MagicElement.Earth,
                ElementMarkEffectKind.KnockbackTaken,
                0.20f,
                "중첩당 받는 밀치기 +20%",
                new ElementMasteryProcDefinition(
                    ElementMasteryProcKind.StunAndShockwave,
                    ElementMasteryProcTiming.OnReachedThreeStacks,
                    "3중첩 시 0.5초 기절과 작은 충격파",
                    stunSeconds: 0.5f,
                    createsShockwave: true)),
            new ElementRuleDefinition(
                MagicElement.Dark,
                ElementMarkEffectKind.AllDamageTaken,
                0.05f,
                "중첩당 모든 받는 피해 +5%",
                new ElementMasteryProcDefinition(
                    ElementMasteryProcKind.DeathMarkSpread,
                    ElementMasteryProcTiming.OnArmedTargetDeath,
                    "3중첩 적 사망 시 주변 3명에게 암흑 표식 1개 전파",
                    targetCount: 3,
                    appliedMarkStacks: 1))
        });

    private static readonly Dictionary<MagicId, MagicDefinition> MagicById = BuildMagicLookup();
    private static readonly Dictionary<FusionKind, FusionContentDefinition> FusionByKind = BuildFusionLookup();
    private static readonly Dictionary<MagicElement, ElementRuleDefinition> ElementRuleByElement = BuildElementRuleLookup();

    public static IReadOnlyList<MagicDefinition> AllBaseMagics => BaseMagics;
    public static IReadOnlyList<FusionContentDefinition> AllFusions => Fusions;
    public static IReadOnlyList<ElementRuleDefinition> AllElementRules => ElementRules;

    public static MagicDefinition GetMagic(MagicId id)
    {
        if (MagicById.TryGetValue(id, out MagicDefinition magic))
        {
            return magic;
        }

        throw new ArgumentOutOfRangeException(nameof(id), id, "등록되지 않은 마법입니다.");
    }

    public static FusionContentDefinition GetFusion(FusionKind kind)
    {
        if (FusionByKind.TryGetValue(kind, out FusionContentDefinition fusion))
        {
            return fusion;
        }

        throw new ArgumentOutOfRangeException(nameof(kind), kind, "등록되지 않은 융합입니다.");
    }

    public static ElementRuleDefinition GetElementRule(MagicElement element)
    {
        if (ElementRuleByElement.TryGetValue(element, out ElementRuleDefinition rule))
        {
            return rule;
        }

        throw new ArgumentOutOfRangeException(nameof(element), element, "등록되지 않은 원소입니다.");
    }

    private static MagicDefinition BaseMagic(
        MagicId id,
        string name,
        MagicAttackKind attackKind,
        MagicElement element,
        float damage,
        float cooldown,
        float? radius = null,
        int pierceCount = 0,
        int chainTargetCount = 0)
    {
        return new MagicDefinition(
            id,
            name,
            attackKind,
            element,
            null,
            new MagicAttackStats(
                damage,
                cooldown,
                radius,
                pierceCount,
                chainTargetCount));
    }

    private static MagicDefinition FusionMagic(
        MagicId id,
        string name,
        MagicElement firstParent,
        MagicElement secondParent,
        float damage,
        float cooldown,
        float? radius = null,
        int pierceCount = 0,
        int chainTargetCount = 0,
        float? durationSeconds = null,
        float? tickIntervalSeconds = null)
    {
        return new MagicDefinition(
            id,
            name,
            MagicAttackKind.Fusion,
            firstParent,
            secondParent,
            new MagicAttackStats(
                damage,
                cooldown,
                radius,
                pierceCount,
                chainTargetCount,
                durationSeconds,
                tickIntervalSeconds));
    }

    private static Dictionary<MagicId, MagicDefinition> BuildMagicLookup()
    {
        var lookup = new Dictionary<MagicId, MagicDefinition>();
        for (int index = 0; index < BaseMagics.Count; index++)
        {
            MagicDefinition magic = BaseMagics[index];
            lookup.Add(magic.Id, magic);
        }

        for (int index = 0; index < Fusions.Count; index++)
        {
            MagicDefinition magic = Fusions[index].AutomaticAttack;
            lookup.Add(magic.Id, magic);
        }

        return lookup;
    }

    private static Dictionary<FusionKind, FusionContentDefinition> BuildFusionLookup()
    {
        var lookup = new Dictionary<FusionKind, FusionContentDefinition>();
        for (int index = 0; index < Fusions.Count; index++)
        {
            FusionContentDefinition fusion = Fusions[index];
            lookup.Add(fusion.Kind, fusion);
        }

        return lookup;
    }

    private static Dictionary<MagicElement, ElementRuleDefinition> BuildElementRuleLookup()
    {
        var lookup = new Dictionary<MagicElement, ElementRuleDefinition>();
        for (int index = 0; index < ElementRules.Count; index++)
        {
            ElementRuleDefinition rule = ElementRules[index];
            lookup.Add(rule.Element, rule);
        }

        return lookup;
    }
}
