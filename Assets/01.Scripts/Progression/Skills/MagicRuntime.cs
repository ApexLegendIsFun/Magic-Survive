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

    // 발사할 때 ProjectileSpec 으로 넘길 특화 스냅샷
    public ElementSpecializationBonus Specialization { get; private set; } = ElementSpecializationBonus.None;
    public float Cooldown { get; private set; }
    public float Damage { get; private set; }
    public int PierceCount { get; private set; }
    public float Range { get; }
    public float Speed { get; }
    public float MaxDistance { get; }
    public float HitRadius { get; }

    // 기존 호출부 호환용. 특화 없이 레벨만 반영
    public void SetSkillLevel(int level) => SetSkillLevel(level, ElementSpecializationBonus.None);

    public void SetSkillLevel(int level, ElementSpecializationBonus specialization)
    {
        ElementSkillStats stats = MagicContentCatalog.GetStats(Element, level);
        SkillLevel = level;
        Specialization = specialization;
        // 기본 성장(레벨 배율)을 먼저 적용하고 그 결과에 특화 배율을 곱함
        Damage = stats.Damage * specialization.DamageMultiplier;
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
            PierceCount,
            Element,      // 선동님 확인으로 추가함. 원소 3중첩 발동관련 로직
            SkillLevel,
            Specialization
            );

        context.Launcher.Fire(spec, context.Origin, direction);
        return true;
    }

}
