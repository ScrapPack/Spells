using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runtime builder for the simple box arena scene.
/// Attach to an empty GameObject in a new scene. Assign the three
/// inspector fields, hit Play — the rest is constructed at runtime.
///
/// Scene flow:
///   Play → arena geometry built → press any button to join (up to 2 players)
///   → match auto-starts once 2 players joined → Round → Draft (auto-pick) → loop → MatchEnd
///
/// Inspector fields required:
///   playerPrefab   — Player prefab from Prefabs/Player/
///   allCards       — All PowerCardData assets from Data/Cards/
///   defaultClassData — A ClassData asset from Data/Classes/ (e.g. WizardData)
/// </summary>
public class BoxArenaBuilder : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private PowerCardData[] allCards;
    [SerializeField] private ClassData defaultClassData;

    [Header("Match Settings")]
    [SerializeField] private int winsToWinMatch = 3;

    [Header("Arena Dimensions (interior)")]
    [SerializeField] private float arenaWidth = 24f;
    [SerializeField] private float arenaHeight = 14f;

    [Header("Colors")]
    [SerializeField] private Color floorColor   = new Color(0.35f, 0.32f, 0.28f);
    [SerializeField] private Color wallColor    = new Color(0.22f, 0.20f, 0.24f);
    [SerializeField] private Color ceilingColor = new Color(0.18f, 0.18f, 0.22f);
    [SerializeField] private Color killZoneColor = new Color(0.6f, 0.1f, 0.1f, 0.4f);

    // Spawn point positions (relative to arena center)
    private static readonly Vector2[] SpawnOffsets = { new Vector2(-5f, 1.5f), new Vector2(5f, 1.5f) };

    private Transform[] spawnPoints;
    private MultiTargetCamera multiCam;

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("BoxArenaBuilder: playerPrefab is not assigned!", this);
            return;
        }

        BuildBoxArena();
        spawnPoints = CreateSpawnPoints();
        multiCam = SetupCamera();
        SetupManagers();

        Debug.Log("BoxArenaBuilder: Scene ready. Press any button on a controller or keyboard to join (max 2 players).");
    }

    // =========================================================
    // Arena Geometry
    // =========================================================

    private void BuildBoxArena()
    {
        var arena = new GameObject("BoxArena");

        float hw = arenaWidth  / 2f;  // half-width
        float hh = arenaHeight / 2f;  // half-height
        float t  = 1f;                // wall thickness

        // Floor — top surface at Y=0
        CreateBox("Floor",     arena.transform, new Vector3(0, -t / 2f, 0),          new Vector3(arenaWidth + t * 2f, t, 1f), floorColor);

        // Left wall — right face at X = -hw (must be "Wall" layer for IsTouchingWall detection)
        CreateBox("Wall_Left", arena.transform, new Vector3(-hw - t / 2f, hh / 2f, 0), new Vector3(t, arenaHeight + t * 2f, 1f), wallColor, zeroFriction: true, layerName: "Wall");

        // Right wall — left face at X = +hw (must be "Wall" layer for IsTouchingWall detection)
        CreateBox("Wall_Right",arena.transform, new Vector3( hw + t / 2f, hh / 2f, 0), new Vector3(t, arenaHeight + t * 2f, 1f), wallColor, zeroFriction: true, layerName: "Wall");

        // Ceiling — bottom surface at Y = arenaHeight
        CreateBox("Ceiling",   arena.transform, new Vector3(0, arenaHeight + t / 2f, 0), new Vector3(arenaWidth + t * 2f, t, 1f), ceilingColor);

        // Kill zone — trigger well below the floor
        CreateKillZone("KillZone", arena.transform, new Vector3(0, -4f, 0), new Vector3(arenaWidth + 4f, 4f, 1f));
    }

    private void CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Color color,
                           bool zeroFriction = false, string layerName = "Ground")
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;

        // Material
        var renderer = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        renderer.material = mat;

        // Swap 3D collider → 2D collider
        DestroyImmediate(go.GetComponent<Collider>());
        var col2d = go.AddComponent<BoxCollider2D>();
        col2d.size = Vector2.one;

        if (zeroFriction)
        {
            var phyMat = new PhysicsMaterial2D("WallNoFriction") { friction = 0f, bounciness = 0f };
            col2d.sharedMaterial = phyMat;
        }

        int layer = LayerMask.NameToLayer(layerName);
        go.layer = layer >= 0 ? layer : 0;
        if (layer < 0)
            Debug.LogWarning($"BoxArenaBuilder: Layer '{layerName}' not found. '{name}' placed on Default layer.");
    }

    private void CreateKillZone(string name, Transform parent, Vector3 position, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;

        // Semi-transparent red visual
        var renderer = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = killZoneColor;
        renderer.material = mat;

        // Trigger collider (instant-kill damage via EnvironmentHazard)
        DestroyImmediate(go.GetComponent<Collider>());
        var col2d = go.AddComponent<BoxCollider2D>();
        col2d.size = Vector2.one;
        col2d.isTrigger = true;

        // KillZoneDamageOverride removes EnvironmentHazard and adds InstantKillTrigger
        // on Start(), since EnvironmentHazard's damage field can't be set at runtime.
        go.AddComponent<EnvironmentHazard>();
        go.AddComponent<KillZoneDamageOverride>();
    }

    // =========================================================
    // Spawn Points
    // =========================================================

    private Transform[] CreateSpawnPoints()
    {
        var parent = new GameObject("SpawnPoints").transform;
        var points = new Transform[SpawnOffsets.Length];
        for (int i = 0; i < SpawnOffsets.Length; i++)
        {
            var sp = new GameObject($"SpawnPoint_P{i + 1}");
            sp.transform.SetParent(parent);
            sp.transform.position = new Vector3(SpawnOffsets[i].x, SpawnOffsets[i].y, 0f);
            points[i] = sp.transform;
        }
        return points;
    }

    // =========================================================
    // Camera
    // =========================================================

    private MultiTargetCamera SetupCamera()
    {
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            mainCam = camGo.AddComponent<Camera>();
        }

        mainCam.orthographic = true;
        mainCam.orthographicSize = 10f;
        mainCam.transform.position = new Vector3(0f, arenaHeight / 2f, -10f);
        mainCam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);

        var cam = mainCam.GetComponent<MultiTargetCamera>();
        if (cam == null)
            cam = mainCam.gameObject.AddComponent<MultiTargetCamera>();

        return cam;
    }

    // =========================================================
    // Managers
    // =========================================================

    private void SetupManagers()
    {
        // All managers go on a dedicated GameManager object
        var gmGo = new GameObject("GameManager");

        var roundManager = gmGo.AddComponent<RoundManager>();

        var draftManager = gmGo.AddComponent<DraftManager>();
        draftManager.SetCardDatabase(allCards);

        var autoDraft = gmGo.AddComponent<AutoDraftPicker>();

        var matchManager = gmGo.AddComponent<MatchManager>();

        // Player spawning (on this GameObject so SendMessages reaches OnPlayerJoined)
        var spawnManager = gameObject.GetComponent<PlayerSpawnManager>();
        if (spawnManager == null)
            spawnManager = gameObject.AddComponent<PlayerSpawnManager>();

        spawnManager.Initialize(spawnPoints, multiCam);
        spawnManager.SetMatchManager(matchManager);
        if (defaultClassData != null)
            spawnManager.SetDefaultClass(defaultClassData);
        spawnManager.SetAutoStartCount(SpawnOffsets.Length); // auto-start when 2 players join

        var inputManager = gameObject.GetComponent<PlayerInputManager>();
        if (inputManager == null)
            inputManager = gameObject.AddComponent<PlayerInputManager>();

        inputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
        inputManager.playerPrefab = playerPrefab;

        // Wire MatchManager refs (must happen before its Start() runs next frame)
        matchManager.SetReferences(roundManager, draftManager, spawnManager, multiCam, winsToWinMatch);

        // AutoDraftPicker needs DraftManager ref (must happen before DraftManager.Start())
        autoDraft.Initialize(draftManager);

        // Respawn players at round start (reposition + re-enable after death)
        matchManager.OnStateChanged.AddListener(OnMatchStateChanged);
    }

    // =========================================================
    // Round Respawn
    // =========================================================

    private void OnMatchStateChanged(MatchManager.MatchState state)
    {
        if (state == MatchManager.MatchState.RoundStart)
            RespawnAllPlayers();
    }

    /// <summary>
    /// Repositions all players to their spawn points and resets death state.
    /// Called at the start of every round (including round 1, though players
    /// are already positioned correctly at join time).
    /// </summary>
    private void RespawnAllPlayers()
    {
        // FindObjectsByType with FindObjectsInactive.Include catches deactivated dead players
        var players = FindObjectsByType<PlayerIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var identity in players)
        {
            // Re-enable via PlayerDeathHandler (restores input, colliders, rigidbody)
            var deathHandler = identity.GetComponent<PlayerDeathHandler>();
            if (deathHandler != null)
                deathHandler.ResetForRound();

            // Position at the correct spawn point
            int spawnIndex = Mathf.Clamp(identity.PlayerID, 0, spawnPoints.Length - 1);
            identity.transform.position = spawnPoints[spawnIndex].position;

            // Zero velocity so they don't carry momentum from death into the next round
            var rb = identity.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }
}

/// <summary>
/// Sets kill zone damage to 999 on Start so it's an instant kill.
/// Placed on the same GameObject as EnvironmentHazard.
/// Separated into its own component to avoid needing to expose
/// EnvironmentHazard's private serialized fields.
/// </summary>
internal class KillZoneDamageOverride : MonoBehaviour
{
    private void Start()
    {
        // EnvironmentHazard reads 'damage' in its trigger. We can't set the private
        // field at runtime without reflection, so we replace the component entirely
        // with a simple direct-kill trigger instead.
        Destroy(GetComponent<EnvironmentHazard>());
        gameObject.AddComponent<InstantKillTrigger>();
    }
}

/// <summary>
/// Kills any player that enters this trigger immediately.
/// </summary>
internal class InstantKillTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var health = other.GetComponent<HealthSystem>();
        if (health != null && health.IsAlive)
            health.TakeDamage(9999f, -1);
    }
}
