using UnityEngine;
using UnityEngine.Rendering;

// Visual-only companion. Movement, hits, health and pool ownership stay in Combat.
[DisallowMultipleComponent]
public sealed class AssetVisualLifecycle : MonoBehaviour
{
    public Transform visual;
    public bool faceMovement;
    public bool repeatAnimation;
    public Color tint = Color.white;
    private Animator[] animators;
    private SpriteRenderer[] sprites;
    private ParticleSystem[] particles;
    private TrailRenderer[] trails;
    private bool[] hasMove;
    private bool[] hasState;
    private SortingGroup sorting;
    private Vector3 initialScale;
    private Vector3 previousPosition;
    private static readonly int Move = Animator.StringToHash("1_Move");
    private static readonly int State = Animator.StringToHash("State");

    private void Awake()
    {
        if (visual == null) visual = transform;
        initialScale = visual.localScale;
        animators = visual.GetComponentsInChildren<Animator>(true);
        sprites = visual.GetComponentsInChildren<SpriteRenderer>(true);
        particles = visual.GetComponentsInChildren<ParticleSystem>(true);
        trails = visual.GetComponentsInChildren<TrailRenderer>(true);
        sorting = visual.GetComponent<SortingGroup>();
        hasMove = new bool[animators.Length];
        hasState = new bool[animators.Length];
        for (int i = 0; i < animators.Length; i++)
        {
            foreach (var parameter in animators[i].parameters)
            {
                hasMove[i] |= parameter.nameHash == Move && parameter.type == AnimatorControllerParameterType.Bool;
                hasState[i] |= parameter.nameHash == State && parameter.type == AnimatorControllerParameterType.Int;
            }
        }
    }
    private void OnEnable()
    {
        previousPosition = transform.position;
        visual.localScale = initialScale;
        foreach (var animator in animators)
        {
            if (!animator.isActiveAndEnabled) continue;
            animator.Rebind();
            animator.Update(0f);
        }
        foreach (var particle in particles) particle.Play(true);
        foreach (var trail in trails) trail.Clear();
        ApplyTint();
    }
    private void OnDisable()
    {
        foreach (var particle in particles)
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var trail in trails) trail.Clear();
    }
    private void LateUpdate()
    {
        Vector3 delta = transform.position - previousPosition;
        previousPosition = transform.position;
        if (Time.deltaTime <= 0f) return;
        bool moving = delta.sqrMagnitude > 0.0000001f;
        if (faceMovement && Mathf.Abs(delta.x) > 0.00001f)
            visual.localScale = new Vector3(Mathf.Abs(initialScale.x) * (delta.x > 0f ? -1f : 1f),
                initialScale.y, initialScale.z);
        for (int i = 0; i < animators.Length; i++)
        {
            var animator = animators[i];
            if (!animator.isActiveAndEnabled) continue;
            if (hasMove[i]) animator.SetBool(Move, moving);
            if (hasState[i]) animator.SetInteger(State, moving ? 2 : 0);
            if (repeatAnimation && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
                animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);
        }
        if (faceMovement && sorting != null)
            sorting.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 100f);
        ApplyTint();
    }
    private void ApplyTint()
    {
        if (tint == Color.white) return;
        foreach (var sprite in sprites)
            sprite.color = new Color(tint.r, tint.g, tint.b, sprite.color.a);
    }
}
