using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandCharacterController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private float groundOffset = 0.02f;

        private float islandSize;
        private float peakHeight;
        private Camera cachedCamera;
        private float viewYaw;
        private bool hasViewYaw;
        private bool inputEnabled = true;

        public void Configure(float terrainSize, float terrainPeakHeight)
        {
            islandSize = terrainSize;
            peakHeight = terrainPeakHeight;
            SnapToGround();
        }

        public void SetViewYaw(float yaw)
        {
            viewYaw = yaw;
            hasViewYaw = true;
            ApplyViewRotation();
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

        private void OnEnable()
        {
            SnapToGround();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                SnapToGround();
                return;
            }

            if (!inputEnabled)
            {
                SnapToGround();
                ApplyViewRotation();
                return;
            }

            Vector2 input = ReadMovementInput();
            Vector3 moveDirection = ResolveMoveDirection(input);

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                Move(moveDirection.normalized);
            }
            else
            {
                SnapToGround();
            }

            ApplyViewRotation();
        }

        private void Move(Vector3 moveDirection)
        {
            Vector3 nextPosition = transform.position + (moveDirection * moveSpeed * Time.deltaTime);
            nextPosition = ClampToIsland(nextPosition);
            nextPosition.y = SampleGroundHeight(nextPosition) + groundOffset;
            transform.position = nextPosition;

            if (!hasViewYaw)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        private void SnapToGround()
        {
            Vector3 position = ClampToIsland(transform.position);
            position.y = SampleGroundHeight(position) + groundOffset;
            transform.position = position;
        }

        private Vector3 ClampToIsland(Vector3 position)
        {
            Vector2 planar = new Vector2(position.x, position.z);
            float maxRadius = islandSize * 0.46f;
            if (planar.magnitude > maxRadius)
            {
                planar = planar.normalized * maxRadius;
            }

            position.x = planar.x;
            position.z = planar.y;
            return position;
        }

        private void ApplyViewRotation()
        {
            if (!hasViewYaw)
            {
                return;
            }

            transform.rotation = Quaternion.Euler(0f, viewYaw, 0f);
        }

        private Vector3 ResolveMoveDirection(Vector2 input)
        {
            if (input.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            cachedCamera ??= Camera.main;
            if (cachedCamera == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 forward = cachedCamera.transform.forward;
            Vector3 right = cachedCamera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return (right * input.x) + (forward * input.y);
        }

        private Vector2 ReadMovementInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                horizontal -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                horizontal += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                vertical -= 1f;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                vertical += 1f;
            }

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private float SampleGroundHeight(Vector3 position)
        {
            return IslandMeshBuilder.SampleHeight(position.x, position.z, islandSize, peakHeight);
        }
    }
}
