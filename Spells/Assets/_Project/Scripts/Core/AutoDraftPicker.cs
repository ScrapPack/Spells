using System.Collections;
using UnityEngine;

/// <summary>
/// Automatically picks a random card during the draft phase.
/// Used by BoxArenaScene (and any scene without a full DraftUI) so the
/// game loop completes without requiring UI interaction.
///
/// Mimics DraftUI.OnCardSelected: applies the card to the drafter's
/// CardInventory and calls DraftManager.PickCard().
/// </summary>
public class AutoDraftPicker : MonoBehaviour
{
    [Tooltip("Seconds to wait before auto-picking (gives time to read the log)")]
    [SerializeField] private float pickDelay = 1.5f;

    private DraftManager draftManager;

    public void Initialize(DraftManager manager)
    {
        draftManager = manager;
        manager.OnShowOptions.AddListener(OnShowOptions);
    }

    private void OnShowOptions(int playerID, PowerCardData[] options)
    {
        if (options == null || options.Length == 0) return;
        StartCoroutine(AutoPick(playerID, options));
    }

    private IEnumerator AutoPick(int playerID, PowerCardData[] options)
    {
        yield return new WaitForSeconds(pickDelay);

        int slotIndex = Random.Range(0, options.Length);
        PowerCardData picked = options[slotIndex];

        // Apply card to the drafter's CardInventory
        var players = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.PlayerID == playerID)
            {
                var inventory = player.GetComponent<CardInventory>();
                if (inventory != null)
                    inventory.AddCard(picked);
                break;
            }
        }

        Debug.Log($"[AutoDraftPicker] Player {playerID + 1} auto-picked: {picked.cardName}");
        draftManager.PickCard(playerID, slotIndex, options);
    }
}
