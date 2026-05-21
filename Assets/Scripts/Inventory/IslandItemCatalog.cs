using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrivateIsland
{
    public static class IslandItemCatalog
    {
        public const string FlowerId = "flower";
        public const string RockId = "rock";
        public const string CoconutId = "coconut";
        public const string WoodId = "wood";
        public const string MapId = "treasure_map";
        public const string HiddenNoteId = "hidden_note";
        public const string CompassId = "compass";
        public const string TorchId = "torch";
        public const string CanteenId = "canteen";

        private static readonly Dictionary<string, ItemDefinition> Definitions = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [FlowerId] = new ItemDefinition(FlowerId, "Flower", new Color(0.91f, 0.44f, 0.64f), new Color(1f, 0.89f, 0.36f), new Color(0.29f, 0.64f, 0.28f), ItemVisualKind.Flower),
            [RockId] = new ItemDefinition(RockId, "Rock", new Color(0.52f, 0.53f, 0.57f), new Color(0.76f, 0.77f, 0.8f), new Color(0.33f, 0.34f, 0.38f), ItemVisualKind.Rock),
            [CoconutId] = new ItemDefinition(CoconutId, "Coconut", new Color(0.45f, 0.28f, 0.15f), new Color(0.62f, 0.44f, 0.25f), new Color(0.18f, 0.1f, 0.06f), ItemVisualKind.Coconut),
            [WoodId] = new ItemDefinition(WoodId, "Wood", new Color(0.46f, 0.3f, 0.16f), new Color(0.75f, 0.56f, 0.34f), new Color(0.24f, 0.14f, 0.08f), ItemVisualKind.Wood),
            [MapId] = new ItemDefinition(MapId, "Treasure Map", new Color(0.93f, 0.85f, 0.58f), new Color(0.85f, 0.2f, 0.18f), new Color(0.57f, 0.39f, 0.18f), ItemVisualKind.Map),
            [HiddenNoteId] = new ItemDefinition(HiddenNoteId, "Hidden Note", new Color(0.94f, 0.89f, 0.72f), new Color(0.67f, 0.5f, 0.24f), new Color(0.34f, 0.22f, 0.11f), ItemVisualKind.Map),
            [CompassId] = new ItemDefinition(CompassId, "Compass", new Color(0.22f, 0.45f, 0.73f), new Color(0.91f, 0.29f, 0.23f), new Color(0.88f, 0.88f, 0.9f), ItemVisualKind.Compass),
            [TorchId] = new ItemDefinition(TorchId, "Torch", new Color(0.58f, 0.35f, 0.14f), new Color(1f, 0.62f, 0.18f), new Color(1f, 0.92f, 0.54f), ItemVisualKind.Torch),
            [CanteenId] = new ItemDefinition(CanteenId, "Canteen", new Color(0.18f, 0.54f, 0.68f), new Color(0.74f, 0.87f, 0.95f), new Color(0.11f, 0.16f, 0.2f), ItemVisualKind.Canteen)
        };

        private static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private const string RockAssetResourcePath = "Nature/Rock_Medium_1";

        public static bool TryGetDefinition(string itemId, out ItemDefinition definition)
        {
            return Definitions.TryGetValue(itemId ?? string.Empty, out definition);
        }

        public static ItemDefinition GetDefinition(string itemId)
        {
            if (TryGetDefinition(itemId, out ItemDefinition definition))
            {
                return definition;
            }

            return Definitions[RockId];
        }

        public static Sprite GetIcon(string itemId)
        {
            if (IconCache.TryGetValue(itemId, out Sprite icon) && icon != null)
            {
                return icon;
            }

            ItemDefinition definition = GetDefinition(itemId);
            icon = CreateIcon(definition);
            IconCache[itemId] = icon;
            return icon;
        }

        public static string GetDisplayName(string itemId)
        {
            return GetDefinition(itemId).DisplayName;
        }

        public static void BuildWorldVisual(string itemId, Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            ItemDefinition definition = GetDefinition(itemId);
            switch (definition.VisualKind)
            {
                case ItemVisualKind.Flower:
                    BuildFlower(parent, definition);
                    break;
                case ItemVisualKind.Rock:
                    BuildRock(parent, definition);
                    break;
                case ItemVisualKind.Coconut:
                    BuildCoconut(parent, definition);
                    break;
                case ItemVisualKind.Wood:
                    BuildWood(parent, definition);
                    break;
                case ItemVisualKind.Map:
                    BuildMap(parent, definition);
                    break;
                case ItemVisualKind.Compass:
                    BuildCompass(parent, definition);
                    break;
                case ItemVisualKind.Torch:
                    BuildTorch(parent, definition);
                    break;
                case ItemVisualKind.Canteen:
                    BuildCanteen(parent, definition);
                    break;
            }
        }

        private static Sprite CreateIcon(ItemDefinition definition)
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = $"{definition.DisplayName} Icon"
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            texture.SetPixels(pixels);

            switch (definition.VisualKind)
            {
                case ItemVisualKind.Flower:
                    FillCircle(texture, 22, 36, 8, definition.PrimaryColor);
                    FillCircle(texture, 42, 36, 8, definition.PrimaryColor);
                    FillCircle(texture, 32, 46, 8, definition.PrimaryColor);
                    FillCircle(texture, 32, 26, 8, definition.PrimaryColor);
                    FillCircle(texture, 32, 36, 7, definition.SecondaryColor);
                    DrawLine(texture, 32, 7, 32, 26, 4, definition.AccentColor);
                    DrawLine(texture, 32, 17, 20, 23, 3, definition.AccentColor * 0.96f);
                    DrawLine(texture, 32, 16, 43, 22, 3, definition.AccentColor * 0.96f);
                    break;
                case ItemVisualKind.Rock:
                    FillEllipse(texture, 32, 31, 18, 13, definition.PrimaryColor);
                    FillEllipse(texture, 24, 36, 11, 9, definition.PrimaryColor * 0.92f);
                    FillEllipse(texture, 40, 36, 12, 10, definition.PrimaryColor * 0.84f);
                    FillEllipse(texture, 28, 36, 6, 4, definition.SecondaryColor);
                    FillEllipse(texture, 37, 31, 4, 3, definition.SecondaryColor);
                    break;
                case ItemVisualKind.Coconut:
                    FillEllipse(texture, 32, 31, 15, 17, definition.PrimaryColor);
                    FillEllipse(texture, 30, 36, 11, 9, definition.SecondaryColor * 0.85f);
                    FillCircle(texture, 26, 22, 2, definition.AccentColor);
                    FillCircle(texture, 32, 20, 2, definition.AccentColor);
                    FillCircle(texture, 38, 22, 2, definition.AccentColor);
                    break;
                case ItemVisualKind.Wood:
                    FillRect(texture, 12, 24, 40, 16, definition.PrimaryColor);
                    FillCircle(texture, 12, 32, 8, definition.SecondaryColor);
                    FillCircle(texture, 52, 32, 8, definition.SecondaryColor);
                    FillCircle(texture, 52, 32, 4, definition.AccentColor);
                    DrawLine(texture, 18, 26, 45, 26, 2, definition.AccentColor);
                    DrawLine(texture, 18, 38, 45, 38, 2, definition.AccentColor);
                    break;
                case ItemVisualKind.Map:
                    FillRect(texture, 14, 14, 36, 36, definition.PrimaryColor);
                    DrawLine(texture, 20, 22, 26, 30, 3, definition.SecondaryColor);
                    DrawLine(texture, 26, 22, 20, 30, 3, definition.SecondaryColor);
                    DrawLine(texture, 30, 18, 30, 46, 2, definition.AccentColor * 0.72f);
                    DrawLine(texture, 41, 16, 41, 48, 2, definition.AccentColor * 0.72f);
                    DrawLine(texture, 14, 14, 50, 14, 2, definition.AccentColor * 0.72f);
                    DrawLine(texture, 14, 50, 50, 50, 2, definition.AccentColor * 0.72f);
                    break;
                case ItemVisualKind.Compass:
                    FillCircle(texture, 32, 32, 18, definition.PrimaryColor);
                    FillCircle(texture, 32, 32, 12, definition.AccentColor);
                    DrawLine(texture, 32, 44, 32, 20, 3, definition.SecondaryColor);
                    DrawLine(texture, 32, 20, 24, 32, 3, definition.SecondaryColor);
                    DrawLine(texture, 32, 20, 40, 32, 3, definition.SecondaryColor);
                    DrawLine(texture, 32, 22, 32, 44, 2, Color.white);
                    break;
                case ItemVisualKind.Torch:
                    DrawLine(texture, 21, 14, 38, 40, 5, definition.PrimaryColor);
                    FillCircle(texture, 42, 43, 8, definition.SecondaryColor);
                    FillCircle(texture, 44, 46, 4, definition.AccentColor);
                    break;
                case ItemVisualKind.Canteen:
                    FillEllipse(texture, 32, 32, 14, 18, definition.PrimaryColor);
                    FillRect(texture, 26, 42, 12, 8, definition.SecondaryColor);
                    FillRect(texture, 28, 50, 8, 6, definition.AccentColor);
                    FillEllipse(texture, 29, 35, 4, 7, Color.Lerp(definition.PrimaryColor, Color.white, 0.28f));
                    break;
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 64f);
        }

        private static void BuildFlower(Transform parent, ItemDefinition definition)
        {
            Material stemMaterial = GetMaterial($"{definition.Id}_stem", definition.AccentColor, 0.08f);
            Material petalMaterial = GetMaterial($"{definition.Id}_petal", definition.PrimaryColor, 0.18f);
            Material centerMaterial = GetMaterial($"{definition.Id}_center", definition.SecondaryColor, 0.22f);

            CreatePart(parent, "Stem", PrimitiveType.Cylinder, stemMaterial, new Vector3(0f, 0.22f, 0f), Vector3.zero, new Vector3(0.06f, 0.23f, 0.06f));
            CreatePart(parent, "LeafLeft", PrimitiveType.Capsule, stemMaterial, new Vector3(-0.07f, 0.17f, 0f), new Vector3(-28f, 0f, 26f), new Vector3(0.05f, 0.12f, 0.03f));
            CreatePart(parent, "LeafRight", PrimitiveType.Capsule, stemMaterial, new Vector3(0.07f, 0.15f, 0f), new Vector3(20f, 0f, -24f), new Vector3(0.05f, 0.12f, 0.03f));
            CreatePart(parent, "Center", PrimitiveType.Sphere, centerMaterial, new Vector3(0f, 0.42f, 0f), Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f));

            Vector3[] petalOffsets =
            {
                new Vector3(-0.11f, 0.42f, 0f),
                new Vector3(0.11f, 0.42f, 0f),
                new Vector3(0f, 0.53f, 0f),
                new Vector3(0f, 0.31f, 0f),
                new Vector3(-0.08f, 0.5f, 0f),
                new Vector3(0.08f, 0.5f, 0f)
            };

            for (int i = 0; i < petalOffsets.Length; i++)
            {
                CreatePart(parent, $"Petal_{i}", PrimitiveType.Sphere, petalMaterial, petalOffsets[i], Vector3.zero, new Vector3(0.11f, 0.11f, 0.08f));
            }
        }

        private static void BuildRock(Transform parent, ItemDefinition definition)
        {
            if (TryBuildRockFromNatureAsset(parent))
            {
                return;
            }

            Material stoneMaterial = GetMaterial($"{definition.Id}_stone", definition.PrimaryColor, 0.06f);
            Material brightFacet = GetMaterial($"{definition.Id}_facet", definition.SecondaryColor, 0.08f);
            Material darkFacet = GetMaterial($"{definition.Id}_shadow", definition.AccentColor, 0.04f);

            CreatePart(parent, "PileBase", PrimitiveType.Cube, darkFacet, new Vector3(0f, 0.055f, 0f), new Vector3(0f, 18f, 0f), new Vector3(0.36f, 0.08f, 0.28f));
            CreatePart(parent, "StoneA", PrimitiveType.Cube, stoneMaterial, new Vector3(-0.13f, 0.1f, -0.05f), new Vector3(8f, 24f, -6f), new Vector3(0.16f, 0.12f, 0.12f));
            CreatePart(parent, "StoneB", PrimitiveType.Cube, stoneMaterial, new Vector3(0.08f, 0.11f, 0.03f), new Vector3(-6f, 38f, 8f), new Vector3(0.15f, 0.11f, 0.11f));
            CreatePart(parent, "StoneC", PrimitiveType.Cube, brightFacet, new Vector3(0.02f, 0.14f, -0.09f), new Vector3(14f, 12f, 16f), new Vector3(0.12f, 0.09f, 0.09f));
            CreatePart(parent, "StoneD", PrimitiveType.Cube, darkFacet, new Vector3(0.15f, 0.09f, 0.11f), new Vector3(-10f, 30f, -12f), new Vector3(0.11f, 0.08f, 0.09f));
            CreatePart(parent, "StoneE", PrimitiveType.Cube, brightFacet, new Vector3(-0.02f, 0.12f, 0.1f), new Vector3(12f, 44f, -4f), new Vector3(0.1f, 0.08f, 0.08f));
            CreatePart(parent, "StoneF", PrimitiveType.Cube, stoneMaterial, new Vector3(-0.17f, 0.08f, 0.08f), new Vector3(-8f, 16f, 10f), new Vector3(0.1f, 0.07f, 0.09f));
        }

        private static bool TryBuildRockFromNatureAsset(Transform parent)
        {
            GameObject rockPrefab = Resources.Load<GameObject>(RockAssetResourcePath);
            if (rockPrefab == null)
            {
                return false;
            }

            CreateRockChunk(parent, rockPrefab, "RockChunkA", new Vector3(-0.12f, 0.055f, -0.02f), new Vector3(-4f, 18f, 6f), new Vector3(0.11f, 0.11f, 0.11f));
            CreateRockChunk(parent, rockPrefab, "RockChunkB", new Vector3(0.08f, 0.06f, 0.04f), new Vector3(3f, -28f, -5f), new Vector3(0.1f, 0.1f, 0.1f));
            CreateRockChunk(parent, rockPrefab, "RockChunkC", new Vector3(0.01f, 0.08f, -0.08f), new Vector3(8f, 42f, 10f), new Vector3(0.08f, 0.08f, 0.08f));
            CreateRockChunk(parent, rockPrefab, "RockChunkD", new Vector3(-0.04f, 0.045f, 0.11f), new Vector3(-2f, 64f, -6f), new Vector3(0.072f, 0.072f, 0.072f));
            return true;
        }

        private static void CreateRockChunk(Transform parent, GameObject prefab, string name, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            GameObject chunk = UnityEngine.Object.Instantiate(prefab, parent, false);
            chunk.name = name;
            chunk.transform.localPosition = localPosition;
            chunk.transform.localRotation = Quaternion.Euler(localEulerAngles);
            chunk.transform.localScale = localScale;
            StripPhysicsComponents(chunk);
        }

        private static void StripPhysicsComponents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(colliders[i]);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(colliders[i]);
                }
            }

            Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(rigidbodies[i]);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(rigidbodies[i]);
                }
            }
        }

        private static void BuildCoconut(Transform parent, ItemDefinition definition)
        {
            Material shellMaterial = GetMaterial($"{definition.Id}_shell", definition.PrimaryColor, 0.12f);
            Material fiberMaterial = GetMaterial($"{definition.Id}_fiber", definition.SecondaryColor, 0.06f);
            Material markMaterial = GetMaterial($"{definition.Id}_mark", new Color(0.08f, 0.05f, 0.03f), 0.02f);

            CreatePart(parent, "Shell", PrimitiveType.Sphere, shellMaterial, new Vector3(0f, 0.24f, 0f), new Vector3(8f, 0f, -12f), new Vector3(0.35f, 0.42f, 0.35f));
            CreatePart(parent, "Fiber", PrimitiveType.Sphere, fiberMaterial, new Vector3(0f, 0.24f, 0.03f), new Vector3(0f, 0f, 18f), new Vector3(0.25f, 0.16f, 0.22f));
            CreatePart(parent, "EyeA", PrimitiveType.Sphere, markMaterial, new Vector3(-0.09f, 0.11f, 0.24f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.03f));
            CreatePart(parent, "EyeB", PrimitiveType.Sphere, markMaterial, new Vector3(0f, 0.085f, 0.25f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.03f));
            CreatePart(parent, "EyeC", PrimitiveType.Sphere, markMaterial, new Vector3(0.09f, 0.11f, 0.24f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.03f));
        }

        private static void BuildWood(Transform parent, ItemDefinition definition)
        {
            Material barkMaterial = GetMaterial($"{definition.Id}_bark", definition.PrimaryColor, 0.08f);
            Material ringMaterial = GetMaterial($"{definition.Id}_ring", definition.SecondaryColor, 0.12f);
            Material darkRingMaterial = GetMaterial($"{definition.Id}_ring_dark", definition.AccentColor, 0.08f);

            CreatePart(parent, "Log", PrimitiveType.Cylinder, barkMaterial, new Vector3(0f, 0.17f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.16f, 0.38f, 0.16f));
            CreatePart(parent, "EndLeft", PrimitiveType.Cylinder, ringMaterial, new Vector3(-0.39f, 0.17f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.17f, 0.018f, 0.17f));
            CreatePart(parent, "EndRight", PrimitiveType.Cylinder, ringMaterial, new Vector3(0.39f, 0.17f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.17f, 0.018f, 0.17f));
            CreatePart(parent, "RingLeft", PrimitiveType.Cylinder, darkRingMaterial, new Vector3(-0.39f, 0.17f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.08f, 0.021f, 0.08f));
            CreatePart(parent, "RingRight", PrimitiveType.Cylinder, darkRingMaterial, new Vector3(0.39f, 0.17f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.08f, 0.021f, 0.08f));
        }

        private static void BuildMap(Transform parent, ItemDefinition definition)
        {
            Material paperMaterial = GetMaterial($"{definition.Id}_paper", definition.PrimaryColor, 0.08f);
            Material accentMaterial = GetMaterial($"{definition.Id}_accent", definition.SecondaryColor, 0.12f);
            Material inkMaterial = GetMaterial($"{definition.Id}_ink", definition.AccentColor, 0.04f);

            CreatePart(parent, "Sheet", PrimitiveType.Cube, paperMaterial, new Vector3(0f, 0.18f, 0f), new Vector3(10f, 18f, -4f), new Vector3(0.46f, 0.03f, 0.34f));
            CreatePart(parent, "RollLeft", PrimitiveType.Cylinder, accentMaterial, new Vector3(-0.2f, 0.2f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.05f, 0.17f, 0.05f));
            CreatePart(parent, "RollRight", PrimitiveType.Cylinder, accentMaterial, new Vector3(0.2f, 0.16f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.05f, 0.17f, 0.05f));
            CreatePart(parent, "CrossA", PrimitiveType.Cube, inkMaterial, new Vector3(-0.04f, 0.21f, 0.03f), new Vector3(10f, 18f, 45f), new Vector3(0.14f, 0.012f, 0.03f));
            CreatePart(parent, "CrossB", PrimitiveType.Cube, inkMaterial, new Vector3(-0.04f, 0.21f, 0.03f), new Vector3(10f, 18f, -45f), new Vector3(0.14f, 0.012f, 0.03f));
            CreatePart(parent, "Trail", PrimitiveType.Cube, inkMaterial, new Vector3(0.09f, 0.205f, -0.03f), new Vector3(8f, 18f, -12f), new Vector3(0.16f, 0.01f, 0.02f));
        }

        private static void BuildCompass(Transform parent, ItemDefinition definition)
        {
            Material rimMaterial = GetMaterial($"{definition.Id}_rim", definition.PrimaryColor, 0.24f);
            Material faceMaterial = GetMaterial($"{definition.Id}_face", definition.AccentColor, 0.08f);
            Material needleMaterial = GetMaterial($"{definition.Id}_needle", definition.SecondaryColor, 0.18f);

            CreatePart(parent, "Rim", PrimitiveType.Cylinder, rimMaterial, new Vector3(0f, 0.08f, 0f), Vector3.zero, new Vector3(0.34f, 0.05f, 0.34f));
            CreatePart(parent, "Face", PrimitiveType.Cylinder, faceMaterial, new Vector3(0f, 0.11f, 0f), Vector3.zero, new Vector3(0.26f, 0.02f, 0.26f));
            CreatePart(parent, "NeedleNorth", PrimitiveType.Cube, needleMaterial, new Vector3(0f, 0.13f, 0.08f), new Vector3(0f, 20f, 0f), new Vector3(0.04f, 0.015f, 0.18f));
            CreatePart(parent, "NeedleSouth", PrimitiveType.Cube, GetMaterial($"{definition.Id}_needle_light", Color.white, 0.18f), new Vector3(0f, 0.125f, -0.08f), new Vector3(0f, 20f, 0f), new Vector3(0.035f, 0.012f, 0.16f));
        }

        private static void BuildTorch(Transform parent, ItemDefinition definition)
        {
            Material handleMaterial = GetMaterial($"{definition.Id}_handle", definition.PrimaryColor, 0.08f);
            Material flameMaterial = GetMaterial($"{definition.Id}_flame", definition.SecondaryColor, 0.18f);
            Material emberMaterial = GetMaterial($"{definition.Id}_ember", definition.AccentColor, 0.22f);

            CreatePart(parent, "Handle", PrimitiveType.Cylinder, handleMaterial, new Vector3(-0.04f, 0.18f, 0f), new Vector3(0f, 0f, 22f), new Vector3(0.05f, 0.32f, 0.05f));
            CreatePart(parent, "Wrap", PrimitiveType.Cylinder, emberMaterial, new Vector3(0.02f, 0.4f, 0f), new Vector3(0f, 0f, 22f), new Vector3(0.06f, 0.08f, 0.06f));
            CreatePart(parent, "FlameOuter", PrimitiveType.Sphere, flameMaterial, new Vector3(0.12f, 0.56f, 0f), Vector3.zero, new Vector3(0.16f, 0.24f, 0.16f));
            CreatePart(parent, "FlameCore", PrimitiveType.Sphere, emberMaterial, new Vector3(0.12f, 0.59f, 0f), Vector3.zero, new Vector3(0.08f, 0.14f, 0.08f));
        }

        private static void BuildCanteen(Transform parent, ItemDefinition definition)
        {
            Material bodyMaterial = GetMaterial($"{definition.Id}_body", definition.PrimaryColor, 0.18f);
            Material capMaterial = GetMaterial($"{definition.Id}_cap", definition.AccentColor, 0.06f);
            Material strapMaterial = GetMaterial($"{definition.Id}_strap", definition.SecondaryColor, 0.12f);

            CreatePart(parent, "Body", PrimitiveType.Sphere, bodyMaterial, new Vector3(0f, 0.26f, 0f), new Vector3(0f, 0f, 8f), new Vector3(0.3f, 0.38f, 0.24f));
            CreatePart(parent, "Cap", PrimitiveType.Cylinder, capMaterial, new Vector3(0f, 0.49f, 0f), Vector3.zero, new Vector3(0.09f, 0.05f, 0.09f));
            CreatePart(parent, "StrapLeft", PrimitiveType.Cylinder, strapMaterial, new Vector3(-0.14f, 0.35f, 0f), new Vector3(0f, 0f, 28f), new Vector3(0.02f, 0.12f, 0.02f));
            CreatePart(parent, "StrapRight", PrimitiveType.Cylinder, strapMaterial, new Vector3(0.14f, 0.35f, 0f), new Vector3(0f, 0f, -28f), new Vector3(0.02f, 0.12f, 0.02f));
        }

        private static GameObject CreatePart(Transform parent, string name, PrimitiveType primitiveType, Material material, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            GameObject part = IslandInteractionUtility.CreateMeshObject(name, primitiveType, material, parent);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEulerAngles);
            part.transform.localScale = localScale;
            return part;
        }

        private static Material GetMaterial(string key, Color color, float smoothness)
        {
            if (MaterialCache.TryGetValue(key, out Material material) && material != null)
            {
                return material;
            }

            material = IslandInteractionUtility.CreateLitMaterial($"Item {key}", color, smoothness);
            MaterialCache[key] = material;
            return material;
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int px = x; px < x + width; px++)
            {
                for (int py = y; py < y + height; py++)
                {
                    SetPixel(texture, px, py, color);
                }
            }
        }

        private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            int radiusSquared = radius * radius;
            for (int px = centerX - radius; px <= centerX + radius; px++)
            {
                for (int py = centerY - radius; py <= centerY + radius; py++)
                {
                    int dx = px - centerX;
                    int dy = py - centerY;
                    if ((dx * dx) + (dy * dy) <= radiusSquared)
                    {
                        SetPixel(texture, px, py, color);
                    }
                }
            }
        }

        private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            float radiusXSquared = radiusX * radiusX;
            float radiusYSquared = radiusY * radiusY;
            for (int px = centerX - radiusX; px <= centerX + radiusX; px++)
            {
                for (int py = centerY - radiusY; py <= centerY + radiusY; py++)
                {
                    float dx = px - centerX;
                    float dy = py - centerY;
                    if (((dx * dx) / radiusXSquared) + ((dy * dy) / radiusYSquared) <= 1f)
                    {
                        SetPixel(texture, px, py, color);
                    }
                }
            }
        }

        private static void DrawLine(Texture2D texture, int startX, int startY, int endX, int endY, int thickness, Color color)
        {
            int dx = Mathf.Abs(endX - startX);
            int dy = Mathf.Abs(endY - startY);
            int sx = startX < endX ? 1 : -1;
            int sy = startY < endY ? 1 : -1;
            int error = dx - dy;
            int halfThickness = Mathf.Max(1, thickness) / 2;

            while (true)
            {
                for (int offsetX = -halfThickness; offsetX <= halfThickness; offsetX++)
                {
                    for (int offsetY = -halfThickness; offsetY <= halfThickness; offsetY++)
                    {
                        SetPixel(texture, startX + offsetX, startY + offsetY, color);
                    }
                }

                if (startX == endX && startY == endY)
                {
                    break;
                }

                int doubleError = error * 2;
                if (doubleError > -dy)
                {
                    error -= dy;
                    startX += sx;
                }

                if (doubleError < dx)
                {
                    error += dx;
                    startY += sy;
                }
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        public readonly struct ItemDefinition
        {
            public ItemDefinition(string id, string displayName, Color primaryColor, Color secondaryColor, Color accentColor, ItemVisualKind visualKind)
            {
                Id = id;
                DisplayName = displayName;
                PrimaryColor = primaryColor;
                SecondaryColor = secondaryColor;
                AccentColor = accentColor;
                VisualKind = visualKind;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public Color PrimaryColor { get; }
            public Color SecondaryColor { get; }
            public Color AccentColor { get; }
            public ItemVisualKind VisualKind { get; }
        }

        public enum ItemVisualKind
        {
            Flower,
            Rock,
            Coconut,
            Wood,
            Map,
            Compass,
            Torch,
            Canteen
        }
    }
}
