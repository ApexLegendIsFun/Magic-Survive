using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum Item
{
    Fire,
    Elec,
    Ice,
    Earth,
    Dark
}

public class PopupUi : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private TextMeshProUGUI itemNameText; // 실제 출력될 아이템 이름
    [SerializeField] private TextMeshProUGUI itemDescriptionText; // 실제 출력될 아이템 설명

    [Header("Item Button")]
    [SerializeField] private Button[] itemButtons; // 아이템 버튼&이미지

    [Header("Item Info")]
    [SerializeField] private string[] itemNames; // 아이템 이름 배열
    [SerializeField] private string[] itemDescriptions; // 아이템 설명 배열

    [Header("Item Outline")]
    [SerializeField] private Image[] itemOutline; // Hover용 테두리 이미지

    [Header("Start Button")]
    [SerializeField] private Button startButton;

    private Color normalColor = Color.white;
    private Color hoverColor = Color.red;

    // 현재 선택한 아이템
    private Item selectedItem = Item.Fire;

    private void Start()
    {
        for (int i = 0; i < itemButtons.Length; i++)
        {
            int index = i;

            // 클릭
            itemButtons[i].onClick.AddListener(() => SelectItem(index));

            // 마우스 이벤트
            EventTrigger trigger = itemButtons[i].gameObject.AddComponent<EventTrigger>();

            // 마우스를 올렸을 때
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => OnPointerEnter(index));

            // 마우스를 뗐을 때
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => OnPointerExit(index));

            trigger.triggers.Add(pointerEnter);
            trigger.triggers.Add(pointerExit);
        }

        // 시작 기본값 : 화염
        SelectItem(0);

        // 시작 버튼
        startButton.onClick.AddListener(OnClickStart);
    }

    private void OnPointerEnter(int index)
    {
        itemOutline[index].color = hoverColor;
    }

    private void OnPointerExit(int index)
    {
        itemOutline[index].color = normalColor;
    }

    private void SelectItem(int index)
    {
        // 현재 선택한 아이템 저장
        selectedItem = (Item)index;

        // 해당 아이템의 이름과 설명 출력
        itemNameText.text = itemNames[index];
        itemDescriptionText.text = itemDescriptions[index];
    }

    private void OnClickStart()
    {
        //값 전달 => selectedItem 
        Debug.Log($"선택한 아이템 : {selectedItem}");
    }

    // 다른 스크립트에서 선택한 아이템을 가져감
    public Item GetSelectedItem()
    {
        return selectedItem;
    }
}