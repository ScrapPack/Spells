using UnityEngine;

/// <summary>
/// Hex Mark (Witch Doctor, Tier 1): Cursed opponents take +1 damage from all sources.
/// You can only curse one opponent at a time.
///
/// GDD: "Cursed opponents take +1 damage from all sources.
/// You can only curse one opponent at a time."
///
/// Marks one opponent with a hex via projectile hit. All damage the
/// marked target takes is amplified. Switching targets removes the
/// previous curse. Creates target priority and psychological pressure.
///
/// Stacking: Each stack increases bonus damage by +1.
/// </summary>
public class HexMarkEffect : SpellEffect
{
    private int bonusDamage;
    private HexMarkStatus currentCurse;
    private bool subscribedToFire;

    protected override void OnApply()
    {
        bonusDamage = StackCount;

        if (Spawner != null && !subscribedToFire)
        {
            Spawner.OnProjectileFired.AddListener(OnProjectileFired);
            subscribedToFire = true;
        }
    }

    private void OnProjectileFired()
    {
        if (Spawner == null) return;

        var lastProj = Spawner.LastFiredProjectile;
        if (lastProj == null) return;

        var projectile = lastProj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.OnHitPlayer += OnProjectileHit;
        }
    }

    private void OnProjectileHit(GameObject target, float damage)
    {
        if (target == null) return;

        // Remove existing curse (only one at a time)
        if (currentCurse != null)
        {
            currentCurse.Remove();
            currentCurse = null;
        }

        // Remove any existing hex on this target from other sources
        var existingHex = target.GetComponent<HexMarkStatus>();
        if (existingHex != null)
            existingHex.Remove();

        // Apply new curse
        currentCurse = target.AddComponent<HexMarkStatus>();
        currentCurse.Initialize(bonusDamage);
    }

    public override void OnRoundStart()
    {
        // Clear curse between rounds
        if (currentCurse != null)
        {
            currentCurse.Remove();
            currentCurse = null;
        }
    }

    public override void OnRemove()
    {
        if (currentCurse != null)
            currentCurse.Remove();

        if (Spawner != null && subscribedToFire)
        {
            Spawner.OnProjectileFired.RemoveListener(OnProjectileFired);
            subscribedToFire = false;
        }
    }
}
