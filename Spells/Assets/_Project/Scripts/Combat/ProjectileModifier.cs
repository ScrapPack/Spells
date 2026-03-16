using UnityEngine;

/// <summary>
/// Modifies projectile behavior post-spawn. Applied by SpellEffects or
/// class abilities. Multiple modifiers can stack on a single player's
/// ProjectileSpawner to create compound behaviors.
///
/// Modifiers are applied to each projectile after it's spawned via
/// OnProjectileSpawned callback from ProjectileSpawner.
///
/// Types:
/// - Split: projectile spawns N copies at angles on hit/timer
/// - Homing: projectile gently curves toward nearest enemy
/// - Explosive: projectile deals AoE damage on impact
/// - Ricochet: projectile bounces toward nearest enemy after wall hit
/// </summary>
[System.Serializable]
public class ProjectileModifier
{
    public enum ModifierType { Split, Homing, Explosive, Ricochet }

    public ModifierType type;

    [Header("Split")]
    [Tooltip("Number of projectiles to split into")]
    public int splitCount = 3;
    [Tooltip("Angle spread (degrees) for split projectiles")]
    public float splitSpreadAngle = 30f;
    [Tooltip("Damage multiplier for split fragments (prevent infinite damage)")]
    public float splitDamageMultiplier = 0.5f;

    [Header("Homing")]
    [Tooltip("How strongly the projectile curves toward targets (degrees/sec)")]
    public float homingStrength = 90f;
    [Tooltip("Detection radius for homing target acquisition")]
    public float homingRadius = 5f;

    [Header("Explosive")]
    [Tooltip("Explosion radius")]
    public float explosionRadius = 2f;
    [Tooltip("Explosion damage multiplier (applied to base damage)")]
    public float explosionDamageMultiplier = 0.75f;
    [Tooltip("Explosion knockback force")]
    public float explosionKnockback = 8f;

    [Header("Ricochet")]
    [Tooltip("Max angle to redirect ricochet toward a target")]
    public float ricochetAimAssist = 45f;
}
