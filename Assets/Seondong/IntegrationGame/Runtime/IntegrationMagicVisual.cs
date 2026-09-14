using UnityEngine;

namespace Seondong.IntegrationGame
{
    // A pooled projectile captures its look once per launch. Never edits the attack spec.
    [DisallowMultipleComponent]
    public sealed class IntegrationMagicVisual : MonoBehaviour
    {
        public MagicVisualProfile profile;
        private readonly GameObject[] instances = new GameObject[4];
        private PlayerSkillSystem skills;
        public int CapturedLevel { get; private set; }
        public int Tier { get; private set; }
        public int ActivationSerial { get; private set; }
        public GameObject ActiveVisual => instances[Tier];
        private void OnEnable()
        {
            if (profile == null || !profile.IsComplete) { Debug.LogError("[Magic Visual] Incomplete profile", this); return; }
            if (skills == null) skills = FindFirstObjectByType<PlayerSkillSystem>();
            CapturedLevel = skills != null ? Mathf.Max(1, skills.GetSkillLevel(profile.element)) : 1;
            Tier = MagicVisualProfile.TierForLevel(CapturedLevel);
            if (instances[Tier] == null) instances[Tier] = Instantiate(profile.tiers[Tier], transform, false);
            instances[Tier].SetActive(true);
            ActivationSerial++;
        }
        private void OnDisable()
        {
            foreach (var instance in instances) if (instance != null) instance.SetActive(false);
        }
    }
}
