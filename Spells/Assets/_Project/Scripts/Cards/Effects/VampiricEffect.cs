using UnityEngine;

/// <summary>
/// Vampiric: heal 1 HP when your projectile kills an enemy.
/// Stacking increases heal amount (1 per stack).
/// Subscribes to each alive HealthSystem.OnDeath at round start.
/// </summary>
public class VampiricEffect : SpellEffect
{
    private int healPerKill;

    protected override void OnApply()
    {
        healPerKill = StackCount;
    }

    public override void OnRoundStart()
    {
        // Subscribe to every other player's death event so we can check kill credit
        var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (Identity != null && player.PlayerID == Identity.PlayerID) continue;
            var victimHealth = player.GetComponent<HealthSystem>();
            if (victimHealth != null)
                victimHealth.OnDeath.AddListener(() => OnAnyPlayerDied(victimHealth));
        }
    }

    private void OnAnyPlayerDied(HealthSystem victimHealth)
    {
        if (Identity == null || Health == null || !Health.IsAlive) return;
        if (victimHealth.LastAttackerID == Identity.PlayerID)
            Health.Heal(healPerKill);
    }

    public override void OnRemove() { }
}
