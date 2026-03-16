using UnityEngine;

/// <summary>
/// Area-of-effect zone created by Alchemist potions on impact.
/// Deals damage over time to players standing in the zone.
/// Modified by StickyBrew (longer + smaller) and VolatileMix (explodes on contact).
/// </summary>
public class PotionZone : MonoBehaviour
{
    public float Duration { get; set; } = 3f;
    public float Radius { get; set; } = 1.5f;
    public float DamagePerTick { get; set; } = 1f;
    public float TickInterval { get; set; } = 1f;
    public int OwnerPlayerID { get; set; }
    public bool CanHitOwner { get; set; }
    public bool IsVolatile { get; set; }

    private float elapsed;
    private float tickTimer;
    private CircleCollider2D zoneCollider;
    private bool hasExploded;

    private void Start()
    {
        // Create trigger zone
        zoneCollider = gameObject.AddComponent<CircleCollider2D>();
        zoneCollider.radius = Radius;
        zoneCollider.isTrigger = true;

        // Add a rigidbody so triggers work (kinematic, no gravity)
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Update()
    {
        if (IsVolatile) return; // Volatile zones don't tick — they explode on contact

        elapsed += Time.deltaTime;
        if (elapsed >= Duration)
        {
            Destroy(gameObject);
            return;
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= TickInterval)
        {
            tickTimer -= TickInterval;
            DamagePlayersInZone();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsVolatile || hasExploded) return;

        // Check if a player entered
        var identity = other.GetComponent<PlayerIdentity>();
        if (identity == null) return;
        if (identity.PlayerID == OwnerPlayerID && !CanHitOwner) return;

        // EXPLODE!
        Explode();
    }

    private void Explode()
    {
        hasExploded = true;
        DamagePlayersInZone();
        Destroy(gameObject); // One-shot
    }

    private void DamagePlayersInZone()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position, Radius);
        foreach (var col in colliders)
        {
            var identity = col.GetComponent<PlayerIdentity>();
            if (identity == null) continue;
            if (identity.PlayerID == OwnerPlayerID && !CanHitOwner) continue;

            var health = col.GetComponent<HealthSystem>();
            if (health != null && health.IsAlive)
            {
                health.TakeDamage(DamagePerTick, OwnerPlayerID);
            }
        }
    }
}
