using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelUpController : MonoBehaviour
{
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerSkillSystem playerSkillSystem;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private GrayboxGameFlowView view;

    private readonly List<string> choiceLabels = new List<string>(3);
    private int pendingLevelUps;
    private bool isPresenting;
    private bool isSubscribed;

    public int PendingLevelUps => pendingLevelUps;

    private void Awake()
    {
        ResolveLocalDependencies();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        SyncViewToCurrentState();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Initialize(
        PlayerProgression progression,
        PlayerSkillSystem skillSystem,
        GameFlowController flowController,
        GrayboxGameFlowView gameFlowView)
    {
        Unsubscribe();

        playerProgression = progression;
        playerSkillSystem = skillSystem;
        gameFlowController = flowController;
        view = gameFlowView;

        if (isActiveAndEnabled)
        {
            Subscribe();
            SyncViewToCurrentState();
        }
    }

    private void HandleLevelUpRequested(int _)
    {
        if (gameFlowController != null && gameFlowController.State == GameFlowState.GameOver)
        {
            return;
        }

        if (pendingLevelUps < int.MaxValue)
        {
            pendingLevelUps++;
        }

        TryPresentNextLevelUp();
    }

    private void HandleChoiceSelected(int choiceIndex)
    {
        if (!isPresenting || gameFlowController == null ||
            gameFlowController.State != GameFlowState.LevelUp)
        {
            return;
        }

        if (playerSkillSystem == null || !playerSkillSystem.ApplyChoice(choiceIndex))
        {
            return;
        }

        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        isPresenting = false;
        view?.HideLevelUp();

        if (pendingLevelUps > 0)
        {
            TryPresentNextLevelUp();
            return;
        }

        gameFlowController.TryResumePlaying();
    }

    private void HandleStateChanged(GameFlowState state)
    {
        switch (state)
        {
            case GameFlowState.Playing:
                view?.HideGameOver();
                TryPresentNextLevelUp();
                break;

            case GameFlowState.LevelUp:
                view?.HideGameOver();
                break;

            case GameFlowState.GameOver:
                pendingLevelUps = 0;
                isPresenting = false;
                view?.HideLevelUp();
                view?.ShowGameOver();
                break;
        }
    }

    private void HandleRestartRequested()
    {
        if (gameFlowController != null &&
            gameFlowController.State == GameFlowState.GameOver)
        {
            gameFlowController.RestartCurrentScene();
        }
    }

    private void TryPresentNextLevelUp()
    {
        if (pendingLevelUps <= 0 || isPresenting ||
            playerSkillSystem == null || gameFlowController == null || view == null)
        {
            return;
        }

        if (!gameFlowController.TryEnterLevelUp())
        {
            return;
        }

        IReadOnlyList<SkillChoice> choices = playerSkillSystem.GetLevelUpChoices();
        if (choices == null || choices.Count == 0)
        {
            Debug.LogError("Level up choices are unavailable.", this);
            pendingLevelUps = 0;
            view.HideLevelUp();
            gameFlowController.TryResumePlaying();
            return;
        }

        choiceLabels.Clear();
        for (int i = 0; i < choices.Count; i++)
        {
            SkillChoice choice = choices[i];
            choiceLabels.Add(string.IsNullOrWhiteSpace(choice.Description)
                ? choice.Title
                : $"{choice.Title}\n{choice.Description}");
        }

        isPresenting = true;
        view.ShowLevelUp(choiceLabels);
    }

    private void SyncViewToCurrentState()
    {
        if (gameFlowController == null || view == null)
        {
            return;
        }

        view.HideLevelUp();
        HandleStateChanged(gameFlowController.State);
    }

    private void ResolveLocalDependencies()
    {
        if (playerProgression == null)
        {
            playerProgression = GetComponent<PlayerProgression>();
        }

        if (playerSkillSystem == null)
        {
            playerSkillSystem = GetComponent<PlayerSkillSystem>();
        }

        if (gameFlowController == null)
        {
            gameFlowController = GetComponent<GameFlowController>();
        }

        if (view == null)
        {
            view = GetComponentInChildren<GrayboxGameFlowView>(true);
        }
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        if (playerProgression != null)
        {
            playerProgression.LevelUpRequested += HandleLevelUpRequested;
        }

        if (gameFlowController != null)
        {
            gameFlowController.StateChanged += HandleStateChanged;
        }

        if (view != null)
        {
            view.ChoiceSelected += HandleChoiceSelected;
            view.RestartRequested += HandleRestartRequested;
        }

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (playerProgression != null)
        {
            playerProgression.LevelUpRequested -= HandleLevelUpRequested;
        }

        if (gameFlowController != null)
        {
            gameFlowController.StateChanged -= HandleStateChanged;
        }

        if (view != null)
        {
            view.ChoiceSelected -= HandleChoiceSelected;
            view.RestartRequested -= HandleRestartRequested;
        }

        isSubscribed = false;
    }
}
