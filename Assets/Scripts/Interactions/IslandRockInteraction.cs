using System.Collections;
using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandRockInteraction : IslandInteractable
    {
        [SerializeField] private float mineDuration = 5f;
        [SerializeField] private int looseStoneCount = 5;

        private Quaternion restingRotation;
        private Vector3 restingPosition;
        private bool interactionRunning;
        private bool mined;
        private Coroutine reactionRoutine;

        public void Configure(float interactionRadius, float scale)
        {
            looseStoneCount = Mathf.Clamp(Mathf.RoundToInt(scale * 1.35f), 4, 7);
            SetInteractionPrompt("Press E or F to mine the rock");
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(Mathf.Max(0.8f, scale * 0.48f));
        }

        private void Awake()
        {
            restingRotation = transform.localRotation;
            restingPosition = transform.localPosition;
        }

        public override bool CanInteract(Transform interactor)
        {
            return !mined && !interactionRunning;
        }

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            StartCoroutine(MineRoutine(interactor));
        }

        private IEnumerator MineRoutine(Transform interactor)
        {
            interactionRunning = true;

            IslandCharacterController controller = interactor != null
                ? interactor.GetComponent<IslandCharacterController>() ?? interactor.GetComponentInParent<IslandCharacterController>()
                : null;
            IslandFirstPersonCamera firstPersonCamera = Camera.main != null
                ? Camera.main.GetComponent<IslandFirstPersonCamera>()
                : null;
            if (controller != null)
            {
                controller.SetInputEnabled(false);
            }

            firstPersonCamera?.SetInputSuspended(true);

            IslandInteractionPromptUI promptUI = IslandInteractionPromptUI.GetOrCreate();
            IslandActionToolVisual toolVisual = IslandActionToolVisual.GetOrCreate();
            toolVisual?.ShowTool(IslandActionToolVisual.ToolKind.Pickaxe);

            float elapsed = 0f;
            int lastHitIndex = -1;
            while (elapsed < mineDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / mineDuration);
                promptUI.ShowProgress("Mining rock...", progress);

                float strikePhase = Mathf.Repeat(elapsed, 1f);
                toolVisual?.UpdateSwing(strikePhase, 1f);

                int hitIndex = Mathf.FloorToInt(elapsed);
                if (strikePhase >= 0.55f && hitIndex != lastHitIndex)
                {
                    lastHitIndex = hitIndex;
                    TriggerHitReaction();
                }

                yield return null;
            }

            toolVisual?.HideTool();
            promptUI.HideProgress();

            if (controller != null)
            {
                controller.SetInputEnabled(true);
            }

            firstPersonCamera?.SetInputSuspended(false);

            mined = true;
            SpawnMinedStones();

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void SpawnMinedStones()
        {
            if (!IslandInteractionUtility.TryGetCompositeBounds(transform, out Bounds bounds))
            {
                return;
            }

            Vector3 center = bounds.center;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.8f;

            for (int i = 0; i < looseStoneCount; i++)
            {
                float angle = (360f / Mathf.Max(1, looseStoneCount)) * i;
                float spread = radius * Mathf.Lerp(0.16f, 0.54f, (i + 1f) / (looseStoneCount + 1f));
                Vector3 ringOffset = Quaternion.Euler(0f, angle + Random.Range(-18f, 18f), 0f) * new Vector3(spread, 0f, 0f);
                Vector3 spawnPosition = center + ringOffset + Vector3.up * Mathf.Max(0.4f, bounds.extents.y * 0.75f);

                IslandWorldItem looseStone = IslandWorldItem.SpawnWorldItem(
                    IslandItemCatalog.RockId,
                    1,
                    spawnPosition,
                    Random.rotation,
                    false,
                    true,
                    (ringOffset.normalized * Random.Range(0.12f, 0.28f)) + Vector3.up * Random.Range(0.1f, 0.22f),
                    new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.22f, 0.22f), Random.Range(-0.1f, 0.1f)));

                if (looseStone != null)
                {
                    looseStone.SetWorldScale(Vector3.one * Random.Range(1.85f, 2.35f));
                }
            }
        }

        private void TriggerHitReaction()
        {
            if (reactionRoutine != null)
            {
                StopCoroutine(reactionRoutine);
            }

            reactionRoutine = StartCoroutine(HitReactionRoutine());
        }

        private IEnumerator HitReactionRoutine()
        {
            float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(normalized * Mathf.PI) * 0.055f;
                transform.localPosition = restingPosition + new Vector3(pulse * 0.35f, Mathf.Abs(pulse) * 0.16f, -pulse * 0.24f);
                transform.localRotation = restingRotation * Quaternion.Euler(pulse * 90f, pulse * 150f, pulse * 55f);
                yield return null;
            }

            transform.localPosition = restingPosition;
            transform.localRotation = restingRotation;
            reactionRoutine = null;
        }
    }
}
