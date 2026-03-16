using UnityEngine;

/// <summary>
/// Sticky Brew (Alchemist, Tier 1): Potion zones last twice as long.
/// Potion zones are half the size.
///
/// GDD: "Potion zones last twice as long. Potion zones are half the size."
///
/// Adds zone-creation behavior to all Alchemist projectiles.
/// Each potion now leaves a lingering damage zone on impact.
/// Zones deal 1 damage per second to players standing in them.
///
/// Stacking: Each stack adds +50% more duration.
/// </summary>
public class StickyBrewEffect : SpellEffect
{
    private bool subscribedToFire;
    private float durationMultiplier;
    private float radiusMultiplier = 0.5f; // Half size

    protected override void OnApply()
    {
        durationMultiplier = 2f + (StackCount - 1) * 0.5f; // 2x, 2.5x, 3x...

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

        // Don't double-add zone spawners
        if (lastProj.GetComponent<PotionZoneSpawner>() != null) return;

        float baseDuration = 3f;
        float baseRadius = 1.5f;

        var spawner = lastProj.AddComponent<PotionZoneSpawner>();
        spawner.Initialize(
            Identity.PlayerID,
            baseDuration * durationMultiplier,
            baseRadius * radiusMultiplier,
            false,  // Can't hit owner (Sticky Brew is safe)
            false   // Not volatile
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
