/// <summary>
/// Death Blossom: when your magazine reloads, a free 16-bullet ring fires automatically
/// for 30% damage each — a devastating area burst every time you reload.
/// Stacks increase the bullet count and damage of the blossom ring.
/// Reload penalty lives on the card's negativeEffects array.
/// </summary>
public class DeathBlossomEffect : SpellEffect
{
    private bool subscribedToReload;

    protected override void OnApply()
    {
        if (Spawner != null && !subscribedToReload)
        {
            Spawner.OnAmmoRefilled.AddListener(OnAmmoRefilled);
            subscribedToReload = true;
        }
    }

    private void OnAmmoRefilled()
    {
        if (Spawner == null) return;

        int bulletCount = 16 + (StackCount - 1) * 4;
        float damage    = 0.3f + (StackCount - 1) * 0.05f;
        Spawner.FireFreeRing(bulletCount, damage);
    }

    public override void OnRemove()
    {
        if (Spawner != null && subscribedToReload)
        {
            Spawner.OnAmmoRefilled.RemoveListener(OnAmmoRefilled);
            subscribedToReload = false;
        }
    }
}
