using UnityEngine;
using System.Collections.Generic;


// 투사체 1개 이동, 피격, 관통 처리
public class Projectile : MonoBehaviour
{
    // OverlapCircle 결과 재사용하기. 모든 투사체가 공유
    // TODO: 16개 고정. 광역 마법 도입 시 크기 재검토
    private static readonly Collider2D[] HitBuffer = new Collider2D[16];

    // 관통 중 같은 적을 매 프레임 다시 때리기 방지
    private readonly List<IDamageable> alreadyHit = new List<IDamageable>(4);

    private ProjectileSpec spec;
    private Vector2 direction;

    private float traveledDistance;

    private int remainingPierce;

    private bool isActive;
    public bool IsActive => isActive;

    // 투사체 생성 직ㅎ ㅜ초기화
    // 추후 풀링 적용 시 재사용 가능
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
    public void Tick(float deltaTime, in ContactFilter2D enemyFilter)
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
        if (CheckHits(nextPosition, enemyFilter))
        {
            return;
        }

        // 최대 이동 거리 도달 시 종료
        if (traveledDistance >= spec.MaxDistance)
        {
            isActive = false;
        }
    }


    private bool CheckHits(Vector2 position, in ContactFilter2D enemyFilter)
    {
        int hitCount = Physics2D.OverlapCircle(position, spec.HitRadius, enemyFilter, HitBuffer);

        for (int i = 0; i < hitCount; i++)
        {

            if (!HitBuffer[i].TryGetComponent(out IDamageable target))
            {
                continue;
            }

            // 이미 죽은 적 & 이미 맞춘 적 제외
            if (!target.IsAlive || alreadyHit.Contains(target))
            {
                continue;
            }

            alreadyHit.Add(target);

            //데미지 적용
            target.TakeDamage(spec.Damage);

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
