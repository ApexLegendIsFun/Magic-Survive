using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PopupUi : MonoBehaviour
{
    [Header("Level Up Controller")]
    [SerializeField] private LevelUpController levelUpController;

    [Header("Fusion Line")]
    [SerializeField] private FusionLineManager fusionLineManager;

    [Header("Item")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("Item Button")]
    [SerializeField] private Button[] itemButtons;

    [Header("Item Info")]
    [SerializeField] private string[] itemNames;
    [SerializeField] private string[] itemDescriptions;

    [Header("Item Outline")]
    [SerializeField] private Image[] itemOutline;

    [Header("Title")]
    [SerializeField] private GameObject startTitle;
    [SerializeField] private GameObject levelupTitle;

    [Header("Start Button (원소 선택 전용)")]
    [SerializeField] private Button startButton;

    [Header("LevelUp Button Group (획득/취소)")]
    [SerializeField] private GameObject levelUpButtonGroup; // gainButton + cancelButton을 담는 부모
    [SerializeField] private Button gainButton;
    [SerializeField] private Button cancelButton;

    private readonly Color normalColor = Color.white;
    private readonly Color hoverColor = Color.red;

    private MagicElement selectedElement;
    private bool hasSelection = false;

    // 해당 팝업 Ui는 처음엔 "시작 선택"이었다가, 한 번 확정되면 이후로는 영구히 레벨업 팝업으로 남는다.
    private bool isLevelUpMode = false;

    private void Awake()
    {
        if (levelUpController == null)
        {
            levelUpController = FindFirstObjectByType<LevelUpController>();
        }
        if (fusionLineManager == null)
        {
            fusionLineManager = FindFirstObjectByType<FusionLineManager>();
        }
    }

    private void Start()
    {
        for (int i = 0; i < itemButtons.Length; i++)
        {
            int index = i;
            itemButtons[i].onClick.AddListener(() => SelectItem(index));

            EventTrigger trigger = itemButtons[i].gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = itemButtons[i].gameObject.AddComponent<EventTrigger>();
            }

            //오브젝트 호버, 드래그 감지 
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => OnPointerEnter(index));

            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => OnPointerExit(index));

            trigger.triggers.Add(pointerEnter);
            trigger.triggers.Add(pointerExit);
        }

        startButton.onClick.AddListener(OnClickStart);
        gainButton.onClick.AddListener(OnClickGain);
        cancelButton.onClick.AddListener(OnClickCancel);

        hasSelection = false;
        SelectItem(0, applySelectionVisual: false);
        fusionLineManager?.SetActiveElement(null);
    }

    private void SelectItem(int index) => SelectItem(index, applySelectionVisual: true);

    private void SelectItem(int index, bool applySelectionVisual)
    {
        selectedElement = (MagicElement)index;
        itemNameText.text = itemNames[index];
        itemDescriptionText.text = itemDescriptions[index];

        if (applySelectionVisual)
        {
            hasSelection = true;
            fusionLineManager?.SetActiveElement(selectedElement);
        }
    }

    private void OnPointerEnter(int index)
    {
        itemOutline[index].color = hoverColor;
        fusionLineManager?.SetActiveElement((MagicElement)index);
    }

    private void OnPointerExit(int index)
    {
        itemOutline[index].color = normalColor;
        fusionLineManager?.SetActiveElement(hasSelection ? selectedElement : (MagicElement?)null);
    }

    // 시작 원소 확정 (최초 1회)
    private void OnClickStart()
    {
        if (levelUpController == null) return;

        levelUpController.TryChooseStartingElement(selectedElement);
        Debug.Log($"{selectedElement} 시작 원소로 선택됨");

        SwitchToLevelUpMode();
        gameObject.SetActive(false);
    }



    // 레벨업 확정
    private void OnClickGain()
    {
        if (levelUpController == null) return;

        // TODO:실제 레벨업 확정 로직 연결
        Debug.Log($"{selectedElement} 레벨업으로 획득됨");

        gameObject.SetActive(false);
    }

    // 레벨업 취소 (스킬포인트 적립)
    private void OnClickCancel()
    {
        // TODO: 스킬포인트 +1 연결 => HudStaticUi에서 연결 가능 

        gameObject.SetActive(false);
    }

    private void SwitchToLevelUpMode() //Ui를 레벨업 모드로 전환 
    {
        isLevelUpMode = true;

        startTitle.SetActive(false);
        startButton.gameObject.SetActive(false);

        levelupTitle.SetActive(true);
        levelUpButtonGroup.SetActive(true);
    }
}