using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// BossSpawner 연결이 아직 안 된 테스트 씬에서, 보스를 즉시 강제 스폰하기 위한 임시 디버그.
/// F10 = 화면 중앙 근처에 보스 1마리 스폰.
/// </summary>
public class BossSpawnDebugTrigger : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private EnemyData bossData; // IsBoss가 true인 EnemyData 에셋
    [SerializeField] private Vector2 spawnOffset = new Vector2(3f, 0f); // 플레이어 기준 상대 위치

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f10Key.wasPressedThisFrame)
        {
            SpawnBoss();
        }
    }

    private void SpawnBoss()
    {
        if (enemyManager == null || bossData == null)
        {
            Debug.LogWarning("[BossSpawnDebugTrigger] enemyManager / bossData 중 비어있는 게 있음");
            return;
        }

        Vector2 position = spawnOffset; // 필요하면 플레이어 Transform 기준으로 바꿔도 됨
        Enemy enemy = enemyManager.Spawn(bossData, position);

        if (enemy != null)
        {
            Debug.Log($"[BossSpawnDebugTrigger] 보스 스폰 @ {position}, IsBoss={enemy.IsBoss}");
        }
    }
}