using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandPlayerInteractor : MonoBehaviour
    {
        [SerializeField] private KeyCode interactionKey = KeyCode.F;
        [SerializeField] private KeyCode alternateInteractionKey = KeyCode.E;
        [SerializeField] private KeyCode tertiaryInteractionKey = KeyCode.T;
        [SerializeField] private float lookDotThreshold = 0.25f;

        private Camera mainCamera;
        private IslandInteractionPromptUI promptUI;
        private IslandCharacterController characterController;
        private IslandInventory inventory;
        private EnvironmentMenuUI environmentMenuUI;
        private IslandInteractable currentTarget;

        private void Awake()
        {
            characterController = GetComponent<IslandCharacterController>();
            inventory = GetComponent<IslandInventory>();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            promptUI ??= IslandInteractionPromptUI.GetOrCreate();
            mainCamera ??= Camera.main;
            inventory ??= GetComponent<IslandInventory>();
            environmentMenuUI ??= FindAnyObjectByType<EnvironmentMenuUI>();

            if ((environmentMenuUI != null && environmentMenuUI.IsMenuOpen) ||
                (inventory != null && inventory.IsOpen) ||
                (characterController != null && !characterController.IsInputEnabled))
            {
                currentTarget = null;
                if (!promptUI.HasActiveProgress)
                {
                    promptUI.Hide();
                }

                return;
            }

            currentTarget = FindBestTarget();
            if (currentTarget != null)
            {
                promptUI.Show(currentTarget.InteractionPrompt);
                TryInteract(interactionKey);
                TryInteract(alternateInteractionKey);
                TryInteract(tertiaryInteractionKey);

                return;
            }

            if (!promptUI.HasActiveProgress)
            {
                promptUI.Hide();
            }
        }

        private IslandInteractable FindBestTarget()
        {
            if (mainCamera == null)
            {
                return null;
            }

            IslandInteractable bestTarget = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < IslandInteractable.Active.Count; i++)
            {
                IslandInteractable candidate = IslandInteractable.Active[i];
                if (candidate == null || !candidate.isActiveAndEnabled || !candidate.CanInteract(transform))
                {
                    continue;
                }

                Vector3 toCandidate = candidate.FocusPoint - mainCamera.transform.position;
                float distance = toCandidate.magnitude;
                if (distance > candidate.InteractionRadius || distance <= 0.01f)
                {
                    continue;
                }

                Vector3 direction = toCandidate / distance;
                float dot = Vector3.Dot(mainCamera.transform.forward, direction);
                if (dot < lookDotThreshold && distance > 1.75f)
                {
                    continue;
                }

                float score = (dot * 4f) - distance;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private void TryInteract(KeyCode key)
        {
            if (currentTarget == null || !Input.GetKeyDown(key) || !currentTarget.SupportsInteractionKey(key))
            {
                return;
            }

            currentTarget.Interact(transform, key);
        }
    }
}
