using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PopupUi : MonoBehaviour
{
    // 원소 슬롯
    [System.Serializable]
    public class ElementSlot
    {
        public MagicElement element;
        public Button button;
        public Image icon;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI statusLabel;

        public GameObject lockIcon;
        public GameObject checkmark;
        public GameObject pendingHighlight;
    }

    // 강화 카드 슬롯 (오른쪽 "02. 강화 카드 선택" 패널, 3장 고정)
    [System.Serializable]
    public class SpecializationCardSlot
    {
        public GameObject root;        // 카드 통짜 오브젝트 (없는 카드일 때 꺼야 하니까)
        public Image icon;             // 원소 아이콘 재사용 (SpecializationDefinition엔 아이콘 필드가 없음)
                                                 // 아래 주석은 예시.
        public TextMeshProUGUI nameLabel;        // "집중 화염"
        public TextMeshProUGUI effectLabel;      // "화염탄 피해 +10%"
        public TextMeshProUGUI countLabel;       // "선택 0회 · 누적 +0%"
        public Button selectButton;

        [System.NonSerialized] public SpecializationId assignedId;
    }

    // 원소 해금 시(Lv.0 → Lv.1) 뜨는 단일 카드 — 특화 카드 3장이 없는 특수 케이스
    [System.Serializable]
    public class UnlockCardSlot
    {
        public GameObject root;
        public Image icon;
                                                 //아래 주석은 예시.
        public TextMeshProUGUI titleLabel;       // "원소 해금"
        public TextMeshProUGUI descriptionLabel; // "기본 번개탄 · 피해 5 / 0.7초"
        public TextMeshProUGUI noteLabel;        // "첫 획득 · 특화 선택 없음"
        public Button confirmButton;             // "원소 획득" — 기존 gainButton과 동일한 확정 경로 재사용
    }

    //  LevelUpController
    [Header("Level Up Controller")]
    [SerializeField] private LevelUpController levelUpController;


    // 시작 원소 선택

    [Header("Start Select")]
    [SerializeField] private Button[] startItemButtons;
    [SerializeField] private Image[] startItemOutline;

    [SerializeField] private GameObject startTitle;
    [SerializeField] private Button startButton;

    private readonly Color normalColor = Color.white;
    private readonly Color hoverColor = Color.red;

    private MagicElement selectedStartElement;

    // 레벨업
    [Header("LevelUp")]
    [SerializeField] private GameObject levelupTitle;
    [SerializeField] private GameObject levelUpButtonGroup;

    [SerializeField] private Button gainButton; //획득 버튼
    [SerializeField] private Button cancelButton; //취소 버튼

    // 레벨업 원소 슬롯
    [Header("LevelUp Element Slots")]
    [SerializeField] private ElementSlot[] elementSlots;

    [Header("Selected Node Preview")]
    [SerializeField] private TextMeshProUGUI previewNameText;
    [SerializeField] private TextMeshProUGUI previewDescriptionText;

    // "레벨 상승 시 기본 획득 / 피해 +15%" 텍스트
    [Header("Growth Cards (오른쪽 패널)")]
    [SerializeField] private TextMeshProUGUI growthLevelRangeLabel; // "Lv 1 => Lv 2"
    [SerializeField] private TextMeshProUGUI baseGainLabel;
    [SerializeField] private SpecializationCardSlot[] cardSlots; // 3개 고정
    [SerializeField] private UnlockCardSlot unlockCardSlot;      // Lv.0 → Lv.1 전용

    [Header("Open Animation")]
    [SerializeField] private RectTransform panelRoot; // 스케일 애니메이션 대상 (팝업 전체를 감싸는 RectTransform)
    [SerializeField] private CanvasGroup canvasGroup;  // 없으면 페이드는 생략되고 스케일만 적용됨
    [SerializeField] private float openDuration = 0.22f;
    [SerializeField] private float openStartScale = 0.85f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine openRoutine;

    private PlayerSkillSystem currentSkillSystem;
    private MagicElement previewedElement;
    private bool isLevelUpMode;
    private bool initialized;

    public bool IsConfigured => startButton != null && gainButton != null &&
        elementSlots != null && elementSlots.Length == 5;

    private void Awake() { InitializeButtons(); }
    private void InitializeButtons()
    {
        if (initialized) return;
        initialized = true;
        if (levelUpController == null) levelUpController = FindFirstObjectByType<LevelUpController>();
        if (startButton != null) startButton.onClick.AddListener(OnClickStart);
        if (gainButton != null) gainButton.onClick.AddListener(OnClickGain);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnClickCancel);
        if (elementSlots == null) return;
        foreach (var slot in elementSlots)
        {
            var captured = slot;
            if (captured.button == null) continue;
        

            captured.button.onClick.AddListener(() => OnClickElement(captured.element));
        }
        if (startItemButtons != null)
        {
            for (int i = 0; i < startItemButtons.Length; i++)
            {
                int index = i;
                if (startItemButtons[i] == null) continue;
                EventTrigger trigger = startItemButtons[i].GetComponent<EventTrigger>();
                if (trigger == null) trigger = startItemButtons[i].gameObject.AddComponent<EventTrigger>();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => SetOutline(index, hoverColor));
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => SetOutline(index, normalColor));
                trigger.triggers.Add(enter);
                trigger.triggers.Add(exit);
            }
        }

        if (cardSlots != null)
        {
            foreach (var slot in cardSlots)
            {
                var captured = slot;
                if (captured.selectButton == null) continue;
               

                captured.selectButton.onClick.AddListener(() => OnClickSpecializationCard(captured));
            }
        }

        if (unlockCardSlot?.confirmButton != null)
        {
            // "원소 획득"은 특화 선택이 아니라 기존 gainButton과 같은 확정 경로(ConfirmSelectedSkill)를 그대로 씀
            unlockCardSlot.confirmButton.onClick.AddListener(OnClickGain);
        }
    }
    private void SetOutline(int index, Color color)
    {
        if (startItemOutline != null && index < startItemOutline.Length && startItemOutline[index] != null)
            startItemOutline[index].color = color;
    }
    private static void SetVisible(GameObject target, bool visible)
    {
        if (target != null) target.SetActive(visible);
    }
    private void SetMode(bool levelUp)
    {
        isLevelUpMode = levelUp;
        SetVisible(startTitle, !levelUp);
        if (startButton != null) SetVisible(startButton.gameObject, !levelUp);
        SetVisible(levelupTitle, levelUp);
        SetVisible(levelUpButtonGroup, levelUp);
    }
    public void ShowElementSelect()
    {
        InitializeButtons();
        gameObject.SetActive(true);
        SetMode(false);
        PlayOpenAnimation();
        foreach (var slot in elementSlots)
        {
            if (slot.button != null)
            {
                slot.button.gameObject.SetActive(true);
                slot.button.interactable = true;
            }
            SetVisible(slot.lockIcon, false);
            SetVisible(slot.checkmark, false);
            SetVisible(slot.pendingHighlight, false);
            if (slot.statusLabel != null) slot.statusLabel.text = string.Empty;
        }
        HideGrowthCards(); // 시작 선택 화면엔 강화 카드 패널이 없음
        OnClickElement(MagicElement.Fire);
    }

    private void HideGrowthCards()
    {
        if (growthLevelRangeLabel != null) growthLevelRangeLabel.text = string.Empty;
        if (baseGainLabel != null) baseGainLabel.text = string.Empty;
        if (cardSlots != null)
        {
            foreach (var slot in cardSlots) SetVisible(slot.root, false);
        }
        SetVisible(unlockCardSlot?.root, false);
    }

    private void PlayOpenAnimation()
    {
        if (panelRoot == null) return;
        if (openRoutine != null) StopCoroutine(openRoutine);
        openRoutine = StartCoroutine(AnimateOpen());
    }

    private IEnumerator AnimateOpen()
    {
        panelRoot.localScale = Vector3.one * openStartScale;
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        float t = 0f;
        while (t < openDuration)
        {
            // 레벨업 중 Time.timeScale이 0으로 멈춰있을 수도 있어서 unscaled 사용
            t += Time.unscaledDeltaTime;
            float p = openCurve.Evaluate(Mathf.Clamp01(t / openDuration));
            panelRoot.localScale = Vector3.one * Mathf.Lerp(openStartScale, 1f, p);
            if (canvasGroup != null) canvasGroup.alpha = p;
            yield return null;
        }

        panelRoot.localScale = Vector3.one;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        openRoutine = null;
    }
    private void OnClickElement(MagicElement element)
    {
        if (!isLevelUpMode)
        {
            selectedStartElement = element;
            ShowPreview(element, 1);
            return;
        }
        if (currentSkillSystem == null || levelUpController == null ||
            !levelUpController.TrySelectSkill(element)) return;
        ShowPreview(element, LevelUpSlotMapper.GetPreviewLevel(element, currentSkillSystem.Tree));
        RefreshSkillTree(currentSkillSystem);
        RefreshGrowthCards(element);
    }

    // 오른쪽 "02. 강화 카드 선택" 패널 채우기
    private void RefreshGrowthCards(MagicElement element)
    {
        if (currentSkillSystem == null) return;
        previewedElement = element;

        GrowthPreview preview = currentSkillSystem.GetGrowthPreview(element);

        if (growthLevelRangeLabel != null)
        {
            int nextLevel = preview.NextLevel ?? preview.CurrentLevel; // MAX면 다음 레벨 없음 → 현재 레벨 그대로
            growthLevelRangeLabel.text = $"Lv {preview.CurrentLevel} => Lv {nextLevel}";
        }

        if (baseGainLabel != null)
        {
            baseGainLabel.text = BuildBaseGainText(preview);
        }

        bool isUnlockCase = preview.State == ElementGrowthState.Unlock;

        SetVisible(unlockCardSlot?.root, isUnlockCase);
        if (isUnlockCase)
        {
            RefreshUnlockCard(element, preview);
        }

        if (cardSlots == null) return;
        for (int i = 0; i < cardSlots.Length; i++)
        {
            var slot = cardSlots[i];
            bool hasCard = !isUnlockCase && i < preview.Cards.Count;

            SetVisible(slot.root, hasCard);
            if (!hasCard) continue;

            SpecializationCardPreview card = preview.Cards[i];
            slot.assignedId = card.Id;

            if (slot.icon != null) slot.icon.sprite = GetElementIcon(element);
            if (slot.nameLabel != null) slot.nameLabel.text = card.Name;
            if (slot.effectLabel != null) slot.effectLabel.text = card.Description;
            if (slot.countLabel != null) slot.countLabel.text = card.AccumulationDescription;
            if (slot.selectButton != null) slot.selectButton.interactable = card.CanSelect;
        }
    }

    private void RefreshUnlockCard(MagicElement element, GrowthPreview preview)
    {
        if (unlockCardSlot == null) return;

        if (unlockCardSlot.icon != null) unlockCardSlot.icon.sprite = GetElementIcon(element);
        if (unlockCardSlot.titleLabel != null) unlockCardSlot.titleLabel.text = "원소 해금";
        if (unlockCardSlot.noteLabel != null) unlockCardSlot.noteLabel.text = "첫 획득 · 특화 선택 없음";
        if (unlockCardSlot.descriptionLabel != null)
        {
            int level = preview.NextLevel ?? 1;
            unlockCardSlot.descriptionLabel.text = MagicContentCatalog.GetLevelDescription(element, level);
        }
        if (unlockCardSlot.confirmButton != null) unlockCardSlot.confirmButton.interactable = preview.CanSelect;
    }

    private string BuildBaseGainText(GrowthPreview preview)
    {
        // 레벨 0(미보유) → 다음 레벨 수치만 있음, 퍼센트 비교 불가. previewDescriptionText/unlock 카드와 동일한 소스 재사용.
        if (!preview.CurrentStats.HasValue)
        {
            int level = preview.NextLevel ?? 1;
            return preview.NextStats.HasValue
                ? MagicContentCatalog.GetLevelDescription(preview.Element, level)
                : string.Empty;
        }

        if (!preview.NextStats.HasValue)
        {
            return "MAX";
        }

        float current = preview.CurrentStats.Value.Damage;
        float next = preview.NextStats.Value.Damage;
        float percent = current > 0f ? (next - current) / current * 100f : 0f;

        return $"피해 +{percent:0.#}%";
    }

    private Sprite GetElementIcon(MagicElement element)
    {
        var slot = elementSlots?.FirstOrDefault(s => s.element.Equals(element));
        return slot?.icon != null ? slot.icon.sprite : null;
    }

    private void OnClickSpecializationCard(SpecializationCardSlot slot)
    {
        if (levelUpController == null) return;

        bool success = levelUpController.TryConfirmSpecialization(previewedElement, slot.assignedId);
        Debug.Log($"특화 카드 확정: {slot.assignedId}, 성공: {success}");

        // 성공 시 TryConfirmSpecialization
    }
    private void ShowPreview(MagicElement element, int level)
    {
        if (previewNameText != null) previewNameText.text = MagicContentCatalog.GetDisplayName(element);
        if (previewDescriptionText != null)
            previewDescriptionText.text = MagicContentCatalog.GetLevelDescription(element, level);
    }
    private void OnClickStart()
    {
        if (levelUpController != null) levelUpController.TryChooseStartingElement(selectedStartElement);
    }
    public void ShowSkillTree(PlayerSkillSystem skillSystem)
    {
        InitializeButtons();
        currentSkillSystem = skillSystem;
        gameObject.SetActive(true);
        SetMode(true);
        PlayOpenAnimation();
        if (previewNameText != null) previewNameText.text = string.Empty;
        if (previewDescriptionText != null) previewDescriptionText.text = string.Empty;
        HideGrowthCards();
        RefreshSkillTree(skillSystem);
    }
    public void RefreshSkillTree(PlayerSkillSystem skillSystem)
    {
        if (skillSystem == null) return;
        currentSkillSystem = skillSystem;
        foreach (var slot in elementSlots)
        {
            bool offered = skillSystem.IsOffered(slot.element);
            if (slot.button != null)
            {
                slot.button.gameObject.SetActive(offered);
                slot.button.interactable = offered;
            }
            bool maxed = LevelUpSlotMapper.IsMaxed(slot.element, skillSystem.Tree);
            SetVisible(slot.lockIcon, !offered && !maxed);
            SetVisible(slot.checkmark, maxed);
            SetVisible(slot.pendingHighlight, skillSystem.Tree.PendingSelection == slot.element);

            if (slot.statusLabel != null)
            {
                int level = skillSystem.GetSkillLevel(slot.element);
                slot.statusLabel.text = maxed ? "MAX"
                    : level > 0 ? $"Lv.{level}"       // 보유 중 => 현재 레벨 표시
                    : offered ? "New!"                 // 미보유 + 선택 가능(인접) => New!
                    : string.Empty;                    // 미보유 + 잠김 → 자물쇠만, 텍스트 없음
            }
        }
        if (gainButton != null) gainButton.interactable = skillSystem.Tree.PendingSelection.HasValue;
    }
    private void OnClickGain()
    {
        if (levelUpController != null) levelUpController.ConfirmSelectedSkill();
    }



    private void OnClickCancel()
    {
        if (currentSkillSystem == null) return;
        currentSkillSystem.CancelSelectedSkill();
        if (previewNameText != null) previewNameText.text = string.Empty;
        if (previewDescriptionText != null) previewDescriptionText.text = string.Empty;
        RefreshSkillTree(currentSkillSystem);
    }





    public void HideLevelUp()
    {
        currentSkillSystem = null;
        gameObject.SetActive(false);
    }
}