using TMPro;
using UnityEngine;
//using DG.Tweening;

public class DamageUi : MonoBehaviour
{
    // 예: DOTween을 사용하여 Fade Out + 이동 연출(고려중)
    // TODO: Enemy 머리위에 데미지 Text 출력 예정 

    [SerializeField] private TextMeshProUGUI damageText;

    [Header("Animation")]
    [SerializeField] private float duration = 1f;
    [SerializeField] private float moveDistance = 0.5f;

    private CanvasGroup canvasGroup;


    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }


    public void SetDamage(int damage)
    {
        damageText.text = $"{damage}";
    }


    public void Show(Vector3 position, int damage)
    {
        transform.position = position;

        SetDamage(damage);

        // 풀링으로 재사용되므로 상태 초기화
        canvasGroup.alpha = 1f;

        //// 이전 Tween 제거
        //transform.DOKill();        => 여기서 뱀서류 특성상, dmg 텍스트는 수없이 반복되므로 DoKill이 계속해서 반복될 예정이므로 트윈 사용 고려중 
        //canvasGroup.DOKill();

        //// 위로 이동
        //transform.DOMoveY(
        //    position.y + moveDistance,
        //    duration
        //);

        //// Fade Out
        //canvasGroup.DOFade(0f, duration)
        //    .OnComplete(ReturnToPool);
    }


    public void ReturnToPool()
    {
        
    }
}
