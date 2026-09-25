using UnityEngine;

namespace Seondong.IntegrationGame
{
    public sealed class FinalSubmissionExit : MonoBehaviour
    {
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
