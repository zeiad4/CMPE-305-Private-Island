using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
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
        private Material bushAssetMaterial;
        private Material rockAssetMaterial;
        private GameObject bushAssetPrefab;
        private GameObject rockAssetPrefab;
        private bool isRebuilding;
#if UNITY_EDITOR
        private bool editorRebuildQueued;
#endif

        private static Mesh cachedCubeMesh;
        private static Mesh cachedCylinderMesh;
        private static Mesh cachedSphereMesh;
        private static Mesh cachedCapsuleMesh;

        private const string BushAssetResourcePath = "Nature/Bush_Common";
        private const string RockAssetResourcePath = "Nature/Rock_Medium_1";
        private const string BushTextureResourcePath = "Nature/Leaves";
        private const string RockTextureResourcePath = "Nature/Rocks_Diffuse";
        private const float RockPlacementRadius = 4.4f;
        private const float BushPlacementRadius = 3.2f;

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

            leavesMaterial.SetColor("_BaseColor", new Color(0.29f, 0.58f, 0.22f));
            leavesMaterial.SetFloat("_Smoothness", 0.06f);

            rockMaterial.SetColor("_BaseColor", new Color(0.5f, 0.52f, 0.54f));
            rockMaterial.SetFloat("_Smoothness", 0.06f);

            dockMaterial.SetColor("_BaseColor", new Color(0.52f, 0.38f, 0.24f));
            dockMaterial.SetFloat("_Smoothness", 0.2f);

            EnsureNatureAssetsLoaded();

            System.Random random = new System.Random(seed);
            Vector2 dockDirection = new Vector2(0.42f, 0.91f).normalized;
            Vector3 characterSpawn = GetCharacterSpawnPosition(dockDirection);
            List<Vector2> reservedDecorPositions = new List<Vector2>(rockCount + bushCount);
            List<float> reservedDecorRadii = new List<float>(rockCount + bushCount);

            BuildDock(propsRoot, dockDirection);

            BuildFixedRockSet(propsRoot, characterSpawn, reservedDecorPositions, reservedDecorRadii);

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

            BuildFixedBushSet(propsRoot, characterSpawn, reservedDecorPositions, reservedDecorRadii);

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
            GetOrAddComponent<IslandPlayerInteractor>(characterRoot.gameObject);
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
            rock.transform.localPosition = position + new Vector3(0f, -scale * 0.08f, 0f);
            rock.transform.localRotation = Quaternion.Euler(
                RandomRange(random, -6f, 6f),
                yaw + RandomRange(random, -18f, 18f),
                RandomRange(random, -5f, 5f));

            Color mainTint = new Color(0.55f, 0.56f, 0.58f);
            Color coolTint = new Color(0.43f, 0.49f, 0.57f);
            Color warmTint = new Color(0.68f, 0.63f, 0.54f);
            Color shadowTint = new Color(0.33f, 0.37f, 0.44f);

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
            ConfigureRockObstacleCollider(rock);

            IslandRockInteraction interaction = GetOrAddComponent<IslandRockInteraction>(rock);
            interaction.Configure(Mathf.Clamp(scale * 1.18f, 2.2f, 4.6f), scale);
        }

        private void BuildFixedRockSet(
            Transform parent,
            Vector3 characterSpawn,
            List<Vector2> reservedDecorPositions,
            List<float> reservedDecorRadii)
        {
            int placedRocks = 0;
            int candidateCount = Mathf.Max(rockCount * 4, 28);

            for (int i = 0; i < candidateCount && placedRocks < rockCount; i++)
            {
                float scale = GetDeterministicRockScale(i);
                if (!TryGetDeterministicPlacement(
                        i,
                        candidateCount,
                        islandSize * 0.16f,
                        islandSize * 0.46f,
                        seaLevel + 0.2f,
                        characterSpawn,
                        5.5f,
                        0.13f,
                        RockPlacementRadius * (scale / 1.8f),
                        reservedDecorPositions,
                        reservedDecorRadii,
                        out Vector3 position))
                {
                    continue;
                }

                CreateRockAssetInstance(parent, position, scale);
                placedRocks++;
            }
        }

        private void BuildFixedBushSet(
            Transform parent,
            Vector3 characterSpawn,
            List<Vector2> reservedDecorPositions,
            List<float> reservedDecorRadii)
        {
            int placedBushes = 0;
            int candidateCount = Mathf.Max(bushCount * 4, 32);

            for (int i = 0; i < candidateCount && placedBushes < bushCount; i++)
            {
                if (!TryGetDeterministicPlacement(
                        i,
                        candidateCount,
                        islandSize * 0.1f,
                        islandSize * 0.44f,
                        seaLevel + 0.5f,
                        characterSpawn,
                        7.5f,
                        0.37f,
                        BushPlacementRadius,
                        reservedDecorPositions,
                        reservedDecorRadii,
                        out Vector3 position))
                {
                    continue;
                }

                CreateBushAssetInstance(parent, position);
                placedBushes++;
            }
        }

        private bool TryGetDeterministicPlacement(
            int index,
            int candidateCount,
            float minRadius,
            float maxRadius,
            float minimumHeight,
            Vector3 characterSpawn,
            float clearance,
            float radialOffset,
            float footprintRadius,
            List<Vector2> reservedDecorPositions,
            List<float> reservedDecorRadii,
            out Vector3 position)
        {
            float angle = Mathf.Repeat(index * 137.50776f, 360f) * Mathf.Deg2Rad;
            float radialT = Mathf.Repeat((index * 0.61803395f) + radialOffset, 1f);
            float bandT = (index + 0.5f) / candidateCount;
            float radius = Mathf.Lerp(minRadius, maxRadius, Mathf.Lerp(radialT, bandT, 0.35f));
            position = SampleSurfacePosition(angle, radius);

            if (position.y < minimumHeight || IsNearCharacterSpawn(position, characterSpawn, clearance))
            {
                return false;
            }

            if (!TryReserveDecorFootprint(position, footprintRadius, reservedDecorPositions, reservedDecorRadii))
            {
                return false;
            }

            return true;
        }

        private void CreateRockAssetInstance(Transform parent, Vector3 position, float scale)
        {
            if (rockAssetPrefab == null)
            {
                CreateRock(parent, position, scale, 0f, new System.Random(0));
                return;
            }

            GameObject rock = new GameObject("Rock");
            rock.transform.SetParent(parent, false);
            rock.transform.localPosition = position + new Vector3(0f, -0.06f * (scale / 1.8f), 0f);
            rock.transform.localRotation = Quaternion.identity;
            rock.transform.localScale = Vector3.one * scale;

            GameObject visual = Instantiate(rockAssetPrefab, rock.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            ApplyMaterialToHierarchy(visual.transform, rockAssetMaterial);
            ConfigureRockObstacleCollider(rock);

            IslandRockInteraction interaction = GetOrAddComponent<IslandRockInteraction>(rock);
            interaction.Configure(Mathf.Clamp(scale * 1.68f, 2.3f, 4.4f), scale);
        }

        private void CreateBushAssetInstance(Transform parent, Vector3 position)
        {
            if (bushAssetPrefab == null)
            {
                CreateBush(parent, position, 1.45f, 0f, new System.Random(0));
                return;
            }

            GameObject bush = new GameObject("Bush");
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = position;
            bush.transform.localRotation = Quaternion.identity;
            bush.transform.localScale = Vector3.one * 1.6f;

            GameObject visual = Instantiate(bushAssetPrefab, bush.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            ApplyMaterialToHierarchy(visual.transform, bushAssetMaterial);

            IslandBushReactive interaction = GetOrAddComponent<IslandBushReactive>(bush);
            interaction.Configure(1.4f);
        }

        private void EnsureNatureAssetsLoaded()
        {
            bushAssetPrefab ??= Resources.Load<GameObject>(BushAssetResourcePath);
            rockAssetPrefab ??= Resources.Load<GameObject>(RockAssetResourcePath);

            bushAssetMaterial ??= CreateNatureAssetMaterial("Island Bush Asset Material", BushTextureResourcePath, true, true);
            rockAssetMaterial ??= CreateNatureAssetMaterial("Island Rock Asset Material", null, false, false);
            ApplyMaterialTint(rockAssetMaterial, new Color(0.66f, 0.68f, 0.71f));
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
            CreateRockPiece(parent, "Base", PrimitiveType.Capsule, new Vector3(0f, scale * 0.24f, 0f), new Vector3(scale * 1.02f, scale * 0.3f, scale * 0.82f), new Vector3(-8f, 14f, 3f), mainTint);
            CreateRockPiece(parent, "LeftMass", PrimitiveType.Cube, new Vector3(-scale * 0.28f, scale * 0.38f, scale * 0.12f), new Vector3(scale * 0.48f, scale * 0.28f, scale * 0.4f), new Vector3(12f, -10f, 18f), coolTint);
            CreateRockPiece(parent, "RightMass", PrimitiveType.Cube, new Vector3(scale * 0.24f, scale * 0.34f, -scale * 0.08f), new Vector3(scale * 0.44f, scale * 0.24f, scale * 0.38f), new Vector3(-10f, 20f, -11f), shadowTint);
            CreateRockPiece(parent, "TopCap", PrimitiveType.Cube, new Vector3(scale * 0.02f, scale * 0.56f, scale * 0.04f), new Vector3(scale * 0.34f, scale * 0.14f, scale * 0.3f), new Vector3(14f, 8f, -5f), warmTint);
            CreateRockPiece(parent, "FacePlate", PrimitiveType.Cube, new Vector3(-scale * 0.06f, scale * 0.42f, scale * 0.3f), new Vector3(scale * 0.3f, scale * 0.22f, scale * 0.08f), new Vector3(2f, 18f, 8f), new Color(0.77f, 0.73f, 0.65f));
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
            CreateRockPiece(parent, "BaseShelf", PrimitiveType.Cube, new Vector3(0f, scale * 0.18f, 0f), new Vector3(scale * 1.08f, scale * 0.22f, scale * 0.72f), new Vector3(-5f, 8f, 2f), mainTint);
            CreateRockPiece(parent, "MidShelf", PrimitiveType.Cube, new Vector3(-scale * 0.08f, scale * 0.36f, -scale * 0.12f), new Vector3(scale * 0.84f, scale * 0.17f, scale * 0.52f), new Vector3(8f, -16f, 7f), shadowTint);
            CreateRockPiece(parent, "TopShelf", PrimitiveType.Cube, new Vector3(scale * 0.08f, scale * 0.54f, scale * 0.04f), new Vector3(scale * 0.68f, scale * 0.12f, scale * 0.38f), new Vector3(4f, 18f, -2f), warmTint);
            CreateRockPiece(parent, "SideButtress", PrimitiveType.Cube, new Vector3(scale * 0.38f, scale * 0.26f, 0f), new Vector3(scale * 0.24f, scale * 0.22f, scale * 0.28f), new Vector3(-6f, 16f, -12f), coolTint);
            CreateRockPiece(parent, "SunPlate", PrimitiveType.Cube, new Vector3(-scale * 0.06f, scale * 0.48f, scale * 0.24f), new Vector3(scale * 0.4f, scale * 0.06f, scale * 0.16f), new Vector3(-18f, 24f, 14f), new Color(0.78f, 0.75f, 0.67f));
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
            CreateRockPiece(parent, "Base", PrimitiveType.Cube, new Vector3(0f, scale * 0.22f, 0f), new Vector3(scale * 0.88f, scale * 0.26f, scale * 0.64f), new Vector3(-10f, 4f, 2f), mainTint);
            CreateRockPiece(parent, "Spine", PrimitiveType.Cube, new Vector3(scale * 0.06f, scale * 0.62f, -scale * 0.06f), new Vector3(scale * 0.2f, scale * 0.56f, scale * 0.46f), new Vector3(8f, -6f, 16f), warmTint);
            CreateRockPiece(parent, "LeanSlab", PrimitiveType.Cube, new Vector3(-scale * 0.3f, scale * 0.44f, scale * 0.12f), new Vector3(scale * 0.32f, scale * 0.34f, scale * 0.26f), new Vector3(-8f, 24f, -14f), coolTint);
            CreateRockPiece(parent, "Wing", PrimitiveType.Cube, new Vector3(scale * 0.28f, scale * 0.28f, scale * 0.18f), new Vector3(scale * 0.26f, scale * 0.14f, scale * 0.18f), new Vector3(16f, 14f, -8f), shadowTint);
            CreateRockPiece(parent, "FaceShard", PrimitiveType.Cube, new Vector3(scale * 0.04f, scale * 0.5f, scale * 0.2f), new Vector3(scale * 0.16f, scale * 0.36f, scale * 0.08f), new Vector3(2f, 28f, 4f), new Color(0.74f, 0.71f, 0.64f));
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
            CreateRockPiece(parent, "MassA", PrimitiveType.Cube, new Vector3(-scale * 0.2f, scale * 0.28f, 0f), new Vector3(scale * 0.56f, scale * 0.3f, scale * 0.44f), new Vector3(-4f, 18f, 12f), mainTint);
            CreateRockPiece(parent, "MassB", PrimitiveType.Cube, new Vector3(scale * 0.22f, scale * 0.3f, -scale * 0.02f), new Vector3(scale * 0.58f, scale * 0.32f, scale * 0.46f), new Vector3(8f, -16f, -8f), coolTint);
            CreateRockPiece(parent, "CrackBase", PrimitiveType.Cube, new Vector3(0f, scale * 0.18f, -scale * 0.14f), new Vector3(scale * 0.58f, scale * 0.1f, scale * 0.24f), new Vector3(-2f, 8f, 2f), shadowTint);
            CreateRockPiece(parent, "TopBeam", PrimitiveType.Cube, new Vector3(scale * 0.04f, scale * 0.58f, scale * 0.08f), new Vector3(scale * 0.28f, scale * 0.16f, scale * 0.26f), new Vector3(12f, 10f, -12f), warmTint);
            CreateRockPiece(parent, "FrontPlate", PrimitiveType.Cube, new Vector3(-scale * 0.04f, scale * 0.44f, scale * 0.22f), new Vector3(scale * 0.22f, scale * 0.28f, scale * 0.08f), new Vector3(-8f, 24f, 5f), new Color(0.76f, 0.73f, 0.66f));
        }

        private void CreateRockDebrisRing(Transform parent, float scale, Color mainTint, Color shadowTint, System.Random random)
        {
            int debrisCount = random.Next(3, 6);
            for (int i = 0; i < debrisCount; i++)
            {
                float angle = (360f / debrisCount) * i + RandomRange(random, -24f, 24f);
                float radius = scale * RandomRange(random, 0.34f, 0.58f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, scale * RandomRange(random, 0.02f, 0.08f), 0f);
                CreateRockPiece(
                    parent,
                    $"Debris_{i}",
                    random.NextDouble() > 0.45d ? PrimitiveType.Cube : PrimitiveType.Capsule,
                    offset,
                    new Vector3(scale * RandomRange(random, 0.12f, 0.2f), scale * RandomRange(random, 0.05f, 0.11f), scale * RandomRange(random, 0.1f, 0.18f)),
                    new Vector3(RandomRange(random, -16f, 16f), angle, RandomRange(random, -16f, 16f)),
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
                GameObject coconut = CreateMeshPart("Coconut", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), rockMaterial, palm.transform);
                coconut.transform.localPosition = crownCenter + (Quaternion.Euler(0f, angle, 0f) * new Vector3(0.18f, -0.16f, 0.12f));
                coconut.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
                ApplyTint(coconut, new Color(0.39f, 0.26f, 0.14f));
            }

            IslandPalmInteraction interaction = GetOrAddComponent<IslandPalmInteraction>(palm);
            interaction.Configure(Mathf.Clamp(height * 0.74f, 3f, 5.5f), height);
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
            CreateBushStemCluster(parent, scale, random, 6, 0.26f, 0.18f);
            CreateBushCanopyRing(parent, scale, random, 6, scale * 0.18f, scale * 0.36f, scale * 0.2f, scale * 0.32f, 0f);
            CreateBushCanopyRing(parent, scale, random, 4, scale * 0.04f, scale * 0.18f, scale * 0.42f, scale * 0.5f, 28f);
            CreateBushLeafCluster(parent, new Vector3(0f, scale * 0.52f, 0f), new Vector3(scale * 0.42f, scale * 0.24f, scale * 0.38f), 0f, random);

            int broadLeafCount = random.Next(10, 15);
            for (int i = 0; i < broadLeafCount; i++)
            {
                float angle = (360f / broadLeafCount) * i + RandomRange(random, -12f, 12f);
                float radius = scale * RandomRange(random, 0.22f, 0.4f);
                float height = scale * RandomRange(random, 0.16f, 0.34f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushBroadLeaf(parent, offset, angle, scale * RandomRange(random, 0.18f, 0.24f), scale * RandomRange(random, 0.06f, 0.08f), random);
            }
        }

        private void CreateWindsweptBushVariant(Transform parent, float scale, System.Random random)
        {
            CreateBushStemCluster(parent, scale, random, 4, 0.32f, 0.24f);

            for (int i = 0; i < 3; i++)
            {
                float angle = -26f + (i * 16f) + RandomRange(random, -6f, 6f);
                CreateBushLeafCluster(parent, Quaternion.Euler(0f, angle, 0f) * new Vector3(scale * (0.12f + (i * 0.12f)), scale * (0.22f + (i * 0.08f)), 0f), new Vector3(scale * 0.42f, scale * 0.2f, scale * 0.28f), angle, random);
            }

            int sprigCount = 5;
            for (int i = 0; i < sprigCount; i++)
            {
                float angle = -38f + (i * 18f) + RandomRange(random, -6f, 6f);
                CreateBushSprig(parent, angle, scale, random, scale * 0.18f, scale * 0.34f, 3, 5);
            }

            CreateBushCanopyRing(parent, scale, random, 5, scale * 0.18f, scale * 0.34f, scale * 0.18f, scale * 0.28f, -34f);
        }

        private void CreateDenseBushVariant(Transform parent, float scale, System.Random random)
        {
            CreateBushStemCluster(parent, scale, random, 7, 0.24f, 0.16f);
            CreateBushCanopyRing(parent, scale, random, 8, scale * 0.08f, scale * 0.26f, scale * 0.14f, scale * 0.3f, 0f);
            CreateBushCanopyRing(parent, scale, random, 6, scale * 0.02f, scale * 0.14f, scale * 0.28f, scale * 0.42f, 22f);
            CreateBushLeafCluster(parent, new Vector3(0f, scale * 0.5f, 0f), new Vector3(scale * 0.38f, scale * 0.22f, scale * 0.34f), 0f, random);

            int leafCount = random.Next(12, 18);
            for (int i = 0; i < leafCount; i++)
            {
                float angle = (360f / leafCount) * i + RandomRange(random, -14f, 14f);
                float radius = scale * RandomRange(random, 0.16f, 0.3f);
                float height = scale * RandomRange(random, 0.12f, 0.28f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushLeaf(parent, offset, angle + RandomRange(random, -12f, 12f), scale * RandomRange(random, 0.14f, 0.2f), RandomRange(random, 0.35f, 1f));
            }
        }

        private void CreateWildBushVariant(Transform parent, float scale, System.Random random)
        {
            CreateBushStemCluster(parent, scale, random, 5, 0.3f, 0.2f);
            CreateBushCanopyRing(parent, scale, random, 5, scale * 0.16f, scale * 0.34f, scale * 0.18f, scale * 0.3f, 12f);

            for (int i = 0; i < 4; i++)
            {
                float angle = (90f * i) + RandomRange(random, -12f, 12f);
                CreateBushSprig(parent, angle, scale, random, scale * 0.16f, scale * 0.3f, 3, 5);
            }

            int broadLeafCount = random.Next(10, 14);
            for (int i = 0; i < broadLeafCount; i++)
            {
                float angle = (360f / broadLeafCount) * i + RandomRange(random, -18f, 18f);
                float radius = scale * RandomRange(random, 0.2f, 0.38f);
                float height = scale * RandomRange(random, 0.18f, 0.34f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushBroadLeaf(parent, offset, angle, scale * RandomRange(random, 0.18f, 0.24f), scale * RandomRange(random, 0.055f, 0.075f), random);
            }
        }

        private void CreateBushCanopyRing(
            Transform parent,
            float scale,
            System.Random random,
            int clusterCount,
            float minRadius,
            float maxRadius,
            float minHeight,
            float maxHeight,
            float angleOffset)
        {
            for (int i = 0; i < clusterCount; i++)
            {
                float angle = angleOffset + ((360f / clusterCount) * i) + RandomRange(random, -14f, 14f);
                float radius = RandomRange(random, minRadius, maxRadius);
                float height = RandomRange(random, minHeight, maxHeight);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, height, 0f);
                CreateBushLeafCluster(
                    parent,
                    offset,
                    new Vector3(
                        scale * RandomRange(random, 0.3f, 0.48f),
                        scale * RandomRange(random, 0.16f, 0.24f),
                        scale * RandomRange(random, 0.28f, 0.42f)),
                    angle,
                    random);
            }
        }

        private void CreateBushStemCluster(Transform parent, float scale, System.Random random, int stemCount, float stemHeightScale, float spreadScale)
        {
            for (int i = 0; i < stemCount; i++)
            {
                float angle = (360f / stemCount) * i + RandomRange(random, -18f, 18f);
                float radius = scale * RandomRange(random, 0.02f, spreadScale);
                Vector3 localPosition = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, scale * 0.06f, 0f);

                GameObject stem = CreateMeshPart("Stem", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), trunkMaterial, parent);
                stem.transform.localPosition = localPosition;
                stem.transform.localRotation = Quaternion.Euler(RandomRange(random, 16f, 28f), angle, RandomRange(random, -12f, 12f));
                stem.transform.localScale = new Vector3(scale * RandomRange(random, 0.04f, 0.06f), scale * RandomRange(random, stemHeightScale * 0.72f, stemHeightScale), scale * RandomRange(random, 0.04f, 0.06f));
                ApplyTint(stem, Color.Lerp(new Color(0.33f, 0.24f, 0.14f), new Color(0.44f, 0.34f, 0.18f), i / (float)Mathf.Max(1, stemCount - 1)));
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
            stem.transform.localScale = new Vector3(scale * 0.025f, scale * 0.14f, scale * 0.025f);
            ApplyTint(stem, new Color(0.33f, 0.24f, 0.14f));

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
                78f + RandomRange(random, -10f, 10f),
                yaw + RandomRange(random, -16f, 16f),
                RandomRange(random, -12f, 12f));
            broadLeaf.transform.localScale = new Vector3(width * 0.9f, length, width * 1.18f);
            ApplyTint(broadLeaf, Color.Lerp(new Color(0.21f, 0.46f, 0.17f), new Color(0.43f, 0.73f, 0.28f), (Mathf.Sin(yaw * Mathf.Deg2Rad) + 1f) * 0.5f));
        }

        private void CreateBushLeafCluster(Transform parent, Vector3 localPosition, Vector3 localScale, float yaw, System.Random random)
        {
            Mesh canopyMesh = random.NextDouble() > 0.3d
                ? cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere)
                : cachedCapsuleMesh ??= GetPrimitiveMesh(PrimitiveType.Capsule);

            GameObject cluster = CreateMeshPart("LeafCluster", canopyMesh, leavesMaterial, parent);
            cluster.transform.localPosition = localPosition;
            cluster.transform.localRotation = Quaternion.Euler(RandomRange(random, -12f, 12f), yaw, RandomRange(random, -16f, 16f));
            cluster.transform.localScale = localScale;
            ApplyTint(cluster, Color.Lerp(new Color(0.2f, 0.44f, 0.17f), new Color(0.38f, 0.7f, 0.26f), (Mathf.Cos(yaw * Mathf.Deg2Rad) + 1f) * 0.5f));

            if (random.NextDouble() > 0.35d)
            {
                GameObject highlight = CreateMeshPart("LeafHighlight", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), leavesMaterial, cluster.transform);
                highlight.transform.localPosition = new Vector3(localScale.x * 0.08f, localScale.y * 0.18f, localScale.z * 0.12f);
                highlight.transform.localRotation = Quaternion.Euler(RandomRange(random, -10f, 10f), yaw, RandomRange(random, -10f, 10f));
                highlight.transform.localScale = new Vector3(localScale.x * 0.52f, localScale.y * 0.46f, localScale.z * 0.48f);
                ApplyTint(highlight, new Color(0.49f, 0.8f, 0.31f));
            }
        }

        private void CreateBushLeaf(Transform parent, Vector3 localPosition, float yaw, float length, float tintLerp)
        {
            GameObject leaf = CreateMeshPart("Leaf", cachedCapsuleMesh ??= GetPrimitiveMesh(PrimitiveType.Capsule), leavesMaterial, parent);
            leaf.transform.localPosition = localPosition;
            leaf.transform.localRotation = Quaternion.Euler(86f, yaw, Mathf.Sign(yaw) * 8f);
            leaf.transform.localScale = new Vector3(length * 0.14f, length * 0.46f, length * 0.12f);
            ApplyTint(leaf, Color.Lerp(new Color(0.2f, 0.43f, 0.16f), new Color(0.4f, 0.7f, 0.28f), tintLerp));
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

        private void CreateIslandExplorer(Transform parent)
        {
            GameObject torso = CreateMeshPart("Torso", cachedCapsuleMesh ??= GetPrimitiveMesh(PrimitiveType.Capsule), characterShirtMaterial, parent);
            torso.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            torso.transform.localScale = new Vector3(0.68f, 0.62f, 0.56f);

            GameObject hips = CreateMeshPart("Hips", cachedCubeMesh ??= GetPrimitiveMesh(PrimitiveType.Cube), characterShortsMaterial, parent);
            hips.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            hips.transform.localScale = new Vector3(0.7f, 0.36f, 0.44f);

            GameObject leftLeg = CreateMeshPart("LeftLeg", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterSkinMaterial, parent);
            leftLeg.transform.localPosition = new Vector3(-0.18f, 0.36f, 0f);
            leftLeg.transform.localScale = new Vector3(0.12f, 0.36f, 0.12f);

            GameObject rightLeg = CreateMeshPart("RightLeg", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterSkinMaterial, parent);
            rightLeg.transform.localPosition = new Vector3(0.18f, 0.36f, 0f);
            rightLeg.transform.localScale = new Vector3(0.12f, 0.36f, 0.12f);

            GameObject leftArm = CreateMeshPart("LeftArm", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterSkinMaterial, parent);
            leftArm.transform.localPosition = new Vector3(-0.5f, 1.36f, 0f);
            leftArm.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            leftArm.transform.localScale = new Vector3(0.09f, 0.34f, 0.09f);

            GameObject rightArm = CreateMeshPart("RightArm", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterSkinMaterial, parent);
            rightArm.transform.localPosition = new Vector3(0.5f, 1.36f, 0f);
            rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            rightArm.transform.localScale = new Vector3(0.09f, 0.34f, 0.09f);

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

        private static Material CreateNatureAssetMaterial(string materialName, string textureResourcePath, bool alphaClip, bool doubleSided)
        {
            Material material = CreateRuntimeMaterial(materialName);
            Texture2D albedo = Resources.Load<Texture2D>(textureResourcePath);

            if (albedo != null)
            {
                material.SetTexture("_BaseMap", albedo);
                material.SetTexture("_MainTex", albedo);
                material.mainTexture = albedo;
            }

            material.color = Color.white;
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Smoothness", alphaClip ? 0f : 0.05f);

            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", 0.33f);
            }

            if (doubleSided)
            {
                material.SetFloat("_Cull", 0f);
            }

            return material;
        }

        private static void ApplyMaterialTint(Material material, Color tint)
        {
            if (material == null)
            {
                return;
            }

            material.color = tint;
            material.SetColor("_BaseColor", tint);
            material.SetColor("_Color", tint);
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

        private static void ApplyMaterialToHierarchy(Transform root, Material material)
        {
            if (root == null || material == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material[] materials = renderer.sharedMaterials;

                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                }
                else
                {
                    for (int j = 0; j < materials.Length; j++)
                    {
                        materials[j] = material;
                    }

                    renderer.sharedMaterials = materials;
                }

                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static float RandomWave(float value, float offset)
        {
            return Mathf.Sin(value + offset);
        }

        private static float GetDeterministicRockScale(int index)
        {
            float t = Mathf.Repeat((index * 0.7548777f) + 0.21f, 1f);
            return Mathf.Lerp(1.3f, 2.4f, t);
        }

        private static bool TryReserveDecorFootprint(
            Vector3 position,
            float footprintRadius,
            List<Vector2> reservedDecorPositions,
            List<float> reservedDecorRadii)
        {
            if (reservedDecorPositions == null || reservedDecorRadii == null)
            {
                return true;
            }

            Vector2 planarPosition = new Vector2(position.x, position.z);
            for (int i = 0; i < reservedDecorPositions.Count; i++)
            {
                float combinedRadius = footprintRadius + reservedDecorRadii[i];
                if ((planarPosition - reservedDecorPositions[i]).sqrMagnitude < combinedRadius * combinedRadius)
                {
                    return false;
                }
            }

            reservedDecorPositions.Add(planarPosition);
            reservedDecorRadii.Add(footprintRadius);
            return true;
        }

        private static void ConfigureRockObstacleCollider(GameObject rock)
        {
            if (rock == null || !IslandInteractionUtility.TryGetCompositeBounds(rock.transform, out Bounds bounds))
            {
                return;
            }

            BoxCollider collider = GetOrAddComponent<BoxCollider>(rock);
            collider.isTrigger = false;

            Vector3 localCenter = rock.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = rock.transform.InverseTransformVector(bounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            localSize.x = Mathf.Max(localSize.x, 0.8f);
            localSize.y = Mathf.Max(localSize.y, 0.8f);
            localSize.z = Mathf.Max(localSize.z, 0.8f);

            collider.center = localCenter;
            collider.size = localSize;
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
            ReleaseObject(bushAssetMaterial);
            ReleaseObject(rockAssetMaterial);

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
            bushAssetMaterial = null;
            rockAssetMaterial = null;
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
