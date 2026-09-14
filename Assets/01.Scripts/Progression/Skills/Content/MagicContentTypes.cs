// 기존 ScriptableObject의 직렬화 값은 보존한다.
public enum MagicId
{
    FireBolt = 0,
    ChainLightning = 2,
    IceSpear = 4,
    RockSpear = 6,
    ShadowOrb = 8
}

public readonly struct ElementSkillStats
{
    public ElementSkillStats(float damage, float cooldown, int pierce)
    { Damage = damage; Cooldown = cooldown; PierceCount = pierce; }
    public float Damage { get; }
    public float Cooldown { get; }
    public int PierceCount { get; }
}
