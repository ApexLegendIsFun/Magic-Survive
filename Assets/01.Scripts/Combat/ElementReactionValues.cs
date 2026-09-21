

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

    // 5원소 공통. 공통 레벨 상승표의 "6 범위,관통,대상 수 강화"
    // 관통 +1 은 MagicContentCatalog.GetStats 가 처리. 이 상수는 범위·대상 수에만 사용
    public const int ReinforceUnlockLevel = 6;

    // 5원소 공통. 공통 레벨 상승표의 7 원소 고유 효과 강화
    // Mastery 는 ElementMarkRules.ShouldTriggerMastery(3중첩)가 이미 쓰는 이름이라 피함
    public const int ReactionBoostUnlockLevel = 7;

    // 5원소 공통. 공통 레벨 상승표의 8 원소 각성 효과 해금
    public const int AwakeningUnlockLevel = 8;

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

    // 냉기 표식 둔화. 중첩당 이동속도 감소
    // 기획에 기본 수치가 없어 기존 Enemy 값을 그대로 옮김. 확인 요청 대상
    // Enemy 에서 옮긴 이유: 6레벨 강화 계산이 기본값을 알아야 함
    public const float FrostMovementSpeedReductionPerStack = 0.10f;

    // 대지 1레벨 밀치기. 임시값. 확인 요청 대상
    // hitRadius 0.5와 같은 값이고 점화 1.3, 충격파 1.5 대역보다 작음
    public const float KnockbackDistance = 0.5f;


    // 6, 7레벨 강화
    //
    // 위 기본 상수는 그대로 두고 배율·가산값만 따로 둠
    // 기획 문구가 "+25%", "+0.3초" 처럼 기본값 기준이라 그 형태를 유지
    // IntegrationGameEditor 가 KnockbackDistance == 0.5 를 검사하므로 기본값을 바꾸지 않음

    // 화염 7레벨. 점화 폭발 피해 +25%, 반경 +20%
    // 전염 반경(FireSpreadRadius)은 따라가지 않으며 점화 폭발만.
    public const float IgniteBoostDamageMultiplier = 1.25f;
    public const float IgniteBoostRadiusMultiplier = 1.2f;

    // 번개 6레벨. 연쇄 대상 +1
    // 보스 1대상 제한은 Projectile.FindChainBoss 가 그대로 처리
    public const int ChainReinforceExtraTargets = 1;

    // 번개 7레벨. 방전 피해 +25%
    public const float DischargeBoostDamageMultiplier = 1.25f;

    // 냉기 7레벨. 빙결 지속시간 +0.3초
    // 보스는 Enemy.ApplyFreeze 에서 CrowdControlDurationMultiplier 0 이 곱해져 그대로 면역
    public const float FreezeBoostExtraSeconds = 0.3f;

    // 대지 6레벨. 충격파 반경 +15%
    // 지진 구역(EarthquakeRadius)은 따라가지 않으며 충격파만. 확인 요청 대상
    public const float ShockwaveReinforceRadiusMultiplier = 1.15f;

    // 대지 7레벨. 밀치기 거리 +30%
    // 보스는 Enemy.IsKnockbackImmune 으로 그대로 면역
    public const float KnockbackBoostDistanceMultiplier = 1.3f;

    // 냉기 6레벨. 냉기 표식 둔화 효과 +10%
    // 다른 +% 강화와 같이 기본값에 곱하는 배율로 해석. 확인 요청 대상
    // 중첩당 0.10 -> 0.11. 보스는 Enemy.Tick 에서 표식 둔화 면역
    public const float FrostSlowReinforceMultiplier = 1.1f;

    // 암흑 7레벨. 암흑 피해 증가 효과 +10%
    // 같은 배율 해석. 0.15 → 0.165. 보스에게도 적용
    public const float DarkAmplificationBoostMultiplier = 1.1f;


    // 8레벨 각성
    // 암흑 8레벨 처형. 남은 체력이 최대 체력의 이 비율 이하면 즉시 처형
    // 일반 적만. 보스는 Enemy.TryDarkExecute 에서 isBoss 로 제외
    public const float DarkExecuteHealthRatio = 0.10f;

    // 냉기 8레벨 서리 장판. 빙결이 풀린 자리에 남음
    // 반경은 기획에 없어 지진 구역과 같은 값으로 시작. 확인 요청 대상
    // 특화 "서리 장판 반경 +10%" 의 기준값.
    // 보스 면역은 GroundAreaState 가 원소로 이미 거름
    public const float FrostGroundRadius = 1.5f;
    public const float FrostGroundDurationSeconds = 2.5f;
    public const float FrostGroundSlowPercent = 0.35f;

    // 화염 8레벨 불장판. 점화 폭발이 터진 자리에 남음
    // 점화 폭발의 반경을 그대로 사용
    // 기획에 장판 반경이 없어 폭발 원과 맞춤. 확인 요청 대상
    // 둔화 x. 피해만 주는 첫 장판이고 2초 동안 0.5초마다 4 씩 4회
    public const float FireGroundDurationSeconds = 2f;
    public const float FireGroundDamagePerTick = 4f;
    public const float FireGroundDamageIntervalSeconds = 0.5f;

    // 8레벨 각성의 발동 주기. 3레벨 충격파·연쇄의 HitsPerTrigger 와 별개로 셈
    // 같은 카운터를 쓰면 한쪽 발동이 다른 쪽 카운트를 0 으로 되돌림
    // 기획 115행은 "3번째 적중마다", 119행은 "5번째 대지 공격마다" 로 단어가 다르기에
    // 이쪽은 적중이 아니라 발사를 셈. 확인 요청 대상
    public const int AwakeningHitsPerTrigger = 5;

    public const int RockfallCount = 3;
    public const float RockfallDamage = 14f;
    public const float RockfallRadius = 1.2f;
    public const float RockfallSpawnDistance = 1.5f;
    public const float RockfallFirstAngleDegrees = 90f;

    // 번개 8레벨 낙뢰.
    // 기획에 사거리가 없어 살아 있는 적 전체가 후보이고, 그중 플레이어에게 가까운 순으로 고름
    // 보스도 대상.
    public const float LightningStormIntervalSeconds = 6f;
    public const float LightningStormDamage = 12f;
    public const int LightningStormMaxTargets = 3;


    // 레벨별 값 계산
    // 호출부는 스킬 레벨만 넘김. 게이트와 기본 강화 수치는 이 클래스가 관리
    // 카탈로그 api 연결 시 이 계산 경로를 교체

    public static float GetIgniteDamage(int skillLevel)
    {
        if (skillLevel >= ReactionBoostUnlockLevel)
        {
            return IgniteDamage * IgniteBoostDamageMultiplier;
        }

        return IgniteDamage;
    }

    public static float GetIgniteRadius(int skillLevel)
    {
        if (skillLevel >= ReactionBoostUnlockLevel)
        {
            return IgniteRadius * IgniteBoostRadiusMultiplier;
        }

        return IgniteRadius;
    }

    public static int GetChainMaxTargets(int skillLevel)
    {
        if (skillLevel >= ReinforceUnlockLevel)
        {
            return ChainMaxTargets + ChainReinforceExtraTargets;
        }

        return ChainMaxTargets;
    }

    public static float GetDischargeDamage(int skillLevel)
    {
        if (skillLevel >= ReactionBoostUnlockLevel)
        {
            return DischargeDamage * DischargeBoostDamageMultiplier;
        }

        return DischargeDamage;
    }

    public static float GetFreezeDurationSeconds(int skillLevel)
    {
        if (skillLevel >= ReactionBoostUnlockLevel)
        {
            return FreezeDurationSeconds + FreezeBoostExtraSeconds;
        }

        return FreezeDurationSeconds;
    }

    public static float GetShockwaveRadius(int skillLevel)
    {
        if (skillLevel >= ReinforceUnlockLevel)
        {
            return ShockwaveRadius * ShockwaveReinforceRadiusMultiplier;
        }

        return ShockwaveRadius;
    }

    public static float GetKnockbackDistance(int skillLevel)
    {
        if (skillLevel >= ReactionBoostUnlockLevel)
        {
            return KnockbackDistance * KnockbackBoostDistanceMultiplier;
        }

        return KnockbackDistance;
    }

    public static float GetFrostSlowPerStack(int skillLevel)
    {
        if (skillLevel >= ReinforceUnlockLevel)
        {
            return FrostMovementSpeedReductionPerStack * FrostSlowReinforceMultiplier;
        }

        return FrostMovementSpeedReductionPerStack;
    }

    public static float GetDarkAmplificationBonus(int skillLevel)
    {
        if (skillLevel >= ReactionBoostUnlockLevel)
        {
            return DarkAmplificationBonus * DarkAmplificationBoostMultiplier;
        }

        return DarkAmplificationBonus;
    }

}

