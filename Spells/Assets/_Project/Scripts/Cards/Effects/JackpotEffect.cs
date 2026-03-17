using UnityEngine;

/// <summary>
/// Jackpot (Jester, Tier 2): When you get a kill, all opponents take 1 damage.
/// When you die, all opponents heal 1 HP.
/// Stacking increases the damage/heal by 1 per stack.
/// </summary>
public class JackpotEffect : SpellEffect
{
    private int effectPower;
    private bool subscribedToEvents;

    protected override void OnApply()
    {
        effectPower = StackCount;

        if (subscribedToEvents) return;

        if (Health != null)
            Health.OnDeath.AddListener(OnSelfDied);

        subscribedToEvents = true;
    }

    public override void OnRoundStart()
    {
        // Subscribe to other players' deaths to detect our kills
        var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (Identity != null && player.PlayerID == Identity.PlayerID) continue;
            var victimHealth = player.GetComponent<HealthSystem>();
            if (victimHealth != null)
                victimHealth.OnDeath.AddListener(() => OnOtherPlayerDied(victimHealth));
        }
    }

    private void OnOtherPlayerDied(HealthSystem victimHealth)
    {
        if (Identity == null || Health == null || !Health.IsAlive) return;
        if (victimHealth.LastAttackerID == Identity.PlayerID)
            DamageAllOpponents(effectPower);
    }

    private void OnSelfDied()
    {
        HealAllOpponents(effectPower);
    }

    private void DamageAllOpponents(int amount)
    {
        var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (Identity != null && player.PlayerID == Identity.PlayerID) continue;
            var otherHealth = player.GetComponent<HealthSystem>();
            if (otherHealth != null && otherHealth.IsAlive)
                otherHealth.TakeDamage(amount, Identity.PlayerID);
        }
    }

    private void HealAllOpponents(int amount)
    {
        var allPlayers = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (Identity != null && player.PlayerID == Identity.PlayerID) continue;
            var otherHealth = player.GetComponent<HealthSystem>();
            if (otherHealth != null && otherHealth.IsAlive)
                otherHealth.Heal(amount);
        }
    }

    public override void OnRemove()
    {
        if (!subscribedToEvents) return;
        if (Health != null)
            Health.OnDeath.RemoveListener(OnSelfDied);
        subscribedToEvents = false;
    }
}
