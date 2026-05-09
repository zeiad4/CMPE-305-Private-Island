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
        private bool isRebuilding;
#if UNITY_EDITOR
        private bool editorRebuildQueued;
#endif

        private static Mesh cachedCubeMesh;
        private static Mesh cachedCylinderMesh;
        private static Mesh cachedSphereMesh;
        private static Mesh cachedCapsuleMesh;

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

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.72f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.43f, 0.54f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.17f, 0.18f, 0.15f);
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
            sun.color = new Color(1f, 0.95f, 0.86f);
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
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.95f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.35f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.52f;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = true;

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

            trunkMaterial.SetColor("_BaseColor", new Color(0.39f, 0.26f, 0.16f));
            trunkMaterial.SetFloat("_Smoothness", 0.18f);

            leavesMaterial.SetColor("_BaseColor", new Color(0.3f, 0.55f, 0.27f));
            leavesMaterial.SetFloat("_Smoothness", 0.12f);

            rockMaterial.SetColor("_BaseColor", new Color(0.43f, 0.42f, 0.4f));
            rockMaterial.SetFloat("_Smoothness", 0.16f);

            dockMaterial.SetColor("_BaseColor", new Color(0.52f, 0.38f, 0.24f));
            dockMaterial.SetFloat("_Smoothness", 0.2f);

            System.Random random = new System.Random(seed);
            Vector2 dockDirection = new Vector2(0.42f, 0.91f).normalized;
            Vector3 characterSpawn = GetCharacterSpawnPosition(dockDirection);

            BuildDock(propsRoot, dockDirection);

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
                CreateRock(propsRoot, position, scale, angle * Mathf.Rad2Deg);
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

        private void CreateRock(Transform parent, Vector3 position, float scale, float yaw)
        {
            GameObject rock = CreateMeshPart("Rock", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), rockMaterial, parent);
            rock.transform.localPosition = position + new Vector3(0f, scale * 0.16f, 0f);
            rock.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            rock.transform.localScale = new Vector3(scale * 1.3f, scale * 0.75f, scale);
        }

        private void CreatePalm(Transform parent, Vector3 position, float height, float tilt, float yaw)
        {
            GameObject palm = new GameObject("Palm");
            palm.transform.SetParent(parent, false);
            palm.transform.localPosition = position;
            palm.transform.localRotation = Quaternion.Euler(0f, yaw, tilt);

            GameObject trunk = CreateMeshPart("Trunk", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), trunkMaterial, palm.transform);
            trunk.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            trunk.transform.localScale = new Vector3(0.22f, height * 0.5f, 0.22f);

            Vector3 crownCenter = new Vector3(0f, height * 0.98f, 0f);

            for (int i = 0; i < 5; i++)
            {
                float angle = (360f / 5f) * i;
                GameObject frond = CreateMeshPart("Frond", cachedCapsuleMesh ??= GetPrimitiveMesh(PrimitiveType.Capsule), leavesMaterial, palm.transform);
                frond.transform.localPosition = crownCenter + (Quaternion.Euler(0f, angle, 0f) * new Vector3(0.8f, 0.2f, 0f));
                frond.transform.localRotation = Quaternion.Euler(18f, angle, 72f);
                frond.transform.localScale = new Vector3(0.14f, 1.05f, 0.14f);
            }
        }

        private void CreateBush(Transform parent, Vector3 position, float scale, float yaw, System.Random random)
        {
            GameObject bush = new GameObject("Bush");
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = position;
            bush.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject baseClump = CreateMeshPart("Base", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), leavesMaterial, bush.transform);
            baseClump.transform.localPosition = new Vector3(0f, scale * 0.22f, 0f);
            baseClump.transform.localScale = new Vector3(scale * 1.18f, scale * 0.42f, scale * 1.02f);

            GameObject coreClump = CreateMeshPart("Core", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), leavesMaterial, bush.transform);
            coreClump.transform.localPosition = new Vector3(scale * RandomRange(random, -0.08f, 0.08f), scale * 0.4f, scale * RandomRange(random, -0.08f, 0.08f));
            coreClump.transform.localScale = new Vector3(scale * 0.88f, scale * 0.54f, scale * 0.82f);

            int ringClumps = random.Next(5, 8);
            float startAngle = RandomRange(random, 0f, 360f);

            for (int i = 0; i < ringClumps; i++)
            {
                GameObject clump = CreateMeshPart("Clump", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), leavesMaterial, bush.transform);
                float angleStep = 360f / ringClumps;
                float angle = startAngle + (angleStep * i) + RandomRange(random, -16f, 16f);
                float radius = scale * RandomRange(random, 0.2f, 0.46f);
                float height = scale * RandomRange(random, 0.28f, 0.54f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, 0f, 0f);
                clump.transform.localPosition = new Vector3(offset.x, height, offset.z);

                float width = scale * RandomRange(random, 0.46f, 0.72f);
                float vertical = scale * RandomRange(random, 0.34f, 0.58f);
                float depth = scale * RandomRange(random, 0.42f, 0.7f);
                clump.transform.localScale = new Vector3(width, vertical, depth);
            }

            int topClumps = random.Next(2, 4);
            for (int i = 0; i < topClumps; i++)
            {
                GameObject clump = CreateMeshPart("Top", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), leavesMaterial, bush.transform);
                float offsetX = scale * RandomRange(random, -0.22f, 0.22f);
                float offsetZ = scale * RandomRange(random, -0.22f, 0.22f);
                float offsetY = scale * RandomRange(random, 0.52f, 0.78f);
                clump.transform.localPosition = new Vector3(offsetX, offsetY, offsetZ);

                float width = scale * RandomRange(random, 0.34f, 0.56f);
                float vertical = scale * RandomRange(random, 0.28f, 0.46f);
                float depth = scale * RandomRange(random, 0.34f, 0.56f);
                clump.transform.localScale = new Vector3(width, vertical, depth);
            }
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

            GameObject hair = CreateMeshPart("Hair", cachedSphereMesh ??= GetPrimitiveMesh(PrimitiveType.Sphere), characterHairMaterial, parent);
            hair.transform.localPosition = new Vector3(0f, 2.26f, -0.04f);
            hair.transform.localScale = new Vector3(0.54f, 0.3f, 0.54f);

            GameObject hatBrim = CreateMeshPart("HatBrim", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterStrawMaterial, parent);
            hatBrim.transform.localPosition = new Vector3(0f, 2.43f, 0f);
            hatBrim.transform.localScale = new Vector3(0.42f, 0.03f, 0.42f);

            GameObject hatCrown = CreateMeshPart("HatCrown", cachedCylinderMesh ??= GetPrimitiveMesh(PrimitiveType.Cylinder), characterStrawMaterial, parent);
            hatCrown.transform.localPosition = new Vector3(0f, 2.56f, 0f);
            hatCrown.transform.localScale = new Vector3(0.25f, 0.13f, 0.25f);

            GameObject backpack = CreateMeshPart("Backpack", cachedCubeMesh ??= GetPrimitiveMesh(PrimitiveType.Cube), dockMaterial, parent);
            backpack.transform.localPosition = new Vector3(0f, 1.32f, -0.27f);
            backpack.transform.localScale = new Vector3(0.34f, 0.5f, 0.16f);
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
