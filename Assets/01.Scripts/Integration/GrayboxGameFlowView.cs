using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 승범 최종 UI가 연결되기 전 규칙 검증에만 쓰는 회색 폴백 화면입니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class GrayboxGameFlowView : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.16f, 0.16f, 0.16f, 0.97f);
    private static readonly Color ButtonColor = new Color(0.34f, 0.34f, 0.34f, 1f);
    private static readonly Color HighlightColor = new Color(0.48f, 0.48f, 0.48f, 1f);
    private static readonly Color PendingColor = new Color(0.28f, 0.52f, 0.72f, 1f);
    private static readonly Color OwnedColor = new Color(0.22f, 0.45f, 0.27f, 1f);
    private static readonly Color LockedColor = new Color(0.20f, 0.20f, 0.20f, 1f);

    private readonly Dictionary<SkillTreeNodeId, Button> nodeButtons =
        new Dictionary<SkillTreeNodeId, Button>(28);
    private readonly Dictionary<SkillTreeNodeId, TextMeshProUGUI> nodeLabels =
        new Dictionary<SkillTreeNodeId, TextMeshProUGUI>(28);

    private GameObject elementPanel;
    private GameObject skillTreePanel;
    private GameObject resultPanel;
    private Button confirmButton;
    private Button restartButton;
    private Button titleButton;
    private TextMeshProUGUI resultTitle;
    private TextMeshProUGUI resultBody;
    private PlayerSkillSystem displayedSkillSystem;
    private bool built;

    public event Action<MagicElement> StartingElementSelected;
    public event Action<SkillTreeNodeId> NodeSelected;
    public event Action ConfirmRequested;
    public event Action RestartRequested;
    public event Action TitleRequested;

    private void Awake()
    {
        EnsureBuilt();
        HideElementSelect();
        HideLevelUp();
        HideResult();
    }

    public void ShowElementSelect(IReadOnlyList<MagicElement> elements)
    {
        EnsureBuilt();
        HideLevelUp();
        HideResult();
        elementPanel.SetActive(true);

        Button first = elementPanel.GetComponentInChildren<Button>(true);
        SelectForKeyboard(first);
    }

    public void HideElementSelect()
    {
        if (elementPanel != null)
        {
            elementPanel.SetActive(false);
        }
    }

    public void ShowSkillTree(PlayerSkillSystem skillSystem)
    {
        EnsureBuilt();
        displayedSkillSystem = skillSystem;
        HideElementSelect();
        HideResult();
        skillTreePanel.SetActive(true);
        RefreshSkillTree(skillSystem);
    }

    public void RefreshSkillTree(PlayerSkillSystem skillSystem)
    {
        if (skillSystem == null)
        {
            return;
        }

        EnsureBuilt();
        displayedSkillSystem = skillSystem;

        IReadOnlyList<SkillTreeNodeDefinition> definitions = skillSystem.GetTreeDefinitions();
        SkillTreeNodeId? pending = skillSystem.Tree.PendingSelection;
        Button firstAvailable = null;

        for (int index = 0; index < definitions.Count; index++)
        {
            SkillTreeNodeDefinition definition = definitions[index];
            Button button = nodeButtons[definition.Id];
            SkillTreeNodePreview preview = skillSystem.GetNodePreview(definition.Id);
            SkillTreeNodeState state = preview.State;
            bool hidden = state == SkillTreeNodeState.Hidden;

            button.gameObject.SetActive(!hidden);
            if (hidden)
            {
                continue;
            }

            bool selected = pending.HasValue && pending.Value == definition.Id;
            button.interactable = state == SkillTreeNodeState.Available;
            button.image.color = selected
                ? PendingColor
                : GetNodeColor(state);
            nodeLabels[definition.Id].text =
                $"{definition.DisplayName}\n<size=16>{definition.Description}\n" +
                $"{preview.CurrentValue} → {preview.AppliedValue}</size>";

            if (firstAvailable == null && button.interactable)
            {
                firstAvailable = button;
            }
        }

        confirmButton.interactable = pending.HasValue;
        if (firstAvailable != null && EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == null)
        {
            SelectForKeyboard(firstAvailable);
        }
    }

    public void HideLevelUp()
    {
        displayedSkillSystem = null;
        if (skillTreePanel != null)
        {
            skillTreePanel.SetActive(false);
        }
    }

    public void ShowResult(RunResult result)
    {
        EnsureBuilt();
        HideElementSelect();
        HideLevelUp();
        resultPanel.SetActive(true);

        if (result == null)
        {
            resultTitle.text = "DEFEAT";
            resultBody.text = string.Empty;
        }
        else
        {
            resultTitle.text = result.IsVictory
                ? "VICTORY"
                : result.Outcome == RunOutcome.Timeout ? "TIME OUT" : "DEFEAT";
            resultBody.text = BuildResultText(result);
        }

        SelectForKeyboard(restartButton);
    }

    public void ShowGameOver()
    {
        ShowResult(null);
    }

    public void HideGameOver()
    {
        HideResult();
    }

    public void HideResult()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void EnsureBuilt()
    {
        if (built)
        {
            return;
        }

        built = true;
        EnsureEventSystem();

        GameObject canvasObject = new GameObject(
            "GrayboxGameFlowCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildElementPanel(canvasObject.transform);
        BuildSkillTreePanel(canvasObject.transform);
        BuildResultPanel(canvasObject.transform);
    }

    private void BuildElementPanel(Transform parent)
    {
        elementPanel = CreatePanel(parent, "ElementSelectPanel", new Vector2(1380f, 420f));
        CreateText(
            elementPanel.transform,
            "Title",
            "CHOOSE STARTING ELEMENT",
            42f,
            new Vector2(0f, 130f),
            new Vector2(1200f, 70f));

        IReadOnlyList<MagicElement> elements = SkillTreeCatalog.PentagonElements;
        for (int index = 0; index < elements.Count; index++)
        {
            MagicElement element = elements[index];
            Button button = CreateButton(
                elementPanel.transform,
                $"Element_{element}",
                new Vector2(-480f + index * 240f, -35f),
                new Vector2(210f, 100f),
                out TextMeshProUGUI label);
            label.text = element.ToString().ToUpperInvariant();
            button.onClick.AddListener(() => StartingElementSelected?.Invoke(element));
        }
    }

    private void BuildSkillTreePanel(Transform parent)
    {
        skillTreePanel = CreatePanel(parent, "SkillTreePanel", new Vector2(1640f, 980f));
        CreateText(
            skillTreePanel.transform,
            "Title",
            "SKILL TREE — SELECT ONE NODE",
            36f,
            new Vector2(0f, 435f),
            new Vector2(1450f, 60f));

        IReadOnlyList<SkillTreeNodeDefinition> definitions = SkillTreeCatalog.Nodes;
        for (int index = 0; index < definitions.Count; index++)
        {
            SkillTreeNodeDefinition definition = definitions[index];
            int column = index % 5;
            int row = index / 5;
            Button button = CreateButton(
                skillTreePanel.transform,
                $"Node_{definition.Id}",
                new Vector2(-580f + column * 290f, 335f - row * 125f),
                new Vector2(270f, 105f),
                out TextMeshProUGUI label);

            SkillTreeNodeId capturedId = definition.Id;
            button.onClick.AddListener(() => NodeSelected?.Invoke(capturedId));
            nodeButtons.Add(definition.Id, button);
            nodeLabels.Add(definition.Id, label);
        }

        confirmButton = CreateButton(
            skillTreePanel.transform,
            "ConfirmButton",
            new Vector2(0f, -425f),
            new Vector2(420f, 72f),
            out TextMeshProUGUI confirmLabel);
        confirmLabel.text = "CONFIRM";
        confirmButton.onClick.AddListener(() => ConfirmRequested?.Invoke());
    }

    private void BuildResultPanel(Transform parent)
    {
        resultPanel = CreatePanel(parent, "ResultPanel", new Vector2(760f, 620f));
        resultTitle = CreateText(
            resultPanel.transform,
            "Title",
            "RESULT",
            50f,
            new Vector2(0f, 230f),
            new Vector2(650f, 80f));
        resultBody = CreateText(
            resultPanel.transform,
            "Body",
            string.Empty,
            28f,
            new Vector2(0f, 40f),
            new Vector2(650f, 260f));

        restartButton = CreateButton(
            resultPanel.transform,
            "RestartButton",
            new Vector2(-190f, -225f),
            new Vector2(300f, 78f),
            out TextMeshProUGUI restartLabel);
        restartLabel.text = "RESTART";
        restartButton.onClick.AddListener(() => RestartRequested?.Invoke());

        titleButton = CreateButton(
            resultPanel.transform,
            "TitleButton",
            new Vector2(190f, -225f),
            new Vector2(300f, 78f),
            out TextMeshProUGUI titleLabel);
        titleLabel.text = "TITLE";
        titleButton.onClick.AddListener(() => TitleRequested?.Invoke());
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("GrayboxEventSystem", typeof(EventSystem));
        eventSystemObject.transform.SetParent(transform, false);
        InputSystemUIInputModule module = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    private static string BuildResultText(RunResult result)
    {
        StringBuilder builder = new StringBuilder(192);
        int totalSeconds = Mathf.FloorToInt(result.CombatTime);
        builder.Append("TIME  ")
            .Append(totalSeconds / 60)
            .Append(':')
            .Append((totalSeconds % 60).ToString("00"))
            .Append("\nKILLS  ")
            .Append(result.KillCount)
            .Append("\nLEVEL  ")
            .Append(result.Level)
            .Append("\nELEMENTS  ")
            .Append(Join(result.Elements))
            .Append("\nFUSIONS  ")
            .Append(Join(result.Fusions));
        return builder.ToString();
    }

    private static string Join<T>(IReadOnlyList<T> values)
    {
        if (values == null || values.Count == 0)
        {
            return "-";
        }

        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < values.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(values[index]);
        }

        return builder.ToString();
    }

    private static Color GetNodeColor(SkillTreeNodeState state)
    {
        switch (state)
        {
            case SkillTreeNodeState.Owned:
                return OwnedColor;
            case SkillTreeNodeState.Locked:
                return LockedColor;
            default:
                return ButtonColor;
        }
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = PanelColor;
        return panel;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        out TextMeshProUGUI label)
    {
        GameObject buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = ButtonColor;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = HighlightColor;
        colors.selectedColor = HighlightColor;
        colors.pressedColor = PendingColor;
        button.colors = colors;

        label = CreateText(
            buttonObject.transform,
            "Label",
            name,
            23f,
            Vector2.zero,
            size - new Vector2(16f, 8f));
        return button;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string text,
        float fontSize,
        Vector2 position,
        Vector2 size)
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static void SelectForKeyboard(Selectable selectable)
    {
        if (EventSystem.current != null && selectable != null)
        {
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
