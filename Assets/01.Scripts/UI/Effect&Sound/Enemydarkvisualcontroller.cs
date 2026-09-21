using UnityEngine;

/// <summary>
/// 암흑 3중첩 증폭 상태(IsDarkAmplified)를 감지해서 StatusEffectVisual에 켜고/끄라고 지시만 함.
/// 실제 그래픽 처리는 StatusEffectVisual이 담당 (재사용 가능).
/// Enemy와 같은 오브젝트에 붙임.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyDarkVisualController : MonoBehaviour
{
    [SerializeField] private StatusEffectVisual visual;

    private Enemy enemy;
    private Health health;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        health = GetComponent<Health>();
        if (visual == null) visual = GetComponent<StatusEffectVisual>();
    }

    private void OnEnable()
    {
        enemy.DarkAmplifiedChanged += HandleDarkAmplifiedChanged;
        if (health != null) health.Died += HandleDied;

        // 풀 재사용 시 이벤트를 놓쳤을 가능성에 기대지 않고, 현재 실제 상태로 강제 동기화
        visual?.SetImmediate(enemy.IsDarkAmplified);
    }

    private void OnDisable()
    {
        enemy.DarkAmplifiedChanged -= HandleDarkAmplifiedChanged;
        if (health != null) health.Died -= HandleDied;

        // 해제 이벤트가 항상 먼저 온다고 가정하지 않음 — 무조건 꺼서 다음 재사용에 안전하게
        visual?.SetImmediate(false);
    }

    private void HandleDarkAmplifiedChanged(bool amplified)
    {
        if (amplified) visual?.Show();
        else visual?.Hide();
    }

    private void HandleDied()
    {
        // 사망~풀 반환 사이 1프레임 동안 IsDarkAmplified가 아직 true인 걸 기다리지 않고 즉시 끔
        visual?.Hide();
    }
}