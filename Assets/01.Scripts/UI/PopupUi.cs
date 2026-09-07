using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PopupUi : MonoBehaviour
{
    [Header("Level Up Controller")]
    [SerializeField] private LevelUpController levelUpController;

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

    private void Awake()
    {
        if (levelUpController == null)
        {
            levelUpController = FindFirstObjectByType<LevelUpController>();
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

        SelectItem(0);
    }

    private void SelectItem(int index)
    {
        selectedElement = (MagicElement)index;

        itemNameText.text = itemNames[index];
        itemDescriptionText.text = itemDescriptions[index];
    }

    private void OnPointerEnter(int index)
    {
        itemOutline[index].color = hoverColor;
    }

    private void OnPointerExit(int index)
    {
        itemOutline[index].color = normalColor;
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