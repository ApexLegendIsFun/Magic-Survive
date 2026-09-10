using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudStatcUi : MonoBehaviour
{
    [System.Serializable]
    public class ElementIconEntry
    {
        public MagicElement element;
        public Sprite icon;
    }

    [Header("Weapon Image")]
    [SerializeField] private Image[] weaponImages;

    [Header("Weapon Text")]
    [SerializeField] private TextMeshProUGUI[] weaponTexts;

    //해당 부분은 아직 주석처리
    //[Header("Fusion Weapon Image")]
    //[SerializeField] private Image fusionWeaponImage;

    //[Header("Fusion Weapon Text")]
    //[SerializeField] private TextMeshProUGUI fusionWeaponText;

    //레벨업 포인트(취소시 생기는) 마찬가지로 해당부분도 주석처리
    //[Header("LevelUp PointText & Button")]
    //[SerializeField] private TextMeshProUGUI levelupPoint;
    //[SerializeField] private Button levelUpButton;

    [Header("Data Source")]
    [SerializeField] private PlayerSkillSystem playerSkillSystem;

    [Header("Icon Lookup ")]
    [SerializeField] private ElementIconEntry[] elementIcons;

    private bool isSubscribed;

    private void Awake()
    {
        if (playerSkillSystem == null)
        {
            playerSkillSystem = FindFirstObjectByType<PlayerSkillSystem>();
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (isSubscribed || playerSkillSystem == null) return;

        playerSkillSystem.ElementUnlocked += HandleElementUnlocked;
        //playerSkillSystem.FusionUnlocked += HandleFusionUnlocked; // 융합 미확정이라 주석처리
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || playerSkillSystem == null) return;

        playerSkillSystem.ElementUnlocked -= HandleElementUnlocked;
        //playerSkillSystem.FusionUnlocked -= HandleFusionUnlocked;
        isSubscribed = false;
    }

    private void HandleElementUnlocked(MagicElement element)
    {
       
        int slotIndex = playerSkillSystem.GetOwnedElements().Count - 1;

        var targetNodeId = SkillTreeCatalog.GetTargetNode(element);
        var targetNode = SkillTreeCatalog.GetNode(targetNodeId);
        Sprite icon = GetElementIcon(element);

        SetWeapon(slotIndex, icon, targetNode.DisplayName);
    }

    //융합은 아직 주석처리 
    //private void HandleFusionUnlocked(FusionKind fusion)
    //{
    //    var fusionDef = SkillTreeCatalog.GetFusion(fusion);
    //    SetFusionWeapon(GetFusionIcon(fusion), fusionDef.DisplayName);
    //}


    private Sprite GetElementIcon(MagicElement element)
    {
        var entry = elementIcons?.FirstOrDefault(e => e.element.Equals(element));
        if (entry == null)
        {
            Debug.Log($"[HudStatcUi] {element} 아이콘이 elementIcons에 등록되어 있지 않음");
        }
        return entry?.icon;
    }

    public void SetWeapon(int index, Sprite icon, string weaponName)
    {
        if (index < 0 || index >= weaponImages.Length)
        {
            Debug.Log($"[HudStatcUi] 유효하지 않은 무기 슬롯 index: {index}");
            return;
        }
        weaponImages[index].sprite = icon;
        weaponTexts[index].text = weaponName;
    }

    //융합은 아직 주석처리
    //public void SetFusionWeapon(Sprite icon, string weaponName)
    //{
    //    fusionWeaponImage.sprite = icon;
    //    fusionWeaponText.text = weaponName;
    //}
}