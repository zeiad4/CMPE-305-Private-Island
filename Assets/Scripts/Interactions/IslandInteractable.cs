using System.Collections.Generic;
using UnityEngine;

namespace PrivateIsland
{
    public abstract class IslandInteractable : MonoBehaviour
    {
        private static readonly List<IslandInteractable> ActiveInteractables = new List<IslandInteractable>();

        [SerializeField] private string interactionPrompt = "Press F to interact";
        [SerializeField] private float interactionRadius = 3f;
        [SerializeField] private float focusHeight = 1.2f;

        public static IReadOnlyList<IslandInteractable> Active => ActiveInteractables;

        public string InteractionPrompt => interactionPrompt;
        public float InteractionRadius => interactionRadius;
        public virtual Vector3 FocusPoint => transform.position + (Vector3.up * focusHeight);
        public virtual bool SupportsInteractionKey(KeyCode key) => key == KeyCode.E || key == KeyCode.F;

        protected void SetInteractionPrompt(string prompt)
        {
            interactionPrompt = string.IsNullOrWhiteSpace(prompt) ? "Press F to interact" : prompt;
        }

        protected void SetInteractionRadius(float radius)
        {
            interactionRadius = Mathf.Max(0.5f, radius);
        }

        protected void SetFocusHeight(float height)
        {
            focusHeight = Mathf.Max(0.1f, height);
        }

        protected virtual void OnEnable()
        {
            if (!ActiveInteractables.Contains(this))
            {
                ActiveInteractables.Add(this);
            }
        }

        protected virtual void OnDisable()
        {
            ActiveInteractables.Remove(this);
        }

        public abstract bool CanInteract(Transform interactor);

        public virtual void Interact(Transform interactor, KeyCode key)
        {
            if (SupportsInteractionKey(key))
            {
                Interact(interactor);
            }
        }

        public abstract void Interact(Transform interactor);
    }
}
