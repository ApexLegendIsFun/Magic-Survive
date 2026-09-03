using TMPro;
using UnityEngine;
//using DG.Tweening;

public class DamageUi : MonoBehaviour
{
    // TODO: Enemy 머리위에 데미지 Text 출력 예정 

    [SerializeField] private TextMeshProUGUI damageText;

    [Header("Animation")]
    [SerializeField] private float duration = 1f; //필요 시 값 조정
    [SerializeField] private float moveSpeed = 50f;

    private float timer;

    public void Show(int damage, Vector3 position)
    {
        transform.position = position;

        damageText.text = $"{damage}";

        timer = 0f;
        damageText.alpha = 1f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        float progress = timer / duration;
        damageText.alpha = 1f - progress;

        if (timer >= duration)
        {
            UiObjectPool.instance.ReturnObject(
                "DamageText",
                gameObject
            );
        }
    }
}
