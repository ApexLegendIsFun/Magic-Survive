using System;

public enum MagicAttackKind
{
    Targeted = 0,
    Area = 1,
    Fusion = 2
}

public enum ElementMarkEffectKind
{
    DamageOverTime = 0,
    LightningDamageTaken = 1,
    MovementSpeedReduction = 2,
    KnockbackTaken = 3,
    AllDamageTaken = 4
}

public enum ElementMasteryProcKind
{
    DeathExplosion = 0,
    Discharge = 1,
    Freeze = 2,
    StunAndShockwave = 3,
    DeathMarkSpread = 4
}

public enum ElementMasteryProcTiming
{
    OnReachedThreeStacks = 0,
    OnArmedTargetDeath = 1
}

public enum FusionReactionDamageMode
{
    Instant = 0,
    PerTick = 1,
    PerSpawn = 2
}

/// <summary>
/// 자동공격 하나의 명시된 전투 수치입니다. 기획에 없는 선택 수치는 null입니다.
/// </summary>
public sealed class MagicAttackStats
{
    internal MagicAttackStats(
        float damage,
        float cooldownSeconds,
        float? radius = null,
        int pierceCount = 0,
        int chainTargetCount = 0,
        float? durationSeconds = null,
        float? tickIntervalSeconds = null)
    {
        Damage = damage;
        CooldownSeconds = cooldownSeconds;
        Radius = radius;
        PierceCount = pierceCount;
        ChainTargetCount = chainTargetCount;
        DurationSeconds = durationSeconds;
        TickIntervalSeconds = tickIntervalSeconds;
    }

    public float Damage { get; }
    public float CooldownSeconds { get; }
    public float? Radius { get; }
    public int PierceCount { get; }
    public int ChainTargetCount { get; }
    public float? DurationSeconds { get; }
    public float? TickIntervalSeconds { get; }
}

/// <summary>
/// 기본 또는 융합 자동공격 정의입니다. 융합 마법만 SecondaryElement를 가집니다.
/// </summary>
public sealed class MagicDefinition
{
    internal MagicDefinition(
        MagicId id,
        string displayName,
        MagicAttackKind attackKind,
        MagicElement primaryElement,
        MagicElement? secondaryElement,
        MagicAttackStats attack)
    {
        Id = id;
        DisplayName = displayName;
        AttackKind = attackKind;
        PrimaryElement = primaryElement;
        SecondaryElement = secondaryElement;
        Attack = attack ?? throw new ArgumentNullException(nameof(attack));
    }

    public MagicId Id { get; }
    public string DisplayName { get; }
    public MagicAttackKind AttackKind { get; }
    public MagicElement PrimaryElement { get; }
    public MagicElement? SecondaryElement { get; }
    public MagicAttackStats Attack { get; }
    public bool IsFusion => SecondaryElement.HasValue;
}

/// <summary>
/// 원소 숙련의 고유 발동 수치입니다.
/// </summary>
public sealed class ElementMasteryProcDefinition
{
    internal ElementMasteryProcDefinition(
        ElementMasteryProcKind kind,
        ElementMasteryProcTiming timing,
        string description,
        float damage = 0f,
        float? radius = null,
        int targetCount = 0,
        float freezeSeconds = 0f,
        float stunSeconds = 0f,
        int appliedMarkStacks = 0,
        bool createsShockwave = false)
    {
        Kind = kind;
        Timing = timing;
        Description = description;
        Damage = damage;
        Radius = radius;
        TargetCount = targetCount;
        FreezeSeconds = freezeSeconds;
        StunSeconds = stunSeconds;
        AppliedMarkStacks = appliedMarkStacks;
        CreatesShockwave = createsShockwave;
    }

    public ElementMasteryProcKind Kind { get; }
    public ElementMasteryProcTiming Timing { get; }
    public string Description { get; }
    public float Damage { get; }
    public float? Radius { get; }
    public int TargetCount { get; }
    public float FreezeSeconds { get; }
    public float StunSeconds { get; }
    public int AppliedMarkStacks { get; }
    public bool CreatesShockwave { get; }
}

/// <summary>
/// 원소별 표식과 숙련 규칙입니다.
/// </summary>
public sealed class ElementRuleDefinition
{
    internal ElementRuleDefinition(
        MagicElement element,
        ElementMarkEffectKind markEffectKind,
        float markEffectPerStack,
        string markDescription,
        ElementMasteryProcDefinition masteryProc)
    {
        Element = element;
        MarkEffectKind = markEffectKind;
        MarkEffectPerStack = markEffectPerStack;
        MarkDescription = markDescription;
        MasteryProc = masteryProc ?? throw new ArgumentNullException(nameof(masteryProc));
    }

    public MagicElement Element { get; }
    public ElementMarkEffectKind MarkEffectKind { get; }
    public float MarkEffectPerStack { get; }
    public string MarkDescription { get; }
    public ElementMasteryProcDefinition MasteryProc { get; }
    public int MaxMarkStacks => MagicContentCatalog.MaxMarkStacks;
    public float MarkDurationSeconds => MagicContentCatalog.MarkDurationSeconds;
    public int MasteryTriggerFromStacks => MagicContentCatalog.MasteryTriggerFromStacks;
    public int MasteryTriggerToStacks => MagicContentCatalog.MasteryTriggerToStacks;
    public float MasteryDamageMultiplier => MagicContentCatalog.BaseMasteryDamageMultiplier;
    public float MasteryRangeMultiplier => MagicContentCatalog.BaseMasteryRangeMultiplier;
}

/// <summary>
/// 3+3 표식 소비로 발생하는 융합 반응 수치입니다.
/// </summary>
public sealed class FusionReactionDefinition
{
    internal FusionReactionDefinition(
        FusionReactionDamageMode damageMode,
        string description,
        float damage,
        float? radius = null,
        float? durationSeconds = null,
        float? tickIntervalSeconds = null,
        float freezeSeconds = 0f,
        float stunSeconds = 0f,
        int spawnCount = 0,
        float chainDamage = 0f,
        int chainTargetCount = 0)
    {
        DamageMode = damageMode;
        Description = description;
        Damage = damage;
        Radius = radius;
        DurationSeconds = durationSeconds;
        TickIntervalSeconds = tickIntervalSeconds;
        FreezeSeconds = freezeSeconds;
        StunSeconds = stunSeconds;
        SpawnCount = spawnCount;
        ChainDamage = chainDamage;
        ChainTargetCount = chainTargetCount;
    }

    public FusionReactionDamageMode DamageMode { get; }
    public string Description { get; }
    public float Damage { get; }
    public float? Radius { get; }
    public float? DurationSeconds { get; }
    public float? TickIntervalSeconds { get; }
    public float FreezeSeconds { get; }
    public float StunSeconds { get; }
    public int SpawnCount { get; }
    public float ChainDamage { get; }
    public int ChainTargetCount { get; }
}

/// <summary>
/// 융합 숙련이 반응에 적용하는 수치 또는 추가 동작입니다.
/// </summary>
public sealed class FusionMasteryDefinition
{
    internal FusionMasteryDefinition(
        string description,
        float? radius = null,
        float? durationSeconds = null,
        int? chainTargetCount = null,
        int? spawnCount = null,
        float? secondaryWaveDelaySeconds = null,
        bool addsLargerSecondaryWave = false,
        bool increasesSpawnRange = false)
    {
        Description = description;
        Radius = radius;
        DurationSeconds = durationSeconds;
        ChainTargetCount = chainTargetCount;
        SpawnCount = spawnCount;
        SecondaryWaveDelaySeconds = secondaryWaveDelaySeconds;
        AddsLargerSecondaryWave = addsLargerSecondaryWave;
        IncreasesSpawnRange = increasesSpawnRange;
    }

    public string Description { get; }
    public float DamageMultiplier => MagicContentCatalog.FusionMasteryDamageMultiplier;
    public float? Radius { get; }
    public float? DurationSeconds { get; }
    public int? ChainTargetCount { get; }
    public int? SpawnCount { get; }
    public float? SecondaryWaveDelaySeconds { get; }
    public bool AddsLargerSecondaryWave { get; }
    public bool IncreasesSpawnRange { get; }
}

/// <summary>
/// 융합 자동공격, 반응, 숙련의 한 묶음입니다.
/// </summary>
public sealed class FusionContentDefinition
{
    internal FusionContentDefinition(
        FusionKind kind,
        string displayName,
        MagicElement firstParent,
        MagicElement secondParent,
        MagicDefinition automaticAttack,
        FusionReactionDefinition reaction,
        FusionMasteryDefinition mastery)
    {
        Kind = kind;
        DisplayName = displayName;
        FirstParent = firstParent;
        SecondParent = secondParent;
        AutomaticAttack = automaticAttack ?? throw new ArgumentNullException(nameof(automaticAttack));
        Reaction = reaction ?? throw new ArgumentNullException(nameof(reaction));
        Mastery = mastery ?? throw new ArgumentNullException(nameof(mastery));
    }

    public FusionKind Kind { get; }
    public string DisplayName { get; }
    public MagicElement FirstParent { get; }
    public MagicElement SecondParent { get; }
    public MagicDefinition AutomaticAttack { get; }
    public FusionReactionDefinition Reaction { get; }
    public FusionMasteryDefinition Mastery { get; }
    public int AppliedMarkStacksPerParent => MagicContentCatalog.FusionAttackMarkStacksPerParent;
    public int RequiredMarkStacksPerParent => MagicContentCatalog.FusionReactionRequiredStacksPerParent;
    public int ConsumedMarkStacksPerParent => MagicContentCatalog.FusionReactionConsumedStacksPerParent;
    public bool ReactionAppliesMarks => false;
}
