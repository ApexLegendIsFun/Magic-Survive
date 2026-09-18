using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// BossShockwaveEffectPlayer만 독립적으로 테스트하기 위한 임시 디버그.
/// static 이벤트라 F1/F2처럼 GameEvents를 직접 호출.
/// F9 = 예고 시작 → 실측값(0.8초) 뒤 자동으로 발동까지 이어짐.
/// </summary>
public class BossShockwaveDebugTrigger : MonoBehaviour
{
    [SerializeField] private Transform testOrigin;
    [SerializeField] private float radius = 2.5f;           // 실측값
    [SerializeField] private float telegraphSeconds = 0.8f; // 실측값

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            StartCoroutine(TriggerSequence());
        }
    }

    private IEnumerator TriggerSequence()
    {
        Vector2 center = testOrigin != null ? (Vector2)testOrigin.position : (Vector2)transform.position;

        Debug.Log($"[BossShockwaveDebugTrigger] 예고 시작 @ {center}, radius={radius}, telegraph={telegraphSeconds}");
        GameEvents.RaiseBossShockwaveTelegraph(center, radius, telegraphSeconds);

        yield return new WaitForSecondsRealtime(telegraphSeconds);

        Debug.Log($"[BossShockwaveDebugTrigger] 발동 @ {center}");
        GameEvents.RaiseBossShockwaveTriggered(center, radius);
    }
}