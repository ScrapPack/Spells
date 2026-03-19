using UnityEngine;

/// <summary>
/// Added to a projectile to redirect it toward the nearest enemy after
/// bouncing off a wall. Works with the existing bounce system in Projectile —
/// after Projectile.HandleEnvironmentHit does the bounce, this component
/// adjusts the new velocity to aim toward a nearby enemy.
///
/// Only redirects within a configurable angle cone — feels like aim assist
/// rather than auto-targeting.
/// </summary>
public class RicochetBehavior : MonoBehaviour
{
    private float maxRedirectAngle;
    private int ownerID;
    private Rigidbody2D rb;
    private Vector2 lastVelocity;

    public void Initialize(float aimAssistAngle, int ownerPlayerID)
    {
        maxRedirectAngle = aimAssistAngle;
        ownerID = ownerPlayerID;
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Detect velocity direction change (bounce happened)
        Vector2 currentVel = rb.linearVelocity;
        if (currentVel.sqrMagnitude < 0.1f)
        {
            lastVelocity = currentVel;
            return;
        }

        // Check if direction changed significantly (bounce)
        if (lastVelocity.sqrMagnitude > 0.1f)
        {
            float angleDiff = Vector2.Angle(lastVelocity.normalized, currentVel.normalized);
            if (angleDiff > 30f) // Bounce threshold
            {
                TryRedirect();
            }
        }

        lastVelocity = currentVel;
    }

    private void TryRedirect()
    {
        // Find nearest enemy
        Transform bestTarget = null;
        float bestAngle = maxRedirectAngle;
        float bestDist = 15f; // Max search distance

        foreach (var player in PlayerIdentity.All)
        {
            if (player.PlayerID == ownerID) continue;

            var health = player.GetComponent<HealthSystem>();
            if (health == null || !health.IsAlive) continue;

            Vector2 toTarget = (player.transform.position - transform.position);
            float dist = toTarget.magnitude;
            if (dist > bestDist) continue;

            float angle = Vector2.Angle(rb.linearVelocity.normalized, toTarget.normalized);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestDist = dist;
                bestTarget = player.transform;
            }
        }

        if (bestTarget != null)
        {
            // Redirect velocity toward target
            Vector2 toTarget = (bestTarget.position - transform.position).normalized;
            float speed = rb.linearVelocity.magnitude;
            rb.linearVelocity = toTarget * speed;

            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
