using UnityEngine;

/// <summary>
/// Jackpot (Jester, Tier 2): When you get a kill, all opponents take 1 damage.
/// When you die, all opponents heal 1 HP.
///
/// GDD: "When you get a kill, all opponents also take 1 damage.
/// When you die, all opponents heal 1 HP."
///
/// This is pure chaos: a Jackpot kill in a 4-player game can eliminate
/// multiple weakened opponents simultaneously. But dying with Jackpot
/// heals everyone else. The card creates dramatic swings.
///
/// Stacking: Each stack increases the damage/heal by 1.
/// Jackpot x2 = kills deal 2 AoE damage, death heals 2 to all.
/// </summary>
public class JackpotEffect : SpellEffect
{
    private int effectPower;
    private bool subscribedToEvents;

    protected override void OnApply()
    {
        effectPower = StackCount;

        if (subscribedToEvents) return; // Already subscribed

        var roundManager = Object.FindAnyObjectByType<RoundManager>();
        if (roundManager != null)
        {
            roundManager.OnPlayerEliminated.AddListener(OnPlayerEliminated);
        }

        if (Health != null)
        {
            Health.OnDeath.AddListener(OnSelfDied);
        }

        subscribedToEvents = true;
    }

    private void OnPlayerEliminated(int eliminatedPlayerID)
    {
        if (Identity == null || Health == null || !Health.IsAlive) return;
        if (eliminatedPlayerID == Identity.PlayerID) return; // That's us dying, handled separately

        // Check if WE killed this player
        var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.PlayerID == eliminatedPlayerID)
            {
                var victimHealth = player.GetComponent<HealthSystem>();
                if (victimHealth != null && victimHealth.LastAttackerID == Identity.PlayerID)
                {
                    // JACKPOT! Damage all other opponents
                    DamageAllOpponents(effectPower);
                }
                break;
            }
        }
    }

    private void OnSelfDied()
    {
        // We died — heal all opponents
        HealAllOpponents(effectPower);
    }

    private void DamageAllOpponents(int amount)
    {
        var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.PlayerID == Identity.PlayerID) continue; // Skip self

            var otherHealth = player.GetComponent<HealthSystem>();
            if (otherHealth != null && otherHealth.IsAlive)
            {
                otherHealth.TakeDamage(amount, Identity.PlayerID);
            }
        }
    }

    private void HealAllOpponents(int amount)
    {
        var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.PlayerID == Identity.PlayerID) continue; // Skip self

            var otherHealth = player.GetComponent<HealthSystem>();
            if (otherHealth != null && otherHealth.IsAlive)
            {
                otherHealth.Heal(amount);
            }
        }
    }

    public override void OnRemove()
    {
        if (!subscribedToEvents) return;

        var roundManager = Object.FindAnyObjectByType<RoundManager>();
        if (roundManager != null)
            roundManager.OnPlayerEliminated.RemoveListener(OnPlayerEliminated);

        if (Health != null)
            Health.OnDeath.RemoveListener(OnSelfDied);

        subscribedToEvents = false;
    }
}
