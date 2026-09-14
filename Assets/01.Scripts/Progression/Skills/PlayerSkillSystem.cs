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
    private readonly List<MagicElement> choices = new List<MagicElement>(5);
    private readonly Dictionary<SpecializationId, int> specializationCounts = new Dictionary<SpecializationId, int>();
    private bool isCommitting;
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
    public event Action GrowthChanged;

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
    public bool TryChooseStartingElement(MagicElement element)
    {
        if (isCommitting || gameFlowController == null || gameFlowController.State != GameFlowState.ElementSelect ||
            !HasDefinition(element)) return false;
        isCommitting = true;
        try
        {
            if (!tree.TryChooseStartingElement(element)) return false;
            GrowthChanged?.Invoke();
            return true;
        }
        finally { isCommitting = false; }
    }

    // 보유·인접 원소를 고정 순서로 전부 제공한다.
    internal void PrepareChoices()
    {
        tree.Cancel();
        choices.Clear();
        foreach (var element in MagicContentCatalog.PentagonElements)
            if (tree.CanUpgrade(element) && HasDefinition(element)) choices.Add(element);
    }
    public bool IsOffered(MagicElement element) => choices.Contains(element);
    public bool TrySelectSkill(MagicElement element) =>
        !isCommitting && CanSpendSkillPoint() && IsOffered(element) && tree.TrySelectSkill(element);
    public bool CancelSelectedSkill() => !isCommitting && tree.Cancel();
    public bool ConfirmSelectedSkill()
    {
        return tree.PendingSelection.HasValue && TryCommitGrowth(tree.PendingSelection.Value, null);
    }

    public bool TryConfirmSpecialization(MagicElement element, SpecializationId id) => TryCommitGrowth(element, id);

    private bool TryCommitGrowth(MagicElement element, SpecializationId? id)
    {
        if (isCommitting || !CanSpendSkillPoint() || !IsOffered(element) ||
            !HasDefinition(element) || !tree.CanUpgrade(element)) return false;
        if (id.HasValue && (GetSkillLevel(element) == 0 ||
            !SpecializationCatalog.TryGet(id.Value, out var definition) || definition.Element != element)) return false;

        isCommitting = true;
        try
        {
            // Select before changing counts; all validation has completed. Reentrant writes are blocked.
            if (!tree.TrySelectSkill(element)) return false;
            if (id.HasValue) specializationCounts[id.Value] = GetSpecializationCount(id.Value) + 1;
            if (!tree.Confirm())
            {
                if (id.HasValue) specializationCounts[id.Value]--;
                return false;
            }
            choices.Clear();
            // Settle pending opportunities before publishing the UI snapshot, independent of subscriber order.
            levelUpController.CompleteGrowth();
            SkillPointSpent?.Invoke(element);
            GrowthChanged?.Invoke();
            return true;
        }
        finally { isCommitting = false; }
    }

    public int GetSpecializationCount(SpecializationId id) =>
        specializationCounts.TryGetValue(id, out int count) ? count : 0;

    // Display units: Percent 20 -> future multiplier 1.20; PercentagePoints 4 -> future additive rate 0.04.
    public float GetSpecializationBonus(SpecializationId id) =>
        SpecializationCatalog.TryGet(id, out var definition) ? GetSpecializationCount(id) * definition.Amount : 0f;

    public GrowthPreview GetGrowthPreview(MagicElement element)
    {
        bool valid = MagicContentCatalog.IsElement(element);
        int level = GetSkillLevel(element);
        bool max = valid && level >= MagicContentCatalog.MaxSkillLevel;
        bool eligible = valid && !max && tree.CanUpgrade(element) && HasDefinition(element);
        var state = !valid ? ElementGrowthState.Invalid : max ? ElementGrowthState.Max :
            !eligible ? ElementGrowthState.Locked : level == 0 ? ElementGrowthState.Unlock : ElementGrowthState.Upgrade;
        bool canSelect = eligible && CanSpendSkillPoint() && IsOffered(element);
        ElementSkillStats? current = valid && level > 0 ? MagicContentCatalog.GetStats(element, level) : (ElementSkillStats?)null;
        ElementSkillStats? next = valid && !max ? MagicContentCatalog.GetStats(element, level + 1) : (ElementSkillStats?)null;
        var cards = new List<SpecializationCardPreview>(3);
        if (level > 0)
            foreach (var definition in SpecializationCatalog.All)
                if (definition.Element == element)
                    cards.Add(new SpecializationCardPreview(definition, GetSpecializationCount(definition.Id), canSelect));
        return new GrowthPreview(element, level, state, canSelect, current, next, cards.AsReadOnly());
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
