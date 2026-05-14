using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandRockInteraction : IslandInteractable
    {
        [SerializeField] private float cooldown = 2.8f;
        [SerializeField] private int looseStoneCount = 5;

        private Quaternion restingRotation;
        private Vector3 restingPosition;
        private float nextInteractionTime;
        private bool interactionRunning;

        public void Configure(float interactionRadius, float scale)
        {
            SetInteractionPrompt("Press F to loosen the rock");
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
            return !interactionRunning && Time.time >= nextInteractionTime;
        }

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            StartCoroutine(LoosenRoutine());
        }

        private IEnumerator LoosenRoutine()
        {
            interactionRunning = true;
            nextInteractionTime = Time.time + cooldown;
            SpawnLooseStones(looseStoneCount + Random.Range(0, 3));

            float duration = 0.55f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float shake = Mathf.Sin(normalized * Mathf.PI * 7f) * (1f - normalized) * 0.08f;
                transform.localPosition = restingPosition + new Vector3(shake, Mathf.Abs(shake) * 0.2f, -shake * 0.5f);
                transform.localRotation = restingRotation * Quaternion.Euler(shake * 70f, shake * 110f, shake * 45f);
                yield return null;
            }

            transform.localPosition = restingPosition;
            transform.localRotation = restingRotation;
            interactionRunning = false;
        }

        private void SpawnLooseStones(int count)
        {
            if (!IslandInteractionUtility.TryGetCompositeBounds(transform, out Bounds bounds))
            {
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            Renderer referenceRenderer = renderers.Length > 0 ? renderers[0] : null;
            Material stoneMaterial = referenceRenderer != null ? referenceRenderer.sharedMaterial : null;
            Color tint = IslandInteractionUtility.ResolveRendererColor(referenceRenderer, new Color(0.52f, 0.5f, 0.46f));

            for (int i = 0; i < count; i++)
            {
                PrimitiveType primitiveType = Random.value > 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere;
                GameObject stone = IslandInteractionUtility.CreateMeshObject("LooseStone", primitiveType, stoneMaterial);
                Renderer stoneRenderer = stone.GetComponent<Renderer>();
                IslandInteractionUtility.ApplyTint(stoneRenderer, Color.Lerp(tint, new Color(0.68f, 0.66f, 0.61f), Random.Range(0f, 0.35f)));

                Vector3 lateral = Random.insideUnitSphere;
                lateral.y = Mathf.Abs(lateral.y) * 0.35f;
                lateral.Normalize();

                stone.transform.position = bounds.center + new Vector3(
                    Random.Range(-bounds.extents.x * 0.35f, bounds.extents.x * 0.35f),
                    Random.Range(bounds.extents.y * 0.35f, bounds.extents.y * 0.9f),
                    Random.Range(-bounds.extents.z * 0.35f, bounds.extents.z * 0.35f));
                stone.transform.rotation = Random.rotation;

                float size = Random.Range(0.12f, 0.24f);
                stone.transform.localScale = Vector3.one * size;

                if (primitiveType == PrimitiveType.Cube)
                {
                    stone.AddComponent<BoxCollider>();
                }
                else
                {
                    stone.AddComponent<SphereCollider>();
                }

                Rigidbody rigidbody = stone.AddComponent<Rigidbody>();
                rigidbody.mass = 0.28f;
                rigidbody.linearDamping = 0.08f;
                rigidbody.angularDamping = 0.12f;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.AddForce((lateral * Random.Range(0.75f, 1.35f)) + (Vector3.up * Random.Range(0.15f, 0.42f)), ForceMode.Impulse);

                IslandTimedDestroy cleanup = stone.AddComponent<IslandTimedDestroy>();
                cleanup.Configure(16f);
            }
        }
    }
}
