using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandPalmInteraction : IslandInteractable
    {
        [SerializeField] private float cooldown = 3.5f;
        [SerializeField] private float swayAngle = 10f;
        [SerializeField] private float swayDuration = 1.1f;
        [SerializeField] private int leafBurstCount = 8;
        [SerializeField] private int coconutDropCount = 2;
        [SerializeField] private int woodDropCount = 4;
        [SerializeField] private float chopDuration = 4f;
        [SerializeField] private float woodRevealDelay = 1.2f;

        private readonly List<Renderer> leafRenderers = new List<Renderer>();
        private readonly List<Transform> coconutRoots = new List<Transform>();
        private readonly List<Transform> trunkSegments = new List<Transform>();

        private Quaternion restingRotation;
        private float nextInteractionTime;
        private float palmHeight;
        private bool interactionRunning;
        private bool chopped;
        private bool woodDropped;
        private Rigidbody palmBody;
        private Coroutine hitReactionRoutine;

        public void Configure(float interactionRadius, float canopyHeight)
        {
            palmHeight = canopyHeight;
            woodDropCount = Mathf.Clamp(Mathf.RoundToInt(canopyHeight * 0.6f), 3, 6);
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(Mathf.Max(1.6f, canopyHeight * 0.82f));
            RefreshPrompt();
        }

        private void Awake()
        {
            restingRotation = transform.localRotation;
            CacheParts();
            RefreshPrompt();
        }

        public override bool SupportsInteractionKey(KeyCode key)
        {
            if (chopped)
            {
                return false;
            }

            return key == KeyCode.T || key == KeyCode.E || key == KeyCode.F;
        }

        public override bool CanInteract(Transform interactor)
        {
            return !chopped && !interactionRunning && Time.time >= nextInteractionTime;
        }

        public override void Interact(Transform interactor, KeyCode key)
        {
            if (key == KeyCode.T)
            {
                ChopDown(interactor);
                return;
            }

            if (key == KeyCode.E || key == KeyCode.F)
            {
                Interact(interactor);
            }
        }

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            StartCoroutine(ShakeRoutine());
        }

        private void ChopDown(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            StartCoroutine(ChopRoutine(interactor));
        }

        private IEnumerator ShakeRoutine()
        {
            interactionRunning = true;
            nextInteractionTime = Time.time + cooldown;
            CacheParts();
            DropLeaves(leafBurstCount + Random.Range(0, 3));
            DropCoconuts(coconutDropCount);

            float elapsed = 0f;
            while (elapsed < swayDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / swayDuration);
                float wave = Mathf.Sin(normalized * Mathf.PI * 4f) * (1f - normalized);
                transform.localRotation = restingRotation * Quaternion.Euler(0f, 0f, wave * swayAngle);
                yield return null;
            }

            transform.localRotation = restingRotation;
            interactionRunning = false;
        }

        private IEnumerator ChopRoutine(Transform interactor)
        {
            interactionRunning = true;
            CacheParts();

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
            toolVisual?.ShowTool(IslandActionToolVisual.ToolKind.Axe);

            float elapsed = 0f;
            int lastHitIndex = -1;
            while (elapsed < chopDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / chopDuration);
                promptUI.ShowProgress("Chopping tree...", progress);
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

            chopped = true;
            nextInteractionTime = float.MaxValue;
            RefreshPrompt();

            DropLeaves(leafBurstCount + 4);
            DropCoconuts(int.MaxValue);
            EnablePalmPhysics(interactor);

            yield return new WaitForSeconds(woodRevealDelay);

            if (!woodDropped)
            {
                SpawnWoodDrops();
                woodDropped = true;
            }

            interactionRunning = false;
        }

        private void CacheParts()
        {
            leafRenderers.Clear();
            coconutRoots.Clear();
            trunkSegments.Clear();

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.gameObject.name.Contains("Leaf"))
                {
                    leafRenderers.Add(renderer);
                }
            }

            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in transforms)
            {
                if (child != null && child.name.Contains("Coconut"))
                {
                    coconutRoots.Add(child);
                }

                if (child != null && child.name.Contains("TrunkSegment"))
                {
                    trunkSegments.Add(child);
                }
            }
        }

        private void DropLeaves(int count)
        {
            if (leafRenderers.Count == 0)
            {
                return;
            }

            if (!IslandInteractionUtility.TryGetCompositeBounds(transform, out Bounds bounds))
            {
                return;
            }

            Material leafMaterial = leafRenderers[0].sharedMaterial;
            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.center.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z));

                GameObject leaf = IslandInteractionUtility.CreateMeshObject("LoosePalmLeaf", PrimitiveType.Capsule, leafMaterial);
                leaf.transform.position = spawnPosition;
                leaf.transform.rotation = Random.rotation;
                leaf.transform.localScale = new Vector3(0.045f, 0.16f, 0.05f);

                CapsuleCollider collider = leaf.AddComponent<CapsuleCollider>();
                collider.direction = 1;

                Rigidbody rigidbody = leaf.AddComponent<Rigidbody>();
                rigidbody.mass = 0.03f;
                rigidbody.linearDamping = 0.22f;
                rigidbody.angularDamping = 0.08f;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.AddForce(new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(0.2f, 0.55f), Random.Range(-0.9f, 0.9f)), ForceMode.Impulse);

                IslandTimedDestroy cleanup = leaf.AddComponent<IslandTimedDestroy>();
                cleanup.Configure(14f);
            }
        }

        private void DropCoconuts(int requestedCount)
        {
            List<Transform> activeCoconuts = new List<Transform>();
            for (int i = 0; i < coconutRoots.Count; i++)
            {
                Transform coconut = coconutRoots[i];
                if (coconut != null && coconut.gameObject.activeSelf)
                {
                    activeCoconuts.Add(coconut);
                }
            }

            int dropCount = requestedCount == int.MaxValue
                ? activeCoconuts.Count
                : Mathf.Min(requestedCount, activeCoconuts.Count);

            for (int i = 0; i < dropCount; i++)
            {
                int selectedIndex = Random.Range(0, activeCoconuts.Count);
                Transform coconut = activeCoconuts[selectedIndex];
                activeCoconuts.RemoveAt(selectedIndex);

                Vector3 launch = ((coconut.position - transform.position).normalized * Random.Range(0.65f, 1.2f)) + (Vector3.up * 0.32f);
                IslandWorldItem droppedCoconut = IslandWorldItem.SpawnWorldItem(
                    IslandItemCatalog.CoconutId,
                    1,
                    coconut.position + Vector3.up * 0.08f,
                    coconut.rotation,
                    false,
                    true,
                    launch,
                    new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(0.7f, 1.2f), Random.Range(-0.45f, 0.45f)));

                if (droppedCoconut != null)
                {
                    droppedCoconut.SetWorldScale(coconut.lossyScale);
                }

                coconut.gameObject.SetActive(false);
            }
        }

        private void EnablePalmPhysics(Transform interactor)
        {
            palmBody ??= GetComponent<Rigidbody>();
            if (palmBody == null)
            {
                palmBody = gameObject.AddComponent<Rigidbody>();
            }

            palmBody.mass = Mathf.Clamp(palmHeight * 1.45f, 6f, 12f);
            palmBody.linearDamping = 0.38f;
            palmBody.angularDamping = 0.5f;
            palmBody.interpolation = RigidbodyInterpolation.Interpolate;
            palmBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            palmBody.isKinematic = false;

            Vector3 pushDirection = interactor != null
                ? transform.position - interactor.position
                : transform.right;
            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude < 0.001f)
            {
                pushDirection = transform.right;
            }

            pushDirection.Normalize();

            Vector3 torqueAxis = Vector3.Cross(Vector3.up, pushDirection).normalized;
            if (torqueAxis.sqrMagnitude < 0.001f)
            {
                torqueAxis = transform.forward;
            }

            palmBody.AddForce((pushDirection * 1.65f) + (Vector3.up * 0.4f), ForceMode.Impulse);
            palmBody.AddTorque(torqueAxis * Mathf.Clamp(palmHeight * 3.2f, 18f, 32f), ForceMode.Impulse);
        }

        private void SpawnWoodDrops()
        {
            if (trunkSegments.Count == 0)
            {
                return;
            }

            int dropCount = Mathf.Min(woodDropCount, trunkSegments.Count);
            for (int i = 0; i < dropCount; i++)
            {
                int segmentIndex = Mathf.RoundToInt(((trunkSegments.Count - 1f) * i) / Mathf.Max(1f, dropCount - 1f));
                Transform segment = trunkSegments[Mathf.Clamp(segmentIndex, 0, trunkSegments.Count - 1)];
                if (segment == null)
                {
                    continue;
                }

                Vector3 woodAxis = segment.up.sqrMagnitude > 0.001f ? segment.up.normalized : transform.right;
                Quaternion woodRotation = Quaternion.FromToRotation(Vector3.right, woodAxis);
                Vector3 spawnPosition = segment.position + Vector3.up * 0.08f;

                IslandWorldItem woodItem = IslandWorldItem.SpawnWorldItem(
                    IslandItemCatalog.WoodId,
                    1,
                    spawnPosition + Vector3.up * 0.06f,
                    woodRotation,
                    false,
                    true,
                    (woodAxis * Random.Range(-0.16f, 0.16f)) + Vector3.up * Random.Range(0.12f, 0.24f),
                    new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.28f, 0.28f), Random.Range(-0.2f, 0.2f)));

                if (woodItem != null)
                {
                    woodItem.SetWorldScale(Vector3.one * 1.08f);
                }
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

        private void RefreshPrompt()
        {
            SetInteractionPrompt(chopped
                ? string.Empty
                : "Press F to shake the palm or T to chop it down");
        }

        private void TriggerHitReaction()
        {
            if (hitReactionRoutine != null)
            {
                StopCoroutine(hitReactionRoutine);
            }

            hitReactionRoutine = StartCoroutine(HitReactionRoutine());
        }

        private IEnumerator HitReactionRoutine()
        {
            float duration = 0.24f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(normalized * Mathf.PI) * 4.6f;
                transform.localRotation = restingRotation * Quaternion.Euler(pulse * 0.25f, pulse * 0.42f, pulse);
                yield return null;
            }

            transform.localRotation = restingRotation;
            hitReactionRoutine = null;
        }
    }
}
