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

    private PlayerSkillSystem currentSkillSystem;
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
            // The authored prefab shares each button between starting selection and level-up.
            captured.button.onClick.AddListener(() => OnClickElement(captured.element));
        }
        if (startItemButtons == null) return;
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
        OnClickElement(MagicElement.Fire);
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
        if (previewNameText != null) previewNameText.text = string.Empty;
        if (previewDescriptionText != null) previewDescriptionText.text = string.Empty;
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
            if (slot.statusLabel != null) slot.statusLabel.text = maxed ? "MAX" :
                $"Lv.{skillSystem.GetSkillLevel(slot.element)} → {LevelUpSlotMapper.GetPreviewLevel(slot.element, skillSystem.Tree)}";
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
