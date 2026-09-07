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

    [Header("Start Button")]
    [SerializeField] private Button startButton;

    private readonly Color normalColor = Color.white;
    private readonly Color hoverColor = Color.red;

    private MagicElement selectedElement;
    private bool hasSelection = false;

    private void Awake()
    {

        //첫 오브젝트 선택을위해 레벨업 컨트롤러를 사용함 
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

        // 아직 아무것도 확정 선택되지 않은 초기 상태.
        // 첫 번째 아이템 정보만 미리 보여주되, 선(융합 가능선)은 중립 상태로 둔다.
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

        // hover 중에는 hover된 원소 기준으로 미리보기
        fusionLineManager?.SetActiveElement((MagicElement)index);
    }

    private void OnPointerExit(int index)
    {
        itemOutline[index].color = normalColor;

        // hover가 끝나면: 이미 확정 선택된 게 있으면 그 상태로 복귀,
        // 없으면 중립(전부 기본색) 상태로 복귀
        fusionLineManager?.SetActiveElement(hasSelection ? selectedElement : (MagicElement?)null);
    }

    private void OnClickStart()
    {
        if (levelUpController == null)
        {
            return;
        }
        //levelUpController 참조함 (임시)
        levelUpController.TryChooseStartingElement(selectedElement);
        gameObject.SetActive(false);
        Debug.Log($"{selectedElement} 선택됨");
    }
}