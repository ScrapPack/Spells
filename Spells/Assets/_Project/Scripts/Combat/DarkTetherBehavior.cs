using UnityEngine;

/// <summary>
/// Added to Warlock projectiles by DarkTetherEffect.
/// Phase 1 (outgoing): gently homes toward nearest enemy.
/// Phase 2 (return): after lifetime, reverses and homes toward owner.
/// Returning orbs can hit the owner (the card's downside).
/// </summary>
public class DarkTetherBehavior : MonoBehaviour
{
    private Transform ownerTransform;
    private int ownerID;
    private float homingStrength;
    private float returnHomingStrength;
    private float lifetime;
    private float timer;
    private Rigidbody2D rb;
    private Projectile projectile;
    private bool isReturning;
    private float retargetTimer;
    private Transform target;

    public void Initialize(Transform owner, int ownerId, float strength, float returnStrength, float projLifetime)
    {
        ownerTransform = owner;
        ownerID = ownerId;
        homingStrength = strength;
        returnHomingStrength = returnStrength;
        lifetime = projLifetime;
        rb = GetComponent<Rigidbody2D>();
        projectile = GetComponent<Projectile>();

        if (projectile != null)
            projectile.PreventAutoExpire = true;
    }

    private void Update()
    {
        if (rb == null || ownerTransform == null) return;

        timer += Time.deltaTime;

        if (!isReturning && timer >= lifetime)
        {
            isReturning = true;
            if (projectile != null)
                projectile.CanHitOwner = true; // Downside: returning orbs can hit you
        }

        if (isReturning)
        {
            HomeToward(ownerTransform, returnHomingStrength);

            // Check if reached owner or timed out
            float dist = Vector2.Distance(transform.position, ownerTransform.position);
            if (dist < 0.5f || timer > lifetime * 3f)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            // Home toward nearest enemy
            retargetTimer -= Time.deltaTime;
            if (retargetTimer <= 0f || target == null)
            {
                FindTarget();
                retargetTimer = 0.2f;
            }
            if (target != null)
                HomeToward(target, homingStrength);
        }
    }

    private void HomeToward(Transform t, float strength)
    {
        if (t == null || rb.linearVelocity.sqrMagnitude < 0.1f) return;

        Vector2 toTarget = ((Vector2)t.position - (Vector2)transform.position).normalized;
        Vector2 currentDir = rb.linearVelocity.normalized;
        float angleDiff = Vector2.SignedAngle(currentDir, toTarget);
        float maxTurn = strength * Time.deltaTime;
        float actualTurn = Mathf.Clamp(angleDiff, -maxTurn, maxTurn);

        float speed = rb.linearVelocity.magnitude;
        float currentAngle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
        float newAngle = (currentAngle + actualTurn) * Mathf.Deg2Rad;

        rb.linearVelocity = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)) * speed;
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle + actualTurn);
    }

    private void FindTarget()
    {
        target = null;
        float closestDist = 100f; // 10 unit detection radius

        var players = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.PlayerID == ownerID) continue;

            var health = player.GetComponent<HealthSystem>();
            if (health == null || !health.IsAlive) continue;

            float dist = (player.transform.position - transform.position).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                target = player.transform;
            }
        }
    }
}
