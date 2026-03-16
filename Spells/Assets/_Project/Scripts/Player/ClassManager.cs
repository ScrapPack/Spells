using UnityEngine;

/// <summary>
/// Applies a ClassData configuration to a player on spawn.
/// Initializes HealthSystem, ProjectileSpawner, ParrySystem, and PlayerIdentity
/// with the correct class-specific values.
///
/// This is the single point where class identity is applied — all combat
/// components are generic and configured through data, not inheritance.
/// </summary>
public class ClassManager : MonoBehaviour
{
    [SerializeField] private ClassData classData;

    public ClassData CurrentClass => classData;
    public CombatData CombatData { get; private set; }

    private HealthSystem health;
    private ProjectileSpawner spawner;
    private ParrySystem parry;

    /// <summary>
    /// Initialize this player as a specific class. Can be called at spawn
    /// or when switching classes in character select.
    /// </summary>
    public void Initialize(ClassData data, int playerID)
    {
        classData = data;

        // Clone combat data so power cards can modify per-player without affecting asset
        CombatData = data.combatData != null ? data.combatData.Clone() : null;

        if (CombatData == null)
        {
            Debug.LogError($"ClassManager: {data.className} has no CombatData assigned!", this);
            return;
        }

        // Initialize identity
        var identity = GetComponent<PlayerIdentity>();
        if (identity != null)
            identity.Initialize(playerID);

        // Initialize health
        health = GetComponent<HealthSystem>();
        if (health != null)
            health.Initialize(CombatData.maxHP, CombatData.invincibilityDuration);

        // Initialize projectile spawner
        spawner = GetComponent<ProjectileSpawner>();
        if (spawner != null)
            spawner.Initialize(CombatData, data.projectilePrefab);

        // Initialize parry
        parry = GetComponent<ParrySystem>();
        if (parry != null)
            parry.Initialize(CombatData);
    }

    /// <summary>
    /// Reset all combat systems for a new round.
    /// HP refills, ammo resets, parry state clears.
    /// </summary>
    public void ResetForRound()
    {
        if (health != null) health.ResetForRound();
        if (spawner != null) spawner.ResetForRound();
        if (parry != null) parry.ResetForRound();
    }

    /// <summary>
    /// Apply a stat modifier from a power card.
    /// This modifies the cloned CombatData directly.
    /// </summary>
    public void ApplyStatModifier(StatModifier modifier)
    {
        if (CombatData == null) return;
        modifier.Apply(CombatData);

        // Re-initialize systems that cache values from CombatData
        if (health != null && modifier.AffectsHealth)
            health.ModifyMaxHP(modifier.MaxHPDelta);
    }
}
