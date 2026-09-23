using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// "중심 → 개별 대상"으로 이어지는 반응 전부를 원소별로 처리.
/// (예전엔 Lightning만 처리하던 LightningChainEffectPlayer였는데, 화염도 같은 이벤트를 쓰기 시작해서 범용으로 확장).
/// </summary>
public class ChainReactionEffectPlayer : MonoBehaviour
{
    [System.Serializable]
    public class ChainVisualEntry
    {
        public MagicElement element;
        public LineRenderer boltPrefab;   // 중심→대상 하나를 잇는 줄기 프리팹
        public float boltLifetime = 0.15f;

        [Header("타격 지점 버스트 (선택)")]
        public GameObject burstEffectPrefab;
        public AnimationClip burstAnimationClip;
        public float burstFallbackLifetime = 0.3f;

        public SFXType sfxType;
    }

    [SerializeField] private ChainVisualEntry[] entries;

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
        var entry = entries?.FirstOrDefault(e => e.element.Equals(element));
        if (entry == null)
        {
            
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            DrawBolt(entry, origin, targets[i]);
            SpawnBurst(entry, targets[i]);
        }

        SoundManager.instance?.PlaySFX(entry.sfxType);
    }

    private void DrawBolt(ChainVisualEntry entry, Vector2 from, Vector2 to)
    {
        if (entry.boltPrefab == null) return;

        var bolt = Instantiate(entry.boltPrefab);
        bolt.positionCount = 2;
        bolt.SetPosition(0, from);
        bolt.SetPosition(1, to);
        Destroy(bolt.gameObject, entry.boltLifetime);
    }

    private void SpawnBurst(ChainVisualEntry entry, Vector2 position)
    {
        if (entry.burstEffectPrefab == null) return;

        var instance = Instantiate(entry.burstEffectPrefab, position, Quaternion.identity);
        float lifetime = entry.burstAnimationClip != null ? entry.burstAnimationClip.length : entry.burstFallbackLifetime;
        Destroy(instance, lifetime);
    }
}