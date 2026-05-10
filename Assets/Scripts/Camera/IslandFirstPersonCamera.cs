using UnityEngine;

namespace PrivateIsland
{
    [ExecuteAlways]
    public sealed class IslandFirstPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 viewOffset = new Vector3(0f, 1.92f, 0.12f);
        [SerializeField] private float yaw;
        [SerializeField] private float pitch = -4f;
        [SerializeField] private float lookSensitivity = 3f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private bool lockCursorWhilePlaying = true;

        private IslandCharacterController cachedController;
        private bool inputSuspended;

        public void Configure(Transform target, Vector3 targetViewOffset, float targetYaw, float targetPitch)
        {
            followTarget = target;
            viewOffset = targetViewOffset;
            yaw = targetYaw;
            pitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
            CacheController();
            ApplyTransform();
        }

        public void SetInputSuspended(bool suspended)
        {
            inputSuspended = suspended;
        }

        private void OnEnable()
        {
            CacheController();
            ApplyTransform();

            if (Application.isPlaying && lockCursorWhilePlaying)
            {
                LockCursor();
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying && lockCursorWhilePlaying)
            {
                UnlockCursor();
            }
        }

        private void OnValidate()
        {
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            ApplyTransform();
        }

        private void Update()
        {
            if (followTarget == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                if (!inputSuspended)
                {
                    HandleCursorState();
                }

                if (!inputSuspended && Cursor.lockState == CursorLockMode.Locked)
                {
                    yaw += Input.GetAxis("Mouse X") * lookSensitivity;
                    pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
                    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                }
            }

            ApplyTransform();
        }

        private void HandleCursorState()
        {
            if (!lockCursorWhilePlaying)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockCursor();
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                LockCursor();
            }
        }

        private void ApplyTransform()
        {
            if (followTarget == null)
            {
                return;
            }

            CacheController();

            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            transform.position = followTarget.position + (yawRotation * viewOffset);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            if (cachedController != null)
            {
                cachedController.SetViewYaw(yaw);
            }
        }

        private void CacheController()
        {
            if (followTarget == null)
            {
                cachedController = null;
                return;
            }

            if (cachedController == null || cachedController.transform != followTarget)
            {
                cachedController = followTarget.GetComponent<IslandCharacterController>();
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
