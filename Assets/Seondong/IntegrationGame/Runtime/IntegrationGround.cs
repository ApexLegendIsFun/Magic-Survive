using UnityEngine;

namespace Seondong.IntegrationGame
{
    [DisallowMultipleComponent]
    public sealed class IntegrationGround : MonoBehaviour
    {
        public Camera followCamera;
        private void LateUpdate()
        {
            if (followCamera == null) return;
            float height = followCamera.orthographicSize * 2 + 8;
            float width = followCamera.orthographicSize * 2 * followCamera.aspect + 8;
            transform.position = new Vector3(followCamera.transform.position.x, followCamera.transform.position.y, 5);
            transform.localScale = new Vector3(width, height, 1);
        }
    }
}
