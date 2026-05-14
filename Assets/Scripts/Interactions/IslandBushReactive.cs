using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandBushReactive : MonoBehaviour
    {
        [SerializeField] private float triggerRadius = 1.5f;
        [SerializeField] private float cooldown = 1.2f;
        [SerializeField] private int rustleLeafCount = 4;

        private readonly List<Renderer> leafRenderers = new List<Renderer>();
        private Transform playerTransform;
        private IslandCharacterController playerController;
        private Quaternion restingRotation;
        private float nextResponseTime;
        private bool reactionRunning;

        public void Configure(float radius)
        {
            triggerRadius = Mathf.Max(0.9f, radius);
        }

        private void Awake()
        {
            restingRotation = transform.localRotation;
            CacheLeafRenderers();
        }

        private void Update()
        {
            if (!Application.isPlaying || reactionRunning)
            {
                return;
            }

            ResolvePlayer();
            if (playerTransform == null || playerController == null)
            {
                return;
            }

            if (Time.time < nextResponseTime || playerController.CurrentVelocity.sqrMagnitude < 0.25f)
            {
                return;
            }

            Vector3 planarDelta = playerTransform.position - transform.position;
            planarDelta.y = 0f;
            if (planarDelta.sqrMagnitude > triggerRadius * triggerRadius)
            {
                return;
            }

            StartCoroutine(RustleRoutine(planarDelta.normalized));
        }

        private IEnumerator RustleRoutine(Vector3 playerDirection)
        {
            reactionRunning = true;
            nextResponseTime = Time.time + cooldown;
            DropLeaves(rustleLeafCount + Random.Range(0, 3));

            float duration = 0.65f;
            float elapsed = 0f;
            Vector3 axis = Vector3.Cross(Vector3.up, playerDirection.sqrMagnitude > 0.0001f ? playerDirection : transform.forward);
            if (axis.sqrMagnitude <= 0.001f)
            {
                axis = transform.right;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(normalized * Mathf.PI * 3f) * (1f - normalized) * 12f;
                transform.localRotation = restingRotation * Quaternion.AngleAxis(wave, axis.normalized);
                yield return null;
            }

            transform.localRotation = restingRotation;
            reactionRunning = false;
        }

        private void ResolvePlayer()
        {
            if (playerTransform != null && playerController != null)
            {
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                return;
            }

            playerTransform = playerObject.transform;
            playerController = playerObject.GetComponent<IslandCharacterController>();
        }

        private void CacheLeafRenderers()
        {
            leafRenderers.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && (renderer.gameObject.name.Contains("Leaf") || renderer.gameObject.name.Contains("Cluster")))
                {
                    leafRenderers.Add(renderer);
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
                GameObject leaf = IslandInteractionUtility.CreateMeshObject("BushLeaf", PrimitiveType.Capsule, leafMaterial);
                leaf.transform.position = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.center.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z));
                leaf.transform.rotation = Random.rotation;
                leaf.transform.localScale = new Vector3(0.035f, 0.12f, 0.04f);

                CapsuleCollider collider = leaf.AddComponent<CapsuleCollider>();
                collider.direction = 1;

                Rigidbody rigidbody = leaf.AddComponent<Rigidbody>();
                rigidbody.mass = 0.02f;
                rigidbody.linearDamping = 0.18f;
                rigidbody.angularDamping = 0.06f;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.AddForce(new Vector3(Random.Range(-0.55f, 0.55f), Random.Range(0.08f, 0.28f), Random.Range(-0.55f, 0.55f)), ForceMode.Impulse);

                IslandTimedDestroy cleanup = leaf.AddComponent<IslandTimedDestroy>();
                cleanup.Configure(10f);
            }
        }
    }
}
