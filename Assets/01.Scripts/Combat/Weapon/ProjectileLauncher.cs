using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

// 투사체 생성, 관리
public class ProjectileLauncher : MonoBehaviour
{

    [SerializeField] private EnemyManager enemyManager;

    // 공격 so에서 프리팹 미지정시 사용할 기본 프리팹
    [SerializeField] private Projectile defaultProjectilePrefab;

    // 적 투사체가 때릴 대상. 비워두면 Awake가 같은 오브젝트의 Health를 쓴다
    [SerializeField] private Health playerHealth;

    // 플레이어 피격 반경
    //  PlayerContactDamage의 Check Radius와 같은 값을 따로 들고 있음.
    //  단일 원본화가 필요하나 그러면 이 컴포넌트가 PlayerContactDamage를
    //  참조하게 되므로 지금은 두 값을 인스펙터에 나란히 두기
    [SerializeField] private float playerHitRadius = 0.4f;

    private readonly List<Projectile> activeProjectiles = new List<Projectile>(64);

    // 프리팹마다 풀을 따로 두기. 섞이면 다른 마법의 투사체가 나옴
    private readonly Dictionary<Projectile, ObjectPool<Projectile>>
        pools = new Dictionary<Projectile, ObjectPool<Projectile>>();

    // 원소별 적중 횟수. 씬이 다시 로드되면 이 컴포넌트와 함께 새로 만들어짐
    private readonly ElementHitCounter hitCounter = new ElementHitCounter();

    // 번개 8레벨 낙뢰.
    //
    // 타이머와 해금을 따로 둠.
    private bool lightningStormUnlocked;
    private float lightningStormTimer = ElementReactionValues.LightningStormIntervalSeconds;

    // [연동:Combat] 검증용. 다음 낙뢰까지 남은 시간과 해금 여부
    public float LightningStormTimer => lightningStormTimer;
    public bool IsLightningStormUnlocked => lightningStormUnlocked;

    public int ActiveCount => activeProjectiles.Count;


    private void Awake()
    {
        if (enemyManager == null)
        {
            Debug.LogError("[ProjectileLauncher] EnemyManager 미연결. 투사체 판정을 비활성화합니다.", this);

            enabled = false;

            return;
        }

        // 적 행동 컴포넌트는 프리팹이라 씬의 이 런처를 직렬화로 받을 수 없다
        // EnemyManager가 Spawn에서 넘겨주도록 여기서 스스로 등록한다
        //
        // 객체 참조는 이 등록으로 양방향이 된다
        // 얻는 것은 새 직렬화 참조가 생기지 않는 것이다.
        // EnemyManager에 [SerializeField]를 두면 통합 씬에도 배선이 하나 늘어난다
        enemyManager.SetProjectileLauncher(this);

        // 이 컴포넌트는 Player 오브젝트에 붙어 있고 같은 오브젝트에 Health가 있다
        // 인스펙터로 따로 지정했으면 그 값을 그대로 쓴다
        if (playerHealth == null)
        {
            playerHealth = GetComponent<Health>();
        }

        if (playerHealth == null)
        {
            // 플레이어 공격은 정상 동작하므로 컴포넌트를 끄지 않는다
            // 적 투사체만 발사되지 않는다
            Debug.LogWarning("[ProjectileLauncher] Player Health 미연결. 적 투사체가 발사되지 않습니다.", this);
        }
    }


    /// <summary>
    /// 투사체를 발사한다. 마법 SO의 Execute 안에서 호출.
    /// 투사체는 풀에서 재사용
    /// Instantiate를 직접 부르지 말 것
    /// </summary>

    // [연동:성장]
    public void Fire(in ProjectileSpec spec, Vector2 origin, Vector2 direction)
    {

        // Spec에 프리팹 있으면 우선 사용
        // 없으면 defaultProjectilePrefab 사용
        Projectile prefab = spec.Prefab != null ? spec.Prefab : defaultProjectilePrefab;

        if (prefab == null)
        {
            return;
        }

        Projectile projectile = GetPool(prefab).Get();

        projectile.Launch(spec, origin, direction);

        projectile.gameObject.SetActive(true);

        activeProjectiles.Add(projectile);

        // 대지 8레벨 낙석.
        // 탄이 실제로 나간 뒤에 셈. 프리팹이 없어 발사가 취소되면 세지 않음
        // 빗나가도 세고, 관통으로 여럿을 맞혀도 한 발은 1회.
        if (spec.Element == MagicElement.Earth
            && spec.SkillLevel >= ElementReactionValues.AwakeningUnlockLevel
            && hitCounter.RegisterAwakeningHit(spec.Element))
        {
            enemyManager.ResolveRockfall();
        }

        // 번개 8레벨 낙뢰 해금. 
        if (spec.Element == MagicElement.Lightning
            && spec.SkillLevel >= ElementReactionValues.AwakeningUnlockLevel)
        {
            lightningStormUnlocked = true;
        }
    }

    /// <summary>
    /// 적이 플레이어에게 투사체를 발사한다. 소환술사 원거리탄, 보스 부채꼴이 사용.
    /// 풀은 플레이어 투사체와 같은 것을 쓴다. 프리팹이 다르면 풀도 자동으로 분리된다
    /// </summary>
    public void FireAtPlayer(in ProjectileSpec spec, Vector2 origin, Vector2 direction)
    {
        if (playerHealth == null)
        {
            return;
        }

        Projectile prefab = spec.Prefab != null ? spec.Prefab : defaultProjectilePrefab;

        if (prefab == null)
        {
            return;
        }

        Projectile projectile = GetPool(prefab).Get();

        projectile.LaunchAtPlayer(spec, origin, direction, playerHealth, playerHitRadius);

        projectile.gameObject.SetActive(true);

        activeProjectiles.Add(projectile);
    }

    // 해당 프리팹의 풀을 반환. 없으면 만들어 등록
    private ObjectPool<Projectile> GetPool(Projectile prefab)
    {
        if (pools.TryGetValue(prefab, out ObjectPool<Projectile> existing))
        {
            return existing;
        }

        ObjectPool<Projectile> pool = new ObjectPool<Projectile>(
            createFunc: () => CreateProjectile(prefab),
            actionOnRelease: DeactivateProjectile,
            actionOnDestroy: DestroyProjectile);

        pools.Add(prefab, pool);

        return pool;
    }


    // 풀이 비어 있을 때 호출된다
    private Projectile CreateProjectile(Projectile prefab)
    {
        Projectile created = Instantiate(prefab);

        // 반납할 풀을 찾기 위해 출신 프리팹을 기억시킴
        created.SetSourcePrefab(prefab);

        // 활성화는 Fire가 Launch 뒤에 함
        created.gameObject.SetActive(false);

        return created;
    }


    // 풀 최대 크기 초과 또는 풀 정리 시 호출
    private void DeactivateProjectile(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
    }


    // 풀이 넘쳐서 버릴 때 호출
    private void DestroyProjectile(Projectile projectile)
    {
        Destroy(projectile.gameObject);
    }


    void Update()
    {

        float deltaTime = Time.deltaTime;

        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            Projectile projectile = activeProjectiles[i];

            // 끝난 투사체를 목록에서 제거하고 풀에 반납
            if (projectile == null || !projectile.IsActive)
            {
                RemoveAtSwapBack(i);

                if (projectile != null)
                {
                    pools[projectile.SourcePrefab].Release(projectile);

                }

                continue;

            }

            // 개별 Projectile.Update 대신 Launcher에서 Tick 호출
            projectile.Tick(deltaTime, enemyManager, hitCounter);

        }

        // 투사체 뒤에 둔다. 이번 프레임에 막 붙은 표식도 대상에 들어감
        TickLightningStorm(deltaTime);
    }

    // 번개 8레벨 낙뢰 주기.
    // 대상이 하나도 없어도 격자는 그대로 흘러간다. 다음 기회는 6초 뒤
    // enemyManager 는 Awake 에서 확인하고 없으면 컴포넌트를 껐으므로 null 검사 x
    private void TickLightningStorm(float deltaTime)
    {
        lightningStormTimer -= deltaTime;

        if (lightningStormTimer > 0f)
        {
            return;
        }

        // 남은 시간을 더해 다음 주기로 넘김. 매번 6초로 덮어쓰면 주기가 조금씩 밀림
        lightningStormTimer += ElementReactionValues.LightningStormIntervalSeconds;

        if (lightningStormTimer <= 0f)
        {
            lightningStormTimer = ElementReactionValues.LightningStormIntervalSeconds;
        }

        if (!lightningStormUnlocked)
        {
            return;
        }

        enemyManager.ResolveLightningStorm();
    }

    // 순서가 필요 없어 마지막 항목을 덮어쓰는 방식으로 제거
    private void RemoveAtSwapBack(int index)
    {
        int lastIndex = activeProjectiles.Count - 1;

        activeProjectiles[index] = activeProjectiles[lastIndex];

        activeProjectiles.RemoveAt(lastIndex);

    }
}
