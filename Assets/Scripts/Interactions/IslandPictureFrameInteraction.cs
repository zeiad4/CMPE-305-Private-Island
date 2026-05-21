using System.Collections;
using UnityEngine;

namespace PrivateIsland
{
    [DisallowMultipleComponent]
    public sealed class IslandPictureFrameInteraction : IslandInteractable
    {
        [SerializeField] private Transform frameRoot;
        [SerializeField] private Vector3 fallenLocalPosition;
        [SerializeField] private Vector3 fallenLocalEulerAngles = new Vector3(90f, 90f, 0f);

        private IslandHiddenNoteInteraction hiddenNote;
        private Vector3 standingLocalPosition;
        private Quaternion standingLocalRotation;
        private Quaternion fallenLocalRotation;
        private Vector3 fallenLocalClearanceOffset;
        private float floorLiftPadding = 0.02f;
        private bool fallen;
        private bool interactionRunning;

        public void Configure(
            Transform targetFrame,
            Vector3 dropTargetLocalPosition,
            Quaternion dropTargetLocalRotation,
            IslandHiddenNoteInteraction noteToReveal,
            float interactionRadius,
            float focusHeight,
            string interactionPrompt)
        {
            frameRoot = targetFrame != null ? targetFrame : transform;
            standingLocalPosition = frameRoot.localPosition;
            standingLocalRotation = frameRoot.localRotation;
            fallenLocalPosition = dropTargetLocalPosition;
            fallenLocalRotation = dropTargetLocalRotation;
            fallenLocalEulerAngles = fallenLocalRotation.eulerAngles;
            Vector3 faceNormal = fallenLocalRotation * Vector3.forward;
            if (Vector3.Dot(faceNormal, Vector3.up) < 0f)
            {
                faceNormal = -faceNormal;
            }

            fallenLocalClearanceOffset = faceNormal.normalized * 0.032f;
            hiddenNote = noteToReveal;

            SetInteractionPrompt(interactionPrompt);
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(focusHeight);
        }

        public override bool SupportsInteractionKey(KeyCode key)
        {
            return key == KeyCode.E;
        }

        public override bool CanInteract(Transform interactor)
        {
            return !fallen && !interactionRunning && interactor != null && frameRoot != null;
        }

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            IslandActionToolVisual toolVisual = IslandActionToolVisual.GetOrCreate();
            toolVisual?.PlayOneShot(IslandActionToolVisual.ToolKind.Hand, FocusPoint + (frameRoot.right * 0.16f), 0.19f);
            hiddenNote?.Reveal();

            if (!Application.isPlaying)
            {
                frameRoot.localRotation = fallenLocalRotation;
                frameRoot.localPosition = ResolveGroundedLocalPosition(fallenLocalPosition + fallenLocalClearanceOffset);
                fallen = true;
                enabled = false;
                return;
            }

            StartCoroutine(FallRoutine());
        }

        private IEnumerator FallRoutine()
        {
            interactionRunning = true;

            Vector3 startPosition = frameRoot.localPosition;
            Quaternion startRotation = frameRoot.localRotation;
            const float duration = 0.27f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float horizontalProgress = Mathf.SmoothStep(0f, 1f, progress);
                float verticalProgress = progress * progress;
                Vector3 travel = fallenLocalPosition - startPosition;
                Vector3 basePosition = startPosition + new Vector3(
                    travel.x * horizontalProgress,
                    travel.y * verticalProgress,
                    travel.z * horizontalProgress);

                // Push the frame briefly out from the wall before it slaps down.
                float outwardKick = Mathf.Sin(progress * Mathf.PI) * 0.2f;
                float sideWobble = Mathf.Sin(progress * Mathf.PI * 2f) * 0.04f;
                frameRoot.localPosition = basePosition + new Vector3(outwardKick, 0f, sideWobble) + (fallenLocalClearanceOffset * horizontalProgress);

                float rotationProgress = 1f - Mathf.Pow(1f - progress, 2.2f);
                frameRoot.localRotation = Quaternion.Slerp(startRotation, fallenLocalRotation, rotationProgress);
                yield return null;
            }

            frameRoot.localRotation = fallenLocalRotation;
            frameRoot.localPosition = ResolveGroundedLocalPosition(fallenLocalPosition + fallenLocalClearanceOffset);
            fallen = true;
            interactionRunning = false;
            enabled = false;
        }

        private Vector3 ResolveGroundedLocalPosition(Vector3 desiredLocalPosition)
        {
            if (frameRoot == null || frameRoot.parent == null)
            {
                return desiredLocalPosition;
            }

            Vector3 originalPosition = frameRoot.localPosition;
            Quaternion originalRotation = frameRoot.localRotation;

            frameRoot.localRotation = fallenLocalRotation;
            frameRoot.localPosition = desiredLocalPosition;

            Vector3 resolvedPosition = desiredLocalPosition;
            if (IslandInteractionUtility.TryGetCompositeBounds(frameRoot, out Bounds bounds))
            {
                float floorWorldY = frameRoot.parent.TransformPoint(new Vector3(0f, fallenLocalPosition.y, 0f)).y;
                float lift = (floorWorldY + floorLiftPadding) - bounds.min.y;
                if (lift > 0f)
                {
                    resolvedPosition += frameRoot.parent.InverseTransformVector(Vector3.up * lift);
                }
            }

            frameRoot.localPosition = originalPosition;
            frameRoot.localRotation = originalRotation;
            return resolvedPosition;
        }
    }
}
