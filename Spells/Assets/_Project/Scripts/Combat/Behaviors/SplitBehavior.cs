using UnityEngine;

/// <summary>
/// Added to a projectile to make it split into multiple fragments on impact.
/// When the projectile is destroyed, spawns N child projectiles in a fan pattern.
/// Child projectiles have reduced damage and do NOT split again (prevents infinite recursion).
/// </summary>
public class SplitBehavior : MonoBehaviour
{
    private int splitCount;
    private float spreadAngle;
    private float damageMultiplier;

    /// <summary>
    /// Marks this projectile as a split fragment (won't split again).
    /// </summary>
    public bool IsFragment { get; set; }

    public void Initialize(int count, float angle, float damageMult)
    {
        splitCount = count;
        spreadAngle = angle;
        damageMultiplier = damageMult;
    }

    private void OnDestroy()
    {
        // Don't split if we're a fragment (prevents infinite recursion)
        if (IsFragment) return;

        // Don't split during scene teardown
        if (!gameObject.scene.isLoaded) return;

        Split();
    }

    private void Split()
    {
        var projectile = GetComponent<Projectile>();
        if (projectile == null) return;

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        Vector2 baseDir = rb.linearVelocity.normalized;
        float speed = rb.linearVelocity.magnitude;
        float startAngle = -spreadAngle * 0.5f;
        float angleStep = splitCount > 1 ? spreadAngle / (splitCount - 1) : 0f;

        for (int i = 0; i < splitCount; i++)
        {
            float angleOffset = startAngle + angleStep * i;
            float rad = angleOffset * Mathf.Deg2Rad;

            // Rotate base direction by angle offset
            Vector2 dir = new Vector2(
                baseDir.x * Mathf.Cos(rad) - baseDir.y * Mathf.Sin(rad),
                baseDir.x * Mathf.Sin(rad) + baseDir.y * Mathf.Cos(rad)
            );

            // Create a new projectile at current position
            var fragObj = new GameObject($"SplitFragment_{i}");
            fragObj.transform.position = transform.position;

            // Add required components
            var fragRb = fragObj.AddComponent<Rigidbody2D>();
            fragObj.AddComponent<CircleCollider2D>();
            var fragProj = fragObj.AddComponent<Projectile>();

            // Initialize with reduced damage, no bounce/pierce/retrieve to keep simple
            fragProj.Initialize(
                projectile.OwnerPlayerID,
                dir,
                speed * 0.8f,   // Slightly slower
                projectile.Damage * damageMultiplier,
                projectile.KnockbackForce * 0.5f,
                projectile.HitstunDuration * 0.5f,
                0.5f,            // Short lifetime
                0.08f,           // Small radius
                0f,              // No gravity
                false, 0,        // No bounce
                false,           // No pierce
                false            // Not retrievable
            );

            // Mark as fragment so it doesn't split again
            var fragSplit = fragObj.AddComponent<SplitBehavior>();
            fragSplit.IsFragment = true;

            // Set layer to match parent
            fragObj.layer = gameObject.layer;
        }
    }
}
