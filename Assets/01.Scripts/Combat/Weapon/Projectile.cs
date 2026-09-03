using UnityEngine;
using System.Collections.Generic;


// 투사체 1개 이동, 피격, 관통 처리
public class Projectile : MonoBehaviour
{
    // 반경 질의 결과 버퍼. 인스턴스 필드
    private readonly List<Enemy> hitBuffer = new List<Enemy>(16);

    // 관통 중 같은 적을 매 프레임 다시 때리기 방지
    private readonly List<Enemy> alreadyHit = new List<Enemy>(4);


    // 이 투사체를 만든 프리팹. 어느 풀로 반납할지 찾는 데 쓰임
    private Projectile sourcePrefab;

    public Projectile SourcePrefab => sourcePrefab;

    private ProjectileSpec spec;
    private Vector2 direction;

    private float traveledDistance;

    private int remainingPierce;

    private bool isActive;
    public bool IsActive => isActive;


    public void SetSourcePrefab(Projectile prefab)
    {
        sourcePrefab = prefab;
    }

    // 투사체 발사 전 상태 초기화
    // 풀에서 재사용될 때도 매번 호출
    public void Launch(in ProjectileSpec launchSpec, Vector2 origin, Vector2 launchDirection)
    {
        spec = launchSpec;
        direction = launchDirection.normalized;
        traveledDistance = 0f;
        remainingPierce = launchSpec.PierceCount;

        // 이전 발사에 맞은 적 기록 초기화
        alreadyHit.Clear();

        isActive = true;

        transform.position = origin;

        // 스프라이트 오른쪽 방향을 진행방향으로 맞춤
        transform.right = direction;
    }

    // ProjectileLauncher에서 매 프레임 호출
    public void Tick(float deltaTime, EnemyManager enemyManager)
    {
        if (!isActive)
        {
            return;
        }

        // TODO: step이 HitRadius*2보다 커지면 적 통과함. 속도 향상 시에 재검토
        float step = spec.Speed * deltaTime;
 
        Vector2 nextPosition = (Vector2)transform.position + direction * step;

        transform.position = nextPosition;

        traveledDistance += step;

        // 적 명중으로 소멸시 이번 Tick 종료
        if (CheckHits(nextPosition, enemyManager))
        {
            return;
        }

        // 최대 이동 거리 도달 시 종료
        if (traveledDistance >= spec.MaxDistance)
        {
            isActive = false;
        }
    }


    private bool CheckHits(Vector2 position, EnemyManager enemyManager)
    {
        enemyManager.FindOverlappingEnemies(position, spec.HitRadius, hitBuffer);

        for (int i = 0; i < hitBuffer.Count; i++)
        {

            Enemy enemy = hitBuffer[i];

            if (alreadyHit.Contains(enemy))
            {
                continue;
            }

            alreadyHit.Add(enemy);

            enemy.TakeDamage(spec.Damage);

            // PierceCount 0이며 첫 명중 시 소멸
            if (remainingPierce <= 0)
            {
                isActive = false;
                return true;
            }

            remainingPierce--;

        }


        return false;




    }

}
