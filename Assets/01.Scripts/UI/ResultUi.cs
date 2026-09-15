using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultUi : MonoBehaviour
{
    [System.Serializable]
    public class ElementIconEntry
    {
        public MagicElement element;
        public Sprite icon;
    }

    [System.Serializable]
    public class ElementResultSlot
    {
        public Image icon;
        public TextMeshProUGUI levelText; // "Lv_0"
    }

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleText; // "승리" / "패배"

    [Header("Game Info")]
    [SerializeField] private TextMeshProUGUI time;
    [SerializeField] private TextMeshProUGUI killCount;
    [SerializeField] private TextMeshProUGUI level;

    [Header("Elements (5개 고정)")]
    [SerializeField] private ElementResultSlot[] elementSlots;
    [SerializeField] private ElementIconEntry[] elementIcons;

    [Header("Gameflow Button")]
    [SerializeField] private Button title;
    [SerializeField] private Button reStart;

    [Header("Flow")]
    [SerializeField] private GameFlowController gameFlowController;

    private void Awake()
    {
        // Ttitle Scene, 또는 ReStart 위해 선동님의 스크립트 참조 
        if (gameFlowController == null)
        {
            gameFlowController = FindFirstObjectByType<GameFlowController>();
        }

        if (title != null) title.onClick.AddListener(OnClickTitle);
        if (reStart != null) reStart.onClick.AddListener(OnClickRestart);
    }

    public void ShowResult(RunResult result)
    {
        gameObject.SetActive(true);

        if (result == null)
        {
            // TODO: RunResult가 null인 케이스(패배/게임오버)의 정확한 표시 규칙 확인 필요
            if (titleText != null) titleText.text = "패배";
            SetElementSlots(null);
            return;
        }

        // TODO: RunResult.IsVictory 필드명이 최신 버전에도 동일한지 확인 필요
        if (titleText != null) titleText.text = BuildTitleText(result.Outcome);

        if (time != null)
        {
            int totalSeconds = Mathf.FloorToInt(result.CombatTime);
            time.text = $"{totalSeconds / 60}:{(totalSeconds % 60):00}";
        }

        if (level != null) level.text = result.Level.ToString();
        if (killCount != null) killCount.text = result.KillCount.ToString();

        // TODO: RunResult가 원소별 레벨까지 담고 있는지 확인 필요.
        // 지금은 Elements가 "보유한 원소 목록"이라고 가정하고, 목록에 있으면 그냥 보유 표시만 함.
        // 실제로 개별 레벨(Lv.2, Lv.3 등)까지 나와야 하면 RunResult에 레벨 정보가 추가로 필요함.
        SetElementSlots(result.Elements);
    }

    private static string BuildTitleText(RunOutcome outcome)
    {
        switch (outcome)
        {
            case RunOutcome.Victory: return "승리";
            case RunOutcome.Timeout: return "시간 종료";
            case RunOutcome.Defeat:
            default: return "패배";
        }
    }

    private void SetElementSlots(IReadOnlyList<MagicElement> ownedElements)
    {
        if (elementSlots == null) return;

        var fixedOrder = MagicContentCatalog.PentagonElements; // 고정 순서, HudStatcUi와 동일 기준

        for (int i = 0; i < elementSlots.Length; i++)
        {
            bool hasSlotElement = i < fixedOrder.Count;
            var slot = elementSlots[i];

            if (!hasSlotElement)
            {
                if (slot.icon != null) slot.icon.enabled = false;
                if (slot.levelText != null) slot.levelText.text = string.Empty;
                continue;
            }

            var element = fixedOrder[i];
            bool owned = ownedElements != null && ownedElements.Contains(element);

            if (slot.icon != null)
            {
                slot.icon.enabled = true;
                slot.icon.sprite = GetElementIcon(element);
            }

            // RunResult엔 원소별 레벨이 없고 보유 목록만 있음 (확인 완료).
            // 레벨까지 보여주려면 RunResult에 필드 추가를 요청해야 함 — 그 전까진 보유 여부만 표시.
            if (slot.levelText != null) slot.levelText.text = owned ? "보유" : string.Empty;
        }
    }

    private Sprite GetElementIcon(MagicElement element)
    {
        var entry = elementIcons?.FirstOrDefault(e => e.element.Equals(element));
        return entry?.icon;
    }

    private void OnClickTitle()
    {
        gameFlowController?.LoadTitleScene();
    }

    private void OnClickRestart()
    {
        gameFlowController?.RestartCurrentScene();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}