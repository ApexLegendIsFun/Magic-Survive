using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 엘리트/보스 등장 알림 패널. 오른쪽 화면 밖 → 중앙(대기) → 왼쪽 화면 밖으로 나가는 연출.
/// RunDirector.EliteSpawnRequested / BossSpawnRequested를 직접 구독.
/// </summary>
public class EliteAnnouncementUi : MonoBehaviour
{
    [System.Serializable]
    public class EliteAnnouncementEntry
    {
        public EliteKind kind;
        public string title; // "돌진자 등장!" 등
        public Sprite icon;  // 선택
    }

    [Header("Data Source")]
    [SerializeField] private RunDirector runDirector;

    [Header("Panel (애니메이션 대상)")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image iconImage; // 없으면 비워도 됨

    [Header("Elite Entries")]
    [SerializeField] private EliteAnnouncementEntry[] eliteEntries;

    [Header("Boss")]
    [SerializeField] private string bossTitle = "보스 등장!";
    [SerializeField] private Sprite bossIcon;

    [Header("Animation")]
    [Tooltip("패널이 화면 밖으로 나가는 X축 거리. 패널 폭+화면 절반보다 커야 완전히 안 보임")]
    [SerializeField] private float offscreenDistance = 1400f;
    [SerializeField] private float enterDuration = 0.35f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private float exitDuration = 0.3f;
    [SerializeField] private AnimationCurve enterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve exitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Queue<(string title, Sprite icon)> queue = new Queue<(string, Sprite)>();
    private bool isPlaying;
    private float centerY;

    private void Awake()
    {
        if (panelRoot != null)
        {
            centerY = panelRoot.anchoredPosition.y;
            panelRoot.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (runDirector == null) runDirector = FindFirstObjectByType<RunDirector>();
        if (runDirector != null)
        {
            runDirector.EliteSpawnRequested += HandleEliteSpawnRequested;
            runDirector.BossSpawnRequested += HandleBossSpawnRequested;
        }
    }

    private void OnDisable()
    {
        if (runDirector != null)
        {
            runDirector.EliteSpawnRequested -= HandleEliteSpawnRequested;
            runDirector.BossSpawnRequested -= HandleBossSpawnRequested;
        }
    }

    private void HandleEliteSpawnRequested(EliteKind kind)
    {
        var entry = eliteEntries?.FirstOrDefault(e => e.kind == kind);
        if (entry == null)
        {
            Debug.LogWarning($"[EliteAnnouncementUi] {kind} 항목이 등록되지 않음");
            return;
        }
        Enqueue(entry.title, entry.icon);
    }

    private void HandleBossSpawnRequested()
    {
        Enqueue(bossTitle, bossIcon);
    }

    // 테스트용 진입점. RunDirector의 event는 외부에서 강제 발행이 불가능해서,
    // 디버그 트리거가 RunDirector를 거치지 않고 이 UI를 직접 부르는 용도.
    public void DebugTriggerElite(EliteKind kind) => HandleEliteSpawnRequested(kind);
    public void DebugTriggerBoss() => HandleBossSpawnRequested();

    private void Enqueue(string title, Sprite icon)
    {
        queue.Enqueue((title, icon));
        if (!isPlaying) StartCoroutine(PlayQueue());
    }

    private IEnumerator PlayQueue()
    {
        isPlaying = true;
        while (queue.Count > 0)
        {
            var (title, icon) = queue.Dequeue();
            yield return PlayAnnouncement(title, icon);
        }
        isPlaying = false;
    }

    private IEnumerator PlayAnnouncement(string title, Sprite icon)
    {
        if (panelRoot == null) yield break;

        if (titleText != null) titleText.text = title;
        if (iconImage != null)
        {
            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
        }

        panelRoot.gameObject.SetActive(true);

        yield return AnimateX(offscreenDistance, 0f, enterDuration, enterCurve);   // 오른쪽 밖 → 중앙
        yield return WaitUnscaled(holdDuration);                                   // 중앙 대기
        yield return AnimateX(0f, -offscreenDistance, exitDuration, exitCurve);    // 중앙 → 왼쪽 밖

        panelRoot.gameObject.SetActive(false);
    }

    private IEnumerator AnimateX(float fromX, float toX, float duration, AnimationCurve curve)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // 레벨업/일시정지 중에도 정상 재생
            float p = curve.Evaluate(Mathf.Clamp01(t / duration));
            panelRoot.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, p), centerY);
            yield return null;
        }
        panelRoot.anchoredPosition = new Vector2(toX, centerY);
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}