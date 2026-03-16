using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Player Colors")]
    [SerializeField] private Color[] playerColors = new Color[]
    {
        new Color(0.2f, 0.5f, 1f),   // Blue
        new Color(1f, 0.3f, 0.3f),    // Red
        new Color(0.3f, 1f, 0.3f),    // Green
        new Color(1f, 0.9f, 0.2f),    // Yellow
    };

    [Header("References")]
    [SerializeField] private MultiTargetCamera multiTargetCamera;
    [SerializeField] private MatchManager matchManager;

    [Header("Class Assignment")]
    [Tooltip("Default class data assigned to joining players (temporary — replaced by character select)")]
    [SerializeField] private ClassData defaultClassData;

    private int playerCount = 0;
    private bool isInitialized = false;

    /// <summary>
    /// Call this from TestArenaBuilder to wire up references at runtime.
    /// </summary>
    public void Initialize(Transform[] spawns, MultiTargetCamera camera)
    {
        spawnPoints = spawns;
        multiTargetCamera = camera;

        // Subscribe to PlayerInputManager events
        var manager = GetComponent<PlayerInputManager>();
        if (manager != null)
        {
            manager.onPlayerJoined += OnPlayerJoined;
            manager.onPlayerLeft += OnPlayerLeft;
        }

        isInitialized = true;
    }

    private void OnEnable()
    {
        // Only auto-subscribe if references were set via inspector (not runtime)
        if (isInitialized) return;

        var manager = GetComponent<PlayerInputManager>();
        if (manager != null && spawnPoints != null && spawnPoints.Length > 0)
        {
            manager.onPlayerJoined += OnPlayerJoined;
            manager.onPlayerLeft += OnPlayerLeft;
        }
    }

    private void OnDisable()
    {
        var manager = GetComponent<PlayerInputManager>();
        if (manager != null)
        {
            manager.onPlayerJoined -= OnPlayerJoined;
            manager.onPlayerLeft -= OnPlayerLeft;
        }
    }

    private void OnPlayerJoined(PlayerInput playerInput)
    {
        int index = playerCount;
        playerCount++;

        // Position at spawn point
        if (spawnPoints != null && index < spawnPoints.Length)
        {
            playerInput.transform.position = spawnPoints[index].position;
        }

        // Assign color and ensure sprite is visible
        var spriteRenderer = playerInput.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Create a placeholder sprite if none assigned
            if (spriteRenderer.sprite == null)
            {
                var tex = new Texture2D(64, 64);
                var pixels = new Color[64 * 64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
            }

            if (index < playerColors.Length)
            {
                spriteRenderer.color = playerColors[index];
            }
        }

        // Initialize combat components
        var identity = playerInput.GetComponent<PlayerIdentity>();
        if (identity != null)
            identity.Initialize(index);

        var classManager = playerInput.GetComponent<ClassManager>();
        if (classManager != null && defaultClassData != null)
            classManager.Initialize(defaultClassData, index);

        // Register with camera
        if (multiTargetCamera != null)
        {
            multiTargetCamera.AddTarget(playerInput.transform);
        }

        // Register with match manager
        if (matchManager != null)
        {
            matchManager.RegisterPlayer(playerInput.gameObject, index);
        }

        Debug.Log($"Player {index + 1} joined as {(defaultClassData != null ? defaultClassData.className : "unknown")} (Device: {playerInput.currentControlScheme})");
    }

    private void OnPlayerLeft(PlayerInput playerInput)
    {
        if (multiTargetCamera != null)
        {
            multiTargetCamera.RemoveTarget(playerInput.transform);
        }

        playerCount--;
        Debug.Log("Player left");
    }
}
