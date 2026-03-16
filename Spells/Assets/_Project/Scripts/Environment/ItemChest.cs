using UnityEngine;

/// <summary>
/// Trigger-based item chest that grants a random item on first player contact.
/// Opens once per round. Item selection is weighted and filtered by level pool.
/// Grants item via TemporaryItemInventory on the player.
/// </summary>
public class ItemChest : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private bool isOpened;

    private ItemData[] availableItems;
    private int levelPool;

    /// <summary>
    /// Initialize chest with available items and current level pool.
    /// Called by ChestSpawnManager after creation.
    /// </summary>
    public void Initialize(ItemData[] items, int currentLevelPool)
    {
        availableItems = items;
        levelPool = currentLevelPool;
        isOpened = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpened) return;

        // Only players can open chests
        var identity = other.GetComponent<PlayerIdentity>();
        if (identity == null) return;

        var health = other.GetComponent<HealthSystem>();
        if (health == null || !health.IsAlive) return;

        // Pick a random item using weighted selection
        ItemData picked = PickWeightedItem();
        if (picked == null) return;

        // Grant to player's temporary inventory
        var inventory = other.GetComponent<TemporaryItemInventory>();
        if (inventory != null)
        {
            inventory.AddItem(picked);
        }

        isOpened = true;

        // Visual feedback: change color to indicate opened
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);

        Debug.Log($"Player {identity.PlayerID + 1} opened chest and got: {picked.itemName}");
    }

    private ItemData PickWeightedItem()
    {
        if (availableItems == null || availableItems.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var item in availableItems)
        {
            if (item != null)
                totalWeight += item.dropWeight;
        }

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var item in availableItems)
        {
            if (item == null) continue;
            cumulative += item.dropWeight;
            if (roll <= cumulative)
                return item;
        }

        return availableItems[availableItems.Length - 1];
    }
}
