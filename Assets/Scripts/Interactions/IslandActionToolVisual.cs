using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandActionToolVisual : MonoBehaviour
    {
        public enum ToolKind
        {
            Hand,
            Axe,
            Pickaxe
        }

        private static IslandActionToolVisual instance;

        private Transform anchor;
        private GameObject activeTool;
        private Coroutine activeOneShotRoutine;
        private ToolKind currentToolKind;
        private Vector3 aimTargetWorld;
        private bool hasAimTarget;

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
            ShowTool(toolKind, Vector3.zero, false);
        }

        public void ShowTool(ToolKind toolKind, Vector3 targetWorldPosition)
        {
            ShowTool(toolKind, targetWorldPosition, true);
        }

        public void SetAimTarget(Vector3 targetWorldPosition)
        {
            aimTargetWorld = hasAimTarget
                ? Vector3.Lerp(aimTargetWorld, targetWorldPosition, 0.42f)
                : targetWorldPosition;
            hasAimTarget = true;
        }

        public void PlayOneShot(ToolKind toolKind, Vector3 targetWorldPosition, float duration = 0.22f)
        {
            AttachToCamera(Camera.main != null ? Camera.main.transform : null);
            if (anchor == null || !Application.isPlaying)
            {
                return;
            }

            if (activeOneShotRoutine != null)
            {
                StopCoroutine(activeOneShotRoutine);
                activeOneShotRoutine = null;
            }

            activeOneShotRoutine = StartCoroutine(OneShotRoutine(toolKind, targetWorldPosition, duration));
        }

        private void ShowTool(ToolKind toolKind, Vector3 targetWorldPosition, bool shouldTrackTarget)
        {
            AttachToCamera(Camera.main != null ? Camera.main.transform : null);
            if (anchor == null)
            {
                return;
            }

            HideTool();

            activeTool = new GameObject(toolKind switch
            {
                ToolKind.Axe => "AxeTool",
                ToolKind.Pickaxe => "PickaxeTool",
                _ => "HandAction"
            });
            activeTool.transform.SetParent(anchor, false);
            activeTool.transform.localPosition = new Vector3(0.28f, -0.36f, 0.4f);
            activeTool.transform.localRotation = Quaternion.Euler(18f, -30f, -28f);
            activeTool.transform.localScale = Vector3.one;
            currentToolKind = toolKind;
            aimTargetWorld = targetWorldPosition;
            hasAimTarget = shouldTrackTarget;

            Material handleMaterial = IslandInteractionUtility.CreateLitMaterial("ActionTool_Handle", new Color(0.47f, 0.31f, 0.17f), 0.12f);
            Material metalMaterial = IslandInteractionUtility.CreateLitMaterial("ActionTool_Metal", new Color(0.58f, 0.62f, 0.67f), 0.32f);
            Material strapMaterial = IslandInteractionUtility.CreateLitMaterial("ActionTool_Strap", new Color(0.14f, 0.12f, 0.1f), 0.08f);
            Material handMaterial = IslandInteractionUtility.CreateLitMaterial("ActionTool_Hand", new Color(0.78f, 0.62f, 0.44f), 0.18f);
            Material sleeveMaterial = IslandInteractionUtility.CreateLitMaterial("ActionTool_Sleeve", new Color(0.22f, 0.63f, 0.78f), 0.12f);

            GameObject forearm = IslandInteractionUtility.CreateMeshObject("Forearm", PrimitiveType.Cube, sleeveMaterial, activeTool.transform);
            forearm.transform.localPosition = new Vector3(-0.08f, -0.08f, -0.01f);
            forearm.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            forearm.transform.localScale = new Vector3(0.1f, 0.24f, 0.09f);

            GameObject palm = IslandInteractionUtility.CreateMeshObject("Palm", PrimitiveType.Cube, handMaterial, activeTool.transform);
            palm.transform.localPosition = new Vector3(0.02f, 0.08f, -0.01f);
            palm.transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            palm.transform.localScale = new Vector3(0.1f, 0.1f, 0.09f);
            if (toolKind == ToolKind.Hand)
            {
                return;
            }

            GameObject handle = IslandInteractionUtility.CreateMeshObject("Handle", PrimitiveType.Cube, handleMaterial, activeTool.transform);
            handle.transform.localPosition = new Vector3(0.02f, -0.06f, 0f);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            handle.transform.localScale = new Vector3(0.06f, 0.84f, 0.06f);

            GameObject grip = IslandInteractionUtility.CreateMeshObject("Grip", PrimitiveType.Cube, strapMaterial, activeTool.transform);
            grip.transform.localPosition = new Vector3(0.01f, -0.31f, 0f);
            grip.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            grip.transform.localScale = new Vector3(0.07f, 0.18f, 0.07f);

            if (toolKind == ToolKind.Axe)
            {
                GameObject blade = IslandInteractionUtility.CreateMeshObject("Blade", PrimitiveType.Cube, metalMaterial, activeTool.transform);
                blade.transform.localPosition = new Vector3(0.16f, 0.34f, 0f);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
                blade.transform.localScale = new Vector3(0.24f, 0.18f, 0.05f);

                GameObject bladeEdge = IslandInteractionUtility.CreateMeshObject("BladeEdge", PrimitiveType.Cube, metalMaterial, activeTool.transform);
                bladeEdge.transform.localPosition = new Vector3(0.26f, 0.33f, 0f);
                bladeEdge.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
                bladeEdge.transform.localScale = new Vector3(0.12f, 0.1f, 0.04f);
            }
            else
            {
                GameObject head = IslandInteractionUtility.CreateMeshObject("Head", PrimitiveType.Cube, metalMaterial, activeTool.transform);
                head.transform.localPosition = new Vector3(0.04f, 0.34f, 0f);
                head.transform.localScale = new Vector3(0.28f, 0.06f, 0.07f);

                GameObject pointA = IslandInteractionUtility.CreateMeshObject("PointA", PrimitiveType.Cube, metalMaterial, activeTool.transform);
                pointA.transform.localPosition = new Vector3(-0.12f, 0.34f, 0f);
                pointA.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
                pointA.transform.localScale = new Vector3(0.14f, 0.04f, 0.04f);

                GameObject pointB = IslandInteractionUtility.CreateMeshObject("PointB", PrimitiveType.Cube, metalMaterial, activeTool.transform);
                pointB.transform.localPosition = new Vector3(0.2f, 0.34f, 0f);
                pointB.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
                pointB.transform.localScale = new Vector3(0.14f, 0.04f, 0.04f);
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

            Vector3 startPosition = currentToolKind switch
            {
                ToolKind.Axe => new Vector3(0.34f, -0.34f, 0.42f),
                ToolKind.Pickaxe => new Vector3(0.3f, -0.32f, 0.44f),
                _ => new Vector3(0.14f, -0.18f, 0.3f)
            };
            Vector3 hitPosition = currentToolKind switch
            {
                ToolKind.Axe => new Vector3(0.06f, -0.06f, 0.88f),
                ToolKind.Pickaxe => new Vector3(0.02f, -0.02f, 0.96f),
                _ => new Vector3(0.06f, -0.06f, 0.44f)
            };

            Vector3 startRotation = currentToolKind switch
            {
                ToolKind.Axe => new Vector3(24f, -38f, -42f),
                ToolKind.Pickaxe => new Vector3(18f, -34f, -36f),
                _ => new Vector3(6f, -24f, -22f)
            };
            Vector3 hitRotation = currentToolKind switch
            {
                ToolKind.Axe => new Vector3(136f, 2f, -92f),
                ToolKind.Pickaxe => new Vector3(148f, -6f, -82f),
                _ => new Vector3(34f, -6f, -8f)
            };

            if (hasAimTarget && anchor != null)
            {
                Vector3 localTargetForTravel = anchor.InverseTransformPoint(aimTargetWorld);
                float forwardReach = Mathf.Clamp(localTargetForTravel.z * 0.54f, currentToolKind == ToolKind.Hand ? 0.5f : 0.82f, 1.16f);
                hitPosition.x = Mathf.Lerp(hitPosition.x, Mathf.Clamp(localTargetForTravel.x * 0.34f, -0.12f, 0.14f), currentToolKind == ToolKind.Hand ? 0.85f : 0.72f);
                hitPosition.y = Mathf.Lerp(hitPosition.y, Mathf.Clamp(localTargetForTravel.y * 0.22f - 0.1f, -0.24f, 0.14f), 0.6f);
                hitPosition.z = Mathf.Max(hitPosition.z, forwardReach);
            }

            Vector3 swungPosition = Vector3.Lerp(startPosition, hitPosition, strikeProgress);
            float arc = Mathf.Sin(strikeProgress * Mathf.PI);
            swungPosition += currentToolKind switch
            {
                ToolKind.Axe => new Vector3(-0.06f * arc, 0.11f * arc, 0.04f * arc),
                ToolKind.Pickaxe => new Vector3(-0.08f * arc, 0.14f * arc, 0.03f * arc),
                _ => new Vector3(-0.03f * arc, 0.05f * arc, 0.02f * arc)
            };
            activeTool.transform.localPosition = swungPosition;

            Quaternion baseRotation = Quaternion.Euler(Vector3.Lerp(startRotation, hitRotation, strikeProgress));
            if (hasAimTarget && anchor != null)
            {
                Vector3 localTarget = anchor.InverseTransformPoint(aimTargetWorld);
                localTarget.x = Mathf.Clamp(localTarget.x, -0.36f, 0.42f);
                localTarget.y = Mathf.Clamp(localTarget.y, -0.7f, 0.34f);
                localTarget.z = Mathf.Clamp(localTarget.z, 0.95f, 2.8f);

                Vector3 toolHeadOffset = currentToolKind switch
                {
                    ToolKind.Axe => new Vector3(0.26f, 0.34f, 0f),
                    ToolKind.Pickaxe => new Vector3(0.2f, 0.34f, 0f),
                    _ => new Vector3(0.1f, 0.1f, 0f)
                };

                Vector3 toolHeadLocalPosition = activeTool.transform.localPosition + (baseRotation * toolHeadOffset);
                Vector3 desiredDirection = localTarget - toolHeadLocalPosition;
                if (desiredDirection.sqrMagnitude > 0.0001f)
                {
                    desiredDirection.Normalize();
                    Vector3 strikeAxis = currentToolKind switch
                    {
                        ToolKind.Axe => new Vector3(0.98f, 0.12f, 0f).normalized,
                        ToolKind.Pickaxe => new Vector3(0.98f, 0.14f, 0f).normalized,
                        _ => new Vector3(0.9f, 0.1f, 0f).normalized
                    };
                    Quaternion aimedRotation = Quaternion.FromToRotation(baseRotation * strikeAxis, desiredDirection) * baseRotation;
                    baseRotation = Quaternion.Slerp(baseRotation, aimedRotation, Mathf.SmoothStep(0.34f, 1f, strikeProgress));
                }
            }

            activeTool.transform.localRotation = baseRotation;
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
            hasAimTarget = false;
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

        private System.Collections.IEnumerator OneShotRoutine(ToolKind toolKind, Vector3 targetWorldPosition, float duration)
        {
            ShowTool(toolKind, targetWorldPosition, true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float phase = duration <= 0.0001f ? 1f : elapsed / duration;
                SetAimTarget(targetWorldPosition);
                UpdateSwing(phase, 1f / Mathf.Max(duration, 0.08f));
                yield return null;
            }

            HideTool();
            activeOneShotRoutine = null;
        }
    }
}
