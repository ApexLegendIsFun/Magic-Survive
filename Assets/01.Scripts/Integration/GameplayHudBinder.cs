using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayHudBinder : MonoBehaviour
{
    [SerializeField] private HudDynamicUi hud;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerProgression progression;

    private Health boundHealth;
    private PlayerProgression boundProgression;
    private bool enemyKilledBound;
    private int killCount;

    public int KillCount => killCount;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        Bind();
        RefreshAll();
    }

    private void Start()
    {
        // Handles objects created by another component during Awake/OnEnable.
        ResolveReferences();
        Bind();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unbind();
    }

    public void Configure(
        HudDynamicUi hudDynamicUi,
        Health health,
        PlayerProgression playerProgression)
    {
        Unbind();

        hud = hudDynamicUi;
        playerHealth = health;
        progression = playerProgression;

        if (isActiveAndEnabled)
        {
            Bind();
            RefreshAll();
        }
    }

    public void RefreshAll()
    {
        if (hud == null)
        {
            return;
        }

        hud.UpdateKillCount(killCount);

        if (playerHealth != null)
        {
            RefreshHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        if (progression != null)
        {
            RefreshProgress(progression.CurrentExperience, progression.RequiredExperience);
            RefreshLevel(progression.Level);
        }
    }

    private void ResolveReferences()
    {
        if (hud == null)
        {
            hud = FindFirstObjectByType<HudDynamicUi>();
        }

        if (playerHealth == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            playerHealth = player != null
                ? player.GetComponent<Health>()
                : FindFirstObjectByType<Health>();
        }

        if (progression == null)
        {
            progression = FindFirstObjectByType<PlayerProgression>();
        }
    }

    private void Bind()
    {
        if (!enemyKilledBound)
        {
            GameEvents.EnemyKilled += HandleEnemyKilled;
            enemyKilledBound = true;
        }

        if (boundHealth != playerHealth)
        {
            if (boundHealth != null)
            {
                boundHealth.HealthChanged -= RefreshHealth;
            }

            boundHealth = playerHealth;

            if (boundHealth != null)
            {
                boundHealth.HealthChanged += RefreshHealth;
            }
        }

        if (boundProgression != progression)
        {
            if (boundProgression != null)
            {
                boundProgression.ProgressChanged -= RefreshProgress;
                boundProgression.LevelChanged -= RefreshLevel;
            }

            boundProgression = progression;

            if (boundProgression != null)
            {
                boundProgression.ProgressChanged += RefreshProgress;
                boundProgression.LevelChanged += RefreshLevel;
            }
        }
    }

    private void Unbind()
    {
        if (enemyKilledBound)
        {
            GameEvents.EnemyKilled -= HandleEnemyKilled;
            enemyKilledBound = false;
        }

        if (boundHealth != null)
        {
            boundHealth.HealthChanged -= RefreshHealth;
            boundHealth = null;
        }

        if (boundProgression != null)
        {
            boundProgression.ProgressChanged -= RefreshProgress;
            boundProgression.LevelChanged -= RefreshLevel;
            boundProgression = null;
        }
    }

    private void HandleEnemyKilled(Vector2 position, int experienceReward)
    {
        killCount++;

        if (hud != null)
        {
            hud.UpdateKillCount(killCount);
        }
    }

    private void RefreshHealth(float current, float maximum)
    {
        if (hud == null)
        {
            return;
        }

        hud.UpdateHp(current, maximum);
    }

    private void RefreshProgress(int current, int required)
    {
        if (hud == null)
        {
            return;
        }

        hud.UpdateLevelUp(current, required);
    }

    private void RefreshLevel(int level)
    {
        if (hud != null)
        {
            hud.UpdateLvtext(level);
        }
    }
}
