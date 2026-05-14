using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandTimedDestroy : MonoBehaviour
    {
        [SerializeField] private float lifetime = 12f;
        private float destroyAt;

        public void Configure(float duration)
        {
            lifetime = Mathf.Max(0.05f, duration);
            destroyAt = Time.time + lifetime;
        }

        private void OnEnable()
        {
            destroyAt = Time.time + lifetime;
        }

        private void Update()
        {
            if (Application.isPlaying && Time.time >= destroyAt)
            {
                Destroy(gameObject);
            }
        }
    }
}
