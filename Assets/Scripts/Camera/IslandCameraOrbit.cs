using UnityEngine;

namespace PrivateIsland
{
    [ExecuteAlways]
    public sealed class IslandCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 focusPoint = new Vector3(0f, 7f, 0f);
        [SerializeField] private float distance = 110f;
        [SerializeField] private float yaw = 32f;
        [SerializeField] private float pitch = 28f;
        [SerializeField] private float idleOrbitSpeed = 4f;
        [SerializeField] private float zoomSpeed = 16f;
        [SerializeField] private float dragSensitivity = 3.2f;
        [SerializeField] private float minDistance = 54f;
        [SerializeField] private float maxDistance = 220f;
        [SerializeField] private Transform focusTarget;
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, 2f, 0f);
        [SerializeField] private bool enableIdleOrbit = true;

        public void Configure(Vector3 targetFocusPoint, float targetDistance, float targetYaw, float targetPitch, Transform target = null, Vector3? targetFocusOffset = null)
        {
            focusTarget = target;
            focusOffset = targetFocusOffset ?? new Vector3(0f, 2f, 0f);
            enableIdleOrbit = focusTarget == null;
            focusPoint = targetFocusPoint;
            minDistance = Mathf.Max(14f, targetDistance * 0.55f);
            maxDistance = Mathf.Max(minDistance + 18f, targetDistance * 2.1f);
            distance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            yaw = targetYaw;
            pitch = Mathf.Clamp(targetPitch, 12f, 65f);
            ApplyTransform();
        }

        private void OnEnable()
        {
            ApplyTransform();
        }

        private void OnValidate()
        {
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            pitch = Mathf.Clamp(pitch, 12f, 65f);
            ApplyTransform();
        }

        private void Update()
        {
            UpdateFocusPoint();

            if (!Application.isPlaying)
            {
                ApplyTransform();
                return;
            }

            bool isDragging = Input.GetMouseButton(1);

            if (isDragging)
            {
                yaw += Input.GetAxis("Mouse X") * dragSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * dragSensitivity;
            }
            else if (enableIdleOrbit)
            {
                yaw += idleOrbitSpeed * Time.deltaTime;
            }

            distance -= Input.mouseScrollDelta.y * zoomSpeed * 0.1f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            pitch = Mathf.Clamp(pitch, 12f, 65f);

            ApplyTransform();
        }

        private void UpdateFocusPoint()
        {
            if (focusTarget == null)
            {
                return;
            }

            Vector3 desiredFocusPoint = focusTarget.position + focusOffset;
            if (!Application.isPlaying)
            {
                focusPoint = desiredFocusPoint;
                return;
            }

            float blend = 1f - Mathf.Exp(-10f * Time.deltaTime);
            focusPoint = Vector3.Lerp(focusPoint, desiredFocusPoint, blend);
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = focusPoint + (rotation * new Vector3(0f, 0f, -distance));
            transform.rotation = rotation;
        }
    }
}
