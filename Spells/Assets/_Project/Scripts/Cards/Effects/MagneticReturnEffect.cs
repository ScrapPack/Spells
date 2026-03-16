using UnityEngine;

/// <summary>
/// Magnetic Return (Warrior, Tier 1): Axes automatically return after 3 seconds.
/// Returning axes can hit you.
///
/// GDD: "Axes automatically return after 3 seconds. Returning axes can hit you."
///
/// Removes the need to pick up axes manually but adds danger — your own
/// returning axes are live weapons. Synergizes with Heavy Throw (returning
/// piercing axes clear the field).
///
/// Stacking: Each stack increases return speed.
/// </summary>
public class MagneticReturnEffect : SpellEffect
{
    private bool subscribedToFire;
    private float returnDelay = 3f;

    protected override void OnApply()
    {
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
        if (lastProj.GetComponent<MagneticReturnBehavior>() != null) return;

        var returnBehavior = lastProj.AddComponent<MagneticReturnBehavior>();
        float speed = 15f + (StackCount - 1) * 5f; // Faster return with stacks
        returnBehavior.Initialize(transform, speed, returnDelay);
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
