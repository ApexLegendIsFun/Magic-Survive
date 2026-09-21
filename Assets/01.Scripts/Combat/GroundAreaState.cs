using System.Collections.Generic;
using UnityEngine;

// 지속 장판 목록. 대지 5레벨 지진 구역과 냉기 8레벨 서리 장판이 공유
//
// MonoBehaviour 가 아니다. EnemyManager 가 인스턴스 필드로 소유
// ProjectileLauncher 가 ElementHitCounter 를 들고 있는 것과 같은 방식이고
// 씬 배선이 증가 x
public class GroundAreaState
{
    // 이번 프레임에 터질 장판 피해. EnemyManager 가 받아서 적에게 적용한다
    //
    // 여기서 적을 직접 때리지 않는 이유: GroundAreaState 는 적 목록을 모른다
    // 둔화는 적이 자기 위치로 조회하지만 피해는 장판이 대상을 찾아야 해서 방향이 반대다
    public struct DamagePulse
    {
        public MagicElement Element;
        public Vector2 Center;
        public float Radius;
        public float Damage;
    }

    private struct Area
    {
        // 보스 면역이 원소마다 다름
        // 기획 냉기 항목에만 "보스에게는 둔화 효과도 적용하지 않는다" 가 있고
        // 대지 항목은 "대지 피해는 정상 적용한다"
        public MagicElement Element;

        public Vector2 Center;
        public float Radius;
        public float RemainingSeconds;

        // 이동속도에 곱할 값. 0.7 이면 30% 감소. 작을수록 강함
        public float SlowMultiplier;

        // 화염 8레벨 불장판. 0 이면 피해가 없는 장판(지진, 서리)
        public float DamagePerTick;
        public float DamageIntervalSeconds;
        public float DamageTimer;
    }
    

    private readonly List<Area> areas = new List<Area>(16);

    public int ActiveCount => areas.Count;

    /// <summary>
    /// 피해가 없는 장판. 지진 구역과 서리 장판이 쓰는 기존 경로
    /// </summary>
    public bool Add(
        MagicElement element, Vector2 center, float radius,
        float durationSeconds, float slowPercent)
    {
        return Add(element, center, radius, durationSeconds, slowPercent, 0f, 0f);
    }

    /// <summary>
    /// 장판을 추가. slowPercent 0.3 이면 이동속도 30% 감소.
    /// </summary>
    public bool Add(
        MagicElement element, Vector2 center, float radius,
        float durationSeconds, float slowPercent,
        float damagePerTick, float damageIntervalSeconds)
    {
        if (radius <= 0f || durationSeconds <= 0f)
        {
            return false;
        }

        // 둘 중 하나라도 비면 피해 없는 장판.
        bool hasDamage = damagePerTick > 0f && damageIntervalSeconds > 0f;

        areas.Add(new Area
        {
            Element = element,
            Center = center,
            Radius = radius,
            RemainingSeconds = durationSeconds,

            // 저장 시점에 곱셈 형태로 바꿔 두기.
            SlowMultiplier = Mathf.Clamp01(1f - slowPercent),

            DamagePerTick = hasDamage ? damagePerTick : 0f,
            DamageIntervalSeconds = hasDamage ? damageIntervalSeconds : 0f,

            // 첫 피해는 한 주기 뒤. 폭발 피해가 방금 들어갔으므로 생성 즉시 때리기 x
            DamageTimer = hasDamage ? damageIntervalSeconds : 0f
        });

        return true;
    }

    // EnemyManager.Update 가 적 순회 전에 한 번 부름
    //
    // pulses 는 호출부가 들고 있는 버퍼. 이번 프레임에 터질 장판 피해가 담김
    public void Tick(float deltaTime, List<DamagePulse> pulses)
    {
        if (pulses != null)
        {
            pulses.Clear();
        }

        // 역순 순회 + swap-back. 적과 투사체 목록과 같은 방식
        for (int i = areas.Count - 1; i >= 0; i--)
        {
            Area area = areas[i];

            // 피해 타이머에는 장판이 실제로 살아 있던 시간만 넣는다.
            // 프레임이 남은 시간보다 길면(에디터 멈춤, 로딩 직후) 만료 뒤 구간까지 때리게 된다
            float activeDeltaTime = Mathf.Min(deltaTime, area.RemainingSeconds);

            area.RemainingSeconds -= deltaTime;

            // 피해 주기는 만료 검사보다 먼저.
            // 뒤에 두면 지속시간이 주기의 배수일 때 마지막 한 번이 사라진다 (2초 장판의 2.0초 지점)
            //
            // while 인 이유: 프레임이 주기보다 길면 놓친 만큼 한 번에 처리한다
            // DamageIntervalSeconds 는 Add 에서 0 보다 크다고 보장되므로 무한 루프가 없다
            if (area.DamagePerTick > 0f && pulses != null)
            {
                area.DamageTimer -= activeDeltaTime;

                while (area.DamageTimer <= 0f)
                {
                    area.DamageTimer += area.DamageIntervalSeconds;

                    pulses.Add(new DamagePulse
                    {
                        Element = area.Element,
                        Center = area.Center,
                        Radius = area.Radius,
                        Damage = area.DamagePerTick
                    });
                }
            }

            if (area.RemainingSeconds <= 0f)
            {
                int lastIndex = areas.Count - 1;

                areas[i] = areas[lastIndex];
                areas.RemoveAt(lastIndex);

                continue;
            }

            areas[i] = area;
        }
    }

    /// <summary>
    /// 이 위치에 걸리는 이동속도 배율. 장판이 없으면 1.
    ///
    /// 장판이 여러 개 겹치면 가장 강한 하나만 쓴다
    /// 전부 곱하면 2개 0.49, 3개 0.343 이 되는데 기획에 중첩 규정이 없다
    /// Lv6 관통이 들어오면 충격파 주기가 장판 지속보다 짧아져 실제로 겹친다
    /// </summary>
    public float GetSlowMultiplier(Vector2 position, bool isBoss)
    {
        float strongest = 1f;

        for (int i = 0; i < areas.Count; i++)
        {
            Area area = areas[i];

            // 기획 냉기 항목의 "보스에게는 둔화 효과도 적용하지 않는다"
            // 대지 지진은 여기 해당하지 않음. 보스에게도 걸림
            if (isBoss && area.Element == MagicElement.Frost)
            {
                continue;
            }

            // 적의 피격 반경은 더하지 않음
            // 장판은 발밑 판정이고 화면에 그려지는 원과 맞아야 함
            if ((position - area.Center).sqrMagnitude >= area.Radius * area.Radius)
            {
                continue;
            }

            if (area.SlowMultiplier < strongest)
            {
                strongest = area.SlowMultiplier;
            }
        }

        return strongest;
    }

    // 재시작과 보스 전환에서 EnemyManager.DespawnAll 이 부름
    public void Clear()
    {
        areas.Clear();
    }
}