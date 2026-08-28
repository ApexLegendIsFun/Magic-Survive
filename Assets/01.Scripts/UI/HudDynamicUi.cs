using TMPro;
using UnityEngine;

public class HudDynamicUi : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float gameTime = 900f; //기본값 15분 

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

            GameEnd(); // TODO : 게임종료 팝업임시 
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

    private void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void GameEnd()
    {
        Debug.Log("게임 종료!");
    }
}
