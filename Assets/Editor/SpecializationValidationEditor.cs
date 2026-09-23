using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// API regression test, not a natural-speed gameplay completion test.
[InitializeOnLoad]
public static class SpecializationValidationEditor
{
    private const string Key = "MagicSurvive.SpecializationCheck";
    private const string ScenePath = "Assets/Seondong/IntegrationGame/Scenes/SampleScene.unity";
    private static PlayerSkillSystem previousSkills;
    private static double deadline;
    private static int phase;
    private static int assertions;

    static SpecializationValidationEditor()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key, false) &&
                SessionState.GetBool(Key + ".done", false))
            {
                SessionState.SetBool(Key, false);
                if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(Key + ".pass", false) ? 0 : 1);
            }
        };
    }

    [MenuItem("Tools/Magic Survive/Validate Specialization API")]
    public static void RunBatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        previousSkills = null;
        deadline = 0;
        phase = 0;
        assertions = 0;
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".done", false);
        SessionState.SetBool(Key + ".pass", false);
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || SessionState.GetBool(Key + ".done", false) || !EditorApplication.isPlaying) return;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 60;
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "API test timed out");
            var skills = UnityEngine.Object.FindFirstObjectByType<PlayerSkillSystem>();
            var controller = UnityEngine.Object.FindFirstObjectByType<LevelUpController>();
            if (skills == null || controller == null) return;
            if (phase == 0)
            {
                // Allow scene Start callbacks to settle before the synchronous contract tests.
                phase = 1;
                return;
            }
            if (phase == 1)
            {
                Validate(skills, controller);
                previousSkills = skills;
                phase = 2;
                SceneManager.LoadScene(ScenePath);
                return;
            }
            if (skills == previousSkills) return;
            Require(!skills.Tree.HasStartingElement, "Restart kept element levels");
            foreach (var definition in SpecializationCatalog.All)
                Require(skills.GetSpecializationCount(definition.Id) == 0, "Restart kept specialization count");
            Require(controller.PendingLevelUps == 0, "Restart kept pending opportunities");
            Finish(true, $"PASS: {assertions} assertions; catalog, previews, accumulation, transaction, reentrancy, legacy, MAX, scene restart. UI/combat specialization integration NOT VERIFIED.");
        }
        catch (Exception exception) { Finish(false, exception.ToString()); }
    }

    private static void Validate(PlayerSkillSystem skills, LevelUpController controller)
    {
        var progression = skills.GetComponent<PlayerProgression>();
        var flow = skills.GetComponent<GameFlowController>();
        string mockup = File.ReadAllText("Docs/Mockups/ElementSkillReview/unified.js");
        Require(SpecializationCatalog.All.Count == 15, "Expected fifteen cards");
        Require(SpecializationCatalog.All.Select(x => x.Id).Distinct().Count() == 15, "Duplicate IDs");
        foreach (var definition in SpecializationCatalog.All)
        {
            Require(mockup.Contains("['" + definition.Name + "','" + definition.Description + "']"), "Catalog differs from mockup: " + definition.Name);
            Require(definition.CombatStatus == SpecializationCombatStatus.Applied, "Implemented effect reported as unconnected");
        }
        foreach (var element in MagicContentCatalog.PentagonElements)
            Require(SpecializationCatalog.All.Count(x => x.Element == element) == 3, "Expected three cards per element");
        Require(skills.GetGrowthPreview((MagicElement)99).State == ElementGrowthState.Invalid, "Invalid element preview");
        Require(controller.TryChooseStartingElement(MagicElement.Fire), "Start Fire failed");
        Require(!controller.TryConfirmSpecialization(MagicElement.Fire, SpecializationId.FireDamage), "Accepted without opportunity");

        Action earn = () => progression.AddExperience(progression.RequiredExperience - progression.CurrentExperience);
        earn();
        var locked = skills.GetGrowthPreview(MagicElement.Frost);
        Require(locked.State == ElementGrowthState.Locked && !locked.CanSelect, "Nonadjacent element not locked");
        Require(!controller.TryConfirmSpecialization(MagicElement.Frost, SpecializationId.FrostDamage), "Accepted locked element");
        var unlock = skills.GetGrowthPreview(MagicElement.Lightning);
        Require(unlock.State == ElementGrowthState.Unlock && unlock.CanSelect && unlock.Cards.Count == 0 &&
            !unlock.CurrentStats.HasValue && unlock.NextLevel == 1, "Unlock preview incorrect");
        Require(!controller.TryConfirmSpecialization(MagicElement.Lightning, SpecializationId.LightningDamage), "Unowned element received specialization");
        Require(!controller.TryConfirmSpecialization(MagicElement.Fire, SpecializationId.DarkDamage), "Cross-element card accepted");
        Require(!controller.TryConfirmSpecialization(MagicElement.Fire, (SpecializationId)99), "Invalid card accepted");
        Require(controller.PendingLevelUps == 1 && skills.GetSkillLevel(MagicElement.Fire) == 1, "Rejection consumed growth");

        var before = skills.GetGrowthPreview(MagicElement.Fire);
        Require(before.Cards[0].CurrentBonus == 0 && before.Cards[0].AfterSelectionBonus == 10, "Initial bonus preview");
        for (int i = 0; i < 10; i++) skills.GetGrowthPreview(MagicElement.Fire);
        Require(skills.GetSpecializationCount(SpecializationId.FireDamage) == 0 && controller.PendingLevelUps == 1, "Preview mutated state");
        int events = 0;
        bool sawSettled = false;
        Action changed = () =>
        {
            events++;
            sawSettled = controller.PendingLevelUps == 0 && flow.State == GameFlowState.Playing &&
                skills.GetSkillLevel(MagicElement.Fire) == 2 && skills.GetSpecializationBonus(SpecializationId.FireDamage) == 10;
            Require(!controller.TryConfirmSpecialization(MagicElement.Fire, SpecializationId.FireDamage), "Event reentered commit");
        };
        skills.GrowthChanged += changed;
        Require(controller.TryConfirmSpecialization(MagicElement.Fire, SpecializationId.FireDamage), "First specialization failed");
        skills.GrowthChanged -= changed;
        Require(events == 1 && sawSettled, "GrowthChanged observed unsettled state");
        Require(before.CurrentLevel == 1 && before.Cards[0].SelectionCount == 0, "Preview was not immutable");
        Require(!controller.TryConfirmSpecialization(MagicElement.Fire, SpecializationId.FireDamage), "Duplicate confirmed without opportunity");
        Require(Mathf.Approximately(skills.ActiveMagics.First(x => x.Element == MagicElement.Fire).Damage, 6.9f), "Specialization accidentally changed combat");

        earn(); earn();
        int pending = controller.PendingLevelUps;
        Require(pending == 2, "Could not queue two level-ups");
        Action reentrant = () => Require(!controller.TryConfirmSpecialization(MagicElement.Fire, SpecializationId.FireDamage), "Reentrant commit with pending opportunity");
        skills.GrowthChanged += reentrant;
        bool runtimeReentry = false;
        Action<MagicElement, int> runtimeChanged = (_, __) =>
        {
            runtimeReentry = true;
            Require(!skills.ConfirmSelectedSkill() && !skills.TrySelectSkill(MagicElement.Fire), "Runtime callback reentered");
        };
        skills.SkillLevelChanged += runtimeChanged;
        Require(controller.TryConfirmSpecialization(MagicElement.Fire, SpecializationId.FireDamage), "Repeat specialization failed");
        skills.GrowthChanged -= reentrant;
        skills.SkillLevelChanged -= runtimeChanged;
        Require(runtimeReentry && controller.PendingLevelUps == pending - 1 && flow.State == GameFlowState.LevelUp, "Queued level-up did not settle");
        Require(skills.GetSpecializationBonus(SpecializationId.FireDamage) == 20, "Percent did not add");
        Require(controller.TryConfirmSpecialization(MagicElement.Fire, SpecializationId.FireExplosionRadius), "Second card failed");
        Require(skills.GetSpecializationBonus(SpecializationId.FireDamage) == 20 &&
            skills.GetSpecializationBonus(SpecializationId.FireExplosionRadius) == 10, "Cards shared counts");

        earn();
        Require(controller.TrySelectSkill(MagicElement.Dark) && controller.ConfirmSelectedSkill(), "Legacy unlock failed");
        Require(skills.GetSkillLevel(MagicElement.Dark) == 1 && skills.GetGrowthPreview(MagicElement.Dark).Cards.All(x => x.SelectionCount == 0), "Unlock granted specialization");
        for (int i = 0; i < 2; i++)
        {
            earn();
            Require(controller.TryConfirmSpecialization(MagicElement.Dark, SpecializationId.DarkThreeStackAmplification), "Dark specialization failed");
        }
        Require(skills.GetSpecializationBonus(SpecializationId.DarkThreeStackAmplification) == 4, "Percentage points did not add");
        earn();
        Require(controller.TrySelectSkill(MagicElement.Lightning) && controller.ConfirmSelectedSkill(), "Lightning unlock failed");
        earn();
        Require(skills.Choices.Count == 5 && skills.Choices.SequenceEqual(MagicContentCatalog.PentagonElements), "Not all eligible elements offered in order");
        Require(controller.TrySelectSkill(MagicElement.Fire) && controller.ConfirmSelectedSkill(), "Legacy owned upgrade failed");
        Require(skills.GetSpecializationCount(SpecializationId.FireDamage) == 2, "Legacy path auto-selected specialization");
        while (skills.GetSkillLevel(MagicElement.Fire) < 8)
        {
            earn();
            var id = skills.GetSkillLevel(MagicElement.Fire) == 5 ? SpecializationId.FireGroundDuration : SpecializationId.FireDamage;
            Require(controller.TryConfirmSpecialization(MagicElement.Fire, id), "Level 7 to 8 failed");
        }
        var max = skills.GetGrowthPreview(MagicElement.Fire);
        Require(max.State == ElementGrowthState.Max && !max.CanSelect && !max.NextLevel.HasValue &&
            !max.NextStats.HasValue && max.Cards.All(x => !x.CanSelect), "MAX preview invalid");
        earn();
        int finalPending = controller.PendingLevelUps;
        Require(!controller.TryConfirmSpecialization(MagicElement.Fire, SpecializationId.FireDamage) &&
            controller.PendingLevelUps == finalPending, "MAX consumed opportunity");
        Require(controller.TrySelectSkill(MagicElement.Frost) && controller.ConfirmSelectedSkill(), "Frost unlock failed");
        earn();
        Require(controller.TrySelectSkill(MagicElement.Earth) && controller.ConfirmSelectedSkill(), "Earth unlock failed");
        foreach (var definition in SpecializationCatalog.All.Where(x => x.Element != MagicElement.Fire))
        {
            earn();
            int count = skills.GetSpecializationCount(definition.Id);
            var card = skills.GetGrowthPreview(definition.Element).Cards.Single(x => x.Id == definition.Id);
            Require(controller.TryConfirmSpecialization(definition.Element, definition.Id), "Card could not confirm: " + definition.Id);
            Require(skills.GetSpecializationCount(definition.Id) == count + 1 &&
                skills.GetSpecializationBonus(definition.Id) == card.AfterSelectionBonus,
                "Committed amount differs from preview: " + definition.Id);
        }
        Require(SpecializationCatalog.All.All(x => skills.GetSpecializationCount(x.Id) > 0), "Not all fifteen cards exercised");
    }

    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Finish(bool passed, string message)
    {
        Directory.CreateDirectory("Logs/Specialization");
        File.WriteAllText("Logs/Specialization/ApiReport.txt", message);
        if (passed) Debug.Log("[Specialization API] " + message);
        else Debug.LogError("[Specialization API] " + message);
        SessionState.SetBool(Key + ".pass", passed);
        SessionState.SetBool(Key + ".done", true);
        EditorApplication.ExitPlaymode();
    }
}
