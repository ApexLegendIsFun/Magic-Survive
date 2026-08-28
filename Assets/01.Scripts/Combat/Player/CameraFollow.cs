using UnityEngine;


//MainCamera에 붙음 
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField] private float smoothTime = 0.1f;

    private Vector2 currentVelocity;


    // 플레이어 움직임이 Update에서 처리되므로, 이동이 끝난 후에 LateUpdate로(떨림방지)
    private void LateUpdate()
    {
        Vector2 smoothed = Vector2.SmoothDamp(transform.position, target.position, ref currentVelocity, smoothTime);

        // 카메라 z 위치 유지(화면이 빌 수 있으므로)
        transform.position = new Vector3(smoothed.x, smoothed.y, transform.position.z);
    }

}
