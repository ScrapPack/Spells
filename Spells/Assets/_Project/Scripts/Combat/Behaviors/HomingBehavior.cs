using UnityEngine;

/// <summary>
/// Added to a projectile to make it gently curve toward the nearest enemy.
/// Doesn't snap — applies angular velocity toward the target for a
/// satisfying curving trajectory. Target must be alive and not the owner.
/// </summary>
public class HomingBehavior : MonoBehaviour
{
    private float turnRate;      // degrees/sec
    private float detectionRadius;
    private int ownerID;
    private Rigidbody2D rb;
    private Transform target;
    private float retargetTimer;

    public void Initialize(float strength, float radius, int ownerPlayerID)
    {
        turnRate = strength;
        detectionRadius = radius;
        ownerID = ownerPlayerID;
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (rb == null || rb.linearVelocity.sqrMagnitude < 0.1f) return;

        // Retarget periodically (not every frame for perf)
        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0f || target == null)
        {
            FindTarget();
            retargetTimer = 0.2f;
        }

        if (target == null) return;

        // Calculate angle to target
        Vector2 toTarget = (target.position - transform.position).normalized;
        Vector2 currentDir = rb.linearVelocity.normalized;

        float angleDiff = Vector2.SignedAngle(currentDir, toTarget);

        // Clamp turn rate
        float maxTurn = turnRate * Time.deltaTime;
        float actualTurn = Mathf.Clamp(angleDiff, -maxTurn, maxTurn);

        // Apply rotation to velocity
        float speed = rb.linearVelocity.magnitude;
        float currentAngle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
        float newAngle = (currentAngle + actualTurn) * Mathf.Deg2Rad;

        rb.linearVelocity = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)) * speed;

        // Update visual rotation
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle + actualTurn);
    }

    private void FindTarget()
    {
        target = null;
        float closestDist = detectionRadius * detectionRadius;

        foreach (var player in PlayerIdentity.All)
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
