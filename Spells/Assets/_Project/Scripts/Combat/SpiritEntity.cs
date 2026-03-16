using UnityEngine;

/// <summary>
/// Mirror summon created by SpiritBondEffect.
/// Follows the owner with a mirrored offset (opposite side).
/// Has its own collider — when hit by projectiles, the OWNER takes damage.
/// This is the downside: damage to the spirit is shared with the caster.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class SpiritEntity : MonoBehaviour
{
    private Transform ownerTransform;
    private int ownerPlayerID;
    private HealthSystem ownerHealth;
    private Vector3 mirrorOffset;
    private float shareDamageAmount;

    public void Initialize(Transform owner, int ownerID, HealthSystem health, Vector3 offset, float damageShare)
    {
        ownerTransform = owner;
        ownerPlayerID = ownerID;
        ownerHealth = health;
        mirrorOffset = offset;
        shareDamageAmount = damageShare;

        // Setup physics: kinematic trigger
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var col = GetComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        // Set layer to Player so projectiles can hit it
        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    private void Update()
    {
        if (ownerTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        // Mirror position: offset flipped horizontally
        transform.position = ownerTransform.position + mirrorOffset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react to projectiles
        var projectile = other.GetComponent<Projectile>();
        if (projectile == null) return;

        // Don't get hit by owner's projectiles (unless reflected)
        if (projectile.OwnerPlayerID == ownerPlayerID && !projectile.IsReflected)
            return;

        // Share damage with owner
        if (ownerHealth != null && ownerHealth.IsAlive)
        {
            ownerHealth.TakeDamage(shareDamageAmount, projectile.OwnerPlayerID);
        }

        // Destroy the projectile (spirit absorbs it)
        if (!other.GetComponent<Projectile>().IsReflected) // Don't destroy reflected projectiles
            Destroy(other.gameObject);
    }
}
