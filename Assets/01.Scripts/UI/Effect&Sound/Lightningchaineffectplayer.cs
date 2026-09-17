using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 번개 연쇄 전용 연출. 원형(ElementReactionTriggered)으로는 표현이 안 되는
/// "중심 → 각 대상"으로 이어지는 줄기를 그림.
///
///    반영되면 OnEnable/OnDisable 구독 주석만 풀면 바로 동작.
/// </summary>
public class LightningChainEffectPlayer : MonoBehaviour
{
    [SerializeField] private LineRenderer boltPrefab; // 중심→대상 하나를 잇는 번개 줄기 프리팹
    [SerializeField] private float boltLifetime = 0.15f;
    [SerializeField] private SFXType sfxType = SFXType.Chain_lightning; // TODO: 번개 연쇄 전용 SFXType 추가되면 교체

    private void OnEnable()
    {
        // TODO: GameEvents.ChainReactionTriggered 추가되면 주석 해제
        // GameEvents.ChainReactionTriggered += HandleChainReaction;
    }

    private void OnDisable()
    {
        // GameEvents.ChainReactionTriggered -= HandleChainReaction;
    }

    private void HandleChainReaction(MagicElement element, Vector2 origin, IReadOnlyList<Vector2> targets)
    {
        if (element != MagicElement.Lightning || boltPrefab == null) return;

        for (int i = 0; i < targets.Count; i++)
        {
            DrawBolt(origin, targets[i]);
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
}