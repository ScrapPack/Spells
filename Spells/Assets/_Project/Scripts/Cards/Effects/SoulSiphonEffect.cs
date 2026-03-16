using UnityEngine;

/// <summary>
/// Soul Siphon (Warlock, Tier 1): Gain 1 HP when any opponent is eliminated.
/// Lose 1 HP when YOU eliminate someone directly.
///
/// GDD: "Gain 1 HP when an opponent is eliminated.
/// Lose 1 HP when you eliminate someone directly."
///
/// Net on YOUR kills: +1 (opponent died) -1 (you killed) = 0 HP.
/// Net on OTHERS' kills: +1 HP free. Rewards passive play; benefits
/// from chaotic multi-player fights where others do the killing.
///
/// Stacking: Each stack increases both heal and cost by 1.
/// </summary>
public class SoulSiphonEffect : SpellEffect
{
    private int effectPower;
    private bool subscribedToEvents;

    protected override void OnApply()
    {
        effectPower = StackCount;

        if (subscribedToEvents) return;

        var roundManager = Object.FindAnyObjectByType<RoundManager>();
        if (roundManager != null)
        {
            roundManager.OnPlayerEliminated.AddListener(OnPlayerEliminated);
        }

        subscribedToEvents = true;
    }

    private void OnPlayerEliminated(int eliminatedPlayerID)
    {
        if (Identity == null || Health == null || !Health.IsAlive) return;
        if (eliminatedPlayerID == Identity.PlayerID) return; // We died

        // Any opponent eliminated = heal
        Health.Heal(effectPower);

        // If WE got the kill = self-damage (net zero on our own kills)
        var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.PlayerID == eliminatedPlayerID)
            {
                var victimHealth = player.GetComponent<HealthSystem>();
                if (victimHealth != null && victimHealth.LastAttackerID == Identity.PlayerID)
                {
                    Health.TakeSelfDamage(effectPower);
                }
                break;
            }
        }
    }

    public override void OnRemove()
    {
        if (!subscribedToEvents) return;

        var roundManager = Object.FindAnyObjectByType<RoundManager>();
        if (roundManager != null)
            roundManager.OnPlayerEliminated.RemoveListener(OnPlayerEliminated);

        subscribedToEvents = false;
    }
}
