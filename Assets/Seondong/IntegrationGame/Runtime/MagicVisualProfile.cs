using UnityEngine;

namespace Seondong.IntegrationGame
{
    [CreateAssetMenu(menuName = "Seondong/Magic Visual Profile")]
    public sealed class MagicVisualProfile : ScriptableObject
    {
        public MagicElement element;
        public GameObject[] tiers = new GameObject[4];
        public static int TierForLevel(int level) => level >= 8 ? 3 : level >= 5 ? 2 : level >= 3 ? 1 : 0;
        public bool IsComplete => tiers != null && tiers.Length == 4 &&
            tiers[0] != null && tiers[1] != null && tiers[2] != null && tiers[3] != null;
    }
}
