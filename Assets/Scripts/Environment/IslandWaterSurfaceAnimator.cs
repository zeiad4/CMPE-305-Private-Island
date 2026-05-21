using UnityEngine;

namespace PrivateIsland
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class IslandWaterSurfaceAnimator : MonoBehaviour
    {
        [SerializeField] private float calmAmplitude = 0.1f;
        [SerializeField] private float stormAmplitude = 0.24f;
        [SerializeField] private float waveSpeed = 0.85f;
        [SerializeField] private float secondaryWaveSpeed = 1.33f;
        [SerializeField] private float waveFrequency = 0.06f;
        [SerializeField] private float secondaryWaveFrequency = 0.09f;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh animatedMesh;
        private Vector3[] baseVertices;
        private Vector3[] animatedVertices;
        private WorldEnvironmentManager environmentManager;
        private Color calmColor = new Color(0.16f, 0.56f, 0.7f, 1f);
        private Color stormColor = new Color(0.11f, 0.34f, 0.44f, 1f);
        private float radius = 1f;

        public void Configure(float waterRadius)
        {
            radius = Mathf.Max(1f, waterRadius);
        }

        private void OnEnable()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            environmentManager = FindAnyObjectByType<WorldEnvironmentManager>(FindObjectsInactive.Exclude);
            EnsureMeshInstance();
            ApplyColor(0f);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureMeshInstance();
            if (animatedMesh == null || baseVertices == null || animatedVertices == null)
            {
                return;
            }

            float weatherBlend = ResolveWeatherBlend();
            float amplitude = Mathf.Lerp(calmAmplitude, stormAmplitude, weatherBlend);
            float time = Time.time;

            for (int i = 0; i < baseVertices.Length; i++)
            {
                Vector3 vertex = baseVertices[i];
                if (i == 0)
                {
                    animatedVertices[i] = vertex;
                    continue;
                }

                float radialBlend = Mathf.Clamp01(new Vector2(vertex.x, vertex.z).magnitude / radius);
                float ripple = Mathf.Sin((vertex.x * waveFrequency) + (time * waveSpeed));
                float crossRipple = Mathf.Sin((vertex.z * secondaryWaveFrequency) - (time * secondaryWaveSpeed));
                float chop = Mathf.Sin(((vertex.x + vertex.z) * 0.045f) + (time * 1.9f));
                float displacement = (ripple * 0.56f) + (crossRipple * 0.3f) + (chop * 0.14f);

                vertex.y = displacement * amplitude * Mathf.Lerp(0.25f, 1f, radialBlend);
                animatedVertices[i] = vertex;
            }

            animatedMesh.vertices = animatedVertices;
            animatedMesh.RecalculateNormals();
            animatedMesh.RecalculateBounds();
            ApplyColor(weatherBlend);
        }

        private void EnsureMeshInstance()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            if (animatedMesh != null && meshFilter.sharedMesh == animatedMesh && baseVertices != null)
            {
                return;
            }

            animatedMesh = Instantiate(meshFilter.sharedMesh);
            animatedMesh.name = $"{meshFilter.sharedMesh.name} Animated";
            meshFilter.sharedMesh = animatedMesh;
            baseVertices = animatedMesh.vertices;
            animatedVertices = new Vector3[baseVertices.Length];
            baseVertices.CopyTo(animatedVertices, 0);
        }

        private float ResolveWeatherBlend()
        {
            if (environmentManager == null)
            {
                environmentManager = FindAnyObjectByType<WorldEnvironmentManager>(FindObjectsInactive.Exclude);
            }

            if (environmentManager == null)
            {
                return 0f;
            }

            return environmentManager.CurrentWeather switch
            {
                WeatherType.Thunderstorm => 1f,
                WeatherType.Rain => 0.55f,
                _ => 0f
            };
        }

        private void ApplyColor(float weatherBlend)
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            Material material = meshRenderer != null ? meshRenderer.sharedMaterial : null;
            if (material == null)
            {
                return;
            }

            Color surfaceColor = Color.Lerp(calmColor, stormColor, weatherBlend);
            material.SetColor("_BaseColor", surfaceColor);
            material.SetColor("_Color", surfaceColor);
            material.SetColor("_EmissionColor", Color.Lerp(new Color(0.08f, 0.28f, 0.33f) * 0.3f, new Color(0.05f, 0.16f, 0.21f) * 0.45f, weatherBlend));
        }
    }
}
