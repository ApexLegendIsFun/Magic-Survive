

// 개편 기획의 원소 3레벨 반응 수치 임시 미러링
//
// 원본은 Docs/ElementSkillEliteBossPlan.md §3
// MagicContentCatalog.GetStats는 damage/cooldown/pierce 3개만 주고,
// 3/5/7/8레벨 고유 효과 수치는 Combat이 읽을 경로가 없음
// 카탈로그 API를 받으면 이 클래스만 교체. 호출부는 안 바뀜
//


public static class ElementReactionValues
{
    // 5원소 모두 3레벨에서 첫 고유 효과가 해금
    public const int ReactionUnlockLevel = 3;

    // 화염 3레벨 점화
    public const float IgniteDamage = 12f;
    public const float IgniteRadius = 1.3f;

    // 냉기 3레벨 빙결
    public const float FreezeDurationSeconds = 0.6f;

    // 암흑 3레벨. 3중첩 대상이 받는 모든 피해 증가
    // 1회성이 아니라 대상에 남는 지속 상태
    public const float DarkAmplificationBonus = 0.15f;
}