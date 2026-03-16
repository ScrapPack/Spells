using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for per-class special abilities.
/// Each class has one unique ability beyond their basic attack/parry.
/// Abilities have a cooldown and are activated by a dedicated input.
///
/// Examples:
/// - Wizard: Teleport (short-range blink)
/// - Warrior: Shield Bash (close-range stun)
/// - Warlock: Life Drain (channeled beam, heals on hit)
/// - Alchemist: Potion Toss (AoE heal or damage zone)
///
/// Subclasses override Activate() and optionally Tick() for channeled abilities.
/// </summary>
public abstract class ClassAbility : MonoBehaviour
{
    [Header("Ability Settings")]
    [SerializeField] protected float cooldownDuration = 5f;
    [SerializeField] protected string abilityName = "Ability";

    [Header("Events")]
    public UnityEvent OnAbilityUsed;
    public UnityEvent OnAbilityReady;

    public float CooldownRemaining { get; protected set; }
    public float CooldownProgress => cooldownDuration > 0f
        ? 1f - (CooldownRemaining / cooldownDuration) : 1f;
    public bool IsReady => CooldownRemaining <= 0f;
    public bool IsActive { get; protected set; }
    public string AbilityName => abilityName;

    protected PlayerIdentity Identity { get; private set; }
    protected HealthSystem Health { get; private set; }
    protected Rigidbody2D Rb { get; private set; }
    protected IInputProvider Input { get; private set; }

    protected virtual void Start()
    {
        Identity = GetComponent<PlayerIdentity>();
        Health = GetComponent<HealthSystem>();
        Rb = GetComponent<Rigidbody2D>();
        Input = GetComponent<IInputProvider>();
    }

    protected virtual void Update()
    {
        // Tick cooldown
        if (CooldownRemaining > 0f)
        {
            CooldownRemaining -= Time.deltaTime;
            if (CooldownRemaining <= 0f)
            {
                CooldownRemaining = 0f;
                OnAbilityReady?.Invoke();
            }
        }

        // Tick active ability
        if (IsActive)
        {
            Tick();
        }
    }

    /// <summary>
    /// Try to use the ability. Returns true if activated.
    /// </summary>
    public bool TryActivate()
    {
        if (!IsReady || IsActive) return false;
        if (Health != null && !Health.IsAlive) return false;

        Activate();
        CooldownRemaining = cooldownDuration;
        OnAbilityUsed?.Invoke();
        return true;
    }

    /// <summary>
    /// Override: perform the ability action.
    /// </summary>
    protected abstract void Activate();

    /// <summary>
    /// Override: called every frame while ability is active (for channeled abilities).
    /// Default implementation does nothing.
    /// </summary>
    protected virtual void Tick() { }

    /// <summary>
    /// End the active ability. Call from subclass when done.
    /// </summary>
    protected void EndAbility()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reset cooldown (new round).
    /// </summary>
    public void ResetCooldown()
    {
        CooldownRemaining = 0f;
        IsActive = false;
    }
}
