using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Projectile/MagicRuntime 쪽 3중첩 파이프라인이 아직 안 합쳐졌을 때,
/// GameEvents.RaiseElementReaction을 직접 호출해서 이펙트/사운드(ElementReactionEffectPlayer)만
/// 독립적으로 테스트하기 위한 임시 디버그용 스크립트.
///
/// 씬의 아무 빈 오브젝트에 붙여서 쓰고, 실제 파이프라인이 합쳐지면 통째로 빼거나
/// 컴포넌트를 비활성화하세요. 배포 빌드에 남아있으면 안 됩니다.
/// </summary>
public class ElementReactionDebugTrigger : MonoBehaviour
{
    [Header("테스트 위치 (비워두면 이 오브젝트 위치 사용)")]
    [SerializeField] private Transform testOrigin;

    [Header("문서 수치 기준 기본값")]
    [SerializeField] private float igniteTestRadius = 1.3f; // 화염 점화 반경
    [SerializeField] private float freezeTestRadius = 0f;   // 빙결은 단일 대상이라 0

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Trigger(MagicElement.Fire, igniteTestRadius);
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            Trigger(MagicElement.Frost, freezeTestRadius);
        }
    }

    private void Trigger(MagicElement element, float radius)
    {
        Vector2 position = testOrigin != null
            ? (Vector2)testOrigin.position
            : (Vector2)transform.position;

        Debug.Log($"[ElementReactionDebugTrigger] {element} 강제 발동 @ {position}, radius={radius}");
        GameEvents.RaiseElementReaction(element, position, radius);
    }
}