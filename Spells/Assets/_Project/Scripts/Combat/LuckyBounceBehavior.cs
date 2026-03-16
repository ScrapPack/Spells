using UnityEngine;

/// <summary>
/// Added to Jester projectiles by LuckyBounceEffect.
/// Direct hits (0 bounces) deal 0 damage. Each bounce adds damage.
/// Continuously monitors bounce count and updates DamageMultiplier.
/// </summary>
public class LuckyBounceBehavior : MonoBehaviour
{
    private Projectile projectile;
    private int damagePerBounce;
    private int lastBounceCount;

    public void Initialize(int stackCount)
    {
        projectile = GetComponent<Projectile>();
        damagePerBounce = stackCount;
        lastBounceCount = 0;

        if (projectile != null)
        {
            // Direct hit (0 bounces) = 0 damage
            projectile.DamageMultiplier = 0f;
        }
    }

    private void Update()
    {
        if (projectile == null) return;

        int currentBounces = projectile.CurrentBounceCount;
        if (currentBounces != lastBounceCount)
        {
            lastBounceCount = currentBounces;
            // Each bounce sets multiplier: bounce 1 = 1x, bounce 2 = 2x, etc.
            projectile.DamageMultiplier = currentBounces * damagePerBounce;
        }
    }
}
