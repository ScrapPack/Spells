using UnityEngine;

/// <summary>
/// Added to Rogue projectiles by AmbushEffect.
/// Uses the OnBeforeHit callback to check target facing at impact time.
/// If target is facing away: ambush multiplier (2x+). Otherwise: penalty (0.75x).
/// </summary>
public class AmbushProjectileBehavior : MonoBehaviour
{
    private float ambushMult;
    private float normalMult;
    private Projectile projectile;
    private Rigidbody2D rb;

    public void Initialize(float ambushMultiplier, float normalMultiplier)
    {
        ambushMult = ambushMultiplier;
        normalMult = normalMultiplier;
        projectile = GetComponent<Projectile>();
        rb = GetComponent<Rigidbody2D>();

        if (projectile != null)
        {
            // Default to penalty; upgraded to ambush in OnBeforeHit if facing away
            projectile.DamageMultiplier = normalMult;
            projectile.OnBeforeHit += CheckAmbush;
        }
    }

    private void CheckAmbush(GameObject target)
    {
        if (projectile == null || rb == null || target == null) return;

        var targetInput = target.GetComponent<IInputProvider>();
        if (targetInput == null)
        {
            // Can't determine facing — use normal multiplier
            projectile.DamageMultiplier = normalMult;
            return;
        }

        float projDirX = Mathf.Sign(rb.linearVelocity.x);
        float targetFacing = targetInput.MoveInput.x;

        // Target is facing away if they're moving in the same direction
        // as the projectile (projectile hitting their back)
        bool facingAway = Mathf.Abs(targetFacing) > 0.1f &&
                         Mathf.Sign(targetFacing) == Mathf.Sign(projDirX);

        projectile.DamageMultiplier = facingAway ? ambushMult : normalMult;
    }

    private void OnDestroy()
    {
        if (projectile != null)
            projectile.OnBeforeHit -= CheckAmbush;
    }
}
