using System;
using UnityEngine;

namespace Seondong.IntegrationGame
{
    [DisallowMultipleComponent]
    public sealed class IntegrationEliteSpawner : MonoBehaviour
    {
        [SerializeField] private RunDirector runDirector;
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private Camera spawnCamera;
        [SerializeField] private EnemyData charger;
        [SerializeField] private EnemyData summoner;
        private readonly bool[] requested = new bool[2];
        public int ChargerSpawnCount { get; private set; }
        public int SummonerSpawnCount { get; private set; }
        public int MissingCount { get; private set; }
        public event Action<EliteKind, Enemy> Spawned;

        private void OnEnable()
        {
            if (runDirector != null) runDirector.EliteSpawnRequested += OnRequested;
        }
        private void OnDisable()
        {
            if (runDirector != null) runDirector.EliteSpawnRequested -= OnRequested;
        }
        private void OnRequested(EliteKind kind)
        {
            int index = (int)kind;
            if (index < 0 || index >= requested.Length || requested[index]) return;
            requested[index] = true;
            var data = kind == EliteKind.Charger ? charger : summoner;
            if (data == null || data.Prefab == null || enemyManager == null || spawnCamera == null)
            {
                MissingCount++;
                Debug.LogError($"[Integration Elite] Missing binding: {kind}", this);
                return;
            }
            float halfHeight = spawnCamera.orthographicSize;
            float halfWidth = halfHeight * spawnCamera.aspect;
            Vector2 center = spawnCamera.transform.position;
            Vector2 position = center + (index == 0 ? new Vector2(halfWidth + 1, 0) : new Vector2(-halfWidth - 1, 0));
            Enemy enemy = enemyManager.Spawn(data, position);
            if (enemy == null) { MissingCount++; Debug.LogError("[Integration Elite] Spawn failed: " + kind); return; }
            var difficulty = DifficultyRules.Evaluate(runDirector.ElapsedCombatTime);
            enemy.ApplyDifficulty(difficulty.HealthMultiplier, difficulty.DamageMultiplier);
            if (kind == EliteKind.Charger) ChargerSpawnCount++; else SummonerSpawnCount++;
            Debug.Log($"[Integration Elite] Spawned={kind}; data={data.name}; seconds={runDirector.ElapsedCombatTime:F2}");
            Spawned?.Invoke(kind, enemy);
        }
    }
}
