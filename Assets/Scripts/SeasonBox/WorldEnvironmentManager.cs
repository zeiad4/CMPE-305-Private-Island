using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace PrivateIsland
{
    public sealed class WorldEnvironmentManager : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private const string SnowLowLayerName = "SnowLowLayer";
        private const string SnowGroundLayerName = "SnowGroundLayer";
        private const string AutumnLeafEmitterPrefix = "AutumnLeafEmitter_";

        [Header("Current State")]
        [SerializeField] private TimeOfDay currentTime = TimeOfDay.Day;
        [SerializeField] private Season currentSeason = Season.Spring;
        [SerializeField] private WeatherType currentWeather = WeatherType.Clear;

        [Header("Scene References")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Transform islandRoot;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private GameObject daySunObject;
        [SerializeField] private GameObject nightMoonObject;

        [Header("Time Lighting")]
        [SerializeField] private Vector3 daySunRotation = new Vector3(38f, -32f, 0f);
        [SerializeField] private Vector3 nightSunRotation = new Vector3(18f, -16f, 0f);
        [SerializeField] private float daySunIntensity = 1.45f;
        [SerializeField] private float nightSunIntensity = 0.25f;
        [SerializeField] private Color daySunColor = new Color(1f, 0.84f, 0.52f);
        [SerializeField] private Color nightSunColor = new Color(0.88f, 0.94f, 1f);
        [SerializeField] private Color dayAmbientColor = new Color(0.56f, 0.72f, 0.82f);
        [SerializeField] private Color nightAmbientColor = new Color(0.11f, 0.14f, 0.22f);
        [SerializeField] private GameObject[] nightLightObjects;

        [Header("Renderers")]
        [SerializeField] private Renderer[] groundRenderers;
        [SerializeField] private Renderer[] treeLeafRenderers;
        [SerializeField] private Renderer[] rockRenderers;
        [SerializeField] private Renderer[] extraSeasonRenderers;
        [SerializeField] private Renderer[] controlBoxAccentRenderers;

        [Header("Ground Materials")]
        [SerializeField] private Material springGroundMaterial;
        [SerializeField] private Material summerGroundMaterial;
        [SerializeField] private Material autumnGroundMaterial;
        [SerializeField] private Material winterGroundMaterial;
        [SerializeField] private Material wetGroundMaterial;

        [Header("Leaf Materials")]
        [SerializeField] private Material springLeafMaterial;
        [SerializeField] private Material summerLeafMaterial;
        [SerializeField] private Material autumnLeafMaterial;
        [SerializeField] private Material winterLeafMaterial;

        [Header("Extra Materials")]
        [SerializeField] private Material springExtraMaterial;
        [SerializeField] private Material summerExtraMaterial;
        [SerializeField] private Material autumnExtraMaterial;
        [SerializeField] private Material winterExtraMaterial;

        [Header("Objects")]
        [SerializeField] private GameObject[] flowerObjects;
        [SerializeField] private GameObject[] snowOverlayObjects;
        [SerializeField] private GameObject[] autumnObjects;
        [SerializeField] private GameObject[] rainOnlyObjects;
        [SerializeField] private GameObject[] cloudObjects;

        [Header("Particles And Effects")]
        [SerializeField] private GameObject rainEffect;
        [SerializeField] private GameObject winterSnowEffect;
        [FormerlySerializedAs("snowEffect")]
        [SerializeField] private GameObject thunderstormEffect;
        [SerializeField] private GameObject autumnLeavesEffect;
        [SerializeField] private Material snowParticleMaterial;
        [SerializeField] private Material autumnParticleMaterial;

        [Header("Weather Effect Behavior")]
        [SerializeField] private bool autoCreateWeatherEffectsIfMissing = true;
        [SerializeField] private bool followTargetWithWeatherEffects;
        [SerializeField] private float weatherFollowHeight = 44f;
        [SerializeField] private Vector3 weatherFollowOffset = Vector3.zero;
        [SerializeField] private float snowAccumulationRate = 0.18f;
        [SerializeField] private float snowMeltRate = 0.12f;
        [SerializeField] private float winterBaseSnowAmount = 0.35f;
        [SerializeField] private float maximumSnowOverlayScale = 1.75f;
        [SerializeField] private Vector2 islandWeatherArea = new Vector2(220f, 220f);

        [Header("Auto Binding")]
        [SerializeField] private bool autoCollectGeneratedIslandReferences = true;
        [SerializeField] private bool flowersStayActiveInSummer;

        public event Action WorldStateChanged;
        public event Action<string> WarningRaised;

        public TimeOfDay CurrentTime => currentTime;
        public Season CurrentSeason => currentSeason;
        public WeatherType CurrentWeather => currentWeather;

        private readonly Dictionary<Renderer, Material[]> originalRendererMaterials = new Dictionary<Renderer, Material[]>();
        private readonly Dictionary<Transform, Vector3> originalSnowOverlayScales = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Vector3> originalLeafScales = new Dictionary<Transform, Vector3>();
        private readonly HashSet<string> issuedWarnings = new HashSet<string>();
        private readonly List<GameObject> runtimeAutumnLeafObjects = new List<GameObject>();

        private MaterialPropertyBlock propertyBlock;
        private Material cachedSunMaterial;
        private Material cachedMoonMaterial;
        private Material runtimeSnowParticleMaterial;
        private Material thunderBoltMaterial;
        private Mesh cachedAutumnLeafMesh;
        private Vector3 cachedAutumnLeafScale = new Vector3(0.04f, 0.18f, 0.05f);
        private readonly List<Transform> thunderBoltRoots = new List<Transform>();
        private bool runtimeEffectsCreated;
        private float snowAccumulationAmount;
        private float nextAutumnLeafFallTimer;
        private float nextThunderStrikeTimer;
        private float thunderStrikeVisibleTimer;
        private Light thunderFlashLight;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                RefreshSceneReferences();
                ApplyWorldState();
            }
        }

        private IEnumerator Start()
        {
            yield return null;
            RefreshSceneReferences();
            ApplyWorldState();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            UpdateSnowAccumulation(Time.deltaTime);
            UpdateAutumnLeafFall(Time.deltaTime);
            UpdateThunderstorm(Time.deltaTime);
        }

        private void LateUpdate()
        {
            UpdateWeatherEffectPosition();
        }

        [ContextMenu("Refresh Scene References")]
        public void RefreshSceneReferences()
        {
            ResolveSceneReferences();
            SanitizeArrays();
            RemoveCustomCelestialObjects();

            if (autoCollectGeneratedIslandReferences)
            {
                AutoCollectGeneratedIslandReferences();
            }

            EnsureFlowerPickups();
            RebuildRuntimeAutumnLeafPiles();
            SanitizeArrays();
            CacheOriginalMaterials();
            EnsureWeatherEffects();
        }

        [ContextMenu("Apply World State")]
        public void ApplyWorldState()
        {
            NormalizeWorldState();
            RefreshSceneReferences();
            ApplyTime();
            ApplySeason();
            ApplyWeather();
            WorldStateChanged?.Invoke();
        }

        public void SetDay()
        {
            currentTime = TimeOfDay.Day;

            if (currentWeather == WeatherType.Thunderstorm)
            {
                currentWeather = WeatherType.Rain;
                ShowWarning("Thunderstorm is only available at night, so the weather was changed to rain.");
            }

            ApplyWorldState();
        }

        public void SetNight()
        {
            currentTime = TimeOfDay.Night;
            ApplyWorldState();
        }

        public void SetSpring()
        {
            currentSeason = Season.Spring;
            ApplyWorldState();
        }

        public void SetSummer()
        {
            currentSeason = Season.Summer;
            ApplyWorldState();
        }

        public void SetAutumn()
        {
            currentSeason = Season.Autumn;
            ApplyWorldState();
        }

        public void SetWinter()
        {
            currentSeason = Season.Winter;
            ApplyWorldState();
        }

        public void SetClearWeather()
        {
            currentWeather = WeatherType.Clear;
            ApplyWorldState();
        }

        public void SetRainWeather()
        {
            currentWeather = WeatherType.Rain;
            ApplyWorldState();
        }

        public void SetThunderstormWeather()
        {
            if (currentTime != TimeOfDay.Night)
            {
                ShowWarning("Thunderstorm can only be selected at night.");
                return;
            }

            currentWeather = WeatherType.Thunderstorm;
            ApplyWorldState();
        }

        public void ApplyTime()
        {
            ResolveSceneReferences();

            if (sunLight == null)
            {
                WarnOnce("MissingSunLight", $"{nameof(WorldEnvironmentManager)} could not find a directional light for day/night switching.");
                return;
            }

            bool isDay = currentTime == TimeOfDay.Day;
            sunLight.transform.rotation = Quaternion.Euler(isDay ? daySunRotation : nightSunRotation);
            sunLight.intensity = isDay ? daySunIntensity : nightSunIntensity;
            sunLight.color = isDay ? daySunColor : nightSunColor;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = isDay ? 0.92f : 0.55f;
            RenderSettings.sun = sunLight;

            Color ambientColor = isDay ? dayAmbientColor : nightAmbientColor;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;

            Color fogColor = isDay ? new Color(0.63f, 0.78f, 0.86f) : new Color(0.06f, 0.09f, 0.16f);
            if (currentWeather == WeatherType.Rain)
            {
                fogColor = Color.Lerp(fogColor, new Color(0.34f, 0.39f, 0.44f), isDay ? 0.38f : 0.25f);
            }
            else if (currentWeather == WeatherType.Thunderstorm)
            {
                fogColor = Color.Lerp(fogColor, new Color(0.18f, 0.22f, 0.28f), isDay ? 0.62f : 0.44f);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = isDay ? 90f : 35f;
            RenderSettings.fogEndDistance = isDay ? 360f : 210f;
            RenderSettings.haloStrength = 0f;
            RenderSettings.flareStrength = 0f;

            Material skybox = RenderSettings.skybox;
            if (skybox != null)
            {
                if (skybox.HasProperty("_SunDisk"))
                {
                    skybox.SetFloat("_SunDisk", 2f);
                }

                if (skybox.HasProperty("_SunSize"))
                {
                    skybox.SetFloat("_SunSize", isDay ? 0.048f : 0.058f);
                }

                if (skybox.HasProperty("_SunSizeConvergence"))
                {
                    skybox.SetFloat("_SunSizeConvergence", isDay ? 5.2f : 4.2f);
                }

                if (skybox.HasProperty("_SkyTint"))
                {
                    skybox.SetColor("_SkyTint", isDay ? new Color(0.56f, 0.66f, 0.82f) : new Color(0.06f, 0.08f, 0.17f));
                }

                if (skybox.HasProperty("_GroundColor"))
                {
                    skybox.SetColor("_GroundColor", isDay ? new Color(0.28f, 0.23f, 0.18f) : new Color(0.02f, 0.03f, 0.06f));
                }

                if (skybox.HasProperty("_Exposure"))
                {
                    skybox.SetFloat("_Exposure", isDay ? 1.08f : 0.74f);
                }

                if (skybox.HasProperty("_AtmosphereThickness"))
                {
                    skybox.SetFloat("_AtmosphereThickness", isDay ? 0.78f : 0.46f);
                }
            }

            if (targetCamera != null)
            {
                targetCamera.backgroundColor = isDay ? new Color(0.41f, 0.69f, 0.87f) : new Color(0.04f, 0.07f, 0.13f);
            }

            UpdateSkyObjects(isDay);
            SetObjectsActive(nightLightObjects, !isDay);
        }

        public void ApplySeason()
        {
            Material seasonGroundMaterial = GetSeasonGroundMaterial(currentSeason);
            Material seasonLeafMaterial = GetSeasonLeafMaterial(currentSeason);
            Material seasonExtraMaterial = GetSeasonExtraMaterial(currentSeason);

            ApplyRendererGroup(groundRenderers, seasonGroundMaterial, GetSeasonGroundTint(currentSeason), seasonGroundMaterial == null);
            ApplyRendererGroup(treeLeafRenderers, seasonLeafMaterial, GetSeasonLeafTint(currentSeason), seasonLeafMaterial == null);
            ApplyRendererGroup(extraSeasonRenderers, seasonExtraMaterial, GetSeasonExtraTint(currentSeason), seasonExtraMaterial == null);
            ApplyRendererGroup(controlBoxAccentRenderers, seasonExtraMaterial, GetSeasonExtraTint(currentSeason), seasonExtraMaterial == null);

            ApplyTintOnlyGroup(rockRenderers, GetSeasonRockTint(currentSeason));

            bool showFlowers = currentSeason == Season.Spring || (flowersStayActiveInSummer && currentSeason == Season.Summer);
            SetObjectsActive(flowerObjects, showFlowers);
            SetObjectsActive(autumnObjects, currentSeason == Season.Autumn);
            SetObjectsActive(snowOverlayObjects, currentSeason == Season.Winter);
            ApplyAutumnLeafShedding();
            ConfigureAutumnLeavesEffect(autumnLeavesEffect);

            if (currentSeason == Season.Autumn)
            {
                PlayEffect(autumnLeavesEffect);
            }
            else
            {
                StopEffect(autumnLeavesEffect);
            }
        }

        public void ApplyWeather()
        {
            bool winterSnowActive = ShouldPlayWinterSnow();
            bool rainActive = currentWeather == WeatherType.Rain;
            bool thunderstormActive = currentWeather == WeatherType.Thunderstorm;
            bool rainLikeWeather = rainActive || thunderstormActive;
            bool cloudsActive = rainLikeWeather;

            ApplyGroundMaterialForCurrentState(rainLikeWeather, winterSnowActive);

            SetObjectsActive(rainOnlyObjects, rainLikeWeather);
            SetObjectsActive(cloudObjects, cloudsActive);
            SetObjectsActive(snowOverlayObjects, currentSeason == Season.Winter);
            UpdateWeatherEffectAppearance();
            ApplySnowAccumulationVisuals();

            if (winterSnowActive)
            {
                PlayEffect(EnsureWinterSnowEffectObject());
            }
            else
            {
                StopEffect(winterSnowEffect);
            }

            if (rainLikeWeather)
            {
                PlayEffect(rainEffect);
            }
            else
            {
                StopEffect(rainEffect);
            }

            if (thunderstormActive)
            {
                GameObject activeThunderstormEffect = EnsureThunderstormEffectObject();
                ConfigureThunderstormEffect(activeThunderstormEffect);
                PlayEffect(activeThunderstormEffect);
            }
            else
            {
                StopEffect(thunderstormEffect);
                HideThunderstormStrike();
            }
        }

        private bool ShouldPlayWinterSnow()
        {
            return currentSeason == Season.Winter &&
                currentTime == TimeOfDay.Night &&
                currentWeather == WeatherType.Clear;
        }

        public string GetCurrentStateText()
        {
            return $"Current: {currentTime} / {currentSeason} / {currentWeather}";
        }

        public void ShowWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                WarningRaised?.Invoke(message);
            }
        }

        public void PlayEffect(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.SetActive(true);
            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem != null)
                {
                    particleSystem.Play(true);
                }
            }
        }

        public void StopEffect(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                if (particleSystem != null)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            effect.SetActive(false);
        }

        private void NormalizeWorldState()
        {
            if (currentTime != TimeOfDay.Night && currentWeather == WeatherType.Thunderstorm)
            {
                currentWeather = WeatherType.Rain;
            }
        }

        private void ResolveSceneReferences()
        {
            if (sunLight == null)
            {
                sunLight = RenderSettings.sun;
            }

            if (sunLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        sunLight = light;
                        break;
                    }
                }
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                targetCamera = FindAnyObjectByType<Camera>();
            }

            if (islandRoot == null)
            {
                IslandSceneBootstrap bootstrap = FindAnyObjectByType<IslandSceneBootstrap>();
                if (bootstrap != null)
                {
                    islandRoot = bootstrap.transform.Find("Island");
                }
            }

            if (islandRoot == null)
            {
                GameObject fallbackIsland = GameObject.Find("Island");
                if (fallbackIsland != null)
                {
                    islandRoot = fallbackIsland.transform;
                }
            }

            if (islandRoot == null)
            {
                WarnOnce("MissingIslandRoot", $"{nameof(WorldEnvironmentManager)} could not find the generated island root. Seasonal visuals will wait until the island exists.");
            }
        }

        private void SanitizeArrays()
        {
            groundRenderers = RemoveNullEntries(groundRenderers);
            treeLeafRenderers = RemoveNullEntries(treeLeafRenderers);
            rockRenderers = RemoveNullEntries(rockRenderers);
            extraSeasonRenderers = RemoveNullEntries(extraSeasonRenderers);
            controlBoxAccentRenderers = RemoveNullEntries(controlBoxAccentRenderers);

            flowerObjects = RemoveNullEntries(flowerObjects);
            snowOverlayObjects = RemoveNullEntries(snowOverlayObjects);
            autumnObjects = RemoveNullEntries(autumnObjects);
            rainOnlyObjects = RemoveNullEntries(rainOnlyObjects);
            cloudObjects = RemoveNullEntries(cloudObjects);
            nightLightObjects = RemoveNullEntries(nightLightObjects);
        }

        private void AutoCollectGeneratedIslandReferences()
        {
            if (islandRoot == null)
            {
                return;
            }

            List<Renderer> autoGround = new List<Renderer>();
            List<Renderer> autoLeaves = new List<Renderer>();
            List<Renderer> autoRocks = new List<Renderer>();

            Renderer[] renderers = islandRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (IsGroundRenderer(renderer))
                {
                    autoGround.Add(renderer);
                }

                if (IsLeafRenderer(renderer))
                {
                    autoLeaves.Add(renderer);
                }

                if (IsRockRenderer(renderer))
                {
                    autoRocks.Add(renderer);
                }
            }

            groundRenderers = MergeUnique(groundRenderers, autoGround);
            treeLeafRenderers = MergeUnique(treeLeafRenderers, autoLeaves);
            rockRenderers = MergeUnique(rockRenderers, autoRocks);
        }

        private void EnsureFlowerPickups()
        {
            if (!Application.isPlaying || flowerObjects == null || flowerObjects.Length == 0)
            {
                return;
            }

            foreach (GameObject flowerObject in flowerObjects)
            {
                if (flowerObject == null)
                {
                    continue;
                }

                if (flowerObject.GetComponent<IslandFlowerClusterPickup>() == null)
                {
                    flowerObject.AddComponent<IslandFlowerClusterPickup>();
                }
            }
        }

        private void RebuildRuntimeAutumnLeafPiles()
        {
            ClearRuntimeAutumnLeafPiles();

            if (islandRoot == null)
            {
                return;
            }

            Transform propsRoot = islandRoot.Find("Props");
            if (propsRoot == null)
            {
                GameObject propsObject = GameObject.Find("Props");
                if (propsObject != null)
                {
                    propsRoot = propsObject.transform;
                }
            }

            if (propsRoot == null)
            {
                return;
            }

            Material leafMaterial = autumnExtraMaterial != null ? autumnExtraMaterial : autumnGroundMaterial;
            if (leafMaterial == null)
            {
                return;
            }

            GameObject rootObject = new GameObject("RuntimeAutumnLeaves");
            rootObject.transform.SetParent(transform, false);
            runtimeAutumnLeafObjects.Add(rootObject);

            int seed = 0;
            foreach (Transform child in propsRoot)
            {
                if (child == null)
                {
                    continue;
                }

                if (child.name.Contains("Palm", StringComparison.OrdinalIgnoreCase))
                {
                    CreateRuntimeLeafPilesAroundSource(rootObject.transform, child, leafMaterial, ref seed, 5, 0.78f, 22, 1f);
                }
                else if (child.name.Contains("Bush", StringComparison.OrdinalIgnoreCase))
                {
                    CreateRuntimeLeafPilesAroundSource(rootObject.transform, child, leafMaterial, ref seed, 3, 0.48f, 12, 0.56f);
                }
            }

            List<GameObject> merged = new List<GameObject>();
            if (autumnObjects != null)
            {
                foreach (GameObject autumnObject in autumnObjects)
                {
                    if (autumnObject != null)
                    {
                        merged.Add(autumnObject);
                    }
                }
            }

            merged.AddRange(runtimeAutumnLeafObjects);
            autumnObjects = merged.ToArray();
        }

        private void ClearRuntimeAutumnLeafPiles()
        {
            foreach (GameObject runtimeObject in runtimeAutumnLeafObjects)
            {
                if (runtimeObject == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(runtimeObject);
                }
                else
                {
                    DestroyImmediate(runtimeObject);
                }
            }

            runtimeAutumnLeafObjects.Clear();
        }

        private void CreateRuntimeLeafPilesAroundSource(
            Transform parent,
            Transform source,
            Material material,
            ref int seed,
            int pileCount,
            float radiusScale,
            int leavesPerPile,
            float pileSizeScale)
        {
            if (parent == null || source == null || pileCount <= 0)
            {
                return;
            }

            if (!TryGetCompositeBounds(source, out Bounds bounds))
            {
                bounds = new Bounds(source.position, Vector3.one);
            }

            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * radiusScale + 0.08f;
            float groundY = bounds.min.y + 0.03f;

            for (int i = 0; i < pileCount; i++)
            {
                float angle = ((360f / pileCount) * i) + ((seed * 17f) % 45f);
                Vector3 radialOffset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, 0f, 0f);
                Vector3 worldPosition = new Vector3(bounds.center.x + radialOffset.x, groundY, bounds.center.z + radialOffset.z);
                CreateRuntimeLeafPile(parent, source, worldPosition, material, seed++, leavesPerPile, pileSizeScale);
            }
        }

        private void CreateRuntimeLeafPile(Transform parent, Transform source, Vector3 worldPosition, Material material, int seed, int leafCount, float pileSizeScale)
        {
            GameObject pile = new GameObject($"RuntimeLeafPile_{seed}");
            pile.transform.SetParent(parent, false);
            pile.transform.position = worldPosition;

            TryGetAutumnLeafTemplate(source, out Mesh leafMesh, out Vector3 leafScale);
            Vector3 pileLeafScale = new Vector3(
                Mathf.Max(0.014f, leafScale.x * pileSizeScale),
                Mathf.Max(0.042f, leafScale.y * pileSizeScale * 0.62f),
                Mathf.Max(0.016f, leafScale.z * pileSizeScale));

            Color[] leafColors =
            {
                new Color(0.78f, 0.36f, 0.12f),
                new Color(0.86f, 0.52f, 0.16f),
                new Color(0.88f, 0.66f, 0.2f),
                new Color(0.66f, 0.24f, 0.1f)
            };

            int clampedLeafCount = Mathf.Max(6, leafCount);
            for (int i = 0; i < clampedLeafCount; i++)
            {
                GameObject leaf = new GameObject($"RuntimeLeaf_{i}", typeof(MeshFilter), typeof(MeshRenderer));
                leaf.name = $"RuntimeLeaf_{i}";
                leaf.transform.SetParent(pile.transform, false);
                leaf.transform.localPosition = new Vector3(
                    Mathf.Sin((seed * 0.37f) + (i * 1.11f)) * (0.92f * pileSizeScale),
                    0.015f + (i * 0.0022f),
                    Mathf.Cos((seed * 0.23f) + (i * 0.96f)) * (0.78f * pileSizeScale));
                leaf.transform.localRotation = Quaternion.Euler(
                    82f + ((i % 3) * 4f),
                    (seed * 17f) + (i * 19f),
                    -26f + ((i % 5) * 9f));
                leaf.transform.localScale = new Vector3(
                    pileLeafScale.x * (0.9f + ((i % 4) * 0.08f)),
                    pileLeafScale.y * (0.9f + ((i % 3) * 0.06f)),
                    pileLeafScale.z * (0.92f + ((i % 5) * 0.05f)));

                MeshFilter meshFilter = leaf.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    meshFilter.sharedMesh = leafMesh;
                }

                Renderer renderer = leaf.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;

                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    Color tint = leafColors[(seed + i) % leafColors.Length];
                    block.SetColor(BaseColorId, tint);
                    block.SetColor(ColorId, tint);
                    renderer.SetPropertyBlock(block);
                }

            }

            runtimeAutumnLeafObjects.Add(pile);
        }

        private void CacheOriginalMaterials()
        {
            CacheOriginalMaterials(groundRenderers);
            CacheOriginalMaterials(treeLeafRenderers);
            CacheOriginalMaterials(rockRenderers);
            CacheOriginalMaterials(extraSeasonRenderers);
            CacheOriginalMaterials(controlBoxAccentRenderers);
            CacheSnowOverlayScales();
            CacheLeafScales();
        }

        private void CacheOriginalMaterials(Renderer[] renderers)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || originalRendererMaterials.ContainsKey(renderer))
                {
                    continue;
                }

                originalRendererMaterials[renderer] = renderer.sharedMaterials;
            }
        }

        private void EnsureWeatherEffects()
        {
            if (!Application.isPlaying || runtimeEffectsCreated || !autoCreateWeatherEffectsIfMissing)
            {
                return;
            }

            if (rainEffect == null)
            {
                rainEffect = CreateRainEffect();
            }

            if (winterSnowEffect == null)
            {
                winterSnowEffect = CreateWinterSnowEffect();
            }

            EnsureThunderstormEffectObject();

            if (autumnLeavesEffect == null)
            {
                autumnLeavesEffect = CreateAutumnLeavesEffect();
            }

            runtimeEffectsCreated = true;
            UpdateWeatherEffectAppearance();
        }

        private GameObject CreateRainEffect()
        {
            GameObject rainObject = new GameObject("SeasonBox Rain Effect");
            rainObject.transform.SetParent(transform, false);
            rainObject.transform.position = new Vector3(0f, weatherFollowHeight, 0f);

            ParticleSystem particleSystem = rainObject.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1.7f;
            main.startSpeed = 0f;
            main.startSize = 0.06f;
            main.startColor = new Color(0.78f, 0.88f, 1f, 0.78f);
            main.maxParticles = 6000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particleSystem.emission;
            emission.rateOverTime = 5200f;

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(islandWeatherArea.x, 1f, islandWeatherArea.y);

            var velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(-26f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            ParticleSystemRenderer renderer = rainObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 8f;
            renderer.velocityScale = 0.18f;

            rainObject.SetActive(false);
            return rainObject;
        }

        private GameObject CreateThunderstormEffect()
        {
            GameObject stormObject = new GameObject("SeasonBox Thunderstorm Effect");
            stormObject.transform.SetParent(transform, false);
            stormObject.SetActive(false);
            return stormObject;
        }

        private GameObject CreateWinterSnowEffect()
        {
            GameObject snowObject = new GameObject("SeasonBox Winter Snow Effect");
            snowObject.transform.SetParent(transform, false);
            snowObject.transform.position = new Vector3(0f, weatherFollowHeight, 0f);

            ParticleSystem particleSystem = snowObject.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 18f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
            main.startColor = new Color(1f, 1f, 1f, 0.98f);
            main.maxParticles = 17000;
            main.gravityModifier = 0.02f;

            var emission = particleSystem.emission;
            emission.rateOverTime = 900f;

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(islandWeatherArea.x, 18f, islandWeatherArea.y);

            var velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(-2.2f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            var noise = particleSystem.noise;
            noise.enabled = true;
            noise.separateAxes = true;
            noise.strengthX = new ParticleSystem.MinMaxCurve(0.26f);
            noise.strengthY = new ParticleSystem.MinMaxCurve(0.03f);
            noise.strengthZ = new ParticleSystem.MinMaxCurve(0.26f);
            noise.frequency = 0.12f;

            ParticleSystemRenderer renderer = snowObject.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sharedMaterial = ResolveSnowParticleMaterial();
            }

            snowObject.SetActive(false);
            return snowObject;
        }

        private GameObject CreateAutumnLeavesEffect()
        {
            GameObject leavesObject = new GameObject("SeasonBox Autumn Leaves Effect");
            leavesObject.transform.SetParent(transform, false);

            ParticleSystem particleSystem = leavesObject.AddComponent<ParticleSystem>();
            ConfigureAutumnCarrierSystem(particleSystem);

            ParticleSystemRenderer renderer = leavesObject.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.enabled = false;
            }

            leavesObject.SetActive(false);
            return leavesObject;
        }

        private void UpdateWeatherEffectPosition()
        {
            if (!Application.isPlaying || !followTargetWithWeatherEffects)
            {
                return;
            }

            Transform followTarget = GetWeatherFollowTarget();
            if (followTarget == null)
            {
                return;
            }

            Vector3 effectPosition = followTarget.position + weatherFollowOffset + (Vector3.up * weatherFollowHeight);

            if (rainEffect != null)
            {
                rainEffect.transform.position = effectPosition;
            }

            if (winterSnowEffect != null)
            {
                winterSnowEffect.transform.position = effectPosition;
            }

            if (thunderstormEffect != null)
            {
                thunderstormEffect.transform.position = effectPosition;
            }
        }

        private void UpdateWeatherEffectAppearance()
        {
            if (!followTargetWithWeatherEffects)
            {
                Vector3 islandCenter = islandRoot != null ? islandRoot.position : Vector3.zero;
                Vector3 fixedPosition = islandCenter + weatherFollowOffset + (Vector3.up * weatherFollowHeight);

                if (rainEffect != null)
                {
                    rainEffect.transform.position = fixedPosition;
                }

                if (winterSnowEffect != null)
                {
                    winterSnowEffect.transform.position = fixedPosition;
                }

                if (thunderstormEffect != null)
                {
                    thunderstormEffect.transform.position = fixedPosition;
                }
            }

            ConfigureRainEffect(rainEffect);
            ConfigureWinterSnowEffect(EnsureWinterSnowEffectObject());
            ConfigureThunderstormEffect(EnsureThunderstormEffectObject());
            ConfigureAutumnLeavesEffect(autumnLeavesEffect);
        }

        private void ConfigureRainEffect(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
            Color rainColor = currentTime == TimeOfDay.Day
                ? new Color(0.7f, 0.86f, 1f, 0.78f)
                : new Color(0.6f, 0.69f, 0.82f, 0.72f);

            foreach (ParticleSystem system in systems)
            {
                if (system == null)
                {
                    continue;
                }

                var main = system.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = 1.7f;
                main.startSpeed = 0f;
                main.startSize = 0.06f;
                main.startColor = rainColor;
                main.gravityModifier = 0f;

                var shape = system.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(islandWeatherArea.x, 1f, islandWeatherArea.y);

                var velocity = system.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = new ParticleSystem.MinMaxCurve(0f);
                velocity.y = new ParticleSystem.MinMaxCurve(-26f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f);

                var noise = system.noise;
                noise.enabled = false;

                ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.renderMode = ParticleSystemRenderMode.Stretch;
                    renderer.lengthScale = 8f;
                    renderer.velocityScale = 0.18f;
                }
            }
        }

        private GameObject EnsureThunderstormEffectObject()
        {
            if (IsLegacySnowEffect(thunderstormEffect))
            {
                RemoveWeatherEffectObject(thunderstormEffect);
                thunderstormEffect = null;
            }

            if (thunderstormEffect == null)
            {
                thunderstormEffect = CreateThunderstormEffect();
            }

            return thunderstormEffect;
        }

        private GameObject EnsureWinterSnowEffectObject()
        {
            if (winterSnowEffect == null)
            {
                winterSnowEffect = CreateWinterSnowEffect();
            }

            return winterSnowEffect;
        }

        private void ConfigureWinterSnowEffect(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem system in systems)
            {
                if (system == null)
                {
                    continue;
                }

                var main = system.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.prewarm = true;
                main.startLifetime = 18f;
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
                main.startColor = new Color(1f, 1f, 1f, 0.98f);
                main.maxParticles = 17000;
                main.gravityModifier = 0.02f;

                var emission = system.emission;
                emission.enabled = true;
                emission.rateOverTime = 900f;

                var shape = system.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(islandWeatherArea.x, 18f, islandWeatherArea.y);

                var velocity = system.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = new ParticleSystem.MinMaxCurve(0f);
                velocity.y = new ParticleSystem.MinMaxCurve(-2.2f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f);

                var noise = system.noise;
                noise.enabled = true;
                noise.separateAxes = true;
                noise.strengthX = new ParticleSystem.MinMaxCurve(0.26f);
                noise.strengthY = new ParticleSystem.MinMaxCurve(0.03f);
                noise.strengthZ = new ParticleSystem.MinMaxCurve(0.26f);
                noise.frequency = 0.12f;

                ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    renderer.sharedMaterial = ResolveSnowParticleMaterial();
                }
            }
        }

        private Material ResolveSnowParticleMaterial()
        {
            if (snowParticleMaterial != null)
            {
                return snowParticleMaterial;
            }

            if (runtimeSnowParticleMaterial != null)
            {
                return runtimeSnowParticleMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            runtimeSnowParticleMaterial = new Material(shader)
            {
                name = "SeasonBox Snow Particle Material"
            };

            Color snowColor = new Color(1f, 1f, 1f, 0.98f);
            if (runtimeSnowParticleMaterial.HasProperty(BaseColorId))
            {
                runtimeSnowParticleMaterial.SetColor(BaseColorId, snowColor);
            }

            if (runtimeSnowParticleMaterial.HasProperty(ColorId))
            {
                runtimeSnowParticleMaterial.SetColor(ColorId, snowColor);
            }

            return runtimeSnowParticleMaterial;
        }

        private void ConfigureThunderstormEffect(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            Vector3 islandCenter = islandRoot != null ? islandRoot.position : Vector3.zero;
            effect.transform.position = islandCenter;
            effect.transform.rotation = Quaternion.identity;

            EnsureThunderstormLight(effect.transform);
            EnsureThunderBoltRoots(effect.transform);
        }

        private void EnsureThunderstormLight(Transform effectRoot)
        {
            if (effectRoot == null)
            {
                return;
            }

            Transform lightTransform = effectRoot.Find("ThunderFlashLight");
            if (lightTransform == null)
            {
                GameObject lightObject = new GameObject("ThunderFlashLight", typeof(Light));
                lightObject.transform.SetParent(effectRoot, false);
                lightTransform = lightObject.transform;
            }

            thunderFlashLight = lightTransform.GetComponent<Light>();
            if (thunderFlashLight == null)
            {
                thunderFlashLight = lightTransform.gameObject.AddComponent<Light>();
            }

            thunderFlashLight.type = LightType.Point;
            thunderFlashLight.color = new Color(0.78f, 0.88f, 1f);
            thunderFlashLight.range = 95f;
            thunderFlashLight.intensity = 0f;
            thunderFlashLight.shadows = LightShadows.None;
        }

        private void EnsureThunderBoltRoots(Transform effectRoot)
        {
            thunderBoltRoots.Clear();
            Material boltMaterial = GetThunderBoltMaterial();

            for (int i = 0; i < 18; i++)
            {
                string boltName = $"ThunderBolt_{i}";
                Transform boltRoot = effectRoot.Find(boltName);
                if (boltRoot == null)
                {
                    GameObject boltObject = new GameObject(boltName);
                    boltObject.transform.SetParent(effectRoot, false);
                    boltRoot = boltObject.transform;
                }

                EnsureThunderBoltSegments(boltRoot, boltMaterial);
                boltRoot.gameObject.SetActive(false);
                thunderBoltRoots.Add(boltRoot);
            }
        }

        private void EnsureThunderBoltSegments(Transform boltRoot, Material boltMaterial)
        {
            for (int i = 0; i < 8; i++)
            {
                string segmentName = $"Segment_{i}";
                Transform segment = boltRoot.Find(segmentName);
                if (segment == null)
                {
                    GameObject segmentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    segmentObject.name = segmentName;
                    segmentObject.transform.SetParent(boltRoot, false);
                    foreach (Component component in segmentObject.GetComponents<Component>())
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        if (component.GetType().Name.Contains("Collider", StringComparison.Ordinal))
                        {
                            if (Application.isPlaying)
                            {
                                Destroy(component);
                            }
                            else
                            {
                                DestroyImmediate(component);
                            }
                        }
                    }

                    segment = segmentObject.transform;
                }

                Renderer renderer = segment.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = boltMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.enabled = true;
                }

                segment.gameObject.SetActive(false);
            }
        }

        private Material GetThunderBoltMaterial()
        {
            if (thunderBoltMaterial != null)
            {
                return thunderBoltMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            thunderBoltMaterial = new Material(shader)
            {
                name = "SeasonBox Thunder Bolt Material"
            };

            Color boltColor = new Color(1f, 0.96f, 0.68f, 1f);
            if (thunderBoltMaterial.HasProperty(BaseColorId))
            {
                thunderBoltMaterial.SetColor(BaseColorId, boltColor);
            }

            if (thunderBoltMaterial.HasProperty(ColorId))
            {
                thunderBoltMaterial.SetColor(ColorId, boltColor);
            }

            if (thunderBoltMaterial.HasProperty("_Smoothness"))
            {
                thunderBoltMaterial.SetFloat("_Smoothness", 0.18f);
            }

            if (thunderBoltMaterial.HasProperty("_EmissionColor"))
            {
                thunderBoltMaterial.EnableKeyword("_EMISSION");
                thunderBoltMaterial.SetColor("_EmissionColor", new Color(3.2f, 2.9f, 1.6f, 1f));
            }

            return thunderBoltMaterial;
        }

        private void UpdateSnowAccumulation(float deltaTime)
        {
            float targetAmount = currentSeason == Season.Winter ? winterBaseSnowAmount : 0f;

            float moveRate = targetAmount > snowAccumulationAmount ? snowAccumulationRate : snowMeltRate;
            float nextAmount = Mathf.MoveTowards(snowAccumulationAmount, targetAmount, moveRate * deltaTime);
            if (Mathf.Approximately(nextAmount, snowAccumulationAmount))
            {
                return;
            }

            snowAccumulationAmount = nextAmount;
            ApplySnowAccumulationVisuals();
        }

        private void CacheSnowOverlayScales()
        {
            if (snowOverlayObjects == null)
            {
                return;
            }

            foreach (GameObject overlayObject in snowOverlayObjects)
            {
                if (overlayObject == null)
                {
                    continue;
                }

                Transform overlayTransform = overlayObject.transform;
                if (!originalSnowOverlayScales.ContainsKey(overlayTransform))
                {
                    originalSnowOverlayScales[overlayTransform] = overlayTransform.localScale;
                }
            }
        }

        private void CacheLeafScales()
        {
            if (treeLeafRenderers == null)
            {
                return;
            }

            foreach (Renderer renderer in treeLeafRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Transform targetTransform = renderer.transform;
                if (!originalLeafScales.ContainsKey(targetTransform))
                {
                    originalLeafScales[targetTransform] = targetTransform.localScale;
                }
            }
        }

        private void ApplySnowAccumulationVisuals()
        {
            if (snowOverlayObjects == null)
            {
                return;
            }

            foreach (GameObject overlayObject in snowOverlayObjects)
            {
                if (overlayObject == null)
                {
                    continue;
                }

                Transform overlayTransform = overlayObject.transform;
                if (!originalSnowOverlayScales.TryGetValue(overlayTransform, out Vector3 baseScale))
                {
                    baseScale = overlayTransform.localScale;
                    originalSnowOverlayScales[overlayTransform] = baseScale;
                }

                float widthScale = Mathf.Lerp(0.08f, maximumSnowOverlayScale, snowAccumulationAmount);
                float heightScale = Mathf.Lerp(0.04f, maximumSnowOverlayScale, snowAccumulationAmount);

                overlayTransform.localScale = new Vector3(
                    baseScale.x * widthScale,
                    baseScale.y * heightScale,
                    baseScale.z * widthScale);
            }
        }

        private void UpdateAutumnLeafFall(float deltaTime)
        {
            if (currentSeason != Season.Autumn || autumnLeavesEffect == null || !autumnLeavesEffect.activeInHierarchy)
            {
                nextAutumnLeafFallTimer = 0f;
                return;
            }

            nextAutumnLeafFallTimer -= deltaTime;
            if (nextAutumnLeafFallTimer > 0f)
            {
                return;
            }

            List<ParticleSystem> emitters = GetAutumnLeafEmitters(autumnLeavesEffect.transform);
            if (emitters.Count == 0)
            {
                nextAutumnLeafFallTimer = 2f;
                return;
            }

            int treeBurstCount = Mathf.Min(emitters.Count, UnityEngine.Random.Range(2, 4));
            List<int> availableEmitterIndices = new List<int>(emitters.Count);
            for (int i = 0; i < emitters.Count; i++)
            {
                availableEmitterIndices.Add(i);
            }

            for (int burstIndex = 0; burstIndex < treeBurstCount && availableEmitterIndices.Count > 0; burstIndex++)
            {
                int randomListIndex = UnityEngine.Random.Range(0, availableEmitterIndices.Count);
                int emitterIndex = availableEmitterIndices[randomListIndex];
                availableEmitterIndices.RemoveAt(randomListIndex);

                ParticleSystem emitter = emitters[emitterIndex];
                int leafBurstCount = UnityEngine.Random.Range(2, 4);
                emitter?.Emit(leafBurstCount);
            }

            nextAutumnLeafFallTimer = UnityEngine.Random.Range(4.4f, 5.4f);
        }

        private static List<ParticleSystem> GetAutumnLeafEmitters(Transform effectRoot)
        {
            List<ParticleSystem> emitters = new List<ParticleSystem>();
            if (effectRoot == null)
            {
                return emitters;
            }

            foreach (Transform child in effectRoot)
            {
                if (child == null || !child.name.StartsWith(AutumnLeafEmitterPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                ParticleSystem emitter = child.GetComponent<ParticleSystem>();
                if (emitter != null)
                {
                    emitters.Add(emitter);
                }
            }

            return emitters;
        }

        private void UpdateThunderstorm(float deltaTime)
        {
            if (currentWeather != WeatherType.Thunderstorm)
            {
                nextThunderStrikeTimer = 0f;
                HideThunderstormStrike();
                return;
            }

            GameObject activeThunderstormEffect = EnsureThunderstormEffectObject();
            ConfigureThunderstormEffect(activeThunderstormEffect);

            if (activeThunderstormEffect == null || !activeThunderstormEffect.activeInHierarchy)
            {
                return;
            }

            if (thunderStrikeVisibleTimer > 0f)
            {
                thunderStrikeVisibleTimer -= deltaTime;
                if (thunderStrikeVisibleTimer <= 0f)
                {
                    HideThunderstormStrike();
                }
            }

            nextThunderStrikeTimer -= deltaTime;
            if (nextThunderStrikeTimer > 0f)
            {
                return;
            }

            TriggerThunderStrike();
            nextThunderStrikeTimer = UnityEngine.Random.Range(0.58f, 1.05f);
        }

        private List<Vector3> CollectThunderCloudCenters()
        {
            List<Vector3> cloudCenters = new List<Vector3>();
            if (cloudObjects == null)
            {
                return cloudCenters;
            }

            Transform focusTarget = GetWeatherFollowTarget();
            Vector3 focusPosition = focusTarget != null ? focusTarget.position : (islandRoot != null ? islandRoot.position : Vector3.zero);

            foreach (GameObject cloudObject in cloudObjects)
            {
                if (cloudObject == null || !cloudObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 cloudPosition = cloudObject.transform.position;
                float planarDistance = Vector2.Distance(
                    new Vector2(cloudPosition.x, cloudPosition.z),
                    new Vector2(focusPosition.x, focusPosition.z));

                if (planarDistance <= 115f)
                {
                    cloudCenters.Add(cloudPosition);
                }
            }

            if (cloudCenters.Count == 0)
            {
                foreach (GameObject cloudObject in cloudObjects)
                {
                    if (cloudObject == null || !cloudObject.activeInHierarchy)
                    {
                        continue;
                    }

                    cloudCenters.Add(cloudObject.transform.position);
                }
            }

            if (cloudCenters.Count == 0)
            {
                Vector3 islandCenter = islandRoot != null ? islandRoot.position : Vector3.zero;
                cloudCenters.Add(islandCenter + new Vector3(-18f, weatherFollowHeight - 18f, -12f));
                cloudCenters.Add(islandCenter + new Vector3(12f, weatherFollowHeight - 17f, 8f));
                cloudCenters.Add(islandCenter + new Vector3(0f, weatherFollowHeight - 16f, 0f));
            }

            return cloudCenters;
        }

        private void TriggerThunderStrike()
        {
            GameObject activeThunderstormEffect = EnsureThunderstormEffectObject();
            if (activeThunderstormEffect == null)
            {
                return;
            }

            if (thunderBoltRoots.Count == 0)
            {
                EnsureThunderBoltRoots(activeThunderstormEffect.transform);
            }

            List<Vector3> cloudCenters = CollectThunderCloudCenters();
            int boltIndex = 0;
            int strikeGroupCount = Mathf.Clamp(UnityEngine.Random.Range(6, 9), 6, 8);
            int createdGroupCount = 0;
            Vector3 flashAnchor = Vector3.zero;
            Transform focusTarget = GetWeatherFollowTarget();
            Vector3 groundFocus = focusTarget != null ? focusTarget.position : (islandRoot != null ? islandRoot.position : Vector3.zero);

            for (int group = 0; group < strikeGroupCount && boltIndex < thunderBoltRoots.Count; group++)
            {
                Vector3 cloudCenter = cloudCenters[UnityEngine.Random.Range(0, cloudCenters.Count)];
                Vector3 strikeStart = cloudCenter + new Vector3(
                    UnityEngine.Random.Range(-2.4f, 2.4f),
                    UnityEngine.Random.Range(1.2f, 3.8f),
                    UnityEngine.Random.Range(-2.4f, 2.4f));

                Vector3 strikeEnd = new Vector3(
                    Mathf.Lerp(strikeStart.x, groundFocus.x, 0.55f) + UnityEngine.Random.Range(-8f, 8f),
                    groundFocus.y + UnityEngine.Random.Range(0.6f, 1.4f),
                    Mathf.Lerp(strikeStart.z, groundFocus.z, 0.55f) + UnityEngine.Random.Range(-8f, 8f));

                ConfigureThunderBolt(thunderBoltRoots[boltIndex++], strikeStart, strikeEnd, 7, 1.45f, 0.86f);

                int branchCount = Mathf.Clamp(UnityEngine.Random.Range(2, 4), 2, 3);
                for (int branch = 0; branch < branchCount && boltIndex < thunderBoltRoots.Count; branch++)
                {
                    Vector3 branchStart = Vector3.Lerp(strikeStart, strikeEnd, UnityEngine.Random.Range(0.16f, 0.42f));
                    Vector3 branchEnd = Vector3.Lerp(strikeStart, strikeEnd, UnityEngine.Random.Range(0.55f, 0.82f)) + new Vector3(
                        UnityEngine.Random.Range(-4.2f, 4.2f),
                        UnityEngine.Random.Range(-2.2f, 0.4f),
                        UnityEngine.Random.Range(-4.2f, 4.2f));
                    ConfigureThunderBolt(thunderBoltRoots[boltIndex++], branchStart, branchEnd, 5, 0.9f, 0.52f);
                }

                flashAnchor += strikeStart;
                createdGroupCount++;
            }

            for (int i = boltIndex; i < thunderBoltRoots.Count; i++)
            {
                if (thunderBoltRoots[i] != null)
                {
                    thunderBoltRoots[i].gameObject.SetActive(false);
                }
            }

            if (thunderFlashLight != null)
            {
                Vector3 flashPosition = flashAnchor / Mathf.Max(1, createdGroupCount);
                thunderFlashLight.transform.position = flashPosition;
                thunderFlashLight.intensity = currentTime == TimeOfDay.Day ? 9f : 13f;
            }

            thunderStrikeVisibleTimer = 0.92f;
        }

        private void ConfigureThunderBolt(Transform boltRoot, Vector3 start, Vector3 end, int pointCount, float branchScale, float thickness)
        {
            if (boltRoot == null)
            {
                return;
            }

            int clampedPointCount = Mathf.Max(2, pointCount);
            Vector3[] points = new Vector3[clampedPointCount];

            for (int i = 0; i < clampedPointCount; i++)
            {
                float t = i / (float)(clampedPointCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                if (i > 0 && i < clampedPointCount - 1)
                {
                    point.x += UnityEngine.Random.Range(-1.3f, 1.3f) * branchScale;
                    point.z += UnityEngine.Random.Range(-1.3f, 1.3f) * branchScale;
                    point.y += UnityEngine.Random.Range(-0.7f, 0.35f) * branchScale;
                }

                points[i] = point;
            }

            boltRoot.gameObject.SetActive(true);

            for (int i = 0; i < 8; i++)
            {
                Transform segment = boltRoot.Find($"Segment_{i}");
                if (segment == null)
                {
                    continue;
                }

                if (i >= clampedPointCount - 1)
                {
                    segment.gameObject.SetActive(false);
                    continue;
                }

                Vector3 from = points[i];
                Vector3 to = points[i + 1];
                Vector3 direction = to - from;
                float length = direction.magnitude;
                if (length <= 0.001f)
                {
                    segment.gameObject.SetActive(false);
                    continue;
                }

                segment.gameObject.SetActive(true);
                segment.position = (from + to) * 0.5f;
                segment.rotation = Quaternion.LookRotation(direction.normalized);
                segment.localScale = new Vector3(thickness, thickness, length);
            }
        }

        private void HideThunderstormStrike()
        {
            thunderStrikeVisibleTimer = 0f;

            if (thunderFlashLight != null)
            {
                thunderFlashLight.intensity = 0f;
            }

            foreach (Transform boltRoot in thunderBoltRoots)
            {
                if (boltRoot != null)
                {
                    boltRoot.gameObject.SetActive(false);
                }
            }
        }

        private void ApplyAutumnLeafShedding()
        {
            if (treeLeafRenderers == null)
            {
                return;
            }

            foreach (Renderer renderer in treeLeafRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                string objectName = renderer.gameObject.name;
                bool isLeafPiece =
                    objectName.Contains("Frond", StringComparison.OrdinalIgnoreCase) ||
                    objectName.Contains("Leaflet", StringComparison.OrdinalIgnoreCase) ||
                    objectName.Contains("LeafPad", StringComparison.OrdinalIgnoreCase) ||
                    objectName.Contains("BroadLeaf", StringComparison.OrdinalIgnoreCase) ||
                    objectName.Contains("Leaf", StringComparison.OrdinalIgnoreCase);

                if (!isLeafPiece || objectName.Contains("Spine", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool isBushLeaf = HasAncestorNamed(renderer.transform, "Bush");
                float leafScaleFactor = 1f;
                if (currentSeason == Season.Autumn)
                {
                    leafScaleFactor = isBushLeaf ? 0.04f : 0.2f;
                }

                Transform targetTransform = renderer.transform;
                if (!originalLeafScales.TryGetValue(targetTransform, out Vector3 baseScale))
                {
                    baseScale = targetTransform.localScale;
                    originalLeafScales[targetTransform] = baseScale;
                }

                targetTransform.localScale = baseScale * leafScaleFactor;
            }
        }

        private void ConfigureAutumnLeavesEffect(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            ParticleSystem carrierSystem = effect.GetComponent<ParticleSystem>();
            if (carrierSystem == null)
            {
                carrierSystem = effect.AddComponent<ParticleSystem>();
            }

            ConfigureAutumnCarrierSystem(carrierSystem);

            ParticleSystemRenderer carrierRenderer = effect.GetComponent<ParticleSystemRenderer>();
            Material particleMaterial = ResolveAutumnParticleMaterial(carrierRenderer);
            if (carrierRenderer != null)
            {
                carrierRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                carrierRenderer.enabled = false;
                if (particleMaterial != null)
                {
                    carrierRenderer.sharedMaterial = particleMaterial;
                }
            }

            List<Transform> sources = CollectAutumnLeafSources();
            SyncAutumnLeafEmitters(effect.transform, sources, particleMaterial);
        }

        private void ConfigureAutumnCarrierSystem(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.1f;
            main.startSpeed = 0f;
            main.startSize = 0f;
            main.maxParticles = 1;

            var emission = particleSystem.emission;
            emission.rateOverTime = 0f;
            emission.enabled = false;

            var shape = particleSystem.shape;
            shape.enabled = false;
        }

        private Material ResolveAutumnParticleMaterial(ParticleSystemRenderer carrierRenderer)
        {
            if (autumnLeafMaterial != null)
            {
                return autumnLeafMaterial;
            }

            if (autumnExtraMaterial != null)
            {
                return autumnExtraMaterial;
            }

            if (autumnParticleMaterial != null)
            {
                return autumnParticleMaterial;
            }

            if (carrierRenderer != null && carrierRenderer.sharedMaterial != null)
            {
                autumnParticleMaterial = carrierRenderer.sharedMaterial;
                return autumnParticleMaterial;
            }

            autumnParticleMaterial = autumnLeafMaterial != null ? autumnLeafMaterial : autumnExtraMaterial;
            return autumnParticleMaterial;
        }

        private bool TryGetAutumnLeafTemplate(out Mesh leafMesh, out Vector3 leafScale)
        {
            if (cachedAutumnLeafMesh != null)
            {
                leafMesh = cachedAutumnLeafMesh;
                leafScale = cachedAutumnLeafScale;
                return true;
            }

            Renderer[] candidateRenderers = treeLeafRenderers;
            if (candidateRenderers == null || candidateRenderers.Length == 0)
            {
                RefreshSceneReferences();
                candidateRenderers = treeLeafRenderers;
            }

            if (candidateRenderers != null)
            {
                Renderer bestRenderer = null;
                foreach (Renderer renderer in candidateRenderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    string objectName = renderer.gameObject.name;
                    bool preferredLeaf =
                        objectName.Contains("Leaflet", StringComparison.OrdinalIgnoreCase) ||
                        objectName.Contains("BroadLeaf", StringComparison.OrdinalIgnoreCase);

                    if (!preferredLeaf)
                    {
                        continue;
                    }

                    if (TryGetSharedMesh(renderer, out Mesh candidateMesh))
                    {
                        bestRenderer = renderer;
                        cachedAutumnLeafMesh = candidateMesh;
                        cachedAutumnLeafScale = renderer.transform.lossyScale;
                        break;
                    }
                }

                if (bestRenderer == null)
                {
                    foreach (Renderer renderer in candidateRenderers)
                    {
                        if (renderer != null && TryGetSharedMesh(renderer, out Mesh candidateMesh))
                        {
                            cachedAutumnLeafMesh = candidateMesh;
                            cachedAutumnLeafScale = renderer.transform.lossyScale;
                            break;
                        }
                    }
                }
            }

            leafMesh = cachedAutumnLeafMesh;
            leafScale = cachedAutumnLeafScale;
            return leafMesh != null;
        }

        private bool TryGetAutumnLeafTemplate(Transform source, out Mesh leafMesh, out Vector3 leafScale)
        {
            if (source != null)
            {
                Renderer[] sourceRenderers = source.GetComponentsInChildren<Renderer>(true);
                Renderer fallbackRenderer = null;
                foreach (Renderer renderer in sourceRenderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    string objectName = renderer.gameObject.name;
                    bool preferredLeaf =
                        objectName.Contains("BroadLeaf", StringComparison.OrdinalIgnoreCase) ||
                        objectName.Contains("Leaflet", StringComparison.OrdinalIgnoreCase) ||
                        objectName.Contains("Leaf", StringComparison.OrdinalIgnoreCase);

                    if (!preferredLeaf)
                    {
                        continue;
                    }

                    fallbackRenderer ??= renderer;
                    if (TryGetSharedMesh(renderer, out Mesh sourceMesh))
                    {
                        leafMesh = sourceMesh;
                        leafScale = renderer.transform.lossyScale;
                        return true;
                    }
                }

                if (fallbackRenderer != null && TryGetSharedMesh(fallbackRenderer, out Mesh fallbackMesh))
                {
                    leafMesh = fallbackMesh;
                    leafScale = fallbackRenderer.transform.lossyScale;
                    return true;
                }
            }

            return TryGetAutumnLeafTemplate(out leafMesh, out leafScale);
        }

        private static bool TryGetSharedMesh(Renderer renderer, out Mesh sharedMesh)
        {
            sharedMesh = null;
            if (renderer == null)
            {
                return false;
            }

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                sharedMesh = meshFilter.sharedMesh;
                return true;
            }

            SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
            if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            {
                sharedMesh = skinnedMeshRenderer.sharedMesh;
                return true;
            }

            return false;
        }

        private List<Transform> CollectAutumnLeafSources()
        {
            List<Transform> sources = new List<Transform>();

            if (islandRoot == null)
            {
                ResolveSceneReferences();
            }

            Transform propsRoot = islandRoot != null ? islandRoot.Find("Props") : null;
            if (propsRoot == null)
            {
                GameObject propsObject = GameObject.Find("Props");
                if (propsObject != null)
                {
                    propsRoot = propsObject.transform;
                }
            }

            if (propsRoot == null)
            {
                return sources;
            }

            foreach (Transform child in propsRoot)
            {
                if (child != null &&
                    (child.name.Contains("Palm", StringComparison.OrdinalIgnoreCase) ||
                     child.name.Contains("Bush", StringComparison.OrdinalIgnoreCase)))
                {
                    sources.Add(child);
                }
            }

            return sources;
        }

        private void SyncAutumnLeafEmitters(Transform effectRoot, List<Transform> sources, Material particleMaterial)
        {
            List<Transform> emitters = new List<Transform>();
            foreach (Transform child in effectRoot)
            {
                if (child != null && child.name.StartsWith(AutumnLeafEmitterPrefix, StringComparison.Ordinal))
                {
                    emitters.Add(child);
                }
            }

            for (int i = 0; i < sources.Count; i++)
            {
                Transform source = sources[i];
                if (source == null)
                {
                    continue;
                }

                Transform emitterTransform;
                if (i < emitters.Count)
                {
                    emitterTransform = emitters[i];
                }
                else
                {
                    GameObject emitterObject = new GameObject($"{AutumnLeafEmitterPrefix}{i}", typeof(ParticleSystem));
                    emitterObject.transform.SetParent(effectRoot, false);
                    emitterTransform = emitterObject.transform;
                    emitters.Add(emitterTransform);
                }

                emitterTransform.name = $"{AutumnLeafEmitterPrefix}{i}";
                ConfigureAutumnLeafEmitter(emitterTransform, source, particleMaterial, i);
            }

            for (int i = emitters.Count - 1; i >= sources.Count; i--)
            {
                Transform emitterTransform = emitters[i];
                if (emitterTransform == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(emitterTransform.gameObject);
                }
                else
                {
                    DestroyImmediate(emitterTransform.gameObject);
                }
            }
        }

        private void ConfigureAutumnLeafEmitter(Transform emitterTransform, Transform source, Material particleMaterial, int emitterIndex)
        {
            if (!TryGetCompositeBounds(source, out Bounds bounds))
            {
                bounds = new Bounds(source.position + Vector3.up, Vector3.one * 2f);
            }

            TryGetAutumnLeafTemplate(source, out Mesh leafMesh, out Vector3 leafScale);
            Vector3 fallingLeafScale = new Vector3(
                Mathf.Max(0.03f, leafScale.x * 1.02f),
                Mathf.Max(0.16f, leafScale.y * 1.02f),
                Mathf.Max(0.038f, leafScale.z * 1.02f));

            emitterTransform.position = new Vector3(bounds.center.x, bounds.max.y - 0.35f, bounds.center.z);
            emitterTransform.rotation = Quaternion.identity;
            emitterTransform.localScale = Vector3.one;

            ParticleSystem particleSystem = emitterTransform.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                particleSystem = emitterTransform.gameObject.AddComponent<ParticleSystem>();
            }

            float canopyWidth = Mathf.Clamp(bounds.size.x * 0.62f, 1.1f, 3.2f);
            float canopyDepth = Mathf.Clamp(bounds.size.z * 0.62f, 1.1f, 3.2f);
            float lifetime = Mathf.Clamp(bounds.size.y * 0.5f, 3.4f, 5.4f);

            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.34f, 0.72f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(fallingLeafScale.x * 0.92f, fallingLeafScale.x * 1.18f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(fallingLeafScale.y * 0.9f, fallingLeafScale.y * 1.15f);
            main.startSizeZ = new ParticleSystem.MinMaxCurve(fallingLeafScale.z * 0.92f, fallingLeafScale.z * 1.18f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.76f, 0.34f, 0.11f, 1f),
                new Color(0.9f, 0.66f, 0.22f, 1f));
            main.maxParticles = 18;
            main.gravityModifier = 0.28f;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(canopyWidth, 0.55f, canopyDepth);

            var velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(-1.4f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            var noise = particleSystem.noise;
            noise.enabled = true;
            noise.separateAxes = true;
            noise.strengthX = new ParticleSystem.MinMaxCurve(0.22f);
            noise.strengthY = new ParticleSystem.MinMaxCurve(0.06f);
            noise.strengthZ = new ParticleSystem.MinMaxCurve(0.22f);
            noise.frequency = 0.38f + ((emitterIndex % 4) * 0.05f);

            ParticleSystemRenderer renderer = emitterTransform.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = leafMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard;
                renderer.mesh = leafMesh;
                if (particleMaterial != null)
                {
                    renderer.sharedMaterial = particleMaterial;
                }
            }
        }

        private static bool HasAncestorNamed(Transform target, string nameFragment)
        {
            if (target == null || string.IsNullOrWhiteSpace(nameFragment))
            {
                return false;
            }

            Transform current = target;
            while (current != null)
            {
                if (current.name.Contains(nameFragment, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool TryGetCompositeBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root != null ? root.GetComponentsInChildren<Renderer>(true) : null;
            if (renderers == null || renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bool found = false;
            bounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        private void UpdateSkyObjects(bool isDay)
        {
            RemoveCelestialOverlay();
        }

        private void RemoveCelestialOverlay()
        {
            Transform overlay = FindCelestialOverlay();
            if (overlay == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(overlay.gameObject);
            }
            else
            {
                DestroyImmediate(overlay.gameObject);
            }
        }

        private static Transform FindCelestialOverlay()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null)
                {
                    continue;
                }

                Transform overlay = canvas.transform.Find("CelestialOverlay");
                if (overlay != null)
                {
                    return overlay;
                }
            }

            return null;
        }

        private bool IsLegacySnowEffect(GameObject effect)
        {
            if (effect == null)
            {
                return false;
            }

            if (effect.name.Contains("Snow", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return effect.transform.Find(SnowLowLayerName) != null || effect.transform.Find(SnowGroundLayerName) != null;
        }

        private void RemoveWeatherEffectObject(GameObject effect)
        {
            if (effect == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(effect);
            }
            else
            {
                DestroyImmediate(effect);
            }
        }

        private void RemoveCustomCelestialObjects()
        {
            RemoveCelestialObject(daySunObject);
            RemoveCelestialObject(nightMoonObject);
            daySunObject = null;
            nightMoonObject = null;
        }

        private void RemoveCelestialObject(GameObject targetObject)
        {
            if (targetObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(targetObject);
            }
            else
            {
                DestroyImmediate(targetObject);
            }
        }

        private Transform GetWeatherFollowTarget()
        {
            if (targetCamera != null)
            {
                return targetCamera.transform;
            }

            ResolveSceneReferences();
            if (targetCamera != null)
            {
                return targetCamera.transform;
            }

            IslandCharacterController playerController = FindAnyObjectByType<IslandCharacterController>();
            return playerController != null ? playerController.transform : null;
        }

        private void ApplyGroundMaterialForCurrentState(bool rainActive, bool snowActive)
        {
            Material targetGroundMaterial = null;
            bool applyTint = false;
            Color tint = GetSeasonGroundTint(currentSeason);
            bool winterRain = rainActive && currentSeason == Season.Winter;

            if (snowActive)
            {
                targetGroundMaterial = winterGroundMaterial != null ? winterGroundMaterial : GetSeasonGroundMaterial(Season.Winter);
                applyTint = targetGroundMaterial == null;
                tint = GetSeasonGroundTint(Season.Winter);
            }
            else if (winterRain)
            {
                // Winter rain keeps the island snowy; only the weather effects should change.
                targetGroundMaterial = winterGroundMaterial != null ? winterGroundMaterial : GetSeasonGroundMaterial(Season.Winter);
                applyTint = targetGroundMaterial == null;
                tint = GetSeasonGroundTint(Season.Winter);
            }
            else if (rainActive)
            {
                targetGroundMaterial = wetGroundMaterial != null ? wetGroundMaterial : GetSeasonGroundMaterial(currentSeason);
                applyTint = wetGroundMaterial == null;
                tint = GetRainGroundTint(currentSeason);
            }
            else
            {
                targetGroundMaterial = GetSeasonGroundMaterial(currentSeason);
                applyTint = targetGroundMaterial == null;
            }

            ApplyRendererGroup(groundRenderers, targetGroundMaterial, tint, applyTint);
        }

        private void ApplyRendererGroup(Renderer[] renderers, Material targetMaterial, Color tintColor, bool applyTint)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                RestoreOriginalMaterials(renderer);

                if (targetMaterial != null)
                {
                    AssignMaterialToAllSlots(renderer, targetMaterial);
                }

                if (applyTint)
                {
                    ApplyColorOverride(renderer, tintColor);
                }
                else
                {
                    ClearColorOverride(renderer);
                }
            }
        }

        private void ApplyTintOnlyGroup(Renderer[] renderers, Color tintColor)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                RestoreOriginalMaterials(renderer);
                ApplyColorOverride(renderer, tintColor);
            }
        }

        private void RestoreOriginalMaterials(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            if (!originalRendererMaterials.TryGetValue(renderer, out Material[] originalMaterials) || originalMaterials == null || originalMaterials.Length == 0)
            {
                return;
            }

            renderer.sharedMaterials = originalMaterials;
        }

        private void AssignMaterialToAllSlots(Renderer renderer, Material material)
        {
            if (renderer == null || material == null)
            {
                return;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;
            int materialCount = sharedMaterials != null && sharedMaterials.Length > 0 ? sharedMaterials.Length : 1;
            Material[] replacements = new Material[materialCount];

            for (int i = 0; i < replacements.Length; i++)
            {
                replacements[i] = material;
            }

            renderer.sharedMaterials = replacements;
        }

        private void ApplyColorOverride(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            propertyBlock.Clear();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void ClearColorOverride(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            propertyBlock.Clear();
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void SetObjectsActive(GameObject[] objects, bool shouldBeActive)
        {
            if (objects == null)
            {
                return;
            }

            foreach (GameObject targetObject in objects)
            {
                if (targetObject != null)
                {
                    targetObject.SetActive(shouldBeActive);
                }
            }
        }

        private void SetObjectActive(GameObject targetObject, bool shouldBeActive)
        {
            if (targetObject != null)
            {
                targetObject.SetActive(shouldBeActive);
            }
        }

        private Material GetSeasonGroundMaterial(Season season)
        {
            return season switch
            {
                Season.Spring => springGroundMaterial,
                Season.Summer => summerGroundMaterial,
                Season.Autumn => summerGroundMaterial != null ? summerGroundMaterial : springGroundMaterial != null ? springGroundMaterial : autumnGroundMaterial,
                Season.Winter => winterGroundMaterial,
                _ => null
            };
        }

        private Material GetSeasonLeafMaterial(Season season)
        {
            return season switch
            {
                Season.Spring => springLeafMaterial,
                Season.Summer => summerLeafMaterial,
                Season.Autumn => autumnLeafMaterial,
                Season.Winter => winterLeafMaterial,
                _ => null
            };
        }

        private Material GetSeasonExtraMaterial(Season season)
        {
            return season switch
            {
                Season.Spring => springExtraMaterial,
                Season.Summer => summerExtraMaterial,
                Season.Autumn => autumnExtraMaterial,
                Season.Winter => winterExtraMaterial,
                _ => null
            };
        }

        private static Color GetSeasonGroundTint(Season season)
        {
            return season switch
            {
                Season.Spring => new Color(0.72f, 0.95f, 0.68f),
                Season.Summer => new Color(0.5f, 0.75f, 0.42f),
                Season.Autumn => new Color(0.5f, 0.75f, 0.42f),
                Season.Winter => new Color(0.9f, 0.95f, 1f),
                _ => Color.white
            };
        }

        private static Color GetRainGroundTint(Season season)
        {
            return season switch
            {
                Season.Spring => new Color(0.45f, 0.63f, 0.42f),
                Season.Summer => new Color(0.35f, 0.52f, 0.3f),
                Season.Autumn => new Color(0.35f, 0.52f, 0.3f),
                Season.Winter => new Color(0.72f, 0.78f, 0.82f),
                _ => Color.white
            };
        }

        private static Color GetSeasonLeafTint(Season season)
        {
            return season switch
            {
                Season.Spring => new Color(0.48f, 0.86f, 0.42f),
                Season.Summer => new Color(0.27f, 0.65f, 0.26f),
                Season.Autumn => new Color(0.8f, 0.46f, 0.18f),
                Season.Winter => new Color(0.76f, 0.8f, 0.82f),
                _ => Color.white
            };
        }

        private static Color GetSeasonExtraTint(Season season)
        {
            return season switch
            {
                Season.Spring => new Color(1f, 0.98f, 0.96f),
                Season.Summer => new Color(1f, 0.96f, 0.9f),
                Season.Autumn => new Color(0.95f, 0.82f, 0.7f),
                Season.Winter => new Color(0.84f, 0.9f, 0.96f),
                _ => Color.white
            };
        }

        private static Color GetSeasonRockTint(Season season)
        {
            return season switch
            {
                Season.Spring => new Color(0.68f, 0.7f, 0.72f),
                Season.Summer => new Color(0.64f, 0.66f, 0.69f),
                Season.Autumn => new Color(0.62f, 0.63f, 0.65f),
                Season.Winter => new Color(0.8f, 0.83f, 0.87f),
                _ => Color.white
            };
        }

        private static bool IsGroundRenderer(Renderer renderer)
        {
            return renderer.gameObject.name.Contains("Terrain", StringComparison.OrdinalIgnoreCase) || HasMaterialNamed(renderer, "Terrain");
        }

        private static bool IsLeafRenderer(Renderer renderer)
        {
            return HasMaterialNamed(renderer, "Leaves");
        }

        private static bool IsRockRenderer(Renderer renderer)
        {
            return HasMaterialNamed(renderer, "Rock");
        }

        private static bool HasMaterialNamed(Renderer renderer, string materialNameFragment)
        {
            if (renderer == null || string.IsNullOrWhiteSpace(materialNameFragment))
            {
                return false;
            }

            Material[] materials = renderer.sharedMaterials;
            foreach (Material material in materials)
            {
                if (material != null && material.name.Contains(materialNameFragment, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static Renderer[] MergeUnique(Renderer[] existing, List<Renderer> additions)
        {
            List<Renderer> merged = new List<Renderer>();
            HashSet<Renderer> seen = new HashSet<Renderer>();

            if (existing != null)
            {
                foreach (Renderer renderer in existing)
                {
                    if (renderer != null && seen.Add(renderer))
                    {
                        merged.Add(renderer);
                    }
                }
            }

            if (additions != null)
            {
                foreach (Renderer renderer in additions)
                {
                    if (renderer != null && seen.Add(renderer))
                    {
                        merged.Add(renderer);
                    }
                }
            }

            return merged.ToArray();
        }

        private static Renderer[] RemoveNullEntries(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return Array.Empty<Renderer>();
            }

            List<Renderer> cleaned = new List<Renderer>(renderers.Length);
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    cleaned.Add(renderer);
                }
            }

            return cleaned.ToArray();
        }

        private static GameObject[] RemoveNullEntries(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                return Array.Empty<GameObject>();
            }

            List<GameObject> cleaned = new List<GameObject>(objects.Length);
            foreach (GameObject targetObject in objects)
            {
                if (targetObject != null)
                {
                    cleaned.Add(targetObject);
                }
            }

            return cleaned.ToArray();
        }

        private void WarnOnce(string warningKey, string message)
        {
            if (!issuedWarnings.Add(warningKey))
            {
                return;
            }

            Debug.LogWarning(message, this);
        }
    }
}
