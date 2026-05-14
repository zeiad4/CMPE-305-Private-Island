using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandTransientScaler : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.5f;
        [SerializeField] private Vector3 endScale = Vector3.zero;
        [SerializeField] private Vector3 driftVelocity = Vector3.zero;
        [SerializeField] private Vector3 spinSpeed = Vector3.zero;

        private Vector3 startScale;
        private float elapsed;
        private bool configured;

        public void Configure(Vector3 targetScale, float duration, Vector3 drift, Vector3 spin)
        {
            startScale = transform.localScale;
            endScale = targetScale;
            lifetime = Mathf.Max(0.05f, duration);
            driftVelocity = drift;
            spinSpeed = spin;
            elapsed = 0f;
            configured = true;
        }

        private void OnEnable()
        {
            if (!configured)
            {
                startScale = transform.localScale;
            }
        }

        private void Update()
        {
            float duration = Mathf.Max(lifetime, 0.05f);
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            transform.position += driftVelocity * Time.deltaTime;
            transform.Rotate(spinSpeed * Time.deltaTime, Space.Self);

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
