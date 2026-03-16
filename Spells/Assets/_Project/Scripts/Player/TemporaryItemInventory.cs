using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds temporary items on a player. Items are lost on death.
/// Attached to the player prefab alongside HealthSystem.
///
/// On death (HealthSystem.OnDeath): clears all items by calling
/// each item's behavior OnUnequip(), then removes them from the list.
///
/// Items can also be removed manually (ammo runs out, etc.).
/// </summary>
public class TemporaryItemInventory : MonoBehaviour
{
    public int ItemCount => activeItems.Count;

    private readonly List<ActiveItem> activeItems = new List<ActiveItem>();
    private HealthSystem healthSystem;

    /// <summary>
    /// Tracks an active item: its data and instantiated behavior component.
    /// </summary>
    private class ActiveItem
    {
        public ItemData data;
        public ItemBehavior behavior;
        public int remainingAmmo;
    }

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
    }

    private void OnEnable()
    {
        if (healthSystem != null)
            healthSystem.OnDeath.AddListener(OnPlayerDied);
    }

    private void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.OnDeath.RemoveListener(OnPlayerDied);
    }

    /// <summary>
    /// Add a temporary item to this player.
    /// </summary>
    public void AddItem(ItemData itemData)
    {
        if (itemData == null) return;

        var activeItem = new ActiveItem
        {
            data = itemData,
            remainingAmmo = itemData.ammo
        };

        // Instantiate behavior if registered
        if (!string.IsNullOrEmpty(itemData.behaviorID))
        {
            var behaviorType = ItemBehaviorRegistry.GetBehaviorType(itemData.behaviorID);
            if (behaviorType != null)
            {
                var behavior = (ItemBehavior)gameObject.AddComponent(behaviorType);
                behavior.Initialize(this, itemData);
                behavior.OnEquip();
                activeItem.behavior = behavior;
            }
        }

        activeItems.Add(activeItem);
    }

    /// <summary>
    /// Remove a specific item. Calls OnUnequip on its behavior.
    /// </summary>
    public void RemoveItem(ItemData itemData)
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (activeItems[i].data == itemData)
            {
                RemoveItemAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// Use one ammo charge for an item. Auto-removes when ammo depleted.
    /// Returns false if item has no ammo or isn't found.
    /// </summary>
    public bool UseAmmo(ItemData itemData)
    {
        foreach (var item in activeItems)
        {
            if (item.data == itemData && item.remainingAmmo > 0)
            {
                item.remainingAmmo--;
                if (item.remainingAmmo <= 0 && itemData.ammo > 0)
                {
                    RemoveItem(itemData);
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if player has a specific item.
    /// </summary>
    public bool HasItem(ItemData itemData)
    {
        foreach (var item in activeItems)
        {
            if (item.data == itemData) return true;
        }
        return false;
    }

    /// <summary>
    /// Check if player has an item with a specific behavior ID.
    /// </summary>
    public bool HasItemWithBehavior(string behaviorID)
    {
        foreach (var item in activeItems)
        {
            if (item.data != null && item.data.behaviorID == behaviorID) return true;
        }
        return false;
    }

    /// <summary>
    /// Clear all items — called on death.
    /// </summary>
    public void ClearAll()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            RemoveItemAt(i);
        }
        activeItems.Clear();
    }

    private void RemoveItemAt(int index)
    {
        var item = activeItems[index];
        if (item.behavior != null)
        {
            item.behavior.OnUnequip();
            Destroy(item.behavior);
        }
        activeItems.RemoveAt(index);
    }

    private void OnPlayerDied()
    {
        ClearAll();
    }
}
