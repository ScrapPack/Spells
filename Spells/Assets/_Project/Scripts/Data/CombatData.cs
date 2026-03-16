using UnityEngine;

[CreateAssetMenu(fileName = "CombatData", menuName = "Spells/Combat Data")]
public class CombatData : ScriptableObject
{
    [Header("Health")]
    [Tooltip("Maximum hit points. Class-dependent: Rogue/Shaman/Jester = 2, most = 3, Warrior = 4")]
    [Range(1, 10)] public int maxHP = 3;

    [Header("Projectile — Base")]
    [Tooltip("Projectile travel speed in units/sec")]
    [Range(1f, 50f)] public float projectileSpeed = 20f;
    [Tooltip("Seconds between shots (lower = faster fire rate)")]
    [Range(0.05f, 2f)] public float fireCooldown = 0.3f;
    [Tooltip("Damage dealt per projectile hit")]
    [Range(0.5f, 5f)] public float projectileDamage = 1f;
    [Tooltip("Projectile lifetime in seconds before auto-despawn")]
    [Range(0.5f, 10f)] public float projectileLifetime = 3f;
    [Tooltip("Projectile hitbox radius")]
    [Range(0.05f, 1f)] public float projectileRadius = 0.15f;

    [Header("Projectile — Physics")]
    [Tooltip("Gravity scale applied to projectile (0 = straight, >0 = arcing lob)")]
    [Range(0f, 5f)] public float projectileGravity = 0f;
    [Tooltip("Whether projectile bounces off walls")]
    public bool projectileBounces = false;
    [Tooltip("Max bounces before despawn (if bouncing enabled)")]
    [Range(1, 10)] public int maxBounces = 3;
    [Tooltip("Whether projectile pierces through targets")]
    public bool projectilePierces = false;

    [Header("Knockback")]
    [Tooltip("Knockback force applied on hit (direction: away from projectile velocity)")]
    [Range(0f, 30f)] public float knockbackForce = 8f;
    [Tooltip("Duration of hitstun in seconds")]
    [Range(0f, 1f)] public float hitstunDuration = 0.15f;

    [Header("Parry")]
    [Tooltip("Parry active window in seconds. 6-8 frames at 60fps = 100-133ms")]
    [Range(0.05f, 0.5f)] public float parryWindow = 0.117f;
    [Tooltip("Recovery frames after a missed parry (vulnerability window)")]
    [Range(0f, 1f)] public float parryWhiffRecovery = 0.3f;
    [Tooltip("Speed multiplier for reflected projectile")]
    [Range(0.5f, 3f)] public float parryReflectSpeedMult = 1.2f;

    [Header("Invincibility")]
    [Tooltip("Duration of i-frames after being hit")]
    [Range(0f, 2f)] public float invincibilityDuration = 0.5f;

    [Header("Warrior — Axe Retrieval")]
    [Tooltip("Max axes the warrior can carry (0 = not applicable)")]
    [Range(0, 10)] public int maxAmmo = 0;
    [Tooltip("Whether projectiles can be picked up after landing")]
    public bool retrievableProjectiles = false;

    public CombatData Clone()
    {
        return Instantiate(this);
    }
}
