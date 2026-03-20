/// <summary>
/// Ricochet Hell: bullets get 10 total bounces and gain +20% damage per bounce.
/// A bullet that bounces 5 times deals ~2.5× its base damage on the final hit.
/// Direct shots deal 30% less damage — you want to bank every shot.
/// Stacks add 5 more bounces and increase damage scaling.
/// </summary>
public class RicochetHellEffect : SpellEffect
{
    private bool subscribedToFire;

    protected override void OnApply()
    {
        if (Spawner == null) return;

        // Add bonus bounces (prefab starts at 3; we push it to 10 on first stack)
        Spawner.BonusMaxBounces += StackCount == 1 ? 7 : 5;

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

        if (lastProj.GetComponent<BounceScalingBehavior>() != null) return;

        var scaler = lastProj.AddComponent<BounceScalingBehavior>();
        scaler.Initialize(1.2f + (StackCount - 1) * 0.05f);
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
