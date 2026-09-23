using System;

// 특화 누적 보너스를 Combat 이 바로 쓸 수 있는 형태로 묶은 스냅샷
//
// SpecializationUIContract.md 의 단위 규칙을 여기서 한 번만 해석
//   Percent           기준값 x (1 + bonus / 100)
//   PercentagePoints  기준 비율 + bonus / 100
// Combat 은 곱하거나 더하기만 하고 단위를 다시 해석 x
//
// 발사 시점 값. 이미 날아가는 탄은 이후 카드 선택에 영향x
public readonly struct ElementSpecializationBonus
{
    // 카드를 하나도 고르지 않은 상태. 배율 1, 가산 0
    public static readonly ElementSpecializationBonus None =
        new ElementSpecializationBonus(1f, 1f, 1f, 0f);

    public ElementSpecializationBonus(
        float damageMultiplier, float rangeMultiplier, float powerMultiplier, float powerBonus)
    {
        DamageMultiplier = damageMultiplier;
        RangeMultiplier = rangeMultiplier;
        PowerMultiplier = powerMultiplier;
        PowerBonus = powerBonus;
    }

    // 기본 투사체 피해. 5원소 공통
    public float DamageMultiplier { get; }

    // 범위,거리 계열
    // 화염 점화 반경 / 번개 연쇄 탐색 거리 / 냉기 서리 장판 반경 / 대지 충격파 반경 / 암흑 전염 탐색 거리
    public float RangeMultiplier { get; }

    // 효과 강도 계열 (배율)
    // 화염 불장판 지속 / 번개 방전 피해 / 냉기 빙결 지속 / 대지 밀치기 거리. 암흑은 1
    public float PowerMultiplier { get; }

    // 효과 강도 계열 (가산). 암흑 3중첩 증폭만 사용하고 나머지 원소는 0
    public float PowerBonus { get; }

    // 원소별 카드 3장의 누적값을 읽어 스냅샷을 생성.
    // getAccumulatedBonus 는 PlayerSkillSystem.GetSpecializationBonus 를 넘기면 됩니다
    public static ElementSpecializationBonus Build(
        MagicElement element, Func<SpecializationId, float> getAccumulatedBonus)
    {
        if (getAccumulatedBonus == null)
        {
            return None;
        }

        switch (element)
        {
            case MagicElement.Fire:
                return FromPercent(
                    getAccumulatedBonus(SpecializationId.FireDamage),
                    getAccumulatedBonus(SpecializationId.FireExplosionRadius),
                    getAccumulatedBonus(SpecializationId.FireGroundDuration));

            case MagicElement.Lightning:
                return FromPercent(
                    getAccumulatedBonus(SpecializationId.LightningDamage),
                    getAccumulatedBonus(SpecializationId.LightningChainRange),
                    getAccumulatedBonus(SpecializationId.LightningDischargeDamage));

            case MagicElement.Frost:
                return FromPercent(
                    getAccumulatedBonus(SpecializationId.FrostDamage),
                    getAccumulatedBonus(SpecializationId.FrostGroundRadius),
                    getAccumulatedBonus(SpecializationId.FrostFreezeDuration));

            case MagicElement.Earth:
                return FromPercent(
                    getAccumulatedBonus(SpecializationId.EarthDamage),
                    getAccumulatedBonus(SpecializationId.EarthShockwaveRadius),
                    getAccumulatedBonus(SpecializationId.EarthKnockbackDistance));

            case MagicElement.Dark:
                // 암흑의 세 번째 카드만 퍼센트포인트 가산.
                return new ElementSpecializationBonus(
                    ToMultiplier(getAccumulatedBonus(SpecializationId.DarkDamage)),
                    ToMultiplier(getAccumulatedBonus(SpecializationId.DarkSpreadRange)),
                    1f,
                    getAccumulatedBonus(SpecializationId.DarkThreeStackAmplification) * 0.01f);

            default:
                return None;
        }
    }

    private static ElementSpecializationBonus FromPercent(float damage, float range, float power)
    {
        return new ElementSpecializationBonus(
            ToMultiplier(damage), ToMultiplier(range), ToMultiplier(power), 0f);
    }

    // 표시 단위(20)를 배율(1.20)로. 음수 방어만.
    private static float ToMultiplier(float percent)
    {
        return percent <= -100f ? 0f : 1f + percent * 0.01f;
    }
}