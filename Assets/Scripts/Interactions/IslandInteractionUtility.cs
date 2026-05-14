using UnityEngine;
using UnityEngine.Rendering;

namespace PrivateIsland
{
    internal static class IslandInteractionUtility
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static GameObject CreateMeshObject(string objectName, PrimitiveType primitiveType, Material material, Transform parent = null)
        {
            GameObject gameObject = new GameObject(objectName);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = GetPrimitiveMesh(primitiveType);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return gameObject;
        }

        public static Material CreateLitMaterial(string materialName, Color color, float smoothness = 0.12f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");

            Material material = new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };

            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            return material;
        }

        public static void ApplyTint(Renderer renderer, Color tint)
        {
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

        public static Color ResolveRendererColor(Renderer renderer, Color fallback)
        {
            if (renderer == null)
            {
                return fallback;
            }

            Material material = renderer.sharedMaterial;
            if (material == null)
            {
                return fallback;
            }

            if (material.HasProperty(BaseColorId))
            {
                return material.GetColor(BaseColorId);
            }

            if (material.HasProperty(ColorId))
            {
                return material.GetColor(ColorId);
            }

            return fallback;
        }

        public static bool TryGetCompositeBounds(Transform root, out Bounds bounds)
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

        public static Mesh GetPrimitiveMesh(PrimitiveType primitiveType)
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

            GameObject temporary = GameObject.CreatePrimitive(primitiveType);
            Mesh mesh = temporary.GetComponent<MeshFilter>().sharedMesh;

            if (Application.isPlaying)
            {
                Object.Destroy(temporary);
            }
            else
            {
                Object.DestroyImmediate(temporary);
            }

            return mesh;
        }
    }
}
