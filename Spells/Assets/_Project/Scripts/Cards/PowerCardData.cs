using UnityEngine;

/// <summary>
/// Defines a single power card in the draft system.
/// Every card has at least one positive and one negative effect.
/// Effects are implemented as StatModifiers applied to CombatData/MovementData.
///
/// Stacking: taking the same card N times applies the modifiers N times.
/// Some cards have a stackCap (0 = unlimited).
/// </summary>
[CreateAssetMenu(fileName = "PowerCard", menuName = "Spells/Power Card")]
public class PowerCardData : ScriptableObject
{
    [Header("Identity")]
    public string cardName = "New Card";
    [TextArea(2, 4)]
    public string positiveDescription = "✦ Positive effect description";
    [TextArea(2, 4)]
    public string negativeDescription = "✗ Negative effect description";

    [Header("Classification")]
    [Tooltip("Tier 1 = always available, Tier 2 = Level 2+, Tier 3 = Level 3+")]
    [Range(1, 3)] public int tier = 1;
    [Tooltip("Tags for card pool filtering. 'General' = available to all classes.")]
    public string[] classTags = new string[] { "General" };

    [Header("Effects — Positive")]
    [Tooltip("Stat modifications that benefit the player")]
    public StatModifier[] positiveEffects;

    [Header("Effects — Negative")]
    [Tooltip("Stat modifications that penalize the player (the tradeoff)")]
    public StatModifier[] negativeEffects;

    [Header("Stacking")]
    [Tooltip("Maximum times this card can be taken (0 = unlimited)")]
    [Range(0, 5)] public int stackCap = 0;

    [Header("Special Behavior")]
    [Tooltip("If true, this card has custom logic beyond stat modifiers (e.g., Lich Form revive)")]
    public bool hasSpecialBehavior = false;
    [Tooltip("Identifier for special behavior lookup (e.g., 'lich_form', 'chaos_orb')")]
    public string specialBehaviorID = "";

    [Header("Visual")]
    public Sprite cardArt;
    public Color cardColor = Color.white;

    /// <summary>
    /// Check if this card can appear in a pool with the given class tags at a given level.
    /// </summary>
    public bool IsAvailableFor(string[] playerClassTags, int playerLevel)
    {
        // Check tier requirement
        if (tier > 1 && playerLevel < tier - 1) return false;

        // Check class tag match — card must share at least one tag with player
        foreach (string cardTag in classTags)
        {
            if (cardTag == "General") return true;

            foreach (string playerTag in playerClassTags)
            {
                if (cardTag == playerTag) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if this card can be stacked again (given current stack count).
    /// </summary>
    public bool CanStack(int currentStackCount)
    {
        return stackCap == 0 || currentStackCount < stackCap;
    }
}
