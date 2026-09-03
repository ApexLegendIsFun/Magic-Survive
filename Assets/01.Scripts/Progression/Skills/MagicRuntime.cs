using System;
using UnityEngine;

/// <summary>
/// ProjectileMagicDefinition의 시작값을 복사해 한 판 동안 강화하는 공격 인스턴스입니다.
/// </summary>
public sealed class MagicRuntime : IAttackSource
{
    private const float DamageUpgradeAmount = 2f;
    private const float FireRateCooldownMultiplier = 0.9f;

    private readonly Projectile projectilePrefab;
    private float baseCooldown;
    private float baseRange;
    private float baseDamage;
    private float baseMaxDistance;
    private float baseHitRadius;
    private int basePierceCount;
    private float globalDamageMultiplier = 1f;
    private float globalCooldownMultiplier = 1f;
    private float masteryDamageMultiplier = 1f;
    private float masteryRangeMultiplier = 1f;
    private int bonusPierce;

    public MagicRuntime(ProjectileMagicDefinition definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        Id = definition.MagicId;
        Element = definition.Element;
        projectilePrefab = definition.ProjectilePrefab;
        baseCooldown = definition.Cooldown;
        baseRange = definition.Range;
        baseDamage = definition.Damage;
        Speed = definition.Speed;
        baseMaxDistance = definition.MaxDistance;
        baseHitRadius = definition.HitRadius;
        basePierceCount = definition.PierceCount;
    }

    public MagicId Id { get; }
    public MagicElement Element { get; }
    public float Cooldown => baseCooldown * globalCooldownMultiplier;
    public float Range => baseRange * masteryRangeMultiplier;
    public float Damage => baseDamage * globalDamageMultiplier * masteryDamageMultiplier;
    public float Speed { get; }
    public float MaxDistance => baseMaxDistance * masteryRangeMultiplier;
    public float HitRadius => baseHitRadius * masteryRangeMultiplier;
    public int PierceCount => basePierceCount + bonusPierce;

    public bool Execute(in AttackContext context)
    {
        if (context.Target == null || context.Launcher == null)
        {
            return false;
        }

        Vector2 targetPosition = context.Target.transform.position;
        Vector2 direction = targetPosition - context.Origin;

        if (direction.sqrMagnitude < 0.0001f)
        {
            // Enemies can overlap the player's exact position. Refusing to fire here
            // permanently locks nearest-target selection onto that enemy.
            direction = Vector2.right;
        }

        ProjectileSpec spec = new ProjectileSpec(
            projectilePrefab,
            Damage,
            Speed,
            MaxDistance,
            HitRadius,
            PierceCount);

        context.Launcher.Fire(spec, context.Origin, direction);
        return true;
    }

    public void ApplyUpgrade(SkillUpgradeKind kind)
    {
        switch (kind)
        {
            case SkillUpgradeKind.Damage:
                baseDamage += DamageUpgradeAmount;
                break;

            case SkillUpgradeKind.FireRate:
                baseCooldown *= FireRateCooldownMultiplier;
                break;

            case SkillUpgradeKind.Pierce:
                basePierceCount += 1;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "지원하지 않는 강화 종류입니다.");
        }
    }

    public void SetTreeModifiers(
        float damageMultiplier,
        float cooldownMultiplier,
        int additionalPierce,
        float masteryDamage,
        float masteryRange)
    {
        globalDamageMultiplier = Mathf.Max(0f, damageMultiplier);
        globalCooldownMultiplier = Mathf.Max(0.05f, cooldownMultiplier);
        bonusPierce = Mathf.Max(0, additionalPierce);
        masteryDamageMultiplier = Mathf.Max(0f, masteryDamage);
        masteryRangeMultiplier = Mathf.Max(0f, masteryRange);
    }
}
