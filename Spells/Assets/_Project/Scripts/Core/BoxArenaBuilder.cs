using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;
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


    // Scene references
    private Transform[]       spawnPoints;
    private MultiTargetCamera multiCam;

    // Players
    private GameObject[]   players;
    private HealthSystem[] playerHealth;
    private RoundTimerUI   roundTimer;

    // Fight state
    private bool fightActive;

    // Round state (Best of N kills)
    private int[] killsThisRound;   // kills scored in the current round
    private int   currentRound;

    // Game state (first to roundsToWinGame rounds)
    private int[] roundWins;
    private bool  gameOver;

    // Card pick UI controller nav state
    private bool cardPickActive;
    private int cardPickSelection;
    private int cardPickLoserIndex;
    private GameObject cardPickCanvasGo;
    private GameObject[] cardPickCardGOs;
    private PowerCardData[] cardPickOffers;
    private System.Action<int> cardPickConfirmAction; // called with selected index
    private float cardPickNavCooldown;

    // Game over UI controller nav state
    private bool gameOverUIActive;
    private int gameOverSelection;
    private GameObject[] gameOverButtonGOs;
    private System.Action[] gameOverActions;
    private float gameOverNavCooldown;

    // =========================================================
    // Startup
    // =========================================================

    private void Start()
    {
        // Override inspector fields with menu selections if available
        if (GameSetupData.HasSetup)
        {
            if (GameSetupData.PlayerClasses.Length > 0 && GameSetupData.PlayerClasses[0] != null)
                player1ClassData = GameSetupData.PlayerClasses[0];
            if (GameSetupData.PlayerClasses.Length > 1 && GameSetupData.PlayerClasses[1] != null)
                player2ClassData = GameSetupData.PlayerClasses[1];
            if (GameSetupData.RoundsToWin.HasValue)
                roundsToWinGame = GameSetupData.RoundsToWin.Value;
            if (GameSetupData.CardOptionsPerPick.HasValue)
                cardOfferCount = GameSetupData.CardOptionsPerPick.Value;
            GameSetupData.Clear();
        }

        if (player1Prefab == null && player2Prefab == null)
        {
            Debug.LogError("BoxArenaBuilder: At least one player prefab must be assigned!", this);
            return;
        }

        BuildBoxArena();
        spawnPoints = CreateSpawnPoints();
        multiCam    = SetupCamera();

        new GameObject("SpellEffectRegistry").AddComponent<SpellEffectRegistry>();

        // Auto-populate card pools if not set in inspector
        AutoPopulateCardPools();

        // UI clicks require an EventSystem. Create one if none exists.
        // StandaloneInputModule reads Unity's legacy Input.mousePosition so mouse
        // clicks on UI buttons work regardless of which gameplay input backend is used.
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        players        = new GameObject[2];
        playerHealth   = new HealthSystem[2];
        roundWins      = new int[2];
        killsThisRound = new int[2];

        players[0] = SpawnPlayer(0, player1Prefab ?? player2Prefab, player1ClassData);
        players[1] = SpawnPlayer(1, player2Prefab ?? player1Prefab, player2ClassData);

        var hud = gameObject.AddComponent<PlayerHUDOverlay>();
        hud.Initialize(players);

        roundTimer = new GameObject("RoundTimer").AddComponent<RoundTimerUI>();

        StartCoroutine(BeginFirstFightNextFrame());
    }

    // =========================================================
    // Debug Card Shop (Select button / G key to open)
    // =========================================================

    private GameObject debugShopCanvas;
    private int debugShopPlayerIndex;
    private int debugShopSelection;
    private PowerCardData[] debugShopCards;
    private GameObject[] debugShopCardGOs;
    private Text debugShopPlayerLabel;
    private float debugNavCooldown;

    private void Update()
    {
        // Open: G key or Rewired system player Start/Menu button
        bool selectPressed = UnityEngine.Input.GetKeyDown(KeyCode.G);

        // Check all Rewired players for a Start button press
        for (int i = 0; i < ReInput.players.playerCount && !selectPressed; i++)
        {
            var p = ReInput.players.GetPlayer(i);
            if (p != null)
            {
                // Check common "start/select/menu" buttons by element ID
                foreach (var j in p.controllers.Joysticks)
                {
                    // Button 6 = Back/Select, Button 7 = Start on most gamepads
                    if (j.GetButtonDown(6) || j.GetButtonDown(7))
                    {
                        selectPressed = true;
                        break;
                    }
                }
            }
        }

        if (selectPressed && players != null && debugShopCanvas == null)
        {
            debugShopPlayerIndex = 0;
            OpenDebugShop();
        }
        else if (debugShopCanvas != null)
        {
            UpdateDebugShopInput();
        }

        // Card pick controller navigation
        if (cardPickActive)
            UpdateCardPickInput();

        // Game over controller navigation
        if (gameOverUIActive)
            UpdateGameOverInput();
    }

    private void OpenDebugShop()
    {
        var loaded = new System.Collections.Generic.List<PowerCardData>(
            Resources.LoadAll<PowerCardData>("Cards"));
        EnsureRuntimeCards(loaded);
        debugShopCards = loaded.ToArray();
        if (debugShopCards.Length == 0) return;

        debugShopSelection = 0;
        debugNavCooldown = 0f;

        // ---- Canvas ----
        debugShopCanvas = new GameObject("DebugCardShop");
        var canvas = debugShopCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = debugShopCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // ---- Dim overlay ----
        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(debugShopCanvas.transform, false);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.85f);
        var overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // ---- Header ----
        CreateLabel(overlay.transform, "DEBUG CARD SHOP",
            new Vector2(0f, 260f), 36, Color.yellow);

        string playerName = debugShopPlayerIndex == 0 ? "Player 1" : "Player 2";
        var playerLabelGO = CreateLabel(overlay.transform,
            $"Granting to: {playerName}  [Bumper/Tab to switch]",
            new Vector2(0f, 210f), 20, PlayerColors[debugShopPlayerIndex]);
        debugShopPlayerLabel = playerLabelGO.GetComponentInChildren<Text>();

        // ---- Instructions ----
        CreateLabel(overlay.transform,
            "D-Pad/Stick: Navigate  |  A/Jump: Grant  |  Select/G: Close",
            new Vector2(0f, 175f), 16, Color.gray);

        // ---- Card grid ----
        float cardW = 220f;
        float cardH = 270f;
        float spacing = 16f;
        int cols = Mathf.Min(debugShopCards.Length, 5);
        float totalW = cols * cardW + (cols - 1) * spacing;
        float startX = -totalW / 2f + cardW / 2f;
        float startY = 100f;

        debugShopCardGOs = new GameObject[debugShopCards.Length];
        for (int i = 0; i < debugShopCards.Length; i++)
        {
            var card = debugShopCards[i];
            int row = i / cols;
            int col = i % cols;
            float xPos = startX + col * (cardW + spacing);
            float yPos = startY - row * (cardH + spacing);

            var go = CreateDebugCardVisual(overlay.transform, card,
                new Vector2(xPos, yPos), new Vector2(cardW, cardH));
            debugShopCardGOs[i] = go;
        }

        UpdateDebugShopHighlight();
    }

    private void UpdateDebugShopInput()
    {
        debugNavCooldown -= Time.unscaledDeltaTime;

        // Use Rewired player 0 for navigation (the player operating the menu)
        var rw = ReInput.players.GetPlayer(0);
        float h = 0f, v = 0f;
        bool confirmBtn = false;
        bool startBtn = false;

        if (rw != null)
        {
            h = rw.GetAxis("Move Horizontal");
            v = rw.GetAxis("Move Vertical");
            confirmBtn = rw.GetButtonDown("Jump");

            // Check raw joystick buttons for start/select/bumpers
            foreach (var j in rw.controllers.Joysticks)
            {
                if (j.GetButtonDown(6) || j.GetButtonDown(7)) startBtn = true;
                if (j.GetButtonDown(4) || j.GetButtonDown(5))
                {
                    debugShopPlayerIndex = 1 - debugShopPlayerIndex;
                    string pName = debugShopPlayerIndex == 0 ? "Player 1" : "Player 2";
                    if (debugShopPlayerLabel != null)
                    {
                        debugShopPlayerLabel.text = $"Granting to: {pName}  [Bumper/Tab to switch]";
                        debugShopPlayerLabel.color = PlayerColors[debugShopPlayerIndex];
                    }
                }
            }
        }

        // Keyboard fallback
        if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) h = 1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) h = -1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) v = 1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) v = -1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.D)) h = 1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.A)) h = -1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.W)) v = 1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.S)) v = -1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
            confirmBtn = true;
        if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
        {
            debugShopPlayerIndex = 1 - debugShopPlayerIndex;
            string pName = debugShopPlayerIndex == 0 ? "Player 1" : "Player 2";
            if (debugShopPlayerLabel != null)
            {
                debugShopPlayerLabel.text = $"Granting to: {pName}  [Bumper/Tab to switch]";
                debugShopPlayerLabel.color = PlayerColors[debugShopPlayerIndex];
            }
        }

        // Navigation
        int cols = Mathf.Min(debugShopCards.Length, 5);
        bool moved = false;

        if (debugNavCooldown <= 0f)
        {
            if (h > 0.5f) { debugShopSelection++; moved = true; }
            else if (h < -0.5f) { debugShopSelection--; moved = true; }
            else if (v > 0.5f) { debugShopSelection -= cols; moved = true; }
            else if (v < -0.5f) { debugShopSelection += cols; moved = true; }

            if (moved)
            {
                debugShopSelection = Mathf.Clamp(debugShopSelection, 0, debugShopCards.Length - 1);
                debugNavCooldown = 0.2f;
                UpdateDebugShopHighlight();
            }
        }

        if (Mathf.Abs(h) < 0.3f && Mathf.Abs(v) < 0.3f)
            debugNavCooldown = 0f;

        // Confirm selection
        if (confirmBtn)
        {
            var card = debugShopCards[debugShopSelection];
            var inv = players[debugShopPlayerIndex]?.GetComponent<CardInventory>();
            if (inv != null)
            {
                inv.AddCard(card);
                string pn = debugShopPlayerIndex == 0 ? "P1" : "P2";
                Debug.Log($"[DEBUG] Granted '{card.cardName}' to {pn}");
            }
            CloseDebugShop();
            return;
        }

        // Close
        if (startBtn || UnityEngine.Input.GetKeyDown(KeyCode.Escape)
                     || UnityEngine.Input.GetKeyDown(KeyCode.G))
        {
            CloseDebugShop();
        }
    }

    private void UpdateDebugShopHighlight()
    {
        for (int i = 0; i < debugShopCardGOs.Length; i++)
        {
            if (debugShopCardGOs[i] == null) continue;
            var img = debugShopCardGOs[i].GetComponent<Image>();
            if (img == null) continue;

            var card = debugShopCards[i];
            if (i == debugShopSelection)
            {
                // Highlight selected card with bright border color
                img.color = new Color(card.cardColor.r * 0.6f,
                                      card.cardColor.g * 0.6f,
                                      card.cardColor.b * 0.6f, 1f);
            }
            else
            {
                img.color = new Color(card.cardColor.r * 0.2f,
                                      card.cardColor.g * 0.2f,
                                      card.cardColor.b * 0.2f, 1f);
            }
        }
    }

    private void CloseDebugShop()
    {
        if (debugShopCanvas != null)
            Destroy(debugShopCanvas);
        debugShopCanvas = null;
        debugShopCards = null;
        debugShopCardGOs = null;
    }

    private GameObject CreateDebugCardVisual(Transform parent, PowerCardData card,
                                              Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(card.cardName + "_Card");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(card.cardColor.r * 0.2f,
                              card.cardColor.g * 0.2f,
                              card.cardColor.b * 0.2f, 1f);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        // Accent bar
        var bar = new GameObject("AccentBar");
        bar.transform.SetParent(go.transform, false);
        var barImg = bar.AddComponent<Image>();
        barImg.color = card.cardColor;
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.offsetMin = new Vector2(0f, -8f);
        barRt.offsetMax = Vector2.zero;

        // Card name
        CreateCardLabel(go.transform, card.cardName,
            new Vector2(0f, size.y * 0.33f), new Vector2(size.x - 16f, 50f),
            20, FontStyle.Bold, Color.white);

        // Positive
        CreateCardLabel(go.transform, card.positiveDescription,
            new Vector2(0f, 0f), new Vector2(size.x - 20f, size.y * 0.34f),
            13, FontStyle.Normal, new Color(0.4f, 1f, 0.5f));

        // Negative
        CreateCardLabel(go.transform, card.negativeDescription,
            new Vector2(0f, -size.y * 0.35f), new Vector2(size.x - 20f, size.y * 0.28f),
            13, FontStyle.Normal, new Color(1f, 0.4f, 0.4f));

        return go;
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

        if (roundTimer != null) roundTimer.StartTimer();

        RespawnAndSubscribe();
    }

    private void EndRound(int roundWinnerIndex, int roundLoserIndex)
    {
        roundWins[roundWinnerIndex]++;
        Debug.Log($"BoxArenaBuilder: Round {currentRound} won by P{roundWinnerIndex + 1}! " +
                  $"Game score — P1: {roundWins[0]}  P2: {roundWins[1]}");

        if (roundTimer != null) roundTimer.StopTimer();

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
    // Card Pool
    // =========================================================

    /// <summary>
    /// If card arrays are empty or missing, load all PowerCardData assets
    /// from Resources/Cards so every card is available in the draft pool.
    /// Also creates runtime-only card definitions for cards not yet baked as assets.
    /// </summary>
    private void AutoPopulateCardPools()
    {
        var loaded = new List<PowerCardData>(Resources.LoadAll<PowerCardData>("Cards"));
        EnsureRuntimeCards(loaded);

        var allCards = loaded.ToArray();
        if (allCards.Length == 0) return;

        if (player1Cards == null || player1Cards.Length == 0)
            player1Cards = allCards;
        if (player2Cards == null || player2Cards.Length == 0)
            player2Cards = allCards;
    }

    /// <summary>
    /// Creates any missing card definitions at runtime so they don't
    /// require running an editor batch tool.
    /// </summary>
    private static void EnsureRuntimeCards(List<PowerCardData> cards)
    {
        if (!cards.Any(c => c.specialBehaviorID == "charge_shot"))
        {
            var card = ScriptableObject.CreateInstance<PowerCardData>();
            card.cardName = "Charge Shot";
            card.positiveDescription = "\u2726 Hold shoot to charge \u2014 more ammo = bigger, stronger shot";
            card.negativeDescription = "\u2717 Can't rapid fire \u2014 must hold and release";
            card.tier = 1;
            card.classTags = new string[] { "General" };
            card.positiveEffects = new StatModifier[0];
            card.negativeEffects = new StatModifier[0];
            card.stackCap = 1;
            card.hasSpecialBehavior = true;
            card.specialBehaviorID = "charge_shot";
            card.cardColor = new Color(1f, 0.5f, 0f, 1f);
            cards.Add(card);
        }
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

        // ---- Instructions ----
        CreateLabel(overlay.transform,
            "Left/Right: Navigate  |  A/Jump/Space: Select",
            new Vector2(0f, 80f), 16, Color.gray);

        // ---- Card buttons ----
        float cardW    = 240f;
        float cardH    = 300f;
        float spacing  = 20f;
        float totalW   = offers.Length * cardW + (offers.Length - 1) * spacing;
        float startX   = -totalW / 2f + cardW / 2f;

        // Store refs for controller nav
        cardPickCanvasGo = canvasGo;
        cardPickOffers = offers;
        cardPickCardGOs = new GameObject[offers.Length];
        cardPickSelection = 0;
        cardPickLoserIndex = loserIndex;
        cardPickNavCooldown = 0.3f; // initial cooldown to prevent accidental input

        cardPickConfirmAction = (selectedIndex) =>
        {
            var card = offers[selectedIndex];
            var inventory = players[loserIndex]?.GetComponent<CardInventory>();
            inventory?.AddCard(card);
            Debug.Log($"BoxArenaBuilder: {loserName} picked: {card.cardName}");
            cardPickActive = false;
            Destroy(canvasGo);
            onComplete();
        };

        for (int i = 0; i < offers.Length; i++)
        {
            var card       = offers[i];
            float xPos     = startX + i * (cardW + spacing);
            int capturedIndex = i;

            CreateCardButton(overlay.transform, card, new Vector2(xPos, -40f),
                new Vector2(cardW, cardH), () =>
                {
                    if (cardPickActive)
                        cardPickConfirmAction(capturedIndex);
                });

            // Store the card GO (it's the last child added to overlay)
            cardPickCardGOs[i] = overlay.transform.GetChild(overlay.transform.childCount - 1).gameObject;
        }

        cardPickActive = true;
        UpdateCardPickHighlight();
    }

    private void UpdateCardPickInput()
    {
        cardPickNavCooldown -= Time.unscaledDeltaTime;

        // Poll the loser's Rewired player for input
        float h = 0f;
        bool confirmBtn = false;

        if (ReInput.isReady)
        {
            var rw = ReInput.players.GetPlayer(cardPickLoserIndex);
            if (rw != null)
            {
                h = rw.GetAxis("Move Horizontal");
                // Accept Jump (South/A) or Shoot (East/X) as confirm
                if (rw.GetButtonDown("Jump") || rw.GetButtonDown("Shoot"))
                    confirmBtn = true;
            }
        }

        // Keyboard fallback
        if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || UnityEngine.Input.GetKeyDown(KeyCode.D)) h = 1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || UnityEngine.Input.GetKeyDown(KeyCode.A)) h = -1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
            confirmBtn = true;

        // Navigation
        if (cardPickNavCooldown <= 0f)
        {
            bool moved = false;
            if (h > 0.5f) { cardPickSelection++; moved = true; }
            else if (h < -0.5f) { cardPickSelection--; moved = true; }

            if (moved)
            {
                cardPickSelection = Mathf.Clamp(cardPickSelection, 0, cardPickOffers.Length - 1);
                cardPickNavCooldown = 0.2f;
                UpdateCardPickHighlight();
            }
        }

        if (Mathf.Abs(h) < 0.3f)
            cardPickNavCooldown = 0f;

        // Confirm
        if (confirmBtn)
            cardPickConfirmAction?.Invoke(cardPickSelection);
    }

    private void UpdateCardPickHighlight()
    {
        for (int i = 0; i < cardPickCardGOs.Length; i++)
        {
            if (cardPickCardGOs[i] == null) continue;
            var img = cardPickCardGOs[i].GetComponent<Image>();
            if (img == null) continue;

            var card = cardPickOffers[i];
            if (i == cardPickSelection)
            {
                img.color = new Color(card.cardColor.r * 0.45f,
                                      card.cardColor.g * 0.45f,
                                      card.cardColor.b * 0.45f, 1f);
            }
            else
            {
                img.color = new Color(card.cardColor.r * 0.15f,
                                      card.cardColor.g * 0.15f,
                                      card.cardColor.b * 0.15f, 1f);
            }
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

        // Instructions
        CreateLabel(panel.transform,
            "Left/Right: Navigate  |  A/Jump/Space: Select",
            new Vector2(0f, -20f), 16, Color.gray);

        // Set up game over controller nav
        gameOverButtonGOs = new GameObject[3];
        gameOverActions = new System.Action[3];

        gameOverActions[0] = () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        gameOverActions[1] = () => SceneManager.LoadScene("MainMenu");
        gameOverActions[2] = () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        };

        // Retry button
        gameOverButtonGOs[0] = CreateButton(panel.transform, "Retry", new Vector2(-160f, -60f), gameOverActions[0]);
        // Menu button
        gameOverButtonGOs[1] = CreateButton(panel.transform, "Menu", new Vector2(0f, -60f), gameOverActions[1]);
        // Quit button
        gameOverButtonGOs[2] = CreateButton(panel.transform, "Quit", new Vector2(160f, -60f), gameOverActions[2]);

        gameOverSelection = 0;
        gameOverNavCooldown = 0.3f;
        gameOverUIActive = true;
        UpdateGameOverHighlight();
    }

    private void UpdateGameOverInput()
    {
        gameOverNavCooldown -= Time.unscaledDeltaTime;

        float h = 0f;
        bool confirmBtn = false;

        // Poll all Rewired players (either player can navigate)
        if (ReInput.isReady)
        {
            for (int i = 0; i < ReInput.players.playerCount; i++)
            {
                var rw = ReInput.players.GetPlayer(i);
                if (rw != null)
                {
                    float axis = rw.GetAxis("Move Horizontal");
                    if (Mathf.Abs(axis) > Mathf.Abs(h)) h = axis;
                    if (rw.GetButtonDown("Jump") || rw.GetButtonDown("Shoot")) confirmBtn = true;
                }
            }
        }

        // Keyboard fallback
        if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || UnityEngine.Input.GetKeyDown(KeyCode.D)) h = 1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || UnityEngine.Input.GetKeyDown(KeyCode.A)) h = -1f;
        if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
            confirmBtn = true;

        // Navigation
        if (gameOverNavCooldown <= 0f)
        {
            bool moved = false;
            if (h > 0.5f) { gameOverSelection++; moved = true; }
            else if (h < -0.5f) { gameOverSelection--; moved = true; }

            if (moved)
            {
                gameOverSelection = Mathf.Clamp(gameOverSelection, 0, gameOverButtonGOs.Length - 1);
                gameOverNavCooldown = 0.2f;
                UpdateGameOverHighlight();
            }
        }

        if (Mathf.Abs(h) < 0.3f)
            gameOverNavCooldown = 0f;

        // Confirm
        if (confirmBtn)
        {
            gameOverUIActive = false;
            gameOverActions[gameOverSelection]?.Invoke();
        }
    }

    private void UpdateGameOverHighlight()
    {
        for (int i = 0; i < gameOverButtonGOs.Length; i++)
        {
            if (gameOverButtonGOs[i] == null) continue;
            var img = gameOverButtonGOs[i].GetComponent<Image>();
            if (img == null) continue;

            if (i == gameOverSelection)
                img.color = new Color(0.28f, 0.28f, 0.35f);
            else
                img.color = new Color(0.15f, 0.15f, 0.18f);
        }
    }

    private static GameObject CreateLabel(Transform parent, string text,
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

        return go;
    }

    private static GameObject CreateButton(Transform parent, string label,
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

        return go;
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

        var go  = Instantiate(prefab, spawnPos, Quaternion.identity);
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

        if (go.GetComponent<AimController>() == null)
            go.AddComponent<AimController>();

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
