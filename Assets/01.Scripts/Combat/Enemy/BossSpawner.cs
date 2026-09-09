using System;
using UnityEngine;

// 8분 보스 등장과 처치 보고
// RunDirector가 보스를 요청하면 EnemyManager로 스폰하고,
// 보스가 죽으면 RunDirector에 승리를 보고.
//
// SD03' 미수신 상태. Enemy_Boss의 이동속도/접촉 피해/크기/충돌 반경은
// 임시값이며 acceptance 표가 오면 EnemyData와 프리팹에서 교체.
// HP 2000만 문서 확정값. 공격 패턴은 YS12'에서 별도로.
[DisallowMultipleComponent]

public class BossSpawner : MonoBehaviour
{

    [SerializeField] private RunDirector runDirector;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private EnemyData bossData;
    [SerializeField] private Camera spawnCamera;

    // 화면 밖 여유. SpawnDirector와 같은 기준
    [SerializeField, Min(0f)] private float spawnMargin = 1f;

    private Enemy activeBoss;
    private Health activeBossHealth;

    // [연동:UI] 보스 HP 표시, 등장 연출
    public event Action<Enemy> BossSpawned;

    private void Awake()
    {
        if (spawnCamera == null)
        {
            spawnCamera = Camera.main;
        }

        // 하나라도 비어 있으면 조용히 아무 일도 안 일어나므로 여기서 끊기
        if (runDirector == null || enemyManager == null || bossData == null || spawnCamera == null)
        {
            Debug.LogError(
                "[BossSpawner] 필수 참조 미연결. 보스가 등장하지 않습니다. " +
                "Run Director / Enemy Manager / Boss Data / Spawn Camera를 확인하세요.",
                this);

            enabled = false;
        }
    }

    private void OnEnable()
    {
        runDirector.BossSpawnRequested += HandleBossSpawnRequested;
    }

    private void OnDisable()
    {
        if (runDirector != null)
        {
            runDirector.BossSpawnRequested -= HandleBossSpawnRequested;
        }


        UnsubscribeBoss();

    }

    private void HandleBossSpawnRequested()
    {

        // 요청이 두 번 와도 보스를 두 마리 만들지 않음
        if (activeBoss != null)
        {
            return;
        }

        Enemy boss = enemyManager.Spawn(bossData, GetSpawnPosition());

        if (boss == null)
        {
            Debug.LogError("[BossSpawner] 보스 스폰 실패. EnemyData의 Prefab을 확인하세요.", this);
            return;
        }

        activeBoss = boss;

        activeBossHealth = boss.GetComponent<Health>();

        if (activeBossHealth != null)
        {
            activeBossHealth.Died += HandleBossDied;
        }

        BossSpawned?.Invoke(boss);
    }

    private void HandleBossDied()
    {
        UnsubscribeBoss();

        // 승리 전환 판단은 RunDirector가. Boss 상태가 아니면 false가 옴
        if (!runDirector.ReportBossDefeated())
        {
            Debug.LogWarning("[BossSpawner] 보스 처치를 보고했으나 승리로 전환되지 않았습니다.", this);
        }
    }

    private void UnsubscribeBoss()
    {
        if (activeBossHealth != null)
        {
            activeBossHealth.Died -= HandleBossDied;
        }

        activeBossHealth = null;

        activeBoss = null;
    }

    // 화면 밖 네 방향 중 하나
    private Vector2 GetSpawnPosition()
    {
        float halfHeight = spawnCamera.orthographicSize;
        float halfWidth = halfHeight * spawnCamera.aspect;
        Vector2 center = spawnCamera.transform.position;

        switch (UnityEngine.Random.Range(0, 4))
        {

            case 0:
                return center + new Vector2(0f, halfHeight + spawnMargin);

            case 1:
                return center + new Vector2(0f, -halfHeight - spawnMargin);

            case 2: 
                return center + new Vector2(-halfWidth - spawnMargin, 0f);

            default:
                return center + new Vector2(halfWidth + spawnMargin, 0f);
        }
    }
}