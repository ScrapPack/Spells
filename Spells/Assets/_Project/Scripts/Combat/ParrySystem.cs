using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Timing-based parry mechanic. Universal to all classes.
/// On success: reflects incoming projectile back toward attacker.
/// On whiff: brief recovery frames (vulnerability window).
///
/// Parry is strongest in 1v1 duels but risky in free-for-all — you focus on
/// one player while others shoot at your back. This naturally balances it.
/// </summary>
public class ParrySystem : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnParryStart;
    public UnityEvent OnParrySuccess;
    public UnityEvent OnParryWhiff;

    public bool IsParrying { get; private set; }
    public bool IsInRecovery { get; private set; }
    public bool CanParry => !IsParrying && !IsInRecovery;

    private CombatData combatData;
    private PlayerIdentity identity;
    private IInputProvider input;
    private CircleCollider2D parryZone;

    private float parryTimer;
    private float recoveryTimer;
    private bool parryLanded;

    /// <summary>
    /// Initialize with combat data. Called by ClassManager.
    /// </summary>
    public void Initialize(CombatData data)
    {
        combatData = data;
    }

    private void Start()
    {
        identity = GetComponent<PlayerIdentity>();
        input = GetComponent<IInputProvider>();

        // Create a trigger collider for the parry zone (slightly larger than player)
        parryZone = gameObject.AddComponent<CircleCollider2D>();
        parryZone.radius = 0.8f;
        parryZone.isTrigger = true;
        parryZone.enabled = false;
    }

    private void Update()
    {
        if (combatData == null || input == null) return;

        // Recovery tick
        if (IsInRecovery)
        {
            recoveryTimer -= Time.deltaTime;
            if (recoveryTimer <= 0f)
            {
                IsInRecovery = false;
            }
            return;
        }

        // Start parry on input
        if (input.ParryPressed && CanParry)
        {
            input.ConsumeParry();
            StartParry();
        }

        // Active parry window tick
        if (IsParrying)
        {
            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0f)
            {
                EndParry();
            }
        }
    }

    private void StartParry()
    {
        IsParrying = true;
        parryLanded = false;
        parryTimer = combatData.parryWindow;
        parryZone.enabled = true;
        OnParryStart?.Invoke();
    }

    private void EndParry()
    {
        IsParrying = false;
        parryZone.enabled = false;

        if (!parryLanded)
        {
            // Whiff — enter recovery
            IsInRecovery = true;
            recoveryTimer = combatData.parryWhiffRecovery;
            OnParryWhiff?.Invoke();
        }
    }

    /// <summary>
    /// Called when the parry zone trigger detects a projectile.
    /// We use OnTriggerStay2D because the parry zone activates mid-frame
    /// and projectiles might already be inside.
    /// </summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsParrying || parryLanded) return;

        var projectile = other.GetComponent<Projectile>();
        if (projectile == null) return;

        // Don't parry own projectiles
        if (projectile.OwnerPlayerID == identity.PlayerID && !projectile.IsReflected)
            return;

        // Reflect!
        parryLanded = true;

        // Reflect direction: back toward the projectile's origin direction
        Vector2 reflectDir = -projectile.GetComponent<Rigidbody2D>().linearVelocity.normalized;
        projectile.Reflect(identity.PlayerID, reflectDir, combatData.parryReflectSpeedMult);

        OnParrySuccess?.Invoke();

        // End parry early on success
        IsParrying = false;
        parryZone.enabled = false;
    }

    /// <summary>
    /// Reset for new round.
    /// </summary>
    public void ResetForRound()
    {
        IsParrying = false;
        IsInRecovery = false;
        parryTimer = 0f;
        recoveryTimer = 0f;
        parryZone.enabled = false;
    }
}
