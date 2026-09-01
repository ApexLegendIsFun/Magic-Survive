using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSkillSystem : MonoBehaviour
{
    private static readonly SkillChoice[] LevelUpChoices =
    {
        new SkillChoice(
            SkillUpgradeKind.Damage,
            "POWER UP",
            "Projectile damage +2"),
        new SkillChoice(
            SkillUpgradeKind.FireRate,
            "RAPID FIRE",
            "Attack cooldown x0.9"),
        new SkillChoice(
            SkillUpgradeKind.Pierce,
            "PIERCING",
            "Projectile pierce +1")
    };

    [SerializeField] private WeaponRunner weaponRunner;
    [SerializeField] private ProjectileMagicDefinition startingMagic;

    public MagicRuntime CurrentMagic { get; private set; }

    public event Action<SkillChoice> ChoiceApplied;

    private void Awake()
    {
        if (weaponRunner == null)
        {
            Debug.LogError("PlayerSkillSystem에 WeaponRunner가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        if (startingMagic == null)
        {
            Debug.LogError("PlayerSkillSystem에 시작 마법 Definition이 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        CurrentMagic = new MagicRuntime(startingMagic);
        weaponRunner.Register(CurrentMagic);
    }

    private void OnDestroy()
    {
        if (weaponRunner != null && CurrentMagic != null)
        {
            weaponRunner.Unregister(CurrentMagic);
        }
    }

    public IReadOnlyList<SkillChoice> GetLevelUpChoices()
    {
        return LevelUpChoices;
    }

    public bool ApplyChoice(int index)
    {
        if (CurrentMagic == null || index < 0 || index >= LevelUpChoices.Length)
        {
            return false;
        }

        SkillChoice choice = LevelUpChoices[index];
        CurrentMagic.ApplyUpgrade(choice.Kind);
        ChoiceApplied?.Invoke(choice);
        return true;
    }
}
