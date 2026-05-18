using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandCampfireInteraction : IslandInteractable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private bool litOnStart;
        [SerializeField] private float interactionCooldown = 0.3f;
        [SerializeField] private float fireLightIntensity = 5.6f;
        [SerializeField] private float fireLightRange = 12.5f;
        [SerializeField] private float innerGlowLightIntensity = 2.6f;
        [SerializeField] private float innerGlowLightRange = 6.2f;
        [SerializeField] private float flickerSpeed = 4.4f;

        private readonly List<Renderer> emberRenderers = new List<Renderer>();

        private WorldEnvironmentManager worldEnvironmentManager;
        private MaterialPropertyBlock propertyBlock;
        private Transform fireAnchor;
        private ParticleSystem flameEffect;
        private ParticleSystem flameCoreEffect;
        private ParticleSystem smokeEffect;
        private ParticleSystem sparkEffect;
        private Light fireLight;
        private Light innerGlowLight;
        private Material flameMaterial;
        private Material smokeMaterial;
        private Material sparkMaterial;
        private Coroutine residualSmokeRoutine;
        private float nextInteractionTime;
        private float flickerOffset;
        private bool isLit;

        public override Vector3 FocusPoint => fireAnchor != null
            ? fireAnchor.position + new Vector3(0f, 0.42f, 0f)
            : base.FocusPoint;

        public void Configure(float interactionRadius, float focusHeight, bool startLit)
        {
            litOnStart = startLit;
            SetInteractionRadius(interactionRadius);
            SetFocusHeight(focusHeight);
            InitializePresentation();
            isLit = litOnStart;
            UpdatePrompt();
            ApplyLitStateImmediate();
        }

        private void Awake()
        {
            InitializePresentation();
            isLit = litOnStart;
            ApplyLitStateImmediate();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializePresentation();
            ResolveWorldManager();
            SubscribeToWorldManager();
            UpdatePrompt();
            ApplyLitStateImmediate();
        }

        protected override void OnDisable()
        {
            UnsubscribeFromWorldManager();
            base.OnDisable();
        }

        private void OnDestroy()
        {
            ReleaseMaterial(flameMaterial);
            ReleaseMaterial(smokeMaterial);
            ReleaseMaterial(sparkMaterial);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (worldEnvironmentManager == null)
            {
                ResolveWorldManager();
                SubscribeToWorldManager();
            }

            if (isLit)
            {
                UpdateFlicker();
            }
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

            nextInteractionTime = Time.time + interactionCooldown;

            if (isLit)
            {
                Extinguish(false);
                return;
            }

            if (IsRainLikeWeather())
            {
                worldEnvironmentManager?.ShowWarning("The campfire wood is too wet to light while it is raining.");
                UpdatePrompt();
                return;
            }

            Ignite();
        }

        private void HandleWorldStateChanged()
        {
            UpdatePrompt();

            if (isLit && IsRainLikeWeather())
            {
                Extinguish(true);
            }
        }

        private void InitializePresentation()
        {
            propertyBlock ??= new MaterialPropertyBlock();
            flickerOffset = flickerOffset <= 0f ? Random.value * 100f : flickerOffset;

            CacheSceneParts();
            EnsureEffects();
        }

        private void CacheSceneParts()
        {
            fireAnchor = transform.Find("FireAnchor");
            if (fireAnchor == null)
            {
                GameObject anchor = new GameObject("FireAnchor");
                anchor.transform.SetParent(transform, false);
                anchor.transform.localPosition = new Vector3(0f, 0.28f, 0f);
                fireAnchor = anchor.transform;
            }

            emberRenderers.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.gameObject.name.Contains("EmberCoal"))
                {
                    emberRenderers.Add(renderer);
                }
            }
        }

        private void EnsureEffects()
        {
            flameMaterial ??= CreateParticleMaterial("Campfire Flame Particle", new Color(1f, 0.48f, 0.14f, 0.85f));
            smokeMaterial ??= CreateParticleMaterial("Campfire Smoke Particle", new Color(0.32f, 0.32f, 0.32f, 0.45f));
            sparkMaterial ??= CreateParticleMaterial("Campfire Spark Particle", new Color(1f, 0.72f, 0.28f, 0.85f));

            flameEffect = FindOrCreateParticleSystem("FlameEffect", flameMaterial, ConfigureFlameEffect);
            flameCoreEffect = FindOrCreateParticleSystem("FlameCoreEffect", flameMaterial, ConfigureFlameCoreEffect);
            smokeEffect = FindOrCreateParticleSystem("SmokeEffect", smokeMaterial, ConfigureSmokeEffect);
            sparkEffect = FindOrCreateParticleSystem("SparkEffect", sparkMaterial, ConfigureSparkEffect);
            fireLight = FindOrCreateFireLight();
            innerGlowLight = FindOrCreateInnerGlowLight();
        }

        private ParticleSystem FindOrCreateParticleSystem(string childName, Material material, System.Action<ParticleSystem> configure)
        {
            Transform existing = fireAnchor.Find(childName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(childName);
            if (existing == null)
            {
                go.transform.SetParent(fireAnchor, false);
            }

            ParticleSystem system = go.GetComponent<ParticleSystem>();
            if (system == null)
            {
                system = go.AddComponent<ParticleSystem>();
            }

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.OldestInFront;
            configure(system);
            return system;
        }

        private Light FindOrCreateFireLight()
        {
            Transform existing = fireAnchor.Find("FireLight");
            GameObject go = existing != null ? existing.gameObject : new GameObject("FireLight");
            if (existing == null)
            {
                go.transform.SetParent(fireAnchor, false);
            }

            go.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            Light light = go.GetComponent<Light>();
            if (light == null)
            {
                light = go.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = new Color(1f, 0.53f, 0.2f);
            light.intensity = fireLightIntensity;
            light.range = fireLightRange;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            return light;
        }

        private Light FindOrCreateInnerGlowLight()
        {
            Transform existing = fireAnchor.Find("InnerGlowLight");
            GameObject go = existing != null ? existing.gameObject : new GameObject("InnerGlowLight");
            if (existing == null)
            {
                go.transform.SetParent(fireAnchor, false);
            }

            go.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            Light light = go.GetComponent<Light>();
            if (light == null)
            {
                light = go.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = new Color(1f, 0.34f, 0.12f);
            light.intensity = innerGlowLightIntensity;
            light.range = innerGlowLightRange;
            light.shadows = LightShadows.None;
            return light;
        }

        private void ConfigureFlameEffect(ParticleSystem system)
        {
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.05f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.66f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.98f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(CreateFlameGradient());
            main.maxParticles = 420;
            main.gravityModifier = 0f;

            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 70f;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.22f;
            shape.angle = 12f;
            shape.length = 0.2f;

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateFlameFadeGradient());

            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1.28f));

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.34f;
            noise.frequency = 0.9f;
            noise.scrollSpeed = 0.42f;

            system.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        }

        private void ConfigureFlameCoreEffect(ParticleSystem system)
        {
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.52f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.34f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.46f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(CreateFlameCoreGradient());
            main.maxParticles = 180;
            main.gravityModifier = -0.02f;

            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 48f;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.09f;
            shape.angle = 8f;
            shape.length = 0.12f;

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateFlameCoreFadeGradient());

            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.55f, 1f, 0.92f));

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 0.7f;
            noise.scrollSpeed = 0.3f;

            system.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        }

        private void ConfigureSmokeEffect(ParticleSystem system)
        {
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.46f);
            main.startColor = new ParticleSystem.MinMaxGradient(CreateSmokeGradient());
            main.maxParticles = 36;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 4f;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.05f;
            shape.angle = 5f;
            shape.length = 0.12f;

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateSmokeFadeGradient());

            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1.8f));

            var velocityOverLifetime = system.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.38f;
            noise.scrollSpeed = 0.14f;

            system.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        }

        private void ConfigureSparkEffect(ParticleSystem system)
        {
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(CreateSparkGradient());
            main.maxParticles = 80;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);

            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 10f;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.08f;
            shape.angle = 22f;
            shape.length = 0.06f;

            var trails = system.trails;
            trails.enabled = true;
            trails.lifetime = 0.18f;
            trails.dieWithParticles = true;

            system.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        }

        private void Ignite()
        {
            if (residualSmokeRoutine != null)
            {
                StopCoroutine(residualSmokeRoutine);
                residualSmokeRoutine = null;
            }

            isLit = true;
            UpdatePrompt();
            ApplyLitStateImmediate();
        }

        private void Extinguish(bool weatherForced)
        {
            isLit = false;
            UpdatePrompt();
            StopActiveFlameEffects();
            ApplyEmberVisuals(0.06f);

            if (residualSmokeRoutine != null)
            {
                StopCoroutine(residualSmokeRoutine);
            }

            residualSmokeRoutine = StartCoroutine(PlayResidualSmokeRoutine(weatherForced ? 1.7f : 1.0f));
        }

        private void ApplyLitStateImmediate()
        {
            if (isLit)
            {
                PlayParticleSystem(flameEffect);
                PlayParticleSystem(flameCoreEffect);
                PlayParticleSystem(sparkEffect);

                if (fireLight != null)
                {
                    fireLight.enabled = true;
                    fireLight.intensity = fireLightIntensity;
                    fireLight.range = fireLightRange;
                }

                if (innerGlowLight != null)
                {
                    innerGlowLight.enabled = true;
                    innerGlowLight.intensity = innerGlowLightIntensity;
                    innerGlowLight.range = innerGlowLightRange;
                }

                ApplyEmberVisuals(0.82f);
            }
            else
            {
                StopParticleSystem(flameEffect, true);
                StopParticleSystem(flameCoreEffect, true);
                StopParticleSystem(smokeEffect, true);
                StopParticleSystem(sparkEffect, true);

                if (fireLight != null)
                {
                    fireLight.enabled = false;
                }

                if (innerGlowLight != null)
                {
                    innerGlowLight.enabled = false;
                }

                ApplyEmberVisuals(0.06f);
            }
        }

        private void StopActiveFlameEffects()
        {
            StopParticleSystem(flameEffect, true);
            StopParticleSystem(flameCoreEffect, true);
            StopParticleSystem(sparkEffect, true);

            if (fireLight != null)
            {
                fireLight.enabled = false;
            }

            if (innerGlowLight != null)
            {
                innerGlowLight.enabled = false;
            }
        }

        private IEnumerator PlayResidualSmokeRoutine(float duration)
        {
            if (smokeEffect == null)
            {
                yield break;
            }

            var emission = smokeEffect.emission;
            emission.rateOverTime = 7f;
            PlayParticleSystem(smokeEffect);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (isLit)
                {
                    emission.rateOverTime = 4f;
                    residualSmokeRoutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                emission.rateOverTime = Mathf.Lerp(7f, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            emission.rateOverTime = 4f;
            StopParticleSystem(smokeEffect, true);
            residualSmokeRoutine = null;
        }

        private void UpdateFlicker()
        {
            float primaryNoise = Mathf.PerlinNoise(flickerOffset, Time.time * flickerSpeed);
            float secondaryNoise = Mathf.PerlinNoise((flickerOffset * 0.37f) + 11f, Time.time * (flickerSpeed * 1.6f));
            float glow = Mathf.Clamp01(0.48f + (primaryNoise * 0.34f) + (secondaryNoise * 0.24f));

            if (fireLight != null)
            {
                float nightBoost = worldEnvironmentManager != null && worldEnvironmentManager.CurrentTime == TimeOfDay.Night ? 1.22f : 1f;
                fireLight.enabled = true;
                fireLight.intensity = fireLightIntensity * nightBoost * Mathf.Lerp(0.82f, 1.28f, glow);
                fireLight.range = fireLightRange * Mathf.Lerp(0.96f, 1.12f, secondaryNoise);
            }

            if (innerGlowLight != null)
            {
                innerGlowLight.enabled = true;
                innerGlowLight.intensity = innerGlowLightIntensity * Mathf.Lerp(0.86f, 1.18f, primaryNoise);
            }

            if (flameEffect != null)
            {
                flameEffect.transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.16f, primaryNoise);
            }

            if (flameCoreEffect != null)
            {
                flameCoreEffect.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.08f, secondaryNoise);
            }

            ApplyEmberVisuals(Mathf.Lerp(0.5f, 1f, glow));
        }

        private void ApplyEmberVisuals(float glowAmount)
        {
            Color emberColor = Color.Lerp(new Color(0.16f, 0.09f, 0.08f), new Color(0.82f, 0.34f, 0.08f), glowAmount);
            Color emission = Color.Lerp(Color.black, new Color(1f, 0.36f, 0.08f) * 2.2f, glowAmount);

            foreach (Renderer renderer in emberRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                propertyBlock.Clear();
                propertyBlock.SetColor(BaseColorId, emberColor);
                propertyBlock.SetColor(ColorId, emberColor);
                propertyBlock.SetColor(EmissionColorId, emission);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ResolveWorldManager()
        {
            worldEnvironmentManager = FindAnyObjectByType<WorldEnvironmentManager>();
        }

        private void SubscribeToWorldManager()
        {
            if (worldEnvironmentManager != null)
            {
                worldEnvironmentManager.WorldStateChanged -= HandleWorldStateChanged;
                worldEnvironmentManager.WorldStateChanged += HandleWorldStateChanged;
                HandleWorldStateChanged();
            }
        }

        private void UnsubscribeFromWorldManager()
        {
            if (worldEnvironmentManager != null)
            {
                worldEnvironmentManager.WorldStateChanged -= HandleWorldStateChanged;
            }
        }

        private bool IsRainLikeWeather()
        {
            return worldEnvironmentManager != null &&
                   (worldEnvironmentManager.CurrentWeather == WeatherType.Rain ||
                    worldEnvironmentManager.CurrentWeather == WeatherType.Thunderstorm);
        }

        private void UpdatePrompt()
        {
            if (isLit)
            {
                SetInteractionPrompt("Press E or F to extinguish the campfire");
                return;
            }

            if (IsRainLikeWeather())
            {
                SetInteractionPrompt("Press E or F to try lighting the campfire");
                return;
            }

            SetInteractionPrompt("Press E or F to light the campfire");
        }

        private static void PlayParticleSystem(ParticleSystem system)
        {
            if (system == null)
            {
                return;
            }

            system.gameObject.SetActive(true);
            system.Play(true);
        }

        private static void StopParticleSystem(ParticleSystem system, bool clearParticles)
        {
            if (system == null)
            {
                return;
            }

            system.Stop(true, clearParticles ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
            if (clearParticles)
            {
                system.gameObject.SetActive(false);
            }
        }

        private static Material CreateParticleMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            shader ??= Shader.Find("Particles/Standard Unlit");
            shader ??= Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            Material material = new Material(shader)
            {
                name = name,
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };

            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, color);
            }

            if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, color);
            }

            return material;
        }

        private static Gradient CreateFlameGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.96f, 0.6f), 0f),
                    new GradientColorKey(new Color(1f, 0.63f, 0.18f), 0.35f),
                    new GradientColorKey(new Color(0.84f, 0.17f, 0.06f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.92f, 0.14f),
                    new GradientAlphaKey(0.85f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static Gradient CreateFlameFadeGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
                    new GradientColorKey(new Color(1f, 0.48f, 0.12f), 0.55f),
                    new GradientColorKey(new Color(0.52f, 0.08f, 0.05f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.95f, 0.12f),
                    new GradientAlphaKey(0.4f, 0.74f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static Gradient CreateSmokeGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 0.6f),
                    new GradientColorKey(new Color(0.42f, 0.42f, 0.42f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.16f, 0.22f),
                    new GradientAlphaKey(0.08f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static Gradient CreateSmokeFadeGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.42f, 0.42f, 0.42f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.18f, 0.12f),
                    new GradientAlphaKey(0.05f, 0.84f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static Gradient CreateFlameCoreGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.98f, 0.84f), 0f),
                    new GradientColorKey(new Color(1f, 0.82f, 0.36f), 0.45f),
                    new GradientColorKey(new Color(1f, 0.44f, 0.12f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.98f, 0.16f),
                    new GradientAlphaKey(0.88f, 0.74f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static Gradient CreateFlameCoreFadeGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.98f, 0.86f), 0f),
                    new GradientColorKey(new Color(1f, 0.72f, 0.24f), 0.5f),
                    new GradientColorKey(new Color(0.92f, 0.28f, 0.08f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(0.28f, 0.78f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static Gradient CreateSparkGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.92f, 0.58f), 0f),
                    new GradientColorKey(new Color(1f, 0.54f, 0.14f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.08f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static void ReleaseMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }
    }
}
