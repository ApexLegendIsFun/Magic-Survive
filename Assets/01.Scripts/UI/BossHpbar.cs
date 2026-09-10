using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHpbar : MonoBehaviour
{
    // Hpbar는 보스에게만 존재하므로 클래스명 변경함

    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI bossTimer;
    [SerializeField] private Image bossHabar;

    public void UpdateBossHp(float currentHp, float maxHp)
    {

    }

    public void UpdateBossTimer()
    {

    }



}
