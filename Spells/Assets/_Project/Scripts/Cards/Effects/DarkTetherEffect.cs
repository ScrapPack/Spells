using UnityEngine;

/// <summary>
/// Dark Tether (Warlock, Tier 2): Your orbs home slightly toward nearest opponent.
/// Your orbs also home slightly toward you on return.
///
/// GDD: "Your orbs home slightly toward nearest opponent.
/// Your orbs also home slightly toward you on return."
///
/// Warlock orbs become boomerangs: fly out homing toward enemies,
/// then arc back homing toward you. The return path is the danger —
/// your own orbs track you on the way back. Creates unique ring-shaped
/// attack patterns.
///
/// Stacking: Each stack increases outgoing homing strength.
/// </summary>
public class DarkTetherEffect : SpellEffect
{
    private bool subscribedToFire;
    private float outgoingStrength;
    private float returnStrength = 45f; // Return always homes moderately

    protected override void OnApply()
    {
        outgoingStrength = 30f + (StackCount - 1) * 15f; // Stronger homing with stacks

        if (Spawner != null && !subscribedToFire)
        {
            Spawner.OnProjectileFired.AddListener(OnProjectileFired);
            subscribedToFire = true;
        }
    }

    private void OnProjectileFired()
    {
        if (Spawner == null || Identity == null) return;

        var lastProj = Spawner.LastFiredProjectile;
        if (lastProj == null) return;

        if (lastProj.GetComponent<DarkTetherBehavior>() != null) return;

        var proj = lastProj.GetComponent<Projectile>();
        float lifetime = 3f; // Default; will be overridden by actual projectile lifetime
        if (Class != null && Class.CombatData != null)
            lifetime = Class.CombatData.projectileLifetime;

        var tether = lastProj.AddComponent<DarkTetherBehavior>();
        tether.Initialize(transform, Identity.PlayerID, outgoingStrength, returnStrength, lifetime);
    }

    public override void OnRemove()
    {
        if (Spawner != null && subscribedToFire)
        {
            Spawner.OnProjectileFired.RemoveListener(OnProjectileFired);
            subscribedToFire = false;
        }
    }
}
