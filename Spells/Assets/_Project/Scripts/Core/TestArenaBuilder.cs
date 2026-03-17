using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach this to an empty GameObject in a new scene.
/// It builds the test arena, camera, and player spawning at runtime.
/// The arena is designed to test all movement mechanics:
/// - Flat ground for running and dash dancing
/// - Walls for wall sliding and wall jumping
/// - Platforms at varied heights for jumping
/// - Ramps/slopes for wave-landing
/// - A pit area for recovery testing
/// </summary>
public class TestArenaBuilder : MonoBehaviour
{
    [Header("Movement Data")]
    [SerializeField] private MovementData movementData;

    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    private MultiTargetCamera multiTargetCamera;

    private void Start()
    {
        if (movementData == null)
        {
            Debug.LogError("TestArenaBuilder: Assign a MovementData asset!");
            return;
        }

        // Check if procedural generation is available on the same GameObject
        var levelGenerator = GetComponent<ProceduralLevelGenerator>();
        var modularBuilder = GetComponent<ModularArenaBuilder>();

        if (levelGenerator != null && modularBuilder != null)
        {
            // Use procedural generation instead of the static test arena
            var layout = levelGenerator.GenerateForRound(1);
            if (layout != null)
            {
                modularBuilder.Build(layout);
                SetupCamera();

                // Set camera background to biome color
                if (levelGenerator.LastUsedBiome != null && Camera.main != null)
                    Camera.main.backgroundColor = levelGenerator.LastUsedBiome.backgroundColor;

                if (playerPrefab != null)
                    SetupPlayerSpawningWithSpawns(modularBuilder.PlayerSpawnPoints);

                Debug.Log($"TestArenaBuilder: Procedural arena built — {layout.arenaName}");
                return;
            }

            Debug.LogWarning("TestArenaBuilder: Procedural generation failed, falling back to static arena.");
        }

        BuildArena();
        SetupCamera();

        if (playerPrefab != null)
        {
            SetupPlayerSpawning();
        }
        else
        {
            Debug.LogWarning("TestArenaBuilder: No player prefab assigned.");
        }
    }

    private void BuildArena()
    {
        var arena = new GameObject("Arena");

        // ============================================================
        // GROUND — Main floor with ramps on both sides
        // Layout (Y positions are TOP surface of ground):
        //   Elevated (-2.5) --Ramp-- Center ground (-4.5) --Ramp-- Elevated (-2.5)
        // ============================================================

        // Center flat ground — split into two halves with a gap for the bowl
        // Left half: x=-10 to x=-5, right half: x=5 to x=10
        CreatePlatform("Ground_CenterLeft", arena.transform,
            new Vector3(-7.5f, -5, 0), new Vector3(5, 1, 1),
            new Color(0.3f, 0.3f, 0.35f));

        CreatePlatform("Ground_CenterRight", arena.transform,
            new Vector3(7.5f, -5, 0), new Vector3(5, 1, 1),
            new Color(0.3f, 0.3f, 0.35f));

        // Left ramp: connects center ground (y=-5, x=-10) up to elevated ground (y=-2.5, x=-16)
        // Width=6, Height=2.5, positioned so bottom-right aligns with ground left edge
        // Center position: x=-13, y=-3.75 (midpoint of -5 to -2.5)
        CreateRamp("Ramp_Left", arena.transform,
            new Vector3(-13f, -3.75f, 0), 6f, 2.5f, true,
            new Color(0.35f, 0.3f, 0.28f));

        // Right ramp: mirror of left ramp
        CreateRamp("Ramp_Right", arena.transform,
            new Vector3(13f, -3.75f, 0), 6f, 2.5f, false,
            new Color(0.35f, 0.3f, 0.28f));

        // Left elevated ground: x=-21 to x=-16, top surface at y=-2.5
        // (x=-16 aligns with the ramp's top-left corner)
        CreatePlatform("Ground_Left", arena.transform,
            new Vector3(-18.5f, -3f, 0), new Vector3(5, 1, 1),
            new Color(0.3f, 0.3f, 0.35f));

        // Right elevated ground: mirror of left
        CreatePlatform("Ground_Right", arena.transform,
            new Vector3(18.5f, -3f, 0), new Vector3(5, 1, 1),
            new Color(0.3f, 0.3f, 0.35f));

        // ============================================================
        // WALLS — For wall sliding and wall jumping
        // ============================================================

        // Left boundary wall (tall)
        CreatePlatform("Wall_Left", arena.transform,
            new Vector3(-21, 8, 0), new Vector3(1, 28, 1),
            new Color(0.22f, 0.22f, 0.28f), "Wall");

        // Right boundary wall (tall)
        CreatePlatform("Wall_Right", arena.transform,
            new Vector3(21, 8, 0), new Vector3(1, 28, 1),
            new Color(0.22f, 0.22f, 0.28f), "Wall");

        // Center pillar walls — two short walls for wall-jump chaining
        CreatePlatform("Pillar_Left", arena.transform,
            new Vector3(-3, 3, 0), new Vector3(1, 8, 1),
            new Color(0.25f, 0.28f, 0.3f), "Wall");

        CreatePlatform("Pillar_Right", arena.transform,
            new Vector3(3, 3, 0), new Vector3(1, 8, 1),
            new Color(0.25f, 0.28f, 0.3f), "Wall");

        // ============================================================
        // PLATFORMS — Varied heights for jump testing
        // ============================================================

        // Low platforms (just above ground, good for wave-land into jump)
        CreatePlatform("Platform_LowLeft", arena.transform,
            new Vector3(-8, -3, 0), new Vector3(4, 0.5f, 1),
            new Color(0.4f, 0.35f, 0.3f));

        CreatePlatform("Platform_LowRight", arena.transform,
            new Vector3(8, -3, 0), new Vector3(4, 0.5f, 1),
            new Color(0.4f, 0.35f, 0.3f));

        // Mid platforms (between the pillars)
        CreatePlatform("Platform_MidCenter", arena.transform,
            new Vector3(0, 1, 0), new Vector3(4, 0.5f, 1),
            new Color(0.45f, 0.38f, 0.3f));

        // Mid-high platforms on the sides
        CreatePlatform("Platform_MidLeft", arena.transform,
            new Vector3(-12, 3, 0), new Vector3(5, 0.5f, 1),
            new Color(0.42f, 0.36f, 0.3f));

        CreatePlatform("Platform_MidRight", arena.transform,
            new Vector3(12, 3, 0), new Vector3(5, 0.5f, 1),
            new Color(0.42f, 0.36f, 0.3f));

        // High platform (above the pillars) — land here from wall jumps
        CreatePlatform("Platform_High", arena.transform,
            new Vector3(0, 8, 0), new Vector3(6, 0.5f, 1),
            new Color(0.5f, 0.4f, 0.32f));

        // ============================================================
        // RAMP PLATFORMS — Elevated ramps for wave-land from height
        // ============================================================

        // Left mid ramp — slope on a platform, run off the edge for wave-land
        CreateRamp("Ramp_MidLeft", arena.transform,
            new Vector3(-12, 5.5f, 0), 5f, 2f, true,
            new Color(0.38f, 0.34f, 0.28f));

        // Right mid ramp — mirror
        CreateRamp("Ramp_MidRight", arena.transform,
            new Vector3(12, 5.5f, 0), 5f, 2f, false,
            new Color(0.38f, 0.34f, 0.28f));

        // ============================================================
        // TOP PLATFORMS — High area
        // ============================================================

        CreatePlatform("Platform_TopLeft", arena.transform,
            new Vector3(-14, 10, 0), new Vector3(4, 0.5f, 1),
            new Color(0.5f, 0.42f, 0.35f));

        CreatePlatform("Platform_TopRight", arena.transform,
            new Vector3(14, 10, 0), new Vector3(4, 0.5f, 1),
            new Color(0.5f, 0.42f, 0.35f));

        CreatePlatform("Platform_TopCenter", arena.transform,
            new Vector3(0, 13, 0), new Vector3(5, 0.5f, 1),
            new Color(0.52f, 0.44f, 0.36f));

        // ============================================================
        // V-SHAPED BOWL — In the gap between the two ground halves
        // Players can run off the edge, ride the ramp down, build speed,
        // and wave-land at the bottom or up the other side.
        // Ground edges at x=-5 and x=5, top surface y=-4.5
        // Bowl floor at y=-8
        // ============================================================

        // Left ramp of bowl: from ground edge (x=-5, y=-5.5) down to bowl (x=-1, y=-8)
        // slopeLeft=true means high on left, low on right
        CreateRamp("Bowl_Left", arena.transform,
            new Vector3(-3f, -6.75f, 0), 4f, 3.5f, true,
            new Color(0.28f, 0.28f, 0.32f));

        // Right ramp of bowl: from (x=1, y=-8) up to ground edge (x=5, y=-5.5)
        // slopeLeft=false means high on right, low on left
        CreateRamp("Bowl_Right", arena.transform,
            new Vector3(3f, -6.75f, 0), 4f, 3.5f, false,
            new Color(0.28f, 0.28f, 0.32f));

        // Bowl floor — flat bottom between the two ramps
        CreatePlatform("Bowl_Floor", arena.transform,
            new Vector3(0, -8.75f, 0), new Vector3(2, 0.5f, 1),
            new Color(0.26f, 0.26f, 0.3f));

        // ============================================================
        // CEILING
        // ============================================================

        CreatePlatform("Ceiling", arena.transform,
            new Vector3(0, 21, 0), new Vector3(44, 1, 1),
            new Color(0.22f, 0.22f, 0.28f));

        Debug.Log("TestArenaBuilder: Arena built with ramps and slopes! Press any button to join as a player.");
    }

    /// <summary>
    /// Creates a rectangular platform with a BoxCollider2D.
    /// </summary>
    private void CreatePlatform(string name, Transform parent, Vector3 position, Vector3 scale, Color color, string layerName = "Ground")
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.parent = parent;
        go.transform.position = position;
        go.transform.localScale = scale;

        // Set color
        var renderer = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        renderer.material = mat;

        // Remove 3D collider immediately, then add 2D collider
        var collider3D = go.GetComponent<Collider>();
        if (collider3D != null) DestroyImmediate(collider3D);

        var collider2D = go.AddComponent<BoxCollider2D>();
        collider2D.size = Vector2.one;

        // Set layer
        go.layer = LayerMask.NameToLayer(layerName);
        if (go.layer == -1)
        {
            go.layer = LayerMask.NameToLayer("Ground");
            if (go.layer == -1) go.layer = 0;
            Debug.LogWarning($"Layer '{layerName}' not found. Using Ground layer for '{name}'.");
        }
    }

    /// <summary>
    /// Creates a triangular ramp/slope using a PolygonCollider2D.
    /// The ramp forms a right triangle:
    /// - slopeLeft=true: high on the left, slopes down to the right (left-to-right downhill)
    /// - slopeLeft=false: high on the right, slopes down to the left (right-to-left downhill)
    /// Position is the center of the ramp's bounding box.
    /// </summary>
    private void CreateRamp(string name, Transform parent, Vector3 position, float width, float height, bool slopeLeft, Color color, string layerName = "Ground")
    {
        var go = new GameObject(name);
        go.transform.parent = parent;
        go.transform.position = position;

        // Create a visual mesh for the ramp (triangle shape)
        var meshFilter = go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        meshRenderer.material = mat;

        // Build triangle mesh
        float halfW = width / 2f;
        float halfH = height / 2f;

        Vector3[] vertices;
        if (slopeLeft)
        {
            // High on left, low on right
            // Triangle: bottom-left, bottom-right, top-left
            vertices = new Vector3[]
            {
                new Vector3(-halfW, -halfH, -0.5f), // bottom-left front
                new Vector3(halfW, -halfH, -0.5f),   // bottom-right front
                new Vector3(-halfW, halfH, -0.5f),   // top-left front
                new Vector3(-halfW, -halfH, 0.5f),   // bottom-left back
                new Vector3(halfW, -halfH, 0.5f),    // bottom-right back
                new Vector3(-halfW, halfH, 0.5f),    // top-left back
            };
        }
        else
        {
            // High on right, low on left
            // Triangle: bottom-left, bottom-right, top-right
            vertices = new Vector3[]
            {
                new Vector3(-halfW, -halfH, -0.5f), // bottom-left front
                new Vector3(halfW, -halfH, -0.5f),   // bottom-right front
                new Vector3(halfW, halfH, -0.5f),    // top-right front
                new Vector3(-halfW, -halfH, 0.5f),   // bottom-left back
                new Vector3(halfW, -halfH, 0.5f),    // bottom-right back
                new Vector3(halfW, halfH, 0.5f),     // top-right back
            };
        }

        int[] triangles = new int[]
        {
            // Front face
            0, 2, 1,
            // Back face
            3, 4, 5,
            // Bottom face
            0, 1, 4,
            0, 4, 3,
            // Hypotenuse (slope face)
            1, 2, 5,
            1, 5, 4,
            // Vertical side
            0, 3, 5,
            0, 5, 2,
        };

        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < normals.Length; i++)
            normals[i] = Vector3.back; // Simple normal for 2D view

        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        // Add 2D polygon collider matching the triangle shape
        var polyCollider = go.AddComponent<PolygonCollider2D>();

        Vector2[] colliderPoints;
        if (slopeLeft)
        {
            colliderPoints = new Vector2[]
            {
                new Vector2(-halfW, -halfH), // bottom-left
                new Vector2(halfW, -halfH),  // bottom-right
                new Vector2(-halfW, halfH),  // top-left
            };
        }
        else
        {
            colliderPoints = new Vector2[]
            {
                new Vector2(-halfW, -halfH), // bottom-left
                new Vector2(halfW, -halfH),  // bottom-right
                new Vector2(halfW, halfH),   // top-right
            };
        }

        polyCollider.SetPath(0, colliderPoints);

        // Set layer
        go.layer = LayerMask.NameToLayer(layerName);
        if (go.layer == -1)
        {
            go.layer = LayerMask.NameToLayer("Ground");
            if (go.layer == -1) go.layer = 0;
            Debug.LogWarning($"Layer '{layerName}' not found. Using Ground layer for '{name}'.");
        }
    }

    private void SetupCamera()
    {
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            var camGo = new GameObject("Main Camera");
            mainCam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }

        mainCam.orthographic = true;
        mainCam.orthographicSize = 15f;
        mainCam.transform.position = new Vector3(0, 6, -10);
        mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);

        multiTargetCamera = mainCam.GetComponent<MultiTargetCamera>();
        if (multiTargetCamera == null)
            multiTargetCamera = mainCam.gameObject.AddComponent<MultiTargetCamera>();
    }

    private void SetupPlayerSpawning()
    {
        // Create spawn points
        var spawnParent = new GameObject("SpawnPoints");
        Transform[] spawnPoints = new Transform[4];

        Vector3[] positions = new Vector3[]
        {
            new Vector3(-9, -4, 0),   // On left center ground
            new Vector3(-6, -4, 0),   // On left center ground
            new Vector3(6, -4, 0),    // On right center ground
            new Vector3(9, -4, 0),    // On right center ground
        };

        for (int i = 0; i < 4; i++)
        {
            var sp = new GameObject($"Spawn {i + 1}");
            sp.transform.parent = spawnParent.transform;
            sp.transform.position = positions[i];
            spawnPoints[i] = sp.transform;
        }

        SetupPlayerSpawningWithSpawns(spawnPoints);
    }

    /// <summary>
    /// Wire up PlayerSpawnManager + PlayerInputManager with provided spawn points.
    /// Used by both the static arena path and the procedural generation path.
    /// </summary>
    private void SetupPlayerSpawningWithSpawns(Transform[] spawnPoints)
    {
        // Set up PlayerSpawnManager FIRST (before PlayerInputManager can fire events)
        var spawnManager = gameObject.GetComponent<PlayerSpawnManager>();
        if (spawnManager == null)
            spawnManager = gameObject.AddComponent<PlayerSpawnManager>();

        spawnManager.Initialize(spawnPoints, multiTargetCamera);

        // Set up PlayerInputManager AFTER spawn manager is ready
        var manager = gameObject.GetComponent<PlayerInputManager>();
        if (manager == null)
            manager = gameObject.AddComponent<PlayerInputManager>();

        manager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
        manager.playerPrefab = playerPrefab;
    }
}
