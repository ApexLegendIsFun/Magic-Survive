using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 소환 예고(SummonTelegraph) 연출.
/// "실제 소환 완료" 이벤트가 따로 없음 — 예고가 끝나면 정확히 그 위치에 적이 나온다는
/// 씬에 하나만 배치.
/// </summary>
public class SummonEffectPlayer : MonoBehaviour
{
    [Header("예고 (경고 링 등, 위치마다 하나씩)")]
    [SerializeField] private GameObject warningIndicatorPrefab;

    [Header("실제 등장 시점 (버스트 이펙트, 예고 시간 뒤 자동 재생)")]
    [SerializeField] private GameObject summonBurstPrefab;
    [SerializeField] private AnimationClip summonBurstClip; // 재생 시간 계산용
    [SerializeField] private SFXType summonSfxType = SFXType.Summon;

    private void OnEnable()
    {
        GameEvents.SummonTelegraph += HandleSummonTelegraph;
    }

    private void OnDisable()
    {
        GameEvents.SummonTelegraph -= HandleSummonTelegraph;
    }

    private void HandleSummonTelegraph(IReadOnlyList<Vector2> positions, float telegraphSeconds)
    {
        if (positions == null) return;

        for (int i = 0; i < positions.Count; i++)
        {
            Vector2 position = positions[i];

            if (warningIndicatorPrefab != null)
            {
                var indicator = Instantiate(warningIndicatorPrefab, position, Quaternion.identity);
                Destroy(indicator, telegraphSeconds); // 예고 시간과 정확히 맞춰서 사라짐
            }

            StartCoroutine(SpawnBurstAfterDelay(position, telegraphSeconds));
        }
    }

    private System.Collections.IEnumerator SpawnBurstAfterDelay(Vector2 position, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (summonBurstPrefab != null)
        {
            var instance = Instantiate(summonBurstPrefab, position, Quaternion.identity);
            float lifetime = summonBurstClip != null ? summonBurstClip.length : 0.5f;
            Destroy(instance, lifetime);
        }

        SoundManager.instance?.PlaySFX(summonSfxType);
    }
}