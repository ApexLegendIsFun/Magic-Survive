using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum RunOutcome
{
    None = 0,
    Victory = 1,
    Defeat = 2,
    Timeout = 3
}

public sealed class RunResult
{
    private readonly ReadOnlyCollection<MagicElement> elements;
    private readonly ReadOnlyCollection<FusionKind> fusions;

    public RunResult(
        RunOutcome outcome,
        float combatTime,
        int killCount,
        int level,
        IEnumerable<MagicElement> ownedElements,
        IEnumerable<FusionKind> ownedFusions)
    {
        Outcome = outcome;
        CombatTime = Math.Max(0f, combatTime);
        KillCount = Math.Max(0, killCount);
        Level = Math.Max(1, level);
        elements = Array.AsReadOnly(Copy(ownedElements));
        fusions = Array.AsReadOnly(Copy(ownedFusions));
    }

    public RunOutcome Outcome { get; }
    public bool IsVictory => Outcome == RunOutcome.Victory;
    public float CombatTime { get; }
    public int KillCount { get; }
    public int Level { get; }
    public IReadOnlyList<MagicElement> Elements => elements;
    public IReadOnlyList<FusionKind> Fusions => fusions;

    private static T[] Copy<T>(IEnumerable<T> source)
    {
        if (source == null)
        {
            return Array.Empty<T>();
        }

        List<T> copy = new List<T>();
        foreach (T item in source)
        {
            copy.Add(item);
        }

        return copy.ToArray();
    }
}
