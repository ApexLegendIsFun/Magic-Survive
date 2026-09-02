public static class RunTimelineRules
{
    public const float FirstEliteTime = 180f;
    public const float SecondEliteTime = 360f;
    public const float BossTime = 480f;
    public const float TimeLimit = 600f;

    public static bool Reached(float elapsedTime, float threshold)
    {
        return elapsedTime >= threshold;
    }
}
