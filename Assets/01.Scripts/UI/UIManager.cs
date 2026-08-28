using UnityEngine;



public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] Canvas hudStatcUi;
    [SerializeField] Canvas hudDynamicUi;
    [SerializeField] Canvas popupUi;
    [SerializeField] Canvas damageUi;

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

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
