using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 소환술사 로직이 아직 없을 때 SummonEffectPlayer만 독립 테스트하기 위한 임시 디버그.
/// </summary>
public class SummonDebugTrigger : MonoBehaviour
{
    [SerializeField] private Transform testOrigin;
    [SerializeField] private float telegraphSeconds = 1.5f; // 임시값

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            TriggerSummonTelegraph();
        }
    }

    private void TriggerSummonTelegraph()
    {
        Vector2 basePos = testOrigin != null ? (Vector2)testOrigin.position : (Vector2)transform.position;

        var positions = new List<Vector2>
        {
            basePos + Vector2.left,
            basePos + Vector2.right,
            basePos + Vector2.up
        };

        Debug.Log($"[SummonDebugTrigger] 예고 시작, 대상 {positions.Count}곳, telegraph={telegraphSeconds}");
        GameEvents.RaiseSummonTelegraph(positions, telegraphSeconds);
    }
}