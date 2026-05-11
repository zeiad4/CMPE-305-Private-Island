using System.Collections.Generic;
using PrivateIsland;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PrivateIslandEditor
{
    public static class SeasonBoxSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MaterialsFolder = "Assets/Materials/SeasonBox";

        [MenuItem("Tools/SeasonBox/Build Sample Scene")]
        public static void BuildSampleScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            CreateMaterialsFolder();
            SeasonBoxMaterialSet materialSet = CreateMaterialSet();

            GameObject existingRoot = GameObject.Find("SeasonBox Root");
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot);
            }

            GameObject seasonBoxRoot = new GameObject("SeasonBox Root");
            seasonBoxRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            WorldEnvironmentManager environmentManager = seasonBoxRoot.AddComponent<WorldEnvironmentManager>();
            GameObject kiosk = CreateWorldControlBox(materialSet, seasonBoxRoot.transform);
            EnvironmentMenuUI menuUI = CreateCanvasAndMenu(seasonBoxRoot.transform, environmentManager);
            WorldControlBox controlBox = kiosk.GetComponent<WorldControlBox>();
            SerializedObject serializedControlBox = new SerializedObject(controlBox);
            SetObjectReference(serializedControlBox, "menuUI", menuUI);
            serializedControlBox.ApplyModifiedPropertiesWithoutUndo();

            EnsureEventSystemExists();

            SkyObjects skyObjects = CreateSkyObjects(seasonBoxRoot.transform, materialSet);
            DemoObjects demoObjects = CreateDemoObjects(seasonBoxRoot.transform, kiosk.transform.position, materialSet);
            AssignEnvironmentManager(environmentManager, seasonBoxRoot, demoObjects, materialSet, skyObjects);

            environmentManager.RefreshSceneReferences();
            environmentManager.ApplyWorldState();

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);

            Debug.Log("SeasonBox scene setup completed and saved to SampleScene.");
        }

        private static void CreateMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "SeasonBox");
            }
        }

        private static SeasonBoxMaterialSet CreateMaterialSet()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                litShader = Shader.Find("Standard");
            }

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null)
            {
                unlitShader = Shader.Find("Unlit/Color");
            }

            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null)
            {
                particleShader = Shader.Find("Particles/Standard Unlit");
            }

            var set = new SeasonBoxMaterialSet
            {
                springGround = CreateLitMaterial("Spring_Ground", litShader, new Color(0.52f, 0.78f, 0.35f), 0.12f),
                summerGround = CreateLitMaterial("Summer_Ground", litShader, new Color(0.34f, 0.6f, 0.22f), 0.12f),
                autumnGround = CreateLitMaterial("Autumn_Ground", litShader, new Color(0.68f, 0.43f, 0.2f), 0.16f),
                winterGround = CreateLitMaterial("Winter_Ground", litShader, new Color(0.9f, 0.95f, 0.98f), 0.2f),
                wetGround = CreateLitMaterial("Wet_Ground", litShader, new Color(0.28f, 0.39f, 0.23f), 0.5f),

                springLeaves = CreateLitMaterial("Spring_Leaves", litShader, new Color(0.47f, 0.8f, 0.35f), 0.12f),
                summerLeaves = CreateLitMaterial("Summer_Leaves", litShader, new Color(0.28f, 0.58f, 0.22f), 0.12f),
                autumnLeaves = CreateLitMaterial("Autumn_Leaves", litShader, new Color(0.76f, 0.42f, 0.15f), 0.12f),
                winterLeaves = CreateLitMaterial("Winter_Leaves", litShader, new Color(0.79f, 0.84f, 0.86f), 0.18f),

                springExtra = CreateLitMaterial("Spring_Extra", litShader, new Color(0.95f, 0.84f, 0.92f), 0.2f),
                summerExtra = CreateLitMaterial("Summer_Extra", litShader, new Color(0.97f, 0.89f, 0.54f), 0.2f),
                autumnExtra = CreateLitMaterial("Autumn_Extra", litShader, new Color(0.85f, 0.52f, 0.2f), 0.18f),
                winterExtra = CreateLitMaterial("Winter_Extra", litShader, new Color(0.88f, 0.92f, 0.98f), 0.24f),

                wood = CreateLitMaterial("Kiosk_Wood", litShader, new Color(0.47f, 0.31f, 0.18f), 0.2f),
                panel = CreateLitMaterial("Kiosk_Panel", litShader, new Color(0.22f, 0.24f, 0.29f), 0.35f),
                cloud = CreateUnlitMaterial("Cloud", unlitShader, new Color(0.44f, 0.47f, 0.52f, 0.96f)),
                snowOverlay = CreateLitMaterial("Snow_Overlay", litShader, new Color(0.95f, 0.97f, 1f), 0.35f),
                autumnPile = CreateLitMaterial("Autumn_Pile", litShader, new Color(0.72f, 0.38f, 0.13f), 0.15f),
                puddle = CreateLitMaterial("Puddle", litShader, new Color(0.21f, 0.35f, 0.44f), 0.85f),
                flowerStem = CreateLitMaterial("Flower_Stem", litShader, new Color(0.26f, 0.62f, 0.22f), 0.08f),
                flowerPetal = CreateLitMaterial("Flower_Petal", litShader, new Color(0.93f, 0.4f, 0.68f), 0.18f),
                flowerPetalBlue = CreateLitMaterial("Flower_Petal_Blue", litShader, new Color(0.44f, 0.72f, 0.96f), 0.18f),
                flowerPetalWhite = CreateLitMaterial("Flower_Petal_White", litShader, new Color(0.98f, 0.98f, 0.98f), 0.18f),
                flowerPetalYellow = CreateLitMaterial("Flower_Petal_Yellow", litShader, new Color(0.98f, 0.88f, 0.34f), 0.18f),
                flowerPetalLavender = CreateLitMaterial("Flower_Petal_Lavender", litShader, new Color(0.72f, 0.58f, 0.94f), 0.18f),
                flowerPetalCoral = CreateLitMaterial("Flower_Petal_Coral", litShader, new Color(0.96f, 0.46f, 0.34f), 0.18f),
                flowerCenter = CreateLitMaterial("Flower_Center", litShader, new Color(0.93f, 0.8f, 0.2f), 0.18f),
                sun = CreateUnlitMaterial("Sun_Orb", unlitShader, new Color(1f, 0.88f, 0.22f)),
                moon = CreateUnlitMaterial("Moon_Orb", unlitShader, Color.white),
                rainParticle = CreateParticleMaterial("Rain_Particle", particleShader, new Color(0.78f, 0.88f, 1f, 0.8f)),
                snowParticle = CreateParticleMaterial("Snow_Particle", particleShader, new Color(1f, 1f, 1f, 0.98f)),
                autumnParticle = CreateParticleMaterial("Autumn_Particle", particleShader, new Color(0.86f, 0.5f, 0.22f, 0.95f))
            };

            return set;
        }

        private static Material CreateLitMaterial(string materialName, Shader shader, Color color, float smoothness)
        {
            string assetPath = $"{MaterialsFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.name = materialName;
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateUnlitMaterial(string materialName, Shader shader, Color color)
        {
            string assetPath = $"{MaterialsFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.name = materialName;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateParticleMaterial(string materialName, Shader shader, Color color)
        {
            string assetPath = $"{MaterialsFolder}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.name = materialName;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetEmission(Material material, Color emissionColor)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }
        }

        private static GameObject CreateWorldControlBox(SeasonBoxMaterialSet materialSet, Transform parent)
        {
            Vector3 basePosition = ResolveKioskWorldPosition();

            GameObject kioskRoot = new GameObject("World Control Box");
            kioskRoot.transform.SetParent(parent, false);
            kioskRoot.transform.position = basePosition;

            WorldControlBox controlBox = kioskRoot.AddComponent<WorldControlBox>();
            SerializedObject serializedControlBox = new SerializedObject(controlBox);
            serializedControlBox.FindProperty("interactionDistance").floatValue = 3.75f;
            serializedControlBox.ApplyModifiedPropertiesWithoutUndo();

            CreateCylinderPart("TableBase", kioskRoot.transform, new Vector3(0f, 0.1f, 0f), new Vector3(1.55f, 0.08f, 1.55f), materialSet.wood);
            CreateCubePart("TableTop", kioskRoot.transform, new Vector3(0f, 0.96f, 0f), new Vector3(2.35f, 0.16f, 1.45f), materialSet.wood);
            CreateCubePart("AccentTrim", kioskRoot.transform, new Vector3(0f, 1.06f, 0f), new Vector3(2.15f, 0.05f, 1.25f), materialSet.springExtra);

            CreateCubePart("LegA", kioskRoot.transform, new Vector3(-0.95f, 0.46f, -0.58f), new Vector3(0.14f, 0.92f, 0.14f), materialSet.wood);
            CreateCubePart("LegB", kioskRoot.transform, new Vector3(0.95f, 0.46f, -0.58f), new Vector3(0.14f, 0.92f, 0.14f), materialSet.wood);
            CreateCubePart("LegC", kioskRoot.transform, new Vector3(-0.95f, 0.46f, 0.58f), new Vector3(0.14f, 0.92f, 0.14f), materialSet.wood);
            CreateCubePart("LegD", kioskRoot.transform, new Vector3(0.95f, 0.46f, 0.58f), new Vector3(0.14f, 0.92f, 0.14f), materialSet.wood);

            CreateCubePart("HeaderBoard", kioskRoot.transform, new Vector3(0f, 1.9f, -0.44f), new Vector3(1.45f, 0.32f, 0.1f), materialSet.springExtra);
            CreateCubePart("HeaderPoleLeft", kioskRoot.transform, new Vector3(-0.74f, 1.48f, -0.36f), new Vector3(0.08f, 0.78f, 0.08f), materialSet.panel);
            CreateCubePart("HeaderPoleRight", kioskRoot.transform, new Vector3(0.74f, 1.48f, -0.36f), new Vector3(0.08f, 0.78f, 0.08f), materialSet.panel);

            CreateCubePart("ControlFrame", kioskRoot.transform, new Vector3(0f, 1.26f, -0.05f), new Vector3(1.4f, 0.64f, 0.5f), materialSet.springExtra);
            CreateCubePart("ControlScreen", kioskRoot.transform, new Vector3(0f, 1.27f, -0.08f), new Vector3(1.22f, 0.48f, 0.38f), materialSet.panel);
            CreateCubePart("FrontBoard", kioskRoot.transform, new Vector3(0f, 1.14f, -0.54f), new Vector3(1.6f, 0.36f, 0.08f), materialSet.springExtra);

            CreateSeasonTile("SpringTile", kioskRoot.transform, new Vector3(-0.45f, 1.27f, -0.29f), materialSet.springGround);
            CreateSeasonTile("SummerTile", kioskRoot.transform, new Vector3(-0.15f, 1.27f, -0.29f), materialSet.summerGround);
            CreateSeasonTile("AutumnTile", kioskRoot.transform, new Vector3(0.15f, 1.27f, -0.29f), materialSet.autumnGround);
            CreateSeasonTile("WinterTile", kioskRoot.transform, new Vector3(0.45f, 1.27f, -0.29f), materialSet.winterGround);

            CreateMiniWeatherTile("ClearTile", kioskRoot.transform, new Vector3(-0.28f, 1.03f, -0.5f), new Color(0.95f, 0.97f, 1f), materialSet);
            CreateMiniWeatherTile("RainTile", kioskRoot.transform, new Vector3(0f, 1.03f, -0.5f), new Color(0.38f, 0.64f, 0.94f), materialSet);
            CreateMiniWeatherTile("ThunderTile", kioskRoot.transform, new Vector3(0.28f, 1.03f, -0.5f), new Color(0.72f, 0.84f, 1f), materialSet);

            return kioskRoot;
        }

        private static Vector3 ResolveKioskWorldPosition()
        {
            Vector2 dockDirection = new Vector2(0.42f, 0.91f).normalized;
            Vector2 perpendicular = new Vector2(-dockDirection.y, dockDirection.x);
            Vector2 planar = (dockDirection * 52f) + (perpendicular * -8f) + (dockDirection * -2f);
            float y = SampleIslandHeight(planar.x, planar.y, 220f, 9f) + 0.05f;
            return new Vector3(planar.x, y, planar.y);
        }

        private static float SampleIslandHeight(float x, float z, float islandSize, float peakHeight)
        {
            float halfSize = islandSize * 0.5f;
            float radial = Mathf.Sqrt((x * x) + (z * z)) / halfSize;
            radial = Mathf.Clamp01(radial);

            float corePlateau = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.58f, 0.84f, radial));
            float shorelineShelf = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.78f, 1f, radial));
            float interiorBlend = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.2f, 0.86f, radial));

            float broadNoise = Mathf.PerlinNoise((x * 0.018f) + 17.4f, (z * 0.018f) + 9.2f) - 0.5f;
            float detailNoise = Mathf.PerlinNoise((x * 0.045f) + 51.8f, (z * 0.045f) + 29.6f) - 0.5f;
            float coastNoise = Mathf.PerlinNoise((x * 0.038f) + 103.7f, (z * 0.038f) + 65.3f) - 0.5f;

            float plateauHeight = Mathf.Lerp(0.54f, 0.74f, corePlateau);
            float noise = ((broadNoise * 0.22f) + (detailNoise * 0.08f)) * interiorBlend;
            float coastVariation = coastNoise * shorelineShelf * 0.05f;

            float shape = plateauHeight + (shorelineShelf * 0.05f) + noise + coastVariation;
            shape *= Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.9f, 1f, radial));
            shape = Mathf.Clamp01(shape);

            return shape * peakHeight;
        }

        private static EnvironmentMenuUI CreateCanvasAndMenu(Transform parent, WorldEnvironmentManager environmentManager)
        {
            GameObject canvasObject = new GameObject("SeasonBox Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnvironmentMenuUI menuUI = canvasObject.AddComponent<EnvironmentMenuUI>();

            GameObject promptBackdrop = CreateImagePanel("InteractionPromptBackdrop", canvasObject.transform, new Color(0.05f, 0.08f, 0.14f, 0.84f));
            RectTransform promptBackdropRect = promptBackdrop.GetComponent<RectTransform>();
            promptBackdropRect.anchorMin = new Vector2(0.5f, 0f);
            promptBackdropRect.anchorMax = new Vector2(0.5f, 0f);
            promptBackdropRect.pivot = new Vector2(0.5f, 0f);
            promptBackdropRect.anchoredPosition = new Vector2(0f, 28f);
            promptBackdropRect.sizeDelta = new Vector2(840f, 72f);

            GameObject promptObject = CreateText("InteractionPromptText", canvasObject.transform, "Press E to open SeasonBox Controls", 28, TextAnchor.MiddleCenter);
            RectTransform promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0f, 38f);
            promptRect.sizeDelta = new Vector2(790f, 46f);
            Text promptText = promptObject.GetComponent<Text>();
            promptText.color = Color.white;

            GameObject panelObject = new GameObject("SeasonBoxMenuPanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(860f, 720f);
            panelRect.anchoredPosition = Vector2.zero;
            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);

            GameObject titleBanner = CreateImagePanel("TitleBanner", panelObject.transform, new Color(0.42f, 0.74f, 0.46f, 1f));
            RectTransform titleBannerRect = titleBanner.GetComponent<RectTransform>();
            titleBannerRect.anchorMin = new Vector2(0.5f, 1f);
            titleBannerRect.anchorMax = new Vector2(0.5f, 1f);
            titleBannerRect.pivot = new Vector2(0.5f, 1f);
            titleBannerRect.anchoredPosition = new Vector2(0f, -26f);
            titleBannerRect.sizeDelta = new Vector2(710f, 72f);

            GameObject titleObject = CreateText("TitleText", panelObject.transform, "SeasonBox Controls", 40, TextAnchor.MiddleCenter);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -36f);
            titleRect.sizeDelta = new Vector2(670f, 48f);
            titleObject.GetComponent<Text>().color = Color.white;

            GameObject stateBackdrop = CreateImagePanel("CurrentStateBackdrop", panelObject.transform, new Color(0.94f, 0.96f, 1f, 0.92f));
            RectTransform stateBackdropRect = stateBackdrop.GetComponent<RectTransform>();
            stateBackdropRect.anchorMin = new Vector2(0.5f, 1f);
            stateBackdropRect.anchorMax = new Vector2(0.5f, 1f);
            stateBackdropRect.pivot = new Vector2(0.5f, 1f);
            stateBackdropRect.anchoredPosition = new Vector2(0f, -118f);
            stateBackdropRect.sizeDelta = new Vector2(690f, 54f);

            GameObject stateObject = CreateText("CurrentStateText", panelObject.transform, "Current: Day / Spring / Clear", 26, TextAnchor.MiddleCenter);
            RectTransform stateRect = stateObject.GetComponent<RectTransform>();
            stateRect.anchorMin = new Vector2(0.5f, 1f);
            stateRect.anchorMax = new Vector2(0.5f, 1f);
            stateRect.pivot = new Vector2(0.5f, 1f);
            stateRect.anchoredPosition = new Vector2(0f, -123f);
            stateRect.sizeDelta = new Vector2(650f, 40f);
            stateObject.GetComponent<Text>().color = new Color(0.08f, 0.1f, 0.14f);

            GameObject warningBackdrop = CreateImagePanel("WarningBackdrop", panelObject.transform, new Color(0.82f, 0.42f, 0.18f, 0.9f));
            RectTransform warningBackdropRect = warningBackdrop.GetComponent<RectTransform>();
            warningBackdropRect.anchorMin = new Vector2(0.5f, 1f);
            warningBackdropRect.anchorMax = new Vector2(0.5f, 1f);
            warningBackdropRect.pivot = new Vector2(0.5f, 1f);
            warningBackdropRect.anchoredPosition = new Vector2(0f, -174f);
            warningBackdropRect.sizeDelta = new Vector2(690f, 40f);

            GameObject warningObject = CreateText("WarningText", panelObject.transform, string.Empty, 20, TextAnchor.MiddleCenter);
            RectTransform warningRect = warningObject.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(0.5f, 1f);
            warningRect.anchorMax = new Vector2(0.5f, 1f);
            warningRect.pivot = new Vector2(0.5f, 1f);
            warningRect.anchoredPosition = new Vector2(0f, -179f);
            warningRect.sizeDelta = new Vector2(620f, 28f);
            warningObject.GetComponent<Text>().color = Color.white;

            GameObject timeCard = CreateImagePanel("TimeSectionCard", panelObject.transform, new Color(1f, 1f, 1f, 0.1f));
            SetCardRect(timeCard.GetComponent<RectTransform>(), new Vector2(0f, -302f), new Vector2(720f, 114f));
            GameObject seasonCard = CreateImagePanel("SeasonSectionCard", panelObject.transform, new Color(1f, 1f, 1f, 0.1f));
            SetCardRect(seasonCard.GetComponent<RectTransform>(), new Vector2(0f, -435f), new Vector2(720f, 126f));
            GameObject weatherCard = CreateImagePanel("WeatherSectionCard", panelObject.transform, new Color(1f, 1f, 1f, 0.1f));
            SetCardRect(weatherCard.GetComponent<RectTransform>(), new Vector2(0f, -577f), new Vector2(720f, 124f));

            CreateSectionLabel(panelObject.transform, "TimeLabel", "Time", new Vector2(0f, -233f));
            CreateSectionLabel(panelObject.transform, "SeasonLabel", "Season", new Vector2(0f, -367f));
            CreateSectionLabel(panelObject.transform, "WeatherLabel", "Weather", new Vector2(0f, -509f));

            Button dayButton = CreateButton(panelObject.transform, "DayButton", "Day", new Vector2(-135f, -286f), new Vector2(230f, 58f));
            Button nightButton = CreateButton(panelObject.transform, "NightButton", "Night", new Vector2(135f, -286f), new Vector2(230f, 58f));

            Button springButton = CreateButton(panelObject.transform, "SpringButton", "Spring", new Vector2(-258f, -418f), new Vector2(152f, 54f));
            Button summerButton = CreateButton(panelObject.transform, "SummerButton", "Summer", new Vector2(-86f, -418f), new Vector2(152f, 54f));
            Button autumnButton = CreateButton(panelObject.transform, "AutumnButton", "Autumn", new Vector2(86f, -418f), new Vector2(152f, 54f));
            Button winterButton = CreateButton(panelObject.transform, "WinterButton", "Winter", new Vector2(258f, -418f), new Vector2(152f, 54f));

            Button clearButton = CreateButton(panelObject.transform, "ClearButton", "Clear", new Vector2(-210f, -560f), new Vector2(178f, 54f));
            Button rainButton = CreateButton(panelObject.transform, "RainButton", "Rain", new Vector2(0f, -560f), new Vector2(178f, 54f));
            Button thunderstormButton = CreateButton(panelObject.transform, "ThunderstormButton", "Thunderstorm", new Vector2(210f, -560f), new Vector2(220f, 54f));
            Button closeButton = CreateButton(panelObject.transform, "CloseButton", "Close", new Vector2(0f, -649f), new Vector2(250f, 48f));

            SerializedObject serializedMenu = new SerializedObject(menuUI);
            SetObjectReference(serializedMenu, "worldEnvironmentManager", environmentManager);
            SetObjectReference(serializedMenu, "seasonBoxMenuPanel", panelObject);
            SetObjectReference(serializedMenu, "currentStateText", stateObject.GetComponent<Text>());
            SetObjectReference(serializedMenu, "warningText", warningObject.GetComponent<Text>());
            SetObjectReference(serializedMenu, "interactionPromptText", promptText);
            SetObjectReference(serializedMenu, "menuPanelImage", panelImage);
            SetObjectReference(serializedMenu, "titleBannerImage", titleBanner.GetComponent<Image>());
            SetObjectReference(serializedMenu, "currentStateBackgroundImage", stateBackdrop.GetComponent<Image>());
            SetObjectReference(serializedMenu, "warningBackgroundImage", warningBackdrop.GetComponent<Image>());
            SetObjectReference(serializedMenu, "interactionPromptBackgroundImage", promptBackdrop.GetComponent<Image>());
            SetObjectReference(serializedMenu, "timeSectionBackgroundImage", timeCard.GetComponent<Image>());
            SetObjectReference(serializedMenu, "seasonSectionBackgroundImage", seasonCard.GetComponent<Image>());
            SetObjectReference(serializedMenu, "weatherSectionBackgroundImage", weatherCard.GetComponent<Image>());
            SetObjectReference(serializedMenu, "dayButton", dayButton);
            SetObjectReference(serializedMenu, "nightButton", nightButton);
            SetObjectReference(serializedMenu, "springButton", springButton);
            SetObjectReference(serializedMenu, "summerButton", summerButton);
            SetObjectReference(serializedMenu, "autumnButton", autumnButton);
            SetObjectReference(serializedMenu, "winterButton", winterButton);
            SetObjectReference(serializedMenu, "clearButton", clearButton);
            SetObjectReference(serializedMenu, "rainButton", rainButton);
            SetObjectReference(serializedMenu, "thunderstormButton", thunderstormButton);
            SetObjectReference(serializedMenu, "closeButton", closeButton);
            serializedMenu.FindProperty("manageCursorWhenMenuOpen").boolValue = true;
            serializedMenu.FindProperty("freezePlayerWhileMenuOpen").boolValue = true;
            serializedMenu.ApplyModifiedPropertiesWithoutUndo();

            panelObject.SetActive(false);
            promptObject.SetActive(false);
            promptBackdrop.SetActive(false);
            warningBackdrop.SetActive(false);
            return menuUI;
        }

        private static void CreateSectionLabel(Transform parent, string objectName, string textValue, Vector2 anchoredPosition)
        {
            GameObject labelObject = CreateText(objectName, parent, textValue, 24, TextAnchor.MiddleCenter);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(300f, 28f);
            labelObject.GetComponent<Text>().color = new Color(0.9f, 0.93f, 0.98f);
            labelObject.GetComponent<Text>().fontStyle = FontStyle.Bold;
        }

        private static GameObject CreateImagePanel(string objectName, Transform parent, Color color)
        {
            GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            panelObject.GetComponent<Image>().color = color;
            return panelObject;
        }

        private static void SetCardRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static Button CreateButton(Transform parent, string objectName, string labelText, Vector2 anchoredPosition, Vector2? size = null)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size ?? new Vector2(150f, 42f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.92f, 0.95f, 0.99f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.95f, 0.97f, 1f);
            colors.pressedColor = new Color(0.74f, 0.8f, 0.88f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.55f, 0.58f, 0.62f, 0.65f);
            button.colors = colors;

            GameObject labelObject = CreateText("Label", buttonObject.transform, labelText, 23, TextAnchor.MiddleCenter);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelObject.GetComponent<Text>().color = Color.black;
            labelObject.GetComponent<Text>().fontStyle = FontStyle.Bold;

            return button;
        }

        private static GameObject CreateText(string objectName, Transform parent, string textValue, int fontSize, TextAnchor anchor)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.text = textValue;
            text.font = ResolveBuiltinFont();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            return textObject;
        }

        private static Font ResolveBuiltinFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void EnsureEventSystemExists()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.transform.SetParent(null, false);
        }

        private static SkyObjects CreateSkyObjects(Transform parent, SeasonBoxMaterialSet materialSet)
        {
            GameObject skyRoot = new GameObject("SeasonBox Sky");
            skyRoot.transform.SetParent(parent, false);

            GameObject daySun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            daySun.name = "Day Sun";
            daySun.transform.SetParent(skyRoot.transform, false);
            daySun.transform.position = new Vector3(48f, 42f, -62f);
            daySun.transform.localScale = new Vector3(7.5f, 7.5f, 7.5f);
            ApplyMaterial(daySun, materialSet.sun);

            GameObject nightMoon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nightMoon.name = "Night Moon";
            nightMoon.transform.SetParent(skyRoot.transform, false);
            nightMoon.transform.position = new Vector3(-44f, 40f, -58f);
            nightMoon.transform.localScale = new Vector3(5.8f, 5.8f, 5.8f);
            ApplyMaterial(nightMoon, materialSet.moon);

            return new SkyObjects
            {
                daySun = daySun,
                nightMoon = nightMoon
            };
        }

        private static DemoObjects CreateDemoObjects(Transform parent, Vector3 kioskPosition, SeasonBoxMaterialSet materialSet)
        {
            GameObject demoRoot = new GameObject("SeasonBox Demo Objects");
            demoRoot.transform.SetParent(parent, false);

            var demoObjects = new DemoObjects
            {
                flowerObjects = new List<GameObject>(),
                snowOverlayObjects = new List<GameObject>(),
                autumnObjects = new List<GameObject>(),
                rainOnlyObjects = new List<GameObject>(),
                cloudObjects = new List<GameObject>(),
                nightLightObjects = new List<GameObject>()
            };

            GameObject flowersRoot = new GameObject("Flowers");
            flowersRoot.transform.SetParent(demoRoot.transform, false);
            CreateSpringFlowerField(flowersRoot.transform, materialSet, demoObjects.flowerObjects);

            GameObject autumnRoot = new GameObject("AutumnDecor");
            autumnRoot.transform.SetParent(demoRoot.transform, false);
            CreateAutumnGroundLeavesForTrees(autumnRoot.transform, materialSet.autumnPile, demoObjects.autumnObjects);

            GameObject puddleRoot = new GameObject("RainOnly");
            puddleRoot.transform.SetParent(demoRoot.transform, false);
            CreatePuddle(puddleRoot.transform, kioskPosition + new Vector3(1.6f, 0.01f, 0.7f), materialSet.puddle, demoObjects.rainOnlyObjects);
            CreatePuddle(puddleRoot.transform, kioskPosition + new Vector3(-1.4f, 0.01f, -0.8f), materialSet.puddle, demoObjects.rainOnlyObjects);

            GameObject cloudRoot = new GameObject("Clouds");
            cloudRoot.transform.SetParent(demoRoot.transform, false);
            CreateStormCloudField(cloudRoot.transform, materialSet.cloud, demoObjects.cloudObjects);

            GameObject snowRoot = new GameObject("SnowOverlays");
            snowRoot.transform.SetParent(demoRoot.transform, false);
            CreateSnowOverlays(snowRoot.transform, materialSet.snowOverlay, demoObjects.snowOverlayObjects);

            GameObject nightLightRoot = new GameObject("NightLights");
            nightLightRoot.transform.SetParent(demoRoot.transform, false);
            CreateNightLight(nightLightRoot.transform, kioskPosition + new Vector3(-0.7f, 1.4f, 0f), demoObjects.nightLightObjects);
            CreateNightLight(nightLightRoot.transform, kioskPosition + new Vector3(0.7f, 1.4f, 0f), demoObjects.nightLightObjects);

            demoObjects.rainEffect = CreateRainEffect(parent, "Rain Effect", materialSet.rainParticle);
            demoObjects.winterSnowEffect = CreateWinterSnowEffect(parent, "Winter Snow Effect", materialSet.snowParticle);
            demoObjects.thunderstormEffect = CreateThunderstormEffect(parent, "Thunderstorm Effect");
            demoObjects.autumnLeavesEffect = CreateAutumnLeavesEffect(parent, kioskPosition + new Vector3(0f, 2f, 0f), materialSet.autumnParticle);

            return demoObjects;
        }

        private static void CreateSpringFlowerField(Transform parent, SeasonBoxMaterialSet materialSet, List<GameObject> flowerObjects)
        {
            Vector3 kioskPosition = ResolveKioskWorldPosition();
            int seed = 0;

            for (float x = -98f; x <= 98f; x += 8.75f)
            {
                for (float z = -98f; z <= 98f; z += 8.75f)
                {
                    float radial = Mathf.Sqrt((x * x) + (z * z));
                    if (radial > 100f)
                    {
                        continue;
                    }

                    float density = Mathf.PerlinNoise((x * 0.061f) + 14.2f, (z * 0.061f) + 27.8f);
                    if (density < 0.18f)
                    {
                        continue;
                    }

                    float jitterX = Mathf.Lerp(-3.8f, 3.8f, Mathf.PerlinNoise((x * 0.12f) + 51.4f, (z * 0.12f) + 77.3f));
                    float jitterZ = Mathf.Lerp(-3.8f, 3.8f, Mathf.PerlinNoise((x * 0.12f) + 87.1f, (z * 0.12f) + 12.9f));
                    float worldX = x + jitterX;
                    float worldZ = z + jitterZ;

                    if (Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(kioskPosition.x, kioskPosition.z)) < 5.5f)
                    {
                        continue;
                    }

                    float height = SampleIslandHeight(worldX, worldZ, 220f, 9f);
                    if (height < 0.55f)
                    {
                        continue;
                    }

                    Vector3 flowerPosition = new Vector3(worldX, height + 0.02f, worldZ);
                    CreateFlowerCluster(parent, flowerPosition, materialSet, flowerObjects, seed);
                    seed++;
                }
            }

            Vector2[] meadowCenters =
            {
                new Vector2(-34f, 18f),
                new Vector2(26f, -22f),
                new Vector2(42f, 34f),
                new Vector2(-48f, -28f),
                new Vector2(4f, 48f),
                new Vector2(-18f, -46f),
                new Vector2(58f, 8f),
                new Vector2(-62f, 36f)
            };

            for (int i = 0; i < meadowCenters.Length; i++)
            {
                CreateFlowerMeadow(parent, meadowCenters[i], materialSet, flowerObjects, ref seed);
            }
        }

        private static void CreateFlowerCluster(Transform parent, Vector3 position, SeasonBoxMaterialSet materialSet, List<GameObject> flowerObjects, int seed)
        {
            GameObject cluster = new GameObject($"FlowerCluster_{seed}");
            cluster.transform.SetParent(parent, false);
            cluster.transform.position = position;
            Material[] petalMaterials =
            {
                materialSet.flowerPetal,
                materialSet.flowerPetalBlue,
                materialSet.flowerPetalWhite,
                materialSet.flowerPetalYellow,
                materialSet.flowerPetalLavender,
                materialSet.flowerPetalCoral
            };

            int flowerCount = 10 + (seed % 7);
            for (int i = 0; i < flowerCount; i++)
            {
                float angle = (i / (float)flowerCount) * Mathf.PI * 2f;
                float radius = 0.2f + ((i % 4) * 0.12f) + Mathf.PerlinNoise((seed * 0.37f) + i, (seed * 0.11f) + 3.9f) * 0.2f;
                Vector3 localOffset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Material petalMaterial = petalMaterials[(seed + (i * 3)) % petalMaterials.Length];
                int flowerType = (seed + i) % 6;

                switch (flowerType)
                {
                    case 0:
                        CreateDaisyFlower(cluster.transform, localOffset, materialSet.flowerStem, petalMaterial, materialSet.flowerCenter, 0.36f + ((i % 2) * 0.04f));
                        break;
                    case 1:
                        CreateTulipFlower(cluster.transform, localOffset, materialSet.flowerStem, petalMaterial, 0.42f + ((i % 3) * 0.05f));
                        break;
                    case 2:
                        CreateWildflowerSpike(cluster.transform, localOffset, materialSet.flowerStem, petalMaterial, materialSet.flowerCenter, 0.48f + ((i % 2) * 0.06f));
                        break;
                    case 3:
                        CreateCupFlower(cluster.transform, localOffset, materialSet.flowerStem, petalMaterial, materialSet.flowerCenter, 0.31f + ((i % 3) * 0.04f));
                        break;
                    case 4:
                        CreateStarFlower(cluster.transform, localOffset, materialSet.flowerStem, petalMaterial, materialSet.flowerCenter, 0.34f + ((i % 2) * 0.05f));
                        break;
                    default:
                        CreateSunflowerBloom(cluster.transform, localOffset, materialSet.flowerStem, petalMaterial, materialSet.flowerCenter, 0.52f + ((i % 3) * 0.05f));
                        break;
                }
            }

            flowerObjects.Add(cluster);
        }

        private static void CreateFlowerMeadow(Transform parent, Vector2 center, SeasonBoxMaterialSet materialSet, List<GameObject> flowerObjects, ref int seed)
        {
            for (int ring = 0; ring < 3; ring++)
            {
                int patchCount = 7 + (ring * 4);
                float ringRadius = 2.8f + (ring * 2.2f);

                for (int i = 0; i < patchCount; i++)
                {
                    float angle = (i / (float)patchCount) * Mathf.PI * 2f;
                    float noiseRadius = ringRadius + Mathf.PerlinNoise((seed * 0.19f) + i, (seed * 0.07f) + ring) * 1.8f;
                    float x = center.x + (Mathf.Cos(angle) * noiseRadius);
                    float z = center.y + (Mathf.Sin(angle) * noiseRadius);

                    float radial = Mathf.Sqrt((x * x) + (z * z));
                    if (radial > 100f)
                    {
                        continue;
                    }

                    float height = SampleIslandHeight(x, z, 220f, 9f);
                    if (height < 0.58f)
                    {
                        continue;
                    }

                    Vector3 position = new Vector3(x, height + 0.02f, z);
                    CreateFlowerCluster(parent, position, materialSet, flowerObjects, seed);
                    seed++;
                }
            }
        }

        private static void CreateDaisyFlower(Transform parent, Vector3 localOffset, Material stemMaterial, Material petalMaterial, Material centerMaterial, float stemHeight)
        {
            GameObject stem = CreateCylinderPart("DaisyStem", parent, localOffset + new Vector3(0f, stemHeight * 0.5f, 0f), new Vector3(0.018f, stemHeight * 0.5f, 0.018f), stemMaterial);
            stem.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);

            Vector3 bloomCenter = localOffset + new Vector3(0f, stemHeight, 0f);
            for (int p = 0; p < 6; p++)
            {
                float angle = p * 60f;
                Vector3 petalOffset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.08f, 0f, 0f);
                GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                petal.name = $"DaisyPetal_{p}";
                petal.transform.SetParent(parent, false);
                petal.transform.localPosition = bloomCenter + petalOffset;
                petal.transform.localScale = new Vector3(0.09f, 0.03f, 0.045f);
                petal.transform.localRotation = Quaternion.Euler(10f, angle, 28f);
                ApplyMaterial(petal, petalMaterial);
            }

            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            center.name = "DaisyCenter";
            center.transform.SetParent(parent, false);
            center.transform.localPosition = bloomCenter + new Vector3(0f, 0.005f, 0f);
            center.transform.localScale = new Vector3(0.06f, 0.045f, 0.06f);
            ApplyMaterial(center, centerMaterial);
        }

        private static void CreateTulipFlower(Transform parent, Vector3 localOffset, Material stemMaterial, Material petalMaterial, float stemHeight)
        {
            GameObject stem = CreateCylinderPart("TulipStem", parent, localOffset + new Vector3(0f, stemHeight * 0.5f, 0f), new Vector3(0.016f, stemHeight * 0.5f, 0.016f), stemMaterial);
            stem.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);

            Vector3 bloomBase = localOffset + new Vector3(0f, stemHeight, 0f);
            for (int p = 0; p < 3; p++)
            {
                float angle = 20f + (p * 120f);
                GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                petal.name = $"TulipPetal_{p}";
                petal.transform.SetParent(parent, false);
                petal.transform.localPosition = bloomBase + (Quaternion.Euler(0f, angle, 0f) * new Vector3(0.028f, 0.02f, 0f));
                petal.transform.localScale = new Vector3(0.08f, 0.14f, 0.08f);
                petal.transform.localRotation = Quaternion.Euler(-18f, angle, 0f);
                ApplyMaterial(petal, petalMaterial);
            }
        }

        private static void CreateWildflowerSpike(Transform parent, Vector3 localOffset, Material stemMaterial, Material petalMaterial, Material centerMaterial, float stemHeight)
        {
            GameObject stem = CreateCylinderPart("SpikeStem", parent, localOffset + new Vector3(0f, stemHeight * 0.5f, 0f), new Vector3(0.014f, stemHeight * 0.5f, 0.014f), stemMaterial);
            stem.transform.localRotation = Quaternion.Euler(0f, 0f, -5f);

            for (int b = 0; b < 4; b++)
            {
                float budHeight = stemHeight * (0.65f + (b * 0.12f));
                Vector3 budCenter = localOffset + new Vector3(((b % 2 == 0) ? -0.03f : 0.03f), budHeight, 0f);

                GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                petal.name = $"SpikeBud_{b}";
                petal.transform.SetParent(parent, false);
                petal.transform.localPosition = budCenter;
                petal.transform.localScale = new Vector3(0.055f, 0.075f, 0.055f);
                ApplyMaterial(petal, petalMaterial);

                GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                center.name = $"SpikeCenter_{b}";
                center.transform.SetParent(parent, false);
                center.transform.localPosition = budCenter + new Vector3(0f, 0.005f, 0.02f);
                center.transform.localScale = new Vector3(0.022f, 0.022f, 0.022f);
                ApplyMaterial(center, centerMaterial);
            }
        }

        private static void CreateCupFlower(Transform parent, Vector3 localOffset, Material stemMaterial, Material petalMaterial, Material centerMaterial, float stemHeight)
        {
            GameObject stem = CreateCylinderPart("CupStem", parent, localOffset + new Vector3(0f, stemHeight * 0.5f, 0f), new Vector3(0.015f, stemHeight * 0.5f, 0.015f), stemMaterial);
            stem.transform.localRotation = Quaternion.Euler(0f, 0f, 2f);

            Vector3 bloomCenter = localOffset + new Vector3(0f, stemHeight, 0f);
            for (int p = 0; p < 5; p++)
            {
                float angle = p * 72f;
                GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                petal.name = $"CupPetal_{p}";
                petal.transform.SetParent(parent, false);
                petal.transform.localPosition = bloomCenter + (Quaternion.Euler(0f, angle, 0f) * new Vector3(0.04f, 0.015f, 0f));
                petal.transform.localScale = new Vector3(0.055f, 0.1f, 0.055f);
                petal.transform.localRotation = Quaternion.Euler(-8f, angle, 0f);
                ApplyMaterial(petal, petalMaterial);
            }

            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            center.name = "CupCenter";
            center.transform.SetParent(parent, false);
            center.transform.localPosition = bloomCenter + new Vector3(0f, 0.01f, 0f);
            center.transform.localScale = new Vector3(0.032f, 0.032f, 0.032f);
            ApplyMaterial(center, centerMaterial);
        }

        private static void CreateStarFlower(Transform parent, Vector3 localOffset, Material stemMaterial, Material petalMaterial, Material centerMaterial, float stemHeight)
        {
            GameObject stem = CreateCylinderPart("StarStem", parent, localOffset + new Vector3(0f, stemHeight * 0.5f, 0f), new Vector3(0.014f, stemHeight * 0.5f, 0.014f), stemMaterial);
            stem.transform.localRotation = Quaternion.Euler(0f, 0f, -4f);

            Vector3 bloomCenter = localOffset + new Vector3(0f, stemHeight, 0f);
            for (int p = 0; p < 8; p++)
            {
                float angle = p * 45f;
                GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                petal.name = $"StarPetal_{p}";
                petal.transform.SetParent(parent, false);
                petal.transform.localPosition = bloomCenter + (Quaternion.Euler(0f, angle, 0f) * new Vector3(0.065f, 0f, 0f));
                petal.transform.localScale = new Vector3(0.075f, 0.012f, 0.03f);
                petal.transform.localRotation = Quaternion.Euler(18f, angle, 28f);
                ApplyMaterial(petal, petalMaterial);
            }

            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            center.name = "StarCenter";
            center.transform.SetParent(parent, false);
            center.transform.localPosition = bloomCenter + new Vector3(0f, 0.01f, 0f);
            center.transform.localScale = new Vector3(0.038f, 0.03f, 0.038f);
            ApplyMaterial(center, centerMaterial);
        }

        private static void CreateSunflowerBloom(Transform parent, Vector3 localOffset, Material stemMaterial, Material petalMaterial, Material centerMaterial, float stemHeight)
        {
            GameObject stem = CreateCylinderPart("SunflowerStem", parent, localOffset + new Vector3(0f, stemHeight * 0.5f, 0f), new Vector3(0.018f, stemHeight * 0.5f, 0.018f), stemMaterial);
            stem.transform.localRotation = Quaternion.Euler(0f, 0f, 3f);

            Vector3 bloomCenter = localOffset + new Vector3(0f, stemHeight, 0f);
            for (int p = 0; p < 10; p++)
            {
                float angle = p * 36f;
                GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                petal.name = $"SunflowerPetal_{p}";
                petal.transform.SetParent(parent, false);
                petal.transform.localPosition = bloomCenter + (Quaternion.Euler(0f, angle, 0f) * new Vector3(0.105f, 0f, 0f));
                petal.transform.localScale = new Vector3(0.11f, 0.03f, 0.045f);
                petal.transform.localRotation = Quaternion.Euler(8f, angle, 18f);
                ApplyMaterial(petal, petalMaterial);
            }

            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            center.name = "SunflowerCenter";
            center.transform.SetParent(parent, false);
            center.transform.localPosition = bloomCenter + new Vector3(0f, 0.01f, 0f);
            center.transform.localScale = new Vector3(0.07f, 0.02f, 0.07f);
            ApplyMaterial(center, centerMaterial);
        }

        private static void CreateLeafPile(Transform parent, Vector3 position, Material material, List<GameObject> autumnObjects)
        {
            GameObject pile = new GameObject("LeafPile");
            pile.transform.SetParent(parent, false);
            pile.transform.position = position;

            for (int i = 0; i < 18; i++)
            {
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = $"Leaf_{i}";
                leaf.transform.SetParent(pile.transform, false);
                leaf.transform.localPosition = new Vector3(
                    Mathf.Sin(i * 1.37f) * 0.88f,
                    0.015f + (i * 0.0025f),
                    Mathf.Cos(i * 1.12f) * 0.72f);
                leaf.transform.localRotation = Quaternion.Euler(0f, i * 21f, 14f + (i * 3f));
                leaf.transform.localScale = new Vector3(
                    0.22f + ((i % 3) * 0.06f),
                    0.012f,
                    0.075f + ((i % 2) * 0.025f));
                ApplyMaterial(leaf, material);
            }

            autumnObjects.Add(pile);
        }

        private static void CreateAutumnGroundLeavesForTrees(Transform parent, Material material, List<GameObject> autumnObjects)
        {
            GameObject propsRoot = GameObject.Find("Props");
            if (propsRoot == null)
            {
                return;
            }

            foreach (Transform child in propsRoot.transform)
            {
                if (!child.name.Contains("Palm"))
                {
                    continue;
                }

                CreateLeafPile(parent, child.position + new Vector3(0.35f, 0.05f, 0.32f), material, autumnObjects);
                CreateLeafPile(parent, child.position + new Vector3(-0.42f, 0.05f, -0.31f), material, autumnObjects);
                CreateLeafPile(parent, child.position + new Vector3(-0.22f, 0.05f, 0.48f), material, autumnObjects);
                CreateLeafPile(parent, child.position + new Vector3(0.52f, 0.05f, -0.18f), material, autumnObjects);
                CreateLeafPile(parent, child.position + new Vector3(-0.56f, 0.05f, 0.12f), material, autumnObjects);
            }
        }

        private static void CreatePuddle(Transform parent, Vector3 position, Material material, List<GameObject> rainOnlyObjects)
        {
            GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddle.name = "Puddle";
            puddle.transform.SetParent(parent, false);
            puddle.transform.position = position;
            puddle.transform.localScale = new Vector3(0.6f, 0.01f, 0.6f);
            ApplyMaterial(puddle, material);
            rainOnlyObjects.Add(puddle);
        }

        private static void CreateStormCloudField(Transform parent, Material material, List<GameObject> cloudObjects)
        {
            Vector3[] centers =
            {
                new Vector3(-162f, 27.4f, -146f), new Vector3(-119f, 25.8f, -131f), new Vector3(-73f, 28.1f, -152f),
                new Vector3(-21f, 26.2f, -139f), new Vector3(33f, 27.6f, -149f), new Vector3(88f, 25.9f, -128f),
                new Vector3(148f, 27.8f, -144f),

                new Vector3(-149f, 25.5f, -92f), new Vector3(-98f, 27.2f, -108f), new Vector3(-47f, 24.9f, -82f),
                new Vector3(4f, 26.6f, -103f), new Vector3(58f, 25.7f, -89f), new Vector3(112f, 27.1f, -110f),
                new Vector3(166f, 25.3f, -84f),

                new Vector3(-171f, 24.8f, -44f), new Vector3(-126f, 26.1f, -18f), new Vector3(-81f, 25.4f, -37f),
                new Vector3(-33f, 24.7f, -9f), new Vector3(14f, 26.3f, -31f), new Vector3(66f, 25.1f, -12f),
                new Vector3(118f, 26.4f, -39f), new Vector3(176f, 24.9f, -21f),

                new Vector3(-158f, 26.2f, 7f), new Vector3(-111f, 24.9f, 29f), new Vector3(-62f, 25.8f, 11f),
                new Vector3(-9f, 24.6f, 37f), new Vector3(42f, 26.5f, 8f), new Vector3(94f, 25.2f, 26f),
                new Vector3(144f, 26.1f, 4f), new Vector3(184f, 24.8f, 33f),

                new Vector3(-146f, 25.6f, 72f), new Vector3(-93f, 27f, 88f), new Vector3(-38f, 25f, 61f),
                new Vector3(12f, 26.4f, 97f), new Vector3(66f, 25.5f, 68f), new Vector3(116f, 26.9f, 92f),
                new Vector3(168f, 25.8f, 59f),

                new Vector3(-132f, 27.5f, 132f), new Vector3(-82f, 26f, 117f), new Vector3(-24f, 28f, 144f),
                new Vector3(29f, 26.4f, 121f), new Vector3(81f, 27.3f, 139f), new Vector3(131f, 25.9f, 116f),
                new Vector3(176f, 27.7f, 147f)
            };

            for (int i = 0; i < centers.Length; i++)
            {
                float width = 1.05f + ((i % 4) * 0.2f);
                float depth = 0.95f + ((i % 3) * 0.18f);
                CreateCloudCluster(parent, centers[i], width, depth, material, cloudObjects, i);
            }
        }

        private static void CreateCloudCluster(
            Transform parent,
            Vector3 center,
            float widthScale,
            float depthScale,
            Material material,
            List<GameObject> cloudObjects,
            int seed)
        {
            GameObject cluster = new GameObject($"CloudCluster_{seed}");
            cluster.transform.SetParent(parent, false);
            cluster.transform.position = center;

            Vector3[] offsets =
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(-4.2f * widthScale, -0.4f, 0.8f * depthScale),
                new Vector3(4.6f * widthScale, -0.25f, -0.6f * depthScale),
                new Vector3(-1.6f * widthScale, 0.5f, -2.6f * depthScale),
                new Vector3(2.2f * widthScale, 0.65f, 2.4f * depthScale),
                new Vector3(-6.8f * widthScale, -0.1f, -1.2f * depthScale),
                new Vector3(7.2f * widthScale, 0.15f, 1.4f * depthScale),
                new Vector3(0.4f, 1.1f, 0.2f)
            };

            Vector3[] scales =
            {
                new Vector3(11f * widthScale, 3.4f, 7.6f * depthScale),
                new Vector3(8.8f * widthScale, 2.9f, 6.5f * depthScale),
                new Vector3(8.4f * widthScale, 2.8f, 6.2f * depthScale),
                new Vector3(7.4f * widthScale, 2.6f, 5.6f * depthScale),
                new Vector3(7.1f * widthScale, 2.5f, 5.4f * depthScale),
                new Vector3(6.5f * widthScale, 2.2f, 4.8f * depthScale),
                new Vector3(6.1f * widthScale, 2.1f, 4.6f * depthScale),
                new Vector3(6f * widthScale, 2.1f, 4.2f * depthScale)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = $"CloudPuff_{i}";
                puff.transform.SetParent(cluster.transform, false);
                puff.transform.localPosition = offsets[i];
                puff.transform.localScale = scales[i];
                puff.transform.localRotation = Quaternion.Euler(0f, (seed * 19f) + (i * 11f), 0f);
                ApplyMaterial(puff, material);

                Renderer renderer = puff.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.allowOcclusionWhenDynamic = false;
                }
            }

            cloudObjects.Add(cluster);
        }

        private static void CreateSnowOverlays(Transform parent, Material material, List<GameObject> snowOverlayObjects)
        {
            GameObject propsRoot = GameObject.Find("Props");
            if (propsRoot == null)
            {
                return;
            }

            int created = 0;
            foreach (Transform child in propsRoot.transform)
            {
                if (!child.name.Contains("Rock"))
                {
                    continue;
                }

                GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                overlay.name = "SnowOverlay";
                overlay.transform.SetParent(parent, false);
                overlay.transform.position = child.position + new Vector3(0f, 0.24f, 0f);
                overlay.transform.localScale = new Vector3(1.05f, 0.12f, 1.05f);
                ApplyMaterial(overlay, material);
                snowOverlayObjects.Add(overlay);
                created++;

                if (created >= 16)
                {
                    break;
                }
            }
        }

        private static void CreateNightLight(Transform parent, Vector3 position, List<GameObject> nightLightObjects)
        {
            GameObject lightObject = new GameObject("NightLamp", typeof(Light));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;

            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.range = 6f;
            light.intensity = 4.2f;
            light.color = new Color(1f, 0.8f, 0.52f);

            nightLightObjects.Add(lightObject);
        }

        private static GameObject CreateRainEffect(Transform parent, string objectName, Material material)
        {
            GameObject rainObject = new GameObject(objectName, typeof(ParticleSystem));
            rainObject.transform.SetParent(parent, false);
            rainObject.transform.position = new Vector3(0f, 44f, 0f);

            ParticleSystem particleSystem = rainObject.GetComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.7f;
            main.startSpeed = 0f;
            main.startSize = 0.06f;
            main.startColor = new Color(0.76f, 0.88f, 1f, 0.78f);
            main.maxParticles = 6000;

            var emission = particleSystem.emission;
            emission.rateOverTime = 5200f;

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(220f, 1f, 220f);

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
            renderer.sharedMaterial = material;

            rainObject.SetActive(false);
            return rainObject;
        }

        private static GameObject CreateThunderstormEffect(Transform parent, string objectName)
        {
            GameObject stormObject = new GameObject(objectName);
            stormObject.transform.SetParent(parent, false);
            stormObject.transform.position = Vector3.zero;
            stormObject.SetActive(false);
            return stormObject;
        }

        private static GameObject CreateWinterSnowEffect(Transform parent, string objectName, Material material)
        {
            GameObject snowObject = new GameObject(objectName, typeof(ParticleSystem));
            snowObject.transform.SetParent(parent, false);
            snowObject.transform.position = new Vector3(0f, 44f, 0f);

            ParticleSystem particleSystem = snowObject.GetComponent<ParticleSystem>();
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
            shape.scale = new Vector3(220f, 18f, 220f);

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
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;

            snowObject.SetActive(false);
            return snowObject;
        }

        private static GameObject CreateAutumnLeavesEffect(Transform parent, Vector3 position, Material material)
        {
            GameObject leavesObject = new GameObject("Autumn Leaves Effect", typeof(ParticleSystem));
            leavesObject.transform.SetParent(parent, false);
            leavesObject.transform.position = position;

            ParticleSystem particleSystem = leavesObject.GetComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 3f;
            main.startSpeed = 1.8f;
            main.startSize = 0.14f;
            main.startColor = new Color(0.86f, 0.48f, 0.18f, 0.95f);
            main.maxParticles = 250;

            var emission = particleSystem.emission;
            emission.rateOverTime = 35f;

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5f, 1f, 5f);

            ParticleSystemRenderer renderer = leavesObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;

            leavesObject.SetActive(false);
            return leavesObject;
        }

        private static void AssignEnvironmentManager(
            WorldEnvironmentManager environmentManager,
            GameObject seasonBoxRoot,
            DemoObjects demoObjects,
            SeasonBoxMaterialSet materialSet,
            SkyObjects skyObjects)
        {
            SerializedObject serializedManager = new SerializedObject(environmentManager);

            GameObject islandBootstrap = GameObject.Find("Island Bootstrap");
            Transform islandRoot = islandBootstrap != null ? islandBootstrap.transform.Find("Island") : null;
            Light sunLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
            Camera mainCamera = Camera.main != null ? Camera.main : GameObject.Find("Main Camera")?.GetComponent<Camera>();
            GameObject worldControlBox = GameObject.Find("World Control Box");

            SetObjectReference(serializedManager, "sunLight", sunLight);
            SetObjectReference(serializedManager, "islandRoot", islandRoot);
            SetObjectReference(serializedManager, "targetCamera", mainCamera);
            SetObjectReference(serializedManager, "daySunObject", skyObjects.daySun);
            SetObjectReference(serializedManager, "nightMoonObject", skyObjects.nightMoon);

            SetObjectReference(serializedManager, "springGroundMaterial", materialSet.springGround);
            SetObjectReference(serializedManager, "summerGroundMaterial", materialSet.summerGround);
            SetObjectReference(serializedManager, "autumnGroundMaterial", materialSet.autumnGround);
            SetObjectReference(serializedManager, "winterGroundMaterial", materialSet.winterGround);
            SetObjectReference(serializedManager, "wetGroundMaterial", materialSet.wetGround);

            SetObjectReference(serializedManager, "springLeafMaterial", materialSet.springLeaves);
            SetObjectReference(serializedManager, "summerLeafMaterial", materialSet.summerLeaves);
            SetObjectReference(serializedManager, "autumnLeafMaterial", materialSet.autumnLeaves);
            SetObjectReference(serializedManager, "winterLeafMaterial", materialSet.winterLeaves);

            SetObjectReference(serializedManager, "springExtraMaterial", materialSet.springExtra);
            SetObjectReference(serializedManager, "summerExtraMaterial", materialSet.summerExtra);
            SetObjectReference(serializedManager, "autumnExtraMaterial", materialSet.autumnExtra);
            SetObjectReference(serializedManager, "winterExtraMaterial", materialSet.winterExtra);

            SetObjectArrayReference(serializedManager, "flowerObjects", demoObjects.flowerObjects);
            SetObjectArrayReference(serializedManager, "snowOverlayObjects", demoObjects.snowOverlayObjects);
            SetObjectArrayReference(serializedManager, "autumnObjects", demoObjects.autumnObjects);
            SetObjectArrayReference(serializedManager, "rainOnlyObjects", demoObjects.rainOnlyObjects);
            SetObjectArrayReference(serializedManager, "cloudObjects", demoObjects.cloudObjects);
            SetObjectArrayReference(serializedManager, "nightLightObjects", demoObjects.nightLightObjects);
            SetObjectArrayReference(serializedManager, "controlBoxAccentRenderers", CollectControlBoxAccentRenderers(worldControlBox));

            SetObjectReference(serializedManager, "rainEffect", demoObjects.rainEffect);
            SetObjectReference(serializedManager, "winterSnowEffect", demoObjects.winterSnowEffect);
            SetObjectReference(serializedManager, "thunderstormEffect", demoObjects.thunderstormEffect);
            SetObjectReference(serializedManager, "autumnLeavesEffect", demoObjects.autumnLeavesEffect);
            SetObjectReference(serializedManager, "snowParticleMaterial", materialSet.snowParticle);
            SetObjectReference(serializedManager, "autumnParticleMaterial", materialSet.autumnParticle);

            serializedManager.FindProperty("autoCreateWeatherEffectsIfMissing").boolValue = false;
            serializedManager.FindProperty("followTargetWithWeatherEffects").boolValue = false;
            serializedManager.FindProperty("weatherFollowHeight").floatValue = 44f;
            serializedManager.FindProperty("autoCollectGeneratedIslandReferences").boolValue = true;
            serializedManager.FindProperty("flowersStayActiveInSummer").boolValue = false;
            serializedManager.FindProperty("islandWeatherArea").vector2Value = new Vector2(220f, 220f);
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateCubePart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            ApplyMaterial(part, material);
            return part;
        }

        private static GameObject CreateCylinderPart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            ApplyMaterial(part, material);
            return part;
        }

        private static void CreateSeasonTile(string name, Transform parent, Vector3 localPosition, Material material)
        {
            GameObject tile = CreateCubePart(name, parent, localPosition, new Vector3(0.24f, 0.18f, 0.06f), material);
            tile.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        }

        private static void CreateMiniWeatherTile(string name, Transform parent, Vector3 localPosition, Color color, SeasonBoxMaterialSet materialSet)
        {
            GameObject tile = CreateCubePart(name, parent, localPosition, new Vector3(0.16f, 0.08f, 0.05f), materialSet.panel);
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }
        }

        private static void ApplyMaterial(GameObject gameObject, Material material)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static List<Renderer> CollectControlBoxAccentRenderers(GameObject worldControlBox)
        {
            List<Renderer> renderers = new List<Renderer>();
            if (worldControlBox == null)
            {
                return renderers;
            }

            Renderer[] childRenderers = worldControlBox.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in childRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                string objectName = renderer.gameObject.name;
                if (objectName.Contains("Accent") ||
                    objectName.Contains("Header") ||
                    objectName.Contains("ControlFrame") ||
                    objectName.Contains("FrontBoard"))
                {
                    renderers.Add(renderer);
                }
            }

            return renderers;
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object reference)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = reference;
            }
        }

        private static void SetObjectArrayReference<T>(SerializedObject serializedObject, string propertyName, List<T> references) where T : Object
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.arraySize = references.Count;
            for (int i = 0; i < references.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
            }
        }

        private sealed class SeasonBoxMaterialSet
        {
            public Material springGround;
            public Material summerGround;
            public Material autumnGround;
            public Material winterGround;
            public Material wetGround;
            public Material springLeaves;
            public Material summerLeaves;
            public Material autumnLeaves;
            public Material winterLeaves;
            public Material springExtra;
            public Material summerExtra;
            public Material autumnExtra;
            public Material winterExtra;
            public Material wood;
            public Material panel;
            public Material cloud;
            public Material snowOverlay;
            public Material autumnPile;
            public Material puddle;
            public Material flowerStem;
            public Material flowerPetal;
            public Material flowerPetalBlue;
            public Material flowerPetalWhite;
            public Material flowerPetalYellow;
            public Material flowerPetalLavender;
            public Material flowerPetalCoral;
            public Material flowerCenter;
            public Material sun;
            public Material moon;
            public Material rainParticle;
            public Material snowParticle;
            public Material autumnParticle;
        }

        private sealed class SkyObjects
        {
            public GameObject daySun;
            public GameObject nightMoon;
        }

        private sealed class DemoObjects
        {
            public List<GameObject> flowerObjects;
            public List<GameObject> snowOverlayObjects;
            public List<GameObject> autumnObjects;
            public List<GameObject> rainOnlyObjects;
            public List<GameObject> cloudObjects;
            public List<GameObject> nightLightObjects;
            public GameObject rainEffect;
            public GameObject winterSnowEffect;
            public GameObject thunderstormEffect;
            public GameObject autumnLeavesEffect;
        }
    }
}
