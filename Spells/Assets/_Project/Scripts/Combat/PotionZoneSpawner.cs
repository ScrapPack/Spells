using UnityEngine;

/// <summary>
/// Added to Alchemist projectiles by StickyBrewEffect and VolatileMixEffect.
/// When the projectile hits ground/wall, creates a PotionZone at impact location.
/// Both this component and Projectile.HandleEnvironmentHit receive the trigger;
/// Unity guarantees both fire before Destroy takes effect.
/// </summary>
public class PotionZoneSpawner : MonoBehaviour
{
    private int ownerID;
    private float zoneDuration = 3f;
    private float zoneRadius = 1.5f;
    private bool canHitOwner;
    private bool isVolatile;
    private bool zoneCreated;

    private int groundLayer;
    private int wallLayer;

    public void Initialize(int ownerId, float duration, float radius, bool hitOwner, bool volatile_)
    {
        ownerID = ownerId;
        zoneDuration = duration;
        zoneRadius = radius;
        canHitOwner = hitOwner;
        isVolatile = volatile_;
    }

    private void Awake()
    {
        groundLayer = LayerMask.NameToLayer("Ground");
        wallLayer = LayerMask.NameToLayer("Wall");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (zoneCreated) return;

        if (other.gameObject.layer == groundLayer || other.gameObject.layer == wallLayer)
        {
            CreateZone();
        }
    }

    private void CreateZone()
    {
        zoneCreated = true;

        var zoneObj = new GameObject("PotionZone");
        zoneObj.transform.position = transform.position;

        var zone = zoneObj.AddComponent<PotionZone>();
        zone.Duration = zoneDuration;
        zone.Radius = zoneRadius;
        zone.OwnerPlayerID = ownerID;
        zone.CanHitOwner = canHitOwner;
        zone.IsVolatile = isVolatile;
    }
}
