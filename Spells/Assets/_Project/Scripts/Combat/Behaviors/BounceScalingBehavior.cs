using UnityEngine;

/// <summary>
/// Attached to a projectile to scale its damage based on how many times it has bounced.
/// Each bounce multiplies damage by <see cref="damagePerBounce"/>.
/// Used by Ricochet Hell to reward wall-banking shots.
/// </summary>
public class BounceScalingBehavior : MonoBehaviour
{
    private float damagePerBounce; // e.g. 1.2 = +20% per bounce
    private Projectile proj;

    public void Initialize(float damageMultiplierPerBounce)
    {
        damagePerBounce = damageMultiplierPerBounce;
        proj = GetComponent<Projectile>();
        if (proj != null)
            proj.OnBeforeHit += OnBeforeHit;
    }

    private void OnDestroy()
    {
        if (proj != null)
            proj.OnBeforeHit -= OnBeforeHit;
    }

    private void OnBeforeHit(UnityEngine.GameObject target)
    {
        if (proj == null) return;
        int bounces = proj.CurrentBounceCount;
        if (bounces > 0)
            proj.DamageMultiplier *= Mathf.Pow(damagePerBounce, bounces);
    }
}
