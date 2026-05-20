using UnityEngine;

namespace PrivateIsland
{
    [DisallowMultipleComponent]
    public sealed class IslandWorldItem : IslandInteractable
    {
        [SerializeField] private string itemId = IslandItemCatalog.FlowerId;
        [SerializeField] private int count = 1;
        [SerializeField] private float rotationSpeed = 52f;
        [SerializeField] private float bobHeight = 0.05f;
        [SerializeField] private float bobSpeed = 2.4f;
        [SerializeField] private bool animateWhileIdle = true;
        [SerializeField] private bool simulatePhysics = true;

        private Transform visualRoot;
        private Rigidbody cachedRigidbody;
        private SphereCollider cachedCollider;
        private Vector3 restingVisualLocalPosition;
        private float stationaryTime;
        private bool initialized;

        public static bool TrySpawnDrop(Transform dropper, IslandInventory.InventoryStack stack)
        {
            if (dropper == null || stack.IsEmpty)
            {
                return false;
            }

            Vector3 forward = dropper.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();

            Vector3 spawnPosition = dropper.position + (forward * 1.25f) + Vector3.up * 1.1f;
            if (Physics.Raycast(spawnPosition + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore))
            {
                spawnPosition = hit.point + Vector3.up * 0.12f;
            }

            return SpawnWorldItem(
                       stack.ItemId,
                       stack.Count,
                       spawnPosition,
                       Quaternion.identity,
                       true,
                       true,
                       (forward * 1.25f) + Vector3.up * 0.75f,
                       new Vector3(0.8f, 1.1f, 0.7f)) != null;
        }

        public static IslandWorldItem SpawnWorldItem(
            string worldItemId,
            int worldItemCount,
            Vector3 position,
            Quaternion rotation,
            bool shouldAnimateWhileIdle,
            bool shouldSimulatePhysics,
            Vector3 initialImpulse,
            Vector3 initialTorque,
            float lifetime = -1f,
            Transform parent = null)
        {
            if (string.IsNullOrWhiteSpace(worldItemId) ||
                worldItemCount <= 0 ||
                !IslandItemCatalog.TryGetDefinition(worldItemId, out _))
            {
                return null;
            }

            GameObject root = new GameObject($"{IslandItemCatalog.GetDisplayName(worldItemId)} Pickup");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            root.transform.position = position;
            root.transform.rotation = rotation;

            IslandWorldItem worldItem = root.AddComponent<IslandWorldItem>();
            worldItem.Configure(worldItemId, worldItemCount, shouldAnimateWhileIdle, shouldSimulatePhysics);
            worldItem.ApplyInitialForces(initialImpulse, initialTorque);

            if (lifetime > 0f)
            {
                IslandTimedDestroy timedDestroy = root.AddComponent<IslandTimedDestroy>();
                timedDestroy.Configure(lifetime);
            }

            return worldItem;
        }

        private void Awake()
        {
            EnsureInitialized();
            UpdatePresentation();
        }

        private void Update()
        {
            if (!Application.isPlaying || visualRoot == null || !animateWhileIdle)
            {
                if (Application.isPlaying)
                {
                    TrySettleOnGround();
                }

                return;
            }

            float bodySpeed = cachedRigidbody != null && !cachedRigidbody.isKinematic
                ? cachedRigidbody.linearVelocity.sqrMagnitude
                : 0f;

            float bobOffset = bodySpeed > 0.02f
                ? 0f
                : Mathf.Sin(Time.time * bobSpeed) * bobHeight;

            visualRoot.localPosition = restingVisualLocalPosition + Vector3.up * bobOffset;

            if (bodySpeed <= 0.02f)
            {
                visualRoot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
            }

            TrySettleOnGround();
        }

        public void Configure(string newItemId, int newCount, bool shouldAnimateWhileIdle = true, bool shouldSimulatePhysics = true)
        {
            itemId = newItemId;
            count = Mathf.Max(1, newCount);
            animateWhileIdle = shouldAnimateWhileIdle;
            simulatePhysics = shouldSimulatePhysics;

            EnsureInitialized();
            UpdatePresentation();
        }

        public void SetWorldScale(Vector3 scale)
        {
            transform.localScale = scale;

            if (initialized)
            {
                UpdatePresentation();
            }
        }

        public override bool CanInteract(Transform interactor)
        {
            return interactor != null;
        }

        public override void Interact(Transform interactor)
        {
            if (interactor == null)
            {
                return;
            }

            IslandInventory inventory = interactor.GetComponent<IslandInventory>() ?? interactor.GetComponentInParent<IslandInventory>();
            if (inventory == null || !inventory.TryAddItem(itemId, count))
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

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            visualRoot = transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                GameObject visualObject = new GameObject("VisualRoot");
                visualObject.transform.SetParent(transform, false);
                visualRoot = visualObject.transform;
            }

            cachedRigidbody = GetComponent<Rigidbody>();
            if (cachedRigidbody == null)
            {
                cachedRigidbody = gameObject.AddComponent<Rigidbody>();
            }

            cachedCollider = GetComponent<SphereCollider>();
            if (cachedCollider == null)
            {
                cachedCollider = gameObject.AddComponent<SphereCollider>();
            }

            initialized = true;
        }

        private void UpdatePresentation()
        {
            if (!IslandItemCatalog.TryGetDefinition(itemId, out IslandItemCatalog.ItemDefinition definition))
            {
                itemId = IslandItemCatalog.RockId;
                definition = IslandItemCatalog.GetDefinition(itemId);
            }

            ClearChildren(visualRoot);
            IslandItemCatalog.BuildWorldVisual(itemId, visualRoot);

            float idleLift = animateWhileIdle ? 0.05f : 0f;
            AlignVisualToGround(idleLift);

            UpdatePrompt(definition.DisplayName);
            ConfigurePhysics();
            UpdateCollider();

            if (!simulatePhysics)
            {
                SnapToGround();
            }
        }

        private void UpdatePrompt(string displayName)
        {
            string quantity = count > 1 ? $" x{count}" : string.Empty;
            SetInteractionPrompt($"Press E or F to pick up {displayName}{quantity}");
            SetInteractionRadius(3.25f);
            SetFocusHeight(0.45f);
        }

        private void ConfigurePhysics()
        {
            if (cachedRigidbody == null)
            {
                return;
            }

            cachedRigidbody.mass = 0.55f;
            cachedRigidbody.linearDamping = 1.25f;
            cachedRigidbody.angularDamping = 1.4f;
            cachedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            stationaryTime = 0f;

            if (!simulatePhysics)
            {
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
            }

            cachedRigidbody.useGravity = simulatePhysics;
            cachedRigidbody.isKinematic = !simulatePhysics;
        }

        private void UpdateCollider()
        {
            if (cachedCollider == null)
            {
                return;
            }

            if (!IslandInteractionUtility.TryGetCompositeBounds(visualRoot, out Bounds bounds))
            {
                cachedCollider.center = new Vector3(0f, 0.2f, 0f);
                cachedCollider.radius = 0.3f;
                return;
            }

            cachedCollider.center = transform.InverseTransformPoint(bounds.center);
            cachedCollider.radius = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)) + 0.04f;
        }

        private void ApplyInitialForces(Vector3 initialImpulse, Vector3 initialTorque)
        {
            if (cachedRigidbody == null || cachedRigidbody.isKinematic)
            {
                return;
            }

            if (initialImpulse.sqrMagnitude > 0.0001f)
            {
                cachedRigidbody.AddForce(initialImpulse, ForceMode.Impulse);
            }

            if (initialTorque.sqrMagnitude > 0.0001f)
            {
                cachedRigidbody.AddTorque(initialTorque, ForceMode.Impulse);
            }
        }

        private void TrySettleOnGround()
        {
            if (!simulatePhysics || cachedRigidbody == null || cachedRigidbody.isKinematic)
            {
                return;
            }

            float linearSpeed = cachedRigidbody.linearVelocity.sqrMagnitude;
            float angularSpeed = cachedRigidbody.angularVelocity.sqrMagnitude;
            if (linearSpeed > 0.01f || angularSpeed > 0.01f)
            {
                stationaryTime = 0f;
                return;
            }

            stationaryTime += Time.deltaTime;
            if (stationaryTime < 0.18f)
            {
                return;
            }

            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
            cachedRigidbody.isKinematic = true;
            AlignVisualToGround(animateWhileIdle ? 0.05f : 0f);
            SnapToGround();
        }

        private void AlignVisualToGround(float idleLift)
        {
            if (visualRoot == null)
            {
                return;
            }

            restingVisualLocalPosition = Vector3.zero;
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;

            if (IslandInteractionUtility.TryGetCompositeBounds(visualRoot, out Bounds bounds))
            {
                float groundAlignment = transform.position.y - bounds.min.y;
                restingVisualLocalPosition = new Vector3(0f, groundAlignment + idleLift, 0f);
            }
            else
            {
                restingVisualLocalPosition = new Vector3(0f, idleLift, 0f);
            }

            visualRoot.localPosition = restingVisualLocalPosition;
            visualRoot.localRotation = Quaternion.identity;
        }

        private void SnapToGround()
        {
            IslandSceneBootstrap bootstrap = FindAnyObjectByType<IslandSceneBootstrap>();
            if (bootstrap != null)
            {
                float terrainHeight = IslandMeshBuilder.SampleHeight(transform.position.x, transform.position.z, bootstrap.IslandSize, bootstrap.PeakHeight);
                transform.position = new Vector3(transform.position.x, terrainHeight, transform.position.z);
                return;
            }

            Vector3 rayOrigin = transform.position + Vector3.up * 1.8f;
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null ||
                    hits[i].collider.transform == transform ||
                    hits[i].collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                transform.position = hits[i].point;
                return;
            }
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }
    }
}
