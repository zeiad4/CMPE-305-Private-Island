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
        private Image progressTrack;
        private Image progressFill;
        private bool hasActiveProgress;

        public bool HasActiveProgress => hasActiveProgress;

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
            hasActiveProgress = false;
            SetProgressVisible(false);
        }

        public void ShowProgress(string message, float normalizedProgress)
        {
            EnsureUI();
            promptCanvas.gameObject.SetActive(true);
            promptText.text = string.IsNullOrWhiteSpace(message) ? "Working..." : message;
            hasActiveProgress = true;
            SetProgressVisible(true);

            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01(normalizedProgress);
            }
        }

        public void HideProgress()
        {
            hasActiveProgress = false;
            SetProgressVisible(false);
        }

        public void Hide()
        {
            EnsureUI();
            hasActiveProgress = false;
            SetProgressVisible(false);
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
                rect.sizeDelta = new Vector2(760f, 72f);
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
                promptText.fontSize = 22;
                promptText.fontStyle = FontStyle.Bold;
                promptText.color = Color.white;
                promptText.text = string.Empty;
                promptText.horizontalOverflow = HorizontalWrapMode.Wrap;
                promptText.verticalOverflow = VerticalWrapMode.Overflow;
                promptText.raycastTarget = false;
            }

            if (progressTrack == null)
            {
                Transform existingTrack = promptBackground.transform.Find("ProgressTrack");
                GameObject trackObject = existingTrack != null ? existingTrack.gameObject : new GameObject("ProgressTrack");
                trackObject.transform.SetParent(promptBackground.transform, false);

                progressTrack = trackObject.GetComponent<Image>();
                if (progressTrack == null)
                {
                    progressTrack = trackObject.AddComponent<Image>();
                }

                RectTransform rect = progressTrack.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.offsetMin = new Vector2(24f, 10f);
                rect.offsetMax = new Vector2(-24f, 26f);
                progressTrack.color = new Color(1f, 1f, 1f, 0.14f);
                progressTrack.raycastTarget = false;
            }

            if (progressFill == null)
            {
                Transform existingFill = progressTrack.transform.Find("ProgressFill");
                GameObject fillObject = existingFill != null ? existingFill.gameObject : new GameObject("ProgressFill");
                fillObject.transform.SetParent(progressTrack.transform, false);

                progressFill = fillObject.GetComponent<Image>();
                if (progressFill == null)
                {
                    progressFill = fillObject.AddComponent<Image>();
                }

                RectTransform rect = progressFill.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                progressFill.type = Image.Type.Filled;
                progressFill.fillMethod = Image.FillMethod.Horizontal;
                progressFill.fillOrigin = 0;
                progressFill.color = new Color(0.94f, 0.79f, 0.28f, 0.96f);
                progressFill.raycastTarget = false;
                progressFill.fillAmount = 0f;
            }

            SetProgressVisible(hasActiveProgress);
        }

        private void SetProgressVisible(bool visible)
        {
            if (progressTrack != null)
            {
                progressTrack.gameObject.SetActive(visible);
            }
        }
    }
}
