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
    private ClassAbility ability;

    private void Start()
    {
        // Auto-initialize if BoxArenaBuilder did not call Initialize() before Start()
        if (CombatData == null && classData != null)
        {
            var identity = GetComponent<PlayerIdentity>();
            Initialize(classData, identity != null ? identity.PlayerID : 0);
        }
    }

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

        // Initialize class ability — remove any previous, then add by type name
        if (ability != null)
        {
            Destroy(ability);
            ability = null;
        }

        if (!string.IsNullOrEmpty(data.abilityClassName))
        {
            System.Type abilityType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                abilityType = asm.GetType(data.abilityClassName);
                if (abilityType != null) break;
            }

            if (abilityType != null && typeof(ClassAbility).IsAssignableFrom(abilityType))
            {
                ability = (ClassAbility)gameObject.AddComponent(abilityType);
                Debug.Log($"ClassManager: Added ability '{data.abilityClassName}' to {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"ClassManager: Ability class '{data.abilityClassName}' not found or not a ClassAbility.", this);
            }
        }
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
        if (ability != null) ability.ResetCooldown();
    }

    /// <summary>
    /// Swap CombatData for temporary item effects (FireWand, HitscanGun).
    /// Returns the previous CombatData so it can be restored on unequip.
    /// Re-initializes ProjectileSpawner and ParrySystem with the new data.
    /// </summary>
    public CombatData SwapCombatData(CombatData newData)
    {
        var previous = CombatData;
        CombatData = newData;

        if (spawner != null)
            spawner.Initialize(CombatData, CurrentClass != null ? CurrentClass.projectilePrefab : null);
        if (parry != null)
            parry.Initialize(CombatData);

        return previous;
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
