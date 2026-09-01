using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GrayboxGameFlowView : MonoBehaviour
{
    private const int ChoiceCount = 3;

    private static readonly Color PanelColor = new Color(0.16f, 0.16f, 0.16f, 0.97f);
    private static readonly Color ButtonColor = new Color(0.34f, 0.34f, 0.34f, 1f);
    private static readonly Color ButtonHighlightColor = new Color(0.48f, 0.48f, 0.48f, 1f);

    private GameObject levelUpPanel;
    private GameObject gameOverPanel;
    private readonly Button[] choiceButtons = new Button[ChoiceCount];
    private readonly TextMeshProUGUI[] choiceLabels = new TextMeshProUGUI[ChoiceCount];
    private Button restartButton;
    private bool built;

    public event Action<int> ChoiceSelected;
    public event Action RestartRequested;

    private void Awake()
    {
        EnsureBuilt();
        HideLevelUp();
        HideGameOver();
    }

    public void ShowLevelUp(IReadOnlyList<string> choices)
    {
        EnsureBuilt();
        gameOverPanel.SetActive(false);

        for (int index = 0; index < ChoiceCount; index++)
        {
            string label = choices != null && index < choices.Count
                ? choices[index]
                : $"Upgrade {index + 1}";

            choiceLabels[index].text = label;
            choiceButtons[index].interactable = true;
        }

        levelUpPanel.SetActive(true);
        SelectForKeyboard(choiceButtons[0]);
    }

    public void HideLevelUp()
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
    }

    public void ShowGameOver()
    {
        EnsureBuilt();
        HideLevelUp();
        gameOverPanel.SetActive(true);
        SelectForKeyboard(restartButton);
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
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

        levelUpPanel = CreatePanel(canvasObject.transform, "LevelUpPanel", new Vector2(760f, 540f));
        CreateText(levelUpPanel.transform, "Title", "LEVEL UP", 42f, new Vector2(0f, 205f), new Vector2(660f, 70f));

        for (int index = 0; index < ChoiceCount; index++)
        {
            int capturedIndex = index;
            Button button = CreateButton(
                levelUpPanel.transform,
                $"Choice{index + 1}",
                new Vector2(0f, 90f - index * 105f),
                new Vector2(620f, 78f),
                out TextMeshProUGUI label);

            button.onClick.AddListener(() => ChoiceSelected?.Invoke(capturedIndex));
            choiceButtons[index] = button;
            choiceLabels[index] = label;
        }

        CreateText(
            levelUpPanel.transform,
            "PauseHint",
            "Choose an upgrade to resume combat.",
            24f,
            new Vector2(0f, -220f),
            new Vector2(660f, 50f));

        gameOverPanel = CreatePanel(canvasObject.transform, "GameOverPanel", new Vector2(640f, 340f));
        CreateText(gameOverPanel.transform, "Title", "GAME OVER", 48f, new Vector2(0f, 72f), new Vector2(540f, 80f));

        restartButton = CreateButton(
            gameOverPanel.transform,
            "RestartButton",
            new Vector2(0f, -72f),
            new Vector2(360f, 82f),
            out TextMeshProUGUI restartLabel);
        restartLabel.text = "RESTART";
        restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("GrayboxEventSystem", typeof(EventSystem));
        eventSystemObject.transform.SetParent(transform, false);

        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
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
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
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
        colors.highlightedColor = ButtonHighlightColor;
        colors.selectedColor = ButtonHighlightColor;
        colors.pressedColor = new Color(0.58f, 0.58f, 0.58f, 1f);
        button.colors = colors;

        label = CreateText(buttonObject.transform, "Label", name, 28f, Vector2.zero, size - new Vector2(40f, 12f));
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
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
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
