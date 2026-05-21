using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandChairSitInteraction : IslandInteractable
    {
        [SerializeField] private Transform sitAnchor;
        [SerializeField] private Transform viewAnchor;
        [SerializeField] private Transform exitAnchor;

        private IslandCharacterController seatedController;
        private IslandFirstPersonCamera seatedCamera;
        private IslandInteractionPromptUI promptUI;
        private bool occupied;

        public void Configure(Transform sitTarget, Transform cameraTarget, Transform exitTarget, float interactionRadius, float focusHeight)
        {
            sitAnchor = sitTarget;
            viewAnchor = cameraTarget;
            exitAnchor = exitTarget;
            SetInteractionPrompt("Press E to sit");
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(focusHeight);
        }

        public override bool SupportsInteractionKey(KeyCode key)
        {
            return key == KeyCode.E || key == KeyCode.F || key == KeyCode.Space;
        }

        public override bool CanInteract(Transform interactor)
        {
            if (sitAnchor == null || viewAnchor == null)
            {
                return false;
            }

            if (!occupied)
            {
                return interactor != null;
            }

            return interactor != null && seatedController != null && interactor == seatedController.transform;
        }

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (occupied)
            {
                StandUp();
                return;
            }

            SitDown(interactor);
        }

        private void Update()
        {
            if (!Application.isPlaying || !occupied)
            {
                return;
            }

            promptUI ??= IslandInteractionPromptUI.GetOrCreate();
            promptUI.Show("Press E, F, or Space to stand up");

            if (ShouldStandUp())
            {
                StandUp();
            }
        }

        private void SitDown(Transform interactor)
        {
            seatedController = interactor.GetComponent<IslandCharacterController>() ?? interactor.GetComponentInParent<IslandCharacterController>();
            if (seatedController == null)
            {
                return;
            }

            seatedCamera = Camera.main != null ? Camera.main.GetComponent<IslandFirstPersonCamera>() : null;
            seatedController.TeleportTo(sitAnchor.position, sitAnchor.eulerAngles.y);
            seatedController.SetMovementEnabled(false);
            seatedCamera?.SetScriptedPose(viewAnchor.position, viewAnchor.rotation);
            occupied = true;
            SetInteractionPrompt("Press E, F, or Space to stand up");
        }

        private void StandUp()
        {
            if (!occupied)
            {
                return;
            }

            Vector3 exitPosition = exitAnchor != null ? exitAnchor.position : sitAnchor.position + (sitAnchor.right * 1.05f);
            float exitYaw = exitAnchor != null ? exitAnchor.eulerAngles.y : sitAnchor.eulerAngles.y;

            seatedCamera?.ClearScriptedPose();
            seatedController?.SetMovementEnabled(true);
            seatedController?.TeleportTo(exitPosition, exitYaw);

            occupied = false;
            seatedController = null;
            seatedCamera = null;
            SetInteractionPrompt("Press E to sit");
        }

        private static bool ShouldStandUp()
        {
            return Input.GetKeyDown(KeyCode.E) ||
                   Input.GetKeyDown(KeyCode.F) ||
                   Input.GetKeyDown(KeyCode.Space) ||
                   Input.GetKeyDown(KeyCode.W) ||
                   Input.GetKeyDown(KeyCode.A) ||
                   Input.GetKeyDown(KeyCode.S) ||
                   Input.GetKeyDown(KeyCode.D);
        }
    }
}
