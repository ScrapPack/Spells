using UnityEngine;

/// <summary>
/// Lucky Bounce (Jester, Tier 1): Bouncing projectiles gain damage with each bounce.
/// First hit (direct, no bounce) deals zero damage.
///
/// GDD: "Bouncing projectiles gain damage with each bounce.
/// First hit (direct, no bounce) deals zero damage."
///
/// Completely changes how the Jester plays: you WANT to bank shots
/// off walls and never shoot directly. The bouncing mechanic that
/// defined Jester becomes the primary damage source.
///
/// Stacking: Each stack increases damage per bounce.
/// </summary>
public class LuckyBounceEffect : SpellEffect
{
    private bool subscribedToFire;

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
        if (Spawner == null) return;

        var lastProj = Spawner.LastFiredProjectile;
        if (lastProj == null) return;

        if (lastProj.GetComponent<LuckyBounceBehavior>() != null) return;

        var luckyBounce = lastProj.AddComponent<LuckyBounceBehavior>();
        luckyBounce.Initialize(StackCount);
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
