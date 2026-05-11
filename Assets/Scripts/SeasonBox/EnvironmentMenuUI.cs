using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PrivateIsland
{
    public sealed class EnvironmentMenuUI : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private WorldEnvironmentManager worldEnvironmentManager;
        [SerializeField] private GameObject seasonBoxMenuPanel;

        [Header("Text")]
        [SerializeField] private Text currentStateText;
        [SerializeField] private Text warningText;
        [SerializeField] private Text interactionPromptText;

        [Header("Theme Images")]
        [SerializeField] private Image menuPanelImage;
        [SerializeField] private Image titleBannerImage;
        [SerializeField] private Image currentStateBackgroundImage;
        [SerializeField] private Image warningBackgroundImage;
        [SerializeField] private Image interactionPromptBackgroundImage;
        [SerializeField] private Image timeSectionBackgroundImage;
        [SerializeField] private Image seasonSectionBackgroundImage;
        [SerializeField] private Image weatherSectionBackgroundImage;

        [Header("Time Buttons")]
        [SerializeField] private Button dayButton;
        [SerializeField] private Button nightButton;

        [Header("Season Buttons")]
        [SerializeField] private Button springButton;
        [SerializeField] private Button summerButton;
        [SerializeField] private Button autumnButton;
        [SerializeField] private Button winterButton;

        [Header("Weather Buttons")]
        [SerializeField] private Button clearButton;
        [SerializeField] private Button rainButton;
        [FormerlySerializedAs("snowButton")]
        [SerializeField] private Button thunderstormButton;

        [Header("Other Buttons")]
        [SerializeField] private Button closeButton;

        [Header("Menu Behavior")]
        [SerializeField] private bool manageCursorWhenMenuOpen = true;
        [SerializeField] private bool freezePlayerWhileMenuOpen = true;
        [SerializeField] private float warningMessageDuration = 3f;

        private bool isMenuOpen;
        private bool listenersBound;
        private Coroutine warningRoutine;
        private IslandCharacterController cachedPlayerController;
        private IslandFirstPersonCamera cachedCameraController;
        private Text thunderstormButtonLabel;
        private Font cachedMenuFont;

        public bool IsMenuOpen => isMenuOpen;

        private void Awake()
        {
            EnsureProfessionalLayout();
            EnsureCenteredText();
            RefreshThunderstormButtonPresentation();
            BindButtonListeners();

            if (seasonBoxMenuPanel != null)
            {
                seasonBoxMenuPanel.SetActive(false);
            }

            if (warningText != null)
            {
                warningText.text = string.Empty;
            }

            SetInteractionPromptVisible(false);
        }

        private void OnEnable()
        {
            ResolveManagerReference();

            if (worldEnvironmentManager != null)
            {
                worldEnvironmentManager.WorldStateChanged += RefreshMenuText;
                worldEnvironmentManager.WarningRaised += ShowWarningMessage;
            }

            RefreshMenuText();
        }

        private void OnDisable()
        {
            if (worldEnvironmentManager != null)
            {
                worldEnvironmentManager.WorldStateChanged -= RefreshMenuText;
                worldEnvironmentManager.WarningRaised -= ShowWarningMessage;
            }
        }

        private void Update()
        {
            if (isMenuOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseMenu();
            }
        }

        public void OpenMenu()
        {
            ResolveManagerReference();
            if (worldEnvironmentManager == null)
            {
                Debug.LogWarning($"{nameof(EnvironmentMenuUI)} on '{name}' is missing a {nameof(WorldEnvironmentManager)} reference.", this);
                return;
            }

            isMenuOpen = true;

            if (seasonBoxMenuPanel != null)
            {
                seasonBoxMenuPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"{nameof(EnvironmentMenuUI)} on '{name}' is missing the menu panel reference.", this);
            }

            ApplyPlayerInputLock(true);
            ApplyCursorState(true);
            SetInteractionPromptVisible(false);
            ClearWarningText();
            RefreshMenuText();
        }

        public void CloseMenu()
        {
            isMenuOpen = false;

            if (seasonBoxMenuPanel != null)
            {
                seasonBoxMenuPanel.SetActive(false);
            }

            ClearWarningText();
            ApplyPlayerInputLock(false);
            ApplyCursorState(false);
        }

        public void SetInteractionPromptVisible(bool visible, string promptMessage = null)
        {
            if (interactionPromptText == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(promptMessage))
            {
                interactionPromptText.text = promptMessage;
            }

            interactionPromptText.alignment = TextAnchor.MiddleCenter;
            interactionPromptText.gameObject.SetActive(visible && !isMenuOpen);

            if (interactionPromptBackgroundImage != null)
            {
                interactionPromptBackgroundImage.gameObject.SetActive(visible && !isMenuOpen);
            }
        }

        public void ShowWarningMessage(string message)
        {
            if (warningText == null)
            {
                return;
            }

            warningText.text = message;

            if (warningBackgroundImage != null)
            {
                warningBackgroundImage.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
            }

            if (warningRoutine != null)
            {
                StopCoroutine(warningRoutine);
            }

            if (isActiveAndEnabled && warningMessageDuration > 0f)
            {
                warningRoutine = StartCoroutine(ClearWarningAfterDelay());
            }
        }

        private IEnumerator ClearWarningAfterDelay()
        {
            yield return new WaitForSeconds(warningMessageDuration);
            ClearWarningText();
        }

        private void ClearWarningText()
        {
            if (warningRoutine != null)
            {
                StopCoroutine(warningRoutine);
                warningRoutine = null;
            }

            if (warningText != null)
            {
                warningText.text = string.Empty;
            }

            if (warningBackgroundImage != null)
            {
                warningBackgroundImage.gameObject.SetActive(false);
            }
        }

        private void RefreshMenuText()
        {
            EnsureProfessionalLayout();

            if (currentStateText != null && worldEnvironmentManager != null)
            {
                currentStateText.text = worldEnvironmentManager.GetCurrentStateText();
            }

            ApplyTheme();
            RefreshThunderstormButtonPresentation();
        }

        private void ApplyTheme()
        {
            if (worldEnvironmentManager == null)
            {
                return;
            }

            Color seasonColor = GetSeasonColor(worldEnvironmentManager.CurrentSeason);
            Color seasonSoft = Color.Lerp(seasonColor, Color.white, 0.48f);
            Color seasonDeep = Color.Lerp(seasonColor, new Color(0.08f, 0.1f, 0.16f), 0.58f);
            bool isNight = worldEnvironmentManager.CurrentTime == TimeOfDay.Night;
            bool thunderstormAvailable = isNight;

            SetImageColor(menuPanelImage, WithAlpha(Color.Lerp(seasonDeep, Color.black, isNight ? 0.34f : 0.08f), 0.965f));
            SetImageColor(titleBannerImage, WithAlpha(Color.Lerp(seasonColor, new Color(0.96f, 0.87f, 0.74f), 0.28f), 1f));
            SetImageColor(currentStateBackgroundImage, WithAlpha(Color.Lerp(new Color(0.94f, 0.95f, 0.98f), seasonSoft, 0.2f), 0.97f));
            SetImageColor(warningBackgroundImage, new Color(0.74f, 0.28f, 0.18f, 0.95f));
            SetImageColor(interactionPromptBackgroundImage, WithAlpha(Color.Lerp(seasonDeep, Color.black, 0.22f), 0.92f));
            SetImageColor(timeSectionBackgroundImage, WithAlpha(Color.Lerp(seasonSoft, new Color(0.12f, 0.16f, 0.22f), isNight ? 0.62f : 0.42f), 0.28f));
            SetImageColor(seasonSectionBackgroundImage, WithAlpha(Color.Lerp(seasonSoft, new Color(0.12f, 0.16f, 0.22f), isNight ? 0.58f : 0.38f), 0.28f));
            SetImageColor(weatherSectionBackgroundImage, WithAlpha(Color.Lerp(seasonSoft, new Color(0.12f, 0.16f, 0.22f), isNight ? 0.6f : 0.4f), 0.28f));

            SetTextColor(currentStateText, new Color(0.12f, 0.15f, 0.2f));
            SetTextColor(warningText, Color.white);
            SetTextColor(interactionPromptText, Color.white);

            ApplyButtonStyle(dayButton, new Color(0.91f, 0.73f, 0.28f), worldEnvironmentManager.CurrentTime == TimeOfDay.Day);
            ApplyButtonStyle(nightButton, new Color(0.23f, 0.34f, 0.66f), worldEnvironmentManager.CurrentTime == TimeOfDay.Night, Color.white);

            ApplyButtonStyle(springButton, GetSeasonColor(Season.Spring), worldEnvironmentManager.CurrentSeason == Season.Spring);
            ApplyButtonStyle(summerButton, GetSeasonColor(Season.Summer), worldEnvironmentManager.CurrentSeason == Season.Summer);
            ApplyButtonStyle(autumnButton, GetSeasonColor(Season.Autumn), worldEnvironmentManager.CurrentSeason == Season.Autumn, Color.white);
            ApplyButtonStyle(winterButton, GetSeasonColor(Season.Winter), worldEnvironmentManager.CurrentSeason == Season.Winter);

            ApplyButtonStyle(clearButton, new Color(0.86f, 0.92f, 0.98f), worldEnvironmentManager.CurrentWeather == WeatherType.Clear);
            ApplyButtonStyle(rainButton, GetRainThemeColor(isNight), worldEnvironmentManager.CurrentWeather == WeatherType.Rain, Color.white);
            ApplyButtonStyle(thunderstormButton, GetThunderstormThemeColor(isNight), worldEnvironmentManager.CurrentWeather == WeatherType.Thunderstorm, Color.white, thunderstormAvailable);
            ApplyButtonStyle(closeButton, new Color(0.22f, 0.15f, 0.18f), false, Color.white);
        }

        private void EnsureProfessionalLayout()
        {
            cachedMenuFont ??= ResolveBuiltinFont();

            ApplyPanelLayout();
            ApplyTextStyling();
            ApplyButtonLayout(dayButton, new Vector2(-135f, -286f), new Vector2(230f, 58f), 24);
            ApplyButtonLayout(nightButton, new Vector2(135f, -286f), new Vector2(230f, 58f), 24);

            ApplyButtonLayout(springButton, new Vector2(-258f, -418f), new Vector2(152f, 54f), 23);
            ApplyButtonLayout(summerButton, new Vector2(-86f, -418f), new Vector2(152f, 54f), 23);
            ApplyButtonLayout(autumnButton, new Vector2(86f, -418f), new Vector2(152f, 54f), 23);
            ApplyButtonLayout(winterButton, new Vector2(258f, -418f), new Vector2(152f, 54f), 23);

            ApplyButtonLayout(clearButton, new Vector2(-210f, -560f), new Vector2(178f, 54f), 23);
            ApplyButtonLayout(rainButton, new Vector2(0f, -560f), new Vector2(178f, 54f), 23);
            ApplyButtonLayout(thunderstormButton, new Vector2(210f, -560f), new Vector2(220f, 54f), 21);
            ApplyButtonLayout(closeButton, new Vector2(0f, -649f), new Vector2(250f, 48f), 22);
        }

        private void ApplyPanelLayout()
        {
            if (seasonBoxMenuPanel == null)
            {
                return;
            }

            RectTransform panelRect = seasonBoxMenuPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.sizeDelta = new Vector2(860f, 720f);
            }

            SetRect(titleBannerImage, new Vector2(0f, -26f), new Vector2(710f, 72f));
            SetRect(currentStateBackgroundImage, new Vector2(0f, -118f), new Vector2(690f, 54f));
            SetRect(warningBackgroundImage, new Vector2(0f, -174f), new Vector2(690f, 40f));
            SetRect(timeSectionBackgroundImage, new Vector2(0f, -302f), new Vector2(720f, 114f));
            SetRect(seasonSectionBackgroundImage, new Vector2(0f, -435f), new Vector2(720f, 126f));
            SetRect(weatherSectionBackgroundImage, new Vector2(0f, -577f), new Vector2(720f, 124f));

            SetRect(currentStateText, new Vector2(0f, -123f), new Vector2(650f, 40f));
            SetRect(warningText, new Vector2(0f, -179f), new Vector2(620f, 28f));

            SetNamedRect("TitleText", new Vector2(0f, -36f), new Vector2(670f, 48f));
            SetNamedRect("TimeLabel", new Vector2(0f, -233f), new Vector2(280f, 32f));
            SetNamedRect("SeasonLabel", new Vector2(0f, -367f), new Vector2(280f, 32f));
            SetNamedRect("WeatherLabel", new Vector2(0f, -509f), new Vector2(280f, 32f));

            SetPromptLayout();
            EnsurePanelShadow(menuPanelImage, new Color(0f, 0f, 0f, 0.34f), new Vector2(0f, -12f));
            EnsurePanelShadow(titleBannerImage, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -4f));
        }

        private void SetPromptLayout()
        {
            if (interactionPromptBackgroundImage != null)
            {
                RectTransform promptBackdropRect = interactionPromptBackgroundImage.GetComponent<RectTransform>();
                if (promptBackdropRect != null)
                {
                    promptBackdropRect.sizeDelta = new Vector2(840f, 72f);
                    promptBackdropRect.anchoredPosition = new Vector2(0f, 28f);
                }
            }

            if (interactionPromptText != null)
            {
                RectTransform promptRect = interactionPromptText.GetComponent<RectTransform>();
                if (promptRect != null)
                {
                    promptRect.sizeDelta = new Vector2(790f, 46f);
                    promptRect.anchoredPosition = new Vector2(0f, 38f);
                }
            }
        }

        private void ApplyTextStyling()
        {
            ApplyTextPreset(currentStateText, 26, FontStyle.Bold);
            ApplyTextPreset(warningText, 20, FontStyle.Bold);
            ApplyTextPreset(interactionPromptText, 28, FontStyle.Bold);
            ApplyTextPreset(FindNamedText("TitleText"), 40, FontStyle.Bold);
            ApplyTextPreset(FindNamedText("TimeLabel"), 23, FontStyle.Bold);
            ApplyTextPreset(FindNamedText("SeasonLabel"), 23, FontStyle.Bold);
            ApplyTextPreset(FindNamedText("WeatherLabel"), 23, FontStyle.Bold);
        }

        private void ApplyButtonLayout(Button button, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }

            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                EnsurePanelShadow(image, new Color(0f, 0f, 0f, 0.16f), new Vector2(0f, -4f));
            }

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                ApplyTextPreset(label, fontSize, FontStyle.Bold);
            }
        }

        private void RefreshThunderstormButtonPresentation()
        {
            if (thunderstormButton == null)
            {
                return;
            }

            RectTransform rect = thunderstormButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(190f, rect.sizeDelta.y <= 0f ? 50f : rect.sizeDelta.y);
            }

            thunderstormButtonLabel ??= thunderstormButton.GetComponentInChildren<Text>(true);
            if (thunderstormButtonLabel != null)
            {
                bool thunderstormAvailable = worldEnvironmentManager == null || worldEnvironmentManager.CurrentTime == TimeOfDay.Night;
                thunderstormButtonLabel.text = thunderstormAvailable ? "Thunderstorm" : "Thunderstorm (Night)";
                thunderstormButtonLabel.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void EnsureCenteredText()
        {
            CenterText(currentStateText);
            CenterText(warningText);
            CenterText(interactionPromptText);
            CenterButtonLabel(dayButton);
            CenterButtonLabel(nightButton);
            CenterButtonLabel(springButton);
            CenterButtonLabel(summerButton);
            CenterButtonLabel(autumnButton);
            CenterButtonLabel(winterButton);
            CenterButtonLabel(clearButton);
            CenterButtonLabel(rainButton);
            CenterButtonLabel(thunderstormButton);
            CenterButtonLabel(closeButton);
        }

        private void CenterText(Text targetText)
        {
            if (targetText != null)
            {
                targetText.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void CenterButtonLabel(Button button)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void BindButtonListeners()
        {
            if (listenersBound)
            {
                return;
            }

            AddListener(dayButton, HandleDayClicked);
            AddListener(nightButton, HandleNightClicked);
            AddListener(springButton, HandleSpringClicked);
            AddListener(summerButton, HandleSummerClicked);
            AddListener(autumnButton, HandleAutumnClicked);
            AddListener(winterButton, HandleWinterClicked);
            AddListener(clearButton, HandleClearClicked);
            AddListener(rainButton, HandleRainClicked);
            AddListener(thunderstormButton, HandleThunderstormClicked);
            AddListener(closeButton, CloseMenu);

            listenersBound = true;
        }

        private void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(action);
        }

        private void HandleDayClicked()
        {
            if (ResolveManagerReference())
            {
                worldEnvironmentManager.SetDay();
            }
        }

        private void HandleNightClicked()
        {
            if (ResolveManagerReference())
            {
                worldEnvironmentManager.SetNight();
            }
        }

        private void HandleSpringClicked()
        {
            if (ResolveManagerReference())
            {
                worldEnvironmentManager.SetSpring();
            }
        }

        private void HandleSummerClicked()
        {
            if (ResolveManagerReference())
            {
                worldEnvironmentManager.SetSummer();
            }
        }

        private void HandleAutumnClicked()
        {
            if (ResolveManagerReference())
            {
                worldEnvironmentManager.SetAutumn();
            }
        }

        private void HandleWinterClicked()
        {
            if (ResolveManagerReference())
            {
                worldEnvironmentManager.SetWinter();
            }
        }

        private void HandleClearClicked()
        {
            if (ResolveManagerReference())
            {
                worldEnvironmentManager.SetClearWeather();
            }
        }

        private void HandleRainClicked()
        {
            if (ResolveManagerReference())
            {
                worldEnvironmentManager.SetRainWeather();
            }
        }

        private void HandleThunderstormClicked()
        {
            if (ResolveManagerReference())
            {
                worldEnvironmentManager.SetThunderstormWeather();
            }
        }

        private bool ResolveManagerReference()
        {
            if (worldEnvironmentManager == null)
            {
                worldEnvironmentManager = FindAnyObjectByType<WorldEnvironmentManager>();
            }

            return worldEnvironmentManager != null;
        }

        private void ApplyPlayerInputLock(bool shouldLock)
        {
            cachedPlayerController ??= FindAnyObjectByType<IslandCharacterController>();
            cachedCameraController ??= FindAnyObjectByType<IslandFirstPersonCamera>();

            if (cachedCameraController != null)
            {
                cachedCameraController.SetInputSuspended(shouldLock);
            }

            if (!freezePlayerWhileMenuOpen)
            {
                return;
            }

            if (cachedPlayerController != null)
            {
                cachedPlayerController.SetInputEnabled(!shouldLock);
            }
        }

        private void ApplyCursorState(bool menuOpen)
        {
            if (!Application.isPlaying || !manageCursorWhenMenuOpen)
            {
                return;
            }

            Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = menuOpen;
        }

        private void ApplyButtonStyle(Button button, Color baseColor, bool isSelected, Color? selectedTextColor = null, bool isInteractable = true)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = isInteractable;

            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                Color idleColor = Color.Lerp(baseColor, new Color(0.96f, 0.96f, 0.98f), 0.22f);
                image.color = isInteractable
                    ? (isSelected ? baseColor : idleColor)
                    : Color.Lerp(idleColor, new Color(0.38f, 0.39f, 0.42f), 0.5f);
            }

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.alignment = TextAnchor.MiddleCenter;
                label.color = isInteractable
                    ? (isSelected ? (selectedTextColor ?? Color.black) : new Color(0.16f, 0.18f, 0.22f))
                    : new Color(0.2f, 0.2f, 0.2f, 0.75f);
                label.fontStyle = isInteractable && isSelected ? FontStyle.Bold : FontStyle.Normal;
            }

            button.transform.localScale = isInteractable && isSelected ? new Vector3(1.03f, 1.03f, 1f) : Vector3.one;
        }

        private void ApplyTextPreset(Text text, int fontSize, FontStyle fontStyle)
        {
            if (text == null)
            {
                return;
            }

            if (cachedMenuFont != null)
            {
                text.font = cachedMenuFont;
            }

            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
        }

        private Text FindNamedText(string objectName)
        {
            if (seasonBoxMenuPanel == null)
            {
                return null;
            }

            Transform child = seasonBoxMenuPanel.transform.Find(objectName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private void SetNamedRect(string objectName, Vector2 anchoredPosition, Vector2 size)
        {
            if (seasonBoxMenuPanel == null)
            {
                return;
            }

            Transform child = seasonBoxMenuPanel.transform.Find(objectName);
            RectTransform rect = child != null ? child.GetComponent<RectTransform>() : null;
            if (rect != null)
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }
        }

        private static void SetRect(Graphic graphic, Vector2 anchoredPosition, Vector2 size)
        {
            if (graphic == null)
            {
                return;
            }

            RectTransform rect = graphic.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }
        }

        private static void EnsurePanelShadow(Graphic graphic, Color color, Vector2 distance)
        {
            if (graphic == null)
            {
                return;
            }

            Shadow shadow = graphic.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = graphic.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static Font ResolveBuiltinFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void SetImageColor(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private void SetTextColor(Text text, Color color)
        {
            if (text != null)
            {
                text.color = color;
            }
        }

        private static Color GetSeasonColor(Season season)
        {
            return season switch
            {
                Season.Spring => new Color(0.52f, 0.86f, 0.47f),
                Season.Summer => new Color(0.96f, 0.77f, 0.29f),
                Season.Autumn => new Color(0.83f, 0.43f, 0.16f),
                Season.Winter => new Color(0.9f, 0.96f, 1f),
                _ => Color.white
            };
        }

        private static Color GetRainThemeColor(bool isNight)
        {
            return isNight ? new Color(0.28f, 0.39f, 0.63f) : new Color(0.36f, 0.64f, 0.93f);
        }

        private static Color GetThunderstormThemeColor(bool isNight)
        {
            return isNight ? new Color(0.23f, 0.29f, 0.42f) : new Color(0.3f, 0.36f, 0.5f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
