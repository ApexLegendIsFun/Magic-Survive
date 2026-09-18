using System.Linq;
using UnityEngine;

/// <summary>
/// 지속 장판 (대지 5레벨 지진 구역 / 냉기 8레벨 서리 장판, 같은 이벤트 공용).
/// 보스 등장·재시작 시 조기 제거되는 경우엔 이 이벤트로 알리지 않는다고 명시돼 있어서,
/// 그 경우 연출이 durationSeconds만큼 화면에 남을 수 있음 — 의도된 동작이니 별도 처리 불필요.
/// </summary>
public class GroundAreaEffectPlayer : MonoBehaviour
{
    [System.Serializable]
    public class GroundAreaEntry
    {
        public MagicElement element;
        public GameObject areaPrefab; // 반경에 맞춰 스케일 조절됨
    }

    [SerializeField] private GroundAreaEntry[] entries;

    private void OnEnable()
    {
        GameEvents.GroundAreaCreated += HandleGroundAreaCreated;
    }

    private void OnDisable()
    {
        GameEvents.GroundAreaCreated -= HandleGroundAreaCreated;
    }

    private void HandleGroundAreaCreated(MagicElement element, Vector2 center, float radius, float durationSeconds)
    {
        var entry = entries?.FirstOrDefault(e => e.element.Equals(element));
        if (entry == null || entry.areaPrefab == null)
        {
            Debug.LogWarning($"[GroundAreaEffectPlayer] {element} 장판 프리팹이 등록되지 않음");
            return;
        }

        var instance = Instantiate(entry.areaPrefab, center, Quaternion.identity);
        instance.transform.localScale = Vector3.one * radius;
        Destroy(instance, durationSeconds);
    }
}