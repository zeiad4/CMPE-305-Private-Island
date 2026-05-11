using UnityEngine;

namespace PrivateIsland
{
    public sealed class WorldControlBox : MonoBehaviour
    {
        [SerializeField] private EnvironmentMenuUI menuUI;
        [SerializeField] private string interactionPromptMessage = "Press E to open SeasonBox Controls";
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private bool closeMenuWhenPlayerLeaves = true;
        [SerializeField] private string playerTag = "Player";
        [Header("Placement")]
        [SerializeField] private bool snapToIslandOnStart = true;
        [SerializeField] private float islandSize = 220f;
        [SerializeField] private float peakHeight = 9f;
        [SerializeField] private Vector2 dockDirection = new Vector2(0.42f, 0.91f);
        [SerializeField] private float placementRadius = 52f;
        [SerializeField] private float sideOffset = -8f;
        [SerializeField] private float forwardOffset = -2f;
        [SerializeField] private float groundOffset = 0.05f;

        private Transform playerTransform;
        private bool warnedMissingMenuUI;
        private bool warnedMissingPlayer;

        private void Start()
        {
            SnapToIslandSurface();
            SetPromptVisible(false);
            ResolvePlayerTransform();
        }

        private void Update()
        {
            EnvironmentMenuUI resolvedMenu = GetMenuUI();
            if (resolvedMenu == null)
            {
                return;
            }

            ResolvePlayerTransform();
            if (playerTransform == null)
            {
                SetPromptVisible(false);
                return;
            }

            bool playerIsNearby = IsPlayerNearby();
            resolvedMenu.SetInteractionPromptVisible(playerIsNearby && !resolvedMenu.IsMenuOpen, interactionPromptMessage);

            if (closeMenuWhenPlayerLeaves && !playerIsNearby && resolvedMenu.IsMenuOpen)
            {
                resolvedMenu.CloseMenu();
                return;
            }

            if (playerIsNearby && !resolvedMenu.IsMenuOpen && Input.GetKeyDown(interactionKey))
            {
                resolvedMenu.OpenMenu();
                resolvedMenu.SetInteractionPromptVisible(false);
            }
        }

        private void OnDisable()
        {
            SetPromptVisible(false);
        }

        [ContextMenu("Snap To Island Surface")]
        private void SnapToIslandSurface()
        {
            if (!snapToIslandOnStart)
            {
                return;
            }

            Vector2 normalizedDockDirection = dockDirection.sqrMagnitude > 0.0001f
                ? dockDirection.normalized
                : new Vector2(0.42f, 0.91f).normalized;

            Vector2 perpendicular = new Vector2(-normalizedDockDirection.y, normalizedDockDirection.x);
            Vector2 planar = (normalizedDockDirection * placementRadius) +
                             (perpendicular * sideOffset) +
                             (normalizedDockDirection * forwardOffset);

            float maxRadius = islandSize * 0.42f;
            if (planar.magnitude > maxRadius)
            {
                planar = planar.normalized * maxRadius;
            }

            float y = IslandMeshBuilder.SampleHeight(planar.x, planar.y, islandSize, peakHeight) + groundOffset;
            transform.position = new Vector3(planar.x, y, planar.y);
        }

        private bool IsPlayerNearby()
        {
            Vector3 playerPosition = playerTransform.position;
            Vector3 kioskPosition = transform.position;
            playerPosition.y = kioskPosition.y;

            return Vector3.Distance(playerPosition, kioskPosition) <= interactionDistance;
        }

        private void ResolvePlayerTransform()
        {
            if (playerTransform != null)
            {
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                return;
            }

            if (!warnedMissingPlayer)
            {
                Debug.LogWarning($"{nameof(WorldControlBox)} on '{name}' could not find a player tagged '{playerTag}'.", this);
                warnedMissingPlayer = true;
            }
        }

        private EnvironmentMenuUI GetMenuUI()
        {
            if (menuUI == null)
            {
                menuUI = FindAnyObjectByType<EnvironmentMenuUI>();
            }

            if (menuUI == null && !warnedMissingMenuUI)
            {
                Debug.LogWarning($"{nameof(WorldControlBox)} on '{name}' is missing an {nameof(EnvironmentMenuUI)} reference.", this);
                warnedMissingMenuUI = true;
            }

            return menuUI;
        }

        private void SetPromptVisible(bool shouldShow)
        {
            EnvironmentMenuUI resolvedMenu = GetMenuUI();
            if (resolvedMenu == null)
            {
                return;
            }

            resolvedMenu.SetInteractionPromptVisible(shouldShow, interactionPromptMessage);
        }
    }
}
