using UnityEngine;

/// <summary>
/// 소환 위치 예고(SummonWarningTriggered) + 실제 소환 순간(EnemySummoned) 연출.
/// 씬에 하나만 배치.
///반영되면 OnEnable/OnDisable 구독 주석만 풀면 바로 동작.
/// </summary>
public class SummonEffectPlayer : MonoBehaviour
{
    [Header("소환 위치 예고 (경고 링 등)")]
    [SerializeField] private GameObject warningIndicatorPrefab; // leadTime 동안 유지되는 오브젝트

    [Header("실제 소환 순간 (버스트 이펙트)")]
    [SerializeField] private GameObject summonBurstPrefab;
    [SerializeField] private AnimationClip summonBurstClip; // 재생 시간 계산용
    //[SerializeField] private SFXType summonSfxType = SFXType.Summon;

    private void OnEnable()
    {
       // GameEvents.SummonWarningTriggered += HandleSummonWarning;
       // GameEvents.EnemySummoned += HandleEnemySummoned;
    }

    private void OnDisable()
    {
        //GameEvents.SummonWarningTriggered -= HandleSummonWarning;
       // GameEvents.EnemySummoned -= HandleEnemySummoned;
    }

    private void HandleSummonWarning(Vector2 position, float leadTimeSeconds)
    {
        if (warningIndicatorPrefab == null) return;

        var indicator = Instantiate(warningIndicatorPrefab, position, Quaternion.identity);
        Destroy(indicator, leadTimeSeconds); // 예고 시간과 정확히 맞춰서 사라짐 (소환 타이밍에 맞물림)
    }

    private void HandleEnemySummoned(Vector2 position)
    {
        if (summonBurstPrefab != null)
        {
            var instance = Instantiate(summonBurstPrefab, position, Quaternion.identity);
            float lifetime = summonBurstClip != null ? summonBurstClip.length : 0.5f;
            Destroy(instance, lifetime);
        }

      //  SoundManager.instance?.PlaySFX(summonSfxType);
    }
}