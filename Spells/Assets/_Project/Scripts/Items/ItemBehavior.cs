using UnityEngine;

/// <summary>
/// Abstract base for item behaviors. Attached to a player when an item is equipped.
/// Handles the lifecycle: OnEquip() when granted, OnUnequip() when removed or on death.
///
/// Concrete implementations: SpiderShoesItem, FireWandItem, HitscanGunItem.
/// Each item behavior modifies the player's state/abilities while active.
///
/// Subclasses should override OnEquip/OnUnequip at minimum.
/// Owner reference provides access to the player's other components.
/// </summary>
public abstract class ItemBehavior : MonoBehaviour
{
    public TemporaryItemInventory Inventory { get; private set; }
    public ItemData ItemData { get; private set; }

    /// <summary>
    /// Initialize references. Called by TemporaryItemInventory before OnEquip.
    /// </summary>
    public void Initialize(TemporaryItemInventory inventory, ItemData data)
    {
        Inventory = inventory;
        ItemData = data;
    }

    /// <summary>
    /// Called when the item is equipped. Apply effects to the player.
    /// </summary>
    public abstract void OnEquip();

    /// <summary>
    /// Called when the item is unequipped (death, ammo out, manual removal).
    /// Reverse all effects applied in OnEquip.
    /// </summary>
    public abstract void OnUnequip();
}
