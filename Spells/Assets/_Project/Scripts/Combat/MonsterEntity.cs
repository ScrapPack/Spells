using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PvE monster entity with its own HP tracking (NOT HealthSystem).
/// Monsters are NOT registered with RoundManager — they exist alongside
/// the PvP combat as optional targets. Killing a monster grants a level.
///
/// AI behavior: Patrol → Detect → Telegraph → Attack → Cooldown
/// Uses negative OwnerPlayerID (-100 - MonsterID) for projectiles
/// so they hit all players (no friendly fire exclusion).
///
/// Pattern reference: TotemEntity (targeting, projectile spawning).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class MonsterEntity : MonoBehaviour
{
    public enum AIState { Patrol, Detect, Telegraph, Attack, Cooldown }

    public int MonsterID { get; private set; }
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public AIState CurrentAIState { get; private set; }

    [Header("Events")]
    public UnityEvent<int> OnMonsterKilled; // (killerPlayerID)
    public UnityEvent<int, int> OnDamaged;   // (currentHP, maxHP)

    private MonsterData data;
    private int totalLevelPool;
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private SpriteRenderer spriteRenderer;

    // Targeting
    private Transform currentTarget;
    private int lastAttackerID = -1;

    // Timers
    private float attackTimer;
    private float telegraphTimer;
    private float patrolTimer;
    private float patrolDirection = 1f;

    // Telegraph visual state
    private Color baseColor;
    private float telegraphFlashTimer;

    /// <summary>
    /// Initialize the monster with data and scaling.
    /// </summary>
    public void Initialize(MonsterData monsterData, int monsterID, int levelPool)
    {
        data = monsterData;
        MonsterID = monsterID;
        totalLevelPool = levelPool;

        MaxHP = data.GetScaledHP(totalLevelPool);
        CurrentHP = MaxHP;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();

        // Setup rigidbody
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Setup collider
        col.isTrigger = false;
        col.size = new Vector2(1.5f, 1.5f);

        // Setup visual
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (data.monsterSprite != null)
        {
            spriteRenderer.sprite = data.monsterSprite;
        }
        else
        {
            // Create placeholder sprite
            var tex = new Texture2D(64, 64);
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        }

        baseColor = data.monsterColor;
        spriteRenderer.color = baseColor;

        // Set layer to Ground so projectiles can hit us via trigger
        // Monster uses a separate trigger collider for projectile detection
        var triggerCol = gameObject.AddComponent<BoxCollider2D>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector2(1.5f, 1.5f);

        CurrentAIState = AIState.Patrol;
        attackTimer = data.GetScaledCooldown(totalLevelPool);
    }

    /// <summary>
    /// Take damage from a player projectile.
    /// </summary>
    public void TakeDamage(int damage, int attackerPlayerID)
    {
        if (CurrentHP <= 0) return;

        CurrentHP -= damage;
        lastAttackerID = attackerPlayerID;
        OnDamaged?.Invoke(CurrentHP, MaxHP);

        // Flash red on hit
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;
        Invoke(nameof(ResetColor), 0.1f);

        if (CurrentHP <= 0)
        {
            Die();
        }
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = baseColor;
    }

    private void Die()
    {
        OnMonsterKilled?.Invoke(lastAttackerID);

        // Screen shake on kill
        if (ScreenShake.Instance != null)
            ScreenShake.Instance.ShakeOnHit();

        Destroy(gameObject);
    }

    private void Update()
    {
        if (CurrentHP <= 0 || data == null) return;

        switch (CurrentAIState)
        {
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Detect:
                UpdateDetect();
                break;
            case AIState.Telegraph:
                UpdateTelegraph();
                break;
            case AIState.Cooldown:
                UpdateCooldown();
                break;
        }
    }

    // =========================================================
    // AI States
    // =========================================================

    private void UpdatePatrol()
    {
        // Simple patrol movement
        if (data.patrolSpeed > 0f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer > 3f)
            {
                patrolDirection *= -1f;
                patrolTimer = 0f;
            }

            rb.linearVelocity = new Vector2(patrolDirection * data.patrolSpeed, rb.linearVelocity.y);
        }

        // Check for players in detection range
        currentTarget = FindNearestPlayer();
        if (currentTarget != null)
        {
            CurrentAIState = AIState.Detect;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void UpdateDetect()
    {
        // Re-check target validity
        currentTarget = FindNearestPlayer();
        if (currentTarget == null)
        {
            CurrentAIState = AIState.Patrol;
            return;
        }

        // Optional pursuit
        if (data.pursuePlayers)
        {
            float dirToTarget = Mathf.Sign(currentTarget.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(dirToTarget * data.pursuitSpeed, rb.linearVelocity.y);
        }

        // Start telegraph before attacking
        telegraphTimer = 0f;
        CurrentAIState = AIState.Telegraph;
    }

    private void UpdateTelegraph()
    {
        telegraphTimer += Time.deltaTime;

        // Visual telegraph: pulse scale and flash color
        telegraphFlashTimer += Time.deltaTime * 8f;
        if (spriteRenderer != null)
        {
            float flash = Mathf.Sin(telegraphFlashTimer * Mathf.PI) * 0.5f + 0.5f;
            spriteRenderer.color = Color.Lerp(baseColor, Color.yellow, flash);
        }

        float scalePulse = 1f + Mathf.Sin(telegraphTimer * 6f) * 0.1f;
        transform.localScale = Vector3.one * scalePulse * 1.5f;

        if (telegraphTimer >= data.attackTelegraphDuration)
        {
            // Fire!
            FireProjectiles();
            transform.localScale = Vector3.one * 1.5f;
            if (spriteRenderer != null)
                spriteRenderer.color = baseColor;

            attackTimer = data.GetScaledCooldown(totalLevelPool);
            CurrentAIState = AIState.Cooldown;
        }
    }

    private void UpdateCooldown()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            CurrentAIState = AIState.Patrol;
        }
    }

    // =========================================================
    // Combat
    // =========================================================

    private void FireProjectiles()
    {
        if (data.projectilePrefab == null || currentTarget == null) return;

        Vector2 baseDir = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
        float scaledDamage = data.GetScaledDamage(totalLevelPool);

        // Negative OwnerPlayerID so projectiles hit all players
        int ownerID = -100 - MonsterID;

        if (data.projectilesPerAttack <= 1)
        {
            SpawnProjectile(baseDir, ownerID, scaledDamage);
        }
        else
        {
            // Spread pattern
            float halfSpread = data.spreadAngle / 2f;
            float step = data.spreadAngle / (data.projectilesPerAttack - 1);

            for (int i = 0; i < data.projectilesPerAttack; i++)
            {
                float angle = -halfSpread + step * i;
                Vector2 dir = RotateVector(baseDir, angle);
                SpawnProjectile(dir, ownerID, scaledDamage);
            }
        }
    }

    private void SpawnProjectile(Vector2 direction, int ownerID, float damage)
    {
        var projObj = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);
        var proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(
                ownerID, direction, data.projectileSpeed, damage,
                5f, 0.1f, data.projectileLifetime, 0.15f, 0f,
                false, 0, false, false
            );
        }
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // =========================================================
    // Targeting
    // =========================================================

    private Transform FindNearestPlayer()
    {
        Transform nearest = null;
        float closestDist = data.detectionRadius * data.detectionRadius;

        var players = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            var health = player.GetComponent<HealthSystem>();
            if (health == null || !health.IsAlive) continue;

            float dist = (player.transform.position - transform.position).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = player.transform;
            }
        }

        return nearest;
    }

    // =========================================================
    // Projectile Detection
    // =========================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        var projectile = other.GetComponent<Projectile>();
        if (projectile == null) return;

        // Only take damage from player projectiles (positive IDs)
        if (projectile.OwnerPlayerID < 0) return;

        float finalDamage = projectile.Damage * projectile.DamageMultiplier;
        TakeDamage(Mathf.RoundToInt(finalDamage), projectile.OwnerPlayerID);

        // Destroy the projectile (unless piercing)
        // Note: we don't destroy here — let Projectile handle its own lifecycle
    }
}
