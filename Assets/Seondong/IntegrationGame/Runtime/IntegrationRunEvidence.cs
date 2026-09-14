using UnityEngine;

namespace Seondong.IntegrationGame
{
    // Observes the real run; never advances time, grants XP, damages enemies or changes outcomes.
    public sealed class IntegrationRunEvidence : MonoBehaviour
    {
        private GameFlowController flow;
        private RunDirector run;
        private PlayerProgression progression;
        private void Start()
        {
            flow = GetComponent<GameFlowController>();
            run = GetComponent<RunDirector>();
            progression = GetComponent<PlayerProgression>();
            flow.StateChanged += StateChanged;
            run.ResultReady += ResultReady;
            progression.LevelChanged += LevelChanged;
            StateChanged(flow.State);
        }
        private void StateChanged(GameFlowState state) => Debug.Log($"[Integration Run] State={state}; combatSeconds={run.ElapsedCombatTime:F2}; realtime={Time.realtimeSinceStartup:F2}");
        private void LevelChanged(int level) => Debug.Log($"[Integration Run] Level={level}; combatSeconds={run.ElapsedCombatTime:F2}");
        private void ResultReady(RunResult result) => Debug.Log($"[Integration Run] Outcome={result.Outcome}; combatSeconds={run.ElapsedCombatTime:F2}");
        private void OnDestroy()
        {
            if (flow != null) flow.StateChanged -= StateChanged;
            if (run != null) run.ResultReady -= ResultReady;
            if (progression != null) progression.LevelChanged -= LevelChanged;
        }
    }
}
