using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스전 HUD. 이름은 고정 문자열, 남은 시간은 RunDirector가 주는 값 그대로.
/// 보스 인스턴스 자체는 IsBoss/Killed(둘 다 확정된 공개 API)로 찾아서 자동 추적함.
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

    private bool isSubscribed;
    private Enemy currentBoss;

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
        UnsubscribeBoss();
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
        SetBossName(bossName); // 고정값이라 스폰 시점에 바로 세팅

        // BossSpawnRequested 시점엔 아직 EnemyManager.Spawn이 안 끝났을 수 있어서 한 프레임 늦춰서 찾음
        StartCoroutine(FindBossNextFrame());
    }

    private IEnumerator FindBossNextFrame()
    {
        yield return null;

        var boss = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .FirstOrDefault(e => e.IsBoss);

        if (boss == null)
        {
            Debug.LogWarning("[BossHudUi] IsBoss인 Enemy를 못 찾음");
            yield break;
        }

        SubscribeBoss(boss);
    }

    private void SubscribeBoss(Enemy boss)
    {
        UnsubscribeBoss(); // 혹시 이전 보스 구독이 남아있으면 정리

        currentBoss = boss;
        currentBoss.Killed += HandleBossKilled;

        // TODO: 체력은 여전히 Enemy 쪽에 HealthRatio/HealthRatioChanged 같은
        // 좁은 조회 API가 필요함 (Health 전체 노출은 지양, health.MaxHealth 필드명은 확정됨).
        // 받으면 여기서 currentBoss.HealthRatioChanged += HandleHealthRatioChanged; 로 교체.
        // 받기 전까진 SetHpRatio를 디버그로 수동 호출해서 테스트.
    }

    private void UnsubscribeBoss()
    {
        if (currentBoss != null)
        {
            currentBoss.Killed -= HandleBossKilled;
        }
        currentBoss = null;
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