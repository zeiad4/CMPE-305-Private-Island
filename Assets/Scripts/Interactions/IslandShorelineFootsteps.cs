using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandShorelineFootsteps : MonoBehaviour
    {
        [SerializeField] private float splashBandHeight = 0.55f;
        [SerializeField] private float minimumStepSpeed = 1.1f;
        [SerializeField] private float baseSpawnInterval = 0.18f;

        private float islandSize;
        private float peakHeight;
        private float seaLevel;
        private float nextSpawnTime;
        private IslandCharacterController playerController;
        private Material rippleMaterial;
        private Material dropletMaterial;

        public void Configure(float terrainSize, float terrainPeakHeight, float terrainSeaLevel)
        {
            islandSize = terrainSize;
            peakHeight = terrainPeakHeight;
            seaLevel = terrainSeaLevel;
        }

        private void Awake()
        {
            playerController = GetComponent<IslandCharacterController>();
            rippleMaterial = IslandInteractionUtility.CreateLitMaterial("Shoreline Ripple", new Color(0.68f, 0.87f, 0.94f), 0.7f);
            dropletMaterial = IslandInteractionUtility.CreateLitMaterial("Shoreline Droplet", new Color(0.84f, 0.95f, 1f), 0.65f);
        }

        private void Update()
        {
            if (!Application.isPlaying || playerController == null)
            {
                return;
            }

            Vector3 velocity = playerController.CurrentVelocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            if (speed < minimumStepSpeed || Time.time < nextSpawnTime)
            {
                return;
            }

            float groundHeight = IslandMeshBuilder.SampleHeight(transform.position.x, transform.position.z, islandSize, peakHeight);
            if (groundHeight < seaLevel - 0.05f || groundHeight > seaLevel + splashBandHeight)
            {
                return;
            }

            SpawnSplash(groundHeight, velocity.normalized);
            nextSpawnTime = Time.time + Mathf.Lerp(baseSpawnInterval, 0.1f, Mathf.Clamp01(speed / 7f));
        }

        private void SpawnSplash(float groundHeight, Vector3 travelDirection)
        {
            Vector3 origin = new Vector3(transform.position.x, groundHeight + 0.03f, transform.position.z);

            GameObject ripple = IslandInteractionUtility.CreateMeshObject("WaterRipple", PrimitiveType.Cylinder, rippleMaterial);
            ripple.transform.position = origin;
            ripple.transform.localScale = new Vector3(0.18f, 0.01f, 0.18f);
            IslandTransientScaler rippleScaler = ripple.AddComponent<IslandTransientScaler>();
            rippleScaler.Configure(new Vector3(0.95f, 0.01f, 0.95f), 0.55f, Vector3.zero, Vector3.zero);

            for (int i = 0; i < 3; i++)
            {
                GameObject droplet = IslandInteractionUtility.CreateMeshObject("WaterDroplet", PrimitiveType.Sphere, dropletMaterial);
                droplet.transform.position = origin + new Vector3(Random.Range(-0.08f, 0.08f), 0.06f, Random.Range(-0.08f, 0.08f));
                droplet.transform.localScale = Vector3.one * Random.Range(0.04f, 0.065f);

                Vector3 drift = (travelDirection * Random.Range(0.15f, 0.4f)) + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.28f, 0.52f), Random.Range(-0.1f, 0.1f));
                IslandTransientScaler dropletScaler = droplet.AddComponent<IslandTransientScaler>();
                dropletScaler.Configure(Vector3.zero, 0.32f, drift, new Vector3(Random.Range(-110f, 110f), Random.Range(-110f, 110f), 0f));
            }
        }
    }
}
