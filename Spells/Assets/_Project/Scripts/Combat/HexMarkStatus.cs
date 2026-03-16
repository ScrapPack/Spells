using UnityEngine;

/// <summary>
/// Damage amplification curse applied by HexMarkEffect.
/// While active, any damage taken triggers additional self-damage.
/// Subscribes to HealthSystem.OnDamaged with recursion guard.
/// </summary>
public class HexMarkStatus : MonoBehaviour
{
    private int extraDamage;
    private HealthSystem health;
    private bool isApplyingBonusDamage;

    public void Initialize(int bonusDamage)
    {
        extraDamage = bonusDamage;
        health = GetComponent<HealthSystem>();

        if (health != null)
            health.OnDamaged.AddListener(OnTookDamage);
    }

    private void OnTookDamage(float amount)
    {
        // Guard against infinite recursion (TakeSelfDamage fires OnDamaged)
        if (isApplyingBonusDamage) return;
        if (health == null || !health.IsAlive) return;

        isApplyingBonusDamage = true;
        health.TakeSelfDamage(extraDamage);
        isApplyingBonusDamage = false;
    }

    /// <summary>
    /// Remove the hex mark. Called by HexMarkEffect when switching targets.
    /// </summary>
    public void Remove()
    {
        if (health != null)
            health.OnDamaged.RemoveListener(OnTookDamage);

        Destroy(this);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDamaged.RemoveListener(OnTookDamage);
    }
}
