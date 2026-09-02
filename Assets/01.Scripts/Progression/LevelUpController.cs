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
    public SkillTreeNodeId? PendingSelection => playerSkillSystem != null
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

        view?.HideElementSelect();
        return gameFlowController.TryBeginPlaying();
    }

    public bool TrySelectNode(SkillTreeNodeId nodeId)
    {
        if (!isPresenting || gameFlowController == null || playerSkillSystem == null ||
            gameFlowController.State != GameFlowState.LevelUp)
        {
            return false;
        }

        bool selected = playerSkillSystem.TrySelectNode(nodeId);
        if (selected)
        {
            view?.RefreshSkillTree(playerSkillSystem);
        }

        return selected;
    }

    public bool ConfirmSelectedNode()
    {
        if (!isPresenting || gameFlowController == null || playerSkillSystem == null ||
            gameFlowController.State != GameFlowState.LevelUp)
        {
            return false;
        }

        return playerSkillSystem.ConfirmSelectedNode();
    }

    private void HandleSkillPointSpent(SkillTreeNodeId _)
    {
        if (!CanSpendSkillPoint)
        {
            return;
        }

        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        isPresenting = false;
        view?.HideLevelUp();
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
                view?.ShowElementSelect(SkillTreeCatalog.PentagonElements);
                break;

            case GameFlowState.Playing:
            case GameFlowState.Boss:
                view?.HideElementSelect();
                view?.HideResult();
                TryPresentNextLevelUp();
                break;

            case GameFlowState.LevelUp:
                view?.HideElementSelect();
                break;

            case GameFlowState.Victory:
            case GameFlowState.GameOver:
                pendingLevelUps = 0;
                isPresenting = false;
                playerSkillSystem?.CancelSelectedNode();
                view?.HideElementSelect();
                view?.HideLevelUp();
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
            view?.RefreshSkillTree(playerSkillSystem);
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

        if (gameFlowController.State != GameFlowState.LevelUp &&
            !gameFlowController.TryEnterLevelUp())
        {
            return;
        }

        isPresenting = true;
        view?.ShowSkillTree(playerSkillSystem);
        LevelUpOpened?.Invoke();
    }

    private void SyncViewToCurrentState()
    {
        if (gameFlowController == null || view == null)
        {
            return;
        }

        view.HideElementSelect();
        view.HideLevelUp();
        view.HideResult();
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
            view.NodeSelected += HandleNodeSelected;
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
            view.NodeSelected -= HandleNodeSelected;
            view.ConfirmRequested -= HandleConfirmRequested;
            view.RestartRequested -= HandleRestartRequested;
            view.TitleRequested -= HandleTitleRequested;
        }

        isSubscribed = false;
    }

    private void HandleConfirmRequested()
    {
        ConfirmSelectedNode();
    }

    private void HandleStartingElementSelected(MagicElement element)
    {
        TryChooseStartingElement(element);
    }

    private void HandleNodeSelected(SkillTreeNodeId nodeId)
    {
        TrySelectNode(nodeId);
    }
}
