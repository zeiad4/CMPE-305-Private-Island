using System.Collections.Generic;
using UnityEngine;

namespace PrivateIsland
{
    internal readonly struct IslandSeasideHouseResult
    {
        public IslandSeasideHouseResult(Vector3 center, float clearanceRadius)
        {
            Center = center;
            ClearanceRadius = clearanceRadius;
        }

        public Vector3 Center { get; }
        public float ClearanceRadius { get; }
    }

    internal static class IslandSeasideHouseBuilder
    {
        private const string BedroomWallPortraitResourcePath = "House/BedroomWallPortrait";

        private static readonly Color PlasterTint = new Color(0.94f, 0.91f, 0.85f);
        private static readonly Color TrimTint = new Color(0.08f, 0.33f, 0.67f);
        private static readonly Color WoodTint = new Color(0.72f, 0.49f, 0.17f);
        private static readonly Color MetalTint = new Color(0.1f, 0.09f, 0.09f);
        private static readonly Color FloorTint = new Color(0.78f, 0.74f, 0.68f);
        private static readonly Color CeilingTint = new Color(0.97f, 0.96f, 0.93f);
        private static readonly Color CourtyardTint = new Color(0.78f, 0.36f, 0.22f);
        private static readonly Color AccentShadowTint = new Color(0.78f, 0.82f, 0.84f);
        private static readonly Color LanternTint = new Color(0.94f, 0.78f, 0.43f);
        private static readonly Color InteriorWallTint = new Color(0.78f, 0.82f, 0.84f);
        private static readonly Color InteriorCeilingTint = new Color(0.7f, 0.74f, 0.76f);

        private const float WallHeight = 4.55f;
        private const float WallThickness = 0.42f;
        private const float HouseWidth = 13f;
        private const float HouseDepth = 10f;
        private const float FrontFaceZ = 4.52f;
        private const float BackFaceZ = -5.48f;
        private const float DoorWidth = 2.42f;
        private const float DoorHeight = 3.06f;

        public static IslandSeasideHouseResult Build(
            Transform parent,
            Vector3 position,
            Vector3 forward,
            Material plasterMaterial,
            Material accentMaterial,
            Material woodMaterial,
            Material detailMaterial,
            Material floorMaterial)
        {
            GameObject houseRoot = new GameObject("SeasideHouse");
            houseRoot.transform.SetParent(parent, false);
            houseRoot.transform.localPosition = position;
            houseRoot.transform.localRotation = Quaternion.LookRotation(
                forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward,
                Vector3.up);

            BuildTerrace(houseRoot.transform, plasterMaterial, accentMaterial, floorMaterial);
            BuildShell(houseRoot.transform, plasterMaterial, floorMaterial);
            BuildExteriorBands(houseRoot.transform, accentMaterial);
            BuildDoorFacade(houseRoot.transform, plasterMaterial, accentMaterial, woodMaterial, detailMaterial);
            BuildInterior(houseRoot.transform, plasterMaterial, accentMaterial, woodMaterial, detailMaterial, floorMaterial);
            BuildDoorInteractions(houseRoot.transform);

            return new IslandSeasideHouseResult(position, 22f);
        }

        private static void BuildTerrace(Transform root, Material plasterMaterial, Material accentMaterial, Material floorMaterial)
        {
            CreatePhysicalBox(root, "Foundation", plasterMaterial, PlasterTint, new Vector3(0f, -1.5f, 0.65f), new Vector3(20.8f, 3f, 19.3f));
            CreatePhysicalBox(root, "UpperTerrace", plasterMaterial, PlasterTint, new Vector3(0f, -1.4f, 7.2f), new Vector3(16.8f, 2.8f, 5.4f));
            CreatePhysicalBox(root, "TerraceFrontSupport", plasterMaterial, PlasterTint, new Vector3(0f, -1.45f, 8.18f), new Vector3(16.2f, 2.9f, 3.2f));
            CreatePhysicalBox(root, "EntranceGroundSupport", plasterMaterial, PlasterTint, new Vector3(0f, -2.1f, 7.44f), new Vector3(13.8f, 4.2f, 3.6f));

            CreatePhysicalBox(root, "Step_0", plasterMaterial, PlasterTint, new Vector3(0f, -0.7f, 5.62f), new Vector3(8.2f, 0.66f, 0.94f));
            CreatePhysicalBox(root, "Step_1", plasterMaterial, PlasterTint, new Vector3(0f, -1.02f, 6.22f), new Vector3(10.2f, 0.86f, 0.94f));
            CreatePhysicalBox(root, "Step_2", plasterMaterial, PlasterTint, new Vector3(0f, -1.38f, 6.82f), new Vector3(12.2f, 1.14f, 0.94f));
            CreatePhysicalBox(root, "Step_3", plasterMaterial, PlasterTint, new Vector3(0f, -1.78f, 7.42f), new Vector3(14.2f, 1.5f, 0.94f));

            CreateWalkableSurface(root, "PlatformWalkable", new Vector3(0f, 0.03f, -0.6f), new Vector3(19.9f, 0.18f, 16.1f));
            CreateWalkableSurface(root, "TerraceWalkable", new Vector3(0f, 0.03f, 6.95f), new Vector3(16.2f, 0.18f, 3.7f));

            CreateCourtyardPattern(root, accentMaterial);

            CreatePhysicalBox(root, "BenchLeftSeat", plasterMaterial, PlasterTint, new Vector3(-7.1f, 0.46f, 6.68f), new Vector3(2.4f, 0.18f, 0.7f));
            CreatePhysicalBox(root, "BenchLeftLegA", plasterMaterial, PlasterTint, new Vector3(-8.03f, 0.24f, 6.68f), new Vector3(0.18f, 0.52f, 0.66f));
            CreatePhysicalBox(root, "BenchLeftLegB", plasterMaterial, PlasterTint, new Vector3(-6.17f, 0.24f, 6.68f), new Vector3(0.18f, 0.52f, 0.66f));

            CreatePhysicalBox(root, "BenchRightSeat", plasterMaterial, PlasterTint, new Vector3(7.1f, 0.46f, 6.68f), new Vector3(2.4f, 0.18f, 0.7f));
            CreatePhysicalBox(root, "BenchRightLegA", plasterMaterial, PlasterTint, new Vector3(6.17f, 0.24f, 6.68f), new Vector3(0.18f, 0.52f, 0.66f));
            CreatePhysicalBox(root, "BenchRightLegB", plasterMaterial, PlasterTint, new Vector3(8.03f, 0.24f, 6.68f), new Vector3(0.18f, 0.52f, 0.66f));

            CreateBox(root, "DoorMat", floorMaterial, CourtyardTint, new Vector3(0f, 0.04f, 5.1f), new Vector3(3.3f, 0.03f, 0.42f));
        }

        private static void BuildShell(Transform root, Material plasterMaterial, Material floorMaterial)
        {
            float halfWidth = HouseWidth * 0.5f;
            float halfDepth = HouseDepth * 0.5f;

            CreateSolidBox(root, "LeftWall", plasterMaterial, PlasterTint, new Vector3(-halfWidth + (WallThickness * 0.5f), WallHeight * 0.5f, -0.48f), new Vector3(WallThickness, WallHeight, HouseDepth));
            CreateSolidBox(root, "RightWall", plasterMaterial, PlasterTint, new Vector3(halfWidth - (WallThickness * 0.5f), WallHeight * 0.5f, -0.48f), new Vector3(WallThickness, WallHeight, HouseDepth));
            CreateSolidBox(root, "BackWall", plasterMaterial, PlasterTint, new Vector3(0f, WallHeight * 0.5f, BackFaceZ + (WallThickness * 0.5f)), new Vector3(HouseWidth, WallHeight, WallThickness));
            CreateSolidBox(root, "FrontWallLeft", plasterMaterial, PlasterTint, new Vector3(-4f, WallHeight * 0.5f, FrontFaceZ - (WallThickness * 0.5f)), new Vector3(5f, WallHeight, WallThickness));
            CreateSolidBox(root, "FrontWallRight", plasterMaterial, PlasterTint, new Vector3(4f, WallHeight * 0.5f, FrontFaceZ - (WallThickness * 0.5f)), new Vector3(5f, WallHeight, WallThickness));
            CreateSolidBox(root, "FrontWallTop", plasterMaterial, PlasterTint, new Vector3(0f, 3.86f, FrontFaceZ - (WallThickness * 0.5f)), new Vector3(3f, 1.38f, WallThickness));

            CreateSolidBox(root, "InteriorFloor", floorMaterial, FloorTint, new Vector3(0f, 0.06f, -0.48f), new Vector3(HouseWidth - 0.38f, 0.12f, HouseDepth - 0.38f));
            CreateWalkableSurface(root, "InteriorWalkableFloor", new Vector3(0f, 0.07f, -0.48f), new Vector3(HouseWidth - 0.42f, 0.16f, HouseDepth - 0.42f));

            GameObject ceiling = CreateSolidBox(root, "Ceiling", plasterMaterial, CeilingTint, new Vector3(0f, 4.56f, -0.48f), new Vector3(HouseWidth - 0.22f, 0.24f, HouseDepth - 0.22f));
            ceiling.AddComponent<IslandWeatherCover>();
            CreateBox(root, "ParapetFront", plasterMaterial, CeilingTint, new Vector3(0f, 4.92f, 4.44f), new Vector3(HouseWidth + 0.96f, 0.38f, 0.42f));
            CreateBox(root, "ParapetBack", plasterMaterial, CeilingTint, new Vector3(0f, 4.92f, -5.4f), new Vector3(HouseWidth + 0.96f, 0.38f, 0.42f));
            CreateBox(root, "ParapetLeft", plasterMaterial, CeilingTint, new Vector3(-6.7f, 4.92f, -0.48f), new Vector3(0.42f, 0.38f, HouseDepth + 0.72f));
            CreateBox(root, "ParapetRight", plasterMaterial, CeilingTint, new Vector3(6.7f, 4.92f, -0.48f), new Vector3(0.42f, 0.38f, HouseDepth + 0.72f));

            CreateBox(root, "RoofBoxLeft", plasterMaterial, CeilingTint, new Vector3(-5.32f, 5.34f, -4.62f), new Vector3(0.9f, 0.76f, 0.86f));
            CreateBox(root, "RoofBoxRight", plasterMaterial, CeilingTint, new Vector3(5.32f, 5.34f, -4.62f), new Vector3(0.9f, 0.76f, 0.86f));
        }

        private static void BuildExteriorBands(Transform root, Material accentMaterial)
        {
            for (int i = 0; i < 5; i++)
            {
                float y = 0.72f + (i * 0.98f);

                CreateBox(root, $"FrontBandLeft_{i}", accentMaterial, TrimTint, new Vector3(-3.92f, y, 4.84f), new Vector3(4.88f, 0.2f, 0.16f));
                CreateBox(root, $"FrontBandRight_{i}", accentMaterial, TrimTint, new Vector3(3.92f, y, 4.84f), new Vector3(4.88f, 0.2f, 0.16f));

                CreateBox(root, $"BackBand_{i}", accentMaterial, TrimTint, new Vector3(0f, y, -5.82f), new Vector3(12.8f, 0.2f, 0.16f));
                CreateBox(root, $"LeftBand_{i}", accentMaterial, TrimTint, new Vector3(-6.74f, y, -0.48f), new Vector3(0.16f, 0.2f, 9.72f));
                CreateBox(root, $"RightBand_{i}", accentMaterial, TrimTint, new Vector3(6.74f, y, -0.48f), new Vector3(0.16f, 0.2f, 9.72f));

                CreateBox(root, $"BandShadowLeft_{i}", accentMaterial, AccentShadowTint, new Vector3(-3.92f, y - 0.12f, 4.68f), new Vector3(4.88f, 0.05f, 0.08f));
                CreateBox(root, $"BandShadowRight_{i}", accentMaterial, AccentShadowTint, new Vector3(3.92f, y - 0.12f, 4.68f), new Vector3(4.88f, 0.05f, 0.08f));
            }
        }

        private static void BuildDoorFacade(Transform root, Material plasterMaterial, Material accentMaterial, Material woodMaterial, Material detailMaterial)
        {
            CreateBox(root, "DoorInsetLeft", plasterMaterial, CeilingTint, new Vector3(-1.56f, 1.94f, 4.78f), new Vector3(0.58f, 3.32f, 0.18f));
            CreateBox(root, "DoorInsetRight", plasterMaterial, CeilingTint, new Vector3(1.56f, 1.94f, 4.78f), new Vector3(0.58f, 3.32f, 0.18f));
            CreateBox(root, "DoorInsetTop", plasterMaterial, CeilingTint, new Vector3(0f, 3.56f, 4.78f), new Vector3(3f, 0.18f, 0.18f));

            CreateBox(root, "DoorLeafLeft", woodMaterial, WoodTint, new Vector3(-0.58f, 1.46f, 4.94f), new Vector3(1.1f, 2.92f, 0.18f));
            CreateBox(root, "DoorLeafRight", woodMaterial, WoodTint, new Vector3(0.58f, 1.46f, 4.94f), new Vector3(1.1f, 2.92f, 0.18f));
            CreateCylinder(root, "DoorArch", woodMaterial, WoodTint, new Vector3(0f, 2.92f, 4.94f), new Vector3(1.22f, 0.09f, 1.22f), Quaternion.Euler(90f, 0f, 0f));
            CreateSolidBox(root, "DoorPassageBlocker", woodMaterial, WoodTint, new Vector3(0f, 1.48f, 4.8f), new Vector3(2.34f, 2.98f, 0.34f));
            CreateSolidBox(root, "DoorPassageBlockerArch", woodMaterial, WoodTint, new Vector3(0f, 2.94f, 4.8f), new Vector3(2.22f, 0.34f, 0.34f));
            CreateBox(root, "DoorInnerSealLeft", woodMaterial, WoodTint, new Vector3(-0.58f, 1.46f, 4.7f), new Vector3(1.14f, 2.96f, 0.22f));
            CreateBox(root, "DoorInnerSealRight", woodMaterial, WoodTint, new Vector3(0.58f, 1.46f, 4.7f), new Vector3(1.14f, 2.96f, 0.22f));
            CreateBox(root, "DoorInnerTopSeal", woodMaterial, WoodTint, new Vector3(0f, 3.02f, 4.68f), new Vector3(2.3f, 0.16f, 0.24f));
            CreateCylinder(root, "DoorInnerArchSeal", woodMaterial, WoodTint, new Vector3(0f, 2.92f, 4.7f), new Vector3(1.16f, 0.12f, 1.16f), Quaternion.Euler(90f, 0f, 0f));

            CreateBox(root, "DoorBandTopLeft", detailMaterial, MetalTint, new Vector3(-0.58f, 2.58f, 5.05f), new Vector3(0.72f, 0.08f, 0.04f));
            CreateBox(root, "DoorBandTopRight", detailMaterial, MetalTint, new Vector3(0.58f, 2.58f, 5.05f), new Vector3(0.72f, 0.08f, 0.04f));
            CreateBox(root, "DoorBandMidLeft", detailMaterial, MetalTint, new Vector3(-0.58f, 1.7f, 5.05f), new Vector3(0.8f, 0.08f, 0.04f));
            CreateBox(root, "DoorBandMidRight", detailMaterial, MetalTint, new Vector3(0.58f, 1.7f, 5.05f), new Vector3(0.8f, 0.08f, 0.04f));
            CreateBox(root, "DoorBandLowLeft", detailMaterial, MetalTint, new Vector3(-0.58f, 0.86f, 5.05f), new Vector3(0.8f, 0.08f, 0.04f));
            CreateBox(root, "DoorBandLowRight", detailMaterial, MetalTint, new Vector3(0.58f, 0.86f, 5.05f), new Vector3(0.8f, 0.08f, 0.04f));
            CreateBox(root, "DoorLatchBarLeft", detailMaterial, MetalTint, new Vector3(-0.34f, 1.34f, 5.08f), new Vector3(0.4f, 0.1f, 0.06f));
            CreateBox(root, "DoorLatchBarCenter", detailMaterial, MetalTint, new Vector3(0.1f, 1.34f, 5.08f), new Vector3(0.38f, 0.1f, 0.06f));
            CreateBox(root, "DoorLatchBarRight", detailMaterial, MetalTint, new Vector3(0.52f, 1.34f, 5.08f), new Vector3(0.32f, 0.1f, 0.06f));
            CreateBox(root, "DoorLatchStem", detailMaterial, MetalTint, new Vector3(0.1f, 1.12f, 5.08f), new Vector3(0.08f, 0.34f, 0.06f));

            CreateBox(root, "AccentLeftLow", accentMaterial, TrimTint, new Vector3(-2.04f, 3.42f, 4.96f), new Vector3(0.34f, 1.54f, 0.12f), Quaternion.Euler(0f, 0f, 35f));
            CreateBox(root, "AccentRightLow", accentMaterial, TrimTint, new Vector3(2.04f, 3.42f, 4.96f), new Vector3(0.34f, 1.54f, 0.12f), Quaternion.Euler(0f, 0f, -35f));
            CreateBox(root, "AccentLeftMid", accentMaterial, TrimTint, new Vector3(-0.72f, 4.18f, 4.96f), new Vector3(0.28f, 1.76f, 0.12f), Quaternion.Euler(0f, 0f, 12f));
            CreateBox(root, "AccentRightMid", accentMaterial, TrimTint, new Vector3(0.72f, 4.18f, 4.96f), new Vector3(0.28f, 1.76f, 0.12f), Quaternion.Euler(0f, 0f, -12f));

            CreateLantern(root, detailMaterial, new Vector3(-4.72f, 1.86f, 4.98f));
            CreateLantern(root, detailMaterial, new Vector3(4.72f, 1.86f, 4.98f));

            CreateSolidBox(root, "DoorBackstopLeft", plasterMaterial, CeilingTint, new Vector3(-1.28f, 1.74f, 4.18f), new Vector3(0.18f, 3.08f, 0.94f));
            CreateSolidBox(root, "DoorBackstopRight", plasterMaterial, CeilingTint, new Vector3(1.28f, 1.74f, 4.18f), new Vector3(0.18f, 3.08f, 0.94f));
            CreateSolidBox(root, "DoorBackstopTop", plasterMaterial, CeilingTint, new Vector3(0f, 3.26f, 4.18f), new Vector3(2.74f, 0.18f, 0.94f));
        }

        private static void BuildInterior(Transform root, Material plasterMaterial, Material accentMaterial, Material woodMaterial, Material detailMaterial, Material floorMaterial)
        {
            CreateSolidBox(root, "EntryPocketLeft", plasterMaterial, PlasterTint, new Vector3(-2.34f, 1.7f, 3.28f), new Vector3(0.24f, 3.4f, 1.98f));
            CreateSolidBox(root, "EntryPocketRight", plasterMaterial, PlasterTint, new Vector3(2.34f, 1.7f, 3.28f), new Vector3(0.24f, 3.4f, 1.98f));
            CreateSolidBox(root, "EntryPocketTop", plasterMaterial, PlasterTint, new Vector3(0f, 3.44f, 3.28f), new Vector3(4.92f, 0.22f, 1.98f));
            GameObject interiorBackLiner = CreateBox(root, "InteriorBackLiner", plasterMaterial, InteriorWallTint, new Vector3(0f, 2.1f, -4.98f), new Vector3(12.02f, 4.02f, 0.06f));
            GameObject interiorLeftLiner = CreateBox(root, "InteriorLeftLiner", plasterMaterial, InteriorWallTint, new Vector3(-6.01f, 2.1f, -0.48f), new Vector3(0.06f, 4.02f, 9.08f));
            GameObject interiorRightLiner = CreateBox(root, "InteriorRightLiner", plasterMaterial, InteriorWallTint, new Vector3(6.01f, 2.1f, -0.48f), new Vector3(0.06f, 4.02f, 9.08f));
            GameObject interiorCeilingLiner = CreateBox(root, "InteriorCeilingLiner", plasterMaterial, InteriorCeilingTint, new Vector3(0f, 4.36f, -0.48f), new Vector3(12.1f, 0.06f, 9.2f));
            Renderer interiorFloorRenderer = root.Find("InteriorFloor")?.GetComponent<Renderer>();

            GameObject bedRoot = BuildBed(root, woodMaterial, floorMaterial, plasterMaterial);
            GameObject rugRoot = BuildBedroomRug(root, accentMaterial);
            GameObject bedsideRoot = BuildBedsideDecor(root, woodMaterial, plasterMaterial, detailMaterial);
            GameObject galleryRoot = BuildBedroomWallPortrait(root, woodMaterial, plasterMaterial, detailMaterial);
            GameObject dresserRoot = BuildBedroomDresser(root, woodMaterial, plasterMaterial, detailMaterial);

            List<Renderer> roomRenderers = new List<Renderer>
            {
                interiorBackLiner.GetComponent<Renderer>(),
                interiorLeftLiner.GetComponent<Renderer>(),
                interiorRightLiner.GetComponent<Renderer>(),
                interiorCeilingLiner.GetComponent<Renderer>(),
                interiorFloorRenderer
            };

            roomRenderers.AddRange(bedRoot.GetComponentsInChildren<Renderer>());
            roomRenderers.AddRange(rugRoot.GetComponentsInChildren<Renderer>());
            roomRenderers.AddRange(bedsideRoot.GetComponentsInChildren<Renderer>());
            roomRenderers.AddRange(galleryRoot.GetComponentsInChildren<Renderer>());
            roomRenderers.AddRange(dresserRoot.GetComponentsInChildren<Renderer>());

            Color[] lightsOnColors =
            {
                InteriorWallTint,
                InteriorWallTint,
                InteriorWallTint,
                InteriorCeilingTint,
                FloorTint
            };

            Color[] lightsOffColors =
            {
                new Color(0.24f, 0.27f, 0.3f),
                new Color(0.24f, 0.27f, 0.3f),
                new Color(0.24f, 0.27f, 0.3f),
                new Color(0.2f, 0.22f, 0.24f),
                new Color(0.19f, 0.19f, 0.2f)
            };

            BuildWallSwitchLight(root, detailMaterial, plasterMaterial, roomRenderers.ToArray(), lightsOnColors, lightsOffColors);
            BuildInteriorCeilingLightFixture(root, detailMaterial, plasterMaterial);
        }

        private static void BuildDoorInteractions(Transform root)
        {
            Transform exteriorAnchor = CreateAnchor(root, "ExteriorAnchor", new Vector3(0f, 0.05f, 5.94f), Quaternion.identity);
            Transform interiorAnchor = CreateAnchor(root, "InteriorAnchor", new Vector3(0f, 0.08f, 1.56f), Quaternion.Euler(0f, 180f, 0f));

            GameObject exteriorDoor = new GameObject("ExteriorDoorInteraction");
            exteriorDoor.transform.SetParent(root, false);
            exteriorDoor.transform.localPosition = new Vector3(0f, 0f, 5.28f);
            IslandHouseDoorInteraction exteriorInteraction = exteriorDoor.AddComponent<IslandHouseDoorInteraction>();
            exteriorInteraction.Configure(interiorAnchor, "Press E to enter the house", 3f, 1.7f, 1);

            GameObject interiorDoor = new GameObject("InteriorDoorInteraction");
            interiorDoor.transform.SetParent(root, false);
            interiorDoor.transform.localPosition = new Vector3(0f, 0f, 2.42f);
            IslandHouseDoorInteraction interiorInteraction = interiorDoor.AddComponent<IslandHouseDoorInteraction>();
            interiorInteraction.Configure(exteriorAnchor, "Press E to leave the house", 2.6f, 1.7f, -1);
        }

        private static void CreateCourtyardPattern(Transform root, Material accentMaterial)
        {
            CreatePatternSquare(root, accentMaterial, new Vector3(-5.32f, 0.03f, 8.52f), new Vector3(4.84f, 0.03f, 2.04f));
            CreatePatternCenter(root, accentMaterial);
            CreatePatternSquare(root, accentMaterial, new Vector3(5.32f, 0.03f, 8.52f), new Vector3(4.84f, 0.03f, 2.04f));
        }

        private static void CreatePatternSquare(Transform root, Material accentMaterial, Vector3 center, Vector3 baseScale)
        {
            for (int i = 0; i < 3; i++)
            {
                float width = baseScale.x - (i * 1.08f);
                float depth = baseScale.z - (i * 0.34f);
                CreateBox(root, "PatternSquare", accentMaterial, CourtyardTint, center, new Vector3(width, baseScale.y, depth));
            }
        }

        private static void CreatePatternCenter(Transform root, Material accentMaterial)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = 8.98f - (i * 0.42f);
                CreateBox(root, "PatternCenter", accentMaterial, CourtyardTint, new Vector3(0f, 0.03f, z), new Vector3(4.86f - (i * 0.54f), 0.03f, 0.18f));
            }
        }

        private static void CreateLantern(Transform root, Material detailMaterial, Vector3 localPosition)
        {
            CreateBox(root, "LanternStem", detailMaterial, MetalTint, localPosition + new Vector3(0f, 0.18f, -0.04f), new Vector3(0.08f, 0.34f, 0.08f));
            CreateBox(root, "LanternBody", detailMaterial, LanternTint, localPosition + new Vector3(0f, -0.08f, -0.04f), new Vector3(0.18f, 0.22f, 0.1f));
        }

        private static GameObject BuildBed(Transform root, Material woodMaterial, Material floorMaterial, Material plasterMaterial)
        {
            GameObject bedRoot = new GameObject("Bed");
            bedRoot.transform.SetParent(root, false);
            bedRoot.transform.localPosition = new Vector3(4.02f, 0f, -3.34f);
            bedRoot.transform.localRotation = Quaternion.identity;

            CreateBox(bedRoot.transform, "BedFrame", woodMaterial, new Color(0.52f, 0.33f, 0.18f), new Vector3(0f, 0.26f, 0f), new Vector3(4.1f, 0.28f, 2.5f));
            CreateBox(bedRoot.transform, "Mattress", plasterMaterial, CeilingTint, new Vector3(0f, 0.48f, 0f), new Vector3(3.74f, 0.26f, 2.24f));
            CreateBox(bedRoot.transform, "Blanket", woodMaterial, new Color(0.21f, 0.54f, 0.71f), new Vector3(-0.42f, 0.6f, 0f), new Vector3(2.46f, 0.14f, 2.18f));
            CreateBox(bedRoot.transform, "PillowLeft", plasterMaterial, new Color(0.95f, 0.95f, 0.93f), new Vector3(1.24f, 0.66f, -0.56f), new Vector3(0.52f, 0.16f, 0.76f));
            CreateBox(bedRoot.transform, "PillowRight", plasterMaterial, new Color(0.95f, 0.95f, 0.93f), new Vector3(1.24f, 0.66f, 0.56f), new Vector3(0.52f, 0.16f, 0.76f));
            CreateBox(bedRoot.transform, "Headboard", woodMaterial, new Color(0.48f, 0.3f, 0.16f), new Vector3(1.82f, 1.02f, 0f), new Vector3(0.18f, 1.12f, 2.42f));
            CreateBox(bedRoot.transform, "Footboard", woodMaterial, new Color(0.48f, 0.3f, 0.16f), new Vector3(-1.82f, 0.76f, 0f), new Vector3(0.18f, 0.62f, 2.42f));

            Transform sleepAnchor = CreateAnchor(
                bedRoot.transform,
                "SleepCameraAnchor",
                new Vector3(1.12f, 0.86f, 0f),
                Quaternion.Euler(-84f, 90f, 0f));

            GameObject interactionObject = new GameObject("BedInteraction");
            interactionObject.transform.SetParent(bedRoot.transform, false);
            interactionObject.transform.localPosition = new Vector3(0.12f, 0f, 0f);
            IslandBedInteraction bedInteraction = interactionObject.AddComponent<IslandBedInteraction>();
            bedInteraction.Configure(sleepAnchor, 2.8f, 0.95f);

            BoxCollider blocker = bedRoot.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, 0.52f, 0f);
            blocker.size = new Vector3(3.98f, 1.06f, 2.42f);
            bedRoot.AddComponent<IslandSolidObstacle>();
            return bedRoot;
        }

        private static GameObject BuildBedroomRug(Transform root, Material accentMaterial)
        {
            GameObject rugRoot = new GameObject("BedroomRug");
            rugRoot.transform.SetParent(root, false);
            rugRoot.transform.localPosition = new Vector3(3.22f, 0.04f, -3.08f);

            CreateBox(rugRoot.transform, "RugBase", accentMaterial, new Color(0.84f, 0.72f, 0.34f), Vector3.zero, new Vector3(4.8f, 0.025f, 3.12f));
            CreateBox(rugRoot.transform, "RugStripeA", accentMaterial, new Color(0.14f, 0.44f, 0.7f), new Vector3(0f, 0.01f, -0.74f), new Vector3(4.48f, 0.01f, 0.22f));
            CreateBox(rugRoot.transform, "RugStripeB", accentMaterial, new Color(0.14f, 0.44f, 0.7f), new Vector3(0f, 0.01f, 0f), new Vector3(4.48f, 0.01f, 0.22f));
            CreateBox(rugRoot.transform, "RugStripeC", accentMaterial, new Color(0.14f, 0.44f, 0.7f), new Vector3(0f, 0.01f, 0.74f), new Vector3(4.48f, 0.01f, 0.22f));
            return rugRoot;
        }

        private static GameObject BuildBedsideDecor(Transform root, Material woodMaterial, Material plasterMaterial, Material detailMaterial)
        {
            GameObject decorRoot = new GameObject("BedsideDecor");
            decorRoot.transform.SetParent(root, false);

            GameObject nightstand = new GameObject("Nightstand");
            nightstand.transform.SetParent(decorRoot.transform, false);
            nightstand.transform.localPosition = new Vector3(5.2f, 0f, -1.86f);

            CreateBox(nightstand.transform, "Body", woodMaterial, new Color(0.52f, 0.34f, 0.2f), new Vector3(0f, 0.34f, 0f), new Vector3(0.92f, 0.68f, 0.74f));
            CreateBox(nightstand.transform, "Shelf", woodMaterial, new Color(0.46f, 0.3f, 0.18f), new Vector3(0f, 0.15f, 0f), new Vector3(0.8f, 0.06f, 0.64f));
            CreateBox(nightstand.transform, "Handle", detailMaterial, MetalTint, new Vector3(0f, 0.38f, 0.39f), new Vector3(0.22f, 0.06f, 0.04f));

            GameObject lampBase = CreateBox(nightstand.transform, "LampBase", detailMaterial, MetalTint, new Vector3(-0.18f, 0.74f, -0.08f), new Vector3(0.14f, 0.08f, 0.14f));
            CreateBox(nightstand.transform, "LampStem", detailMaterial, MetalTint, new Vector3(-0.18f, 1f, -0.08f), new Vector3(0.05f, 0.42f, 0.05f));
            CreateBox(nightstand.transform, "LampShade", plasterMaterial, new Color(0.96f, 0.88f, 0.68f), new Vector3(-0.18f, 1.26f, -0.08f), new Vector3(0.34f, 0.28f, 0.34f));
            CreateBox(nightstand.transform, "BookStackA", woodMaterial, new Color(0.16f, 0.48f, 0.68f), new Vector3(0.18f, 0.76f, 0.04f), new Vector3(0.22f, 0.06f, 0.18f));
            CreateBox(nightstand.transform, "BookStackB", woodMaterial, new Color(0.8f, 0.66f, 0.3f), new Vector3(0.18f, 0.83f, 0.04f), new Vector3(0.22f, 0.05f, 0.18f));

            GameObject trunkBench = new GameObject("FootBench");
            trunkBench.transform.SetParent(decorRoot.transform, false);
            trunkBench.transform.localPosition = new Vector3(0.96f, 0f, -3.26f);
            CreateBox(trunkBench.transform, "BenchBody", woodMaterial, new Color(0.54f, 0.36f, 0.2f), new Vector3(0f, 0.28f, 0f), new Vector3(1.32f, 0.56f, 1.06f));
            CreateBox(trunkBench.transform, "BenchLid", detailMaterial, new Color(0.12f, 0.42f, 0.7f), new Vector3(0f, 0.62f, 0f), new Vector3(1.38f, 0.12f, 1.12f));
            CreateBox(trunkBench.transform, "Latch", detailMaterial, MetalTint, new Vector3(0f, 0.28f, 0.56f), new Vector3(0.14f, 0.2f, 0.04f));

            return decorRoot;
        }

        private static GameObject BuildBedroomWallPortrait(Transform root, Material woodMaterial, Material plasterMaterial, Material detailMaterial)
        {
            GameObject galleryRoot = new GameObject("BedroomPictureWall");
            galleryRoot.transform.SetParent(root, false);
            galleryRoot.transform.localPosition = Vector3.zero;

            CreateFramedPortrait(galleryRoot.transform, "PortraitMain", woodMaterial, new Vector3(-5.93f, 2.26f, -1.62f), Quaternion.Euler(0f, 90f, 0f), new Vector2(2.86f, 1.68f));
            CreateFramedPortrait(galleryRoot.transform, "PortraitUpper", woodMaterial, new Vector3(-5.93f, 3.18f, -3.18f), Quaternion.Euler(0f, 90f, 0f), new Vector2(1.58f, 1f));
            CreateFramedPortrait(galleryRoot.transform, "PortraitLower", woodMaterial, new Vector3(-5.93f, 1.36f, -3.06f), Quaternion.Euler(0f, 90f, 0f), new Vector2(1.42f, 0.92f));
            CreateFramedPortrait(galleryRoot.transform, "PortraitHintCandidate", woodMaterial, new Vector3(-5.93f, 2.18f, 0.3f), Quaternion.Euler(0f, 90f, 0f), new Vector2(1.84f, 1.16f));

            CreateBox(galleryRoot.transform, "GalleryWallTrim", detailMaterial, new Color(0.88f, 0.84f, 0.78f), new Vector3(-5.94f, 0.92f, -1.4f), new Vector3(0.06f, 0.1f, 5.9f));
            CreateBox(galleryRoot.transform, "GalleryWallSconce", plasterMaterial, new Color(0.96f, 0.9f, 0.76f), new Vector3(-5.9f, 3.82f, -1.32f), new Vector3(0.1f, 0.26f, 0.34f));

            return galleryRoot;
        }

        private static GameObject BuildBedroomDresser(Transform root, Material woodMaterial, Material plasterMaterial, Material detailMaterial)
        {
            GameObject dresserRoot = new GameObject("BedroomDresser");
            dresserRoot.transform.SetParent(root, false);
            dresserRoot.transform.localPosition = new Vector3(-5.22f, 0f, -1.5f);
            dresserRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            CreateBox(dresserRoot.transform, "Body", woodMaterial, new Color(0.54f, 0.35f, 0.2f), new Vector3(0f, 0.7f, 0f), new Vector3(2.62f, 1.4f, 0.72f));
            CreateBox(dresserRoot.transform, "Top", woodMaterial, new Color(0.61f, 0.42f, 0.24f), new Vector3(0f, 1.46f, 0f), new Vector3(2.76f, 0.12f, 0.84f));

            for (int row = 0; row < 3; row++)
            {
                float y = 1.1f - (row * 0.36f);
                CreateBox(dresserRoot.transform, $"DrawerLeft_{row}", woodMaterial, new Color(0.48f, 0.31f, 0.18f), new Vector3(-0.62f, y, 0.39f), new Vector3(0.92f, 0.24f, 0.05f));
                CreateBox(dresserRoot.transform, $"DrawerRight_{row}", woodMaterial, new Color(0.48f, 0.31f, 0.18f), new Vector3(0.62f, y, 0.39f), new Vector3(0.92f, 0.24f, 0.05f));
                CreateBox(dresserRoot.transform, $"HandleLeft_{row}", detailMaterial, MetalTint, new Vector3(-0.62f, y, 0.43f), new Vector3(0.16f, 0.04f, 0.03f));
                CreateBox(dresserRoot.transform, $"HandleRight_{row}", detailMaterial, MetalTint, new Vector3(0.62f, y, 0.43f), new Vector3(0.16f, 0.04f, 0.03f));
            }

            CreateBox(dresserRoot.transform, "Vase", plasterMaterial, new Color(0.84f, 0.86f, 0.9f), new Vector3(-0.78f, 1.76f, -0.06f), new Vector3(0.24f, 0.42f, 0.24f));
            CreateBox(dresserRoot.transform, "PlantStem", detailMaterial, new Color(0.25f, 0.55f, 0.27f), new Vector3(-0.78f, 2.04f, -0.06f), new Vector3(0.06f, 0.22f, 0.06f));
            CreateBox(dresserRoot.transform, "PlantLeafA", detailMaterial, new Color(0.31f, 0.68f, 0.3f), new Vector3(-0.86f, 2.18f, -0.06f), new Vector3(0.22f, 0.08f, 0.08f), Quaternion.Euler(0f, 0f, 28f));
            CreateBox(dresserRoot.transform, "PlantLeafB", detailMaterial, new Color(0.31f, 0.68f, 0.3f), new Vector3(-0.7f, 2.16f, -0.06f), new Vector3(0.22f, 0.08f, 0.08f), Quaternion.Euler(0f, 0f, -24f));
            CreateBox(dresserRoot.transform, "BookStackA", woodMaterial, new Color(0.15f, 0.44f, 0.68f), new Vector3(0.22f, 1.66f, -0.08f), new Vector3(0.36f, 0.08f, 0.28f));
            CreateBox(dresserRoot.transform, "BookStackB", woodMaterial, new Color(0.84f, 0.72f, 0.28f), new Vector3(0.22f, 1.76f, -0.08f), new Vector3(0.36f, 0.07f, 0.28f));
            CreateBox(dresserRoot.transform, "ShellBowl", plasterMaterial, new Color(0.93f, 0.88f, 0.8f), new Vector3(0.96f, 1.67f, -0.04f), new Vector3(0.32f, 0.08f, 0.24f));

            return dresserRoot;
        }

        private static void CreateFramedPortrait(Transform parent, string name, Material woodMaterial, Vector3 localPosition, Quaternion localRotation, Vector2 artSize)
        {
            GameObject frameRoot = new GameObject(name);
            frameRoot.transform.SetParent(parent, false);
            frameRoot.transform.localPosition = localPosition;
            frameRoot.transform.localRotation = localRotation;

            float outerWidth = artSize.x + 0.26f;
            float outerHeight = artSize.y + 0.26f;

            CreateBox(frameRoot.transform, "FrameBack", woodMaterial, new Color(0.24f, 0.17f, 0.1f), new Vector3(0f, 0f, -0.012f), new Vector3(outerWidth, outerHeight, 0.04f));
            CreateBox(frameRoot.transform, "FrameTop", woodMaterial, new Color(0.52f, 0.35f, 0.2f), new Vector3(0f, (outerHeight * 0.5f) - 0.06f, 0f), new Vector3(outerWidth + 0.08f, 0.12f, 0.1f));
            CreateBox(frameRoot.transform, "FrameBottom", woodMaterial, new Color(0.52f, 0.35f, 0.2f), new Vector3(0f, (-outerHeight * 0.5f) + 0.06f, 0f), new Vector3(outerWidth + 0.08f, 0.12f, 0.1f));
            CreateBox(frameRoot.transform, "FrameLeft", woodMaterial, new Color(0.46f, 0.31f, 0.18f), new Vector3((-outerWidth * 0.5f) + 0.06f, 0f, 0f), new Vector3(0.12f, outerHeight + 0.08f, 0.1f));
            CreateBox(frameRoot.transform, "FrameRight", woodMaterial, new Color(0.46f, 0.31f, 0.18f), new Vector3((outerWidth * 0.5f) - 0.06f, 0f, 0f), new Vector3(0.12f, outerHeight + 0.08f, 0.1f));

            GameObject portraitObject = IslandInteractionUtility.CreateMeshObject("PortraitImage", PrimitiveType.Quad, CreatePortraitMaterial(), frameRoot.transform);
            portraitObject.transform.localPosition = new Vector3(0f, 0f, 0.032f);
            portraitObject.transform.localRotation = Quaternion.identity;
            portraitObject.transform.localScale = new Vector3(artSize.x, artSize.y, 1f);
        }

        private static void BuildInteriorCeilingLightFixture(Transform root, Material detailMaterial, Material plasterMaterial)
        {
            GameObject fixtureRoot = new GameObject("InteriorLightFixture");
            fixtureRoot.transform.SetParent(root, false);
            fixtureRoot.transform.localPosition = new Vector3(0f, 4.06f, -1.22f);

            CreateBox(fixtureRoot.transform, "FixtureStem", detailMaterial, MetalTint, new Vector3(0f, 0.18f, 0f), new Vector3(0.06f, 0.32f, 0.06f));
            CreateBox(fixtureRoot.transform, "FixtureBar", detailMaterial, MetalTint, new Vector3(0f, -0.02f, 0f), new Vector3(0.64f, 0.04f, 0.14f));
            CreateBox(fixtureRoot.transform, "ShadeLeft", plasterMaterial, new Color(0.96f, 0.88f, 0.72f), new Vector3(-0.22f, -0.18f, 0f), new Vector3(0.26f, 0.22f, 0.26f));
            CreateBox(fixtureRoot.transform, "ShadeCenter", plasterMaterial, new Color(0.96f, 0.88f, 0.72f), new Vector3(0f, -0.24f, 0f), new Vector3(0.3f, 0.24f, 0.3f));
            CreateBox(fixtureRoot.transform, "ShadeRight", plasterMaterial, new Color(0.96f, 0.88f, 0.72f), new Vector3(0.22f, -0.18f, 0f), new Vector3(0.26f, 0.22f, 0.26f));
        }

        private static void BuildWallSwitchLight(Transform root, Material detailMaterial, Material plasterMaterial, Renderer[] roomRenderers, Color[] lightsOnColors, Color[] lightsOffColors)
        {
            GameObject lightRoot = new GameObject("InteriorLight");
            lightRoot.transform.SetParent(root, false);
            lightRoot.transform.localPosition = new Vector3(0f, 4.06f, -1.22f);

            Light pointLight = lightRoot.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.range = 11f;
            pointLight.intensity = 0f;
            pointLight.color = new Color(1f, 0.9f, 0.72f);
            pointLight.shadows = LightShadows.None;
            pointLight.shadowStrength = 0f;

            GameObject switchRoot = new GameObject("WallSwitch");
            switchRoot.transform.SetParent(root, false);
            switchRoot.transform.localPosition = new Vector3(3.18f, 1.36f, -4.96f);
            switchRoot.transform.localRotation = Quaternion.identity;

            CreateBox(switchRoot.transform, "SwitchPlate", plasterMaterial, new Color(0.86f, 0.84f, 0.8f), new Vector3(0f, 0f, 0f), new Vector3(0.24f, 0.38f, 0.06f));
            GameObject switchLever = CreateBox(switchRoot.transform, "SwitchLever", detailMaterial, new Color(0.33f, 0.3f, 0.27f), new Vector3(0f, 0f, 0.04f), new Vector3(0.08f, 0.18f, 0.05f));

            GameObject interactionObject = new GameObject("WallSwitchInteraction");
            interactionObject.transform.SetParent(switchRoot.transform, false);
            interactionObject.transform.localPosition = Vector3.zero;
            IslandWallSwitchLightInteraction lightInteraction = interactionObject.AddComponent<IslandWallSwitchLightInteraction>();
            lightInteraction.Configure(
                pointLight,
                switchLever.transform,
                switchLever.GetComponent<Renderer>(),
                roomRenderers,
                lightsOnColors,
                lightsOffColors,
                true,
                2.3f,
                1.25f);
        }

        private static Material CreatePortraitMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");

            Material material = new Material(shader)
            {
                name = "Bedroom Wall Portrait Material",
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };

            Texture2D portraitTexture = Resources.Load<Texture2D>(BedroomWallPortraitResourcePath);
            if (portraitTexture != null)
            {
                portraitTexture.filterMode = FilterMode.Trilinear;
                portraitTexture.anisoLevel = 8;
                portraitTexture.wrapMode = TextureWrapMode.Clamp;

                material.mainTexture = portraitTexture;
                material.SetTexture("_BaseMap", portraitTexture);
                material.SetTexture("_MainTex", portraitTexture);
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Smoothness", 0.02f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Cull", 0f);
            return material;
        }

        private static Transform CreateAnchor(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = localPosition;
            anchor.transform.localRotation = localRotation;
            return anchor.transform;
        }

        private static GameObject CreateSolidBox(Transform parent, string name, Material material, Color tint, Vector3 localPosition, Vector3 localScale)
        {
            return CreateSolidBox(parent, name, material, tint, localPosition, localScale, Quaternion.identity);
        }

        private static GameObject CreateSolidBox(Transform parent, string name, Material material, Color tint, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            GameObject part = CreateBox(parent, name, material, tint, localPosition, localScale, localRotation);
            BoxCollider collider = part.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            part.AddComponent<IslandSolidObstacle>();
            return part;
        }

        private static GameObject CreatePhysicalBox(Transform parent, string name, Material material, Color tint, Vector3 localPosition, Vector3 localScale)
        {
            return CreatePhysicalBox(parent, name, material, tint, localPosition, localScale, Quaternion.identity);
        }

        private static GameObject CreatePhysicalBox(Transform parent, string name, Material material, Color tint, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            GameObject part = CreateBox(parent, name, material, tint, localPosition, localScale, localRotation);
            BoxCollider collider = part.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            return part;
        }

        private static GameObject CreateBox(Transform parent, string name, Material material, Color tint, Vector3 localPosition, Vector3 localScale)
        {
            return CreateBox(parent, name, material, tint, localPosition, localScale, Quaternion.identity);
        }

        private static GameObject CreateBox(Transform parent, string name, Material material, Color tint, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            GameObject part = IslandInteractionUtility.CreateMeshObject(name, PrimitiveType.Cube, material, parent);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;
            IslandInteractionUtility.ApplyTint(part.GetComponent<Renderer>(), tint);
            return part;
        }

        private static void CreateCylinder(Transform parent, string name, Material material, Color tint, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            GameObject part = IslandInteractionUtility.CreateMeshObject(name, PrimitiveType.Cylinder, material, parent);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;
            IslandInteractionUtility.ApplyTint(part.GetComponent<Renderer>(), tint);
        }

        private static void CreateWalkableSurface(Transform parent, string name, Vector3 localCenter, Vector3 size)
        {
            GameObject surfaceObject = new GameObject(name);
            surfaceObject.transform.SetParent(parent, false);
            surfaceObject.transform.localPosition = localCenter;

            BoxCollider collider = surfaceObject.AddComponent<BoxCollider>();
            collider.size = size;
            surfaceObject.AddComponent<IslandWalkableSurface>();
        }
    }
}
