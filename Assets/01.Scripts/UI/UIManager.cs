using UnityEngine;



public class UIManager : MonoBehaviour
{
    //UiManager를 중계기처럼 사용할 예정 예: UiManager 호출 => UiManager가 아래 클래스의 이벤트들을 관리 및 호출 
    public static UIManager Instance { get; private set; }

    [SerializeField] HudStatcUi hudStatcUi;
    [SerializeField] HudDynamicUi hudDynamicUi;
    [SerializeField] PopupUi popupUi;
    [SerializeField] DamageUi damageUi;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy() //인게임씬에서만 존재하므로, 
    {
        if (Instance == this)
            Instance = null;
    }



    //Event가 발생한다면 여기서 처리

    public void KillCountText(int count) // 킬카운트 텍스트 호출시 사용
    {
        hudDynamicUi.UpdateKillCount(count);
    }

    public void PlayerUpdateHp(int value) // 체력이 바뀔경우,(Slider) 호출시 사용
    {
       
    }

}
