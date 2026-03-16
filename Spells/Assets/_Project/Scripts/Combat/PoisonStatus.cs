using UnityEngine;

/// <summary>
/// Damage-over-time poison applied to a player by VenomDartEffect.
/// Ticks 1 damage per second over the duration. Poison bypasses
/// invincibility (ongoing damage, not a new hit).
/// </summary>
public class PoisonStatus : MonoBehaviour
{
    private int totalDamage;
    private float duration;
    private float elapsed;
    private float tickInterval = 1f;
    private float tickTimer;
    private int damageDealt;
    private HealthSystem health;

    public void Initialize(int damage, float dur)
    {
        totalDamage = damage;
        duration = dur;
        elapsed = 0f;
        tickTimer = 0f;
        damageDealt = 0;
        health = GetComponent<HealthSystem>();
    }

    /// <summary>
    /// Stack additional poison (refreshes duration, adds damage).
    /// </summary>
    public void AddPoison(int additionalDamage, float newDuration)
    {
        totalDamage += additionalDamage;
        duration = Mathf.Max(duration - elapsed, newDuration);
        elapsed = 0f;
    }

    private void Update()
    {
        if (health == null || !health.IsAlive)
        {
            Destroy(this);
            return;
        }

        elapsed += Time.deltaTime;
        tickTimer += Time.deltaTime;

        if (tickTimer >= tickInterval && damageDealt < totalDamage)
        {
            tickTimer -= tickInterval;
            health.TakeSelfDamage(1); // Bypasses invincibility
            damageDealt++;
        }

        if (elapsed >= duration || damageDealt >= totalDamage)
        {
            Destroy(this);
        }
    }
}
