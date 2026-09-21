using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 엘리트/보스 등장 패널만 독립적으로 테스트하기 위한 임시 디버그.
/// RunDirector의 event는 외부에서 강제 발행이 안 되므로, EliteAnnouncementUi를 직접 호출.
/// F6 = 돌진자, F7 = 소환술사, F8 = 보스.
/// 실제 파이프라인(RunTimelineRules 정상값)으로 테스트할 때는 씬에서 빼세요.
/// </summary>
public class EliteAnnouncementDebugTrigger : MonoBehaviour
{
    [SerializeField] private EliteAnnouncementUi announcementUi;

    private void Awake()
    {
        if (announcementUi == null)
        {
            announcementUi = FindFirstObjectByType<EliteAnnouncementUi>();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || announcementUi == null) return;

        if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            Debug.Log("[EliteAnnouncementDebugTrigger] 돌진자 등장 강제 발동");
            announcementUi.DebugTriggerElite(EliteKind.Charger);
        }

        if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            Debug.Log("[EliteAnnouncementDebugTrigger] 소환술사 등장 강제 발동");
            announcementUi.DebugTriggerElite(EliteKind.Summoner);
        }

        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            Debug.Log("[EliteAnnouncementDebugTrigger] 보스 등장 강제 발동");
            announcementUi.DebugTriggerBoss();
        }
    }
}