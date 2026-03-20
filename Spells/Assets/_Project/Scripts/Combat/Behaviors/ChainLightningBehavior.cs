using UnityEngine;

/// <summary>
/// Added to a projectile to chain a lightning strike to the nearest other player
/// when the projectile hits someone. The chain deals a percentage of the direct hit
/// damage as instant, undodgeable damage. Chain range is limited so positioning matters.
/// </summary>
public class ChainLightningBehavior : MonoBehaviour
{
    private float chainDamageMultiplier;
    private float chainRange;
    private int ownerID;
    private Projectile proj;

    public void Initialize(float damageMultiplier, float range, int ownerPlayerID)
    {
        chainDamageMultiplier = damageMultiplier;
        chainRange            = range;
        ownerID               = ownerPlayerID;

        proj = GetComponent<Projectile>();
        if (proj != null)
            proj.OnHitPlayer += OnHitPlayer;
    }

    private void OnDestroy()
    {
        if (proj != null)
            proj.OnHitPlayer -= OnHitPlayer;
    }

    private void OnHitPlayer(GameObject primaryTarget, float damage)
    {
        // Find the nearest alive player who isn't the primary target or the owner
        float closestDistSq = chainRange * chainRange;
        HealthSystem chainTarget = null;

        foreach (var player in PlayerIdentity.All)
        {
            if (player.gameObject == primaryTarget) continue;
            if (player.PlayerID == ownerID) continue;

            var health = player.GetComponent<HealthSystem>();
            if (health == null || !health.IsAlive) continue;

            float distSq = (player.transform.position - primaryTarget.transform.position).sqrMagnitude;
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                chainTarget   = health;
            }
        }

        if (chainTarget == null) return;

        float chainDamage = damage * chainDamageMultiplier;
        chainTarget.TakeDamage(chainDamage, ownerID);

        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(0.08f, 0.06f);
    }
}
