using UnityEngine;
using UnityEngine.UI;

public class FusionLineManager : MonoBehaviour
{
    [System.Serializable]
    public class FusionEdge
    {
        [Tooltip("이 선이 잇는 두 원소")]
        public MagicElement elementA;
        public MagicElement elementB;

        [Tooltip("씬에 이미 배치된 선(Image, 가늘게 늘인 사각형)")]
        public Image lineImage;

        [Tooltip("융합 불가능할 때 켜줄 자물쇠 아이콘 (선 위에 겹쳐둔 오브젝트)")]
        public GameObject lockIcon;

        public bool IsIncident(MagicElement e) => elementA == e || elementB == e;
    }

    [Header("오각형 5개 변")]
    [SerializeField] private FusionEdge[] edges;

    [Header("색상")]
    [SerializeField] private Color validColor = new Color(0.35f, 0.85f, 0.4f); // 초록
    [SerializeField] private Color invalidColor = new Color(1f, 1f, 1f, 0.25f); // 반투명 회색(비활성 느낌)
    [SerializeField] private Color neutralColor = Color.white; // 아무것도 선택/hover 안 된 기본 상태

    private const int ElementCount = 5;

    public static bool AreAdjacent(MagicElement a, MagicElement b)
    {
        int diff = Mathf.Abs((int)a - (int)b);
        return diff == 1 || diff == ElementCount - 1; // 1 또는 4(0↔4로 이어짐)
    }

    public void SetActiveElement(MagicElement? active)
    {
        foreach (var edge in edges)
        {
            if (edge.lineImage == null) continue;

            if (active == null)
            {
                edge.lineImage.color = neutralColor;
                if (edge.lockIcon != null) edge.lockIcon.SetActive(false);
                continue;
            }

            bool possible = edge.IsIncident(active.Value);
            edge.lineImage.color = possible ? validColor : invalidColor;
            if (edge.lockIcon != null) edge.lockIcon.SetActive(!possible);
        }
    }
}