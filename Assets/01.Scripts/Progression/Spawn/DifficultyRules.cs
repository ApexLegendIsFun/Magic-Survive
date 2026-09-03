using UnityEngine;

public enum NormalEnemyRole
{
    Basic = 0,
    Fast = 1,
    Tank = 2,
    Ranged = 3
}

public readonly struct DifficultySnapshot
{
    public DifficultySnapshot(
        float progress,
        float spawnInterval,
        int enemyCap,
        float healthMultiplier,
        float damageMultiplier)
    {
        Progress = progress;
        SpawnInterval = spawnInterval;
        EnemyCap = enemyCap;
        HealthMultiplier = healthMultiplier;
        DamageMultiplier = damageMultiplier;
    }

    public float Progress { get; }
    public float SpawnInterval { get; }
    public int EnemyCap { get; }
    public float HealthMultiplier { get; }
    public float DamageMultiplier { get; }
}

public static class DifficultyRules
{
    public const float GrowthDuration = 480f;
    public const float EnemyRampDuration = 45f;

    private static readonly float[] UnlockTimes = { 0f, 90f, 180f, 270f };
    private static readonly float[] FinalWeights = { 0.35f, 0.30f, 0.20f, 0.15f };

    public static DifficultySnapshot Evaluate(float elapsedTime)
    {
        float progress = Mathf.Clamp01(elapsedTime / GrowthDuration);
        return new DifficultySnapshot(
            progress,
            Mathf.Lerp(1.4f, 0.35f, progress),
            Mathf.RoundToInt(Mathf.Lerp(25f, 100f, progress)),
            Mathf.Lerp(1f, 1.6f, progress),
            Mathf.Lerp(1f, 1.3f, progress));
    }

    public static float GetNormalizedSpawnWeight(NormalEnemyRole role, float elapsedTime)
    {
        int roleIndex = (int)role;
        if (roleIndex < 0 || roleIndex >= FinalWeights.Length)
        {
            return 0f;
        }

        float total = 0f;
        for (int index = 0; index < FinalWeights.Length; index++)
        {
            total += GetRawWeight(index, elapsedTime);
        }

        return total > 0f ? GetRawWeight(roleIndex, elapsedTime) / total : 0f;
    }

    private static float GetRawWeight(int index, float elapsedTime)
    {
        if (index == 0)
        {
            return FinalWeights[index];
        }

        float ramp = Mathf.Clamp01((elapsedTime - UnlockTimes[index]) / EnemyRampDuration);
        return FinalWeights[index] * ramp;
    }
}
