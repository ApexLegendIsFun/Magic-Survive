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
    [SerializeField, Min(0f)] private float range = 8f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float speed = 12f;
    [SerializeField, Min(0f)] private float maxDistance = 10f;
    [SerializeField, Min(0f)] private float hitRadius = 0.25f;

    public MagicId MagicId => magicId;
    public MagicElement Element => element;
    public Projectile ProjectilePrefab => projectilePrefab;
    public float Cooldown => MagicContentCatalog.GetStats(element, 1).Cooldown;
    public float Range => range;
    public float Damage => MagicContentCatalog.GetStats(element, 1).Damage;
    public float Speed => speed;
    public float MaxDistance => maxDistance;
    public float HitRadius => hitRadius;
    public int PierceCount => MagicContentCatalog.GetStats(element, 1).PierceCount;

    private void OnValidate()
    {
        range = Mathf.Max(0f, range);
        speed = Mathf.Max(0f, speed);
        maxDistance = Mathf.Max(0f, maxDistance);
        hitRadius = Mathf.Max(0f, hitRadius);
    }
}
