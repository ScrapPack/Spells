using System.Collections.Generic;

/// <summary>
/// Singleton registry mapping behavior IDs to ItemBehavior types.
/// Mirrors SpellEffectRegistry pattern — maps string IDs from ItemData
/// to concrete MonoBehaviour types that implement the item logic.
///
/// Registration happens statically so it's available before any instance exists.
/// </summary>
public static class ItemBehaviorRegistry
{
    private static readonly Dictionary<string, System.Type> registry = new Dictionary<string, System.Type>();
    private static bool initialized;

    /// <summary>
    /// Get the behavior type for a given ID. Returns null if not found.
    /// </summary>
    public static System.Type GetBehaviorType(string behaviorID)
    {
        EnsureInitialized();
        return registry.ContainsKey(behaviorID) ? registry[behaviorID] : null;
    }

    /// <summary>
    /// Register a behavior type. Called during static initialization.
    /// </summary>
    public static void Register(string behaviorID, System.Type type)
    {
        registry[behaviorID] = type;
    }

    /// <summary>
    /// Check if a behavior ID is registered.
    /// </summary>
    public static bool IsRegistered(string behaviorID)
    {
        EnsureInitialized();
        return registry.ContainsKey(behaviorID);
    }

    private static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        // Register all built-in item behaviors
        Register("spider_shoes", typeof(SpiderShoesItem));
        Register("fire_wand", typeof(FireWandItem));
        Register("hitscan_gun", typeof(HitscanGunItem));
    }
}
