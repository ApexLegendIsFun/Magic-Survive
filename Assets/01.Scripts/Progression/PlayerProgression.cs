using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerProgression : MonoBehaviour
{
    private const int ExperiencePerLevel = 5;

    [SerializeField, Min(1)] private int startingLevel = 1;
    [SerializeField, Min(0)] private int startingExperience;

    public int Level { get; private set; }
    public int CurrentExperience { get; private set; }
    public int RequiredExperience => GetRequiredExperience(Level);

    public event Action<int, int> ProgressChanged;
    public event Action<int> LevelChanged;
    public event Action<int> LevelUpRequested;

    private bool isSubscribed;

    private void Awake()
    {
        SetStartingProgress();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        PublishSnapshot();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        long remainingExperience = (long)CurrentExperience + amount;

        while (remainingExperience >= RequiredExperience)
        {
            remainingExperience -= RequiredExperience;
            Level++;

            LevelChanged?.Invoke(Level);
            LevelUpRequested?.Invoke(Level);
        }

        CurrentExperience = (int)remainingExperience;
        ProgressChanged?.Invoke(CurrentExperience, RequiredExperience);
    }

    public void ResetProgression()
    {
        SetStartingProgress();
        PublishSnapshot();
    }

    public static int GetRequiredExperience(int level)
    {
        long requirement = (long)Mathf.Max(1, level) * ExperiencePerLevel;
        return requirement > int.MaxValue ? int.MaxValue : (int)requirement;
    }

    private void HandleEnemyKilled(Vector2 _, int experienceReward)
    {
        AddExperience(experienceReward);
    }

    private void SetStartingProgress()
    {
        Level = Mathf.Max(1, startingLevel);
        CurrentExperience = Mathf.Clamp(startingExperience, 0, RequiredExperience - 1);
    }

    private void PublishSnapshot()
    {
        LevelChanged?.Invoke(Level);
        ProgressChanged?.Invoke(CurrentExperience, RequiredExperience);
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        GameEvents.EnemyKilled += HandleEnemyKilled;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        GameEvents.EnemyKilled -= HandleEnemyKilled;
        isSubscribed = false;
    }
}
