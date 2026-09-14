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

    private void RefreshWeapons()
    {
        if (playerSkillSystem == null || weaponImages == null) return;
        var elements = playerSkillSystem.GetOwnedElements();
        for (int i = 0; i < weaponImages.Length; i++)
        {
            bool owned = i < elements.Count;
            if (weaponImages[i] != null) weaponImages[i].enabled = owned;
            if (weaponTexts != null && i < weaponTexts.Length && weaponTexts[i] != null)
                weaponTexts[i].text = string.Empty;
            if (!owned) continue;
            var element = elements[i];
            SetWeapon(i, GetElementIcon(element),
                $"{MagicContentCatalog.GetDisplayName(element)} Lv.{playerSkillSystem.GetSkillLevel(element)}");
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
