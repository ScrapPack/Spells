using UnityEngine;

/// <summary>
/// Soul Siphon (Warlock, Tier 1): Gain 1 HP when any opponent is eliminated.
/// Lose 1 HP when YOU eliminate someone directly.
/// Net on your own kills: +1 -1 = 0. Free heals on others' kills.
/// Stacking increases both heal and cost by 1 per stack.
/// </summary>
public class SoulSiphonEffect : SpellEffect
{
    private int effectPower;
    private bool subscribedToEvents;

    protected override void OnApply()
    {
        effectPower = StackCount;
    }

    public override void OnRoundStart()
    {
        if (subscribedToEvents) return;

        var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (Identity != null && player.PlayerID == Identity.PlayerID) continue;
            var victimHealth = player.GetComponent<HealthSystem>();
            if (victimHealth != null)
                victimHealth.OnDeath.AddListener(() => OnOpponentDied(victimHealth));
        }

        subscribedToEvents = true;
    }

    private void OnOpponentDied(HealthSystem victimHealth)
    {
        if (Identity == null || Health == null || !Health.IsAlive) return;

        // Any opponent eliminated = heal
        Health.Heal(effectPower);

        // If WE got the kill = self-damage (net zero on our kills)
        if (victimHealth.LastAttackerID == Identity.PlayerID)
            Health.TakeSelfDamage(effectPower);
    }

    public override void OnRemove()
    {
        subscribedToEvents = false;
    }
}
