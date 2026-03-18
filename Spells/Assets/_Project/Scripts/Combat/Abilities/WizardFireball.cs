using UnityEngine;

/// <summary>
/// Wizard special ability: lobbed fireball.
/// Spawns a large, arcing projectile that deals heavy damage.
/// Uses the player's existing projectile prefab with overridden size,
/// speed, gravity, and damage values for a distinct lobbed feel.
/// </summary>
public class WizardFireball : ClassAbility
{
    [Header("Fireball Settings")]
    [SerializeField] private float fireballSpeed = 12f;
    [SerializeField] private float fireballDamage = 3f;
    [SerializeField] private float fireballKnockback = 14f;
    [SerializeField] private float fireballHitstun = 0.25f;
    [SerializeField] private float fireballRadius = 0.4f;
    [SerializeField] private float fireballGravity = 2f;
    [SerializeField] private float fireballLifetime = 4f;
    [SerializeField] private float fireballScale = 3f;
    [SerializeField] private float lobAngle = 40f;

    private ProjectileSpawner spawner;
    private BoxCollider2D col;

    protected override void Start()
    {
        base.Start();
        abilityName = "Fireball";
        cooldownDuration = 3f;

        spawner = GetComponent<ProjectileSpawner>();
        col = GetComponent<BoxCollider2D>();
    }

    protected override void Activate()
    {
        if (Input == null || Rb == null || spawner == null) return;

        GameObject prefab = GetProjectilePrefab();
        if (prefab == null) return;

        Vector2 aimDir = GetAimDirection();

        // Lob: launch at lobAngle above horizontal in the facing direction
        float facing = Mathf.Sign(aimDir.x);
        if (Mathf.Abs(facing) < 0.1f) facing = 1f;
        float angleRad = lobAngle * Mathf.Deg2Rad;
        aimDir = new Vector2(facing * Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        // Spawn position: clear past the player's collider, accounting for scaled radius
        Vector2 center = col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;
        Vector2 half = col != null ? (Vector2)col.bounds.extents : new Vector2(0.4f, 0.5f);
        float scaledRadius = fireballRadius * fireballScale;
        float clearance = Mathf.Abs(aimDir.x) * half.x
                        + Mathf.Abs(aimDir.y) * half.y
                        + scaledRadius + 0.3f;
        Vector2 spawnPos = center + aimDir * clearance;

        GameObject projObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();

        if (proj != null)
        {
            proj.CanHitOwner = false;
            proj.Initialize(
                Identity.PlayerID,
                aimDir,
                fireballSpeed,
                fireballDamage,
                fireballKnockback,
                fireballHitstun,
                fireballLifetime,
                fireballRadius,
                fireballGravity,
                false,  // no bouncing
                0,
                false,  // no piercing
                false   // not retrievable
            );

            // Override prefab defaults — Initialize uses bulletGravity/bulletBounces
            // from SerializeField instead of the parameters we pass
            var rb = projObj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.gravityScale = fireballGravity;
            proj.DisableBouncing();
        }

        // Scale up the projectile visually
        projObj.transform.localScale = Vector3.one * fireballScale;

        // Apply projectile modifiers from cards
        var modSystem = GetComponent<ProjectileModifierSystem>();
        if (modSystem != null)
        {
            modSystem.ProcessProjectile(projObj);
        }
    }

    private Vector2 GetAimDirection()
    {
        // Right stick (gamepad) or move input (keyboard)
        if (Input.AimDirection.sqrMagnitude > 0.01f && Input.AimDirection.sqrMagnitude <= 1.5f)
            return Input.AimDirection.normalized;

        if (Input.MoveInput.sqrMagnitude > 0.01f)
            return Input.MoveInput.normalized;

        // Default to facing direction
        var controller = GetComponent<PlayerController>();
        float facing = controller != null ? controller.FacingDirection : 1f;
        return new Vector2(facing, 0f);
    }

    private GameObject GetProjectilePrefab()
    {
        // Use the class's assigned projectile prefab via ClassManager
        var classManager = GetComponent<ClassManager>();
        if (classManager != null && classManager.CurrentClass != null
            && classManager.CurrentClass.projectilePrefab != null)
        {
            return classManager.CurrentClass.projectilePrefab;
        }

        // Fallback: try ProjectileSpawner's prefab
        return null;
    }
}
