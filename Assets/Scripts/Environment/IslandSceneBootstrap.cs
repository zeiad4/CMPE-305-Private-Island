using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PrivateIsland
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-500)]
    public sealed class IslandSceneBootstrap : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Terrain")]
        [SerializeField] private int terrainResolution = 128;
        [SerializeField] private int textureResolution = 384;
        [SerializeField] private float islandSize = 220f;
        [SerializeField] private float peakHeight = 9f;
        [SerializeField] private float seaLevel = 3.6f;

        [Header("Set Dressing")]
        [SerializeField] private int treeCount = 42;
        [SerializeField] private int rockCount = 56;
        [SerializeField] private int bushCount = 78;
        [SerializeField] private int driftwoodCount = 18;
        [SerializeField] private int pebbleCount = 72;
        [SerializeField] private int stumpCount = 10;
        [SerializeField] private int seed = 13;

        private Mesh terrainMesh;
        private Mesh waterMesh;
        private Texture2D terrainTexture;
        private Material terrainMaterial;
        private Material waterMaterial;
        private Material trunkMaterial;
        private Material leavesMaterial;
        private Material rockMaterial;
        private Material dockMaterial;
        private Material characterSkinMaterial;
        private Material characterShirtMaterial;
        private Material characterShortsMaterial;
        private Material characterStrawMaterial;
        private Material characterHairMaterial;
        private Material campfireEmberMaterial;
        private Material campfireAshMaterial;
        private Material campfireStoneMaterial;
        private bool isRebuilding;
#if UNITY_EDITOR
        private bool editorRebuildQueued;
#endif

        private static Mesh cachedCubeMesh;
        private static Mesh cachedCylinderMesh;
        private static Mesh cachedSphereMesh;
        private static Mesh cachedCapsuleMesh;

        public float IslandSize => islandSize;
        public float PeakHeight => peakHeight;

        private void Reset()
        {
            RequestRebuild();
        }

        private void OnEnable()
        {
            RequestRebuild();
        }

        private void OnValidate()
        {
            RequestRebuild();
        }

        private void OnDisable()
        {
            ReleaseGeneratedResources();
        }

        private void RebuildScene()
        {
            if (isRebuilding || !gameObject.scene.IsValid())
            {
                return;
            }

            isRebuilding = true;

            try
            {
                terrainResolution = Mathf.Clamp(terrainResolution, 32, 192);
                textureResolution = Mathf.Clamp(textureResolution, 128, 512);
                islandSize = Mathf.Max(40f, islandSize);
                peakHeight = Mathf.Max(6f, peakHeight);
                seaLevel = Mathf.Clamp(seaLevel, 1f, peakHeight - 1f);
                treeCount = Mathf.Clamp(treeCount, 0, 96);
                rockCount = Mathf.Clamp(rockCount, 0, 128);
                bushCount = Mathf.Clamp(bushCount, 0, 160);
                driftwoodCount = Mathf.Clamp(driftwoodCount, 0, 48);
                pebbleCount = Mathf.Clamp(pebbleCount, 0, 160);
                stumpCount = Mathf.Clamp(stumpCount, 0, 32);

                ConfigureRenderSettings();
                ConfigureSun();
                ConfigureVolume();
                BuildTerrain();
                BuildWater();
                BuildProps();
                BuildCharacter();
                ConfigureCamera();
                ReapplyWorldControls();
            }
            finally
            {
                isRebuilding = false;
            }
        }

        private void RequestRebuild()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorRebuild();
                return;
            }
#endif

            RebuildScene();
        }

#if UNITY_EDITOR
        private void QueueEditorRebuild()
        {
            if (editorRebuildQueued || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            editorRebuildQueued = true;
            EditorApplication.delayCall += ExecuteQueuedEditorRebuild;
        }

        private void ExecuteQueuedEditorRebuild()
        {
            EditorApplication.delayCall -= ExecuteQueuedEditorRebuild;
            editorRebuildQueued = false;

            if (this == null || !isActiveAndEnabled || !gameObject.scene.IsValid() || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            RebuildScene();
        }
#endif

        private void ConfigureRenderSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.63f, 0.78f, 0.86f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 360f;
            RenderSettings.haloStrength = 0f;
            RenderSettings.flareStrength = 0f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.72f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.43f, 0.54f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.17f, 0.18f, 0.15f);

            Material skybox = RenderSettings.skybox;
            if (skybox != null)
            {
                if (skybox.HasProperty("_SunDisk"))
                {
                    skybox.SetFloat("_SunDisk", 2f);
                }

                if (skybox.HasProperty("_SunSize"))
                {
                    skybox.SetFloat("_SunSize", 0.048f);
                }

                if (skybox.HasProperty("_SunSizeConvergence"))
                {
                    skybox.SetFloat("_SunSizeConvergence", 5.2f);
                }

                if (skybox.HasProperty("_SkyTint"))
                {
                    skybox.SetColor("_SkyTint", new Color(0.56f, 0.66f, 0.82f));
                }

                if (skybox.HasProperty("_GroundColor"))
                {
                    skybox.SetColor("_GroundColor", new Color(0.28f, 0.23f, 0.18f));
                }

                if (skybox.HasProperty("_Exposure"))
                {
                    skybox.SetFloat("_Exposure", 1.08f);
                }

                if (skybox.HasProperty("_AtmosphereThickness"))
                {
                    skybox.SetFloat("_AtmosphereThickness", 0.78f);
                }
            }
        }

        private void ConfigureSun()
        {
            Light sun = RenderSettings.sun;

            if (sun == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        sun = light;
                        break;
                    }
                }
            }

            if (sun == null)
            {
                return;
            }

            RenderSettings.sun = sun;
            sun.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            sun.color = new Color(1f, 0.84f, 0.52f);
            sun.intensity = 1.45f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.92f;
        }

        private void ConfigureVolume()
        {
            Volume volume = FindAnyObjectByType<Volume>(FindObjectsInactive.Exclude);
            if (volume == null || volume.sharedProfile == null)
            {
                return;
            }

            VolumeProfile profile = volume.sharedProfile;

            Tonemapping tonemapping = GetOrAddVolumeComponent<Tonemapping>(profile);
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            ColorAdjustments colorAdjustments = GetOrAddVolumeComponent<ColorAdjustments>(profile);
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = -0.1f;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = 14f;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = 6f;

            Bloom bloom = GetOrAddVolumeComponent<Bloom>(profile);
            bloom.active = false;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 10f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0f;

            Vignette vignette = GetOrAddVolumeComponent<Vignette>(profile);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.18f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.34f;
        }

        private void ConfigureCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
            }

            if (mainCamera == null)
            {
                return;
            }

            mainCamera.fieldOfView = 72f;
            mainCamera.nearClipPlane = 0.05f;
            mainCamera.farClipPlane = 720f;
            mainCamera.backgroundColor = new Color(0.41f, 0.69f, 0.87f);
            mainCamera.clearFlags = CameraClearFlags.Skybox;

            RemoveComponent<IslandCameraOrbit>(mainCamera.gameObject);

            IslandFirstPersonCamera firstPersonCamera = GetOrAddComponent<IslandFirstPersonCamera>(mainCamera.gameObject);

            Transform character = transform.Find("Island/Character");
            if (character != null)
            {
                firstPersonCamera.Configure(character, new Vector3(0f, 1.92f, 0.12f), character.eulerAngles.y, -4f);
                return;
            }

            RemoveComponent<IslandFirstPersonCamera>(mainCamera.gameObject);
        }

        private void BuildTerrain()
        {
            Transform root = EnsureChild(transform, "Island");
            Transform terrainRoot = EnsureChild(root, "Terrain");

            MeshFilter filter = GetOrAddComponent<MeshFilter>(terrainRoot.gameObject);
            MeshRenderer renderer = GetOrAddComponent<MeshRenderer>(terrainRoot.gameObject);
            MeshCollider collider = GetOrAddComponent<MeshCollider>(terrainRoot.gameObject);

            terrainMesh ??= CreateRuntimeMesh("Generated Island Terrain");
            terrainTexture ??= CreateRuntimeTexture("Generated Island Texture");
            terrainMaterial ??= CreateRuntimeMaterial("Island Terrain Material");

            IslandMeshBuilder.RebuildTerrainMesh(terrainMesh, terrainResolution, islandSize, peakHeight);
            IslandMeshBuilder.RebuildTerrainTexture(terrainTexture, textureResolution, islandSize, peakHeight, seaLevel);

            terrainMaterial.mainTexture = terrainTexture;
            terrainMaterial.SetTexture("_BaseMap", terrainTexture);
            terrainMaterial.SetFloat("_Smoothness", 0.08f);
            terrainMaterial.SetFloat("_Metallic", 0f);

            filter.sharedMesh = terrainMesh;
            renderer.sharedMaterial = terrainMaterial;
            collider.sharedMesh = null;
            collider.sharedMesh = terrainMesh;
        }

        private void BuildWater()
        {
            Transform root = EnsureChild(transform, "Island");
            Transform waterRoot = EnsureChild(root, "Water");

            MeshFilter filter = GetOrAddComponent<MeshFilter>(waterRoot.gameObject);
            MeshRenderer renderer = GetOrAddComponent<MeshRenderer>(waterRoot.gameObject);

            waterMesh ??= CreateRuntimeMesh("Generated Island Water");
            waterMaterial ??= CreateRuntimeMaterial("Island Water Material");

            IslandMeshBuilder.RebuildWaterMesh(waterMesh, islandSize * 1.65f);

            waterRoot.localPosition = new Vector3(0f, seaLevel, 0f);
            filter.sharedMesh = waterMesh;

            waterMaterial.SetColor("_BaseColor", new Color(0.12f, 0.47f, 0.61f, 1f));
            waterMaterial.SetFloat("_Smoothness", 0.9f);
            waterMaterial.SetFloat("_Metallic", 0.02f);
            renderer.sharedMaterial = waterMaterial;
        }

        private void BuildProps()
        {
            Transform root = EnsureChild(transform, "Island");
            Transform propsRoot = EnsureChild(root, "Props");
            ClearChildren(propsRoot);

            trunkMaterial ??= CreateRuntimeMaterial("Island Trunk Material");
            leavesMaterial ??= CreateRuntimeMaterial("Island Leaves Material");
            rockMaterial ??= CreateRuntimeMaterial("Island Rock Material");
            dockMaterial ??= CreateRuntimeMaterial("Island Dock Material");

            trunkMaterial.SetColor("_BaseColor", new Color(0.3f, 0.22f, 0.14f));
            trunkMaterial.SetFloat("_Smoothness", 0.08f);

            leavesMaterial.SetColor("_BaseColor", new Color(0.23f, 0.44f, 0.2f));
            leavesMaterial.SetFloat("_Smoothness", 0.05f);

            rockMaterial.SetColor("_BaseColor", new Color(0.47f, 0.45f, 0.41f));
            rockMaterial.SetFloat("_Smoothness", 0.04f);

            dockMaterial.SetColor("_BaseColor", new Color(0.52f, 0.38f, 0.24f));
            dockMaterial.SetFloat("_Smoothness", 0.2f);

            System.Random random = new System.Random(seed);
            Vector2 dockDirection = new Vector2(0.42f, 0.91f).normalized;
            Vector3 characterSpawn = GetCharacterSpawnPosition(dockDirection);
            Vector3 campfirePosition = GetCampfirePosition(dockDirection, characterSpawn);

            BuildDock(propsRoot, dockDirection);
            BuildCampfire(propsRoot, campfirePosition, dockDirection);

            int placedRocks = 0;
            int rockAttempts = 0;
            while (placedRocks < rockCount && rockAttempts++ < rockCount * 5)
            {
                float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
                float radius = Mathf.Lerp(islandSize * 0.14f, islandSize * 0.48f, (float)random.NextDouble());
                Vector3 position = SampleSurfacePosition(angle, radius);

                if (position.y < seaLevel + 0.2f || IsNearCharacterSpawn(position, characterSpawn, 5.5f))
                {
                    continue;
                }

                float scale = Mathf.Lerp(1.2f, 3.8f, (float)random.NextDouble());
                CreateRock(propsRoot, position, scale, angle * Mathf.Rad2Deg, random);
                placedRocks++;
            }

            int placedTrees = 0;
            int treeAttempts = 0;
            while (placedTrees < treeCount && treeAttempts++ < treeCount * 6)
            {
                float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                if (Vector2.Dot(direction, dockDirection) > 0.85f)
                {
                    continue;
                }

                float radius = Mathf.Lerp(islandSize * 0.1f, islandSize * 0.44f, (float)random.NextDouble());
                Vector3 position = SampleSurfacePosition(angle, radius);

                if (position.y < seaLevel + 0.7f || IsNearCharacterSpawn(position, characterSpawn, 10f))
                {
                    continue;
                }

                float height = Mathf.Lerp(4.8f, 8.2f, (float)random.NextDouble());
                float tilt = Mathf.Lerp(-9f, 9f, (float)random.NextDouble());
                CreatePalm(propsRoot, position, height, tilt, angle * Mathf.Rad2Deg);
                placedTrees++;
            }

            int placedBushes = 0;
            int bushAttempts = 0;
            while (placedBushes < bushCount && bushAttempts++ < bushCount * 5)
            {
                float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
                float radius = Mathf.Lerp(islandSize * 0.08f, islandSize * 0.46f, (float)random.NextDouble());
                Vector3 position = SampleSurfacePosition(angle, radius);

                if (position.y < seaLevel + 0.5f || IsNearCharacterSpawn(position, characterSpawn, 7.5f))
                {
                    continue;
                }

                float scale = Mathf.Lerp(1.2f, 2.6f, (float)random.NextDouble());
                CreateBush(propsRoot, position, scale, angle * Mathf.Rad2Deg, random);
                placedBushes++;
            }

            int placedDriftwood = 0;
            int driftwoodAttempts = 0;
            while (placedDriftwood < driftwoodCount && driftwoodAttempts++ < driftwoodCount * 6)
            {
                float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
                float radius = Mathf.Lerp(islandSize * 0.38f, islandSize * 0.48f, (float)random.NextDouble());
                Vector3 position = SampleSurfacePosition(angle, radius);

                if (position.y > seaLevel + 1.2f || position.y < seaLevel - 0.1f || IsNearCharacterSpawn(position, characterSpawn, 4.5f))
                {
                    continue;
                }

                float length = Mathf.Lerp(1.8f, 3.8f, (float)random.NextDouble());
                CreateDriftwood(propsRoot, position, length, angle * Mathf.Rad2Deg);
                placedDriftwood++;
            }

            int placedPebbles = 0;
            int pebbleAttempts = 0;
            while (placedPebbles < pebbleCount && pebbleAttempts++ < pebbleCount * 4)
            {
                float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
                float radius = Mathf.Lerp(islandSize * 0.18f, islandSize * 0.49f, (float)random.NextDouble());
                Vector3 position = SampleSurfacePosition(angle, radius);

                if (position.y < seaLevel + 0.1f || position.y > seaLevel + 1.6f || IsNearCharacterSpawn(position, characterSpawn, 4.5f))
                {
                    continue;
                }

                float scale = Mathf.Lerp(0.35f, 0.9f, (float)random.NextDouble());
                CreatePebble(propsRoot, position, scale, angle * Mathf.Rad2Deg);
                placedPebbles++;
            }

            int placedStumps = 0;
            int stumpAttempts = 0;
            while (placedStumps < stumpCount && stumpAttempts++ < stumpCount * 8)
            {
                float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
                float radius = Mathf.Lerp(islandSize * 0.18f, islandSize * 0.42f, (float)random.NextDouble());
                Vector3 position = SampleSurfacePosition(angle, radius);

                if (position.y < seaLevel + 0.9f || IsNearCharacterSpawn(position, characterSpawn, 7f))
                {
                    continue;
                }

                float scale = Mathf.Lerp(0.8f, 1.4f, (float)random.NextDouble());
                CreateStump(propsRoot, position, scale, angle * Mathf.Rad2Deg);
                placedStumps++;
            }

            CreateHiddenCollectibles(propsRoot, random, characterSpawn, dockDirection);
        }

        private void BuildCharacter()
        {
            Transform root = EnsureChild(transform, "Island");
            Transform characterRoot = EnsureChild(root, "Character");
            ClearChildren(characterRoot);

            characterSkinMaterial ??= CreateRuntimeMaterial("Island Character Skin");
            characterShirtMaterial ??= CreateRuntimeMaterial("Island Character Shirt");
            characterShortsMaterial ??= CreateRuntimeMaterial("Island Character Shorts");
            characterStrawMaterial ??= CreateRuntimeMaterial("Island Character Straw");
            characterHairMaterial ??= CreateRuntimeMaterial("Island Character Hair");

            characterSkinMaterial.SetColor("_BaseColor", new Color(0.76f, 0.59f, 0.42f));
            characterSkinMaterial.SetFloat("_Smoothness", 0.18f);

            characterShirtMaterial.SetColor("_BaseColor", new Color(0.18f, 0.58f, 0.54f));
            characterShirtMaterial.SetFloat("_Smoothness", 0.12f);

            characterShortsMaterial.SetColor("_BaseColor", new Color(0.91f, 0.52f, 0.25f));
            characterShortsMaterial.SetFloat("_Smoothness", 0.14f);

            characterStrawMaterial.SetColor("_BaseColor", new Color(0.83f, 0.72f, 0.45f));
            characterStrawMaterial.SetFloat("_Smoothness", 0.16f);

            characterHairMaterial.SetColor("_BaseColor", new Color(0.17f, 0.11f, 0.08f));
            characterHairMaterial.SetFloat("_Smoothness", 0.1f);

            Vector2 dockDirection = new Vector2(0.42f, 0.91f).normalized;
            Vector3 spawnPosition = GetCharacterSpawnPosition(dockDirection);
            characterRoot.localPosition = spawnPosition + new Vector3(0f, 0.02f, 0f);
            characterRoot.localRotation = Quaternion.LookRotation(new Vector3(-dockDirection.x, 0f, -dockDirection.y));

            CreateIslandExplorer(characterRoot);

            IslandCharacterController controller = GetOrAddComponent<IslandCharacterController>(characterRoot.gameObject);
            controller.Configure(islandSize, peakHeight);
            GetOrAddComponent<IslandInventory>(characterRoot.gameObject);
            GetOrAddComponent<IslandPlayerInteractor>(characterRoot.gameObject);
            GetOrAddComponent<IslandFootstepAudio>(characterRoot.gameObject);
            IslandShorelineFootsteps shorelineFootsteps = GetOrAddComponent<IslandShorelineFootsteps>(characterRoot.gameObject);
            shorelineFootsteps.Configure(islandSize, peakHeight, seaLevel);
            characterRoot.tag = "Player";
        }

        private void BuildDock(Transform parent, Vector2 direction)
        {
            GameObject dockRoot = new GameObject("Dock");
            dockRoot.transform.SetParent(parent, false);

            float shorelineRadius = islandSize * 0.41f;
            Vector3 shoreline = new Vector3(direction.x, 0f, direction.y) * shorelineRadius;
            shoreline.y = IslandMeshBuilder.SampleHeight(shoreline.x, shoreline.z, islandSize, peakHeight);
            dockRoot.transform.localPosition = shoreline + new Vector3(0f, 0.15f, 0f);
            dockRoot.transform.localRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));

            for (int i = 0; i < 5; i++)
            {
                GameObject plank = CreateMeshPart("Plank", cachedCubeMesh ??= GetPrimitiveMesh(PrimitiveType.Cube), dockMaterial, dockRoot.transform);
                plank.transform.localPosition = new Vector3(0f, 0f, (i * 2.2f) - 1f);
                plank.transform.localScale = new Vector3(3.2f, 0.22f, 1.6f);
            }

            for (int i = 0; i < 4; i++)
            {
                float z = (i * 2.8f) - 1.8f;
                CreateDockPillar(dockRoot.transform, new Vector3(-1.05f, -1.4f, z));
                CreateDockPillar(dockRoot.transform, new Vector3(1.05f, -1.4f, z));
            }
        }

        private void CreateDockPillar(Transform parent, Vector3 localPosition)
        {
            GameObject pillar = CreateMeshPart("Pillar", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), dockMaterial, parent);
            pillar.transform.localPosition = localPosition;
            pillar.transform.localScale = new Vector3(0.18f, 1.6f, 0.18f);
        }

        private void CreateRock(Transform parent, Vector3 position, float scale, float yaw, System.Random random)
        {
            GameObject rock = new GameObject("Rock");
            rock.transform.SetParent(parent, false);
            rock.transform.localPosition = position + new Vector3(0f, -scale * 0.12f, 0f);
            rock.transform.localRotation = Quaternion.Euler(
                RandomRange(random, -9f, 9f),
                yaw + RandomRange(random, -24f, 24f),
                RandomRange(random, -7f, 7f));

            Color mainTint = new Color(0.5f, 0.48f, 0.44f);
            Color coolTint = new Color(0.42f, 0.45f, 0.48f);
            Color warmTint = new Color(0.6f, 0.54f, 0.47f);
            Color shadowTint = new Color(0.36f, 0.38f, 0.4f);

            switch (random.Next(4))
            {
                case 0:
                    CreateRoundedRockVariant(rock.transform, scale, mainTint, coolTint, warmTint, shadowTint, random);
                    break;
                case 1:
                    CreateLayeredRockVariant(rock.transform, scale, mainTint, coolTint, warmTint, shadowTint, random);
                    break;
                case 2:
                    CreateOutcropRockVariant(rock.transform, scale, mainTint, coolTint, warmTint, shadowTint, random);
                    break;
                default:
                    CreateSplitRockVariant(rock.transform, scale, mainTint, coolTint, warmTint, shadowTint, random);
                    break;
            }

            CreateRockDebrisRing(rock.transform, scale, mainTint, shadowTint, random);

            IslandRockInteraction interaction = GetOrAddComponent<IslandRockInteraction>(rock);
            interaction.Configure(Mathf.Clamp(scale * 1.18f, 2.2f, 4.6f), scale);
            ConfigureRockObstacleCollider(rock);
        }

        private void CreateRoundedRockVariant(
            Transform parent,
            float scale,
            Color mainTint,
            Color coolTint,
            Color warmTint,
            Color shadowTint,
            System.Random random)
        {
            CreateRockPiece(parent, "Core", PrimitiveType.Cube, new Vector3(0f, scale * 0.3f, 0f), new Vector3(scale * 0.92f, scale * 0.48f, scale * 0.76f), new Vector3(-8f, 18f, 4f), mainTint);
            CreateRockPiece(parent, "ShoulderA", PrimitiveType.Cube, new Vector3(-scale * 0.26f, scale * 0.28f, scale * 0.16f), new Vector3(scale * 0.42f, scale * 0.28f, scale * 0.34f), new Vector3(18f, -10f, 16f), coolTint);
            CreateRockPiece(parent, "ShoulderB", PrimitiveType.Cube, new Vector3(scale * 0.24f, scale * 0.26f, -scale * 0.1f), new Vector3(scale * 0.4f, scale * 0.26f, scale * 0.3f), new Vector3(-14f, 24f, -12f), shadowTint);
            CreateRockPiece(parent, "Cap", PrimitiveType.Cube, new Vector3(scale * 0.04f, scale * 0.52f, scale * 0.02f), new Vector3(scale * 0.24f, scale * 0.12f, scale * 0.24f), new Vector3(22f, 12f, -6f), warmTint);
            CreateRockPiece(parent, "Wedge", PrimitiveType.Cube, new Vector3(-scale * 0.1f, scale * 0.2f, scale * 0.28f), new Vector3(scale * 0.26f, scale * 0.1f, scale * 0.18f), new Vector3(12f, -18f, 18f), new Color(0.64f, 0.61f, 0.56f));
        }

        private void CreateLayeredRockVariant(
            Transform parent,
            float scale,
            Color mainTint,
            Color coolTint,
            Color warmTint,
            Color shadowTint,
            System.Random random)
        {
            CreateRockPiece(parent, "BaseShelf", PrimitiveType.Cube, new Vector3(0f, scale * 0.22f, 0f), new Vector3(scale * 1.08f, scale * 0.26f, scale * 0.76f), new Vector3(-7f, 10f, 3f), mainTint);
            CreateRockPiece(parent, "RearShelf", PrimitiveType.Cube, new Vector3(-scale * 0.14f, scale * 0.38f, -scale * 0.18f), new Vector3(scale * 0.78f, scale * 0.18f, scale * 0.48f), new Vector3(11f, -18f, 9f), shadowTint);
            CreateRockPiece(parent, "TopShelf", PrimitiveType.Cube, new Vector3(scale * 0.08f, scale * 0.54f, scale * 0.06f), new Vector3(scale * 0.72f, scale * 0.12f, scale * 0.4f), new Vector3(6f, 22f, -4f), warmTint);
            CreateRockPiece(parent, "CornerMass", PrimitiveType.Capsule, new Vector3(scale * 0.38f, scale * 0.26f, scale * 0.04f), new Vector3(scale * 0.28f, scale * 0.22f, scale * 0.3f), new Vector3(-8f, 18f, -14f), coolTint);
            CreateRockPiece(parent, "FracturePlate", PrimitiveType.Cube, new Vector3(-scale * 0.12f, scale * 0.5f, scale * 0.24f), new Vector3(scale * 0.36f, scale * 0.06f, scale * 0.14f), new Vector3(-20f, 26f, 16f), new Color(0.72f, 0.69f, 0.62f));
        }

        private void CreateOutcropRockVariant(
            Transform parent,
            float scale,
            Color mainTint,
            Color coolTint,
            Color warmTint,
            Color shadowTint,
            System.Random random)
        {
            CreateRockPiece(parent, "Base", PrimitiveType.Capsule, new Vector3(0f, scale * 0.26f, 0f), new Vector3(scale * 0.82f, scale * 0.32f, scale * 0.68f), new Vector3(-10f, 6f, 2f), mainTint);
            CreateRockPiece(parent, "Fin", PrimitiveType.Cube, new Vector3(scale * 0.02f, scale * 0.62f, -scale * 0.08f), new Vector3(scale * 0.22f, scale * 0.54f, scale * 0.5f), new Vector3(10f, -8f, 20f), warmTint);
            CreateRockPiece(parent, "Lean", PrimitiveType.Cube, new Vector3(-scale * 0.28f, scale * 0.46f, scale * 0.14f), new Vector3(scale * 0.34f, scale * 0.4f, scale * 0.28f), new Vector3(-12f, 28f, -18f), coolTint);
            CreateRockPiece(parent, "Support", PrimitiveType.Cube, new Vector3(scale * 0.26f, scale * 0.24f, scale * 0.2f), new Vector3(scale * 0.28f, scale * 0.16f, scale * 0.2f), new Vector3(18f, 18f, -10f), shadowTint);
            CreateRockPiece(parent, "SplitFace", PrimitiveType.Cube, new Vector3(scale * 0.08f, scale * 0.48f, scale * 0.24f), new Vector3(scale * 0.18f, scale * 0.44f, scale * 0.08f), new Vector3(4f, 32f, 8f), new Color(0.7f, 0.67f, 0.6f));
        }

        private void CreateSplitRockVariant(
            Transform parent,
            float scale,
            Color mainTint,
            Color coolTint,
            Color warmTint,
            Color shadowTint,
            System.Random random)
        {
            CreateRockPiece(parent, "MassA", PrimitiveType.Capsule, new Vector3(-scale * 0.18f, scale * 0.28f, 0f), new Vector3(scale * 0.58f, scale * 0.34f, scale * 0.48f), new Vector3(-6f, 20f, 14f), mainTint);
            CreateRockPiece(parent, "MassB", PrimitiveType.Capsule, new Vector3(scale * 0.22f, scale * 0.3f, -scale * 0.02f), new Vector3(scale * 0.62f, scale * 0.36f, scale * 0.5f), new Vector3(10f, -18f, -10f), coolTint);
            CreateRockPiece(parent, "Bridge", PrimitiveType.Cube, new Vector3(0f, scale * 0.18f, -scale * 0.16f), new Vector3(scale * 0.62f, scale * 0.12f, scale * 0.26f), new Vector3(-4f, 8f, 4f), shadowTint);
            CreateRockPiece(parent, "TopShard", PrimitiveType.Cube, new Vector3(scale * 0.05f, scale * 0.58f, scale * 0.1f), new Vector3(scale * 0.24f, scale * 0.2f, scale * 0.28f), new Vector3(18f, 12f, -18f), warmTint);
            CreateRockPiece(parent, "FacePlate", PrimitiveType.Cube, new Vector3(-scale * 0.08f, scale * 0.42f, scale * 0.22f), new Vector3(scale * 0.18f, scale * 0.32f, scale * 0.08f), new Vector3(-10f, 26f, 6f), new Color(0.72f, 0.7f, 0.64f));
        }

        private void CreateRockDebrisRing(Transform parent, float scale, Color mainTint, Color shadowTint, System.Random random)
        {
            int debrisCount = random.Next(2, 5);
            for (int i = 0; i < debrisCount; i++)
            {
                float angle = (360f / debrisCount) * i + RandomRange(random, -24f, 24f);
                float radius = scale * RandomRange(random, 0.36f, 0.62f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, scale * RandomRange(random, 0.02f, 0.08f), 0f);
                CreateRockPiece(
                    parent,
                    $"Debris_{i}",
                    PrimitiveType.Cube,
                    offset,
                    new Vector3(scale * RandomRange(random, 0.1f, 0.18f), scale * RandomRange(random, 0.05f, 0.09f), scale * RandomRange(random, 0.08f, 0.16f)),
                    new Vector3(RandomRange(random, -18f, 18f), angle, RandomRange(random, -18f, 18f)),
                    Color.Lerp(mainTint, shadowTint, i / (float)Mathf.Max(1, debrisCount - 1)));
            }
        }

        private GameObject CreateRockPiece(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEulerAngles,
            Color tint)
        {
            Mesh mesh = primitiveType switch
            {
                PrimitiveType.Cube => cachedCubeMesh ??= GetPrimitiveMesh(PrimitiveType.Cube),
                PrimitiveType.Sphere => cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere),
                PrimitiveType.Cylinder => cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder),
                _ => cachedCapsuleMesh ??= GetPrimitiveMesh(PrimitiveType.Capsule)
            };

            GameObject piece = CreateMeshPart(name, mesh, rockMaterial, parent);
            piece.transform.localPosition = localPosition;
            piece.transform.localRotation = Quaternion.Euler(localEulerAngles);
            piece.transform.localScale = localScale;
            ApplyTint(piece, tint);
            return piece;
        }

        private void CreatePalm(Transform parent, Vector3 position, float height, float tilt, float yaw)
        {
            GameObject palm = new GameObject("Palm");
            palm.transform.SetParent(parent, false);
            palm.transform.localPosition = position;
            palm.transform.localRotation = Quaternion.Euler(0f, yaw, tilt);

            int trunkSegments = 6;
            for (int i = 0; i < trunkSegments; i++)
            {
                float t = i / (float)(trunkSegments - 1);
                float segmentHeight = height / trunkSegments;
                float radius = Mathf.Lerp(0.28f, 0.18f, t);
                float offsetX = Mathf.Sin((t * 1.7f) + (yaw * Mathf.Deg2Rad)) * 0.06f;
                float offsetZ = Mathf.Cos((t * 1.4f) + (yaw * Mathf.Deg2Rad * 0.6f)) * 0.04f;

                GameObject segment = CreateMeshPart("TrunkSegment", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), trunkMaterial, palm.transform);
                segment.transform.localPosition = new Vector3(offsetX, (segmentHeight * 0.5f) + (segmentHeight * i), offsetZ);
                segment.transform.localRotation = Quaternion.Euler(RandomWave(t, 0.5f) * 3f, 0f, RandomWave(t, 1.2f) * 2f);
                segment.transform.localScale = new Vector3(radius, segmentHeight * 0.5f, radius);
                ApplyTint(segment, Color.Lerp(new Color(0.44f, 0.3f, 0.18f), new Color(0.3f, 0.2f, 0.12f), t));

                GameObject ring = CreateMeshPart("TrunkRing", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), trunkMaterial, palm.transform);
                ring.transform.localPosition = segment.transform.localPosition + new Vector3(0f, segmentHeight * 0.38f, 0f);
                ring.transform.localScale = new Vector3(radius * 1.08f, segmentHeight * 0.06f, radius * 1.08f);
                ApplyTint(ring, new Color(0.58f, 0.42f, 0.24f));
            }

            Vector3 crownCenter = new Vector3(0f, height * 1.01f, 0f);
            int frondCount = 9;
            for (int i = 0; i < frondCount; i++)
            {
                float angle = (360f / frondCount) * i;
                float frondLength = 1.85f + (0.16f * (i % 4));
                CreatePalmFrond(palm.transform, crownCenter, angle, frondLength);
            }

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                GameObject coconut = new GameObject("Coconut");
                coconut.transform.SetParent(palm.transform, false);
                coconut.transform.localPosition = crownCenter + (Quaternion.Euler(0f, angle, 0f) * new Vector3(0.2f, -0.14f, 0.14f));
                coconut.transform.localRotation = Quaternion.Euler(0f, angle + 24f, 0f);
                coconut.transform.localScale = Vector3.one * 0.78f;
                IslandItemCatalog.BuildWorldVisual(IslandItemCatalog.CoconutId, coconut.transform);
            }

            IslandPalmInteraction interaction = GetOrAddComponent<IslandPalmInteraction>(palm);
            interaction.Configure(Mathf.Clamp(height * 0.74f, 3f, 5.5f), height);
            ConfigurePalmObstacleCollider(palm, height);
        }

        private void CreateBush(Transform parent, Vector3 position, float scale, float yaw, System.Random random)
        {
            GameObject bush = new GameObject("Bush");
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = position;
            bush.transform.localRotation = Quaternion.Euler(
                RandomRange(random, -4f, 4f),
                yaw + RandomRange(random, -18f, 18f),
                RandomRange(random, -4f, 4f));

            switch (random.Next(4))
            {
                case 0:
                    CreateRoundedBushVariant(bush.transform, scale, random);
                    break;
                case 1:
                    CreateWindsweptBushVariant(bush.transform, scale, random);
                    break;
                case 2:
                    CreateDenseBushVariant(bush.transform, scale, random);
                    break;
                default:
                    CreateWildBushVariant(bush.transform, scale, random);
                    break;
            }

            IslandBushReactive interaction = GetOrAddComponent<IslandBushReactive>(bush);
            interaction.Configure(Mathf.Clamp(scale * 0.92f, 1.1f, 2.15f));
        }

        private void CreateRoundedBushVariant(Transform parent, float scale, System.Random random)
        {
            CreateBushStemCluster(parent, scale, random, 5, 0.32f, 0.22f);

            int lobeCount = 6;
            for (int i = 0; i < lobeCount; i++)
            {
                float angle = (360f / lobeCount) * i + RandomRange(random, -16f, 16f);
                float radius = scale * RandomRange(random, 0.18f, 0.34f);
                float height = scale * RandomRange(random, 0.18f, 0.32f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushLeafCluster(parent, offset, new Vector3(scale * RandomRange(random, 0.34f, 0.48f), scale * RandomRange(random, 0.2f, 0.3f), scale * RandomRange(random, 0.28f, 0.42f)), angle, random);
            }

            int broadLeafCount = random.Next(20, 28);
            for (int i = 0; i < broadLeafCount; i++)
            {
                float angle = (360f / broadLeafCount) * i + RandomRange(random, -18f, 18f);
                float radius = scale * RandomRange(random, 0.12f, 0.42f);
                float height = scale * RandomRange(random, 0.12f, 0.42f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushBroadLeaf(parent, offset, angle, scale * RandomRange(random, 0.22f, 0.34f), scale * RandomRange(random, 0.06f, 0.09f), random);
            }
        }

        private void CreateWindsweptBushVariant(Transform parent, float scale, System.Random random)
        {
            CreateBushStemCluster(parent, scale, random, 4, 0.42f, 0.32f);

            int sprigCount = 6;
            for (int i = 0; i < sprigCount; i++)
            {
                float angle = -42f + (i * 18f) + RandomRange(random, -8f, 8f);
                CreateBushSprig(parent, angle, scale, random, scale * 0.18f, scale * 0.34f, 4, 6);
            }

            int padCount = 8;
            for (int i = 0; i < padCount; i++)
            {
                float angle = -56f + (i * 15f) + RandomRange(random, -10f, 10f);
                float radius = scale * RandomRange(random, 0.18f, 0.38f);
                float height = scale * RandomRange(random, 0.16f, 0.34f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushLeafCluster(parent, offset, new Vector3(scale * RandomRange(random, 0.24f, 0.36f), scale * RandomRange(random, 0.12f, 0.2f), scale * RandomRange(random, 0.2f, 0.32f)), angle, random);
            }
        }

        private void CreateDenseBushVariant(Transform parent, float scale, System.Random random)
        {
            CreateBushStemCluster(parent, scale, random, 6, 0.28f, 0.18f);

            int clusterCount = 9;
            for (int i = 0; i < clusterCount; i++)
            {
                float angle = (360f / clusterCount) * i + RandomRange(random, -22f, 22f);
                float radius = scale * RandomRange(random, 0.08f, 0.3f);
                float height = scale * RandomRange(random, 0.14f, 0.38f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushLeafCluster(parent, offset, new Vector3(scale * RandomRange(random, 0.22f, 0.34f), scale * RandomRange(random, 0.16f, 0.26f), scale * RandomRange(random, 0.24f, 0.36f)), angle, random);
            }

            int leafCount = random.Next(18, 26);
            for (int i = 0; i < leafCount; i++)
            {
                float angle = (360f / leafCount) * i + RandomRange(random, -18f, 18f);
                float radius = scale * RandomRange(random, 0.1f, 0.28f);
                float height = scale * RandomRange(random, 0.12f, 0.34f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushLeaf(parent, offset, angle + RandomRange(random, -18f, 18f), scale * RandomRange(random, 0.16f, 0.24f), RandomRange(random, 0.3f, 1f));
            }
        }

        private void CreateWildBushVariant(Transform parent, float scale, System.Random random)
        {
            CreateBushStemCluster(parent, scale, random, 5, 0.36f, 0.24f);

            for (int i = 0; i < 5; i++)
            {
                float angle = (72f * i) + RandomRange(random, -16f, 16f);
                CreateBushSprig(parent, angle, scale, random, scale * 0.14f, scale * 0.28f, 3, 5);
            }

            int broadLeafCount = random.Next(14, 20);
            for (int i = 0; i < broadLeafCount; i++)
            {
                float angle = (360f / broadLeafCount) * i + RandomRange(random, -24f, 24f);
                float radius = scale * RandomRange(random, 0.14f, 0.34f);
                float height = scale * RandomRange(random, 0.16f, 0.42f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushBroadLeaf(parent, offset, angle, scale * RandomRange(random, 0.2f, 0.3f), scale * RandomRange(random, 0.06f, 0.085f), random);
            }

            int clusterCount = 5;
            for (int i = 0; i < clusterCount; i++)
            {
                float angle = (360f / clusterCount) * i + RandomRange(random, -26f, 26f);
                float radius = scale * RandomRange(random, 0.18f, 0.4f);
                float height = scale * RandomRange(random, 0.14f, 0.28f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushLeafCluster(parent, offset, new Vector3(scale * RandomRange(random, 0.22f, 0.32f), scale * RandomRange(random, 0.12f, 0.2f), scale * RandomRange(random, 0.18f, 0.3f)), angle, random);
            }
        }

        private void CreateBushStemCluster(Transform parent, float scale, System.Random random, int stemCount, float stemHeightScale, float spreadScale)
        {
            for (int i = 0; i < stemCount; i++)
            {
                float angle = (360f / stemCount) * i + RandomRange(random, -18f, 18f);
                float radius = scale * RandomRange(random, 0.02f, spreadScale);
                Vector3 localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, scale * 0.08f, 0f);

                GameObject stem = CreateMeshPart("Stem", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), trunkMaterial, parent);
                stem.transform.localPosition = localPosition;
                stem.transform.localRotation = Quaternion.Euler(RandomRange(random, 18f, 34f), angle, RandomRange(random, -18f, 18f));
                stem.transform.localScale = new Vector3(scale * RandomRange(random, 0.03f, 0.05f), scale * RandomRange(random, stemHeightScale * 0.75f, stemHeightScale), scale * RandomRange(random, 0.03f, 0.05f));
                ApplyTint(stem, Color.Lerp(new Color(0.31f, 0.22f, 0.13f), new Color(0.42f, 0.31f, 0.17f), i / (float)Mathf.Max(1, stemCount - 1)));
            }
        }

        private void CreateBushSprig(Transform parent, float yaw, float scale, System.Random random, float baseHeight, float tipHeight, int minLeavesPerSide, int maxLeavesPerSide)
        {
            GameObject sprigRoot = new GameObject("Sprig");
            sprigRoot.transform.SetParent(parent, false);
            sprigRoot.transform.localPosition = new Vector3(0f, baseHeight, 0f);
            sprigRoot.transform.localRotation = Quaternion.Euler(
                RandomRange(random, 16f, 34f),
                yaw + RandomRange(random, -12f, 12f),
                RandomRange(random, -18f, 18f));

            GameObject stem = CreateMeshPart("SprigStem", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), trunkMaterial, sprigRoot.transform);
            stem.transform.localPosition = new Vector3(0f, scale * 0.12f, 0f);
            stem.transform.localScale = new Vector3(scale * 0.02f, scale * 0.14f, scale * 0.02f);
            ApplyTint(stem, new Color(0.3f, 0.22f, 0.13f));

            int leavesPerSide = random.Next(minLeavesPerSide, maxLeavesPerSide);
            for (int i = 0; i < leavesPerSide; i++)
            {
                float t = i / (float)Mathf.Max(1, leavesPerSide - 1);
                float height = Mathf.Lerp(scale * 0.05f, tipHeight, t);
                float outwards = Mathf.Lerp(scale * 0.05f, scale * 0.2f, t);
                float leafLength = Mathf.Lerp(scale * 0.2f, scale * 0.1f, t);

                CreateBushLeaf(sprigRoot.transform, new Vector3(outwards, height, 0f), 24f + (t * 20f), leafLength, Mathf.Lerp(0.2f, 1f, t));
                CreateBushLeaf(sprigRoot.transform, new Vector3(-outwards, height, 0f), -24f - (t * 20f), leafLength, Mathf.Lerp(0.2f, 1f, t));
            }
        }

        private void CreateBushBroadLeaf(Transform parent, Vector3 localPosition, float yaw, float length, float width, System.Random random)
        {
            GameObject broadLeaf = CreateMeshPart("BroadLeaf", cachedCapsuleMesh ??= GetPrimitiveMesh(PrimitiveType.Capsule), leavesMaterial, parent);
            broadLeaf.transform.localPosition = localPosition;
            broadLeaf.transform.localRotation = Quaternion.Euler(
                84f + RandomRange(random, -12f, 12f),
                yaw + RandomRange(random, -22f, 22f),
                RandomRange(random, -18f, 18f));
            broadLeaf.transform.localScale = new Vector3(width, length, width * 1.12f);
            ApplyTint(broadLeaf, Color.Lerp(new Color(0.18f, 0.38f, 0.16f), new Color(0.35f, 0.62f, 0.28f), (Mathf.Sin(yaw * Mathf.Deg2Rad) + 1f) * 0.5f));
        }

        private void CreateBushLeafCluster(Transform parent, Vector3 localPosition, Vector3 localScale, float yaw, System.Random random)
        {
            GameObject cluster = CreateMeshPart("LeafCluster", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), leavesMaterial, parent);
            cluster.transform.localPosition = localPosition;
            cluster.transform.localRotation = Quaternion.Euler(RandomRange(random, -16f, 16f), yaw, RandomRange(random, -20f, 20f));
            cluster.transform.localScale = localScale;
            ApplyTint(cluster, Color.Lerp(new Color(0.19f, 0.41f, 0.17f), new Color(0.32f, 0.58f, 0.25f), (Mathf.Cos(yaw * Mathf.Deg2Rad) + 1f) * 0.5f));
        }

        private void CreateBushLeaf(Transform parent, Vector3 localPosition, float yaw, float length, float tintLerp)
        {
            GameObject leaf = CreateMeshPart("Leaf", cachedCapsuleMesh ??= GetPrimitiveMesh(PrimitiveType.Capsule), leavesMaterial, parent);
            leaf.transform.localPosition = localPosition;
            leaf.transform.localRotation = Quaternion.Euler(90f, yaw, Mathf.Sign(yaw) * 10f);
            leaf.transform.localScale = new Vector3(length * 0.16f, length * 0.5f, length * 0.13f);
            ApplyTint(leaf, Color.Lerp(new Color(0.18f, 0.38f, 0.16f), new Color(0.34f, 0.6f, 0.26f), tintLerp));
        }

        private void CreatePalmFrond(Transform parent, Vector3 crownCenter, float yaw, float length)
        {
            GameObject frondRoot = new GameObject("Frond");
            frondRoot.transform.SetParent(parent, false);
            frondRoot.transform.localPosition = crownCenter;
            frondRoot.transform.localRotation = Quaternion.Euler(12f, yaw, -30f);

            GameObject spine = CreateMeshPart("Spine", cachedCubeMesh ??= GetPrimitiveMesh(PrimitiveType.Cube), leavesMaterial, frondRoot.transform);
            spine.transform.localPosition = new Vector3(0f, 0.02f, length * 0.44f);
            spine.transform.localScale = new Vector3(0.04f, 0.025f, length * 0.92f);
            ApplyTint(spine, new Color(0.28f, 0.58f, 0.24f));

            int leafletPairs = 10;
            for (int i = 0; i < leafletPairs; i++)
            {
                float t = i / (float)(leafletPairs - 1);
                float z = Mathf.Lerp(0.18f, length * 0.84f, t);
                float spread = Mathf.Lerp(0.14f, 0.52f, t);
                float leafletLength = Mathf.Lerp(0.46f, 0.16f, t);
                float droop = Mathf.Lerp(18f, 54f, t);

                CreatePalmLeaflet(frondRoot.transform, new Vector3(spread, 0f, z), yaw, droop, leafletLength);
                CreatePalmLeaflet(frondRoot.transform, new Vector3(-spread, 0f, z), yaw, -droop, leafletLength);
            }
        }

        private void CreatePalmLeaflet(Transform parent, Vector3 localPosition, float yaw, float sideAngle, float leafletLength)
        {
            GameObject leaflet = CreateMeshPart("Leaflet", cachedCapsuleMesh ??= GetPrimitiveMesh(PrimitiveType.Capsule), leavesMaterial, parent);
            leaflet.transform.localPosition = localPosition;
            leaflet.transform.localRotation = Quaternion.Euler(90f, sideAngle, Mathf.Sign(sideAngle) * 16f);
            leaflet.transform.localScale = new Vector3(0.04f, leafletLength * 0.38f, 0.05f);
            ApplyTint(leaflet, Color.Lerp(new Color(0.24f, 0.5f, 0.22f), new Color(0.35f, 0.66f, 0.28f), Mathf.Abs(sideAngle) / 42f));
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private void CreateDriftwood(Transform parent, Vector3 position, float length, float yaw)
        {
            GameObject driftwood = CreateMeshPart("Driftwood", cachedCapsuleMesh ??= GetPrimitiveMesh(PrimitiveType.Capsule), dockMaterial, parent);
            driftwood.transform.localPosition = position + new Vector3(0f, 0.18f, 0f);
            driftwood.transform.localRotation = Quaternion.Euler(82f, yaw, 14f);
            driftwood.transform.localScale = new Vector3(0.18f, length * 0.5f, 0.18f);

            IslandDriftwoodInteraction interaction = GetOrAddComponent<IslandDriftwoodInteraction>(driftwood);
            interaction.Configure(Mathf.Clamp(length * 0.72f, 2f, 3.4f));
        }

        private void CreatePebble(Transform parent, Vector3 position, float scale, float yaw)
        {
            GameObject pebble = CreateMeshPart("Pebble", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), rockMaterial, parent);
            pebble.transform.localPosition = position + new Vector3(0f, scale * 0.08f, 0f);
            pebble.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            pebble.transform.localScale = new Vector3(scale * 1.2f, scale * 0.35f, scale);
        }

        private void CreateStump(Transform parent, Vector3 position, float scale, float yaw)
        {
            GameObject stump = new GameObject("Stump");
            stump.transform.SetParent(parent, false);
            stump.transform.localPosition = position;
            stump.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject trunk = CreateMeshPart("Base", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), trunkMaterial, stump.transform);
            trunk.transform.localPosition = new Vector3(0f, scale * 0.32f, 0f);
            trunk.transform.localScale = new Vector3(scale * 0.26f, scale * 0.32f, scale * 0.26f);

            GameObject top = CreateMeshPart("Top", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), dockMaterial, stump.transform);
            top.transform.localPosition = new Vector3(0f, scale * 0.65f, 0f);
            top.transform.localScale = new Vector3(scale * 0.22f, scale * 0.05f, scale * 0.22f);
        }

        private void CreateHiddenCollectibles(Transform parent, System.Random random, Vector3 characterSpawn, Vector2 dockDirection)
        {
            PlaceHiddenCollectible(parent, IslandItemCatalog.MapId, "HiddenTreasureMap", 1.05f, random, characterSpawn, dockDirection, islandSize * 0.24f, islandSize * 0.42f, seaLevel + 0.75f, peakHeight + 1.2f);
            PlaceHiddenCollectible(parent, IslandItemCatalog.CompassId, "HiddenCompass", 0.95f, random, characterSpawn, dockDirection, islandSize * 0.2f, islandSize * 0.45f, seaLevel + 0.45f, peakHeight + 1.2f);
            PlaceHiddenCollectible(parent, IslandItemCatalog.TorchId, "HiddenTorch", 1.02f, random, characterSpawn, dockDirection, islandSize * 0.34f, islandSize * 0.48f, seaLevel - 0.05f, seaLevel + 1.5f);
            PlaceHiddenCollectible(parent, IslandItemCatalog.CanteenId, "HiddenCanteen", 0.92f, random, characterSpawn, dockDirection, islandSize * 0.26f, islandSize * 0.46f, seaLevel + 0.3f, seaLevel + 2.4f);
        }

        private void PlaceHiddenCollectible(
            Transform parent,
            string itemId,
            string objectName,
            float scale,
            System.Random random,
            Vector3 characterSpawn,
            Vector2 dockDirection,
            float minRadius,
            float maxRadius,
            float minHeight,
            float maxHeight)
        {
            for (int attempt = 0; attempt < 36; attempt++)
            {
                float angle = RandomRange(random, 0f, Mathf.PI * 2f);
                float radius = RandomRange(random, minRadius, maxRadius);
                Vector3 position = SampleSurfacePosition(angle, radius);

                if (position.y < minHeight || position.y > maxHeight || IsNearCharacterSpawn(position, characterSpawn, 11f))
                {
                    continue;
                }

                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (dockDirection.sqrMagnitude > 0.0001f && Vector2.Dot(direction, dockDirection.normalized) > 0.9f)
                {
                    continue;
                }

                IslandWorldItem collectible = IslandWorldItem.SpawnWorldItem(
                    itemId,
                    1,
                    position + new Vector3(0f, 0.04f, 0f),
                    Quaternion.Euler(0f, RandomRange(random, 0f, 360f), 0f),
                    false,
                    false,
                    Vector3.zero,
                    Vector3.zero,
                    -1f,
                    parent);

                if (collectible != null)
                {
                    collectible.name = objectName;
                    collectible.SetWorldScale(Vector3.one * scale);
                }

                return;
            }
        }

        private void BuildCampfire(Transform parent, Vector3 position, Vector2 dockDirection)
        {
            campfireEmberMaterial ??= CreateRuntimeMaterial("Campfire Ember Material");
            campfireAshMaterial ??= CreateRuntimeMaterial("Campfire Ash Material");
            campfireStoneMaterial ??= CreateRuntimeMaterial("Campfire Stone Material");

            campfireEmberMaterial.SetColor("_BaseColor", new Color(0.28f, 0.15f, 0.1f));
            campfireEmberMaterial.SetColor("_EmissionColor", new Color(0.75f, 0.24f, 0.05f) * 0.15f);
            campfireEmberMaterial.EnableKeyword("_EMISSION");
            campfireEmberMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            campfireEmberMaterial.SetFloat("_Smoothness", 0.06f);

            campfireAshMaterial.SetColor("_BaseColor", new Color(0.26f, 0.26f, 0.25f));
            campfireAshMaterial.SetFloat("_Smoothness", 0.02f);

            campfireStoneMaterial.SetColor("_BaseColor", new Color(0.36f, 0.33f, 0.31f));
            campfireStoneMaterial.SetFloat("_Smoothness", 0.05f);

            GameObject campfire = new GameObject("Campfire");
            campfire.transform.SetParent(parent, false);
            campfire.transform.localPosition = position;

            Vector2 forward2D = dockDirection.sqrMagnitude > 0.0001f ? -dockDirection.normalized : new Vector2(-0.42f, -0.91f).normalized;
            campfire.transform.localRotation = Quaternion.LookRotation(new Vector3(forward2D.x, 0f, forward2D.y));

            GameObject outerPad = CreateMeshPart("OuterPad", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), campfireAshMaterial, campfire.transform);
            outerPad.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            outerPad.transform.localScale = new Vector3(2.15f, 0.06f, 2.15f);
            ApplyTint(outerPad, new Color(0.11f, 0.1f, 0.1f));

            GameObject innerAsh = CreateMeshPart("InnerAsh", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), campfireAshMaterial, campfire.transform);
            innerAsh.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            innerAsh.transform.localScale = new Vector3(1.35f, 0.045f, 1.35f);
            ApplyTint(innerAsh, new Color(0.26f, 0.24f, 0.23f));

            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                float radius = 1.18f + (0.08f * Mathf.Sin(i * 1.9f));
                Vector3 localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, 0.18f + (0.04f * (i % 2)), 0f);
                GameObject stone = CreateMeshPart($"FireStone_{i}", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), campfireStoneMaterial, campfire.transform);
                stone.transform.localPosition = localPosition;
                stone.transform.localRotation = Quaternion.Euler(i * 11f, angle, i * 17f);
                stone.transform.localScale = new Vector3(0.38f, 0.24f, 0.34f) * (1f + ((i % 3) * 0.1f));
                ApplyTint(stone, Color.Lerp(new Color(0.31f, 0.29f, 0.28f), new Color(0.43f, 0.39f, 0.36f), (i % 4) / 3f));
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = (i * 120f) + 12f;
                GameObject log = CreateMeshPart($"MainLog_{i}", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), trunkMaterial, campfire.transform);
                log.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.46f, 0.26f + (i * 0.025f), 0f);
                log.transform.localRotation = Quaternion.Euler(86f, angle, 0f);
                log.transform.localScale = new Vector3(0.26f, 1.18f, 0.26f);
                ApplyTint(log, Color.Lerp(new Color(0.34f, 0.23f, 0.13f), new Color(0.19f, 0.13f, 0.08f), i * 0.24f));
            }

            for (int i = 0; i < 5; i++)
            {
                float angle = (i * 72f) + 18f;
                GameObject kindling = CreateMeshPart($"Kindling_{i}", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), dockMaterial, campfire.transform);
                kindling.transform.localPosition = new Vector3(0f, 0.2f + (i * 0.035f), 0f);
                kindling.transform.localRotation = Quaternion.Euler(60f + (i * 6f), angle, 0f);
                kindling.transform.localScale = new Vector3(0.07f, 0.58f, 0.07f);
                ApplyTint(kindling, new Color(0.49f, 0.31f, 0.17f));
            }

            for (int i = 0; i < 7; i++)
            {
                float angle = i * (360f / 7f);
                float radius = 0.18f + (0.06f * (i % 3));
                GameObject ember = CreateMeshPart($"EmberCoal_{i}", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), campfireEmberMaterial, campfire.transform);
                ember.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, 0.13f + (0.04f * (i % 2)), 0f);
                ember.transform.localScale = new Vector3(0.18f, 0.1f, 0.16f) * (1f + (i * 0.05f));
            }

            Transform fireAnchor = EnsureChild(campfire.transform, "FireAnchor");
            fireAnchor.localPosition = new Vector3(0f, 0.34f, 0f);

            GameObject charPatch = CreateMeshPart("CharPatch", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), campfireAshMaterial, campfire.transform);
            charPatch.transform.localPosition = new Vector3(0f, 0.085f, 0f);
            charPatch.transform.localScale = new Vector3(0.62f, 0.03f, 0.62f);
            ApplyTint(charPatch, new Color(0.12f, 0.1f, 0.1f));

            BuildCampfireWoodpile(campfire.transform, dockDirection);

            SphereCollider blocker = GetOrAddComponent<SphereCollider>(campfire);
            blocker.isTrigger = false;
            blocker.center = new Vector3(0f, 0.36f, 0f);
            blocker.radius = 1.3f;

            IslandCampfireInteraction interaction = GetOrAddComponent<IslandCampfireInteraction>(campfire);
            interaction.Configure(4.9f, 1.55f, false);
        }

        private void BuildCampfireWoodpile(Transform campfire, Vector2 dockDirection)
        {
            Vector2 side = dockDirection.sqrMagnitude > 0.0001f
                ? new Vector2(-dockDirection.y, dockDirection.x).normalized
                : Vector2.right;

            CreateCampfireSeatLog(campfire, "SeatLogLeft", new Vector3(side.x * 2.7f, 0.24f, side.y * 2.7f), Quaternion.Euler(90f, 12f, 0f));
            CreateCampfireSeatLog(campfire, "SeatLogRight", new Vector3(-side.x * 2.7f, 0.24f, -side.y * 2.7f), Quaternion.Euler(90f, -12f, 0f));
        }

        private void CreateCampfireSeatLog(Transform campfire, string name, Vector3 localPosition, Quaternion localRotation)
        {
            GameObject seatLog = CreateMeshPart(name, cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), trunkMaterial, campfire);
            seatLog.transform.localPosition = localPosition;
            seatLog.transform.localRotation = localRotation;
            seatLog.transform.localScale = new Vector3(0.34f, 1.72f, 0.34f);
            ApplyTint(seatLog, new Color(0.28f, 0.18f, 0.1f));

            CapsuleCollider collider = seatLog.AddComponent<CapsuleCollider>();
            collider.direction = 2;
            collider.radius = 0.34f;
            collider.height = 3.4f;
            collider.center = Vector3.zero;
        }

        private Vector3 GetCampfirePosition(Vector2 dockDirection, Vector3 characterSpawn)
        {
            Vector2 normalizedDockDirection = dockDirection.sqrMagnitude > 0.0001f
                ? dockDirection.normalized
                : new Vector2(0.42f, 0.91f).normalized;
            Vector2 side = new Vector2(-normalizedDockDirection.y, normalizedDockDirection.x);
            Vector3 position = characterSpawn + new Vector3(side.x * 6.4f, 0f, side.y * 6.4f) - new Vector3(normalizedDockDirection.x * 2.3f, 0f, normalizedDockDirection.y * 2.3f);
            position = ClampToIslandBuildPosition(position);
            position.y = IslandMeshBuilder.SampleHeight(position.x, position.z, islandSize, peakHeight);
            return position;
        }

        private Vector3 ClampToIslandBuildPosition(Vector3 position)
        {
            Vector2 planar = new Vector2(position.x, position.z);
            float maxRadius = islandSize * 0.43f;
            if (planar.magnitude > maxRadius)
            {
                planar = planar.normalized * maxRadius;
            }

            position.x = planar.x;
            position.z = planar.y;
            return position;
        }

        private void CreateIslandExplorer(Transform parent)
        {
            GameObject torso = CreateMeshPart("Torso", cachedCapsuleMesh ??= GetPrimitiveMesh(PrimitiveType.Capsule), characterShirtMaterial, parent);
            torso.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            torso.transform.localScale = new Vector3(0.68f, 0.62f, 0.56f);
            HideFromFirstPerson(torso);

            GameObject hips = CreateMeshPart("Hips", cachedCubeMesh ??= GetPrimitiveMesh(PrimitiveType.Cube), characterShortsMaterial, parent);
            hips.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            hips.transform.localScale = new Vector3(0.7f, 0.36f, 0.44f);
            HideFromFirstPerson(hips);

            GameObject leftLeg = CreateMeshPart("LeftLeg", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterSkinMaterial, parent);
            leftLeg.transform.localPosition = new Vector3(-0.18f, 0.36f, 0f);
            leftLeg.transform.localScale = new Vector3(0.12f, 0.36f, 0.12f);
            HideFromFirstPerson(leftLeg);

            GameObject rightLeg = CreateMeshPart("RightLeg", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterSkinMaterial, parent);
            rightLeg.transform.localPosition = new Vector3(0.18f, 0.36f, 0f);
            rightLeg.transform.localScale = new Vector3(0.12f, 0.36f, 0.12f);
            HideFromFirstPerson(rightLeg);

            GameObject leftArm = CreateMeshPart("LeftArm", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterSkinMaterial, parent);
            leftArm.transform.localPosition = new Vector3(-0.5f, 1.36f, 0f);
            leftArm.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            leftArm.transform.localScale = new Vector3(0.09f, 0.34f, 0.09f);
            HideFromFirstPerson(leftArm);

            GameObject rightArm = CreateMeshPart("RightArm", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterSkinMaterial, parent);
            rightArm.transform.localPosition = new Vector3(0.5f, 1.36f, 0f);
            rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            rightArm.transform.localScale = new Vector3(0.09f, 0.34f, 0.09f);
            HideFromFirstPerson(rightArm);

            GameObject head = CreateMeshPart("Head", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), characterSkinMaterial, parent);
            head.transform.localPosition = new Vector3(0f, 2.12f, 0f);
            head.transform.localScale = new Vector3(0.52f, 0.58f, 0.52f);
            HideFromFirstPerson(head);

            GameObject hair = CreateMeshPart("Hair", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), characterHairMaterial, parent);
            hair.transform.localPosition = new Vector3(0f, 2.26f, -0.04f);
            hair.transform.localScale = new Vector3(0.54f, 0.3f, 0.54f);
            HideFromFirstPerson(hair);

            GameObject hatBrim = CreateMeshPart("HatBrim", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterStrawMaterial, parent);
            hatBrim.transform.localPosition = new Vector3(0f, 2.43f, 0f);
            hatBrim.transform.localScale = new Vector3(0.42f, 0.03f, 0.42f);
            HideFromFirstPerson(hatBrim);

            GameObject hatCrown = CreateMeshPart("HatCrown", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterStrawMaterial, parent);
            hatCrown.transform.localPosition = new Vector3(0f, 2.56f, 0f);
            hatCrown.transform.localScale = new Vector3(0.25f, 0.13f, 0.25f);
            HideFromFirstPerson(hatCrown);

            GameObject backpack = CreateMeshPart("Backpack", cachedCubeMesh ??= GetPrimitiveMesh(PrimitiveType.Cube), dockMaterial, parent);
            backpack.transform.localPosition = new Vector3(0f, 1.32f, -0.27f);
            backpack.transform.localScale = new Vector3(0.34f, 0.5f, 0.16f);
            HideFromFirstPerson(backpack);
        }

        private static void HideFromFirstPerson(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            renderer.receiveShadows = false;
        }

        private void ConfigureRockObstacleCollider(GameObject rock)
        {
            if (rock == null || !IslandInteractionUtility.TryGetCompositeBounds(rock.transform, out Bounds bounds))
            {
                return;
            }

            SphereCollider collider = GetOrAddComponent<SphereCollider>(rock);
            collider.isTrigger = false;
            collider.center = rock.transform.InverseTransformPoint(bounds.center);
            collider.radius = Mathf.Max(0.45f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.72f);
        }

        private void ConfigurePalmObstacleCollider(GameObject palm, float height)
        {
            if (palm == null)
            {
                return;
            }

            CapsuleCollider collider = GetOrAddComponent<CapsuleCollider>(palm);
            collider.isTrigger = false;
            collider.direction = 1;
            collider.center = new Vector3(0f, height * 0.46f, 0f);
            collider.height = Mathf.Max(1.6f, height * 0.9f);
            collider.radius = Mathf.Clamp(height * 0.045f, 0.22f, 0.34f);
        }

        private Vector3 SampleSurfacePosition(float angle, float radius)
        {
            float clampedRadius = Mathf.Min(radius, islandSize * 0.48f);
            float x = Mathf.Cos(angle) * clampedRadius;
            float z = Mathf.Sin(angle) * clampedRadius;
            float y = IslandMeshBuilder.SampleHeight(x, z, islandSize, peakHeight);
            return new Vector3(x, y, z);
        }

        private Vector3 GetCharacterSpawnPosition(Vector2 dockDirection)
        {
            float radius = islandSize * 0.24f;
            Vector3 position = new Vector3(dockDirection.x, 0f, dockDirection.y) * radius;
            position.y = IslandMeshBuilder.SampleHeight(position.x, position.z, islandSize, peakHeight);
            return position;
        }

        private static bool IsNearCharacterSpawn(Vector3 position, Vector3 spawnPosition, float clearance)
        {
            Vector2 planarDelta = new Vector2(position.x - spawnPosition.x, position.z - spawnPosition.z);
            return planarDelta.sqrMagnitude < clearance * clearance;
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void RemoveComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(component);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static T GetOrAddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(true);
            }

            component.active = true;
            return component;
        }

        private static Material CreateRuntimeMaterial(string materialName)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");

            Material material = new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };

            return material;
        }

        private static Mesh CreateRuntimeMesh(string meshName)
        {
            return new Mesh
            {
                name = meshName,
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
        }

        private static Texture2D CreateRuntimeTexture(string textureName)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true)
            {
                name = textureName,
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };

            return texture;
        }

        private static GameObject CreateMeshPart(string name, Mesh mesh, Material material, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            MeshFilter filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return go;
        }

        private static void ApplyTint(GameObject gameObject, Color tint)
        {
            if (gameObject == null)
            {
                return;
            }

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, tint);
            block.SetColor(ColorId, tint);
            renderer.SetPropertyBlock(block);
        }

        private static float RandomWave(float value, float offset)
        {
            return Mathf.Sin(value + offset);
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType primitiveType)
        {
            string resourceName = primitiveType switch
            {
                PrimitiveType.Cube => "Cube.fbx",
                PrimitiveType.Sphere => "Sphere.fbx",
                PrimitiveType.Capsule => "Capsule.fbx",
                PrimitiveType.Cylinder => "Cylinder.fbx",
                _ => null
            };

            if (!string.IsNullOrEmpty(resourceName))
            {
                Mesh builtInMesh = Resources.GetBuiltinResource<Mesh>(resourceName);
                if (builtInMesh != null)
                {
                    return builtInMesh;
                }
            }

            // Fallback for unexpected editor/runtime environments where the built-in mesh lookup fails.
            GameObject temporary = GameObject.CreatePrimitive(primitiveType);
            Mesh mesh = temporary.GetComponent<MeshFilter>().sharedMesh;

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(temporary);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }

            return mesh;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }
            }
        }

        private static void ReapplyWorldControls()
        {
            WorldEnvironmentManager worldEnvironmentManager = FindAnyObjectByType<WorldEnvironmentManager>(FindObjectsInactive.Exclude);
            if (worldEnvironmentManager != null)
            {
                worldEnvironmentManager.ApplyWorldState();
            }
        }

        private void ReleaseGeneratedResources()
        {
            ReleaseObject(terrainMesh);
            ReleaseObject(waterMesh);
            ReleaseObject(terrainTexture);
            ReleaseObject(terrainMaterial);
            ReleaseObject(waterMaterial);
            ReleaseObject(trunkMaterial);
            ReleaseObject(leavesMaterial);
            ReleaseObject(rockMaterial);
            ReleaseObject(dockMaterial);
            ReleaseObject(characterSkinMaterial);
            ReleaseObject(characterShirtMaterial);
            ReleaseObject(characterShortsMaterial);
            ReleaseObject(characterStrawMaterial);
            ReleaseObject(characterHairMaterial);
            ReleaseObject(campfireEmberMaterial);
            ReleaseObject(campfireAshMaterial);
            ReleaseObject(campfireStoneMaterial);

            terrainMesh = null;
            waterMesh = null;
            terrainTexture = null;
            terrainMaterial = null;
            waterMaterial = null;
            trunkMaterial = null;
            leavesMaterial = null;
            rockMaterial = null;
            dockMaterial = null;
            characterSkinMaterial = null;
            characterShirtMaterial = null;
            characterShortsMaterial = null;
            characterStrawMaterial = null;
            characterHairMaterial = null;
            campfireEmberMaterial = null;
            campfireAshMaterial = null;
            campfireStoneMaterial = null;
        }

        private static void ReleaseObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
