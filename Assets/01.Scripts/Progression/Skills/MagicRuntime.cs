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

    public MagicRuntime(ProjectileMagicDefinition definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        Element = definition.Element;
        projectilePrefab = definition.ProjectilePrefab;
        Cooldown = definition.Cooldown;
        Range = definition.Range;
        Damage = definition.Damage;
        Speed = definition.Speed;
        MaxDistance = definition.MaxDistance;
        HitRadius = definition.HitRadius;
        PierceCount = definition.PierceCount;
    }

    public MagicElement Element { get; }
    public float Cooldown { get; private set; }
    public float Range { get; }
    public float Damage { get; private set; }
    public float Speed { get; }
    public float MaxDistance { get; }
    public float HitRadius { get; }
    public int PierceCount { get; private set; }

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
                Damage += DamageUpgradeAmount;
                break;

            case SkillUpgradeKind.FireRate:
                Cooldown *= FireRateCooldownMultiplier;
                break;

            case SkillUpgradeKind.Pierce:
                PierceCount += 1;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "지원하지 않는 강화 종류입니다.");
        }
    }
}
