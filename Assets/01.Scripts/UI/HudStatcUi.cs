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
        RefreshWeapons();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (isSubscribed || playerSkillSystem == null) return;

        playerSkillSystem.SkillLevelChanged += HandleSkillLevelChanged;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || playerSkillSystem == null) return;

        playerSkillSystem.SkillLevelChanged -= HandleSkillLevelChanged;
        isSubscribed = false;
    }

    private void HandleSkillLevelChanged(MagicElement element, int level) { RefreshWeapons(); }

    // 보유 여부와 무관하게 5칸 전부 고정 순서로 항상 표시, 레벨만 갱신 (미보유는 Lv.0)
    private void RefreshWeapons()
    {
        if (playerSkillSystem == null || weaponImages == null) return;

        var elements = MagicContentCatalog.PentagonElements; // 고정 순서 (오각형과 동일)

        for (int i = 0; i < weaponImages.Length; i++)
        {
            bool hasSlotElement = i < elements.Count;

            if (weaponImages[i] != null) weaponImages[i].enabled = hasSlotElement;
            if (!hasSlotElement)
            {
                if (weaponTexts != null && i < weaponTexts.Length && weaponTexts[i] != null)
                    weaponTexts[i].text = string.Empty;
                continue;
            }

            var element = elements[i];
            int level = playerSkillSystem.GetSkillLevel(element); // 미보유면 0

            SetWeapon(i, GetElementIcon(element), $"Lv.{level}");
        }
    }

    private Sprite GetElementIcon(MagicElement element)
    {
        var entry = elementIcons?.FirstOrDefault(e => e.element.Equals(element));
        if (entry == null)
        {
            Debug.Log($" {element}");
        }
        return entry?.icon;
    }

    public void SetWeapon(int index, Sprite icon, string weaponName)
    {
        if (index < 0 || index >= weaponImages.Length)
        {
            Debug.Log($"{index}");
            return;
        }
        if (weaponImages[index] != null) weaponImages[index].sprite = icon;
        if (weaponTexts != null && index < weaponTexts.Length && weaponTexts[index] != null)
            weaponTexts[index].text = weaponName;
    }

}