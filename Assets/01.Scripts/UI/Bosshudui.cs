using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스전 HUD. 이름은 고정 문자열(기획서 기준 "수호자")로 세팅.
/// 체력 Enemy 쪽 API가 필요해서 SetHpRatio를 열어둠 (요청 대기 중).
/// </summary>
public class BossHudUi : MonoBehaviour
{
    [Header("Boss Info")]
    [SerializeField] private TextMeshProUGUI bossTimeText; // 남은 시간
    [SerializeField] private Image bossHpBar;               // Image.Type = Filled 전제

    [Header("Flow")]
    [SerializeField] private RunDirector runDirector;

    private bool isSubscribed;

    private void Awake()
    {
        if (runDirector == null)
        {
            runDirector = FindFirstObjectByType<RunDirector>();
        }

        gameObject.SetActive(false); // 평소엔 숨김, 보스전 시작 시 켜짐
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (isSubscribed || runDirector == null) return;

        runDirector.BossSpawnRequested += HandleBossSpawnRequested;
        runDirector.TimeChanged += HandleTimeChanged;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || runDirector == null) return;

        runDirector.BossSpawnRequested -= HandleBossSpawnRequested;
        runDirector.TimeChanged -= HandleTimeChanged;
        isSubscribed = false;
    }

    private void HandleBossSpawnRequested()
    {
        gameObject.SetActive(true);

        // TODO: 체력은 여전히 Enemy 쪽에 HealthRatio/HealthRatioChanged 같은
        // API가 필요함 (Health 전체 노출은 지양). 요청 대기 중.
        // 받기 전까진 아래 SetHpRatio를 디버그로 수동 호출해서 테스트.
    }

    private void HandleTimeChanged(float elapsed, float remaining)
    {
        if (bossTimeText == null) return;

        int totalSeconds = Mathf.CeilToInt(remaining);
        bossTimeText.text = $"{totalSeconds / 60}:{(totalSeconds % 60):00}";
    }

  

    // 보스 체력 비율(0~1) — 보스 Health 변경 시 호출해줘야 함 (연결 지점 미확정)
    public void SetHpRatio(float ratio)
    {
        if (bossHpBar != null) bossHpBar.fillAmount = Mathf.Clamp01(ratio);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}