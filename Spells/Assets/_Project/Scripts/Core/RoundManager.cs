using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages a single round of combat.
/// Tracks alive players, detects last standing, signals round end.
/// Also manages the camera zoom compression timer.
/// </summary>
public class RoundManager : MonoBehaviour
{
    [Header("Round Settings")]
    [Tooltip("Seconds before camera zoom kicks in")]
    [SerializeField] private float zoomDelaySeconds = 30f;
    [Tooltip("Total round time before full compression")]
    [SerializeField] private float maxRoundSeconds = 90f;
    [Tooltip("If true, round ends in a draw when max time expires")]
    [SerializeField] private bool enableTimeout = true;
    [Tooltip("Extra seconds after full zoom before forced round end")]
    [SerializeField] private float overtimeSeconds = 10f;

    [Header("Events")]
    public UnityEvent OnRoundStart;
    public UnityEvent<int> OnPlayerEliminated;    // (playerID)
    public UnityEvent<int> OnRoundEnd;             // (winnerPlayerID)
    public UnityEvent<List<int>> OnEliminationOrder; // (ordered list: first eliminated → last)

    public bool RoundActive { get; private set; }
    public float RoundTimer { get; private set; }
    public float ZoomProgress { get; private set; }
    public int AliveCount => alivePlayers.Count;

    private readonly List<int> alivePlayers = new List<int>();
    private readonly List<int> eliminationOrder = new List<int>();
    private readonly Dictionary<int, HealthSystem> playerHealthSystems = new Dictionary<int, HealthSystem>();

    /// <summary>
    /// Register players for this round. Call before StartRound.
    /// </summary>
    public void RegisterPlayers(List<GameObject> players)
    {
        alivePlayers.Clear();
        eliminationOrder.Clear();
        playerHealthSystems.Clear();

        foreach (var player in players)
        {
            var identity = player.GetComponent<PlayerIdentity>();
            var health = player.GetComponent<HealthSystem>();
            var classManager = player.GetComponent<ClassManager>();

            if (identity == null || health == null) continue;

            int id = identity.PlayerID;
            alivePlayers.Add(id);
            playerHealthSystems[id] = health;

            // Subscribe to death events
            health.OnDeath.AddListener(() => OnPlayerDied(id));

            // Reset combat state for new round
            if (classManager != null)
                classManager.ResetForRound();
        }
    }

    /// <summary>
    /// Start the round. All registered players should be spawned and ready.
    /// </summary>
    public void StartRound()
    {
        RoundActive = true;
        RoundTimer = 0f;
        ZoomProgress = 0f;
        OnRoundStart?.Invoke();
    }

    private void Update()
    {
        if (!RoundActive) return;

        RoundTimer += Time.deltaTime;

        // Calculate zoom progress: 0 during delay, then linear to 1
        if (RoundTimer > zoomDelaySeconds)
        {
            float zoomTime = RoundTimer - zoomDelaySeconds;
            float zoomDuration = maxRoundSeconds - zoomDelaySeconds;
            ZoomProgress = Mathf.Clamp01(zoomTime / zoomDuration);
        }

        // Round timeout: force end after max time + overtime
        if (enableTimeout && RoundTimer >= maxRoundSeconds + overtimeSeconds)
        {
            // Timeout — determine winner by HP
            int bestPlayer = -1;
            int bestHP = -1;

            foreach (int pid in alivePlayers)
            {
                if (playerHealthSystems.ContainsKey(pid))
                {
                    int hp = playerHealthSystems[pid].CurrentHP;
                    if (hp > bestHP)
                    {
                        bestHP = hp;
                        bestPlayer = pid;
                    }
                }
            }

            // If tied HP, it's a draw (winner = -1)
            int tiedCount = 0;
            foreach (int pid in alivePlayers)
            {
                if (playerHealthSystems.ContainsKey(pid) && playerHealthSystems[pid].CurrentHP == bestHP)
                    tiedCount++;
            }

            if (tiedCount > 1)
                bestPlayer = -1; // Draw

            EndRound();
        }
    }

    private void OnPlayerDied(int playerID)
    {
        if (!RoundActive) return;
        if (!alivePlayers.Contains(playerID)) return;

        alivePlayers.Remove(playerID);
        eliminationOrder.Add(playerID);

        OnPlayerEliminated?.Invoke(playerID);

        // Check for round end: last player standing
        if (alivePlayers.Count <= 1)
        {
            EndRound();
        }
    }

    private void EndRound()
    {
        RoundActive = false;

        int winnerID = alivePlayers.Count > 0 ? alivePlayers[0] : -1;

        // Unsubscribe from death events
        foreach (var kvp in playerHealthSystems)
        {
            kvp.Value.OnDeath.RemoveAllListeners();
        }

        OnRoundEnd?.Invoke(winnerID);
        OnEliminationOrder?.Invoke(new List<int>(eliminationOrder));
    }

    /// <summary>
    /// Force end the round (timeout, etc.).
    /// </summary>
    public void ForceEndRound()
    {
        if (RoundActive)
            EndRound();
    }
}
