using UnityEngine;

/// <summary>
/// Defines a temporary item that can be found in chests.
/// Items are lost on round death (risk/reward mechanic).
/// Each item has a behavior ID that maps to an ItemBehavior class
/// via ItemBehaviorRegistry (same pattern as SpellEffectRegistry).
///
/// Items provide immediate power but at the cost of losing them on death.
/// This creates interesting risk/reward decisions: play safe to keep items,
/// or be aggressive knowing you might lose them.
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Spells/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "New Item";
    [TextArea(2, 4)]
    public string description = "A temporary item found in chests.";

    [Header("Behavior")]
    [Tooltip("Identifier for ItemBehaviorRegistry lookup (e.g., 'spider_shoes', 'fire_wand')")]
    public string behaviorID = "";

    [Header("Ammo")]
    [Tooltip("Number of uses before auto-unequip (0 = unlimited, item lasts until death)")]
    [Range(0, 20)] public int ammo = 0;

    [Header("Combat Overrides")]
    [Tooltip("If set, replaces the player's CombatData while this item is active")]
    public CombatData combatDataOverride;

    [Header("Chest Scaling")]
    [Tooltip("Minimum total level pool for this item to appear in chests")]
    [Range(0, 20)] public int minLevelPool = 0;
    [Tooltip("Drop weight — higher values mean more likely to appear")]
    [Range(0.1f, 10f)] public float dropWeight = 1f;

    [Header("Visual")]
    public Sprite itemIcon;
    public Color itemColor = Color.white;

    /// <summary>
    /// Check if this item can appear in chests at the given total level pool.
    /// </summary>
    public bool IsAvailableAtLevelPool(int totalLevelPool)
    {
        return totalLevelPool >= minLevelPool;
    }
}
