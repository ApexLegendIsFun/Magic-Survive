using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// EliteSpawner 연결이 아직 안 된 테스트 씬에서, 돌진자를 즉시 강제 스폰하기 위한 임시 디버그.
/// F4 = 화면 밖 랜덤 위치에 돌진자 1마리 스폰.
/// 실제 EliteSpawner 배선이 갖춰지면 씬에서 빼세요.
/// </summary>
public class ChargerSpawnDebugTrigger : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private EnemyData chargerData; // 돌진자에 대응하는 EnemyData 에셋
    [SerializeField] private float spawnMargin = 1f;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            SpawnCharger();
        }
    }

    private void SpawnCharger()
    {
        if (enemyManager == null || chargerData == null || mainCamera == null)
        {
            Debug.LogWarning("[ChargerSpawnDebugTrigger] enemyManager / chargerData / camera 중 비어있는 게 있음");
            return;
        }

        Vector2 position = GetRandomOffScreenPosition();
        Enemy enemy = enemyManager.Spawn(chargerData, position);

        if (enemy != null)
        {
            Debug.Log($"[ChargerSpawnDebugTrigger] 돌진자 스폰 @ {position}");
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