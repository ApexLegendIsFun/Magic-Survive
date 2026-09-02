using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TitleSceneController : MonoBehaviour
{
    [SerializeField] private Button gameStartButton;
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private void OnEnable()
    {
        if (gameStartButton != null)
        {
            gameStartButton.onClick.AddListener(StartGame);
        }
    }

    private void OnDisable()
    {
        if (gameStartButton != null)
        {
            gameStartButton.onClick.RemoveListener(StartGame);
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }
}
