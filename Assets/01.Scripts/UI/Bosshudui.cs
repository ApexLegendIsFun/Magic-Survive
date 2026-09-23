using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스전 HUD. 이름은 고정 문자열, 남은 시간은 RunDirector가 주는 값 그대로.
/// 보스 인스턴스는 BossSpawner.BossSpawned(Enemy)로 직접 받음 — 스캔/폴링 불필요.
/// 체력만 여전히 Enemy 쪽에 HealthRatio 같은 좁은 조회 API가 필요해서 대기 중.
/// </summary>
public class BossHudUi : MonoBehaviour
{
    [Header("Boss Info")]
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private string bossName = "원소 수호자"; // 고정값
    [SerializeField] private TextMeshProUGUI bossTimeText; // 남은 시간
    [SerializeField] private Image bossHpBar;               // Image.Type = Filled 전제

    [Header("Flow")]
    [SerializeField] private RunDirector runDirector;
    [SerializeField] private BossSpawner bossSpawner;

    private bool isSubscribed;
    private Enemy currentBoss;
    private Health currentBossHealth;

    private void Awake()
    {
        if (runDirector == null) runDirector = FindFirstObjectByType<RunDirector>();
        if (bossSpawner == null) bossSpawner = FindFirstObjectByType<BossSpawner>();

        gameObject.SetActive(false); // 평소엔 숨김, 보스전 시작 시 켜짐
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        UnsubscribeBoss();
    }

    private void Subscribe()
    {
        if (isSubscribed) return;

        if (runDirector != null) runDirector.TimeChanged += HandleTimeChanged;
        if (bossSpawner != null) bossSpawner.BossSpawned += HandleBossSpawned;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed) return;

        if (runDirector != null) runDirector.TimeChanged -= HandleTimeChanged;
        if (bossSpawner != null) bossSpawner.BossSpawned -= HandleBossSpawned;

        isSubscribed = false;
    }

    // BossSpawner가 실제 스폰 직후 보스 인스턴스를 직접 넘겨줌
    private void HandleBossSpawned(Enemy boss)
    {
        gameObject.SetActive(true);
        SetBossName(bossName);

        SubscribeBoss(boss);
    }

    private void SubscribeBoss(Enemy boss)
    {
        UnsubscribeBoss(); // 혹시 이전 보스 구독이 남아있으면 정리

        currentBoss = boss;
        currentBoss.Killed += HandleBossKilled;

        // Enemy를 거치지 않고 Health를 직접 읽음 (읽기 전용이라 생명주기 보호 원칙 위반 아님).
        currentBossHealth = boss.GetComponent<Health>();
    }

    private void UnsubscribeBoss()
    {
        if (currentBoss != null)
        {
            currentBoss.Killed -= HandleBossKilled;
        }
        currentBoss = null;
        currentBossHealth = null;
    }

    private void Update()
    {
        if (currentBossHealth == null || bossHpBar == null) return;

        float ratio = currentBossHealth.MaxHealth > 0f
            ? currentBossHealth.CurrentHealth / currentBossHealth.MaxHealth
            : 0f;

        bossHpBar.fillAmount = Mathf.Clamp01(ratio);
    }

    private void HandleBossKilled(Enemy boss)
    {
        UnsubscribeBoss();
        Hide();
    }

    private void HandleTimeChanged(float elapsed, float remaining)
    {
        if (bossTimeText == null) return;

        int totalSeconds = Mathf.CeilToInt(remaining);
        bossTimeText.text = $"{totalSeconds / 60}:{(totalSeconds % 60):00}";
    }

    public void SetBossName(string name)
    {
        if (bossNameText != null) bossNameText.text = name;
    }

    // 보스 체력 비율(0~1) — HealthRatioChanged 붙으면 자동 호출되게 교체 예정. 지금은 수동/디버그용
    public void SetHpRatio(float ratio)
    {
        if (bossHpBar != null) bossHpBar.fillAmount = Mathf.Clamp01(ratio);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}