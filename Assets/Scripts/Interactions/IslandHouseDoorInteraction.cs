using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandHouseDoorInteraction : IslandInteractable
    {
        [SerializeField] private Transform destinationAnchor;
        [SerializeField] private int allowedSideSign;
        [SerializeField] private float sideThreshold = 0.08f;

        public void Configure(Transform destination, string prompt, float interactionRadius, float focusHeight, int sideSign = 0)
        {
            destinationAnchor = destination;
            allowedSideSign = Mathf.Clamp(sideSign, -1, 1);
            SetInteractionPrompt(prompt);
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(focusHeight);
        }

        public override bool SupportsInteractionKey(KeyCode key)
        {
            return key == KeyCode.E;
        }

        public override bool CanInteract(Transform interactor)
        {
            if (destinationAnchor == null || interactor == null)
            {
                return false;
            }

            if (allowedSideSign == 0)
            {
                return true;
            }

            Vector3 toInteractor = interactor.position - transform.position;
            float sideDot = Vector3.Dot(transform.forward, toInteractor.normalized);
            return allowedSideSign > 0
                ? sideDot >= sideThreshold
                : sideDot <= -sideThreshold;
        }

        public override void Interact(Transform interactor)
        {
            if (destinationAnchor == null || interactor == null)
            {
                return;
            }

            IslandCharacterController controller = interactor.GetComponent<IslandCharacterController>();
            Vector3 destination = destinationAnchor.position;
            float yaw = destinationAnchor.eulerAngles.y;

            if (controller != null)
            {
                controller.TeleportTo(destination, yaw);
                return;
            }

            interactor.position = destination;
            interactor.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
