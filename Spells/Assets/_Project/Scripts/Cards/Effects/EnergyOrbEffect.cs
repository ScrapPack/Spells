/// <summary>
/// Energy Orb: fires a single massive energy sphere that bounces endlessly around the
/// arena until it hits a player. Deals 5× damage. Both players can parry it back and
/// forth — the last person to reflect it "owns" it. The orb never expires on its own.
///
/// Inspired by the Half-Life 2 Combine energy ball.
///
/// Stacks make the orb larger and more damaging.
/// </summary>
public class EnergyOrbEffect : SpellEffect
{
    private bool subscribedToFire;

    protected override void OnApply()
    {
        if (Spawner == null) return;

        if (StackCount == 1)
        {
            // Reduce magazine to 1 shot — this IS your weapon now
            int delta = -(Spawner.MaxAmmo - 1);
            if (delta < 0) Spawner.AdjustMaxAmmo(delta);

            // The orb lumbers across the arena menacingly
            Spawner.ProjectileSpeed *= 0.25f;

            // Lethal on contact
            Spawner.SpreadDamageMultiplier *= 5f;

            // Dramatic reload — you have one shot, make it count
            Spawner.RefillTime *= 2f;
        }
        else
        {
            // Each stack: larger orb, more damage
            Spawner.SpreadDamageMultiplier *= 1.5f;
        }

        // Orb bounces for the entire round (50 bounces base, +20 per stack)
        Spawner.BonusMaxBounces += 47 + (StackCount - 1) * 20;

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

        // Scale up — CircleCollider2D hitbox scales with transform in Unity 2D
        float scale = 2.5f + (StackCount - 1) * 0.5f;
        lastProj.transform.localScale = UnityEngine.Vector3.one * scale;

        // Never expires — keeps bouncing until it hits flesh
        var proj = lastProj.GetComponent<Projectile>();
        if (proj != null)
            proj.PreventAutoExpire = true;
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
