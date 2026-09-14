using System;
using UnityEngine;

public sealed class MagicRuntime : IAttackSource
{
    private readonly Projectile projectilePrefab;
    public MagicRuntime(ProjectileMagicDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        Id = definition.MagicId;
        Element = definition.Element;
        projectilePrefab = definition.ProjectilePrefab;
        Range = definition.Range;
        Speed = definition.Speed;
        MaxDistance = definition.MaxDistance;
        HitRadius = definition.HitRadius;
        SetSkillLevel(1);
    }
    public MagicId Id { get; }
    public MagicElement Element { get; }
    public int SkillLevel { get; private set; }
    public float Cooldown { get; private set; }
    public float Damage { get; private set; }
    public int PierceCount { get; private set; }
    public float Range { get; }
    public float Speed { get; }
    public float MaxDistance { get; }
    public float HitRadius { get; }
    public void SetSkillLevel(int level)
    {
        ElementSkillStats stats = MagicContentCatalog.GetStats(Element, level);
        SkillLevel = level;
        Damage = stats.Damage;
        Cooldown = stats.Cooldown;
        PierceCount = stats.PierceCount;
    }
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

}
