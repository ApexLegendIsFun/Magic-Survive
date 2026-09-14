using UnityEngine;

namespace Seondong.IntegrationGame
{
    // Decorative local motion; cannot move the projectile or cause hits.
    public sealed class MagicVisualOrbit : MonoBehaviour
    {
        public float degreesPerSecond = 100;
        private void OnEnable() => transform.localRotation = Quaternion.identity;
        private void Update() => transform.Rotate(0, 0, degreesPerSecond * Time.deltaTime);
    }
}
