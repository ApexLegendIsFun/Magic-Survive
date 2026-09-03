using UnityEngine;

[CreateAssetMenu(
    fileName = "ProjectileMagicDefinition",
    menuName = "Magic/Projectile Magic Definition")]
public sealed class ProjectileMagicDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private MagicId magicId = MagicId.FireBolt;
    [SerializeField] private MagicElement element = MagicElement.Fire;

    [Header("Projectile")]
    [SerializeField] private Projectile projectilePrefab;

    [Header("Attack")]
    [SerializeField, Min(0.05f)] private float cooldown = 0.8f;
    [SerializeField, Min(0f)] private float range = 8f;
    [SerializeField, Min(0f)] private float damage = 3f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float speed = 12f;
    [SerializeField, Min(0f)] private float maxDistance = 10f;
    [SerializeField, Min(0f)] private float hitRadius = 0.25f;
    [SerializeField, Min(0)] private int pierceCount;

    public MagicId MagicId => magicId;
    public MagicElement Element => element;
    public Projectile ProjectilePrefab => projectilePrefab;
    public float Cooldown => cooldown;
    public float Range => range;
    public float Damage => damage;
    public float Speed => speed;
    public float MaxDistance => maxDistance;
    public float HitRadius => hitRadius;
    public int PierceCount => pierceCount;

    private void OnValidate()
    {
        cooldown = Mathf.Max(0.05f, cooldown);
        range = Mathf.Max(0f, range);
        damage = Mathf.Max(0f, damage);
        speed = Mathf.Max(0f, speed);
        maxDistance = Mathf.Max(0f, maxDistance);
        hitRadius = Mathf.Max(0f, hitRadius);
        pierceCount = Mathf.Max(0, pierceCount);
    }
}
