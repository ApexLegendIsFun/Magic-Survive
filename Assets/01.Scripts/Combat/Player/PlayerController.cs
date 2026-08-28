using UnityEngine;
using UnityEngine.InputSystem;


// 플레이어 이동 관련
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Vector2 moveInput;


    // PlayerInput의 Move 이벤트에서 호출됨
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // 플레이어 이동
    void Update()
    {
        Vector2 delta = moveInput * moveSpeed * Time.deltaTime;

        transform.position += (Vector3)delta;

    }
}
