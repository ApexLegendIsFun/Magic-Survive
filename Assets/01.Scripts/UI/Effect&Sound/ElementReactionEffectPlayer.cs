using System.Linq;
using UnityEngine;

/// <summary>
/// GameEvents.ElementReactionTriggered(3중첩 반응 발동)를 구독해서
/// 원소별 VFX + SFX를 해당 위치/반경에 재생.
/// 씬에 하나만 배치 (static 이벤트라 여러 개 두면 중복 재생됨).
/// </summary>
public class ElementReactionEffectPlayer : MonoBehaviour
{
    [System.Serializable]
    public class ReactionEffectEntry
    {
        public MagicElement element;
        public GameObject effectPrefab;     // Animator 컴포넌트 + Controller가 붙은 프리팹
        public AnimationClip animationClip; // Controller 안의 재생 클립과 동일한 것. 재생 시간 계산용으로만 씀
        public SFXType sfxType;
    }

    [SerializeField] private ReactionEffectEntry[] effects;

    [Tooltip("radius가 0으로 오는 단일 대상 반응(빙결 등)일 때 쓸 기본 이펙트 스케일")]
    [SerializeField] private float singleTargetEffectScale = 0.5f;

    [Tooltip("animationClip을 안 넣었을 때 안전하게 정리할 기본 유지 시간(초)")]
    [SerializeField] private float fallbackLifetime = 1f;

    private void OnEnable()
    {
        // static 이벤트: GameEvents.Clear() 호출 시 구독이 날아가므로, 재활성화될 때마다 다시 건다
        GameEvents.ElementReactionTriggered += HandleElementReaction;
    }

    private void OnDisable()
    {
        GameEvents.ElementReactionTriggered -= HandleElementReaction;
    }

    private void HandleElementReaction(MagicElement element, Vector2 position, float radius)
    {
        var entry = effects?.FirstOrDefault(e => e.element.Equals(element));
        if (entry == null)
        {
            Debug.LogWarning($"[ElementReactionEffectPlayer] {element} 반응 이펙트가 등록되지 않음");
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

        // Animator는 Instantiate되는 순간 Controller의 기본 State를 자동 재생함.
        float lifetime = entry.animationClip != null ? entry.animationClip.length : fallbackLifetime;
        Destroy(instance, lifetime);
    }

    private void PlaySfx(ReactionEffectEntry entry)
    {
        SoundManager.instance?.PlaySFX(entry.sfxType);
    }
}