using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSkillSystem : MonoBehaviour
{
    [SerializeField] private WeaponRunner weaponRunner;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private LevelUpController levelUpController;
    [SerializeField] private ProjectileMagicDefinition startingMagic;
    [SerializeField] private ProjectileMagicDefinition[] targetedMagicDefinitions = Array.Empty<ProjectileMagicDefinition>();
    private readonly PlayerSkillTree tree = new PlayerSkillTree();
    private readonly Dictionary<MagicElement, ProjectileMagicDefinition> definitions = new Dictionary<MagicElement, ProjectileMagicDefinition>();
    private readonly Dictionary<MagicElement, MagicRuntime> runtimes = new Dictionary<MagicElement, MagicRuntime>();
    private readonly List<MagicRuntime> activeMagics = new List<MagicRuntime>(5);
    private readonly List<MagicElement> choices = new List<MagicElement>(3);
    private IReadOnlyList<MagicElement> choicesView;
    private IReadOnlyList<MagicRuntime> activeMagicsView;
    public IReadOnlyPlayerSkillTree Tree => tree;
    public MagicRuntime CurrentMagic { get; private set; }
    public IReadOnlyList<MagicRuntime> ActiveMagics => activeMagicsView ?? (activeMagicsView = activeMagics.AsReadOnly());
    public IReadOnlyList<MagicElement> Choices => choicesView ?? (choicesView = choices.AsReadOnly());
    public event Action TreeChanged { add => tree.TreeChanged += value; remove => tree.TreeChanged -= value; }
    public event Action<MagicElement> ElementUnlocked { add => tree.ElementUnlocked += value; remove => tree.ElementUnlocked -= value; }
    public event Action<MagicElement, int> SkillLevelChanged;
    public event Action<MagicElement> SkillPointSpent;

    private void Awake()
    {
        if (gameFlowController == null) gameFlowController = GetComponent<GameFlowController>();
        if (levelUpController == null) levelUpController = GetComponent<LevelUpController>();
        if (weaponRunner == null)
        {
            Debug.LogError("PlayerSkillSystem: WeaponRunner missing.", this);
            enabled = false;
            return;
        }
        if (startingMagic != null) definitions[startingMagic.Element] = startingMagic;
        if (targetedMagicDefinitions != null)
            foreach (var definition in targetedMagicDefinitions)
                if (definition != null) definitions[definition.Element] = definition;
        tree.SkillLevelChanged += HandleSkillLevelChanged;
    }
    private void OnDestroy()
    {
        tree.SkillLevelChanged -= HandleSkillLevelChanged;
        if (weaponRunner != null)
            foreach (var runtime in activeMagics) weaponRunner.Unregister(runtime);
    }
    private bool HasDefinition(MagicElement element) =>
        definitions.TryGetValue(element, out var definition) && definition.ProjectilePrefab != null;
    public int GetSkillLevel(MagicElement element) => tree.GetSkillLevel(element);
    public IReadOnlyList<MagicElement> GetOwnedElements() => tree.OwnedElements;
    public bool TryChooseStartingElement(MagicElement element) =>
        gameFlowController != null && gameFlowController.State == GameFlowState.ElementSelect &&
        HasDefinition(element) && tree.TryChooseStartingElement(element);

    // 카드는 창을 열 때 한 번 추첨한다. 선택/취소로 재추첨하지 않는다.
    internal void PrepareChoices()
    {
        tree.Cancel();
        choices.Clear();
        var candidates = new List<MagicElement>(5);
        foreach (var element in MagicContentCatalog.PentagonElements)
            if (tree.CanUpgrade(element) && HasDefinition(element)) candidates.Add(element);
        while (choices.Count < 3 && candidates.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            choices.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
    }
    public bool IsOffered(MagicElement element) => choices.Contains(element);
    public bool TrySelectSkill(MagicElement element) =>
        CanSpendSkillPoint() && IsOffered(element) && tree.TrySelectSkill(element);
    public bool CancelSelectedSkill() => tree.Cancel();
    public bool ConfirmSelectedSkill()
    {
        if (!CanSpendSkillPoint() || !tree.PendingSelection.HasValue ||
            !IsOffered(tree.PendingSelection.Value)) return false;
        MagicElement element = tree.PendingSelection.Value;
        if (!tree.Confirm()) return false;
        choices.Clear();
        SkillPointSpent?.Invoke(element);
        return true;
    }
    private bool CanSpendSkillPoint() => gameFlowController != null && levelUpController != null &&
        gameFlowController.State == GameFlowState.LevelUp && levelUpController.CanSpendSkillPoint;
    private void HandleSkillLevelChanged(MagicElement element, int level)
    {
        if (!runtimes.TryGetValue(element, out var runtime))
        {
            runtime = new MagicRuntime(definitions[element]);
            runtimes.Add(element, runtime);
            activeMagics.Add(runtime);
            CurrentMagic = CurrentMagic ?? runtime;
            weaponRunner.Register(runtime);
        }
        runtime.SetSkillLevel(level);
        SkillLevelChanged?.Invoke(element, level);
    }
}
