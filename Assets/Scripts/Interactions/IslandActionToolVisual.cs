using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandActionToolVisual : MonoBehaviour
    {
        public enum ToolKind
        {
            Axe,
            Pickaxe
        }

        private static IslandActionToolVisual instance;

        private Transform anchor;
        private GameObject activeTool;
        private ToolKind currentToolKind;

        public static IslandActionToolVisual GetOrCreate()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return null;
            }

            if (instance != null)
            {
                instance.AttachToCamera(camera.transform);
                return instance;
            }

            Transform existing = camera.transform.Find("Island Action Tool Visual");
            if (existing != null && existing.TryGetComponent(out instance))
            {
                instance.AttachToCamera(camera.transform);
                return instance;
            }

            GameObject root = new GameObject("Island Action Tool Visual");
            instance = root.AddComponent<IslandActionToolVisual>();
            instance.AttachToCamera(camera.transform);
            return instance;
        }

        public void ShowTool(ToolKind toolKind)
        {
            AttachToCamera(Camera.main != null ? Camera.main.transform : null);
            if (anchor == null)
            {
                return;
            }

            HideTool();

            activeTool = new GameObject(toolKind == ToolKind.Axe ? "AxeTool" : "PickaxeTool");
            activeTool.transform.SetParent(anchor, false);
            activeTool.transform.localPosition = new Vector3(0.38f, -0.3f, 0.6f);
            activeTool.transform.localRotation = Quaternion.Euler(18f, -36f, -26f);
            activeTool.transform.localScale = Vector3.one;
            currentToolKind = toolKind;

            Material handleMaterial = IslandInteractionUtility.CreateLitMaterial("ActionTool_Handle", new Color(0.47f, 0.31f, 0.17f), 0.12f);
            Material metalMaterial = IslandInteractionUtility.CreateLitMaterial("ActionTool_Metal", new Color(0.58f, 0.62f, 0.67f), 0.32f);
            Material strapMaterial = IslandInteractionUtility.CreateLitMaterial("ActionTool_Strap", new Color(0.14f, 0.12f, 0.1f), 0.08f);

            GameObject handle = IslandInteractionUtility.CreateMeshObject("Handle", PrimitiveType.Cylinder, handleMaterial, activeTool.transform);
            handle.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            handle.transform.localScale = new Vector3(0.045f, 0.42f, 0.045f);

            GameObject grip = IslandInteractionUtility.CreateMeshObject("Grip", PrimitiveType.Cylinder, strapMaterial, activeTool.transform);
            grip.transform.localPosition = new Vector3(-0.01f, -0.34f, 0f);
            grip.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            grip.transform.localScale = new Vector3(0.05f, 0.11f, 0.05f);

            if (toolKind == ToolKind.Axe)
            {
                GameObject blade = IslandInteractionUtility.CreateMeshObject("Blade", PrimitiveType.Cube, metalMaterial, activeTool.transform);
                blade.transform.localPosition = new Vector3(0.1f, 0.34f, 0f);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
                blade.transform.localScale = new Vector3(0.2f, 0.16f, 0.04f);

                GameObject bladeEdge = IslandInteractionUtility.CreateMeshObject("BladeEdge", PrimitiveType.Cube, metalMaterial, activeTool.transform);
                bladeEdge.transform.localPosition = new Vector3(0.18f, 0.33f, 0f);
                bladeEdge.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
                bladeEdge.transform.localScale = new Vector3(0.11f, 0.1f, 0.035f);
            }
            else
            {
                GameObject head = IslandInteractionUtility.CreateMeshObject("Head", PrimitiveType.Cube, metalMaterial, activeTool.transform);
                head.transform.localPosition = new Vector3(0f, 0.34f, 0f);
                head.transform.localScale = new Vector3(0.25f, 0.05f, 0.06f);

                GameObject pointA = IslandInteractionUtility.CreateMeshObject("PointA", PrimitiveType.Cube, metalMaterial, activeTool.transform);
                pointA.transform.localPosition = new Vector3(-0.14f, 0.34f, 0f);
                pointA.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
                pointA.transform.localScale = new Vector3(0.12f, 0.035f, 0.035f);

                GameObject pointB = IslandInteractionUtility.CreateMeshObject("PointB", PrimitiveType.Cube, metalMaterial, activeTool.transform);
                pointB.transform.localPosition = new Vector3(0.14f, 0.34f, 0f);
                pointB.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
                pointB.transform.localScale = new Vector3(0.12f, 0.035f, 0.035f);
            }
        }

        public void UpdateSwing(float phase, float speedMultiplier = 1f)
        {
            if (activeTool == null)
            {
                return;
            }

            phase = Mathf.Repeat(phase * Mathf.Max(0.5f, speedMultiplier), 1f);
            float strikeProgress;
            if (phase < 0.34f)
            {
                strikeProgress = Mathf.SmoothStep(0f, 0.26f, phase / 0.34f);
            }
            else if (phase < 0.58f)
            {
                strikeProgress = Mathf.SmoothStep(0.26f, 1f, (phase - 0.34f) / 0.24f);
            }
            else
            {
                strikeProgress = Mathf.SmoothStep(1f, 0f, (phase - 0.58f) / 0.42f);
            }

            Vector3 startPosition = new Vector3(0.38f, -0.3f, 0.6f);
            Vector3 hitPosition = currentToolKind == ToolKind.Axe
                ? new Vector3(0.02f, -0.08f, 0.2f)
                : new Vector3(0.01f, -0.05f, 0.18f);

            Vector3 startRotation = new Vector3(18f, -36f, -26f);
            Vector3 hitRotation = currentToolKind == ToolKind.Axe
                ? new Vector3(96f, -2f, -84f)
                : new Vector3(108f, 2f, -82f);

            activeTool.transform.localPosition = Vector3.Lerp(startPosition, hitPosition, strikeProgress);
            activeTool.transform.localRotation = Quaternion.Euler(Vector3.Lerp(startRotation, hitRotation, strikeProgress));
        }

        public void HideTool()
        {
            if (activeTool == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(activeTool);
            }
            else
            {
                DestroyImmediate(activeTool);
            }

            activeTool = null;
        }

        private void AttachToCamera(Transform cameraTransform)
        {
            if (cameraTransform == null)
            {
                return;
            }

            anchor = cameraTransform;
            transform.SetParent(cameraTransform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
