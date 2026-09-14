using System;
using System.Collections.Generic;

public enum SpecializationId
{
    FireDamage = 0, FireExplosionRadius = 1, FireGroundDuration = 2,
    LightningDamage = 3, LightningChainRange = 4, LightningDischargeDamage = 5,
    FrostDamage = 6, FrostFreezeDuration = 7, FrostGroundRadius = 8,
    EarthDamage = 9, EarthShockwaveRadius = 10, EarthKnockbackDistance = 11,
    DarkDamage = 12, DarkSpreadRange = 13, DarkThreeStackAmplification = 14
}

// Amounts use display units: 10 means +10%, 2 means +2 percentage points.
public enum SpecializationUnit { Percent, PercentagePoints }
public enum SpecializationCombatStatus { NotConnected }

public sealed class SpecializationDefinition
{
    internal SpecializationDefinition(SpecializationId id, MagicElement element, string name,
        string targetEffect, float amount = 10f, SpecializationUnit unit = SpecializationUnit.Percent)
    { Id = id; Element = element; Name = name; TargetEffect = targetEffect; Amount = amount; Unit = unit; }
    public SpecializationId Id { get; }
    public MagicElement Element { get; }
    public string Name { get; }
    public string TargetEffect { get; }
    public float Amount { get; }
    public SpecializationUnit Unit { get; }
    public SpecializationCombatStatus CombatStatus => SpecializationCombatStatus.NotConnected;
    public string Description => $"{TargetEffect} +{Amount:0.##}{UnitText}";
    public string UnitText => Unit == SpecializationUnit.Percent ? "%" : "%p";
}

public static class SpecializationCatalog
{
    public static IReadOnlyList<SpecializationDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        new SpecializationDefinition(SpecializationId.FireDamage, MagicElement.Fire, "집중 화염", "화염탄 피해"),
        new SpecializationDefinition(SpecializationId.FireExplosionRadius, MagicElement.Fire, "넓은 점화", "점화 폭발 반경"),
        new SpecializationDefinition(SpecializationId.FireGroundDuration, MagicElement.Fire, "오래 타는 불", "불장판 지속시간"),
        new SpecializationDefinition(SpecializationId.LightningDamage, MagicElement.Lightning, "고압 전격", "번개탄 피해"),
        new SpecializationDefinition(SpecializationId.LightningChainRange, MagicElement.Lightning, "멀리 뻗는 전류", "연쇄 탐색 거리"),
        new SpecializationDefinition(SpecializationId.LightningDischargeDamage, MagicElement.Lightning, "강한 방전", "방전 피해"),
        new SpecializationDefinition(SpecializationId.FrostDamage, MagicElement.Frost, "날카로운 얼음", "얼음창 피해"),
        new SpecializationDefinition(SpecializationId.FrostFreezeDuration, MagicElement.Frost, "깊은 빙결", "빙결 지속시간"),
        new SpecializationDefinition(SpecializationId.FrostGroundRadius, MagicElement.Frost, "퍼지는 서리", "서리 장판 반경"),
        new SpecializationDefinition(SpecializationId.EarthDamage, MagicElement.Earth, "무거운 바위", "바위창 피해"),
        new SpecializationDefinition(SpecializationId.EarthShockwaveRadius, MagicElement.Earth, "넓은 충격", "충격파 반경"),
        new SpecializationDefinition(SpecializationId.EarthKnockbackDistance, MagicElement.Earth, "거센 밀침", "밀치기 거리"),
        new SpecializationDefinition(SpecializationId.DarkDamage, MagicElement.Dark, "응축된 암흑", "그림자 구체 피해"),
        new SpecializationDefinition(SpecializationId.DarkSpreadRange, MagicElement.Dark, "퍼지는 저주", "표식 전염 탐색 거리"),
        new SpecializationDefinition(SpecializationId.DarkThreeStackAmplification, MagicElement.Dark, "깊은 저주", "3중첩 피해 증가 효과", 2f, SpecializationUnit.PercentagePoints)
    });

    public static bool TryGet(SpecializationId id, out SpecializationDefinition definition)
    {
        foreach (var candidate in All)
            if (candidate.Id == id) { definition = candidate; return true; }
        definition = null;
        return false;
    }
}
