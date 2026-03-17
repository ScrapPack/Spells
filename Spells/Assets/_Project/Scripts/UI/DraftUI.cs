using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for the power card draft between rounds.
/// Shows card options for the current drafter.
/// Call ShowOptions() to display cards, OnCardSelected() handles pick.
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

    private int currentDrafterID;
    private PowerCardData[] currentOptions;
    private float draftTimer;
    private float draftTimeLimit;

    public void ShowOptions(int playerID, PowerCardData[] options, float timeLimit = 0f)
    {
        currentDrafterID = playerID;
        currentOptions   = options;
        draftTimeLimit   = timeLimit;
        draftTimer       = timeLimit;

        if (drafterNameText != null)
            drafterNameText.text = $"Player {playerID + 1} — Pick a card";
        if (instructionText != null)
            instructionText.text = "Every card has a price...";

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

        if (draftPanel != null)
            draftPanel.SetActive(true);
    }

    public void Hide()
    {
        if (draftPanel != null)
            draftPanel.SetActive(false);
    }

    /// <summary>
    /// Called when a card slot is clicked. Applies the card and hides the panel.
    /// </summary>
    public void OnCardSelected(int slotIndex)
    {
        if (currentOptions == null || slotIndex >= currentOptions.Length) return;

        var players = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.PlayerID == currentDrafterID)
            {
                var inventory = player.GetComponent<CardInventory>();
                if (inventory != null)
                    inventory.AddCard(currentOptions[slotIndex]);
                break;
            }
        }

        Hide();
    }

    private void Update()
    {
        if (draftPanel == null || !draftPanel.activeSelf) return;

        if (draftTimeLimit > 0f && draftTimer > 0f)
        {
            draftTimer -= Time.deltaTime;
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(draftTimer).ToString();

            if (draftTimer <= 0f && currentOptions != null && currentOptions.Length > 0)
                OnCardSelected(Random.Range(0, currentOptions.Length));
        }
    }
}
