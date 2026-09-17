using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 번개 연쇄 전용 연출. 원형(ElementReactionTriggered)으로는 표현이 안 되는
/// "중심 → 각 대상"으로 이어지는 줄기를 그림.
///
/// ⚠️ GameEvents.ChainReactionTriggered가 아직 없음 — 유신님께 요청한 상태.
///    반영되면 OnEnable/OnDisable 구독 주석만 풀면 바로 동작.
/// </summary>
public class LightningChainEffectPlayer : MonoBehaviour
{
    [SerializeField] private LineRenderer boltPrefab; // 중심→대상 하나를 잇는 번개 줄기 프리팹
    [SerializeField] private float boltLifetime = 0.15f;
    [SerializeField] private SFXType sfxType = SFXType.Attack; // TODO: 번개 연쇄 전용 SFXType 추가되면 교체

    [Header("타격 지점 버스트 (각 대상 위치에 추가로 터지는 이펙트, 선택)")]
    [SerializeField] private GameObject burstEffectPrefab;     // 화염/냉기와 같은 방식(Animator 자동생성)으로 만든 프리팹
    [SerializeField] private AnimationClip burstAnimationClip; // 재생 시간 계산용
    [SerializeField] private float burstFallbackLifetime = 0.3f;

    private void OnEnable()
    {
    
       GameEvents.ChainReactionTriggered += HandleChainReaction;
    }

    private void OnDisable()
    {
       GameEvents.ChainReactionTriggered -= HandleChainReaction;
    }

    private void HandleChainReaction(MagicElement element, Vector2 origin, IReadOnlyList<Vector2> targets)
    {
        if (element != MagicElement.Lightning || boltPrefab == null) return;

        for (int i = 0; i < targets.Count; i++)
        {
            DrawBolt(origin, targets[i]);
            SpawnBurst(targets[i]);
        }

        SoundManager.instance?.PlaySFX(sfxType);
    }

    private void DrawBolt(Vector2 from, Vector2 to)
    {
        var bolt = Instantiate(boltPrefab);
        bolt.positionCount = 2;
        bolt.SetPosition(0, from);
        bolt.SetPosition(1, to);
        Destroy(bolt.gameObject, boltLifetime);
    }

    private void SpawnBurst(Vector2 position)
    {
        if (burstEffectPrefab == null) return;

        var instance = Instantiate(burstEffectPrefab, position, Quaternion.identity);
        float lifetime = burstAnimationClip != null ? burstAnimationClip.length : burstFallbackLifetime;
        Destroy(instance, lifetime);
    }
}