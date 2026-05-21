using UnityEngine;

namespace PrivateIsland
{
    [DisallowMultipleComponent]
    public sealed class IslandHiddenNoteInteraction : IslandInteractable
    {
        [SerializeField] private string itemId = IslandItemCatalog.HiddenNoteId;
        [SerializeField] private GameObject visualRoot;

        private bool revealed;

        public void Configure(GameObject noteVisualRoot, string inventoryItemId, float interactionRadius, float focusHeight)
        {
            visualRoot = noteVisualRoot;
            itemId = string.IsNullOrWhiteSpace(inventoryItemId) ? IslandItemCatalog.HiddenNoteId : inventoryItemId;

            SetInteractionPrompt("Press E to pick up the hidden note");
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(focusHeight);
            SetVisualVisible(false);
        }

        public void Reveal()
        {
            if (revealed)
            {
                return;
            }

            revealed = true;
            SetVisualVisible(true);
        }

        public override bool SupportsInteractionKey(KeyCode key)
        {
            return key == KeyCode.E;
        }

        public override bool CanInteract(Transform interactor)
        {
            return revealed && interactor != null;
        }

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            IslandInventory inventory = interactor.GetComponent<IslandInventory>() ?? interactor.GetComponentInParent<IslandInventory>();
            if (inventory == null || !inventory.TryAddItem(itemId, 1))
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void SetVisualVisible(bool visible)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(visible);
            }
        }
    }
}
