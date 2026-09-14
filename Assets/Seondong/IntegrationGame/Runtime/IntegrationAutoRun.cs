using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Seondong.IntegrationGame
{
    // Opt-in, engine-level input test. No OS input, combat mutations or forced outcomes.
    public sealed class IntegrationAutoRun : MonoBehaviour
    {
        [Serializable] public class Report
        {
            public string scenario, status = "RUNNING", outcome = "NotReached", input = "Unity Input System virtual devices";
            public int width, height, levelUps, kills, chargerSpawns, summonerSpawns, bossSpawns;
            public float combatSeconds, wallSeconds;
            public bool movement, restarted, titleReturned, quitButtonDispatched;
            public List<string> checkpoints = new List<string>();
            public List<string> failures = new List<string>();
            public string startingElement;
            public int highestStartingSkillLevel, visualPoolReuses;
            public List<int> visualTiers = new List<int>();
        }
        private Report report;
        private Keyboard keyboard;
        private Mouse mouse;
        private string output;
        private float began;
        private Vector2 mousePosition;
        private bool stopping;
        private int imageIndex;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private Enemy boss;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
        private bool previousRunInBackground;
        private MagicElement startingElement = MagicElement.Fire;
        private readonly Dictionary<int, Vector2Int> visualSnapshots = new Dictionary<int, Vector2Int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string scenario = Argument("-integration-auto");
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(scenario)) scenario = UnityEditor.SessionState.GetString("Seondong.IntegrationGame.AutoScenario", "");
            const string request = "Logs/SeondongIntegration/auto-play-request.txt";
            if (string.IsNullOrEmpty(scenario) && File.Exists(request))
            {
                scenario = File.ReadAllText(request).Trim();
                File.Delete(request);
            }
#endif
            if (string.IsNullOrEmpty(scenario)) return;
            if (FindFirstObjectByType<IntegrationAutoRun>() != null) return;
            var runner = new GameObject("IntegrationAutoRun").AddComponent<IntegrationAutoRun>();
            DontDestroyOnLoad(runner.gameObject);
            runner.Begin(scenario);
        }
        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : "";
        }
        private void Begin(string scenario)
        {
            began = Time.realtimeSinceStartup;
            output = Argument("-integration-output");
            if (string.IsNullOrEmpty(output)) output = Path.GetFullPath("Logs/SeondongIntegration/" + scenario + "-editor");
            Directory.CreateDirectory(output);
            report = new Report { scenario = scenario, width = Screen.width, height = Screen.height };
            string elementArgument = Argument("-integration-element");
            if (!string.IsNullOrEmpty(elementArgument)) startingElement = (MagicElement)Enum.Parse(typeof(MagicElement), elementArgument, true);
            report.startingElement = startingElement.ToString();
            previousRunInBackground = Application.runInBackground;
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>("IntegrationKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("IntegrationMouse");
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            Application.logMessageReceived += Log;
            Write();
            StartCoroutine(Guard(Run()));
        }
        private IEnumerator Guard(IEnumerator first)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(first);
            while (stack.Count > 0)
            {
                object next = null; bool advanced = false; Exception failure = null;
                try { advanced = stack.Peek().MoveNext(); if (advanced) next = stack.Peek().Current; }
                catch (Exception error) { failure = error; }
                if (failure != null) { Fail(failure.ToString()); yield break; }
                if (!advanced) { stack.Pop(); continue; }
                if (next is IEnumerator nested && !(next is CustomYieldInstruction)) stack.Push(nested);
                else yield return next;
            }
        }
        private void Log(string message, string trace, LogType kind)
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
            {
                if (report.failures.Count < 30) report.failures.Add("Runtime: " + message);
                Write();
            }
        }
        private void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        private void Mark(string value) { report.checkpoints.Add(value); Debug.Log("[Integration Auto] " + value); Write(); }
        private void Write()
        {
            report.wallSeconds = Time.realtimeSinceStartup - began;
            File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(output, $"{imageIndex++:00}-{name}.png"), texture.EncodeToPNG());
            Destroy(texture);
        }
        private Button FindButton(string name)
        {
            return FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == name && b.isActiveAndEnabled && b.IsInteractable());
        }
        private void BindInput()
        {
            var player = FindFirstObjectByType<PlayerInput>();
            if (player != null) player.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, mouse);
            var ui = FindFirstObjectByType<InputSystemUIInputModule>();
            if (ui != null && ui.actionsAsset != null) ui.actionsAsset.devices = new InputDevice[] { keyboard, mouse };
        }
        private Vector2 ButtonPoint(Button button)
        {
            Check(button != null, "Expected visible button missing.");
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
                Check(screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height,
                    "Button clipped: " + button.name);
            }
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject,
                "Button occluded: " + button.name + "; first=" + (hits.Count > 0 ? hits[0].gameObject.name : "none"));
            return point;
        }
        private IEnumerator Click(string name, bool quit = false)
        {
            // Scene/UI reconstruction can span several frames after a reload.
            yield return Await(() => FindButton(name) != null, 5, "Button did not become visible: " + name);
            BindInput();
            yield return null;
            mousePosition = ButtonPoint(FindButton(name));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = mousePosition });
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = mousePosition, buttons = 1 });
            yield return null; yield return null;
            if (quit) { report.quitButtonDispatched = true; report.status = report.failures.Count == 0 ? "PASS" : "FAIL"; Write(); }
            InputSystem.QueueStateEvent(mouse, new MouseState { position = mousePosition });
            yield return new WaitForSecondsRealtime(.3f);
        }
        private IEnumerator Await(Func<bool> condition, float seconds, string message)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition()) { Check(Time.realtimeSinceStartup < deadline, message); yield return null; }
            yield return null;
        }
        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

        private IEnumerator Run()
        {
            Check(new[] { "death", "victory", "timeout", "layout", "growth" }.Contains(report.scenario), "Unknown scenario.");
            yield return new WaitForSecondsRealtime(1);
            Check(SceneManager.GetActiveScene().path.Contains("Seondong/IntegrationGame"), "Wrong scene loaded.");
            BindInput(); Canvas.ForceUpdateCanvases();
            ButtonPoint(FindButton("START GAME")); ButtonPoint(FindButton("QUIT"));
            Mark("Title buttons visible and topmost"); yield return Capture("title");
            yield return Click("START GAME");
            yield return Await(() => FindFirstObjectByType<GameFlowController>() != null, 10, "Title failed to load gameplay.");
            var flow = FindFirstObjectByType<GameFlowController>();
            var run = FindFirstObjectByType<RunDirector>();
            var progression = FindFirstObjectByType<PlayerProgression>();
            var elites = FindFirstObjectByType<IntegrationEliteSpawner>();
            var spawner = FindFirstObjectByType<BossSpawner>();
            var manager = FindFirstObjectByType<EnemyManager>();
            Check(elites != null && spawner != null && manager != null, "Encounter wiring missing.");
            spawner.BossSpawned += enemy => { boss = enemy; report.bossSpawns++; Check(manager.ActiveCount == 1, "Boss transition left previous enemies."); };
            Check(flow.State == GameFlowState.ElementSelect && Mathf.Approximately(Time.timeScale, 0), "Initial selection must pause.");
            yield return new WaitForSecondsRealtime(.5f);
            foreach (var element in MagicContentCatalog.PentagonElements) ButtonPoint(FindButton("Element_" + element));
            yield return Capture("element-select");
            yield return Click("Element_" + startingElement);
            yield return Await(() => flow.State == GameFlowState.Playing, 5, "Starting selection did not begin combat.");
            BindInput();
            var player = FindFirstObjectByType<PlayerController>();
            Vector3 original = player.transform.position;
            Keys(Key.D); yield return new WaitForSecondsRealtime(.4f); Keys(); yield return null;
            report.movement = Vector3.Distance(original, player.transform.position) > .1f;
            Check(report.movement, "Virtual keyboard did not move player.");
            Mark("Movement through PlayerInput verified"); yield return Capture("combat");
            float deadline = Time.realtimeSinceStartup + 760;
            float nextEvidence = 30;
            int observedElites = 0;
            bool observedBoss = false;
            while (!flow.IsTerminal)
            {
                Check(Time.realtimeSinceStartup < deadline, "Run exceeded real-time timeout.");
                if (flow.State == GameFlowState.LevelUp)
                {
                    Keys(); float pausedTime = run.ElapsedCombatTime;
                    yield return new WaitForSecondsRealtime(.15f);
                    Check(Mathf.Approximately(Time.timeScale, 0) && Mathf.Approximately(pausedTime, run.ElapsedCombatTime), "Level-up failed to pause.");
                    var skills = FindFirstObjectByType<PlayerSkillSystem>();
                    var choices = FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.name.StartsWith("Skill_") && b.IsInteractable()).OrderBy(b => report.scenario == "death" || report.scenario == "layout" || report.scenario == "growth" ? (b.name == "Skill_" + startingElement ? 0 : 1) : skills.GetSkillLevel((MagicElement)Enum.Parse(typeof(MagicElement), b.name.Substring(6)))).ToArray();
                    Check(choices.Length > 0 && choices.Length <= 3, "Invalid skill choices.");
                    foreach (var choice in choices) ButtonPoint(choice);
                    if (report.levelUps == 0) yield return Capture("levelup");
                    yield return Click(choices[0].name); yield return Click("ConfirmButton");
                    yield return Await(() => flow.State != GameFlowState.LevelUp, 3, "Skill confirmation failed to resume combat.");
                    report.levelUps++;
                    Check(progression.Level > 1, "Level did not increase through combat XP.");
                    Mark("Level-up confirmed through input");
                }
                else
                {
                    if (report.scenario == "growth" && report.visualTiers.Count == 4) MoveTowardEnemy(player);
                    else MoveBot(player, report.scenario == "death" || report.scenario == "layout", flow.State == GameFlowState.Boss && report.scenario == "timeout");
                    yield return new WaitForSecondsRealtime(.1f);
                }
                var skillSystem = FindFirstObjectByType<PlayerSkillSystem>();
                report.highestStartingSkillLevel = Mathf.Max(report.highestStartingSkillLevel, skillSystem.GetSkillLevel(startingElement));
                foreach (var visual in FindObjectsByType<IntegrationMagicVisual>(FindObjectsSortMode.None))
                {
                    int id = visual.GetInstanceID();
                    if (visualSnapshots.TryGetValue(id, out var previous))
                    {
                        if (previous.x == visual.ActivationSerial) Check(previous.y == visual.Tier, "In-flight visual tier changed.");
                        else report.visualPoolReuses++;
                    }
                    visualSnapshots[id] = new Vector2Int(visual.ActivationSerial, visual.Tier);
                    Check(visual.Tier == MagicVisualProfile.TierForLevel(visual.CapturedLevel), "Visual level mapping mismatch.");
                    if (visual.profile.element == startingElement && !report.visualTiers.Contains(visual.Tier))
                    {
                        report.visualTiers.Add(visual.Tier);
                        Mark("Real projectile visual: " + startingElement + " tier=" + visual.Tier + " level=" + visual.CapturedLevel);
                        yield return Capture("magic-" + startingElement + "-tier-" + visual.Tier);
                    }
                }
                report.combatSeconds = run.ElapsedCombatTime; report.kills = run.KillCount;
                report.chargerSpawns = elites.ChargerSpawnCount; report.summonerSpawns = elites.SummonerSpawnCount;
                int count = report.chargerSpawns + report.summonerSpawns;
                Check(report.chargerSpawns <= 1 && report.summonerSpawns <= 1 && report.bossSpawns <= 1, "Duplicate encounter.");
                if (count > observedElites) { observedElites = count; Mark("Elite spawned: " + count); yield return Capture("elite-" + count); }
                if (flow.State == GameFlowState.Boss && !observedBoss)
                {
                    observedBoss = true;
                    Check(!progression.ExperienceEnabled && elites.MissingCount == 0, "Boss boundary did not disable XP / elite binding missing.");
                    Check(report.chargerSpawns == 1 && report.summonerSpawns == 1 && report.bossSpawns == 1, "Scheduled encounters missing.");
                    yield return null;
                    var bossTimer = FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Boss_Timer");
                    int remaining = Mathf.CeilToInt(Mathf.Max(0, RunTimelineRules.TimeLimit - run.ElapsedCombatTime));
                    Check(bossTimer != null && bossTimer.text == $"{remaining / 60:00}:{remaining % 60:00}", "Boss HUD timer is not bound to run time.");
                    Mark("Boss boundary and encounter counts verified"); yield return Capture("boss");
                }
                if (run.ElapsedCombatTime >= nextEvidence) { Mark("Combat seconds=" + run.ElapsedCombatTime.ToString("F1")); nextEvidence += 30; }
            }
            Keys(); yield return new WaitForSecondsRealtime(.5f);
            report.outcome = run.Result.Outcome.ToString(); report.combatSeconds = run.ElapsedCombatTime; report.kills = run.KillCount;
            string expected = report.scenario == "victory" ? "Victory" : report.scenario == "timeout" ? "Timeout" : "Defeat";
            if (report.outcome != expected) report.failures.Add("Expected " + expected + ", observed " + report.outcome);
            if (report.scenario == "growth" && (report.highestStartingSkillLevel != 8 || report.visualTiers.Count != 4 || report.visualPoolReuses == 0))
                report.failures.Add("Natural growth did not verify all four visual tiers and pool reuse.");
            if (report.scenario == "victory" || report.scenario == "timeout")
            {
                if (report.levelUps == 0 || report.kills == 0 || report.chargerSpawns != 1 || report.summonerSpawns != 1 || report.bossSpawns != 1)
                    report.failures.Add("Full run did not reach every required combat stage.");
            }
            ButtonPoint(FindButton("RestartButton")); ButtonPoint(FindButton("TitleButton"));
            Check(!FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t => t.name == "Boss_Timer"), "Boss HUD remained over the result.");
            Mark("Result=" + report.outcome); yield return Capture("result");
            yield return Click("RestartButton");
            yield return Await(() => FindFirstObjectByType<GameFlowController>() != flow, 10, "Restart did not reload scene.");
            flow = FindFirstObjectByType<GameFlowController>(); run = FindFirstObjectByType<RunDirector>(); progression = FindFirstObjectByType<PlayerProgression>();
            Check(flow.State == GameFlowState.ElementSelect && run.ElapsedCombatTime == 0 && run.KillCount == 0 && progression.Level == 1, "Restart did not reset run.");
            Check(FindObjectsByType<IntegrationEliteSpawner>(FindObjectsSortMode.None).Length == 1 && FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1, "Duplicate systems after restart.");
            report.restarted = true; Mark("Restart reset verified"); yield return Capture("restart");
            yield return Click("Element_Fire"); BindInput();
            yield return Await(() => FindObjectsByType<IntegrationMagicVisual>(FindObjectsSortMode.None).Any(v => v.isActiveAndEnabled), 15, "No projectile after restart.");
            Check(FindObjectsByType<IntegrationMagicVisual>(FindObjectsSortMode.None).Where(v => v.isActiveAndEnabled).All(v => v.CapturedLevel == 1 && v.Tier == 0), "Restart retained an upgraded projectile visual.");
            Mark("Restart projectile appearance reset to level 1 verified");
            float deathDeadline = Time.realtimeSinceStartup + 240;
            while (!flow.IsTerminal)
            {
                Check(Time.realtimeSinceStartup < deathDeadline, "Second natural death not reached.");
                if (flow.State == GameFlowState.LevelUp)
                {
                    var choice = FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name.StartsWith("Skill_") && b.IsInteractable());
                    yield return Click(choice.name); yield return Click("ConfirmButton");
                }
                var victim = FindFirstObjectByType<PlayerController>();
                var nearest = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Where(e => e.IsAlive).OrderBy(e => (e.transform.position - victim.transform.position).sqrMagnitude).FirstOrDefault();
                if (nearest != null)
                {
                    Vector2 toward = nearest.transform.position - victim.transform.position;
                    var deathKeys = new List<Key>();
                    if (toward.x > .1f) deathKeys.Add(Key.D); if (toward.x < -.1f) deathKeys.Add(Key.A);
                    if (toward.y > .1f) deathKeys.Add(Key.W); if (toward.y < -.1f) deathKeys.Add(Key.S);
                    Keys(deathKeys.ToArray());
                }
                else Keys();
                yield return new WaitForSecondsRealtime(.15f);
            }
            yield return Click("TitleButton");
            yield return Await(() => FindButton("START GAME") != null, 10, "Result failed to return to title.");
            report.titleReturned = true; Mark("Title return verified"); yield return Capture("returned-title");
            yield return Click("QUIT", true);
            yield return new WaitForSecondsRealtime(3);
            Fail("Quit button did not terminate player.");
        }

        private void MoveBot(PlayerController player, bool stand, bool avoidBoss)
        {
            if (stand) { Keys(); return; }
            Vector2 position = player.transform.position;
            var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Where(e => e.IsAlive).ToArray();
            Vector2 nearest = enemies.Length == 0 ? Vector2.right : (Vector2)enemies.OrderBy(e => ((Vector2)e.transform.position - position).sqrMagnitude).First().transform.position - position;
            Vector2 preferred;
            if (avoidBoss && boss != null) preferred = (position - (Vector2)boss.transform.position).normalized;
            else
            {
                float distance = nearest.magnitude;
                Vector2 tangent = new Vector2(-nearest.y, nearest.x).normalized;
                preferred = tangent + nearest.normalized * (distance > 4 ? .8f : distance < 2.5f ? -1f : 0);
            }
            var directions = new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right, new Vector2(1,1).normalized, new Vector2(1,-1).normalized, new Vector2(-1,1).normalized, new Vector2(-1,-1).normalized };
            Vector2 best = directions[0]; float bestScore = float.NegativeInfinity;
            foreach (var direction in directions)
            {
                Vector2 ahead = position + direction * 1.5f;
                float danger = 0;
                foreach (var enemy in enemies)
                {
                    float d = Vector2.Distance(ahead, enemy.transform.position);
                    if (d < 3) danger += (3 - d) * (3 - d);
                }
                float score = Vector2.Dot(direction, preferred.normalized) - danger * 3;
                if (score > bestScore) { bestScore = score; best = direction; }
            }
            var keys = new List<Key>();
            if (best.x > .3f) keys.Add(Key.D); if (best.x < -.3f) keys.Add(Key.A);
            if (best.y > .3f) keys.Add(Key.W); if (best.y < -.3f) keys.Add(Key.S);
            Keys(keys.ToArray());
        }
        private void MoveTowardEnemy(PlayerController player)
        {
            var target = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Where(e => e.IsAlive).OrderBy(e => (e.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
            if (target == null) { Keys(); return; }
            Vector2 delta = target.transform.position-player.transform.position;
            var keys = new List<Key>();
            if (delta.x > .1f) keys.Add(Key.D); if (delta.x < -.1f) keys.Add(Key.A);
            if (delta.y > .1f) keys.Add(Key.W); if (delta.y < -.1f) keys.Add(Key.S);
            Keys(keys.ToArray());
        }
        private void Fail(string message)
        {
            if (stopping) return;
            stopping = true; report.failures.Add(message); report.status = "FAIL"; Write();
            Debug.LogWarning("[Integration Auto] FAIL " + message);
#if UNITY_EDITOR
            UnityEditor.SessionState.EraseString("Seondong.IntegrationGame.AutoScenario");
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(2);
#endif
        }
        private void OnDestroy()
        {
            Application.logMessageReceived -= Log;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            Application.runInBackground = previousRunInBackground;
        }
    }
}
