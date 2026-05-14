using UnityEngine;
using UnityEngine.UI;

namespace PrivateIsland
{
    public sealed class IslandInteractionPromptUI : MonoBehaviour
    {
        private static IslandInteractionPromptUI instance;

        private Canvas promptCanvas;
        private Image promptBackground;
        private Text promptText;

        public static IslandInteractionPromptUI GetOrCreate()
        {
            if (instance != null)
            {
                instance.EnsureUI();
                return instance;
            }

            instance = FindAnyObjectByType<IslandInteractionPromptUI>(FindObjectsInactive.Include);
            if (instance != null)
            {
                instance.EnsureUI();
                return instance;
            }

            GameObject root = new GameObject("Island Interaction Prompt");
            instance = root.AddComponent<IslandInteractionPromptUI>();
            instance.EnsureUI();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureUI();
            Hide();
        }

        public void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Hide();
                return;
            }

            EnsureUI();
            promptCanvas.gameObject.SetActive(true);
            promptText.text = message;
        }

        public void Hide()
        {
            EnsureUI();
            promptCanvas.gameObject.SetActive(false);
        }

        private void EnsureUI()
        {
            promptCanvas ??= GetComponent<Canvas>();
            if (promptCanvas == null)
            {
                promptCanvas = gameObject.AddComponent<Canvas>();
                promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                promptCanvas.sortingOrder = 90;
            }

            if (GetComponent<CanvasScaler>() == null)
            {
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (promptBackground == null)
            {
                Transform existingBackground = transform.Find("PromptBackground");
                GameObject backgroundObject = existingBackground != null ? existingBackground.gameObject : new GameObject("PromptBackground");
                backgroundObject.transform.SetParent(transform, false);

                promptBackground = backgroundObject.GetComponent<Image>();
                if (promptBackground == null)
                {
                    promptBackground = backgroundObject.AddComponent<Image>();
                }

                RectTransform rect = promptBackground.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 56f);
                rect.sizeDelta = new Vector2(560f, 58f);
                promptBackground.color = new Color(0.08f, 0.11f, 0.14f, 0.88f);
                promptBackground.raycastTarget = false;
            }

            if (promptText == null)
            {
                Transform existingText = promptBackground.transform.Find("PromptText");
                GameObject textObject = existingText != null ? existingText.gameObject : new GameObject("PromptText");
                textObject.transform.SetParent(promptBackground.transform, false);

                promptText = textObject.GetComponent<Text>();
                if (promptText == null)
                {
                    promptText = textObject.AddComponent<Text>();
                }

                RectTransform rect = promptText.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(18f, 8f);
                rect.offsetMax = new Vector2(-18f, -8f);
                promptText.alignment = TextAnchor.MiddleCenter;
                promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                promptText.fontSize = 24;
                promptText.fontStyle = FontStyle.Bold;
                promptText.color = Color.white;
                promptText.text = string.Empty;
                promptText.raycastTarget = false;
            }
        }
    }
}
