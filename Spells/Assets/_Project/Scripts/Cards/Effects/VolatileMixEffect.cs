using UnityEngine;

/// <summary>
/// Volatile Mix (Alchemist, Tier 1): Potion zones explode when an opponent steps in.
/// You also trigger your own zones.
///
/// GDD: "Potion zones explode when an opponent steps in them.
/// You also trigger your own zones."
///
/// Changes potions from lingering DoT to instant burst traps.
/// The self-trigger downside means you have to avoid your own zones.
/// Creates area denial: opponents can't walk through potion zones,
/// and you can't either.
///
/// Stacking: Each stack increases explosion damage.
/// </summary>
public class VolatileMixEffect : SpellEffect
{
    private bool subscribedToFire;
    private float explosionDamage;

    protected override void OnApply()
    {
        explosionDamage = 1f + (StackCount - 1) * 0.5f;

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

        // Don't double-add
        if (lastProj.GetComponent<PotionZoneSpawner>() != null) return;

        var spawner = lastProj.AddComponent<PotionZoneSpawner>();
        spawner.Initialize(
            Identity.PlayerID,
            10f,    // Long duration (waits for someone to step in)
            1.5f,   // Normal radius
            true,   // CAN hit owner (the downside!)
            true    // Volatile: explodes on contact
        );
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
