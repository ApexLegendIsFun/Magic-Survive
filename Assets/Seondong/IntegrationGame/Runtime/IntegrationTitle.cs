using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Seondong.IntegrationGame
{
    [RequireComponent(typeof(TitleSceneController))]
    public sealed class IntegrationTitle : MonoBehaviour
    {
        private void Awake()
        {
            Time.timeScale = 1f;
            var canvasObject = new GameObject("IntegrationTitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("IntegrationEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            Label(canvasObject.transform, "MAGIC SURVIVE", 70, 250, new Vector2(1100, 110));
            Label(canvasObject.transform, "SEONDONG / INTEGRATION PLAYTEST", 26, 165, new Vector2(1100, 60));
            var start = Button(canvasObject.transform, "START GAME", 30);
            start.onClick.AddListener(GetComponent<TitleSceneController>().StartGame);
            var quit = Button(canvasObject.transform, "QUIT", -80);
            quit.onClick.AddListener(Quit);
            Label(canvasObject.transform, "WASD / Arrow keys: Move    |    Mouse: Select / Confirm\nAutomatic attack. Survive, level up, defeat the boss.", 25, -230, new Vector2(1200, 100));
            EventSystem.current.SetSelectedGameObject(start.gameObject);
            Debug.Log("[Integration Run] Title ready. Real input required.");
        }

        private static void Quit()
        {
            Debug.Log("[Integration Run] Quit requested by title button.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static Button Button(Transform parent, string text, float y)
        {
            var obj = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(440, 88);
            rect.anchoredPosition = new Vector2(0, y);
            obj.GetComponent<Image>().color = new Color(.15f, .3f, .46f);
            var button = obj.GetComponent<Button>();
            button.targetGraphic = obj.GetComponent<Image>();
            Label(obj.transform, text, 32, 0, new Vector2(420, 80));
            return button;
        }

        private static void Label(Transform parent, string text, float size, float y, Vector2 dimensions)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = dimensions;
            rect.anchoredPosition = new Vector2(0, y);
            var label = obj.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }
    }
}
