using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpawnDirector : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private RunDirector runDirector;
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private EnemyData[] normalEnemies = new EnemyData[4];
    [SerializeField, Min(0f)] private float spawnMargin = 1f;

    private float spawnTimer;

    public DifficultySnapshot CurrentDifficulty => DifficultyRules.Evaluate(
        runDirector != null ? runDirector.ElapsedCombatTime : 0f);

    public event Action<Enemy, NormalEnemyRole, DifficultySnapshot> EnemySpawned;

    private void Awake()
    {
        if (spawnCamera == null)
        {
            spawnCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (enemyManager == null || gameFlowController == null || runDirector == null ||
            spawnCamera == null || gameFlowController.State != GameFlowState.Playing)
        {
            return;
        }

        DifficultySnapshot difficulty = CurrentDifficulty;
        if (enemyManager.ActiveCount >= difficulty.EnemyCap)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        spawnTimer += Mathf.Max(0.02f, difficulty.SpawnInterval);
        SpawnOne(difficulty);
    }

    private void SpawnOne(DifficultySnapshot difficulty)
    {
        if (normalEnemies == null || normalEnemies.Length == 0)
        {
            return;
        }

        if (!TryChooseRole(runDirector.ElapsedCombatTime, out NormalEnemyRole role))
        {
            return;
        }

        EnemyData data = normalEnemies[(int)role];
        Enemy enemy = enemyManager.Spawn(data, GetRandomOffScreenPosition());
        if (enemy != null)
        {
            EnemySpawned?.Invoke(enemy, role, difficulty);
        }
    }

    private bool TryChooseRole(float elapsedTime, out NormalEnemyRole role)
    {
        if (normalEnemies == null || normalEnemies.Length == 0)
        {
            role = NormalEnemyRole.Basic;
            return false;
        }

        float availableWeight = 0f;
        for (int index = 0; index < normalEnemies.Length && index < 4; index++)
        {
            if (normalEnemies[index] != null)
            {
                availableWeight += DifficultyRules.GetNormalizedSpawnWeight(
                    (NormalEnemyRole)index,
                    elapsedTime);
            }
        }

        if (availableWeight <= 0f)
        {
            role = NormalEnemyRole.Basic;
            return false;
        }

        float roll = UnityEngine.Random.value * availableWeight;
        for (int index = 0; index < normalEnemies.Length && index < 4; index++)
        {
            if (normalEnemies[index] == null)
            {
                continue;
            }

            roll -= DifficultyRules.GetNormalizedSpawnWeight((NormalEnemyRole)index, elapsedTime);
            if (roll <= 0f)
            {
                role = (NormalEnemyRole)index;
                return true;
            }
        }

        role = NormalEnemyRole.Basic;
        return normalEnemies.Length > 0 && normalEnemies[0] != null;
    }

    private Vector2 GetRandomOffScreenPosition()
    {
        float halfHeight = spawnCamera.orthographicSize;
        float halfWidth = halfHeight * spawnCamera.aspect;
        Vector2 center = spawnCamera.transform.position;

        switch (UnityEngine.Random.Range(0, 4))
        {
            case 0:
                return center + new Vector2(UnityEngine.Random.Range(-halfWidth, halfWidth), halfHeight + spawnMargin);
            case 1:
                return center + new Vector2(UnityEngine.Random.Range(-halfWidth, halfWidth), -halfHeight - spawnMargin);
            case 2:
                return center + new Vector2(-halfWidth - spawnMargin, UnityEngine.Random.Range(-halfHeight, halfHeight));
            default:
                return center + new Vector2(halfWidth + spawnMargin, UnityEngine.Random.Range(-halfHeight, halfHeight));
        }
    }
}
