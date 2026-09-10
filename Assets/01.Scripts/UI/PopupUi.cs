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

    // PS. 융합부분은 전부 주석처리했습니다.


    // 융합 (현재 미정이므로 주석처리)
    //[System.Serializable]
    //public class FusionSlot
    //{
    //    public FusionKind fusion;
    //    public Button button;
    //    public Image icon;
    //    public TextMeshProUGUI statusLabel;

    //    public GameObject lockIcon;
    //    public GameObject checkmark;
    //    public GameObject pendingHighlight;
    //}


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

    [SerializeField] private Button gainButton;
    [SerializeField] private Button cancelButton;

    // 레벨업 원소 슬롯
    [Header("LevelUp Element Slots")]
    [SerializeField] private ElementSlot[] elementSlots;

    // 레벨업 융합 슬롯
    //[Header("LevelUp Fusion Slots")]
    //[SerializeField] private FusionSlot[] fusionSlots;

    // 중앙 미리보기
    [Header("Selected Node Preview")]
    [SerializeField] private TextMeshProUGUI previewNameText;
    [SerializeField] private TextMeshProUGUI previewDescriptionText;

    private PlayerSkillSystem currentSkillSystem;

    private bool isLevelUpMode = false;


    // 초기화

    private void Awake()
    {
        if (levelUpController == null)
        {
            levelUpController = FindFirstObjectByType<LevelUpController>();
        }
    }


    private void Start()
    {
        SetupStartSelect();

        // 레벨업 버튼
        gainButton.onClick.AddListener(OnClickGain);
        cancelButton.onClick.AddListener(OnClickCancel);

        // 원소 슬롯
        foreach (var slot in elementSlots)
        {
            var captured = slot;

            captured.button.onClick.AddListener(
                () => OnClickElementSlot(captured)
            );
        }

        // 융합 슬롯
        //foreach (var slot in fusionSlots)
        //{
        //    var captured = slot;

        //    captured.button.onClick.AddListener(
        //        () => OnClickFusionSlot(captured)
        //    );
        //}
    }


    // 시작 원소 선택

    private void SetupStartSelect()
    {
        for (int i = 0; i < startItemButtons.Length; i++)
        {
            int index = i;

            // 클릭
            startItemButtons[i].onClick.AddListener(
                () => SelectStartElement(index)
            );

            // 호버
            EventTrigger trigger =
                startItemButtons[i].gameObject.GetComponent<EventTrigger>();

            if (trigger == null)
            {
                trigger =
                    startItemButtons[i].gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry pointerEnter =
                new EventTrigger.Entry();

            pointerEnter.eventID =
                EventTriggerType.PointerEnter;

            pointerEnter.callback.AddListener(
                (_) => startItemOutline[index].color = hoverColor
            );


            EventTrigger.Entry pointerExit =
                new EventTrigger.Entry();

            pointerExit.eventID =
                EventTriggerType.PointerExit;

            pointerExit.callback.AddListener(
                (_) => startItemOutline[index].color = normalColor
            );

            trigger.triggers.Add(pointerEnter);
            trigger.triggers.Add(pointerExit);
        }

        startButton.onClick.AddListener(OnClickStart);

        // 처음에는 0번 원소 선택
        SelectStartElement(0);
    }


    private void SelectStartElement(int index)
    {
        selectedStartElement = (MagicElement)index;

        var chain =
            LevelUpSlotMapper.GetElementChain(selectedStartElement);

        var def = chain[0];

        previewNameText.text = def.DisplayName;
        previewDescriptionText.text = def.Description;
    }

    // 시작 원소 확정
    private void OnClickStart()
    {
        if (levelUpController == null)
        {
            return;
        }

        levelUpController.TryChooseStartingElement(
            selectedStartElement
        );

        Debug.Log(
            $"{selectedStartElement} 시작 원소로 선택됨"
        );

        SwitchToLevelUpMode();

        //시작 선택 팝업 닫기.
        gameObject.SetActive(false);
    }



    // 레벨업 모드 전환
    private void SwitchToLevelUpMode()
    {
        isLevelUpMode = true;

        startTitle.SetActive(false);
        startButton.gameObject.SetActive(false);

        levelupTitle.SetActive(true);
        levelUpButtonGroup.SetActive(true);
    }

    // 레벨업 UI 표시
    public void ShowSkillTree(PlayerSkillSystem skillSystem)
    {
        currentSkillSystem = skillSystem;

        RefreshSkillTree(skillSystem);
    }


    public void RefreshSkillTree(PlayerSkillSystem skillSystem)
    {
        if (skillSystem == null)
        {
            return;
        }

        currentSkillSystem = skillSystem;

        var pending =
            skillSystem.Tree.PendingSelection;


        // 원소
        foreach (var slot in elementSlots)
        {
            var chain =
                LevelUpSlotMapper.GetElementChain(slot.element);

            var (node, maxed) =
                LevelUpSlotMapper.GetRepresentativeNode(
                    chain,
                    skillSystem.Tree
                );

            var state =
                skillSystem.GetNodePreview(node.Id).State;

            ApplySlotVisual(
                node,
                maxed,
                pending,
                state,
                slot.button,
                slot.statusLabel,
                slot.lockIcon,
                slot.checkmark,
                slot.pendingHighlight
            );
        }


 
        //융합 아직은 미정. => (추후 가능하다면 추가 )
        //foreach (var slot in fusionSlots)
        //{
        //    var chain =
        //        LevelUpSlotMapper.GetFusionChain(slot.fusion);

        //    var (node, maxed) =
        //        LevelUpSlotMapper.GetRepresentativeNode(
        //            chain,
        //            skillSystem.Tree
        //        );

        //    var state =
        //        skillSystem.GetNodePreview(node.Id).State;


        //    // 아직 융합 조건이 안 되면 숨김
        //    slot.button.gameObject.SetActive(
        //        state != SkillTreeNodeState.Hidden
        //    );

        //    if (state == SkillTreeNodeState.Hidden)
        //    {
        //        continue;
        //    }


        //    ApplySlotVisual(
        //        node,
        //        maxed,
        //        pending,
        //        state,
        //        slot.button,
        //        slot.statusLabel,
        //        slot.lockIcon,
        //        slot.checkmark,
        //        slot.pendingHighlight
        //    );
        //}


        // 선택된 노드가 있을 때만 획득 가능
        gainButton.interactable =
            pending.HasValue;
    }


    // 슬롯 UI 갱신

    private void ApplySlotVisual(
        SkillTreeNodeDefinition node,
        bool maxed,
        SkillTreeNodeId? pending,
        SkillTreeNodeState state,
        Button button,
        TextMeshProUGUI statusLabel,
        GameObject lockIcon,
        GameObject checkmark,
        GameObject pendingHighlight)
    {
        bool isPending =
            pending.HasValue &&
            pending.Value.Equals(node.Id);


        button.interactable =
            state == SkillTreeNodeState.Available;

        lockIcon.SetActive(
            state == SkillTreeNodeState.Locked
        );

        checkmark.SetActive(
            state == SkillTreeNodeState.Owned
        );

        pendingHighlight.SetActive(
            isPending
        );


        statusLabel.text =
            maxed
                ? "완료"
                : state == SkillTreeNodeState.Owned
                    ? "보유"
                    : state == SkillTreeNodeState.Locked
                        ? "잠김"
                        : string.Empty;
    }


    // 원소 슬롯 클릭


    private void OnClickElementSlot(ElementSlot slot)
    {
        if (currentSkillSystem == null)
        {
            return;
        }

        var chain =
            LevelUpSlotMapper.GetElementChain(slot.element);

        var (node, maxed) =
            LevelUpSlotMapper.GetRepresentativeNode(
                chain,
                currentSkillSystem.Tree
            );

        TrySelectAndPreview(node, maxed);
    }


    // 융합 슬롯 클릭
    //private void OnClickFusionSlot(FusionSlot slot)
    //{
    //    if (currentSkillSystem == null)
    //    {
    //        return;
    //    }

    //    var chain =
    //        LevelUpSlotMapper.GetFusionChain(slot.fusion);

    //    var (node, maxed) =
    //        LevelUpSlotMapper.GetRepresentativeNode(
    //            chain,
    //            currentSkillSystem.Tree
    //        );

    //    TrySelectAndPreview(node, maxed);
    //}


    // 노드 선택

    private void TrySelectAndPreview(
        SkillTreeNodeDefinition node,
        bool maxed)
    {
        if (maxed)
        {
            return;
        }

        if (levelUpController == null)
        {
            return;
        }

        bool success =
            levelUpController.TrySelectNode(node.Id);

        if (!success)
        {
            return;
        }


        // 중앙 미리보기
        previewNameText.text =
            node.DisplayName;

        previewDescriptionText.text =
            node.Description;


        // 슬롯 상태 갱신
        RefreshSkillTree(currentSkillSystem);
    }


    // 레벨업 획득
    // 레벨업 Ui는  현재 만들어놓고 연동만 하면됨.
    private void OnClickGain()
    {
        if (levelUpController == null)
        {
            return;
        }

    }


    // 레벨업 취소

    private void OnClickCancel()
    {

        //아직은 캔슬기능이 존재하지 않음. 

      
    }

    // 레벨업 UI 숨김

    public void HideLevelUp()
    {
        currentSkillSystem = null;
    }
}