using UnityEngine;

/// <summary>
/// Base class for special power card behaviors that go beyond stat modifiers.
/// Subclasses override OnApply, OnRemove, and optionally OnRoundStart/OnRoundEnd.
/// Attached to the player GameObject when a card with hasSpecialBehavior is picked.
///
/// Examples: Lich Form (revive once on death), Chaos Orb (projectiles split),
/// Glass Cannon (1HP but massive damage), Vampiric (heal on kill).
/// </summary>
public abstract class SpellEffect : MonoBehaviour
{
    /// <summary>
    /// The card that granted this effect.
    /// </summary>
    public PowerCardData SourceCard { get; private set; }

    /// <summary>
    /// Stack count — increments each time the same card is picked.
    /// </summary>
    public int StackCount { get; private set; }

    /// <summary>
    /// Cached player references for subclass use.
    /// </summary>
    protected HealthSystem Health { get; private set; }
    protected ClassManager Class { get; private set; }
    protected PlayerIdentity Identity { get; private set; }
    protected ProjectileSpawner Spawner { get; private set; }

    /// <summary>
    /// Initialize the effect. Called by SpellEffectRegistry when applied.
    /// </summary>
    public void Initialize(PowerCardData card, int stackCount)
    {
        SourceCard = card;
        StackCount = stackCount;

        Health = GetComponent<HealthSystem>();
        Class = GetComponent<ClassManager>();
        Identity = GetComponent<PlayerIdentity>();
        Spawner = GetComponent<ProjectileSpawner>();

        OnApply();
    }

    /// <summary>
    /// Called when the effect is first applied (or stacked).
    /// Override to subscribe to events, set flags, etc.
    /// </summary>
    protected abstract void OnApply();

    /// <summary>
    /// Called when the effect is removed (match reset).
    /// Override to clean up state, unsubscribe from events.
    /// </summary>
    public virtual void OnRemove()
    {
        // Base implementation: just destroy the component
    }

    /// <summary>
    /// Called at the start of each new round. Useful for effects
    /// that reset per-round state (e.g., Lich Form revive count).
    /// </summary>
    public virtual void OnRoundStart() { }

    /// <summary>
    /// Called at end of each round.
    /// </summary>
    public virtual void OnRoundEnd() { }
}
