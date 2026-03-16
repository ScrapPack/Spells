using UnityEngine;

/// <summary>
/// Ambush (Rogue, Tier 1): First hit on an opponent facing away deals 2x damage.
/// Non-ambush hits deal 0.75x damage.
///
/// GDD: "First hit on an unaware opponent (facing away) deals 2x damage.
/// Non-ambush hits deal 0.75x damage."
///
/// Rewards flanking and positioning. The Rogue's fast projectiles make
/// it easier to get behind opponents. The damage penalty on direct hits
/// means you can't just face-tank — positioning IS your damage.
///
/// Stacking: Each stack adds +0.5x to ambush multiplier.
/// </summary>
public class AmbushEffect : SpellEffect
{
    private float ambushMultiplier;
    private float normalMultiplier = 0.75f;
    private bool subscribedToFire;

    protected override void OnApply()
    {
        ambushMultiplier = 2f + (StackCount - 1) * 0.5f;

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

        if (lastProj.GetComponent<AmbushProjectileBehavior>() != null) return;

        var ambush = lastProj.AddComponent<AmbushProjectileBehavior>();
        ambush.Initialize(ambushMultiplier, normalMultiplier);
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
