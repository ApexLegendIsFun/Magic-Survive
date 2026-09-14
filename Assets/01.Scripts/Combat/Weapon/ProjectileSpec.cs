using UnityEngine;

// 투사체 1회 발사에 필요한 수치 묶음
// 공격 so -> ProjectileLauncher -> Projectile 순서 전달
// [연동:성장] 마법데이터가 이 값을 채운 후 ProjectileLauncher.Fire에 넘김
public readonly struct ProjectileSpec
{

    public readonly Projectile Prefab;

    public readonly float Damage;
    public readonly float Speed;
    public readonly float MaxDistance;
    public readonly float HitRadius;

    // 0 = 첫 명중에 소멸, 1 = 하나 관통 후 2번째 적에서 소멸
    public readonly int PierceCount;

    // [연동:성장] 적중 시 적용할 표식의 원소와, 3레벨 반응 해금 판정용 스킬 레벨
    // SkillLevel 0 = 미지정. 0이면 표식을 적용하지 않음
    // Element 기본값이 Fire인 것은 MagicElement에 None이 없어서이고,
    // 판정은 전부 SkillLevel로 한다. Element 기본값에 의미를 두지 말 것
    public readonly MagicElement Element;
    public readonly int SkillLevel;

    // 기존 6인자 호출부 호환용. 표식 정보 없음
    // 현재 호출부 2곳: MagicRuntime.Execute, _Test/SimpleProjectileAttack.Execute
    public ProjectileSpec(Projectile prefab, float damage, float speed, float maxDistance, float hitRadius, int pierceCount)
        : this(prefab, damage, speed, maxDistance, hitRadius, pierceCount, MagicElement.Fire, 0)
    {
    }

    public ProjectileSpec(Projectile prefab, float damage, float speed, float maxDistance, float hitRadius, int pierceCount, MagicElement element, int skillLevel)
    {
        Prefab = prefab;
        Damage = damage;
        Speed = speed;
        MaxDistance = maxDistance;
        HitRadius = hitRadius;
        PierceCount = pierceCount;

        Element = element;
        SkillLevel = skillLevel;
    }
}