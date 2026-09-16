using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스전 HUD. 이름/체력은 아직 보스 쪽에서 데이터를 안 주므로 public 메서드로 열어두고,
/// 남은 시간은 RunDirector가 이미 주는 값을 그대로 쓰도록 했습니다. 
/// </summary>
public class BossHudUi : MonoBehaviour
{
    [Header("Boss Info")]
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI bossTimeText; // 남은 시간
    [SerializeField] private Image bossHpBar;               

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
        // TODO: BossSpawnRequested가 지금은 매개변수가 없어서, 어떤 보스가 스폰됐는지, (보스의 이름, 보스의 체력 등등 )

     
        gameObject.SetActive(true);
    }

    private void HandleTimeChanged(float elapsed, float remaining)
    {
        if (bossTimeText == null) return;

        int totalSeconds = Mathf.CeilToInt(remaining);
        bossTimeText.text = $"{totalSeconds / 60}:{(totalSeconds % 60):00}";
    }

    // 보스 이름 — 보스 스폰 시점에 EnemyManager/보스 컴포넌트가 호출해줘야 함 (연결 지점 미확정)
    public void SetBossName(string name)
    {
        if (bossNameText != null) bossNameText.text = name;
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