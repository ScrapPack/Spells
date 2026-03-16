using UnityEngine;

/// <summary>
/// Added to Warrior projectiles by MagneticReturnEffect.
/// After a delay (or on landing), the projectile returns to the owner.
/// Returning projectiles CAN hit the owner (the card's downside).
/// </summary>
public class MagneticReturnBehavior : MonoBehaviour
{
    private Transform ownerTransform;
    private float returnSpeed;
    private float returnDelay;
    private Rigidbody2D rb;
    private Projectile projectile;
    private bool isReturning;
    private float timer;
    private float maxReturnTime = 5f;

    public void Initialize(Transform owner, float speed, float delay)
    {
        ownerTransform = owner;
        returnSpeed = speed;
        returnDelay = delay;
        rb = GetComponent<Rigidbody2D>();
        projectile = GetComponent<Projectile>();

        if (projectile != null)
            projectile.PreventAutoExpire = true;
    }

    private void Update()
    {
        if (ownerTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;

        if (!isReturning)
        {
            // Start return when delay expires or projectile lands early
            if (timer >= returnDelay || (projectile != null && projectile.IsLanded))
            {
                StartReturn();
            }
            return;
        }

        // Returning: fly toward owner
        Vector2 toOwner = (Vector2)ownerTransform.position - (Vector2)transform.position;
        float dist = toOwner.magnitude;

        if (dist < 0.5f || timer > returnDelay + maxReturnTime)
        {
            // Reached owner or timeout — return ammo
            var spawner = ownerTransform.GetComponent<ProjectileSpawner>();
            if (spawner != null)
                spawner.ReturnAmmo(1);

            Destroy(gameObject);
            return;
        }

        rb.linearVelocity = toOwner.normalized * returnSpeed;

        float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void StartReturn()
    {
        isReturning = true;

        // Un-land if needed
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;

        // Returning axes can hit the owner (the downside)
        if (projectile != null)
            projectile.CanHitOwner = true;
    }
}
