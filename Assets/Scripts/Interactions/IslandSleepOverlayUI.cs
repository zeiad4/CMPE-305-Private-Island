using UnityEngine;
using UnityEngine.UI;

namespace PrivateIsland
{
    public sealed class IslandSleepOverlayUI : MonoBehaviour
    {
        private static IslandSleepOverlayUI instance;

        private Canvas overlayCanvas;
        private Image blackoutImage;
        private Text statusText;

        public static IslandSleepOverlayUI GetOrCreate()
        {
            if (instance != null)
            {
                instance.EnsureUI();
                return instance;
            }

            instance = FindAnyObjectByType<IslandSleepOverlayUI>(FindObjectsInactive.Include);
            if (instance != null)
            {
                instance.EnsureUI();
                return instance;
            }

            GameObject root = new GameObject("Island Sleep Overlay");
            instance = root.AddComponent<IslandSleepOverlayUI>();
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
            HideImmediate();
        }

        public void Show(float alpha, string message)
        {
            EnsureUI();
            overlayCanvas.gameObject.SetActive(true);
            blackoutImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
            statusText.text = message ?? string.Empty;
            statusText.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }

        public void HideImmediate()
        {
            EnsureUI();
            blackoutImage.color = new Color(0f, 0f, 0f, 0f);
            statusText.text = string.Empty;
            statusText.color = new Color(1f, 1f, 1f, 0f);
            overlayCanvas.gameObject.SetActive(false);
        }

        private void EnsureUI()
        {
            overlayCanvas ??= GetComponent<Canvas>();
            if (overlayCanvas == null)
            {
                overlayCanvas = gameObject.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.sortingOrder = 220;
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

            if (blackoutImage == null)
            {
                GameObject imageObject = new GameObject("Blackout", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(transform, false);
                blackoutImage = imageObject.GetComponent<Image>();
                RectTransform rect = blackoutImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                blackoutImage.color = new Color(0f, 0f, 0f, 0f);
                blackoutImage.raycastTarget = false;
            }

            if (statusText == null)
            {
                GameObject textObject = new GameObject("StatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textObject.transform.SetParent(transform, false);
                statusText = textObject.GetComponent<Text>();
                RectTransform rect = statusText.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(900f, 120f);
                rect.anchoredPosition = new Vector2(0f, 0f);
                statusText.alignment = TextAnchor.MiddleCenter;
                statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                statusText.fontSize = 34;
                statusText.fontStyle = FontStyle.Bold;
                statusText.color = new Color(1f, 1f, 1f, 0f);
                statusText.text = string.Empty;
                statusText.raycastTarget = false;
            }
        }
    }
}
