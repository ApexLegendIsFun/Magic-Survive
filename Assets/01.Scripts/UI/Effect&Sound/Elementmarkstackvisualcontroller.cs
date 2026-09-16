using TMPro;
using UnityEngine;

/// <summary>
/// 원소 표식 중첩(1/2/3스택)을 적 머리 위 등에 실시간으로 보여줌.
/// 3중첩 반응(ElementReactionEffectPlayer)과는 별개 — 그건 "터지는 순간"만 보여주는것.
/// 이건 "지금 몇 스택 쌓였는지"를 항상 보여줌.
/// Enemy와 같은 프리팹에 붙임. 풀링되는 오브젝트라 OnEnable/OnDisable로 구독 관리.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class ElementMarkStackVisualController : MonoBehaviour
{
    [System.Serializable]
    public class StackDisplayEntry
    {
        public MagicElement element;
        public GameObject root;           // 스택 표시 통짜 오브젝트 (아이콘+숫자 묶음)
        public TextMeshProUGUI stackText; // "1" / "2" / "3"
    }

    [SerializeField] private StackDisplayEntry[] displays;

    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        enemy.ElementMarkChanged += HandleElementMarkChanged;
        RefreshAll(); // 재사용 시 이전 상태가 남아있지 않도록 즉시 동기화
    }

    private void OnDisable()
    {
        enemy.ElementMarkChanged -= HandleElementMarkChanged;
        HideAll();
    }

    private void HandleElementMarkChanged(ElementMarkChange change)
    {
        // change의 필드는 안 쓰고 "뭔가 바뀌었다"는 신호로만 사용.
        // 실제 값은 항상 확정 API(GetElementMark)로 다시 조회해서 반영.
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (displays == null) return;

        foreach (var entry in displays)
        {
            int stacks = enemy.GetElementMark(entry.element).Stacks;
            bool visible = stacks > 0;

            if (entry.root != null) entry.root.SetActive(visible);
            if (entry.stackText != null) entry.stackText.text = visible ? stacks.ToString() : string.Empty;
        }
    }

    private void HideAll()
    {
        if (displays == null) return;
        foreach (var entry in displays)
        {
            if (entry.root != null) entry.root.SetActive(false);
        }
    }
}