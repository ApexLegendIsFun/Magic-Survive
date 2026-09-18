using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// EliteSpawner 연결이 아직 안 된 테스트 씬에서, 소환술사를 즉시 강제 스폰하기 위한 임시 디버그.
/// F5 = 화면 밖 랜덤 위치에 소환술사 1마리 스폰.
/// </summary>
public class SummonerSpawnDebugTrigger : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private EnemyData summonerData; // Enemy_Summoner에 대응하는 EnemyData 에셋
    [SerializeField] private float spawnMargin = 1f;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SpawnSummoner();
        }
    }

    private void SpawnSummoner()
    {
        if (enemyManager == null || summonerData == null || mainCamera == null)
        {
            Debug.LogWarning("[SummonerSpawnDebugTrigger] enemyManager / summonerData / camera 중 비어있는 게 있음");
            return;
        }

        Vector2 position = GetRandomOffScreenPosition();
        Enemy enemy = enemyManager.Spawn(summonerData, position);

        if (enemy != null)
        {
            Debug.Log($"[SummonerSpawnDebugTrigger] 소환술사 스폰 @ {position}");
        }
    }

    private Vector2 GetRandomOffScreenPosition()
    {
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;
        Vector2 center = mainCamera.transform.position;

        switch (Random.Range(0, 4))
        {
            case 0: return center + new Vector2(Random.Range(-halfWidth, halfWidth), halfHeight + spawnMargin);
            case 1: return center + new Vector2(Random.Range(-halfWidth, halfWidth), -halfHeight - spawnMargin);
            case 2: return center + new Vector2(-halfWidth - spawnMargin, Random.Range(-halfHeight, halfHeight));
            default: return center + new Vector2(halfWidth + spawnMargin, Random.Range(-halfHeight, halfHeight));
        }
    }
}