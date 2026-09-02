/// <summary>
/// 오각형 스킬트리의 28개 노드 식별자입니다.
/// </summary>
public enum SkillTreeNodeId
{
    FireTarget = 0,
    FireArea = 1,
    FireMastery = 2,

    LightningTarget = 3,
    LightningArea = 4,
    LightningMastery = 5,

    FrostTarget = 6,
    FrostArea = 7,
    FrostMastery = 8,

    EarthTarget = 9,
    EarthArea = 10,
    EarthMastery = 11,

    DarkTarget = 12,
    DarkArea = 13,
    DarkMastery = 14,

    PlasmaMagic = 15,
    PlasmaMastery = 16,
    StormMagic = 17,
    StormMastery = 18,
    PermafrostMagic = 19,
    PermafrostMastery = 20,
    GraveyardMagic = 21,
    GraveyardMastery = 22,
    HellfireMagic = 23,
    HellfireMastery = 24,

    CommonPower = 25,
    CommonRapidFire = 26,
    CommonPierce = 27
}

public enum SkillTreeNodeState
{
    Hidden = 0,
    Locked = 1,
    Available = 2,
    Owned = 3
}

public enum SkillTreeNodeType
{
    ElementTargetMagic = 0,
    ElementAreaMagic = 1,
    ElementMastery = 2,
    FusionMagic = 3,
    FusionMastery = 4,
    CommonUpgrade = 5
}

/// <summary>
/// 전투 시스템이 등록할 기본 마법 10개와 융합 마법 5개입니다.
/// </summary>
public enum MagicId
{
    FireBolt = 0,
    FlameRing = 1,
    ChainLightning = 2,
    LightningStrike = 3,
    IceSpear = 4,
    FrostBurst = 5,
    RockSpear = 6,
    Earthquake = 7,
    ShadowOrb = 8,
    DarkWave = 9,
    PlasmaLance = 10,
    StormLightning = 11,
    IcePillar = 12,
    GraveyardSpikes = 13,
    HellfireOrb = 14
}

public enum FusionKind
{
    Plasma = 0,
    Storm = 1,
    Permafrost = 2,
    Graveyard = 3,
    Hellfire = 4
}

public enum CommonUpgradeKind
{
    Power = 0,
    RapidFire = 1,
    Pierce = 2
}
