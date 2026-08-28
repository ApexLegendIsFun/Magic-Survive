using UnityEngine;



public class UIManager : MonoBehaviour
{
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

    //Event가 발생한다면  여기서 처리

    public void KillCountText(int count)
    {
        hudDynamicUi.UpdateKillCount(count);
    }

}
