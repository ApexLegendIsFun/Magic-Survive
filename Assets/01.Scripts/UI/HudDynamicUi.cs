using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class HudDynamicUi : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float gameTime = 900f; //기본값 15분 

    [Header("Count")]
    [SerializeField] private TextMeshProUGUI killCount;
  
    [Header("Bar/lvText")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image levelupBar;
    [SerializeField] private TextMeshProUGUI lvText;


    private float currentTime;
    private int lastSecond;

    private void Awake()
    {
        currentTime = gameTime;
        lastSecond = Mathf.CeilToInt(currentTime);

        UpdateTimerText();
    }

    private void Update()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            UpdateTimerText();

            enabled = false;
            return;
        }

        int currentSecond = Mathf.CeilToInt(currentTime); //소수점 숫자를 더 큰 정수로 올림처리 

        if (currentSecond != lastSecond)
        {
            lastSecond = currentSecond;
            UpdateTimerText();
        }
    }


    //1초마다 한번씩 그리도록 처리함 
    private void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void UpdateKillCount(int count) 
    {
        killCount.text = $"{count}";
    }


    public void UpdateLvtext(int level)
    {
        lvText.text = $"{level}";
    }


    public void UpdateHp(float currentHp, float maxHp)
    {
        hpBar.fillAmount = currentHp / maxHp;
    }


    public void UpdateLevelUp(float currentExp, float nextLevelUp)
    {
        levelupBar.fillAmount = currentExp / nextLevelUp;
    }


    //이하 일단 생략 


}
