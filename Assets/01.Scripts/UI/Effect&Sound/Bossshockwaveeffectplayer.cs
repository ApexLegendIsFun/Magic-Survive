using UnityEngine;

/// <summary>
/// 보스 2페이즈 원형 충격파. 예고(BossShockwaveTelegraph) → 발동(BossShockwaveTriggered) 2단계.
/// 예고 중 보스가 죽으면 Triggered가 안 올 수 있음 — 그 경우 예고 링은 telegraphSeconds가
/// 지나면 스스로 사라지므로(Destroy 예약) 별도 취소 처리 없이도 화면에 안 남음.
/// </summary>
public class BossShockwaveEffectPlayer : MonoBehaviour
{
    [Header("예고 (바닥 원형 인디케이터)")]
    [SerializeField] private GameObject telegraphRingPrefab; // 반경에 맞춰 스케일 조절됨

    [Header("발동 (실제 충격파 버스트)")]
    [SerializeField] private GameObject shockwaveBurstPrefab;
    [SerializeField] private AnimationClip shockwaveBurstClip;
    [SerializeField] private SFXType shockwaveSfxType = SFXType.Eart_Shork;

    [Header("보정 (아트가 '스케일 1 = 반경 1'과 다르게 그려졌을 때 조정)")]
    [Tooltip("예고 링 최종 스케일 = radius * telegraphScaleMultiplier")]
    [SerializeField] private float telegraphScaleMultiplier = 1f;
    [Tooltip("버스트 최종 스케일 = radius * burstScaleMultiplier. 예고 링과 아트가 달라 배율도 따로 둠")]
    [SerializeField] private float burstScaleMultiplier = 1f;

    private void OnEnable()
    {
        GameEvents.BossShockwaveTelegraph += HandleTelegraph;
        GameEvents.BossShockwaveTriggered += HandleTriggered;
    }

    private void OnDisable()
    {
        GameEvents.BossShockwaveTelegraph -= HandleTelegraph;
        GameEvents.BossShockwaveTriggered -= HandleTriggered;
    }

    private void HandleTelegraph(Vector2 center, float radius, float telegraphSeconds)
    {
        if (telegraphRingPrefab == null) return;

        var ring = Instantiate(telegraphRingPrefab, center, Quaternion.identity);
        ring.transform.localScale = Vector3.one * (radius * telegraphScaleMultiplier);
        Destroy(ring, telegraphSeconds);
    }

    private void HandleTriggered(Vector2 center, float radius)
    {
        if (shockwaveBurstPrefab == null) return;

        var instance = Instantiate(shockwaveBurstPrefab, center, Quaternion.identity);
        instance.transform.localScale = Vector3.one * (radius * burstScaleMultiplier);
        float lifetime = shockwaveBurstClip != null ? shockwaveBurstClip.length : 0.5f;
        Destroy(instance, lifetime);

        SoundManager.instance?.PlaySFX(shockwaveSfxType);
    }
}