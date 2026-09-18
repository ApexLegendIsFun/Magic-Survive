using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 소환 로직(위치 유지, 실제 스폰)이 아직 구현 안 됐을 때,
/// SummonEffectPlayer(예고/버스트 연출)만 독립적으로 테스트하기 위한 임시 디버그 스크립트.
/// F3 = 예고 표시 → leadTimeSeconds 뒤 F4 없이 자동으로 소환 이벤트까지 이어서 발동.
/// 실제 파이프라인 합쳐지면 씬에서 빼세요.
/// </summary>
public class SummonDebugTrigger : MonoBehaviour
{
    [SerializeField] private Transform testOrigin;
    [SerializeField] private float leadTimeSeconds = 1.5f; // 예고 후 실제 소환까지 걸리는 시간(임시값)

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            TriggerSummonSequence();
        }
    }

    private void TriggerSummonSequence()
    {
        Vector2 position = testOrigin != null
            ? (Vector2)testOrigin.position
            : (Vector2)transform.position;

        Debug.Log($"[SummonDebugTrigger] 예고 시작 @ {position}, leadTime={leadTimeSeconds}");
        // TODO : GameEvents 주석 대기 
        // GameEvents.RaiseSummonWarning(position, leadTimeSeconds);

        Invoke(nameof(FireSummon), leadTimeSeconds);
        pendingPosition = position;
    }

    private Vector2 pendingPosition;

    private void FireSummon()
    {
        Debug.Log($"[SummonDebugTrigger] 실제 소환 @ {pendingPosition}");
        // TODO : GameEvents 주석 대기 
        //GameEvents.RaiseEnemySummoned(pendingPosition);
    }
}