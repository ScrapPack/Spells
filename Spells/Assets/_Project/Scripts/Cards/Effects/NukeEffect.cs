/// <summary>
/// NUKE: magazine holds only 1 shot. That shot is a massive explosive round —
/// 8-unit blast radius, 2× AoE damage multiplier, huge knockback. The direct hit
/// also deals 2× damage. Bullets travel 60% slower so aiming matters.
/// The orb has NO bounces — it detonates on the first surface it touches (wall or player).
/// Stacks increase explosion radius and damage further.
/// </summary>
public class NukeEffect : SpellEffect
{
    private bool subscribedToFire;

    protected override void OnApply()
    {
        if (Spawner == null) return;

        if (StackCount == 1)
        {
            // Reduce max ammo to 1
            int delta = -(Spawner.MaxAmmo - 1);
            if (delta < 0) Spawner.AdjustMaxAmmo(delta);

            // Slow bullets by 60%
            Spawner.ProjectileSpeed *= 0.4f;

            // Boost direct hit damage
            Spawner.SpreadDamageMultiplier *= 2f;
        }

        // Each stack: add a massive explosion modifier
        var modSystem = GetComponent<ProjectileModifierSystem>();
        if (modSystem != null)
        {
            modSystem.AddModifier(new ProjectileModifier
            {
                type                      = ProjectileModifier.ModifierType.Explosive,
                explosionRadius           = 8f + (StackCount - 1) * 3f,
                explosionDamageMultiplier = 2f + (StackCount - 1) * 0.5f,
                explosionKnockback        = 20f + (StackCount - 1) * 5f,
            });
        }

        // Subscribe once — disable bouncing on every NUKE projectile so it detonates
        // on the first wall it contacts rather than bouncing harmlessly.
        if (!subscribedToFire)
        {
            Spawner.OnProjectileFired.AddListener(OnProjectileFired);
            subscribedToFire = true;
        }
    }

    private void OnProjectileFired()
    {
        if (Spawner?.LastFiredProjectile == null) return;

        var proj = Spawner.LastFiredProjectile.GetComponent<Projectile>();
        proj?.DisableBouncing();
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
