

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

    // 5원소 공통. 기획 공통 레벨 상승표의 "5 공격 형태 확장"
    // ReactionUnlockLevel 과 독립. 그쪽 값이 무엇이든 5레벨 효과는 이 상수로만 열림
    public const int ExpansionUnlockLevel = 5;

    // 화염 3레벨 점화
    public const float IgniteDamage = 12f;
    public const float IgniteRadius = 1.3f;

    // 화염 5레벨 전염. 점화 전염과 사망 전염이 함께 사용
    // 반경을 IgniteRadius 와 공유하지 않음
    // 점화 반경과 같은 값으로 시작. 확인 요청 대상
    public const float FireSpreadRadius = 1.3f;
    public const int FireSpreadMaxTargets = 2;

    // 암흑 5레벨 사망 전염
    // DarkSpreadRadius 는 임시값. 기획에 반경이 없어 화염 전염과 같은 값으로 둠
    // 화염과 따로 둔다. 암흑 특화 "전염 탐색 거리 +10%" 가 이 값만 키움
    public const float DarkSpreadRadius = 1.3f;
    public const int DarkSpreadMaxTargets = 3;

    // 냉기 3레벨 빙결
    public const float FreezeDurationSeconds = 0.6f;

    // 냉기 5레벨 파괴
    public const float ShatterDamage = 8f;
    public const float ShatterRadius = 1.2f;

    // 적중 카운터 기반 효과의 발동 주기. 번개 대지 모두 3번째 적중마다
    public const int HitsPerTrigger = 3;

    // 번개 3레벨 연쇄
    // ChainRadius는 기획 수치가 없어 정한 임시값. 확인 요청 대상
    public const float ChainDamageRatio = 0.6f;
    public const float ChainRadius = 1.5f;
    public const int ChainMaxTargets = 2;

    // 번개 5레벨 방전. 3중첩 기준이라 화염 점화와 같은 판정.
    public const float DischargeDamage = 10f;
    public const float DischargeRadius = 1.2f;

    // 대지 3레벨 충격파
    public const float ShockwaveDamage = 10f;
    public const float ShockwaveRadius = 1.5f;

    // 대지 5레벨 지진 구역
    // 반경을 ShockwaveRadius 와 공유하지 않음
    // Lv6 "충격파 반경 +15%" 가 들어오면 공유한 경우 지진까지 같이 커짐
    // 지금은 같은 값이지만 별개 상수. 기획에 지진 반경이 없어 정함
    public const float EarthquakeRadius = 1.5f;
    public const float EarthquakeDurationSeconds = 2f;
    public const float EarthquakeSlowPercent = 0.30f;

    // 암흑 3레벨. 3중첩 대상이 받는 모든 피해 증가
    // 1회성이 아니라 대상에 남는 지속
    public const float DarkAmplificationBonus = 0.15f;

    // 대지 1레벨 밀치기. 임시값. 확인 요청 대상
    // hitRadius 0.5와 같은 값이고 점화 1.3, 충격파 1.5 대역보다 작음
    public const float KnockbackDistance = 0.5f;

}

