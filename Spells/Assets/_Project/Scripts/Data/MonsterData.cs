using UnityEngine;

/// <summary>
/// Defines a monster type for PvE encounters.
/// Monsters are sparse miniboss targets that scale with total player level pool.
/// They provide an alternative progression path (catch-up mechanic) —
/// killing a monster grants a level without needing to win a round.
///
/// Scaling: HP and damage increase with the sum of all player levels.
/// This keeps monsters relevant as the match progresses.
/// </summary>
[CreateAssetMenu(fileName = "MonsterData", menuName = "Spells/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("Identity")]
    public string monsterName = "Stone Golem";
    [TextArea(2, 4)]
    public string description = "A slow-moving golem that fires spread projectiles.";

    [Header("Base Stats")]
    [Tooltip("Hit points at level pool 0. Scales with GetScaledHP().")]
    [Range(1, 20)] public int baseHP = 3;
    [Tooltip("Base damage per projectile hit")]
    [Range(0.5f, 5f)] public float baseDamage = 1f;
    [Tooltip("Seconds between attacks")]
    [Range(0.5f, 5f)] public float attackCooldown = 2f;
    [Tooltip("Detection radius for targeting nearest player")]
    [Range(5f, 30f)] public float detectionRadius = 12f;

    [Header("Scaling")]
    [Tooltip("Additional HP per total level pool point")]
    [Range(0f, 2f)] public float hpPerLevelPool = 0.5f;
    [Tooltip("Additional damage per total level pool point")]
    [Range(0f, 0.5f)] public float damagePerLevelPool = 0.1f;
    [Tooltip("Attack cooldown reduction per level pool (capped at minAttackCooldown)")]
    [Range(0f, 0.1f)] public float cooldownReductionPerPool = 0.03f;
    [Tooltip("Minimum attack cooldown floor")]
    [Range(0.3f, 2f)] public float minAttackCooldown = 0.8f;

    [Header("Attack Pattern")]
    [Tooltip("Number of projectiles per attack (spread pattern)")]
    [Range(1, 8)] public int projectilesPerAttack = 3;
    [Tooltip("Spread angle in degrees for multi-projectile attacks")]
    [Range(0f, 90f)] public float spreadAngle = 30f;
    [Tooltip("Projectile speed")]
    [Range(5f, 30f)] public float projectileSpeed = 12f;
    [Tooltip("Projectile lifetime in seconds")]
    [Range(1f, 10f)] public float projectileLifetime = 3f;

    [Header("Telegraph")]
    [Tooltip("Duration of attack telegraph animation before firing")]
    [Range(0.2f, 2f)] public float attackTelegraphDuration = 0.8f;

    [Header("Movement")]
    [Tooltip("Patrol speed when no player detected")]
    [Range(0f, 5f)] public float patrolSpeed = 1.5f;
    [Tooltip("Whether monster moves toward detected players")]
    public bool pursuePlayers = false;
    [Tooltip("Pursuit speed when chasing a player")]
    [Range(0f, 8f)] public float pursuitSpeed = 3f;

    [Header("Visuals")]
    public Color monsterColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Sprite monsterSprite;

    [Header("Prefab")]
    [Tooltip("Projectile prefab this monster fires")]
    public GameObject projectilePrefab;

    /// <summary>
    /// Get scaled HP based on total level pool.
    /// </summary>
    public int GetScaledHP(int totalLevelPool)
    {
        return Mathf.Max(1, baseHP + Mathf.RoundToInt(hpPerLevelPool * totalLevelPool));
    }

    /// <summary>
    /// Get scaled damage based on total level pool.
    /// </summary>
    public float GetScaledDamage(int totalLevelPool)
    {
        return baseDamage + damagePerLevelPool * totalLevelPool;
    }

    /// <summary>
    /// Get scaled attack cooldown based on total level pool.
    /// </summary>
    public float GetScaledCooldown(int totalLevelPool)
    {
        float reduced = attackCooldown - cooldownReductionPerPool * totalLevelPool;
        return Mathf.Max(minAttackCooldown, reduced);
    }
}
