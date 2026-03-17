using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runtime builder for the simple 2-player box arena scene.
/// Attach to an empty GameObject. Assign the per-player inspector fields,
/// hit Play — both players spawn automatically and the match begins.
///
/// This script owns the full match/round/draft loop — no external managers needed.
///
/// Match flow:
///   Start → arena built → P1/P2 spawned → round loop:
///     StartRound → [players fight] → EndRound → AutoPickCard for loser
///     → next round (or EndMatch when someone reaches winsToWinMatch)
/// </summary>
public class BoxArenaBuilder : MonoBehaviour
{
    [Header("Player 1 (Blue — left spawn)")]
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private ClassData  player1ClassData;
    [SerializeField] private PowerCardData[] player1Cards;

    [Header("Player 2 (Red — right spawn)")]
    [SerializeField] private GameObject player2Prefab;
    [SerializeField] private ClassData  player2ClassData;
    [SerializeField] private PowerCardData[] player2Cards;

    [Header("Match Settings")]
    [SerializeField] private int   winsToWinMatch = 3;
    [SerializeField] private float roundEndDelay  = 2f;

    [Header("Arena Dimensions (interior)")]
    [SerializeField] private float arenaWidth  = 24f;
    [SerializeField] private float arenaHeight = 14f;

    [Header("Kill Zone")]
    [Tooltip("How far outside the arena walls the kill boundary sits.")]
    [SerializeField] private float killZonePadding = 6f;

    [Header("Arena Colors")]
    [SerializeField] private Color floorColor    = new Color(0.35f, 0.32f, 0.28f);
    [SerializeField] private Color wallColor     = new Color(0.22f, 0.20f, 0.24f);
    [SerializeField] private Color ceilingColor  = new Color(0.18f, 0.18f, 0.22f);
    [SerializeField] private Color killZoneColor = new Color(0.6f,  0.1f,  0.1f,  0.4f);

    // Spawn point positions — P1 left, P2 right
    private static readonly Vector2[] SpawnOffsets = { new Vector2(-5f, 1.5f), new Vector2(5f, 1.5f) };

    // P1 blue, P2 red
    private static readonly Color[] PlayerColors =
    {
        new Color(0.2f, 0.5f, 1f),   // Blue
        new Color(1f,   0.3f, 0.3f), // Red
    };

    // P1 = WASD/Mouse, P2 = Arrow keys — both on keyboard, separate bindings
    private static readonly string[] ControlSchemes = { "KeyboardWASD", "KeyboardArrows" };

    // Scene references
    private Transform[]       spawnPoints;
    private MultiTargetCamera multiCam;

    // Match / round state
    private GameObject[]   players;      // [0] = P1, [1] = P2
    private HealthSystem[] playerHealth;
    private int[]          roundWins;
    private bool           roundActive;
    private int            aliveCount;
    private bool           matchOver;

    private void Start()
    {
        GameObject activePrefab = player1Prefab ?? player2Prefab;

        if (activePrefab == null)
        {
            Debug.LogError("BoxArenaBuilder: At least one player prefab must be assigned!", this);
            return;
        }

        BuildBoxArena();
        spawnPoints = CreateSpawnPoints();
        multiCam    = SetupCamera();

        // SpellEffectRegistry must exist before any CardInventory.AddCard call
        new GameObject("SpellEffectRegistry").AddComponent<SpellEffectRegistry>();

        roundWins    = new int[2];
        players      = new GameObject[2];
        playerHealth = new HealthSystem[2];

        players[0] = SpawnPlayer(0, player1Prefab ?? player2Prefab, player1ClassData);
        players[1] = SpawnPlayer(1, player2Prefab ?? player1Prefab, player2ClassData);

        StartCoroutine(StartRoundNextFrame());
    }

    // =========================================================
    // Round / Match Flow
    // =========================================================

    private IEnumerator StartRoundNextFrame()
    {
        yield return null;
        StartRound();
    }

    private void StartRound()
    {
        RespawnAllPlayers();

        // Notify spell effects that a new round is beginning
        foreach (var p in players)
        {
            if (p != null)
                SpellEffectRegistry.Instance?.NotifyRoundStart(p);
        }

        // Subscribe to death events and count alive players
        aliveCount = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;
            playerHealth[i] = players[i].GetComponent<HealthSystem>();
            if (playerHealth[i] != null && playerHealth[i].IsAlive)
            {
                aliveCount++;
                int capturedIndex = i;
                playerHealth[i].OnDeath.AddListener(() => OnPlayerDied(capturedIndex));
            }
        }

        roundActive = true;
        Debug.Log($"BoxArenaBuilder: Round started. Wins — P1: {roundWins[0]}  P2: {roundWins[1]}");
    }

    private void RespawnAllPlayers()
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            // Reset death handler (re-enables GameObject, colliders, input)
            players[i].GetComponent<PlayerDeathHandler>()?.ResetForRound();

            // Move to spawn point
            Vector3 pos = i < spawnPoints.Length ? spawnPoints[i].position : Vector3.zero;
            players[i].transform.position = pos;

            // Reset velocity
            var rb = players[i].GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Full combat reset
            players[i].GetComponent<ClassManager>()?.ResetForRound();
        }
    }

    private void OnPlayerDied(int playerIndex)
    {
        if (!roundActive) return;

        aliveCount--;
        Debug.Log($"BoxArenaBuilder: Player {playerIndex + 1} died. Alive: {aliveCount}");

        if (aliveCount <= 0)
            StartCoroutine(EndRoundAfterDelay());
    }

    private IEnumerator EndRoundAfterDelay()
    {
        roundActive = false;
        yield return new WaitForSeconds(roundEndDelay);
        EndRound();
    }

    private void EndRound()
    {
        // Determine winner (the player still alive, or P1 by default)
        int winnerIndex = -1;
        int loserIndex  = -1;
        for (int i = 0; i < players.Length; i++)
        {
            var h = playerHealth[i];
            if (h != null && h.IsAlive)
                winnerIndex = i;
            else
                loserIndex = i;
        }

        // If both died simultaneously, award P1
        if (winnerIndex < 0) winnerIndex = 0;
        if (loserIndex  < 0) loserIndex  = 1 - winnerIndex;

        roundWins[winnerIndex]++;
        Debug.Log($"BoxArenaBuilder: Round over. P{winnerIndex + 1} wins! Totals — P1: {roundWins[0]}  P2: {roundWins[1]}");

        // Notify spell effects round ended
        foreach (var p in players)
        {
            if (p != null)
                SpellEffectRegistry.Instance?.NotifyRoundEnd(p);
        }

        // Remove stale death listeners
        for (int i = 0; i < players.Length; i++)
        {
            if (playerHealth[i] != null)
                playerHealth[i].OnDeath.RemoveAllListeners();
        }

        if (roundWins[winnerIndex] >= winsToWinMatch)
        {
            EndMatch(winnerIndex);
            return;
        }

        // Loser picks a card, then next round starts
        StartCoroutine(AutoPickCardThenNextRound(loserIndex));
    }

    private IEnumerator AutoPickCardThenNextRound(int loserIndex)
    {
        // Give the loser a random card from the combined pool
        PowerCardData[] pool = loserIndex == 0 ? player1Cards : player2Cards;
        if (pool != null && pool.Length > 0)
        {
            var inventory = players[loserIndex]?.GetComponent<CardInventory>();
            if (inventory != null)
            {
                var pick = pool[Random.Range(0, pool.Length)];
                inventory.AddCard(pick);
                Debug.Log($"BoxArenaBuilder: P{loserIndex + 1} auto-picked card: {pick.cardName}");
            }
        }

        yield return new WaitForSeconds(1f);
        StartCoroutine(StartRoundNextFrame());
    }

    private void EndMatch(int winnerIndex)
    {
        matchOver = true;
        Debug.Log($"BoxArenaBuilder: Match over! P{winnerIndex + 1} wins the match " +
                  $"({roundWins[winnerIndex]}-{roundWins[1 - winnerIndex]})!");
    }

    // =========================================================
    // Arena Geometry
    // =========================================================

    private void BuildBoxArena()
    {
        var arena = new GameObject("BoxArena");

        float hw = arenaWidth  / 2f;
        float hh = arenaHeight / 2f;
        float t  = 1f;

        // Floor — top surface at Y = 0
        CreateBox("Floor", arena.transform,
            new Vector3(0, -t / 2f, 0),
            new Vector3(arenaWidth + t * 2f, t, 1f),
            floorColor);

        // Left wall — "Wall" layer so PhysicsCheck.IsTouchingWall fires
        CreateBox("Wall_Left", arena.transform,
            new Vector3(-hw - t / 2f, hh / 2f, 0),
            new Vector3(t, arenaHeight + t * 2f, 1f),
            wallColor, zeroFriction: true, layerName: "Wall");

        // Right wall
        CreateBox("Wall_Right", arena.transform,
            new Vector3(hw + t / 2f, hh / 2f, 0),
            new Vector3(t, arenaHeight + t * 2f, 1f),
            wallColor, zeroFriction: true, layerName: "Wall");

        // Ceiling — bottom surface at Y = arenaHeight
        CreateBox("Ceiling", arena.transform,
            new Vector3(0, arenaHeight + t / 2f, 0),
            new Vector3(arenaWidth + t * 2f, t, 1f),
            ceilingColor);

        // Kill zone boundary — four trigger strips outside the arena walls.
        // Anything that escapes the playable area and enters these strips is killed/destroyed.
        float kz   = killZonePadding;
        float span = arenaWidth  + kz * 4f; // wide enough to catch corners
        float tall = arenaHeight + kz * 4f;

        CreateKillZone("KillZone_Bottom", arena.transform,
            new Vector3(0f,           -t - kz / 2f,            0f),
            new Vector3(span,          kz,                      1f));

        CreateKillZone("KillZone_Top", arena.transform,
            new Vector3(0f,            arenaHeight + t + kz / 2f, 0f),
            new Vector3(span,          kz,                         1f));

        CreateKillZone("KillZone_Left", arena.transform,
            new Vector3(-hw - t - kz / 2f, arenaHeight / 2f,   0f),
            new Vector3(kz,                tall,                1f));

        CreateKillZone("KillZone_Right", arena.transform,
            new Vector3(hw + t + kz / 2f,  arenaHeight / 2f,   0f),
            new Vector3(kz,                tall,                1f));
    }

    private void CreateBox(string name, Transform parent, Vector3 position, Vector3 scale,
                           Color color, bool zeroFriction = false, string layerName = "Ground")
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position   = position;
        go.transform.localScale = scale;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        go.GetComponent<MeshRenderer>().material = mat;

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
        go.transform.position   = position;
        go.transform.localScale = scale;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = killZoneColor;
        go.GetComponent<MeshRenderer>().material = mat;

        DestroyImmediate(go.GetComponent<Collider>());
        var col2d = go.AddComponent<BoxCollider2D>();
        col2d.size      = Vector2.one;
        col2d.isTrigger = true;

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

        mainCam.orthographic       = true;
        mainCam.orthographicSize   = 10f;
        mainCam.transform.position = new Vector3(0f, arenaHeight / 2f, -10f);
        mainCam.backgroundColor    = new Color(0.08f, 0.08f, 0.12f);

        var cam = mainCam.GetComponent<MultiTargetCamera>();
        if (cam == null)
            cam = mainCam.gameObject.AddComponent<MultiTargetCamera>();

        return cam;
    }

    // =========================================================
    // Player Spawning
    // =========================================================

    /// <summary>
    /// Instantiate one player, position them, apply color/class, register with camera.
    /// Returns the instantiated GameObject.
    /// </summary>
    private GameObject SpawnPlayer(int playerIndex, GameObject prefab, ClassData classData)
    {
        if (prefab == null)
        {
            Debug.LogError($"BoxArenaBuilder: No prefab for Player {playerIndex + 1}!", this);
            return null;
        }

        Vector3 spawnPos = playerIndex < spawnPoints.Length
            ? spawnPoints[playerIndex].position
            : Vector3.zero;

        string scheme = playerIndex < ControlSchemes.Length ? ControlSchemes[playerIndex] : "Gamepad";
        var playerInput = PlayerInput.Instantiate(prefab, playerIndex: playerIndex,
            controlScheme: scheme, splitScreenIndex: -1, pairWithDevice: Keyboard.current);
        playerInput.transform.position = spawnPos;
        var go = playerInput.gameObject;
        go.name = $"Player{playerIndex + 1}";

        // Color — create a placeholder white sprite if none is assigned
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (sr.sprite == null)
            {
                var tex    = new Texture2D(64, 64);
                var pixels = new Color[64 * 64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
            }
            sr.color = playerIndex < PlayerColors.Length ? PlayerColors[playerIndex] : Color.white;
        }

        // Identity
        go.GetComponent<PlayerIdentity>()?.Initialize(playerIndex);

        // Class
        var classManager = go.GetComponent<ClassManager>();
        if (classManager != null && classData != null)
            classManager.Initialize(classData, playerIndex);

        // Camera tracking
        multiCam.AddTarget(go.transform);

        Debug.Log($"BoxArenaBuilder: Player {playerIndex + 1} spawned as " +
                  $"{(classData != null ? classData.className : "unknown")}");

        return go;
    }
}

/// <summary>
/// Replaces EnvironmentHazard (whose damage field is private) with InstantKillTrigger on Start().
/// </summary>
internal class KillZoneDamageOverride : MonoBehaviour
{
    private void Start()
    {
        Destroy(GetComponent<EnvironmentHazard>());
        gameObject.AddComponent<InstantKillTrigger>();
    }
}

/// <summary>Kills any player and destroys any projectile that enters this trigger.</summary>
internal class InstantKillTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kill players
        var health = other.GetComponent<HealthSystem>();
        if (health != null && health.IsAlive)
        {
            health.TakeDamage(9999f, -1);
            return;
        }

        // Destroy projectiles
        if (other.GetComponent<Projectile>() != null)
            Destroy(other.gameObject);
    }
}
