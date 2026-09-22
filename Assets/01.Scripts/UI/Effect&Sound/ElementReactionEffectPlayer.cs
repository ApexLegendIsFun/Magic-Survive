using System.Linq;
using UnityEngine;

/// <summary>
/// GameEvents.ElementReactionTriggered(3중첩 반응 발동)를 구독해서
/// 원소별 VFX + SFX를 해당 위치/반경에 재생.
/// 씬에 하나만 배치 (static 이벤트라 여러 개 두면 중복 재생됨).
///
/// 같은 원소가 서로 다른 반응에서 같은 이벤트를 재사용할 수 있음
/// (예: 냉기 빙결(radius=0) vs 냉기 5레벨 파괴(radius=ShatterRadius)).
/// 이 경우 RadiusMatch로 구분해서 다른 항목을 골라 재생함.
/// </summary>
public class ElementReactionEffectPlayer : MonoBehaviour
{
    public enum RadiusMatch
    {
        Any,          // radius 값과 무관하게 매칭 (기본값, 원소당 반응이 하나뿐일 때)
        ZeroOnly,     // radius <= 0 일 때만 (단일 대상 반응, 예: 빙결)
        PositiveOnly  // radius > 0 일 때만 (범위 반응, 예: 파괴)
    }

    [System.Serializable]
    public class ReactionEffectEntry
    {
        public MagicElement element;
        [Tooltip("한 원소가 반경 0/양수로 서로 다른 반응을 갖는 경우에만 Any 대신 지정")]
        public RadiusMatch radiusMatch = RadiusMatch.Any;
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
        var entry = FindEntry(element, radius);
        if (entry == null)
        {
            Debug.LogWarning($"[ElementReactionEffectPlayer] {element} (radius={radius}) 반응 이펙트가 등록되지 않음");
            return;
        }

        PlayVisual(entry, position, radius);
        PlaySfx(entry);
    }

    // element가 같은 항목이 여러 개면, radius 0/양수에 정확히 맞는 항목을 우선 고르고,
    // 없으면 Any로 지정된 항목으로 대체
    private ReactionEffectEntry FindEntry(MagicElement element, float radius)
    {
        if (effects == null) return null;

        bool isZero = radius <= 0f;
        ReactionEffectEntry fallback = null;

        foreach (var entry in effects)
        {
            if (!entry.element.Equals(element)) continue;

            if (entry.radiusMatch == RadiusMatch.ZeroOnly && isZero) return entry;
            if (entry.radiusMatch == RadiusMatch.PositiveOnly && !isZero) return entry;
            if (entry.radiusMatch == RadiusMatch.Any && fallback == null) fallback = entry;
        }

        return fallback;
    }

    private void PlayVisual(ReactionEffectEntry entry, Vector2 position, float radius)
    {
        if (entry.effectPrefab == null) return;

        var instance = Instantiate(entry.effectPrefab, position, Quaternion.identity);

        float scale = radius > 0f ? radius : singleTargetEffectScale;
        instance.transform.localScale = Vector3.one * scale;

        // Animator는 Instantiate되는 순간 Controller의 기본 State를 자동 재생함.
        // 별도로 Play()를 호출할 필요 없음 (레거시 Animation과의 가장 큰 차이).
        float lifetime = entry.animationClip != null ? entry.animationClip.length : fallbackLifetime;
        Destroy(instance, lifetime);
    }

    private void PlaySfx(ReactionEffectEntry entry)
    {
        SoundManager.instance?.PlaySFX(entry.sfxType);
    }
}