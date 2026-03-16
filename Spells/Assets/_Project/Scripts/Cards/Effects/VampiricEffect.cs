using UnityEngine;

/// <summary>
/// Vampiric: heal 1 HP when your projectile kills an enemy.
/// Stacking increases heal amount (1 per stack).
/// Requires kill credit tracking (HealthSystem.LastAttackerID).
/// </summary>
public class VampiricEffect : SpellEffect
{
    private int healPerKill;

    protected override void OnApply()
    {
        healPerKill = StackCount;

        // Subscribe to all enemy HealthSystems' OnDeath events would be expensive.
        // Instead, we check kills in Update via a simpler pattern:
        // RoundManager fires OnPlayerEliminated, we check if we were the killer.
        var roundManager = Object.FindAnyObjectByType<RoundManager>();
        if (roundManager != null)
        {
            roundManager.OnPlayerEliminated.AddListener(OnPlayerEliminated);
        }
    }

    private void OnPlayerEliminated(int eliminatedPlayerID)
    {
        if (Identity == null || Health == null || !Health.IsAlive) return;

        // Find the eliminated player's HealthSystem to check who killed them
        var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.PlayerID == eliminatedPlayerID)
            {
                var victimHealth = player.GetComponent<HealthSystem>();
                if (victimHealth != null && victimHealth.LastAttackerID == Identity.PlayerID)
                {
                    // We got the kill — heal!
                    Health.Heal(healPerKill);
                }
                break;
            }
        }
    }

    public override void OnRemove()
    {
        var roundManager = Object.FindAnyObjectByType<RoundManager>();
        if (roundManager != null)
        {
            roundManager.OnPlayerEliminated.RemoveListener(OnPlayerEliminated);
        }
    }

    public override void OnRoundStart()
    {
        // Vampiric heal amount persists across rounds (scales with stacks)
    }
}
