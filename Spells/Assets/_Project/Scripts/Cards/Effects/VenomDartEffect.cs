using UnityEngine;

/// <summary>
/// Venom Dart (Witch Doctor, Tier 1): Hits apply poison (1 damage over 5 seconds).
/// Your own projectiles move 20% slower.
///
/// GDD: "Hits apply poison (1 damage over 5 seconds).
/// Your own projectiles move 20% slower."
///
/// The speed penalty means fewer hits, but each hit matters more.
/// Poison damage uses TakeSelfDamage (bypasses invincibility),
/// making it effective against tanky targets.
///
/// Stacking: Each stack adds +1 poison damage over the same duration.
/// Speed penalty is handled by the card's stat modifier.
/// </summary>
public class VenomDartEffect : SpellEffect
{
    private int poisonDamage;
    private bool subscribedToFire;

    protected override void OnApply()
    {
        poisonDamage = StackCount;

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

        // Apply or stack poison
        var existingPoison = target.GetComponent<PoisonStatus>();
        if (existingPoison != null)
        {
            existingPoison.AddPoison(poisonDamage, 5f);
        }
        else
        {
            var poison = target.AddComponent<PoisonStatus>();
            poison.Initialize(poisonDamage, 5f);
        }
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
