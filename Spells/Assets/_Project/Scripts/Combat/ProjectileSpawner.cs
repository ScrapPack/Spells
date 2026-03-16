using UnityEngine;

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

    public int CurrentAmmo { get; private set; }
    public bool HasAmmo => !usesAmmo || CurrentAmmo > 0;

    private CombatData combatData;
    private PlayerIdentity identity;
    private IInputProvider input;
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

        // Aim direction: use input aim or face direction
        Vector2 aimDir = input.AimDirection;
        if (aimDir.sqrMagnitude < 0.01f)
        {
            // Default: face direction based on movement or last facing
            float facing = Mathf.Sign(input.MoveInput.x);
            if (Mathf.Abs(facing) < 0.1f) facing = 1f;
            aimDir = new Vector2(facing, 0f);
        }

        // Spawn position
        Vector2 spawnPos = (Vector2)transform.position + new Vector2(
            muzzleOffset.x * Mathf.Sign(aimDir.x),
            muzzleOffset.y
        );

        // Create projectile
        GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();

        if (proj != null)
        {
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

        // Ammo
        if (usesAmmo)
            CurrentAmmo--;

        // Cooldown
        fireCooldownTimer = combatData.fireCooldown;
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
