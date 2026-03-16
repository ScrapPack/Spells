using UnityEngine;

/// <summary>
/// Environmental hazard that damages players on contact.
/// Used for: fire on ground (from destroyed torches), lava flows,
/// spike traps, swinging chains.
///
/// Hazards exist independently of player action and persist for
/// a configurable duration (or permanently for static hazards).
/// </summary>
public class EnvironmentHazard : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Damage dealt on contact")]
    [SerializeField] private float damage = 1f;
    [Tooltip("Seconds between repeated damage ticks while standing in hazard")]
    [SerializeField] private float damageCooldown = 1f;
    [Tooltip("Knockback force on contact (0 = no knockback)")]
    [SerializeField] private float knockbackForce = 5f;
    [Tooltip("Direction of knockback (Up = launch upward, Zero = away from center)")]
    [SerializeField] private Vector2 knockbackDirection = Vector2.up;

    [Header("Attribution")]
    [Tooltip("Player ID who created this hazard (-1 = environment). Used for kill credit.")]
    [SerializeField] private int sourcePlayerID = -1;

    [Header("Lifetime")]
    [Tooltip("Duration in seconds. 0 = permanent.")]
    [SerializeField] private float lifetime = 0f;
    [Tooltip("Fade out over the last N seconds of lifetime")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    private float lifetimeTimer;
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Set the player who created this hazard (for kill credit).
    /// Called when a DestructibleObject spawns a hazard from a player's attack.
    /// </summary>
    public void SetSourcePlayer(int playerID) => sourcePlayerID = playerID;

    // Track damage cooldowns per player
    private readonly System.Collections.Generic.Dictionary<int, float> cooldowns =
        new System.Collections.Generic.Dictionary<int, float>();

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        lifetimeTimer = 0f;
    }

    private void Update()
    {
        // Tick lifetime
        if (lifetime > 0f)
        {
            lifetimeTimer += Time.deltaTime;

            // Fade out
            if (spriteRenderer != null && lifetimeTimer > lifetime - fadeOutDuration)
            {
                float fadeProgress = (lifetimeTimer - (lifetime - fadeOutDuration)) / fadeOutDuration;
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                spriteRenderer.color = c;
            }

            if (lifetimeTimer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }
        }

        // Tick damage cooldowns
        var keys = new System.Collections.Generic.List<int>(cooldowns.Keys);
        foreach (int key in keys)
        {
            if (cooldowns[key] > 0f)
                cooldowns[key] -= Time.deltaTime;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        var identity = other.GetComponent<PlayerIdentity>();
        var health = other.GetComponent<HealthSystem>();

        if (identity == null || health == null) return;

        int playerID = identity.PlayerID;

        // Check cooldown
        if (cooldowns.ContainsKey(playerID) && cooldowns[playerID] > 0f)
            return;

        // Apply damage (with source player for kill credit if applicable)
        bool didDamage = health.TakeDamage(damage, sourcePlayerID);

        if (didDamage)
        {
            // Apply knockback
            if (knockbackForce > 0f)
            {
                var rb = other.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 dir = knockbackDirection.sqrMagnitude > 0.01f
                        ? knockbackDirection.normalized
                        : (other.transform.position - transform.position).normalized;
                    rb.linearVelocity = dir * knockbackForce;
                }
            }

            // Set cooldown
            cooldowns[playerID] = damageCooldown;
        }
    }
}
