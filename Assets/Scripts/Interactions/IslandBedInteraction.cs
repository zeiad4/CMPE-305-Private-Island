using System.Collections;
using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandBedInteraction : IslandInteractable
    {
        [SerializeField] private Transform sleepCameraAnchor;
        [SerializeField] private float settleDuration = 1.1f;
        [SerializeField] private float blackoutDuration = 5f;

        private bool interactionRunning;

        public void Configure(Transform cameraAnchor, float interactionRadius, float focusHeight)
        {
            sleepCameraAnchor = cameraAnchor;
            SetInteractionPrompt("Press E to sleep");
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(focusHeight);
        }

        public override bool SupportsInteractionKey(KeyCode key)
        {
            return key == KeyCode.E;
        }

        public override bool CanInteract(Transform interactor)
        {
            return !interactionRunning && interactor != null && sleepCameraAnchor != null;
        }

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            StartCoroutine(SleepRoutine(interactor));
        }

        private IEnumerator SleepRoutine(Transform interactor)
        {
            interactionRunning = true;

            IslandCharacterController controller = interactor.GetComponent<IslandCharacterController>() ?? interactor.GetComponentInParent<IslandCharacterController>();
            IslandFirstPersonCamera firstPersonCamera = Camera.main != null
                ? Camera.main.GetComponent<IslandFirstPersonCamera>()
                : null;
            IslandInteractionPromptUI promptUI = IslandInteractionPromptUI.GetOrCreate();
            IslandSleepOverlayUI sleepOverlay = IslandSleepOverlayUI.GetOrCreate();

            controller?.SetInputEnabled(false);
            firstPersonCamera?.SetInputSuspended(true);
            promptUI.Hide();

            Vector3 startPosition = Camera.main != null ? Camera.main.transform.position : sleepCameraAnchor.position;
            Quaternion startRotation = Camera.main != null ? Camera.main.transform.rotation : sleepCameraAnchor.rotation;

            float elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                Vector3 position = Vector3.Lerp(startPosition, sleepCameraAnchor.position, eased);
                Quaternion rotation = Quaternion.Slerp(startRotation, sleepCameraAnchor.rotation, eased);
                firstPersonCamera?.SetScriptedPose(position, rotation);
                sleepOverlay.Show(Mathf.Lerp(0f, 0.2f, eased), "Getting into bed...");
                yield return null;
            }

            float fadeOutDuration = 0.8f;
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                firstPersonCamera?.SetScriptedPose(sleepCameraAnchor.position, sleepCameraAnchor.rotation);
                sleepOverlay.Show(Mathf.Lerp(0.2f, 1f, t), "Sleeping...");
                yield return null;
            }

            sleepOverlay.Show(1f, "Sleeping...");
            yield return new WaitForSeconds(blackoutDuration);

            float fadeInDuration = 0.9f;
            elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                firstPersonCamera?.SetScriptedPose(sleepCameraAnchor.position, sleepCameraAnchor.rotation);
                sleepOverlay.Show(Mathf.Lerp(1f, 0f, t), "You wake up feeling rested.");
                yield return null;
            }

            sleepOverlay.HideImmediate();
            firstPersonCamera?.ClearScriptedPose();
            firstPersonCamera?.SetInputSuspended(false);
            controller?.SetInputEnabled(true);
            interactionRunning = false;
        }
    }
}
