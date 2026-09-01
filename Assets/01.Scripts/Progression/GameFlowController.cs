using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameFlowController : MonoBehaviour
{
    public GameFlowState State { get; private set; } = GameFlowState.Playing;
    public GameFlowState CurrentState => State;

    public event Action<GameFlowState> StateChanged;

    private bool isSubscribed;

    private void Awake()
    {
        State = GameFlowState.Playing;
        ApplyTimeScale(State);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        StateChanged?.Invoke(State);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        if (State != GameFlowState.Playing)
        {
            Time.timeScale = 1f;
        }
    }

    public bool TryEnterLevelUp()
    {
        if (State == GameFlowState.GameOver)
        {
            return false;
        }

        if (State != GameFlowState.LevelUp)
        {
            SetState(GameFlowState.LevelUp);
        }

        return true;
    }

    public bool TryResumePlaying()
    {
        if (State != GameFlowState.LevelUp)
        {
            return false;
        }

        SetState(GameFlowState.Playing);
        return true;
    }

    public void EnterGameOver()
    {
        if (State == GameFlowState.GameOver)
        {
            return;
        }

        SetState(GameFlowState.GameOver);
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(currentScene.buildIndex);
            return;
        }

        SceneManager.LoadScene(currentScene.name);
    }

    private void HandlePlayerDied()
    {
        EnterGameOver();
    }

    private void SetState(GameFlowState nextState)
    {
        if (State == nextState)
        {
            return;
        }

        State = nextState;
        ApplyTimeScale(State);
        StateChanged?.Invoke(State);
    }

    private static void ApplyTimeScale(GameFlowState state)
    {
        Time.timeScale = state == GameFlowState.Playing ? 1f : 0f;
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        GameEvents.PlayerDied += HandlePlayerDied;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        GameEvents.PlayerDied -= HandlePlayerDied;
        isSubscribed = false;
    }
}
