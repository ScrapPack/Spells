using UnityEngine;

/// <summary>
/// Blood Pact (Warlock): Casting a spell costs 1 HP per stack.
/// Creates the core Warlock tension: your shots hit like trucks,
/// but every cast brings you closer to death.
///
/// GDD: "Spells deal 2x damage. Casting costs 1 HP."
/// Damage boost is handled by stat modifier. HP cost is this effect.
///
/// Stacking: Blood Pact x2 = costs 2 HP per cast.
/// At 3HP (Warlock), Blood Pact x3 means you die from casting 2 spells.
/// </summary>
public class BloodPactEffect : SpellEffect
{
    private int hpCostPerCast;

    protected override void OnApply()
    {
        hpCostPerCast = StackCount;

        if (Spawner != null)
        {
            // Remove previous listener if stacking (re-initialization)
            Spawner.OnProjectileFired.RemoveListener(OnCast);
            Spawner.OnProjectileFired.AddListener(OnCast);
        }
    }

    private void OnCast()
    {
        if (Health == null || !Health.IsAlive) return;

        // Self-damage bypasses invincibility — casting always hurts
        Health.TakeSelfDamage(hpCostPerCast);
    }

    public override void OnRemove()
    {
        if (Spawner != null)
            Spawner.OnProjectileFired.RemoveListener(OnCast);
    }

    public override void OnRoundStart()
    {
        // HP cost persists across rounds (stacks with level)
    }
}
