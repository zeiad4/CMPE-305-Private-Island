using UnityEngine;

namespace PrivateIsland
{
    public sealed class IslandCharacterController : MonoBehaviour
    {
        private static readonly Collider[] MovementCollisionHits = new Collider[16];
        private static readonly RaycastHit[] GroundHits = new RaycastHit[24];

        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float sprintMultiplier = 1.65f;
        [SerializeField] private float jumpHeight = 2.1f;
        [SerializeField] private float gravity = 26f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private float groundOffset = 0.02f;
        [SerializeField] private float groundSnapDistance = 0.08f;
        [SerializeField] private float collisionRadius = 0.34f;
        [SerializeField] private float collisionHeight = 1.8f;

        private float islandSize;
        private float peakHeight;
        private Camera cachedCamera;
        private float viewYaw;
        private bool hasViewYaw;
        private bool inputEnabled = true;
        private bool movementEnabled = true;
        private bool isGrounded = true;
        private Vector3 currentVelocity;
        private Vector3 previousPosition;
        private float verticalVelocity;

        public bool IsInputEnabled => inputEnabled;
        public bool IsMovementEnabled => movementEnabled;
        public bool IsGrounded => isGrounded;
        public Vector3 CurrentVelocity => currentVelocity;

        public void Configure(float terrainSize, float terrainPeakHeight)
        {
            islandSize = terrainSize;
            peakHeight = terrainPeakHeight;
            SnapToGround(true);
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

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
        }

        public void TeleportTo(Vector3 position, float yaw)
        {
            transform.position = ClampToIsland(position);
            viewYaw = yaw;
            hasViewYaw = true;
            ApplyViewRotation();
            SnapToGround(true);
            previousPosition = transform.position;
            currentVelocity = Vector3.zero;
        }

        private void OnEnable()
        {
            SnapToGround(true);
            previousPosition = transform.position;
            currentVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                SnapToGround(true);
                currentVelocity = Vector3.zero;
                previousPosition = transform.position;
                return;
            }

            if (!inputEnabled)
            {
                SnapToGround(true);
                ApplyViewRotation();
                currentVelocity = Vector3.zero;
                previousPosition = transform.position;
                return;
            }

            if (!movementEnabled)
            {
                SnapToGround(true);
                ApplyViewRotation();
                currentVelocity = Vector3.zero;
                previousPosition = transform.position;
                return;
            }

            Vector2 input = ReadMovementInput();
            Vector3 moveDirection = ResolveMoveDirection(input);
            Move(moveDirection);

            ApplyViewRotation();
            UpdateVelocity();
        }

        private void Move(Vector3 moveDirection)
        {
            Vector3 nextPosition = ClampToIsland(transform.position);

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                float speed = moveSpeed * (IsSprintHeld() ? sprintMultiplier : 1f);
                Vector3 movementDelta = moveDirection.normalized * speed * Time.deltaTime;
                nextPosition = ResolveHorizontalMovement(nextPosition, movementDelta);

                if (!hasViewYaw)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                }
            }

            float groundHeight = SampleGroundHeight(nextPosition) + groundOffset;
            UpdateGroundState(ref nextPosition, groundHeight);

            if (isGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                isGrounded = false;
            }

            if (isGrounded)
            {
                nextPosition.y = groundHeight;
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
                nextPosition.y += verticalVelocity * Time.deltaTime;

                if (nextPosition.y <= groundHeight)
                {
                    nextPosition.y = groundHeight;
                    verticalVelocity = 0f;
                    isGrounded = true;
                }
            }

            transform.position = nextPosition;
        }

        private void SnapToGround(bool resetVerticalMotion = false)
        {
            Vector3 position = ClampToIsland(transform.position);
            position.y = SampleGroundHeight(position) + groundOffset;
            transform.position = position;

            if (resetVerticalMotion)
            {
                verticalVelocity = 0f;
                isGrounded = true;
            }
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

        private void UpdateGroundState(ref Vector3 position, float groundHeight)
        {
            if (position.y <= groundHeight + groundSnapDistance && verticalVelocity <= 0f)
            {
                position.y = groundHeight;
                verticalVelocity = 0f;
                isGrounded = true;
                return;
            }

            isGrounded = false;
        }

        private static bool IsSprintHeld()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private Vector3 ResolveHorizontalMovement(Vector3 origin, Vector3 movementDelta)
        {
            Vector3 target = ClampToIsland(origin + movementDelta);
            if (CanOccupyPosition(target))
            {
                return target;
            }

            Vector3 horizontalOnly = new Vector3(movementDelta.x, 0f, 0f);
            if (horizontalOnly.sqrMagnitude > 0.000001f)
            {
                Vector3 horizontalTarget = ClampToIsland(origin + horizontalOnly);
                if (CanOccupyPosition(horizontalTarget))
                {
                    origin = horizontalTarget;
                }
            }

            Vector3 depthOnly = new Vector3(0f, 0f, movementDelta.z);
            if (depthOnly.sqrMagnitude > 0.000001f)
            {
                Vector3 depthTarget = ClampToIsland(origin + depthOnly);
                if (CanOccupyPosition(depthTarget))
                {
                    origin = depthTarget;
                }
            }

            return origin;
        }

        private bool CanOccupyPosition(Vector3 position)
        {
            float radius = Mathf.Max(0.05f, collisionRadius);
            float height = Mathf.Max(collisionHeight, radius * 2f);
            Vector3 bottom = position + Vector3.up * radius;
            Vector3 top = position + Vector3.up * (height - radius);

            int hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, MovementCollisionHits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = MovementCollisionHits[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hit.GetComponentInParent<IslandRockInteraction>() != null ||
                    hit.GetComponentInParent<IslandPalmInteraction>() != null ||
                    hit.GetComponentInParent<IslandCampfireInteraction>() != null ||
                    hit.GetComponentInParent<IslandSolidObstacle>() != null)
                {
                    return false;
                }
            }

            return true;
        }

        private float SampleGroundHeight(Vector3 position)
        {
            float terrainHeight = IslandMeshBuilder.SampleHeight(position.x, position.z, islandSize, peakHeight);
            Vector3 rayOrigin = position + Vector3.up * (peakHeight + 24f);
            float rayDistance = peakHeight + 48f;
            int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, GroundHits, rayDistance, ~0, QueryTriggerInteraction.Ignore);

            float bestSurfaceY = float.MinValue;
            bool foundWalkableSurface = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = GroundHits[i];
                Collider collider = hit.collider;
                if (collider == null || collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (collider.GetComponentInParent<IslandWalkableSurface>() == null)
                {
                    continue;
                }

                if (hit.point.y > bestSurfaceY)
                {
                    bestSurfaceY = hit.point.y;
                    foundWalkableSurface = true;
                }
            }

            return foundWalkableSurface ? bestSurfaceY : terrainHeight;
        }

        private void UpdateVelocity()
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            currentVelocity = (transform.position - previousPosition) / deltaTime;
            previousPosition = transform.position;
        }
    }
}
