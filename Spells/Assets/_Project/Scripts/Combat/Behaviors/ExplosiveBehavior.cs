using UnityEngine;

/// <summary>
/// Added to a projectile to make it deal AoE damage on impact.
/// When the projectile is destroyed (hit player, hit wall, lifetime),
/// it creates an explosion that damages all players in radius.
/// The original projectile hit still applies normally — this is bonus AoE.
/// </summary>
public class ExplosiveBehavior : MonoBehaviour
{
    private float radius;
    private float damageMultiplier;
    private float knockbackForce;

    public void Initialize(float explosionRadius, float damageMult, float knockback)
    {
        radius = explosionRadius;
        damageMultiplier = damageMult;
        knockbackForce = knockback;
    }

    private void OnDestroy()
    {
        // Only explode if we're being destroyed during gameplay
        // (not during scene teardown)
        if (!gameObject.scene.isLoaded) return;

        Explode();
    }

    private void Explode()
    {
        var projectile = GetComponent<Projectile>();
        if (projectile == null) return;

        float explosionDamage = projectile.Damage * damageMultiplier;
        Vector2 center = transform.position;

        // Find all players in explosion radius
        var colliders = Physics2D.OverlapCircleAll(center, radius);
        foreach (var col in colliders)
        {
            var health = col.GetComponent<HealthSystem>();
            if (health == null || !health.IsAlive) continue;

            // Don't double-hit the direct target (Projectile already handled them)
            var identity = col.GetComponent<PlayerIdentity>();
            if (identity == null) continue;

            // Apply AoE damage with distance falloff
            float dist = Vector2.Distance(center, col.transform.position);
            float falloff = 1f - (dist / radius);
            falloff = Mathf.Clamp01(falloff);

            float finalDamage = explosionDamage * falloff;
            if (finalDamage < 0.5f) continue; // Below rounding threshold

            health.TakeDamage(finalDamage, projectile.OwnerPlayerID);

            // Knockback away from explosion center
            var rb = col.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 pushDir = ((Vector2)col.transform.position - center).normalized;
                rb.linearVelocity += pushDir * knockbackForce * falloff;
            }
        }

        // Screen shake — stronger for bigger blasts
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(Mathf.Lerp(0.1f, 0.3f, radius / 10f),
                                       Mathf.Lerp(0.1f, 0.25f, radius / 10f));

        // Expanding ring visual
        ExplosionVFX.Spawn(center, radius);
    }
}
