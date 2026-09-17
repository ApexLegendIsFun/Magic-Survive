

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
    public const int ReactionUnlockLevel = 1; //TODO: 빌드 전 수정 필수. 테스트 용이하기 위함, 일시적인 조정입니다.

    // 화염 3레벨 점화
    public const float IgniteDamage = 12f;
    public const float IgniteRadius = 1.3f;

    // 냉기 3레벨 빙결
    public const float FreezeDurationSeconds = 0.6f;

    // 적중 카운터 기반 효과의 발동 주기. 번개 대지 모두 3번째 적중마다
    public const int HitsPerTrigger = 3;

    // 번개 3레벨 연쇄
    // ChainRadius는 기획 수치가 없어 정한 임시값. 확인 요청 대상
    public const float ChainDamageRatio = 0.6f;
    public const float ChainRadius = 1.5f;
    public const int ChainMaxTargets = 2;

    // 대지 3레벨 충격파
    public const float ShockwaveDamage = 10f;
    public const float ShockwaveRadius = 1.5f;

    // 암흑 3레벨. 3중첩 대상이 받는 모든 피해 증가
    // 1회성이 아니라 대상에 남는 지속
    public const float DarkAmplificationBonus = 0.15f;

    // 대지 1레벨 밀치기. 임시값. 확인 요청 대상
    // hitRadius 0.5와 같은 값이고 점화 1.3, 충격파 1.5 대역보다 작음
    public const float KnockbackDistance = 0.5f;

}

