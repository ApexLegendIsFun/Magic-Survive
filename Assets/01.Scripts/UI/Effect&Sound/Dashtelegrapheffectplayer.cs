using UnityEngine;

/// <summary>
/// 돌진자 돌진 예고선. 시작→끝을 잇는 직선(폭이 있는 경고 레인 형태).
/// 취소 이벤트가 따로 없으므로 telegraphSeconds가 지나면 무조건 스스로 지움
/// (죽거나 빙결로 취소돼도 동일하게 처리됨, 계약대로).
/// </summary>
public class DashTelegraphEffectPlayer : MonoBehaviour
{
    [SerializeField] private LineRenderer dashLinePrefab;

    private void OnEnable()
    {
        GameEvents.DashTelegraph += HandleDashTelegraph;
    }

    private void OnDisable()
    {
        GameEvents.DashTelegraph -= HandleDashTelegraph;
    }

    private void HandleDashTelegraph(Vector2 origin, Vector2 direction, float distance, float telegraphSeconds)
    {
        if (dashLinePrefab == null) return;

        var line = Instantiate(dashLinePrefab);
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, origin);
        line.SetPosition(1, origin + direction.normalized * distance);

        Destroy(line.gameObject, telegraphSeconds);
    }
}