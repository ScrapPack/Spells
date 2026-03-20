using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles projectile firing for a player. Reads CombatData for timing and
/// projectile configuration, creates projectiles on shoot input.
///
/// Ammo system: starts with <see cref="startAmmo"/> shots. When fully depleted,
/// automatically refills after <see cref="refillTime"/> seconds.
/// </summary>
public class ProjectileSpawner : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;

    [Header("Aim")]
    [Tooltip("Offset from player center where projectile spawns")]
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0.5f, 0f);

    [Header("Ammo")]
    [Tooltip("Number of shots before a reload is required.")]
    [SerializeField] private int startAmmo = 3;
    [Tooltip("Seconds until ammo fully refills after the last shot is used.")]
    [SerializeField] private float refillTime = 5f;

    [Header("Events")]
    public UnityEvent OnProjectileFired;
    public UnityEvent OnAmmoRefilled;

    // ── Public state ──────────────────────────────────────────────────────────

    public int CurrentAmmo { get; private set; }
    public int MaxAmmo => startAmmo;
    public bool HasAmmo => CurrentAmmo > 0;

    /// <summary>Seconds remaining until the next full refill (0 when not reloading).</summary>
    public float RefillCountdown { get; private set; }

    /// <summary>0–1 progress toward the next refill (1 = full / not reloading).</summary>
    public float RefillProgress => refillTime > 0f ? 1f - (RefillCountdown / refillTime) : 1f;

    /// <summary>Configurable refill duration. Writable so SpellEffects can tune it.</summary>
    public float RefillTime
    {
        get => refillTime;
        set => refillTime = Mathf.Max(0f, value);
    }

    /// <summary>Projectile speed pulled from the cloned CombatData. Writable per-player.</summary>
    public float ProjectileSpeed
    {
        get => combatData != null ? combatData.projectileSpeed : 0f;
        set { if (combatData != null) combatData.projectileSpeed = value; }
    }

    /// <summary>Reference to the most recently fired projectile (for SpellEffects to modify).</summary>
    public GameObject LastFiredProjectile { get; set; }

    /// <summary>When true, normal firing is blocked (a SpellEffect is handling shooting).</summary>
    public bool IsChargingShot { get; set; }

    // ── Spread ────────────────────────────────────────────────────────────────

    /// <summary>How many bullets fire per shot. 1 = normal, 3 = buckshot, etc.</summary>
    public int BulletSpreadCount { get; set; } = 1;

    /// <summary>Total arc (degrees) over which spread bullets are distributed. 0 = no spread.</summary>
    public float BulletSpreadAngle { get; set; } = 0f;

    /// <summary>Damage multiplier per bullet when spread > 1. Keeps total DPS sane.</summary>
    public float SpreadDamageMultiplier { get; set; } = 1f;

    // ── Full Auto ─────────────────────────────────────────────────────────────

    /// <summary>When true, holding Shoot fires continuously at full-auto rate.</summary>
    public bool FullAutoMode { get; set; }

    /// <summary>Multiplier applied to fireCooldown in full-auto mode. 1/3 = 3× fire rate.</summary>
    public float FullAutoFireCooldownMultiplier { get; set; } = 1f;

    // ── Bonus bounces ─────────────────────────────────────────────────────────

    /// <summary>Extra bounces added to each spawned projectile beyond the prefab default.</summary>
    public int BonusMaxBounces { get; set; }

    // ── Private state ─────────────────────────────────────────────────────────

    private CombatData combatData;
    private PlayerIdentity identity;
    private IInputProvider input;
    private AimController aimController;
    private BoxCollider2D col;
    private ParrySystem parrySystem;
    private ClassAbility classAbility;
    private ProjectileModifierSystem modSystem;
    private float fireCooldownTimer;

    // ── Initialization ────────────────────────────────────────────────────────

    /// <summary>
    /// Initialize with combat data. Called by ClassManager.
    /// </summary>
    public void Initialize(CombatData data, GameObject prefab)
    {
        combatData = data;
        if (prefab != null) projectilePrefab = prefab;
        fireCooldownTimer = 0f;

        // Seed ammo from SerializeField; ignore CombatData.maxAmmo for this scene.
        CurrentAmmo    = startAmmo;
        RefillCountdown = 0f;
    }

    private void Start()
    {
        identity      = GetComponent<PlayerIdentity>();
        input         = GetComponent<IInputProvider>();
        aimController = GetComponent<AimController>();
        col           = GetComponent<BoxCollider2D>();
        parrySystem   = GetComponent<ParrySystem>();
        classAbility  = GetComponent<ClassAbility>();
        modSystem     = GetComponent<ProjectileModifierSystem>();

        if (identity == null) Debug.LogError("ProjectileSpawner: No PlayerIdentity found!", this);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (combatData == null || input == null) return;

        // Fire cooldown
        if (fireCooldownTimer > 0f)
            fireCooldownTimer -= Time.deltaTime;

        // Auto-refill when depleted (blocked while charging)
        if (CurrentAmmo <= 0 && !IsChargingShot)
        {
            RefillCountdown -= Time.deltaTime;
            if (RefillCountdown <= 0f)
            {
                CurrentAmmo     = startAmmo;
                RefillCountdown = 0f;
                OnAmmoRefilled?.Invoke();
            }
        }


        // Fire on shoot input — always consume so empty-mag clicks don't buffer.
        // Blocked while parrying, ability active (e.g. shield), or charge shot effect present.
        bool parryLocked   = parrySystem != null && (parrySystem.IsParrying || parrySystem.IsInRecovery);
        bool abilityLocked = classAbility != null && classAbility.IsActive;
        bool shootTap  = input.ShootPressed;
        bool shootHold = FullAutoMode && input.ShootHeld;
        if (shootTap || shootHold)
        {
            if (fireCooldownTimer <= 0f && HasAmmo && !parryLocked && !abilityLocked && !IsChargingShot)
                Fire(FullAutoMode ? FullAutoFireCooldownMultiplier : 1f);
            if (shootTap)
                input.ConsumeShoot();
        }
    }

    // ── Firing ────────────────────────────────────────────────────────────────

    private void Fire(float cooldownMultiplier = 1f)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("ProjectileSpawner: No projectile prefab assigned!", this);
            return;
        }

        Vector2 aimDir = aimController != null ? aimController.AimDirection : Vector2.right;
        Vector2 center = col != null ? (Vector2)col.bounds.center  : (Vector2)transform.position;
        Vector2 half   = col != null ? (Vector2)col.bounds.extents : new Vector2(0.4f, 0.5f);

        // Spawn one bullet per spread slot, fanned evenly across BulletSpreadAngle.
        // Each bullet invokes OnProjectileFired so per-bullet effects (LuckyBounce, etc.)
        // are applied correctly. Ammo and cooldown happen once after the loop.
        int   count    = Mathf.Max(1, BulletSpreadCount);
        float halfSpan = BulletSpreadAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            // Full 360° ring: distribute evenly without overlap at endpoints.
            // Partial spread: linear fan from -halfSpan to +halfSpan.
            float angleOffset = count > 1
                ? (Mathf.Approximately(BulletSpreadAngle, 360f)
                    ? (float)i / count * 360f
                    : Mathf.Lerp(-halfSpan, halfSpan, (float)i / (count - 1)))
                : 0f;

            float   rad    = angleOffset * Mathf.Deg2Rad;
            Vector2 dir    = new Vector2(
                aimDir.x * Mathf.Cos(rad) - aimDir.y * Mathf.Sin(rad),
                aimDir.x * Mathf.Sin(rad) + aimDir.y * Mathf.Cos(rad));

            // Recalculate clearance per-bullet direction so none spawn inside the collider
            float   clear    = Mathf.Abs(dir.x) * half.x + Mathf.Abs(dir.y) * half.y
                             + combatData.projectileRadius + 0.1f;
            Vector2 spawnPos = center + dir * clear;

            var projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj    = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.CanHitOwner = true;
                proj.Initialize(
                    identity.PlayerID, dir,
                    combatData.projectileSpeed,
                    combatData.projectileDamage * SpreadDamageMultiplier,
                    combatData.knockbackForce,
                    combatData.hitstunDuration,
                    combatData.projectileLifetime,
                    combatData.projectileRadius,
                    combatData.projectileGravity,
                    combatData.projectileBounces,
                    combatData.maxBounces,
                    combatData.projectilePierces,
                    combatData.retrievableProjectiles);
            }

            if (BonusMaxBounces > 0)
                proj?.AddBounces(BonusMaxBounces);

            modSystem?.ProcessProjectile(projObj);
            LastFiredProjectile = projObj;
            OnProjectileFired.Invoke(); // per-bullet so behavior hooks apply to each bullet
        }

        // Ammo and cooldown — once per trigger pull regardless of spread count
        CurrentAmmo--;
        if (CurrentAmmo <= 0)
            RefillCountdown = refillTime;

        fireCooldownTimer = combatData.fireCooldown * cooldownMultiplier;

        // Analytics
        var analytics = Object.FindAnyObjectByType<CombatAnalytics>();
        if (analytics != null && identity != null)
            analytics.RecordProjectileFired(identity.PlayerID);
    }

    // ── External API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Fire a free ring of bullets in all directions — no ammo cost, no cooldown.
    /// Used by Death Blossom on reload completion.
    /// </summary>
    public void FireFreeRing(int bulletCount, float damageMultiplier)
    {
        if (projectilePrefab == null || combatData == null) return;

        Vector2 center = col != null ? (Vector2)col.bounds.center  : (Vector2)transform.position;
        Vector2 half   = col != null ? (Vector2)col.bounds.extents : new Vector2(0.4f, 0.5f);
        int ownerID    = identity != null ? identity.PlayerID : -1;

        for (int i = 0; i < bulletCount; i++)
        {
            float angleDeg = (float)i / bulletCount * 360f;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            float clear    = Mathf.Abs(dir.x) * half.x + Mathf.Abs(dir.y) * half.y
                           + combatData.projectileRadius + 0.1f;
            Vector2 spawnPos = center + dir * clear;

            var projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            var proj    = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.CanHitOwner = true;
                proj.Initialize(
                    ownerID, dir,
                    combatData.projectileSpeed,
                    combatData.projectileDamage * damageMultiplier,
                    combatData.knockbackForce,
                    combatData.hitstunDuration,
                    combatData.projectileLifetime,
                    combatData.projectileRadius,
                    combatData.projectileGravity,
                    combatData.projectileBounces,
                    combatData.maxBounces,
                    combatData.projectilePierces,
                    combatData.retrievableProjectiles);
            }

            modSystem?.ProcessProjectile(projObj);
            LastFiredProjectile = projObj;
            OnProjectileFired.Invoke();
        }
    }

    /// <summary>
    /// Consume ammo without firing (used by ChargeShotEffect while charging).
    /// Does NOT start the refill timer — call StartRefillIfEmpty after firing.
    /// </summary>
    public void ConsumeAmmo(int amount)
    {
        CurrentAmmo = Mathf.Max(0, CurrentAmmo - amount);
    }

    /// <summary>
    /// Start the refill timer if ammo is depleted. Called after a charge shot fires.
    /// </summary>
    public void StartRefillIfEmpty()
    {
        if (CurrentAmmo <= 0)
            RefillCountdown = refillTime;
    }
    /// Permanently change the max ammo capacity (used by SpellEffects like Extended Clip).
    /// Clamps to a minimum of 1. Also adjusts current ammo proportionally.
    /// </summary>
    public void AdjustMaxAmmo(int delta)
    {
        startAmmo = Mathf.Max(1, startAmmo + delta);
        CurrentAmmo = Mathf.Clamp(CurrentAmmo + delta, 0, startAmmo);
    }

    /// <summary>
    /// Return ammo (e.g. Warrior picks up a landed axe). Cancels refill if now non-empty.
    /// </summary>
    public void ReturnAmmo(int amount)
    {
        CurrentAmmo = Mathf.Min(CurrentAmmo + amount, startAmmo);
        if (CurrentAmmo > 0)
            RefillCountdown = 0f;
    }

    /// <summary>
    /// Reset for new round — full ammo, no cooldowns.
    /// </summary>
    public void ResetForRound()
    {
        CurrentAmmo     = startAmmo;
        RefillCountdown = 0f;
        fireCooldownTimer = 0f;
    }
}
