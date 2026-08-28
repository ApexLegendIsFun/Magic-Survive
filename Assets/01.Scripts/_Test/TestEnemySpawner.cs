using UnityEngine;


// [임시]  임시 스폰. 웨이브 시스템 완성 시 삭제
// 화면 밖 4방향 랜덤 스폰
public class TestEnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;

    // 테스트시 랜덤으로 생성할 EnemyData 목록
    [SerializeField] private EnemyData[] spawnTable;

    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private float spawnMargin = 1f;


    private Camera mainCamera;
    private float spawnTimer;


    private void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {

        // 스폰에 필요한 값 없으면 실행 안 합니다.
        if (mainCamera == null || enemyManager == null || spawnTable == null || spawnTable.Length == 0)
        {
            return;
        }

        // 너무 짧은 간격 스폰 방지
        float interval = Mathf.Max(0.02f, spawnInterval);


        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnOne();

            spawnTimer += interval;
        }

    }


    private void SpawnOne()
    {
        // 테스트용 랜덤 선택 

        EnemyData data = spawnTable[Random.Range(0, spawnTable.Length)];


        // [연동:스폰]
        enemyManager.Spawn(data, GetRandomOffScreenPosition());


    }


    // 카메라 화면 밖 4방향 중 랜덤 위치를 반환
    private Vector2 GetRandomOffScreenPosition()
    {
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;

        Vector2 cameraCenter = mainCamera.transform.position;

        int side = Random.Range(0, 4);

        float x;
        float y;

        switch (side)
        {
            case 0:     // 위
                x = Random.Range(-halfWidth, halfWidth);
                y = halfHeight + spawnMargin;
                break;
            case 1:     // 아래
                x = Random.Range(-halfWidth, halfWidth);
                y = -halfHeight - spawnMargin;
                break;
            case 2:     // 왼쪽
                x = -halfWidth - spawnMargin;
                y = Random.Range(-halfHeight, halfHeight);
                break;
            default:    // 오른쪽
                x = halfWidth + spawnMargin;
                y = Random.Range(-halfHeight, halfHeight);
                break;
        }

        return cameraCenter + new Vector2(x, y);
    }

}
