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

        private readonly List<Renderer> leafRenderers = new List<Renderer>();
        private readonly List<Transform> coconutRoots = new List<Transform>();
        private Quaternion restingRotation;
        private float nextInteractionTime;
        private bool interactionRunning;

        public void Configure(float interactionRadius, float canopyHeight)
        {
            SetInteractionPrompt("Press F to shake the palm");
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(Mathf.Max(1.6f, canopyHeight * 0.82f));
        }

        private void Awake()
        {
            restingRotation = transform.localRotation;
            CacheParts();
        }

        public override bool CanInteract(Transform interactor)
        {
            return !interactionRunning && Time.time >= nextInteractionTime;
        }

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            StartCoroutine(ShakeRoutine());
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

        private void CacheParts()
        {
            leafRenderers.Clear();
            coconutRoots.Clear();

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

            int dropCount = Mathf.Min(requestedCount, activeCoconuts.Count);
            for (int i = 0; i < dropCount; i++)
            {
                int selectedIndex = Random.Range(0, activeCoconuts.Count);
                Transform coconut = activeCoconuts[selectedIndex];
                activeCoconuts.RemoveAt(selectedIndex);

                Renderer renderer = coconut.GetComponent<Renderer>();
                GameObject fallingCoconut = IslandInteractionUtility.CreateMeshObject("FallingCoconut", PrimitiveType.Sphere, renderer != null ? renderer.sharedMaterial : null);
                fallingCoconut.transform.position = coconut.position;
                fallingCoconut.transform.rotation = coconut.rotation;
                fallingCoconut.transform.localScale = coconut.lossyScale;

                SphereCollider collider = fallingCoconut.AddComponent<SphereCollider>();
                collider.radius = 0.5f;

                Rigidbody rigidbody = fallingCoconut.AddComponent<Rigidbody>();
                rigidbody.mass = 0.9f;
                rigidbody.linearDamping = 0.06f;
                rigidbody.angularDamping = 0.18f;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

                Vector3 launch = ((coconut.position - transform.position).normalized * Random.Range(0.65f, 1.2f)) + (Vector3.up * 0.32f);
                rigidbody.AddForce(launch, ForceMode.Impulse);

                IslandTimedDestroy cleanup = fallingCoconut.AddComponent<IslandTimedDestroy>();
                cleanup.Configure(20f);
                coconut.gameObject.SetActive(false);
            }
        }
    }
}
