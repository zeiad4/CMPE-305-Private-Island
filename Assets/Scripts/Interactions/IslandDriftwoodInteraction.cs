using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandDriftwoodInteraction : IslandInteractable
    {
        [SerializeField] private float cooldown = 1.6f;

        private float nextInteractionTime;
        private Rigidbody driftwoodBody;
        private Material sandEffectMaterial;

        public void Configure(float interactionRadius)
        {
            SetInteractionPrompt("Press F to roll the driftwood");
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(0.45f);
        }

        private void Awake()
        {
            sandEffectMaterial = IslandInteractionUtility.CreateLitMaterial("Driftwood Sand Effect", new Color(0.85f, 0.79f, 0.64f), 0.04f);
        }

        public override bool CanInteract(Transform interactor)
        {
            return Time.time >= nextInteractionTime;
        }

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            nextInteractionTime = Time.time + cooldown;
            driftwoodBody ??= PrepareRigidbody();

            Vector3 pushDirection = interactor != null
                ? (transform.position - interactor.position).normalized
                : transform.right;
            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude < 0.01f)
            {
                pushDirection = transform.right;
            }

            pushDirection.Normalize();
            driftwoodBody.AddForce((pushDirection * 1.6f) + (Vector3.up * 0.18f), ForceMode.Impulse);
            driftwoodBody.AddTorque((transform.forward * 1.8f) + (Vector3.up * 0.65f), ForceMode.Impulse);
            SpawnSandPuffs(pushDirection);
        }

        private Rigidbody PrepareRigidbody()
        {
            CapsuleCollider collider = GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CapsuleCollider>();
                collider.direction = 1;
                collider.center = Vector3.zero;
                collider.radius = 0.5f;
                collider.height = 2f;
            }

            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            rigidbody.mass = 1.4f;
            rigidbody.linearDamping = 0.9f;
            rigidbody.angularDamping = 1.1f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            return rigidbody;
        }

        private void SpawnSandPuffs(Vector3 pushDirection)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject puff = IslandInteractionUtility.CreateMeshObject("SandPuff", PrimitiveType.Sphere, sandEffectMaterial);
                puff.transform.position = transform.position + new Vector3(Random.Range(-0.12f, 0.12f), 0.12f, Random.Range(-0.12f, 0.12f));
                puff.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);

                IslandTransientScaler scaler = puff.AddComponent<IslandTransientScaler>();
                Vector3 drift = (pushDirection * Random.Range(0.35f, 0.65f)) + new Vector3(Random.Range(-0.18f, 0.18f), 0.2f, Random.Range(-0.18f, 0.18f));
                scaler.Configure(new Vector3(0.42f, 0.05f, 0.42f), 0.45f, drift, new Vector3(0f, Random.Range(-50f, 50f), 0f));
            }
        }
    }
}
