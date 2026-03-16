using UnityEngine;

/// <summary>
/// Core projectile behaviour. Handles movement, lifetime, collision,
/// bouncing, piercing, and retrievable axes. Configured by CombatData
/// at spawn time — no direct ScriptableObject reference (data comes from spawner).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Projectile : MonoBehaviour
{
    public int OwnerPlayerID { get; private set; }
    public float Damage { get; private set; }
    public float KnockbackForce { get; private set; }
    public float HitstunDuration { get; private set; }
    public bool IsReflected { get; private set; }

    private Rigidbody2D rb;
    private CircleCollider2D col;

    private float lifetime;
    private float lifetimeTimer;
    private bool bounces;
    private int maxBounces;
    private int bounceCount;
    private bool pierces;
    private bool retrievable;
    private bool isLanded;

    // Layers
    private int playerLayer;
    private int groundLayer;
    private int wallLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        playerLayer = LayerMask.NameToLayer("Player");
        groundLayer = LayerMask.NameToLayer("Ground");
        wallLayer = LayerMask.NameToLayer("Wall");
    }

    /// <summary>
    /// Initialize projectile with all combat parameters.
    /// Called by ProjectileSpawner after instantiation.
    /// </summary>
    public void Initialize(int ownerID, Vector2 direction, float speed, float damage,
                           float knockback, float hitstun, float projectileLifetime,
                           float radius, float gravity, bool bounce, int maxBounce,
                           bool pierce, bool retrieve)
    {
        OwnerPlayerID = ownerID;
        Damage = damage;
        KnockbackForce = knockback;
        HitstunDuration = hitstun;
        IsReflected = false;
        isLanded = false;

        lifetime = projectileLifetime;
        lifetimeTimer = 0f;
        bounces = bounce;
        maxBounces = maxBounce;
        bounceCount = 0;
        pierces = pierce;
        retrievable = retrieve;

        // Collider setup
        col.radius = radius;
        col.isTrigger = true;

        // Rigidbody setup
        rb.gravityScale = gravity;
        rb.linearVelocity = direction.normalized * speed;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        // Rotate to face movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// Reflect this projectile (parry). Changes owner so it can hit original caster.
    /// </summary>
    public void Reflect(int newOwnerID, Vector2 newDirection, float speedMultiplier)
    {
        OwnerPlayerID = newOwnerID;
        IsReflected = true;
        lifetimeTimer = 0f;
        bounceCount = 0;

        rb.linearVelocity = newDirection.normalized * rb.linearVelocity.magnitude * speedMultiplier;

        float angle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// For retrievable projectiles (Warrior axes): mark as landed, become pickup.
    /// </summary>
    public void Land()
    {
        if (!retrievable) return;

        isLanded = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Keep trigger collider active for pickup detection
    }

    public bool IsLanded => isLanded;
    public bool IsRetrievable => retrievable && isLanded;

    private void Update()
    {
        if (isLanded) return;

        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= lifetime)
        {
            if (retrievable)
                Land();
            else
                Destroy(gameObject);
        }

        // Rotate to face velocity (for arcing projectiles)
        if (rb.gravityScale > 0f && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLanded)
        {
            // Pickup logic: owner walks over landed projectile
            HandlePickup(other);
            return;
        }

        // Hit a player
        if (other.gameObject.layer == playerLayer)
        {
            HandlePlayerHit(other);
            return;
        }

        // Hit a wall or ground
        if (other.gameObject.layer == groundLayer || other.gameObject.layer == wallLayer)
        {
            HandleEnvironmentHit(other);
        }
    }

    private void HandlePlayerHit(Collider2D other)
    {
        // Don't hit own owner (unless reflected)
        var otherHealth = other.GetComponent<HealthSystem>();
        if (otherHealth == null) return;

        // Check if this is the owner (skip unless reflected)
        var otherID = other.GetComponent<PlayerIdentity>();
        if (otherID != null && otherID.PlayerID == OwnerPlayerID && !IsReflected)
            return;

        // Apply damage
        bool didDamage = otherHealth.TakeDamage(Damage);

        // Apply knockback and hitstun
        if (didDamage)
        {
            var otherRb = other.GetComponent<Rigidbody2D>();
            if (otherRb != null)
            {
                Vector2 knockDir = rb.linearVelocity.normalized;
                otherRb.linearVelocity = knockDir * KnockbackForce;
            }

            // Trigger hitstun state
            var otherStateMachine = other.GetComponent<PlayerStateMachine>();
            if (otherStateMachine != null && HitstunDuration > 0f)
            {
                otherStateMachine.EnterHitstun(HitstunDuration);
            }

            // Screen shake
            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.ShakeOnHit();
            }
        }

        // Destroy unless piercing
        if (!pierces)
        {
            Destroy(gameObject);
        }
    }

    private void HandleEnvironmentHit(Collider2D other)
    {
        if (bounces && bounceCount < maxBounces)
        {
            // Bounce: reflect velocity off surface normal
            // Using contact point approximation since we're a trigger
            Vector2 toWall = (other.transform.position - transform.position).normalized;
            Vector2 normal = -toWall; // Approximate surface normal
            rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, normal);
            bounceCount++;

            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else if (retrievable)
        {
            Land();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void HandlePickup(Collider2D other)
    {
        var pickupID = other.GetComponent<PlayerIdentity>();
        if (pickupID == null || pickupID.PlayerID != OwnerPlayerID) return;

        var spawner = other.GetComponent<ProjectileSpawner>();
        if (spawner != null)
        {
            spawner.ReturnAmmo(1);
        }

        Destroy(gameObject);
    }
}
