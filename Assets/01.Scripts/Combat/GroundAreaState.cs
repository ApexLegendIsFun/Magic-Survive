using System.Collections.Generic;
using UnityEngine;

// 지속 장판 목록. 대지 5레벨 지진 구역과 냉기 8레벨 서리 장판이 공유
//
// MonoBehaviour 가 아니다. EnemyManager 가 인스턴스 필드로 소유
// ProjectileLauncher 가 ElementHitCounter 를 들고 있는 것과 같은 방식이고
// 씬 배선이 증가 x
public class GroundAreaState
{
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
    }

    private readonly List<Area> areas = new List<Area>(16);

    public int ActiveCount => areas.Count;

    /// <summary>
    /// 장판을 추가한다. slowPercent 0.3 이면 이동속도 30% 감소.
    /// 입력이 잘못되면 false 를 돌려주고 아무것도 만들지 않는다
    /// </summary>
    public bool Add(
        MagicElement element, Vector2 center, float radius,
        float durationSeconds, float slowPercent)
    {
        if (radius <= 0f || durationSeconds <= 0f)
        {
            return false;
        }

        areas.Add(new Area
        {
            Element = element,
            Center = center,
            Radius = radius,
            RemainingSeconds = durationSeconds,

            // 저장 시점에 곱셈 형태로 바꿔 두기. 조회가 매 프레임 적 수만큼 돌기 때문
            SlowMultiplier = Mathf.Clamp01(1f - slowPercent)
        });

        return true;
    }

    // EnemyManager.Update 가 적 순회 전에 한 번 부름
    public void Tick(float deltaTime)
    {
        // 역순 순회 + swap-back. 적과 투사체 목록과 같은 방식
        for (int i = areas.Count - 1; i >= 0; i--)
        {
            Area area = areas[i];

            area.RemainingSeconds -= deltaTime;

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