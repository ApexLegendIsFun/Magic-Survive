using TMPro;
using UnityEngine;

public class DamageUi : MonoBehaviour
{
    //TODO: 오브젝트 풀 사용예정.
    [SerializeField] private TextMeshProUGUI damageText;

    public void SetDamage(int damage)
    {
        damageText.text = $"{damage}";
       
    }

    public void Show(Vector3 position, int damage)
    {
        transform.position = position;
        SetDamage(damage);
    }

    

}