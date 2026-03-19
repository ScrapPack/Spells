using UnityEngine;

/// <summary>
/// Charge Shot: Hold the shoot button to charge a powered shot.
/// While holding, ammo is consumed at a set rate.
/// Per ammo consumed: damage increases, projectile size increases.
/// Reload doesn't start until the charged shot is fired.
/// </summary>
public class ChargeShotEffect : SpellEffect
{
    [Header("Charge Settings")]
    private float ammoConsumeInterval = 0.4f;  // seconds between each ammo consumed
    private float damagePerAmmo = 0.5f;         // bonus damage per ammo consumed
    private float sizePerAmmo = 0.08f;          // bonus radius per ammo consumed

    private IInputProvider input;
    private AimController aimController;
    private BoxCollider2D col;

    private bool isCharging;
    private float chargeTimer;
    private int ammoConsumed;

    protected override void OnApply()
    {
        input = GetComponent<IInputProvider>();
        aimController = GetComponent<AimController>();
        col = GetComponent<BoxCollider2D>();
    }

    public override void OnRoundStart()
    {
        isCharging = false;
        chargeTimer = 0f;
        ammoConsumed = 0;
    }

    private void Update()
    {
        if (Spawner == null || input == null) return;
        if (Class == null || Class.CombatData == null) return;

        // Check if ability is active (e.g. shield) — block charging too
        var ability = GetComponent<ClassAbility>();
        if (ability != null && ability.IsActive) return;

        if (input.ShootHeld)
        {
            if (!isCharging && Spawner.HasAmmo)
            {
                // Start charging
                isCharging = true;
                Spawner.IsChargingShot = true;
                chargeTimer = 0f;
                ammoConsumed = 0;

                // Consume the first ammo immediately (the base shot)
                ConsumeOneAmmo();
            }

            if (isCharging && Spawner.HasAmmo)
            {
                // Consume additional ammo over time
                chargeTimer += Time.deltaTime;
                if (chargeTimer >= ammoConsumeInterval)
                {
                    chargeTimer -= ammoConsumeInterval;
                    ConsumeOneAmmo();
                }
            }
        }
        else if (isCharging)
        {
            // Released — fire the charged shot
            FireChargedShot();
            isCharging = false;
            Spawner.IsChargingShot = false;
            chargeTimer = 0f;
            ammoConsumed = 0;
        }
    }

    private void ConsumeOneAmmo()
    {
        if (!Spawner.HasAmmo) return;
        Spawner.ConsumeAmmo(1);
        ammoConsumed++;
    }

    private void FireChargedShot()
    {
        if (ammoConsumed <= 0) return;

        var combatData = Class.CombatData;
        var prefab = Class.CurrentClass != null ? Class.CurrentClass.projectilePrefab : null;
        if (prefab == null || combatData == null) return;

        // Aim direction
        Vector2 aimDir = aimController != null
            ? aimController.AimDirection
            : Vector2.right;

        // Extra ammo beyond the base shot
        int bonusAmmo = ammoConsumed - 1;

        // Scale stats by ammo consumed
        float finalDamage = combatData.projectileDamage + (damagePerAmmo * bonusAmmo);
        float finalRadius = combatData.projectileRadius + (sizePerAmmo * bonusAmmo);

        // Spawn position (same logic as ProjectileSpawner)
        Vector2 center = col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;
        Vector2 half = col != null ? (Vector2)col.bounds.extents : new Vector2(0.4f, 0.5f);
        float clearance = Mathf.Abs(aimDir.x) * half.x
                        + Mathf.Abs(aimDir.y) * half.y
                        + finalRadius + 0.1f;
        Vector2 spawnPos = center + aimDir * clearance;

        // Create projectile
        GameObject projObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();

        if (proj != null)
        {
            var identity = Identity;
            proj.CanHitOwner = true;
            proj.Initialize(
                identity.PlayerID,
                aimDir,
                combatData.projectileSpeed,
                finalDamage,
                combatData.knockbackForce,
                combatData.hitstunDuration,
                combatData.projectileLifetime,
                finalRadius,
                combatData.projectileGravity,
                combatData.projectileBounces,
                combatData.maxBounces,
                combatData.projectilePierces,
                combatData.retrievableProjectiles
            );

            // Scale the visual to match the larger radius
            if (bonusAmmo > 0)
            {
                float scaleMultiplier = finalRadius / combatData.projectileRadius;
                projObj.transform.localScale *= scaleMultiplier;
            }
        }

        // Apply projectile modifiers (homing, explosive, etc.)
        var modSystem = GetComponent<ProjectileModifierSystem>();
        if (modSystem != null)
            modSystem.ProcessProjectile(projObj);

        Spawner.LastFiredProjectile = projObj;

        // Now start the reload if out of ammo
        Spawner.StartRefillIfEmpty();

        // Analytics
        var analytics = Object.FindAnyObjectByType<CombatAnalytics>();
        if (analytics != null && Identity != null)
            analytics.RecordProjectileFired(Identity.PlayerID);

        Spawner.OnProjectileFired?.Invoke();
    }

    public override void OnRemove()
    {
        if (Spawner != null)
            Spawner.IsChargingShot = false;
        isCharging = false;
    }
}
