using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudStatcUi : MonoBehaviour
{
    [Header("Weapon Image")]
    [SerializeField] private Image[] weaponImages;

    [Header("Weapon Text")]
    [SerializeField] private TextMeshProUGUI[] weaponTexts;

    [Header("Fusion Weapon Image")]
    [SerializeField] private Image FusionweaponImages;

    [Header("Fusion Weapon Text")]
    [SerializeField] private TextMeshProUGUI FusionweaponText;

    [Header("LevelUp PointText & Button")]
    [SerializeField] private TextMeshProUGUI levelupPoint;
    [SerializeField] private Button levelUpButton;

    public void SetWeapon(int index, Sprite icon, string weaponName)
    {
        weaponImages[index].sprite = icon;
        weaponTexts[index].text = weaponName;
    }



    public void SetFusionWeapon(Sprite icon, string weaponName)
    {
        FusionweaponImages.sprite = icon;
        FusionweaponText.text = weaponName;
    }


    
}