using UnityEngine;

/// <summary>
/// Enemy.FrozenChanged(bool)를 구독해서 빙결 연출(오버레이/색 틴트)을 씌우고 벗김.
/// Enemy와 같은 프리팹에 붙임. 풀링되는 오브젝트라 구독/해제를 OnEnable/OnDisable에 맞춤.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyFrozenVisualController : MonoBehaviour
{
    [SerializeField] private GameObject frozenOverlay;      // 얼음 오버레이 스프라이트/이펙트 오브젝트
    [SerializeField] private SpriteRenderer targetRenderer; // 색 틴트를 쓸 경우에 사용하기 없으면 사용하지 않아도됨 
    [SerializeField] private Color frozenTint = new Color(0.75f, 0.92f, 1f, 1f);

    private Enemy enemy;
    private Color originalColor;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        if (targetRenderer != null) originalColor = targetRenderer.color;
    }

    private void OnEnable()
    {
        //TODO: 현재 이부분이 연동되지 않았으므로 연동이 되는대로 주석 해제 후 테스트.
        // enemy.FrozenChanged += HandleFrozenChanged;

        //TODO: 현재 이부분이 연동되지 않았으므로 연동이 되는대로 주석 해제 후 테스트.
        // ApplyVisual(enemy.IsFrozen);
    }

    private void OnDisable()
    {
        //TODO: 현재 이부분이 연동되지 않았으므로 연동이 되는대로 주석 해제 후 테스트.
        // enemy.FrozenChanged -= HandleFrozenChanged;
    }

    private void HandleFrozenChanged(bool frozen)
    {
        ApplyVisual(frozen);
    }

    private void ApplyVisual(bool frozen)
    {
        if (frozenOverlay != null) frozenOverlay.SetActive(frozen);
        if (targetRenderer != null) targetRenderer.color = frozen ? frozenTint : originalColor;
    }
}