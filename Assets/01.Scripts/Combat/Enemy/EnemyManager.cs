using UnityEngine;
using System.Collections.Generic;

// 현재 살아있는 적 관리
// 적 이동, 타겟 검색, 적 생성 관련
public class EnemyManager : MonoBehaviour
{

    [SerializeField] private Transform playerTransform;

    private readonly List<Enemy> activeEnemies = new List<Enemy>();

    // [연동:UI] HUD 적 수 표시, 디버그
    public int ActiveCount => activeEnemies.Count;

    public void Register(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        activeEnemies.Add(enemy);
    }


    // 지정 위치 기준 가장 가까운 적 찾기
    // 자동공격 타겟 검색용
    public Enemy FindNearest(Vector2 from, float maxRange)
    {

        // 제곱근 생략 위해 제곱 거리로 비교. 매 프레임 X 마법 수만큼 돌아감
        float bestSqrDistance = maxRange * maxRange;

        Enemy nearest = null;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];

            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            Vector2 enemyPosition = enemy.transform.position;

            float sqrDistance = (enemyPosition - from).sqrMagnitude;

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;

                nearest = enemy;
            }
        }

        return nearest;
    }

    private void Awake()
    {
        if (playerTransform == null)
        {
            Debug.LogError("[EnemyManager] Player Transform 미연결. 적이 움직이지 않습니다.", this);
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        Vector2 playerPosition = playerTransform.position;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeEnemies[i];

            // 죽거나 삭제된 적을 목록에서 제거
            if (enemy == null || !enemy.IsAlive)
            {
                RemoveAtSwapBack(i);
                continue;
            }

            // Manager에서 이동 처리
            enemy.Tick(deltaTime, playerPosition);

        }
    }

    // 역순 순회 중 List.Remove로 앞쪽 지우면 뒤 항목이 당겨져 하나 건너 뜀
    // 그래서 지금 보고 있는 인덱스에만 마지막 항목을 덮어쓰는 방식으로 지움
    private void RemoveAtSwapBack(int index)
    {
        int lastIndex = activeEnemies.Count - 1;

        activeEnemies[index] = activeEnemies[lastIndex];

        activeEnemies.RemoveAt(lastIndex);
    }


    // [연동:스폰] 스폰 타이밍 결정 후 함수 호출
    // 외부에서 Enemy 직접 Instantiate 시 풀링 우회하게 됨
    // 생성, EnemyData 적용, 관리목록 등록을 처리
    public Enemy Spawn(EnemyData data, Vector2 position)
    {
        if (data == null || data.Prefab == null)
        {
            return null;
        }

        Enemy enemy = Instantiate(data.Prefab, position, Quaternion.identity); // [교체:풀링]

        enemy.Initialize(data);

        Register(enemy);

        return enemy;

    }


    // [연동:스폰] 종료, 재시작 시 호출
    public void DespawnAll()
    {

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = activeEnemies[i];

            if (enemy != null)
            {
                Destroy(enemy.gameObject); // [교체:풀링]
            }
        }

        activeEnemies.Clear();


    }
}