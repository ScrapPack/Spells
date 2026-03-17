using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the power card draft between rounds.
/// Each loser picks one card from 4 options drawn from their class pool.
/// Draft order: first eliminated picks first (best picks for worst performance).
/// Round winner gains a level (no card).
/// </summary>
public class DraftManager : MonoBehaviour
{
    [Header("Draft Settings")]
    [Tooltip("Number of card options shown to each drafter")]
    [SerializeField] private int optionsPerPick = 4;
    [Tooltip("Percentage of draft options drawn from general pool (0-1)")]
    [SerializeField] private float generalPoolRatio = 0.4f;

    [Header("Card Database")]
    [Tooltip("All available power cards in the game")]
    [SerializeField] private PowerCardData[] allCards;

    [Header("Events")]
    public UnityEvent OnDraftStart;
    public UnityEvent<int, PowerCardData[]> OnShowOptions;    // (playerID, options)
    public UnityEvent<int, PowerCardData> OnCardPicked;        // (playerID, card)
    public UnityEvent OnDraftComplete;

    private readonly Dictionary<int, int> playerLevels = new Dictionary<int, int>();
    private List<int> draftOrder;
    private int currentDrafterIndex;

    /// <summary>
    /// Initialize player levels at match start.
    /// </summary>
    public void InitializeMatch(List<int> playerIDs)
    {
        playerLevels.Clear();
        foreach (int id in playerIDs)
            playerLevels[id] = 0;
    }

    /// <summary>
    /// Start a draft phase. Elimination order determines draft order.
    /// Winner gains a level but does not draft.
    /// </summary>
    public void StartDraft(int winnerID, List<int> eliminationOrder)
    {
        // Winner gains a level
        if (playerLevels.ContainsKey(winnerID))
            playerLevels[winnerID]++;

        // Draft order = elimination order (first eliminated picks first)
        draftOrder = new List<int>(eliminationOrder);
        currentDrafterIndex = 0;

        OnDraftStart?.Invoke();

        if (draftOrder.Count > 0)
            ShowOptionsForCurrentDrafter();
        else
            CompleteDraft();
    }

    /// <summary>
    /// Show card options to the current drafter.
    /// </summary>
    private void ShowOptionsForCurrentDrafter()
    {
        if (currentDrafterIndex >= draftOrder.Count)
        {
            CompleteDraft();
            return;
        }

        int playerID = draftOrder[currentDrafterIndex];
        PowerCardData[] options = DrawOptions(playerID);
        OnShowOptions?.Invoke(playerID, options);
    }

    /// <summary>
    /// Player picks a card. Called by UI when selection is made.
    /// </summary>
    public void PickCard(int playerID, int optionIndex, PowerCardData[] currentOptions)
    {
        if (optionIndex < 0 || optionIndex >= currentOptions.Length) return;

        PowerCardData picked = currentOptions[optionIndex];
        OnCardPicked?.Invoke(playerID, picked);

        // Advance to next drafter
        currentDrafterIndex++;
        if (currentDrafterIndex < draftOrder.Count)
            ShowOptionsForCurrentDrafter();
        else
            CompleteDraft();
    }

    private void CompleteDraft()
    {
        OnDraftComplete?.Invoke();
    }

    /// <summary>
    /// Registered player GameObjects, indexed by player ID.
    /// Set during InitializeMatch so draft can look up class tags.
    /// </summary>
    private readonly Dictionary<int, GameObject> playerObjects = new Dictionary<int, GameObject>();

    /// <summary>
    /// Register a player object so draft can look up their ClassManager.
    /// Call after ClassManager.Initialize.
    /// </summary>
    public void RegisterPlayerObject(int playerID, GameObject playerObj)
    {
        playerObjects[playerID] = playerObj;
    }

    /// <summary>
    /// Draw card options for a player based on their class and level.
    /// </summary>
    private PowerCardData[] DrawOptions(int playerID)
    {
        if (allCards == null || allCards.Length == 0)
            return new PowerCardData[0];

        // Look up actual class tags from player's ClassManager
        string[] classTags = new string[] { "General" };
        if (playerObjects.ContainsKey(playerID))
        {
            var classManager = playerObjects[playerID].GetComponent<ClassManager>();
            if (classManager != null && classManager.CurrentClass != null
                && classManager.CurrentClass.cardPoolTags != null
                && classManager.CurrentClass.cardPoolTags.Length > 0)
            {
                classTags = classManager.CurrentClass.cardPoolTags;
            }
        }
        int level = playerLevels.ContainsKey(playerID) ? playerLevels[playerID] : 0;

        // Build eligible card pools
        var generalPool = new List<PowerCardData>();
        var classPool = new List<PowerCardData>();

        foreach (var card in allCards)
        {
            if (card == null) continue;
            if (!card.IsAvailableFor(classTags, level)) continue;

            bool isGeneral = false;
            foreach (string tag in card.classTags)
            {
                if (tag == "General") { isGeneral = true; break; }
            }

            if (isGeneral)
                generalPool.Add(card);
            else
                classPool.Add(card);
        }

        // Draw from pools
        var options = new List<PowerCardData>();
        int generalCount = Mathf.RoundToInt(optionsPerPick * generalPoolRatio);
        int classCount = optionsPerPick - generalCount;

        DrawFromPool(generalPool, generalCount, options);
        DrawFromPool(classPool, classCount, options);

        // Fill remaining slots from either pool
        while (options.Count < optionsPerPick)
        {
            var combined = new List<PowerCardData>();
            combined.AddRange(generalPool);
            combined.AddRange(classPool);

            // Remove already-selected
            foreach (var opt in options)
                combined.Remove(opt);

            if (combined.Count == 0) break;
            options.Add(combined[Random.Range(0, combined.Count)]);
        }

        return options.ToArray();
    }

    private void DrawFromPool(List<PowerCardData> pool, int count, List<PowerCardData> result)
    {
        var available = new List<PowerCardData>(pool);
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int idx = Random.Range(0, available.Count);
            result.Add(available[idx]);
            available.RemoveAt(idx);
        }
    }

    /// <summary>
    /// Set the card database at runtime (called by BoxArenaBuilder before Start()).
    /// </summary>
    public void SetCardDatabase(PowerCardData[] cards)
    {
        allCards = cards;
    }

    /// <summary>
    /// Get a player's current level.
    /// </summary>
    public int GetPlayerLevel(int playerID)
    {
        return playerLevels.ContainsKey(playerID) ? playerLevels[playerID] : 0;
    }

    /// <summary>
    /// Get the sum of all player levels. Used to scale monsters and chest item pools.
    /// </summary>
    public int GetTotalLevelPool()
    {
        int total = 0;
        foreach (var kvp in playerLevels)
            total += kvp.Value;
        return total;
    }

    /// <summary>
    /// Grant a level to a player (e.g., for killing a monster).
    /// Alternative progression path — catch-up mechanic.
    /// </summary>
    public void GrantLevel(int playerID)
    {
        if (playerLevels.ContainsKey(playerID))
            playerLevels[playerID]++;
    }
}
