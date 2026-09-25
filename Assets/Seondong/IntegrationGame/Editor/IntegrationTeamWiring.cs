using UnityEngine.SceneManagement;

namespace Seondong.IntegrationGame
{
    // Preserve earlier integration entry points while using the final team handoff.
    public static class IntegrationTeamWiring
    {
        public static void Configure(Scene scene) => FinalSubmissionWiring.ConfigureGame(scene);
        public static void Validate(Scene scene) => FinalSubmissionWiring.Validate(scene);
    }
}
