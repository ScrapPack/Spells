using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles projectile firing for a player. Reads CombatData for timing and
/// projectile configuration, creates projectiles on shoot input.
/// Supports ammo-limited classes (Warrior axes).
/// </summary>
public class ProjectileSpawner : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;

    [Header("Aim")]
    [Tooltip("Offset from player center where projectile spawns")]
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0.5f, 0f);

    [Header("Events")]
    public UnityEvent OnProjectileFired;

    public int CurrentAmmo { get; private set; }
    public bool HasAmmo => !usesAmmo || CurrentAmmo > 0;

    /// <summary>Reference to the most recently fired projectile (for SpellEffects to modify).</summary>
    public GameObject LastFiredProjectile { get; private set; }

    private CombatData combatData;
    private PlayerIdentity identity;
    private IInputProvider input;
    private BoxCollider2D col;
    private float fireCooldownTimer;
    private bool usesAmmo;

    /// <summary>
    /// Initialize with combat data. Called by ClassManager.
    /// </summary>
    public void Initialize(CombatData data, GameObject prefab)
    {
        combatData = data;
        if (prefab != null) projectilePrefab = prefab;

        usesAmmo = combatData.maxAmmo > 0;
        CurrentAmmo = combatData.maxAmmo;
        fireCooldownTimer = 0f;
    }

    private void Start()
    {
        identity = GetComponent<PlayerIdentity>();
        input = GetComponent<IInputProvider>();
        col = GetComponent<BoxCollider2D>();

        if (identity == null) Debug.LogError("ProjectileSpawner: No PlayerIdentity found!", this);
    }

    private void Update()
    {
        if (combatData == null || input == null) return;

        // Cooldown tick
        if (fireCooldownTimer > 0f)
            fireCooldownTimer -= Time.deltaTime;

        // Fire on shoot input
        if (input.ShootPressed && fireCooldownTimer <= 0f && HasAmmo)
        {
            Fire();
            input.ConsumeShoot();
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("ProjectileSpawner: No projectile prefab assigned!", this);
            return;
        }

        // Aim direction: right stick (gamepad) or WASD/move input (keyboard).
        // Mouse gives screen-space pixel coords (sqrMagnitude >> 100) — skip it and use move input.
        Vector2 aimDir;
        if (input.AimDirection.sqrMagnitude > 0.01f && input.AimDirection.sqrMagnitude <= 1.5f)
            aimDir = input.AimDirection; // right stick
        else
            aimDir = input.MoveInput;   // WASD or left stick

        if (aimDir.sqrMagnitude < 0.01f)
        {
            // No directional input — fire in the direction the player is facing
            var controller = GetComponent<PlayerController>();
            float facing = controller != null ? controller.FacingDirection : 1f;
            aimDir = new Vector2(facing, 0f);
        }

        aimDir = aimDir.normalized;

        // Spawn position: use col.bounds (world-space AABB) so the center and extents
        // are correct even when the player is flipped via localScale.x = -1.
        // Project extents onto aim direction to find the surface point, then add
        // the projectile radius + a small gap so the circle never overlaps the hurtbox.
        Vector2 center = col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;
        Vector2 half   = col != null ? (Vector2)col.bounds.extents : new Vector2(0.4f, 0.5f);
        float clearance = Mathf.Abs(aimDir.x) * half.x
                        + Mathf.Abs(aimDir.y) * half.y
                        + combatData.projectileRadius + 0.1f;
        Vector2 spawnPos = center + aimDir * clearance;

        // Create projectile
        GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();

        if (proj != null)
        {
            proj.CanHitOwner = true;
            proj.Initialize(
                identity.PlayerID,
                aimDir,
                combatData.projectileSpeed,
                combatData.projectileDamage,
                combatData.knockbackForce,
                combatData.hitstunDuration,
                combatData.projectileLifetime,
                combatData.projectileRadius,
                combatData.projectileGravity,
                combatData.projectileBounces,
                combatData.maxBounces,
                combatData.projectilePierces,
                combatData.retrievableProjectiles
            );
        }

        // Apply projectile modifiers (homing, explosive, split, etc.)
        var modSystem = GetComponent<ProjectileModifierSystem>();
        if (modSystem != null)
        {
            modSystem.ProcessProjectile(projObj);
        }

        // Store reference for SpellEffects (Magnetic Return, Venom Dart, etc.)
        LastFiredProjectile = projObj;

        // Ammo
        if (usesAmmo)
            CurrentAmmo--;

        // Cooldown
        fireCooldownTimer = combatData.fireCooldown;

        // Analytics
        var analytics = Object.FindAnyObjectByType<CombatAnalytics>();
        if (analytics != null && identity != null)
            analytics.RecordProjectileFired(identity.PlayerID);

        // Notify listeners (Blood Pact, etc.)
        OnProjectileFired?.Invoke();
    }

    /// <summary>
    /// Return ammo (Warrior picks up landed axe).
    /// </summary>
    public void ReturnAmmo(int amount)
    {
        if (!usesAmmo) return;
        CurrentAmmo = Mathf.Min(CurrentAmmo + amount, combatData.maxAmmo);
    }

    /// <summary>
    /// Reset for new round.
    /// </summary>
    public void ResetForRound()
    {
        if (usesAmmo)
            CurrentAmmo = combatData.maxAmmo;
        fireCooldownTimer = 0f;
    }
}
