using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSkillSystem : MonoBehaviour
{
    private static readonly SkillTreeNodeId[] LegacyCommonChoices =
    {
        SkillTreeNodeId.CommonPower,
        SkillTreeNodeId.CommonRapidFire,
        SkillTreeNodeId.CommonPierce
    };

    [SerializeField] private WeaponRunner weaponRunner;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private LevelUpController levelUpController;
    [SerializeField] private ProjectileMagicDefinition startingMagic;
    [SerializeField] private ProjectileMagicDefinition[] targetedMagicDefinitions =
        Array.Empty<ProjectileMagicDefinition>();

    private readonly PlayerSkillTree tree = new PlayerSkillTree();
    private readonly Dictionary<MagicId, ProjectileMagicDefinition> definitions =
        new Dictionary<MagicId, ProjectileMagicDefinition>();
    private readonly Dictionary<MagicId, MagicRuntime> activeMagicLookup =
        new Dictionary<MagicId, MagicRuntime>();
    private readonly List<MagicRuntime> activeMagics = new List<MagicRuntime>(15);

    public IReadOnlyPlayerSkillTree Tree => tree;
    public MagicRuntime CurrentMagic { get; private set; }
    public IReadOnlyList<MagicRuntime> ActiveMagics => activeMagics;
    public float GlobalDamageMultiplier { get; private set; } = 1f;
    public float GlobalCooldownMultiplier { get; private set; } = 1f;
    public int BonusPierce { get; private set; }
    public int BonusChainTargets { get; private set; }

    public event Action TreeChanged
    {
        add => tree.TreeChanged += value;
        remove => tree.TreeChanged -= value;
    }

    public event Action<SkillTreeNodeId> NodeOwned
    {
        add => tree.NodeOwned += value;
        remove => tree.NodeOwned -= value;
    }

    public event Action<MagicElement> ElementUnlocked
    {
        add => tree.ElementUnlocked += value;
        remove => tree.ElementUnlocked -= value;
    }

    public event Action<FusionKind> FusionUnlocked
    {
        add => tree.FusionUnlocked += value;
        remove => tree.FusionUnlocked -= value;
    }

    public event Action<MagicId> MagicUnlocked;
    public event Action<SkillChoice> ChoiceApplied;
    public event Action<SkillTreeNodeId> SkillPointSpent;

    private void Awake()
    {
        if (gameFlowController == null)
        {
            gameFlowController = GetComponent<GameFlowController>();
        }

        if (levelUpController == null)
        {
            levelUpController = GetComponent<LevelUpController>();
        }

        if (weaponRunner == null)
        {
            Debug.LogError("PlayerSkillSystem에 WeaponRunner가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        IndexDefinitions();
        tree.MagicUnlocked += HandleMagicUnlocked;
        tree.NodeOwned += HandleNodeOwned;
    }

    private void OnDestroy()
    {
        tree.MagicUnlocked -= HandleMagicUnlocked;
        tree.NodeOwned -= HandleNodeOwned;

        if (weaponRunner == null)
        {
            return;
        }

        for (int index = 0; index < activeMagics.Count; index++)
        {
            weaponRunner.Unregister(activeMagics[index]);
        }
    }

    public bool TryChooseStartingElement(MagicElement element)
    {
        if (gameFlowController == null ||
            gameFlowController.State != GameFlowState.ElementSelect)
        {
            return false;
        }

        return tree.TryChooseStartingElement(element);
    }

    public SkillTreeNodeState GetNodeState(SkillTreeNodeId id)
    {
        return tree.GetNodeState(id);
    }

    public bool TrySelectNode(SkillTreeNodeId id)
    {
        if (!CanSpendSkillPoint())
        {
            return false;
        }

        return tree.TrySelectNode(id);
    }

    public bool CancelSelectedNode()
    {
        return tree.Cancel();
    }

    public bool ConfirmSelectedNode()
    {
        if (!CanSpendSkillPoint() || !tree.PendingSelection.HasValue)
        {
            return false;
        }

        SkillTreeNodeId selectedNode = tree.PendingSelection.Value;
        if (!tree.Confirm())
        {
            return false;
        }

        SkillPointSpent?.Invoke(selectedNode);
        return true;
    }

    public IReadOnlyList<SkillTreeNodeDefinition> GetTreeDefinitions()
    {
        return SkillTreeCatalog.Nodes;
    }

    public SkillTreeNodePreview GetNodePreview(SkillTreeNodeId nodeId)
    {
        SkillTreeNodeDefinition definition = SkillTreeCatalog.GetNode(nodeId);
        string currentValue = "미보유";
        string appliedValue = "획득";

        if (definition.CommonUpgrade.HasValue)
        {
            switch (definition.CommonUpgrade.Value)
            {
                case CommonUpgradeKind.Power:
                    currentValue = $"×{GlobalDamageMultiplier:0.##}";
                    appliedValue = $"×{GlobalDamageMultiplier * 1.15f:0.##}";
                    break;
                case CommonUpgradeKind.RapidFire:
                    currentValue = $"×{GlobalCooldownMultiplier:0.##}";
                    appliedValue = $"×{GlobalCooldownMultiplier * 0.9f:0.##}";
                    break;
                case CommonUpgradeKind.Pierce:
                    currentValue = $"관통 {BonusPierce} / 연쇄 {BonusChainTargets}";
                    appliedValue = $"관통 {BonusPierce + 1} / 연쇄 {BonusChainTargets + 1}";
                    break;
            }
        }
        else if (definition.Type == SkillTreeNodeType.ElementMastery)
        {
            currentValue = "피해·범위 ×1";
            appliedValue = "피해·범위 ×1.2";
        }
        else if (definition.Type == SkillTreeNodeType.FusionMastery)
        {
            currentValue = "융합 피해 ×1";
            appliedValue = "융합 피해 ×1.2 + 고유 효과";
        }

        return new SkillTreeNodePreview(
            definition,
            GetNodeState(nodeId),
            tree.PendingSelection.HasValue && tree.PendingSelection.Value == nodeId,
            currentValue,
            appliedValue);
    }

    public IReadOnlyList<MagicElement> GetOwnedElements()
    {
        return tree.OwnedElements;
    }

    public IReadOnlyList<FusionKind> GetOwnedFusions()
    {
        return tree.OwnedFusions;
    }

    public IReadOnlyList<MagicId> GetOwnedMagics()
    {
        return tree.OwnedMagics;
    }

    public bool ApplyChoice(int index)
    {
        if (index < 0 || index >= LegacyCommonChoices.Length)
        {
            return false;
        }

        SkillTreeNodeId node = LegacyCommonChoices[index];
        if (!TrySelectNode(node) || !ConfirmSelectedNode())
        {
            return false;
        }

        SkillUpgradeKind kind = (SkillUpgradeKind)index;
        SkillChoice choice = new SkillChoice(
            kind,
            SkillTreeCatalog.GetNode(node).DisplayName,
            string.Empty);
        ChoiceApplied?.Invoke(choice);
        return true;
    }

    private void IndexDefinitions()
    {
        definitions.Clear();

        if (startingMagic != null)
        {
            definitions[startingMagic.MagicId] = startingMagic;
        }

        if (targetedMagicDefinitions == null)
        {
            return;
        }

        for (int index = 0; index < targetedMagicDefinitions.Length; index++)
        {
            ProjectileMagicDefinition definition = targetedMagicDefinitions[index];
            if (definition != null)
            {
                definitions[definition.MagicId] = definition;
            }
        }
    }

    private void HandleMagicUnlocked(MagicId magicId)
    {
        if (definitions.TryGetValue(magicId, out ProjectileMagicDefinition definition) &&
            !activeMagicLookup.ContainsKey(magicId))
        {
            MagicRuntime runtime = new MagicRuntime(definition);
            activeMagicLookup.Add(magicId, runtime);
            activeMagics.Add(runtime);
            CurrentMagic = CurrentMagic ?? runtime;
            RefreshRuntimeModifiers();
            weaponRunner.Register(runtime);
        }

        MagicUnlocked?.Invoke(magicId);
    }

    private void HandleNodeOwned(SkillTreeNodeId nodeId)
    {
        SkillTreeNodeDefinition definition = SkillTreeCatalog.GetNode(nodeId);
        if (definition.CommonUpgrade.HasValue)
        {
            switch (definition.CommonUpgrade.Value)
            {
                case CommonUpgradeKind.Power:
                    GlobalDamageMultiplier *= 1.15f;
                    break;
                case CommonUpgradeKind.RapidFire:
                    GlobalCooldownMultiplier *= 0.9f;
                    break;
                case CommonUpgradeKind.Pierce:
                    BonusPierce += 1;
                    BonusChainTargets += 1;
                    break;
            }
        }

        RefreshRuntimeModifiers();
    }

    private void RefreshRuntimeModifiers()
    {
        for (int index = 0; index < activeMagics.Count; index++)
        {
            MagicRuntime runtime = activeMagics[index];
            GetMasteryModifiers(runtime, out float masteryDamage, out float masteryRange);
            runtime.SetTreeModifiers(
                GlobalDamageMultiplier,
                GlobalCooldownMultiplier,
                BonusPierce,
                masteryDamage,
                masteryRange);
        }
    }

    private bool CanSpendSkillPoint()
    {
        return gameFlowController != null &&
               levelUpController != null &&
               gameFlowController.State == GameFlowState.LevelUp &&
               levelUpController.CanSpendSkillPoint;
    }

    private void GetMasteryModifiers(
        MagicRuntime runtime,
        out float damageMultiplier,
        out float rangeMultiplier)
    {
        MagicDefinition magic = MagicContentCatalog.GetMagic(runtime.Id);
        if (!magic.IsFusion)
        {
            bool mastered = tree.HasNode(SkillTreeCatalog.GetMasteryNode(magic.PrimaryElement));
            damageMultiplier = mastered
                ? MagicContentCatalog.BaseMasteryDamageMultiplier
                : 1f;
            rangeMultiplier = mastered
                ? MagicContentCatalog.BaseMasteryRangeMultiplier
                : 1f;
            return;
        }

        bool fusionMastered = false;
        for (int index = 0; index < SkillTreeCatalog.Fusions.Count; index++)
        {
            FusionDefinition fusion = SkillTreeCatalog.Fusions[index];
            if (fusion.Magic == runtime.Id)
            {
                fusionMastered = tree.HasNode(fusion.MasteryNode);
                break;
            }
        }

        damageMultiplier = fusionMastered
            ? MagicContentCatalog.FusionMasteryDamageMultiplier
            : 1f;
        rangeMultiplier = 1f;
    }
}
