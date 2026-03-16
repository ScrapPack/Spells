using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Top-level game state machine.
/// Manages the full match flow: CharSelect → Round → Draft → loop → MatchEnd.
/// First to N round wins takes the match.
/// </summary>
public class MatchManager : MonoBehaviour
{
    public enum MatchState { Setup, CharSelect, RoundStart, RoundActive, Draft, MatchEnd }

    [Header("Match Settings")]
    [Tooltip("Round wins needed to win the match")]
    [SerializeField] private int winsToWinMatch = 5;

    [Header("References")]
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private DraftManager draftManager;
    [SerializeField] private MultiTargetCamera multiCamera;
    [SerializeField] private PlayerSpawnManager spawnManager;
    [SerializeField] private RoundAnnouncer announcer;
    [SerializeField] private KillFeed killFeed;
    [SerializeField] private CharacterSelectManager charSelect;
    [SerializeField] private CombatAnalytics analytics;
    [SerializeField] private MonsterSpawnManager monsterSpawnManager;
    [SerializeField] private ChestSpawnManager chestSpawnManager;
    [SerializeField] private ModularArenaBuilder arenaBuilder;

    [Header("Events")]
    public UnityEvent<MatchState> OnStateChanged;
    public UnityEvent<int> OnMatchWin;                     // (winnerPlayerID)
    public UnityEvent<Dictionary<int, int>> OnScoreUpdate; // (playerID → wins)

    public MatchState CurrentState { get; private set; }
    public int CurrentRound { get; private set; }

    private readonly Dictionary<int, int> roundWins = new Dictionary<int, int>();
    private readonly List<GameObject> playerObjects = new List<GameObject>();
    private readonly List<int> playerIDs = new List<int>();

    private void Start()
    {
        // Subscribe to round/draft events
        if (roundManager != null)
        {
            roundManager.OnRoundEnd.AddListener(OnRoundEnded);
            roundManager.OnEliminationOrder.AddListener(OnEliminationOrderReceived);
        }

        if (draftManager != null)
        {
            draftManager.OnDraftComplete.AddListener(OnDraftCompleted);
        }

        if (charSelect != null)
        {
            charSelect.OnCharSelectComplete.AddListener(OnCharSelectComplete);
        }

        ChangeState(MatchState.Setup);
    }

    /// <summary>
    /// Register a player for the match. Call during CharSelect or Setup.
    /// </summary>
    public void RegisterPlayer(GameObject playerObj, int playerID)
    {
        if (!playerIDs.Contains(playerID))
        {
            playerIDs.Add(playerID);
            roundWins[playerID] = 0;
        }

        if (!playerObjects.Contains(playerObj))
            playerObjects.Add(playerObj);

        // Register with camera
        if (multiCamera != null)
            multiCamera.AddTarget(playerObj.transform);

        // Register with draft manager for class-specific card pools
        if (draftManager != null)
            draftManager.RegisterPlayerObject(playerID, playerObj);
    }

    /// <summary>
    /// Begin character selection phase. Called after all players have joined.
    /// </summary>
    public void BeginCharacterSelect()
    {
        ChangeState(MatchState.CharSelect);

        if (charSelect != null)
            charSelect.BeginSelection(playerIDs);
    }

    /// <summary>
    /// Called when character selection is complete. Applies classes and starts match.
    /// </summary>
    private void OnCharSelectComplete()
    {
        if (charSelect == null) return;

        // Apply selected classes to all players
        var selections = charSelect.GetAllSelections();
        foreach (var kvp in selections)
        {
            int playerID = kvp.Key;
            ClassData classData = kvp.Value;

            // Find the player object
            foreach (var obj in playerObjects)
            {
                var id = obj.GetComponent<PlayerIdentity>();
                if (id != null && id.PlayerID == playerID)
                {
                    var classManager = obj.GetComponent<ClassManager>();
                    if (classManager != null)
                        classManager.Initialize(classData, playerID);
                    break;
                }
            }
        }

        StartMatch();
    }

    /// <summary>
    /// Begin the match after character selection.
    /// </summary>
    public void StartMatch()
    {
        CurrentRound = 0;

        // Initialize draft manager
        if (draftManager != null)
            draftManager.InitializeMatch(playerIDs);

        // Clear all card inventories
        foreach (var player in playerObjects)
        {
            var inventory = player.GetComponent<CardInventory>();
            if (inventory != null)
                inventory.ClearAll();
        }

        StartNextRound();
    }

    private void StartNextRound()
    {
        CurrentRound++;
        ChangeState(MatchState.RoundStart);

        // Announce round number
        if (announcer != null)
            announcer.AnnounceRound(CurrentRound);

        // Register alive players with round manager
        if (roundManager != null)
        {
            roundManager.RegisterPlayers(playerObjects);
            roundManager.StartRound();
        }

        // Spawn PvE elements
        if (arenaBuilder != null && arenaBuilder.CurrentLayout != null)
        {
            if (monsterSpawnManager != null)
                monsterSpawnManager.SpawnMonsters(arenaBuilder.CurrentLayout, arenaBuilder.MonsterSpawnPoints);
            if (chestSpawnManager != null)
                chestSpawnManager.SpawnChests(arenaBuilder.CurrentLayout, arenaBuilder.ChestSpawnPoints);
        }

        ChangeState(MatchState.RoundActive);
    }

    private void Update()
    {
        // Feed zoom progress from round manager to camera
        if (CurrentState == MatchState.RoundActive && roundManager != null && multiCamera != null)
        {
            multiCamera.SetZoomProgress(roundManager.ZoomProgress);
        }
    }

    // =========================================================
    // Event Handlers
    // =========================================================

    private int lastRoundWinner;
    private List<int> lastEliminationOrder;

    private void OnRoundEnded(int winnerID)
    {
        lastRoundWinner = winnerID;

        // Award round win
        if (winnerID >= 0 && roundWins.ContainsKey(winnerID))
        {
            roundWins[winnerID]++;
            OnScoreUpdate?.Invoke(new Dictionary<int, int>(roundWins));

            // Record round win in analytics
            if (analytics != null)
                analytics.RecordRoundWin(winnerID);

            // Announce round winner
            string winnerName = $"Player {winnerID + 1}";
            Color winnerColor = GetPlayerClassColor(winnerID);
            if (announcer != null)
                announcer.AnnounceRoundWin(winnerName, winnerColor);
            if (killFeed != null)
                killFeed.AddRoundWin(winnerName, CurrentRound, winnerColor);

            // Check for match win
            if (roundWins[winnerID] >= winsToWinMatch)
            {
                if (announcer != null)
                    announcer.AnnounceMatchWin(winnerName, winnerColor);

                ChangeState(MatchState.MatchEnd);
                OnMatchWin?.Invoke(winnerID);
                return;
            }
        }

        // Despawn PvE elements
        if (monsterSpawnManager != null)
            monsterSpawnManager.DespawnAll();
        if (chestSpawnManager != null)
            chestSpawnManager.DespawnAll();

        // Reset camera zoom
        if (multiCamera != null)
            multiCamera.SetZoomProgress(0f);

        // Wait for elimination order before starting draft
    }

    private void OnEliminationOrderReceived(List<int> eliminationOrder)
    {
        lastEliminationOrder = eliminationOrder;

        // Start draft phase
        ChangeState(MatchState.Draft);
        if (draftManager != null)
            draftManager.StartDraft(lastRoundWinner, eliminationOrder);
    }

    private void OnDraftCompleted()
    {
        // Apply picked cards (CardInventory.AddCard is called by draft UI)
        // Start next round
        StartNextRound();
    }

    private void ChangeState(MatchState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    // =========================================================
    // Queries
    // =========================================================

    public int GetRoundWins(int playerID)
    {
        return roundWins.ContainsKey(playerID) ? roundWins[playerID] : 0;
    }

    public Dictionary<int, int> GetAllScores()
    {
        return new Dictionary<int, int>(roundWins);
    }

    /// <summary>
    /// Get class color for a player. Falls back to white if not found.
    /// </summary>
    private Color GetPlayerClassColor(int playerID)
    {
        foreach (var obj in playerObjects)
        {
            var id = obj.GetComponent<PlayerIdentity>();
            if (id != null && id.PlayerID == playerID)
            {
                var cm = obj.GetComponent<ClassManager>();
                if (cm != null && cm.CurrentClass != null)
                    return cm.CurrentClass.classColor;
            }
        }
        return Color.white;
    }
}
