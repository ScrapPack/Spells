using UnityEngine;

/// <summary>
/// Auto-firing turret entity spawned by AncestralTotemEffect.
/// Shoots at the nearest player within detection radius.
/// The downside: also shoots at the owner if they're closest.
/// Persists for the duration of the round.
/// </summary>
public class TotemEntity : MonoBehaviour
{
    public int OwnerPlayerID { get; private set; }
    public float FireRate { get; set; } = 1f;
    public float DetectionRadius { get; set; } = 8f;
    public float ProjectileSpeed { get; set; } = 12f;
    public float Damage { get; set; } = 1f;
    public bool CanTargetOwner { get; set; } = true;

    private float fireCooldown;
    private GameObject projectilePrefab;

    public void Initialize(int ownerID, GameObject prefab, bool targetsOwner)
    {
        OwnerPlayerID = ownerID;
        projectilePrefab = prefab;
        CanTargetOwner = targetsOwner;
    }

    private void Update()
    {
        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f)
        {
            if (TryFire())
                fireCooldown = 1f / FireRate;
            else
                fireCooldown = 0.2f; // Retry faster when no target
        }
    }

    private bool TryFire()
    {
        Transform target = FindNearestTarget();
        if (target == null || projectilePrefab == null) return false;

        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;

        var projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(
                OwnerPlayerID, dir, ProjectileSpeed, Damage,
                5f, 0.1f, 3f, 0.1f, 0f,
                false, 0, false, false
            );
        }

        return true;
    }

    private Transform FindNearestTarget()
    {
        Transform nearest = null;
        float closestDist = DetectionRadius * DetectionRadius;

        var players = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.PlayerID == OwnerPlayerID && !CanTargetOwner) continue;

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
}
