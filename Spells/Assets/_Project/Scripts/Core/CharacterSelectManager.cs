using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages class selection before a match starts.
/// Each player picks from available ClassData options.
/// Supports ready-up: match starts when all players confirm.
///
/// Can be configured to allow or disallow duplicate class picks.
/// Integrates with MatchManager.Setup → CharSelect states.
/// </summary>
public class CharacterSelectManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ClassData[] availableClasses;
    [SerializeField] private bool allowDuplicates = true;
    [SerializeField] private float readyCountdown = 3f;

    [Header("Events")]
    public UnityEvent OnCharSelectStart;
    public UnityEvent<int, ClassData> OnClassSelected;      // (playerID, class)
    public UnityEvent<int> OnPlayerReady;                    // (playerID)
    public UnityEvent OnAllReady;                            // all players confirmed
    public UnityEvent OnCharSelectComplete;

    public bool IsActive { get; private set; }
    public int RegisteredCount => playerSelections.Count;

    private readonly Dictionary<int, ClassData> playerSelections = new Dictionary<int, ClassData>();
    private readonly HashSet<int> readyPlayers = new HashSet<int>();
    private readonly Dictionary<int, int> selectionIndex = new Dictionary<int, int>();
    private float countdownTimer;
    private bool countdownActive;

    /// <summary>
    /// Start character selection. Called by MatchManager.
    /// </summary>
    public void BeginSelection(List<int> playerIDs)
    {
        IsActive = true;
        playerSelections.Clear();
        readyPlayers.Clear();
        selectionIndex.Clear();
        countdownActive = false;

        foreach (int id in playerIDs)
        {
            selectionIndex[id] = 0;
            // Default to first available class
            if (availableClasses != null && availableClasses.Length > 0)
            {
                playerSelections[id] = availableClasses[0];
                OnClassSelected?.Invoke(id, availableClasses[0]);
            }
        }

        OnCharSelectStart?.Invoke();
    }

    /// <summary>
    /// Player cycles to next class. Called by input handler.
    /// </summary>
    public void CycleClass(int playerID, int direction)
    {
        if (!IsActive || availableClasses == null || availableClasses.Length == 0) return;
        if (readyPlayers.Contains(playerID)) return; // Can't change after readying

        if (!selectionIndex.ContainsKey(playerID)) return;

        int idx = selectionIndex[playerID];
        idx = (idx + direction + availableClasses.Length) % availableClasses.Length;

        // Skip taken classes if duplicates not allowed
        if (!allowDuplicates)
        {
            int attempts = 0;
            while (IsClassTaken(availableClasses[idx], playerID) && attempts < availableClasses.Length)
            {
                idx = (idx + direction + availableClasses.Length) % availableClasses.Length;
                attempts++;
            }
        }

        selectionIndex[playerID] = idx;
        playerSelections[playerID] = availableClasses[idx];
        OnClassSelected?.Invoke(playerID, availableClasses[idx]);
    }

    /// <summary>
    /// Player confirms their class choice.
    /// </summary>
    public void ReadyUp(int playerID)
    {
        if (!IsActive) return;
        if (!playerSelections.ContainsKey(playerID)) return;

        readyPlayers.Add(playerID);
        OnPlayerReady?.Invoke(playerID);

        // Check if all players are ready
        if (readyPlayers.Count >= playerSelections.Count)
        {
            countdownActive = true;
            countdownTimer = readyCountdown;
            OnAllReady?.Invoke();
        }
    }

    /// <summary>
    /// Player unreadies (can change class again).
    /// </summary>
    public void Unready(int playerID)
    {
        if (!IsActive) return;
        readyPlayers.Remove(playerID);
        countdownActive = false;
    }

    private void Update()
    {
        if (!IsActive || !countdownActive) return;

        countdownTimer -= Time.deltaTime;
        if (countdownTimer <= 0f)
        {
            CompleteSelection();
        }
    }

    private void CompleteSelection()
    {
        IsActive = false;
        countdownActive = false;
        OnCharSelectComplete?.Invoke();
    }

    /// <summary>
    /// Get the class a player has selected.
    /// </summary>
    public ClassData GetSelection(int playerID)
    {
        return playerSelections.ContainsKey(playerID) ? playerSelections[playerID] : null;
    }

    /// <summary>
    /// Get all selections (playerID → ClassData).
    /// </summary>
    public Dictionary<int, ClassData> GetAllSelections()
    {
        return new Dictionary<int, ClassData>(playerSelections);
    }

    private bool IsClassTaken(ClassData classData, int excludePlayerID)
    {
        foreach (var kvp in playerSelections)
        {
            if (kvp.Key != excludePlayerID && kvp.Value == classData)
                return true;
        }
        return false;
    }
}
