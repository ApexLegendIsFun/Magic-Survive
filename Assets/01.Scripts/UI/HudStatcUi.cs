using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudStatcUi : MonoBehaviour
{
    [Header("Weapon Image")]
    [SerializeField] private Image[] weaponImages;

    [Header("Weapon Text")]
    [SerializeField] private TextMeshProUGUI[] weaponTexts;


    public void SetWeapon(int index, Sprite icon, string weaponName)
    {
        weaponImages[index].sprite = icon;
        weaponTexts[index].text = weaponName;
    }


}