using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime builder for the simple 2-player box arena scene.
/// Attach to an empty GameObject. Assign the per-player inspector fields,
/// hit Play — both players spawn automatically and the match begins.
///
/// Three-tier cycle:
///   Fight  — one life vs life combat
///   Round  — Best of <fightsToWinRound> fights (default: first to 2 = Best of 3)
///   Game   — first player to <roundsToWinGame> rounds (default: 5) wins
///
/// After each fight the loser gets a card. After the game ends, Retry / Quit buttons appear.
/// </summary>
public class BoxArenaBuilder : MonoBehaviour
{
    [Header("Player 1 (Blue — left spawn)")]
    [SerializeField] private GameObject    player1Prefab;
    [SerializeField] private ClassData     player1ClassData;
    [SerializeField] private PowerCardData[] player1Cards;

    [Header("Player 2 (Red — right spawn)")]
    [SerializeField] private GameObject    player2Prefab;
    [SerializeField] private ClassData     player2ClassData;
    [SerializeField] private PowerCardData[] player2Cards;

    [Header("Round & Game Settings")]
    [Tooltip("Kills needed to win one round (2 = Best of 3, 3 = Best of 5).")]
    [SerializeField] private int   fightsToWinRound  = 2;
    [Tooltip("Rounds needed to win the game.")]
    [SerializeField] private int   roundsToWinGame   = 5;
    [Tooltip("Seconds between a kill and the next respawn or round-end screen.")]
    [SerializeField] private float fightEndDelay     = 2f;
    [Tooltip("How many card choices to offer the round loser.")]
    [SerializeField] private int   cardOfferCount    = 3;

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
    private static readonly Vector2[] SpawnOffsets =
        { new Vector2(-5f, 1.5f), new Vector2(5f, 1.5f) };

    // P1 blue, P2 red
    private static readonly Color[] PlayerColors =
    {
        new Color(0.2f, 0.5f, 1f),
        new Color(1f,   0.3f, 0.3f),
    };

    // P1 = WASD, P2 = Arrow keys
    private static readonly string[] ControlSchemes = { "KeyboardWASD", "KeyboardArrows" };

    // Scene references
    private Transform[]       spawnPoints;
    private MultiTargetCamera multiCam;

    // Players
    private GameObject[]   players;
    private HealthSystem[] playerHealth;

    // Fight state
    private bool fightActive;

    // Round state (Best of N kills)
    private int[] killsThisRound;   // kills scored in the current round
    private int   currentRound;

    // Game state (first to roundsToWinGame rounds)
    private int[] roundWins;
    private bool  gameOver;

    // =========================================================
    // Startup
    // =========================================================

    private void Start()
    {
        if (player1Prefab == null && player2Prefab == null)
        {
            Debug.LogError("BoxArenaBuilder: At least one player prefab must be assigned!", this);
            return;
        }

        BuildBoxArena();
        spawnPoints = CreateSpawnPoints();
        multiCam    = SetupCamera();

        new GameObject("SpellEffectRegistry").AddComponent<SpellEffectRegistry>();

        // UI clicks require an EventSystem. Create one if none exists.
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        players        = new GameObject[2];
        playerHealth   = new HealthSystem[2];
        roundWins      = new int[2];
        killsThisRound = new int[2];

        players[0] = SpawnPlayer(0, player1Prefab ?? player2Prefab, player1ClassData);
        players[1] = SpawnPlayer(1, player2Prefab ?? player1Prefab, player2ClassData);

        StartCoroutine(BeginFirstFightNextFrame());
    }

    private IEnumerator BeginFirstFightNextFrame()
    {
        yield return null;
        StartRound();
    }

    // =========================================================
    // Round (first to fightsToWinRound kills)
    // =========================================================

    private void StartRound()
    {
        killsThisRound[0] = 0;
        killsThisRound[1] = 0;
        currentRound++;
        Debug.Log($"BoxArenaBuilder: === Round {currentRound} begins " +
                  $"(game score  P1 {roundWins[0]} – P2 {roundWins[1]}) ===");

        foreach (var p in players)
            SpellEffectRegistry.Instance?.NotifyRoundStart(p);

        RespawnAndSubscribe();
    }

    private void EndRound(int roundWinnerIndex, int roundLoserIndex)
    {
        roundWins[roundWinnerIndex]++;
        Debug.Log($"BoxArenaBuilder: Round {currentRound} won by P{roundWinnerIndex + 1}! " +
                  $"Game score — P1: {roundWins[0]}  P2: {roundWins[1]}");

        foreach (var p in players)
            SpellEffectRegistry.Instance?.NotifyRoundEnd(p);

        if (roundWins[roundWinnerIndex] >= roundsToWinGame)
        {
            ShowGameOverUI(roundWinnerIndex);
            return;
        }

        ShowCardPickUI(roundLoserIndex, StartRound);
    }

    // =========================================================
    // Kill / Respawn loop
    // =========================================================

    /// <summary>
    /// Respawn both players at their original spawn positions and subscribe to OnDeath.
    /// Called at the start of every round and after each kill within a round.
    /// </summary>
    private void RespawnAndSubscribe()
    {
        RespawnAllPlayers();

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;
            playerHealth[i] = players[i].GetComponent<HealthSystem>();
            playerHealth[i]?.OnDeath.AddListener(OnPlayerDied);
        }

        fightActive = true;
    }

    private void OnPlayerDied()
    {
        if (!fightActive) return;
        fightActive = false;

        // Unsubscribe immediately so a simultaneous double-death doesn't score twice
        for (int i = 0; i < players.Length; i++)
            playerHealth[i]?.OnDeath.RemoveAllListeners();

        // Find winner (alive) and loser (dead)
        int winner = -1, loser = -1;
        for (int i = 0; i < players.Length; i++)
        {
            if (playerHealth[i] != null && playerHealth[i].IsAlive)
                winner = i;
            else
                loser = i;
        }
        if (winner < 0) winner = 0;
        if (loser  < 0) loser  = 1 - winner;

        killsThisRound[winner]++;
        Debug.Log($"BoxArenaBuilder: P{winner + 1} scores a kill! " +
                  $"Round score — P1: {killsThisRound[0]}  P2: {killsThisRound[1]}");

        if (killsThisRound[winner] >= fightsToWinRound)
            StartCoroutine(DelayThen(fightEndDelay, () => EndRound(winner, loser)));
        else
            StartCoroutine(DelayThen(fightEndDelay, RespawnAndSubscribe));
    }

    private IEnumerator DelayThen(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        action();
    }

    // =========================================================
    // Card Pick UI
    // =========================================================

    /// <summary>
    /// Shows a full-screen card selection screen for the round loser.
    /// Presents up to <see cref="cardOfferCount"/> random cards from their pool.
    /// Calls <paramref name="onComplete"/> once a card is chosen (or immediately if no pool).
    /// </summary>
    private void ShowCardPickUI(int loserIndex, System.Action onComplete)
    {
        PowerCardData[] pool = loserIndex == 0 ? player1Cards : player2Cards;

        // No pool assigned — skip straight to next round
        if (pool == null || pool.Length == 0)
        {
            onComplete();
            return;
        }

        // Pick up to cardOfferCount unique cards from the pool
        var offers = PickRandomOffers(pool, cardOfferCount);

        // ---- Canvas ----
        var canvasGo = new GameObject("CardPickCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ---- Dim overlay ----
        var overlay    = new GameObject("Overlay");
        overlay.transform.SetParent(canvasGo.transform, false);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.80f);
        var overlayRect  = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // ---- Header ----
        string loserName  = loserIndex == 0 ? "Player 1" : "Player 2";
        string winnerName = loserIndex == 0 ? "Player 2" : "Player 1";
        CreateLabel(overlay.transform, $"{winnerName} wins the round!",
            new Vector2(0f, 180f), 42, PlayerColors[1 - loserIndex]);
        CreateLabel(overlay.transform, $"{loserName}: choose a power up",
            new Vector2(0f, 120f), 28, Color.white);

        // ---- Card buttons ----
        float cardW    = 240f;
        float cardH    = 300f;
        float spacing  = 20f;
        float totalW   = offers.Length * cardW + (offers.Length - 1) * spacing;
        float startX   = -totalW / 2f + cardW / 2f;

        for (int i = 0; i < offers.Length; i++)
        {
            var card       = offers[i];
            float xPos     = startX + i * (cardW + spacing);
            var inventory  = players[loserIndex]?.GetComponent<CardInventory>();

            CreateCardButton(overlay.transform, card, new Vector2(xPos, -40f),
                new Vector2(cardW, cardH), () =>
                {
                    inventory?.AddCard(card);
                    Debug.Log($"BoxArenaBuilder: {loserName} picked: {card.cardName}");
                    Destroy(canvasGo);
                    onComplete();
                });
        }
    }

    private static PowerCardData[] PickRandomOffers(PowerCardData[] pool, int count)
    {
        // Shuffle a copy and take the first <count> entries
        var indices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < pool.Length; i++) indices.Add(i);
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        int take   = Mathf.Min(count, pool.Length);
        var result = new PowerCardData[take];
        for (int i = 0; i < take; i++) result[i] = pool[indices[i]];
        return result;
    }

    private static void CreateCardButton(Transform parent, PowerCardData card,
                                         Vector2 anchoredPos, Vector2 size,
                                         System.Action onClick)
    {
        // Card backing
        var go  = new GameObject(card.cardName + "_Card");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(card.cardColor.r * 0.25f,
                              card.cardColor.g * 0.25f,
                              card.cardColor.b * 0.25f, 1f);

        var btn    = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = img.color;
        colors.highlightedColor = new Color(card.cardColor.r * 0.45f,
                                            card.cardColor.g * 0.45f,
                                            card.cardColor.b * 0.45f, 1f);
        colors.pressedColor     = new Color(card.cardColor.r * 0.15f,
                                            card.cardColor.g * 0.15f,
                                            card.cardColor.b * 0.15f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick());

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta      = size;
        rt.anchoredPosition = anchoredPos;

        // Color accent bar at top
        var bar    = new GameObject("AccentBar");
        bar.transform.SetParent(go.transform, false);
        var barImg = bar.AddComponent<Image>();
        barImg.color = card.cardColor;
        var barRt  = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.offsetMin = new Vector2(0f, -8f);
        barRt.offsetMax = Vector2.zero;

        // Card name
        CreateCardLabel(go.transform, card.cardName, new Vector2(0f, size.y * 0.33f),
            new Vector2(size.x - 16f, 50f), 22, FontStyle.Bold, Color.white);

        // Positive effect
        CreateCardLabel(go.transform, card.positiveDescription,
            new Vector2(0f, 0f), new Vector2(size.x - 20f, size.y * 0.34f),
            14, FontStyle.Normal, new Color(0.4f, 1f, 0.5f));

        // Negative effect
        CreateCardLabel(go.transform, card.negativeDescription,
            new Vector2(0f, -size.y * 0.35f), new Vector2(size.x - 20f, size.y * 0.28f),
            14, FontStyle.Normal, new Color(1f, 0.4f, 0.4f));
    }

    private static void CreateCardLabel(Transform parent, string text, Vector2 anchoredPos,
                                        Vector2 size, int fontSize, FontStyle style, Color color)
    {
        var go  = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.text      = text;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.UpperCenter;
        txt.color     = color;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta      = size;
        rt.anchoredPosition = anchoredPos;
    }

    // =========================================================
    // Game Over UI
    // =========================================================

    private void ShowGameOverUI(int winnerIndex)
    {
        gameOver = true;
        string winnerName = winnerIndex == 0 ? "Player 1" : "Player 2";
        Debug.Log($"BoxArenaBuilder: GAME OVER — {winnerName} wins " +
                  $"({roundWins[winnerIndex]}–{roundWins[1 - winnerIndex]} rounds)!");

        // Canvas
        var canvasGo = new GameObject("GameOverCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Dim panel
        var panel    = new GameObject("Panel");
        panel.transform.SetParent(canvasGo.transform, false);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.72f);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Winner text
        CreateLabel(panel.transform, $"{winnerName} Wins!",
            new Vector2(0f, 80f), 52, PlayerColors[winnerIndex]);

        // Score text
        CreateLabel(panel.transform,
            $"{roundWins[0]} – {roundWins[1]}",
            new Vector2(0f, 20f), 32, Color.white);

        // Retry button
        CreateButton(panel.transform, "Retry", new Vector2(-110f, -60f), () =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        // Quit button
        CreateButton(panel.transform, "Quit", new Vector2(110f, -60f), () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    private static void CreateLabel(Transform parent, string text,
                                    Vector2 anchoredPos, int fontSize, Color color)
    {
        var go   = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var txt  = go.AddComponent<Text>();
        txt.text      = text;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = color;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta     = new Vector2(600f, 80f);
        rt.anchoredPosition = anchoredPos;
    }

    private static void CreateButton(Transform parent, string label,
                                     Vector2 anchoredPos, System.Action onClick)
    {
        var go  = new GameObject(label + "Button");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.18f, 1f);

        var btn = go.AddComponent<Button>();
        var nav = btn.navigation;
        nav.mode       = Navigation.Mode.None;
        btn.navigation = nav;

        var colors        = btn.colors;
        colors.normalColor    = new Color(0.15f, 0.15f, 0.18f);
        colors.highlightedColor = new Color(0.28f, 0.28f, 0.35f);
        colors.pressedColor   = new Color(0.08f, 0.08f, 0.10f);
        btn.colors        = colors;

        btn.onClick.AddListener(() => onClick());

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(180f, 55f);
        rt.anchoredPosition = anchoredPos;

        // Button label
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txt       = txtGo.AddComponent<Text>();
        txt.text      = label;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 26;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;

        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
    }

    // =========================================================
    // Helpers
    // =========================================================

    private void RespawnAllPlayers()
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;
            players[i].GetComponent<PlayerDeathHandler>()?.ResetForRound();

            Vector3 pos = i < spawnPoints.Length ? spawnPoints[i].position : Vector3.zero;
            players[i].transform.position = pos;
            var rb = players[i].GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = Vector2.zero;
            players[i].GetComponent<ClassManager>()?.ResetForRound();
        }

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

        CreateBox("Floor", arena.transform,
            new Vector3(0, -t / 2f, 0),
            new Vector3(arenaWidth + t * 2f, t, 1f),
            floorColor);

        CreateBox("Wall_Left", arena.transform,
            new Vector3(-hw - t / 2f, hh / 2f, 0),
            new Vector3(t, arenaHeight + t * 2f, 1f),
            wallColor, zeroFriction: true, layerName: "Wall");

        CreateBox("Wall_Right", arena.transform,
            new Vector3(hw + t / 2f, hh / 2f, 0),
            new Vector3(t, arenaHeight + t * 2f, 1f),
            wallColor, zeroFriction: true, layerName: "Wall");

        CreateBox("Ceiling", arena.transform,
            new Vector3(0, arenaHeight + t / 2f, 0),
            new Vector3(arenaWidth + t * 2f, t, 1f),
            ceilingColor);

        float kz   = killZonePadding;
        float span = arenaWidth  + kz * 4f;
        float tall = arenaHeight + kz * 4f;

        CreateKillZone("KillZone_Bottom", arena.transform,
            new Vector3(0f,              -t - kz / 2f,               0f),
            new Vector3(span,             kz,                         1f));

        CreateKillZone("KillZone_Top", arena.transform,
            new Vector3(0f,               arenaHeight + t + kz / 2f, 0f),
            new Vector3(span,             kz,                         1f));

        CreateKillZone("KillZone_Left", arena.transform,
            new Vector3(-hw - t - kz / 2f, arenaHeight / 2f,         0f),
            new Vector3(kz,                tall,                      1f));

        CreateKillZone("KillZone_Right", arena.transform,
            new Vector3(hw + t + kz / 2f,  arenaHeight / 2f,         0f),
            new Vector3(kz,                tall,                      1f));
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
            Debug.LogWarning($"BoxArenaBuilder: Layer '{layerName}' not found. '{name}' on Default layer.");
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
            mainCam   = camGo.AddComponent<Camera>();
        }

        mainCam.orthographic     = true;
        mainCam.orthographicSize = 10f;
        mainCam.transform.position = new Vector3(0f, arenaHeight / 2f, -10f);
        mainCam.backgroundColor  = new Color(0.08f, 0.08f, 0.12f);

        var cam = mainCam.GetComponent<MultiTargetCamera>()
               ?? mainCam.gameObject.AddComponent<MultiTargetCamera>();
        return cam;
    }

    // =========================================================
    // Player Spawning
    // =========================================================

    private GameObject SpawnPlayer(int playerIndex, GameObject prefab, ClassData classData)
    {
        if (prefab == null)
        {
            Debug.LogError($"BoxArenaBuilder: No prefab for Player {playerIndex + 1}!", this);
            return null;
        }

        Vector3 spawnPos = playerIndex < spawnPoints.Length
            ? spawnPoints[playerIndex].position : Vector3.zero;

        string scheme = playerIndex < ControlSchemes.Length
            ? ControlSchemes[playerIndex] : "Gamepad";

        var playerInput = PlayerInput.Instantiate(prefab, playerIndex: playerIndex,
            controlScheme: scheme, splitScreenIndex: -1, pairWithDevice: Keyboard.current);
        playerInput.transform.position = spawnPos;

        var go  = playerInput.gameObject;
        go.name = $"Player{playerIndex + 1}";

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
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64),
                                          new Vector2(0.5f, 0.5f), 64);
            }
            sr.color = playerIndex < PlayerColors.Length ? PlayerColors[playerIndex] : Color.white;
        }

        go.GetComponent<PlayerIdentity>()?.Initialize(playerIndex);

        // Keep GameObject active through death so PlayerInput never loses its device
        // pairing on SetActive cycles. The state machine is disabled on death instead.
        var deathHandler = go.GetComponent<PlayerDeathHandler>();
        if (deathHandler)
            deathHandler.DeactivateOnDeath = false;

        var classManager = go.GetComponent<ClassManager>();
        if (classManager != null && classData != null)
            classManager.Initialize(classData, playerIndex);

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
        var health = other.GetComponent<HealthSystem>();
        if (health != null && health.IsAlive)
        {
            health.TakeDamage(9999f, -1);
            return;
        }

        if (other.GetComponent<Projectile>() != null)
            Destroy(other.gameObject);
    }
}
