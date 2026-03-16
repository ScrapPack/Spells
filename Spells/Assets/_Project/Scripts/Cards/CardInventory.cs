using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks power cards held by a player. Handles adding cards,
/// stacking, and applying all modifiers to combat/movement data.
/// </summary>
public class CardInventory : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent<PowerCardData> OnCardAdded;

    /// <summary>
    /// Cards held by this player, with stack counts.
    /// </summary>
    private readonly Dictionary<PowerCardData, int> cards = new Dictionary<PowerCardData, int>();

    public int TotalCards { get; private set; }

    /// <summary>
    /// Add a power card to the inventory. Applies modifiers immediately.
    /// Returns false if card can't be stacked further.
    /// </summary>
    public bool AddCard(PowerCardData card)
    {
        if (card == null) return false;

        int currentStack = cards.ContainsKey(card) ? cards[card] : 0;
        if (!card.CanStack(currentStack)) return false;

        if (cards.ContainsKey(card))
            cards[card]++;
        else
            cards[card] = 1;

        TotalCards++;

        // Apply modifiers via ClassManager
        var classManager = GetComponent<ClassManager>();
        if (classManager != null)
        {
            // Apply positive effects
            if (card.positiveEffects != null)
            {
                foreach (var mod in card.positiveEffects)
                    classManager.ApplyStatModifier(mod);
            }

            // Apply negative effects
            if (card.negativeEffects != null)
            {
                foreach (var mod in card.negativeEffects)
                    classManager.ApplyStatModifier(mod);
            }

            // Apply movement modifiers if present
            var controller = GetComponent<PlayerController>();
            if (controller != null && controller.Data != null)
            {
                if (card.positiveEffects != null)
                {
                    foreach (var mod in card.positiveEffects)
                    {
                        if (mod.AffectsMovement) mod.Apply(controller.Data);
                    }
                }
                if (card.negativeEffects != null)
                {
                    foreach (var mod in card.negativeEffects)
                    {
                        if (mod.AffectsMovement) mod.Apply(controller.Data);
                    }
                }
            }
        }

        // Apply special behavior if card has one
        if (card.hasSpecialBehavior && SpellEffectRegistry.Instance != null)
        {
            SpellEffectRegistry.Instance.ApplyEffect(gameObject, card);
        }

        OnCardAdded?.Invoke(card);
        return true;
    }

    /// <summary>
    /// Get stack count for a specific card.
    /// </summary>
    public int GetStackCount(PowerCardData card)
    {
        return cards.ContainsKey(card) ? cards[card] : 0;
    }

    /// <summary>
    /// Get all held cards with their stack counts.
    /// </summary>
    public Dictionary<PowerCardData, int> GetAllCards()
    {
        return new Dictionary<PowerCardData, int>(cards);
    }

    /// <summary>
    /// Clear all cards. Called at match start (not between rounds —
    /// cards persist across rounds within a match).
    /// </summary>
    public void ClearAll()
    {
        cards.Clear();
        TotalCards = 0;
    }
}
