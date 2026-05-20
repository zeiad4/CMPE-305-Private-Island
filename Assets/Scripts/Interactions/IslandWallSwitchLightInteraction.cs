using System.Collections;
using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandWallSwitchLightInteraction : IslandInteractable
    {
        [SerializeField] private Light targetLight;
        [SerializeField] private Transform switchLever;
        [SerializeField] private Renderer switchRenderer;
        [SerializeField] private Renderer[] roomRenderers;
        [SerializeField] private Color[] lightsOnColors;
        [SerializeField] private Color[] lightsOffColors;
        [SerializeField] private float offIntensity;
        [SerializeField] private float onIntensity = 6.4f;
        [SerializeField] private Color switchOffTint = new Color(0.33f, 0.3f, 0.27f);
        [SerializeField] private Color switchOnTint = new Color(0.89f, 0.8f, 0.56f);

        private bool isOn;
        private bool interactionRunning;
        private Quaternion leverOffRotation;
        private Quaternion leverOnRotation;

        public void Configure(
            Light lightSource,
            Transform lever,
            Renderer wallSwitchRenderer,
            Renderer[] affectedRoomRenderers,
            Color[] onColors,
            Color[] offColors,
            bool startOn,
            float interactionRadius,
            float focusHeight)
        {
            targetLight = lightSource;
            switchLever = lever;
            switchRenderer = wallSwitchRenderer;
            roomRenderers = affectedRoomRenderers;
            lightsOnColors = onColors;
            lightsOffColors = offColors;
            isOn = startOn;

            leverOffRotation = Quaternion.Euler(-28f, 0f, 0f);
            leverOnRotation = Quaternion.Euler(28f, 0f, 0f);

            SetInteractionPrompt("Press E to use the light switch");
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(focusHeight);
            ApplyVisualState();
        }

        public override bool SupportsInteractionKey(KeyCode key)
        {
            return key == KeyCode.E;
        }

        public override bool CanInteract(Transform interactor)
        {
            return !interactionRunning && interactor != null && targetLight != null && switchLever != null;
        }

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            StartCoroutine(ToggleRoutine(interactor));
        }

        private IEnumerator ToggleRoutine(Transform interactor)
        {
            interactionRunning = true;

            IslandCharacterController controller = interactor.GetComponent<IslandCharacterController>() ?? interactor.GetComponentInParent<IslandCharacterController>();
            IslandFirstPersonCamera firstPersonCamera = Camera.main != null
                ? Camera.main.GetComponent<IslandFirstPersonCamera>()
                : null;
            IslandInteractionPromptUI promptUI = IslandInteractionPromptUI.GetOrCreate();

            controller?.SetInputEnabled(false);
            firstPersonCamera?.SetInputSuspended(true);

            Quaternion startRotation = switchLever.localRotation;
            Quaternion midRotation = isOn ? leverOffRotation : leverOnRotation;

            float duration = 0.24f;
            float elapsed = 0f;
            bool toggled = false;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                promptUI.ShowProgress("Using light switch...", progress);

                if (!toggled && progress >= 0.5f)
                {
                    toggled = true;
                    isOn = !isOn;
                    ApplyVisualState();
                }

                switchLever.localRotation = Quaternion.Slerp(startRotation, midRotation, progress);
                yield return null;
            }

            promptUI.HideProgress();
            firstPersonCamera?.SetInputSuspended(false);
            controller?.SetInputEnabled(true);
            interactionRunning = false;
        }

        private void ApplyVisualState()
        {
            if (targetLight != null)
            {
                targetLight.enabled = isOn;
                targetLight.intensity = isOn ? onIntensity : offIntensity;
            }

            if (switchLever != null)
            {
                switchLever.localRotation = isOn ? leverOnRotation : leverOffRotation;
            }

            if (switchRenderer != null)
            {
                IslandInteractionUtility.ApplyTint(switchRenderer, isOn ? switchOnTint : switchOffTint);
            }

            if (roomRenderers == null)
            {
                return;
            }

            for (int i = 0; i < roomRenderers.Length; i++)
            {
                Renderer renderer = roomRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Color fallback = IslandInteractionUtility.ResolveRendererColor(renderer, Color.white);
                Color onTint = lightsOnColors != null && i < lightsOnColors.Length ? lightsOnColors[i] : fallback;
                Color offTint = lightsOffColors != null && i < lightsOffColors.Length ? lightsOffColors[i] : (onTint * 0.35f);
                IslandInteractionUtility.ApplyTint(renderer, isOn ? onTint : offTint);
            }
        }
    }
}
