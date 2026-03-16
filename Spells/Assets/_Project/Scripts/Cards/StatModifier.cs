using UnityEngine;

/// <summary>
/// A single stat modification that a power card applies.
/// Supports both additive and multiplicative modifiers.
/// Can target MovementData, CombatData, or both.
///
/// Design: modifiers are data — they describe a change, not a behavior.
/// ClassManager applies them by calling modifier.Apply(combatData).
/// </summary>
[System.Serializable]
public class StatModifier
{
    public enum ModType { Additive, Multiplicative }
    public enum Target
    {
        // Movement
        MoveSpeed,
        JumpForce,
        MaxAirJumps,

        // Health
        MaxHP,

        // Projectile
        ProjectileSpeed,
        ProjectileDamage,
        FireCooldown,
        ProjectileLifetime,
        ProjectileGravity,
        KnockbackForce,

        // Parry
        ParryWindow,
        ParryWhiffRecovery,
        ParryReflectSpeedMult,

        // Special
        MaxAmmo
    }

    public Target target;
    public ModType modType = ModType.Additive;
    public float value;

    /// <summary>
    /// Whether this modifier changes max HP (so HealthSystem needs updating).
    /// </summary>
    public bool AffectsHealth => target == Target.MaxHP;

    /// <summary>
    /// For HP modifications, the integer delta.
    /// </summary>
    public int MaxHPDelta => target == Target.MaxHP ? Mathf.RoundToInt(value) : 0;

    /// <summary>
    /// Whether this modifier changes movement values (so MovementData needs updating).
    /// </summary>
    public bool AffectsMovement => target == Target.MoveSpeed
                                || target == Target.JumpForce
                                || target == Target.MaxAirJumps;

    /// <summary>
    /// Apply this modifier to CombatData.
    /// </summary>
    public void Apply(CombatData data)
    {
        if (data == null) return;

        switch (target)
        {
            case Target.MaxHP:
                data.maxHP = ApplyInt(data.maxHP);
                break;
            case Target.ProjectileSpeed:
                data.projectileSpeed = ApplyFloat(data.projectileSpeed);
                break;
            case Target.ProjectileDamage:
                data.projectileDamage = ApplyFloat(data.projectileDamage);
                break;
            case Target.FireCooldown:
                data.fireCooldown = ApplyFloat(data.fireCooldown);
                break;
            case Target.ProjectileLifetime:
                data.projectileLifetime = ApplyFloat(data.projectileLifetime);
                break;
            case Target.ProjectileGravity:
                data.projectileGravity = ApplyFloat(data.projectileGravity);
                break;
            case Target.KnockbackForce:
                data.knockbackForce = ApplyFloat(data.knockbackForce);
                break;
            case Target.ParryWindow:
                data.parryWindow = ApplyFloat(data.parryWindow);
                break;
            case Target.ParryWhiffRecovery:
                data.parryWhiffRecovery = ApplyFloat(data.parryWhiffRecovery);
                break;
            case Target.ParryReflectSpeedMult:
                data.parryReflectSpeedMult = ApplyFloat(data.parryReflectSpeedMult);
                break;
            case Target.MaxAmmo:
                data.maxAmmo = ApplyInt(data.maxAmmo);
                break;
        }
    }

    /// <summary>
    /// Apply this modifier to MovementData.
    /// </summary>
    public void Apply(MovementData data)
    {
        if (data == null) return;

        switch (target)
        {
            case Target.MoveSpeed:
                data.moveSpeed = ApplyFloat(data.moveSpeed);
                break;
            case Target.JumpForce:
                data.jumpForce = ApplyFloat(data.jumpForce);
                break;
            case Target.MaxAirJumps:
                data.maxAirJumps = ApplyInt(data.maxAirJumps);
                break;
        }
    }

    private float ApplyFloat(float current)
    {
        return modType == ModType.Additive ? current + value : current * value;
    }

    private int ApplyInt(int current)
    {
        return modType == ModType.Additive
            ? current + Mathf.RoundToInt(value)
            : Mathf.RoundToInt(current * value);
    }
}
