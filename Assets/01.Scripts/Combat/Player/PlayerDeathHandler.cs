using UnityEngine;

// 플레이어 사망시 조작 중지

[RequireComponent(typeof(Health))]
public class PlayerDeathHandler : MonoBehaviour
{

    // [임시] 조작 정지는 게임 플로우 영역이나, 테스트를 위해 임시 작성
    [SerializeField] private MonoBehaviour[] disableOnDeath;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        health.Died -= HandleDied;
    }

    private void HandleDied()
    {
        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            if (disableOnDeath[i] != null)
            {
                disableOnDeath[i].enabled = false;
            }
        }

        // [연동:성장] 게임오버 플로우 
        // [연동:UI] 게임오버 화면, 사망
        GameEvents.RaisePlayerDied();
    }
}
