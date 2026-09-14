using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelUpController : MonoBehaviour
{
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private PlayerSkillSystem playerSkillSystem;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private RunDirector runDirector;
    [SerializeField] private GrayboxGameFlowView view;

    private PopupUi popupView;
    private bool UsePopup => popupView != null && popupView.IsConfigured;

    private int pendingLevelUps;
    private bool isPresenting;
    private bool isSubscribed;

    public int PendingLevelUps => pendingLevelUps;
    public bool IsPresenting => isPresenting;
    public bool CanSpendSkillPoint =>
        isPresenting &&
        pendingLevelUps > 0 &&
        gameFlowController != null &&
        gameFlowController.State == GameFlowState.LevelUp;
    public MagicElement? PendingSelection => playerSkillSystem != null
        ? playerSkillSystem.Tree.PendingSelection
        : null;

    public event Action LevelUpOpened;
    public event Action LevelUpClosed;

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
        popupView = FindFirstObjectByType<PopupUi>(FindObjectsInactive.Include);

        playerProgression = progression;
        playerSkillSystem = skillSystem;
        gameFlowController = flowController;
        view = gameFlowView;
        runDirector = GetComponent<RunDirector>();

        if (isActiveAndEnabled)
        {
            Subscribe();
            SyncViewToCurrentState();
        }
    }

    public bool TryChooseStartingElement(MagicElement element)
    {
        if (gameFlowController == null || playerSkillSystem == null ||
            gameFlowController.State != GameFlowState.ElementSelect ||
            !playerSkillSystem.TryChooseStartingElement(element))
        {
            return false;
        }

        HideElementSelection();
        return gameFlowController.TryBeginPlaying();
    }

    public bool TrySelectSkill(MagicElement element)
    {
        if (!isPresenting || gameFlowController == null || playerSkillSystem == null ||
            gameFlowController.State != GameFlowState.LevelUp)
        {
            return false;
        }

        bool selected = playerSkillSystem.TrySelectSkill(element);
        if (selected)
        {
            RefreshSkillSelection();
        }

        return selected;
    }

    public bool ConfirmSelectedSkill()
    {
        if (!isPresenting || gameFlowController == null || playerSkillSystem == null ||
            gameFlowController.State != GameFlowState.LevelUp)
        {
            return false;
        }

        return playerSkillSystem.ConfirmSelectedSkill();
    }

    private void HandleSkillPointSpent(MagicElement _)
    {
        if (!CanSpendSkillPoint)
        {
            return;
        }

        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        isPresenting = false;
        HideSkillSelection();
        LevelUpClosed?.Invoke();

        if (pendingLevelUps > 0)
        {
            TryPresentNextLevelUp();
            return;
        }

        gameFlowController.TryResumeActiveState();
    }

    private void HandleLevelUpRequested(int _)
    {
        if (gameFlowController == null || gameFlowController.IsTerminal)
        {
            return;
        }

        if (pendingLevelUps < int.MaxValue)
        {
            pendingLevelUps++;
        }

        TryPresentNextLevelUp();
    }

    private void HandleStateChanged(GameFlowState state)
    {
        switch (state)
        {
            case GameFlowState.ElementSelect:
                ShowElementSelection();
                break;

            case GameFlowState.Playing:
            case GameFlowState.Boss:
                HideElementSelection();
                view?.HideResult();
                TryPresentNextLevelUp();
                break;

            case GameFlowState.LevelUp:
                HideElementSelection();
                break;

            case GameFlowState.Victory:
            case GameFlowState.GameOver:
                pendingLevelUps = 0;
                isPresenting = false;
                playerSkillSystem?.CancelSelectedSkill();
                HideElementSelection();
                HideSkillSelection();
                if (runDirector == null || runDirector.Result == null)
                {
                    view?.ShowGameOver();
                }
                break;
        }
    }

    private void HandleTreeChanged()
    {
        if (isPresenting && playerSkillSystem != null)
        {
            RefreshSkillSelection();
        }
    }

    private void HandleResultReady(RunResult result)
    {
        view?.ShowResult(result);
    }

    private void HandleRestartRequested()
    {
        if (gameFlowController != null && gameFlowController.IsTerminal)
        {
            gameFlowController.RestartCurrentScene();
        }
    }

    private void HandleTitleRequested()
    {
        if (gameFlowController != null && gameFlowController.IsTerminal)
        {
            gameFlowController.LoadTitleScene();
        }
    }

    private void TryPresentNextLevelUp()
    {
        if (pendingLevelUps <= 0 || isPresenting || playerSkillSystem == null ||
            gameFlowController == null)
        {
            return;
        }

        if (playerSkillSystem.Tree.IsMaxed)
        {
            // PlayerProgression already heals 10% once per earned level.
            pendingLevelUps = 0;
            if (gameFlowController.State == GameFlowState.LevelUp)
                gameFlowController.TryResumeActiveState();
            return;
        }
        playerSkillSystem.PrepareChoices();
        if (playerSkillSystem.Choices.Count == 0)
        {
            Debug.LogError("Level-up has no usable skill definitions.", this);
            return;
        }

        if (gameFlowController.State != GameFlowState.LevelUp &&
            !gameFlowController.TryEnterLevelUp())
        {
            return;
        }

        isPresenting = true;
        ShowSkillSelection();
        LevelUpOpened?.Invoke();
    }

    private void ShowElementSelection()
    {
        if (UsePopup)
        {
            view?.HideElementSelect();
            popupView.ShowElementSelect();
        }
        else view?.ShowElementSelect(MagicContentCatalog.PentagonElements);
    }
    private void HideElementSelection()
    {
        view?.HideElementSelect();
        if (UsePopup) popupView.HideLevelUp();
    }
    private void ShowSkillSelection()
    {
        if (UsePopup)
        {
            view?.HideLevelUp();
            popupView.ShowSkillTree(playerSkillSystem);
        }
        else view?.ShowSkillTree(playerSkillSystem);
    }
    private void RefreshSkillSelection()
    {
        if (UsePopup) popupView.RefreshSkillTree(playerSkillSystem);
        else view?.RefreshSkillTree(playerSkillSystem);
    }
    private void HideSkillSelection()
    {
        view?.HideLevelUp();
        if (UsePopup) popupView.HideLevelUp();
    }

    private void SyncViewToCurrentState()
    {
        if (gameFlowController == null)
        {
            return;
        }

        HideElementSelection();
        HideSkillSelection();
        view?.HideResult();
        HandleStateChanged(gameFlowController.State);
    }

    private void ResolveLocalDependencies()
    {
        popupView = FindFirstObjectByType<PopupUi>(FindObjectsInactive.Include);
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

        if (runDirector == null)
        {
            runDirector = GetComponent<RunDirector>();
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

        if (playerSkillSystem != null)
        {
            playerSkillSystem.TreeChanged += HandleTreeChanged;
            playerSkillSystem.SkillPointSpent += HandleSkillPointSpent;
        }

        if (gameFlowController != null)
        {
            gameFlowController.StateChanged += HandleStateChanged;
        }

        if (runDirector != null)
        {
            runDirector.ResultReady += HandleResultReady;
        }

        if (view != null)
        {
            view.StartingElementSelected += HandleStartingElementSelected;
            view.SkillSelected += HandleSkillSelected;
            view.ConfirmRequested += HandleConfirmRequested;
            view.RestartRequested += HandleRestartRequested;
            view.TitleRequested += HandleTitleRequested;
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

        if (playerSkillSystem != null)
        {
            playerSkillSystem.TreeChanged -= HandleTreeChanged;
            playerSkillSystem.SkillPointSpent -= HandleSkillPointSpent;
        }

        if (gameFlowController != null)
        {
            gameFlowController.StateChanged -= HandleStateChanged;
        }

        if (runDirector != null)
        {
            runDirector.ResultReady -= HandleResultReady;
        }

        if (view != null)
        {
            view.StartingElementSelected -= HandleStartingElementSelected;
            view.SkillSelected -= HandleSkillSelected;
            view.ConfirmRequested -= HandleConfirmRequested;
            view.RestartRequested -= HandleRestartRequested;
            view.TitleRequested -= HandleTitleRequested;
        }

        isSubscribed = false;
    }

    private void HandleConfirmRequested()
    {
        ConfirmSelectedSkill();
    }

    private void HandleStartingElementSelected(MagicElement element)
    {
        TryChooseStartingElement(element);
    }

    private void HandleSkillSelected(MagicElement element)
    {
        TrySelectSkill(element);
    }
}
