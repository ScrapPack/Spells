using System.Collections.Generic;
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
    /// <summary>
    /// All currently active Projectile instances.
    /// Populated via OnEnable/OnDisable — zero allocation, no scene scan.
    /// </summary>
    public static readonly List<Projectile> All = new List<Projectile>();

    [Header("Prefab Overrides")]
    [Tooltip("Multiplies the lifetime from CombatData. 5 = five times longer.")]
    [SerializeField] private float lifetimeMultiplier = 5f;

    [Tooltip("How fast the bullet falls. 0 = no gravity, 1 = normal Unity gravity. Keep low (0.1–0.3) for slow arc.")]
    [SerializeField] private float bulletGravity = 0.15f;

    [Tooltip("How many times the bullet bounces off walls and ground before dying.")]
    [SerializeField] private int bulletBounces = 3;

    public int OwnerPlayerID { get; private set; }
    public float Damage { get; private set; }
    public float KnockbackForce { get; private set; }
    public float HitstunDuration { get; private set; }
    public bool IsReflected { get; private set; }

    /// <summary>Player ID that just reflected this projectile. Immune to hitting them briefly.</summary>
    private int reflectedByPlayerID = -1;
    private float reflectionImmunityTimer;

    // === Extension points for SpellEffects ===

    /// <summary>Multiplier applied to damage. Modified by Lucky Bounce, Ambush, etc.</summary>
    public float DamageMultiplier { get; set; } = 1f;

    /// <summary>Current number of wall bounces (for Lucky Bounce damage scaling).</summary>
    public int CurrentBounceCount => bounceCount;

    /// <summary>When true, projectile won't auto-destroy on lifetime expiry (Magnetic Return).</summary>
    public bool PreventAutoExpire { get; set; }

    /// <summary>When true, projectile can damage its owner (returning axes).</summary>
    public bool CanHitOwner { get; set; }

    /// <summary>Disable bouncing after Initialize (overrides prefab bulletBounces).</summary>
    public void DisableBouncing()
    {
        bounces = false;
        maxBounces = 0;
    }

    /// <summary>Called just before dealing damage. Allows modifying DamageMultiplier.</summary>
    public System.Action<GameObject> OnBeforeHit;

    /// <summary>Called after successfully hitting a player. (target, finalDamage)</summary>
    public System.Action<GameObject, float> OnHitPlayer;

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

    private void OnEnable()  => All.Add(this);
    private void OnDisable() => All.Remove(this);

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

        lifetime = projectileLifetime * lifetimeMultiplier;
        lifetimeTimer = 0f;
        bounces = bulletBounces > 0;
        maxBounces = bulletBounces;
        bounceCount = 0;
        pierces = pierce;
        retrievable = retrieve;

        // Collider setup
        col.radius = radius;
        col.isTrigger = true;

        // Rigidbody setup — bulletGravity overrides CombatData gravity
        rb.gravityScale = bulletGravity;
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
        reflectedByPlayerID = newOwnerID;
        reflectionImmunityTimer = 0.15f;

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

        if (reflectionImmunityTimer > 0f)
            reflectionImmunityTimer -= Time.deltaTime;

        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= lifetime)
        {
            if (PreventAutoExpire) { /* External component handles expiry */ }
            else if (retrievable)
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

        // Hit a monster (PvE target)
        var monster = other.GetComponent<MonsterEntity>();
        if (monster != null && OwnerPlayerID >= 0)
        {
            float finalDamage = Damage * DamageMultiplier;
            monster.TakeDamage(Mathf.RoundToInt(finalDamage), OwnerPlayerID);
            if (!pierces)
            {
                Destroy(gameObject);
            }
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

        // Brief immunity for the player who just reflected this projectile
        var otherID = other.GetComponent<PlayerIdentity>();
        if (otherID != null && otherID.PlayerID == reflectedByPlayerID && reflectionImmunityTimer > 0f)
            return;

        // Check if this is the owner (skip unless reflected or CanHitOwner)
        if (otherID != null && otherID.PlayerID == OwnerPlayerID && !IsReflected && !CanHitOwner)
            return;

        // Pre-hit callback: allows modifying DamageMultiplier (Ambush facing check, etc.)
        OnBeforeHit?.Invoke(other.gameObject);

        // Apply damage with multiplier
        float finalDamage = Damage * DamageMultiplier;
        bool didDamage = otherHealth.TakeDamage(finalDamage, OwnerPlayerID);

        // Apply knockback and hitstun
        if (didDamage)
        {
            // Post-hit callback: poison application, hex marking, etc.
            OnHitPlayer?.Invoke(other.gameObject, finalDamage);

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
            // Get the true surface normal via closest-points query so floor/wall/ceiling
            // bounces all reflect correctly (not just walls).
            ColliderDistance2D dist = Physics2D.Distance(col, other);
            Vector2 normal = dist.isValid ? dist.normal : -(other.transform.position - transform.position).normalized;
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
