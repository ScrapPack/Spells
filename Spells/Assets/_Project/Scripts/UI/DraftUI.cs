using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for the power card draft between rounds.
/// Shows 4 card options for the current drafter.
/// Each card displays name, positive effect, negative effect, tier, and class tag.
/// </summary>
public class DraftUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private GameObject draftPanel;
    [SerializeField] private TextMeshProUGUI drafterNameText;
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Card Slots")]
    [SerializeField] private DraftCardSlot[] cardSlots;

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;

    private DraftManager draftManager;
    private int currentDrafterID;
    private PowerCardData[] currentOptions;
    private float draftTimer;
    private float draftTimeLimit;

    public void Initialize(DraftManager manager, float timeLimit)
    {
        draftManager = manager;
        draftTimeLimit = timeLimit;

        if (manager != null)
        {
            manager.OnDraftStart.AddListener(OnDraftStart);
            manager.OnShowOptions.AddListener(OnShowOptions);
            manager.OnDraftComplete.AddListener(OnDraftComplete);
        }

        Hide();
    }

    private void OnDraftStart()
    {
        if (draftPanel != null)
            draftPanel.SetActive(true);
    }

    private void OnShowOptions(int playerID, PowerCardData[] options)
    {
        currentDrafterID = playerID;
        currentOptions = options;
        draftTimer = draftTimeLimit;

        if (drafterNameText != null)
            drafterNameText.text = $"Player {playerID + 1} — Pick a card";
        if (instructionText != null)
            instructionText.text = "Every card has a price...";

        // Populate card slots
        for (int i = 0; i < 4; i++)
        {
            if (i < cardSlots.Length && cardSlots[i] != null)
            {
                if (i < options.Length)
                    cardSlots[i].Show(options[i]);
                else
                    cardSlots[i].Hide();
            }
        }
    }

    /// <summary>
    /// Called when a card slot is clicked.
    /// </summary>
    public void OnCardSelected(int slotIndex)
    {
        if (draftManager == null || currentOptions == null) return;

        // Apply card to player's inventory
        var players = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.PlayerID == currentDrafterID)
            {
                var inventory = player.GetComponent<CardInventory>();
                if (inventory != null && slotIndex < currentOptions.Length)
                {
                    inventory.AddCard(currentOptions[slotIndex]);
                }
                break;
            }
        }

        draftManager.PickCard(currentDrafterID, slotIndex, currentOptions);
    }

    private void OnDraftComplete()
    {
        Hide();
    }

    private void Hide()
    {
        if (draftPanel != null)
            draftPanel.SetActive(false);
    }

    private void Update()
    {
        if (draftPanel == null || !draftPanel.activeSelf) return;

        // Timer
        if (draftTimeLimit > 0f && draftTimer > 0f)
        {
            draftTimer -= Time.deltaTime;
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(draftTimer).ToString();

            // Auto-pick if time runs out
            if (draftTimer <= 0f && currentOptions != null && currentOptions.Length > 0)
            {
                OnCardSelected(Random.Range(0, currentOptions.Length));
            }
        }
    }
}
