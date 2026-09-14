using UnityEngine;
using TMPro;

namespace Seondong.IntegrationGame
{
    // Connect the existing boss and team HUD APIs without changing either implementation.
    public sealed class IntegrationBossHud : MonoBehaviour
    {
        [SerializeField] private BossSpawner spawner;
        [SerializeField] private HudDynamicUi hud;
        [SerializeField] private RunDirector runDirector;
        [SerializeField] private TMP_Text remainingTime;
        [SerializeField] private GameObject bossGroup;
        private Health health;
        private void OnEnable() { if (spawner != null) spawner.BossSpawned += OnBoss; }
        private void OnBoss(Enemy enemy)
        {
            if (health != null) health.HealthChanged -= Refresh;
            health = enemy.GetComponent<Health>();
            hud.EnableBossGroup();
            hud.SetbossNameText("TEMP BOSS");
            health.HealthChanged += Refresh;
            Refresh(health.CurrentHealth, health.MaxHealth);
        }
        private void Refresh(float current, float maximum) => hud.UpdateBossHp(current, maximum);
        private void Update()
        {
            if (runDirector != null && runDirector.Result != null)
            {
                if (bossGroup != null) bossGroup.SetActive(false);
                return;
            }
            if (health == null || remainingTime == null || runDirector == null) return;
            int seconds = Mathf.CeilToInt(Mathf.Max(0, RunTimelineRules.TimeLimit - runDirector.ElapsedCombatTime));
            remainingTime.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
        private void OnDisable()
        {
            if (spawner != null) spawner.BossSpawned -= OnBoss;
            if (health != null) health.HealthChanged -= Refresh;
        }
    }
}
