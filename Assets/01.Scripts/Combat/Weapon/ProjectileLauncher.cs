using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

// 투사체 생성, 관리
public class ProjectileLauncher : MonoBehaviour
{

    // 투사체가 판정할 레이어
    [SerializeField] private LayerMask enemyLayers;

    // 공격 so에서 프리팹 미지정시 사용할 기본 프리팹
    [SerializeField] private Projectile defaultProjectilePrefab;

    private readonly List<Projectile> activeProjectiles = new List<Projectile>(64);

    // 프리팹마다 풀을 따로 두기. 섞이면 다른 마법의 투사체가 나옴
    private readonly Dictionary<Projectile, ObjectPool<Projectile>>
        pools = new Dictionary<Projectile, ObjectPool<Projectile>>();


    // 적 레이어만 검사하도록 하는 필터
    private ContactFilter2D enemyFilter;


    public int ActiveCount => activeProjectiles.Count;


    private void Awake()
    {
        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyLayers);
        enemyFilter.useTriggers = true; //Trigger Collider를 피격 판정에 포함
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
            projectile.Tick(deltaTime, enemyFilter);

        }

    }

    // 순서가 필요 없어 마지막 항목을 덮어쓰는 방식으로 제거
    private void RemoveAtSwapBack(int index)
    {
        int lastIndex = activeProjectiles.Count - 1;

        activeProjectiles[index] = activeProjectiles[lastIndex];

        activeProjectiles.RemoveAt(lastIndex);

    }
}
