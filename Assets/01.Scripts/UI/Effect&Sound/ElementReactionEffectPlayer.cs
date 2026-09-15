using System.Linq;
using UnityEngine;

/// <summary>
/// GameEvents.ElementReactionTriggered(3중첩 반응 발동)를 구독해서
/// 원소별 VFX + SFX를 해당 위치/반경에 재생.
/// 씬에 하나만 배치 (static 이벤트라 여러 개 두면 중복 재생됨).
/// </summary>
/// 
public class ElementReactionEffectPlayer : MonoBehaviour
{
    [System.Serializable]
    public class ReactionEffectEntry
    {
        public MagicElement element;
        public ParticleSystem effectPrefab; // 점화 폭발 / 빙결 파편 등
        public SFXType sfxType;             // SoundManager.SFXType 중 이 원소 반응에 해당하는 항목
    }

    [SerializeField] private ReactionEffectEntry[] effects;

    [Tooltip("radius가 0으로 오는 단일 대상 반응(빙결 등)일 때 쓸 기본 이펙트 스케일")]
    [SerializeField] private float singleTargetEffectScale = 0.5f;

    private void OnEnable()
    {
        //TODO: 현재 이부분이 아직 연동되지 않았으므로, 연동 된 후에 주석을 해제한다.
        // static 이벤트: GameEvents.Clear() 호출 시 구독이 날아가므로, 재활성화될 때마다 다시 건다
        //GameEvents.ElementReactionTriggered += HandleElementReaction;
    }

    private void OnDisable()
    {
        //TODO: 현재 이부분이 아직 연동되지 않았으므로, 연동 된 후에 주석을 해제한다.
        //GameEvents.ElementReactionTriggered -= HandleElementReaction;
    }

    private void HandleElementReaction(MagicElement element, Vector2 position, float radius)
    {
        var entry = effects?.FirstOrDefault(e => e.element.Equals(element));
        if (entry == null)
        {
            Debug.Log($"[ElementReactionEffectPlayer] {element} 반응 이펙트가 등록되지 않음");
            return;
        }

        PlayVisual(entry, position, radius);
        PlaySfx(entry);
    }

    private void PlayVisual(ReactionEffectEntry entry, Vector2 position, float radius)
    {
        if (entry.effectPrefab == null) return;

        var instance = Instantiate(entry.effectPrefab, position, Quaternion.identity);

        float scale = radius > 0f ? radius : singleTargetEffectScale;
        instance.transform.localScale = Vector3.one * scale;
        instance.Play();

        float lifetime = instance.main.duration + instance.main.startLifetime.constantMax;
        Destroy(instance.gameObject, lifetime);
    }

    private void PlaySfx(ReactionEffectEntry entry)
    {
        SoundManager.instance?.PlaySFX(entry.sfxType);
    }
}