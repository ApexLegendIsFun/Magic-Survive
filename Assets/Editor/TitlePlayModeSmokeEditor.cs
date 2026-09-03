using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class TitlePlayModeSmokeEditor
{
    private const string RunningKey = "MagicSurvive.TitleSmoke.Running";
    private const string BatchKey = "MagicSurvive.TitleSmoke.Batch";
    private const string RequestedAtKey = "MagicSurvive.TitleSmoke.RequestedAt";
    private const string TitleScenePath = "Assets/01.Scripts/UI/TitleScene.unity";

    private static bool clicked;
    private static bool finishing;

    static TitlePlayModeSmokeEditor()
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            Hook();
        }
    }

    [MenuItem("Tools/Magic Survive/Run Title Play Mode Smoke")]
    public static void RunFromMenu()
    {
        Begin(false);
    }

    public static void RunBatch()
    {
        Begin(true);
    }

    private static void Begin(bool batchMode)
    {
        clicked = false;
        finishing = false;
        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(BatchKey, batchMode);
        SessionState.SetFloat(RequestedAtKey, (float)EditorApplication.timeSinceStartup);
        EditorSceneManager.OpenScene(TitleScenePath);
        Hook();
        EditorApplication.EnterPlaymode();
    }

    private static void Hook()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= HandleLog;
        Application.logMessageReceived += HandleLog;
    }

    private static void Tick()
    {
        if (finishing || !SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.timeSinceStartup -
                SessionState.GetFloat(RequestedAtKey, (float)EditorApplication.timeSinceStartup) > 30d)
            {
                Finish(1, "Title Play Mode did not start.");
            }

            return;
        }

        try
        {
            if (!clicked)
            {
                TitleSceneController controller = UnityEngine.Object.FindFirstObjectByType<TitleSceneController>();
                Button startButton = UnityEngine.Object.FindObjectsByType<Button>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .FirstOrDefault(button => button.name == "GameStart");
                Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();

                Require(controller != null, "TitleSceneController missing.");
                Require(startButton != null && startButton.interactable,
                    "GameStart button missing or disabled.");
                Require(canvas != null && canvas.transform.lossyScale.sqrMagnitude > 0.01f,
                    "Title canvas has zero scale.");
                RequireButtonCanReceivePointer(startButton);

                clicked = true;
                startButton.onClick.Invoke();
                return;
            }

            if (SceneManager.GetActiveScene().name != "SampleScene")
            {
                return;
            }

            GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            Require(flow != null && flow.State == GameFlowState.ElementSelect,
                "GameStart did not load SampleScene at ElementSelect.");
            Require(Mathf.Approximately(Time.timeScale, 0f),
                "ElementSelect must pause after title transition.");
            Finish(0, "Title button loads SampleScene and enters ElementSelect.");
        }
        catch (Exception exception)
        {
            Finish(1, exception.ToString());
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void RequireButtonCanReceivePointer(Button button)
    {
        EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        RectTransform rect = button.transform as RectTransform;
        Require(eventSystem != null, "Title EventSystem missing.");
        Require(rect != null, "GameStart RectTransform missing.");

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
            null,
            rect.TransformPoint(rect.rect.center));
        PointerEventData pointer = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };
        List<RaycastResult> hits = new List<RaycastResult>();
        eventSystem.RaycastAll(pointer, hits);

        Require(
            hits.Any(hit => hit.gameObject == button.gameObject ||
                            hit.gameObject.transform.IsChildOf(button.transform)),
            "GameStart cannot receive a pointer raycast.");
    }

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (finishing || type == LogType.Log || type == LogType.Warning ||
            stackTrace.Contains("UnityEditor.Search."))
        {
            return;
        }

        Finish(1, $"Runtime {type}: {condition}\n{stackTrace}");
    }

    private static void Finish(int exitCode, string detail)
    {
        if (finishing)
        {
            return;
        }

        finishing = true;
        SessionState.SetBool(RunningKey, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= HandleLog;

        string message = exitCode == 0
            ? $"[Title Smoke] PASS: {detail}"
            : $"[Title Smoke] FAIL: {detail}";
        if (exitCode == 0) Debug.Log(message); else Debug.LogError(message);

        if (SessionState.GetBool(BatchKey, false))
        {
            EditorApplication.Exit(exitCode);
        }
        else
        {
            EditorApplication.ExitPlaymode();
        }
    }
}
