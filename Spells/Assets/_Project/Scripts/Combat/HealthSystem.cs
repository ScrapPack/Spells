using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages HP for a player or damageable entity.
/// Fires events on damage, death, and heal — other systems subscribe to these
/// rather than polling health state.
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged;   // (currentHP, maxHP)
    public UnityEvent<float> OnDamaged;             // (damageAmount)
    public UnityEvent OnDeath;
    public UnityEvent<int> OnHealed;                // (healAmount)

    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public bool IsAlive => CurrentHP > 0;
    public bool IsInvincible { get; private set; }

    /// <summary>
    /// Player ID of whoever last dealt damage. Used for kill credit.
    /// -1 means no attacker (environment, self, etc.).
    /// </summary>
    public int LastAttackerID { get; private set; } = -1;

    private float invincibilityTimer;
    private float invincibilityDuration;

    /// <summary>
    /// Initialize health from CombatData. Called by ClassManager on spawn.
    /// </summary>
    public void Initialize(int maxHP, float iFrameDuration)
    {
        MaxHP = maxHP;
        CurrentHP = maxHP;
        invincibilityDuration = iFrameDuration;
        invincibilityTimer = 0f;
        IsInvincible = false;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    /// <summary>
    /// Modify max HP (e.g., Stone Skin card adds +1, some effects reduce).
    /// Clamps current HP to new max.
    /// </summary>
    public void ModifyMaxHP(int delta)
    {
        MaxHP = Mathf.Max(1, MaxHP + delta);
        CurrentHP = Mathf.Min(CurrentHP, MaxHP);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    /// <summary>
    /// Apply damage with attacker tracking. Respects invincibility frames.
    /// Returns true if damage was actually applied.
    /// </summary>
    public bool TakeDamage(float amount, int attackerID = -1)
    {
        if (!IsAlive || IsInvincible) return false;

        if (attackerID >= 0)
            LastAttackerID = attackerID;

        int intDamage = Mathf.Max(1, Mathf.RoundToInt(amount));
        CurrentHP = Mathf.Max(0, CurrentHP - intDamage);

        OnDamaged?.Invoke(amount);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);

        if (CurrentHP <= 0)
        {
            OnDeath?.Invoke();
            return true;
        }

        // Start i-frames
        if (invincibilityDuration > 0f)
        {
            IsInvincible = true;
            invincibilityTimer = invincibilityDuration;
        }

        return true;
    }

    /// <summary>
    /// Heal HP. Clamps to max.
    /// </summary>
    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        int before = CurrentHP;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        int healed = CurrentHP - before;

        if (healed > 0)
        {
            OnHealed?.Invoke(healed);
            OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        }
    }

    /// <summary>
    /// Grant invincibility for a duration (e.g., teleport blink, spawn protection).
    /// Extends existing invincibility if already active.
    /// </summary>
    public void GrantInvincibility(float duration)
    {
        IsInvincible = true;
        invincibilityTimer = Mathf.Max(invincibilityTimer, duration);
    }

    /// <summary>
    /// Reset to full HP for new round.
    /// </summary>
    public void ResetForRound()
    {
        CurrentHP = MaxHP;
        IsInvincible = false;
        invincibilityTimer = 0f;
        LastAttackerID = -1;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }

    private void Update()
    {
        // Tick invincibility
        if (IsInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                IsInvincible = false;
            }
        }
    }
}
