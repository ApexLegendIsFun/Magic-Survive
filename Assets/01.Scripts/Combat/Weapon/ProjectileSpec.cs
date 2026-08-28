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

    public ProjectileSpec(Projectile prefab, float damage, float speed, float maxDistance, float hitRadius, int pierceCount)
    {
        Prefab = prefab;
        Damage = damage;
        Speed = speed;
        MaxDistance = maxDistance;
        HitRadius = hitRadius;
        PierceCount = pierceCount;
    }




}
