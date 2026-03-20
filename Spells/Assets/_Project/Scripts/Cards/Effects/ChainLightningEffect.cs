/// <summary>
/// Chain Lightning: every bullet that hits a player immediately chains to the nearest
/// other player in range for 50% of the direct hit damage. Basically free area damage
/// on every shot — with a 30% direct damage penalty to balance it.
/// Stacks increase chain damage and range.
/// </summary>
public class ChainLightningEffect : SpellEffect
{
    private bool subscribedToFire;

    protected override void OnApply()
    {
        if (Spawner == null) return;

        if (StackCount == 1)
        {
            // Direct shot penalty
            Spawner.SpreadDamageMultiplier *= 0.7f;
        }

        if (!subscribedToFire)
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

        if (lastProj.GetComponent<ChainLightningBehavior>() != null) return;

        var chain = lastProj.AddComponent<ChainLightningBehavior>();
        int ownerID = Identity != null ? Identity.PlayerID : -1;
        chain.Initialize(
            0.5f + (StackCount - 1) * 0.15f,  // chain damage: 50% → 65% → 80%…
            6f   + (StackCount - 1) * 2f,      // chain range:  6u  → 8u  → 10u…
            ownerID);
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
