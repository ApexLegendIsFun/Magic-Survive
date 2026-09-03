using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameFlowController : MonoBehaviour
{
    public GameFlowState State { get; private set; } = GameFlowState.ElementSelect;
    public GameFlowState CurrentState => State;
    public GameFlowState ResumeState { get; private set; } = GameFlowState.Playing;
    public bool IsTerminal => State == GameFlowState.Victory || State == GameFlowState.GameOver;

    public event Action<GameFlowState> StateChanged;

    private bool isSubscribed;

    private void Awake()
    {
        State = GameFlowState.ElementSelect;
        ResumeState = GameFlowState.Playing;
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
        Time.timeScale = 1f;
    }

    public bool TryBeginPlaying()
    {
        if (State != GameFlowState.ElementSelect)
        {
            return false;
        }

        SetState(GameFlowState.Playing);
        return true;
    }

    public bool TryEnterLevelUp()
    {
        if (State != GameFlowState.Playing && State != GameFlowState.Boss)
        {
            return false;
        }

        ResumeState = State;
        SetState(GameFlowState.LevelUp);
        return true;
    }

    public bool TryResumeActiveState()
    {
        if (State != GameFlowState.LevelUp)
        {
            return false;
        }

        SetState(ResumeState == GameFlowState.Boss
            ? GameFlowState.Boss
            : GameFlowState.Playing);
        return true;
    }

    public bool TryResumePlaying()
    {
        return TryResumeActiveState();
    }

    public bool TryEnterBoss()
    {
        if (State != GameFlowState.Playing)
        {
            return false;
        }

        SetState(GameFlowState.Boss);
        return true;
    }

    public bool TryEnterVictory()
    {
        if (State != GameFlowState.Boss)
        {
            return false;
        }

        SetState(GameFlowState.Victory);
        return true;
    }

    public void EnterGameOver()
    {
        if (IsTerminal)
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

    public void LoadTitleScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
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
        bool combatIsActive = state == GameFlowState.Playing || state == GameFlowState.Boss;
        Time.timeScale = combatIsActive ? 1f : 0f;
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
