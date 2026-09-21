using UnityEngine;

/// <summary>
/// 상태 효과 연출을 켜고 끄는 순수 그래픽 컴포넌트. "언제" 켜고 끌지는 전혀 모르고,
/// Show()/Hide()만 제공. 암흑/빙결처럼 "적 몸에 오버레이+색 틴트를 씌우는" 상태 효과들이
/// 전부 이 하나를 재사용할 수 있음.
/// </summary>
public class StatusEffectVisual : MonoBehaviour
{
    [SerializeField] private GameObject overlay;            // 오버레이 오브젝트 (없어도 됨)
    [SerializeField] private SpriteRenderer targetRenderer; // 색 틴트 대상 (없어도 됨)
    [SerializeField] private Color tintColor = Color.white;

    private Color originalColor;
    private bool isShowing;

    private void Awake()
    {
        if (targetRenderer != null) originalColor = targetRenderer.color;
    }

    public void Show()
    {
        if (isShowing) return;
        isShowing = true;

        if (overlay != null) overlay.SetActive(true);
        if (targetRenderer != null) targetRenderer.color = tintColor;
    }

    public void Hide()
    {
        if (!isShowing) return;
        isShowing = false;

        if (overlay != null) overlay.SetActive(false);
        if (targetRenderer != null) targetRenderer.color = originalColor;
    }

    // 풀 재사용 등으로 상태를 강제로 맞춰야 할 때
    public void SetImmediate(bool visible)
    {
        isShowing = visible;
        if (overlay != null) overlay.SetActive(visible);
        if (targetRenderer != null) targetRenderer.color = visible ? tintColor : originalColor;
    }
}