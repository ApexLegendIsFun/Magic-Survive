using UnityEngine;
using System.Collections.Generic;

// 투사체 생성, 관리
public class ProjectileLauncher : MonoBehaviour
{

    // 투사체가 판정할 레이어
    [SerializeField] private LayerMask enemyLayers;

    // 공격 so에서 프리팹 미지정시 사용할 기본 프리팹
    [SerializeField] private Projectile defaultProjectilePrefab;

    private readonly List<Projectile> activeProjectiles = new List<Projectile>(64);

    // 적 레이어만 검사하도록 하는 필터
    private ContactFilter2D enemyFilter;


    public int ActiveCount => activeProjectiles.Count;


    private void Awake()
    {
        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyLayers);
        enemyFilter.useTriggers = true; //Trigger Collider를 피격 판정에 포함
    }

    // [연동:성장]
    // 마법 Execute 안에서 이 함수를 호출하여 발사
    public void Fire(in ProjectileSpec spec, Vector2 origin, Vector2 direction)
    {

        // Spec에 프리팹 있으면 우선 사용
        // 없으면 defaultProjectilePrefab 사용
        Projectile prefab = spec.Prefab != null ? spec.Prefab : defaultProjectilePrefab;

        if (prefab == null)
        {
            return;
        }

        Projectile projectile = Instantiate(prefab); // [교체:풀링]

        projectile.Launch(spec, origin, direction);

        activeProjectiles.Add(projectile);
    }



    void Update()
    {

        float deltaTime = Time.deltaTime;

        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            Projectile projectile = activeProjectiles[i];

            // 끝난 투사체를 목록에서 제거
            if (projectile == null || !projectile.IsActive)
            {

                RemoveAtSwapBack(i);

                if (projectile != null)
                {
                    Destroy(projectile.gameObject); // [교체:풀링]
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
