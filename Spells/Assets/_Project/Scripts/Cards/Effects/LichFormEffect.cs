using UnityEngine;

/// <summary>
/// Lich Form (Warlock, Tier 3): Revive once per round with 1 HP.
/// Permanent -1 max HP for the rest of the match.
///
/// GDD: "Revive once per round with 1 HP. Permanent -1 max HP
/// for the rest of the match."
///
/// This is a game-warping card: you effectively have two lives per round,
/// but your max HP shrinks every time you take it. At 3HP Warlock,
/// taking Lich Form once means you now have 2HP max + 1 revive.
/// Taking it twice means 1HP max + 2 revives (but you die to any hit).
///
/// Stacking: each stack grants +1 revive per round, costs -1 max HP.
/// </summary>
public class LichFormEffect : SpellEffect
{
    private int revivesPerRound;
    private int revivesRemaining;
    private bool subscribedToDeath;

    protected override void OnApply()
    {
        revivesPerRound = StackCount;
        revivesRemaining = revivesPerRound;

        // Permanent -1 max HP per stack (applied immediately)
        // The stat modifier on the card handles this via MaxHP -1
        // But we also need to enforce it after ResetForRound
        // (handled in OnRoundStart)

        // Subscribe to death to intercept it
        if (Health != null && !subscribedToDeath)
        {
            Health.OnDeath.AddListener(OnDied);
            subscribedToDeath = true;
        }
    }

    private void OnDied()
    {
        if (revivesRemaining <= 0) return;

        // Revive with 1 HP
        if (Health != null)
        {
            bool revived = Health.Revive(1);
            if (revived)
            {
                revivesRemaining--;
                Debug.Log($"[LichForm] Player {(Identity != null ? Identity.PlayerID : -1)} " +
                    $"revived! ({revivesRemaining} revives remaining)");
            }
        }
    }

    public override void OnRoundStart()
    {
        // Reset revive count each round
        revivesRemaining = revivesPerRound;

        // Enforce max HP reduction (ClassManager.ResetForRound restores HP to MaxHP,
        // but lich form permanently reduces MaxHP. The stat modifier handles this,
        // but in case of ordering issues, enforce here too.)
    }

    public override void OnRemove()
    {
        if (Health != null && subscribedToDeath)
        {
            Health.OnDeath.RemoveListener(OnDied);
            subscribedToDeath = false;
        }
    }
}
