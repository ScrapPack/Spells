using System.Collections;
using UnityEngine;

/// <summary>
/// Charge Shot: Press and hold shoot to charge. Consumes 1 ammo immediately,
/// then additional ammo over time while held. On release, fires a single shot
/// scaled by total ammo consumed. Quick tap = 1 ammo = normal shot.
/// Projectile tints toward red the more it's charged.
/// </summary>
public class ChargeShotEffect : SpellEffect
{
    private float ammoConsumeInterval = 0.7f;   // seconds between each extra ammo consumed
    private float damagePerAmmo = 1f;            // bonus damage per extra ammo (beyond first)
    private float sizePerAmmo = 0.4f;            // bonus radius per extra ammo — 5x at ~2 bonus ammo

    // Cached refs (fetched lazily since base class cache may be null)
    private IInputProvider cachedInput;
    private AimController cachedAim;
    private BoxCollider2D cachedCol;
    private ProjectileSpawner cachedSpawner;
    private ClassManager cachedClass;
    private PlayerIdentity cachedIdentity;

    private bool isHolding;
    private float consumeTimer;
    private int ammoConsumed;

    protected override void OnApply()
    {
        CacheRefs();
    }

    public override void OnRoundStart()
    {
        isHolding = false;
        consumeTimer = 0f;
        ammoConsumed = 0;
    }

    private void CacheRefs()
    {
        if (cachedInput == null) cachedInput = GetComponent<IInputProvider>();
        if (cachedAim == null) cachedAim = GetComponent<AimController>();
        if (cachedCol == null) cachedCol = GetComponent<BoxCollider2D>();
        if (cachedSpawner == null) cachedSpawner = GetComponent<ProjectileSpawner>();
        if (cachedClass == null) cachedClass = GetComponent<ClassManager>();
        if (cachedIdentity == null) cachedIdentity = GetComponent<PlayerIdentity>();
    }

    private void Update()
    {
        CacheRefs();

        if (cachedSpawner == null || cachedInput == null) return;
        if (cachedClass == null || cachedClass.CombatData == null) return;

        var ability = GetComponent<ClassAbility>();
        if (ability != null && ability.IsActive) return;

        if (cachedInput.ShootHeld)
        {
            if (!isHolding && cachedSpawner.HasAmmo)
            {
                isHolding = true;
                cachedSpawner.IsChargingShot = true;  // Block refill + normal fire
                consumeTimer = 0f;
                ammoConsumed = 0;
                ConsumeOneAmmo();
            }

            if (isHolding && cachedSpawner.HasAmmo)
            {
                consumeTimer += Time.deltaTime;
                if (consumeTimer >= ammoConsumeInterval)
                {
                    consumeTimer -= ammoConsumeInterval;
                    ConsumeOneAmmo();
                }
            }
        }
        else if (isHolding)
        {
            Debug.Log($"[ChargeShot] Release — ammoConsumed={ammoConsumed}, identity={cachedIdentity != null}, class={cachedClass != null}, combatData={cachedClass?.CombatData != null}, prefab={cachedClass?.CurrentClass?.projectilePrefab != null}");
            FireShot();

            // Now allow refill to start
            cachedSpawner.IsChargingShot = false;
            cachedSpawner.StartRefillIfEmpty();

            isHolding = false;
            consumeTimer = 0f;
            ammoConsumed = 0;
        }
    }

    private void ConsumeOneAmmo()
    {
        if (!cachedSpawner.HasAmmo) return;
        cachedSpawner.ConsumeAmmo(1);
        ammoConsumed++;
    }

    private void FireShot()
    {
        if (ammoConsumed <= 0)
        {
            Debug.Log("[ChargeShot] FireShot skipped — ammoConsumed is 0");
            return;
        }

        var combatData = cachedClass.CombatData;
        var prefab = cachedClass.CurrentClass != null ? cachedClass.CurrentClass.projectilePrefab : null;
        if (prefab == null || combatData == null)
        {
            Debug.Log($"[ChargeShot] FireShot skipped — prefab={prefab != null}, combatData={combatData != null}");
            return;
        }
        Debug.Log($"[ChargeShot] Spawning projectile — damage={combatData.projectileDamage + (damagePerAmmo * (ammoConsumed - 1))}, radius={combatData.projectileRadius + (sizePerAmmo * (ammoConsumed - 1))}");

        Vector2 aimDir = cachedAim != null
            ? cachedAim.AimDirection
            : Vector2.right;

        // Scale by total ammo consumed (1 = base shot, 2+ = charged)
        float finalDamage = combatData.projectileDamage * ammoConsumed;
        float baseRadius = combatData.projectileRadius;
        float scaleMultiplier = 1f + (sizePerAmmo * (ammoConsumed - 1));

        // Spawn position — clearance based on scaled size
        float scaledRadius = baseRadius * scaleMultiplier;
        Vector2 center = cachedCol != null ? (Vector2)cachedCol.bounds.center : (Vector2)transform.position;
        Vector2 half = cachedCol != null ? (Vector2)cachedCol.bounds.extents : new Vector2(0.4f, 0.5f);
        float clearance = Mathf.Abs(aimDir.x) * half.x
                        + Mathf.Abs(aimDir.y) * half.y
                        + scaledRadius + 0.3f;
        Vector2 spawnPos = center + aimDir * clearance;

        GameObject projObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();

        if (proj != null)
        {
            proj.CanHitOwner = false;  // Temporarily disable — re-enabled after clearing player
            proj.Initialize(
                cachedIdentity.PlayerID,
                aimDir,
                combatData.projectileSpeed,
                finalDamage,
                combatData.knockbackForce,
                combatData.hitstunDuration,
                combatData.projectileLifetime,
                baseRadius,             // Use base radius — localScale handles the rest
                combatData.projectileGravity,
                combatData.projectileBounces,
                combatData.maxBounces,
                combatData.projectilePierces,
                combatData.retrievableProjectiles
            );

            // Scale collider via transform
            projObj.transform.localScale *= scaleMultiplier;

            // Give the projectile a visible sprite body (prefab has no sprite assigned)
            var sr = projObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                sr.sprite = CreateCircleSprite();
            }

            // Scale the trail width to match charged size
            var trail = projObj.GetComponent<TrailRenderer>();
            if (trail == null) trail = projObj.GetComponentInChildren<TrailRenderer>();
            if (trail != null)
            {
                trail.startWidth *= scaleMultiplier;
                trail.endWidth *= scaleMultiplier;
            }

            // Tint toward red based on charge level
            if (ammoConsumed > 1 && sr != null)
            {
                float chargeFraction = Mathf.Clamp01((ammoConsumed - 1) / 3f);
                Color baseColor = sr.color;
                Color redTint = new Color(1f, 0.2f, 0.1f, baseColor.a);
                sr.color = Color.Lerp(baseColor, redTint, chargeFraction);
            }
        }

        var modSystem = GetComponent<ProjectileModifierSystem>();
        if (modSystem != null)
            modSystem.ProcessProjectile(projObj);

        cachedSpawner.LastFiredProjectile = projObj;

        var analytics = Object.FindAnyObjectByType<CombatAnalytics>();
        if (analytics != null && cachedIdentity != null)
            analytics.RecordProjectileFired(cachedIdentity.PlayerID);

        cachedSpawner.OnProjectileFired?.Invoke();

        // Re-enable self-damage after the projectile clears the player
        StartCoroutine(EnableOwnerHitAfterDelay(proj, 0.15f));
    }

    private IEnumerator EnableOwnerHitAfterDelay(Projectile proj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (proj != null)
            proj.CanHitOwner = true;
    }

    /// <summary>
    /// Creates a simple filled circle sprite at runtime for the projectile body.
    /// </summary>
    private static Sprite cachedCircleSprite;
    private static Sprite CreateCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radiusSq = center * center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                tex.SetPixel(x, y, dx * dx + dy * dy <= radiusSq ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        cachedCircleSprite = Sprite.Create(tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        return cachedCircleSprite;
    }

    public override void OnRemove()
    {
        if (cachedSpawner != null)
            cachedSpawner.IsChargingShot = false;
        isHolding = false;
    }
}
