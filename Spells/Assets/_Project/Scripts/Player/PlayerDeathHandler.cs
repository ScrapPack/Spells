using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles the player death sequence: disable input, play effects,
/// transition to spectate or disable. Subscribes to HealthSystem.OnDeath.
///
/// The death handler is responsible for:
/// 1. Disabling player input and movement
/// 2. Playing death visual effects
/// 3. Disabling colliders (prevent post-death hits)
/// 4. Optionally enabling spectate mode (not yet implemented)
///
/// Resurrection (e.g., from a Lich Form card) would call Revive().
/// </summary>
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Seconds to wait before disabling the player object")]
    [SerializeField] private float deathDelay = 1f;
    [Tooltip("Should the player object be deactivated after death?")]
    [SerializeField] private bool deactivateOnDeath = true;

    [Header("Events")]
    public UnityEvent OnDeathSequenceStart;
    public UnityEvent OnDeathSequenceComplete;
    public UnityEvent OnRevived;

    public bool IsDead { get; private set; }

    private HealthSystem health;
    private PlayerStateMachine stateMachine;
    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private float deathTimer;
    private bool deathSequenceActive;

    private void Start()
    {
        health = GetComponent<HealthSystem>();
        stateMachine = GetComponent<PlayerStateMachine>();
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();

        if (health != null)
        {
            health.OnDeath.AddListener(OnDied);
        }
    }

    private void OnDied()
    {
        if (IsDead) return;

        IsDead = true;
        deathSequenceActive = true;
        deathTimer = deathDelay;

        // Disable input immediately
        var input = GetComponent<PlayerInputHandler>();
        if (input != null)
            input.enabled = false;

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Disable colliders (prevent post-death interactions)
        foreach (var col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }

        OnDeathSequenceStart?.Invoke();
    }

    private void Update()
    {
        if (!deathSequenceActive) return;

        deathTimer -= Time.deltaTime;
        if (deathTimer <= 0f)
        {
            CompleteDeathSequence();
        }
    }

    private void CompleteDeathSequence()
    {
        deathSequenceActive = false;
        OnDeathSequenceComplete?.Invoke();

        if (deactivateOnDeath)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Revive the player (e.g., Lich Form card).
    /// Restores health to 1 HP, re-enables input and colliders.
    /// </summary>
    public void Revive(int hpToRestore = 1)
    {
        if (!IsDead) return;

        IsDead = false;
        deathSequenceActive = false;
        gameObject.SetActive(true);

        // Restore health
        if (health != null)
        {
            health.ResetForRound();
            // Set to specific HP if needed
            if (hpToRestore < health.MaxHP)
            {
                int delta = hpToRestore - health.MaxHP;
                health.ModifyMaxHP(delta);
            }
        }

        // Re-enable physics
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        // Re-enable colliders
        foreach (var col in colliders)
        {
            if (col != null)
                col.enabled = true;
        }

        // Re-enable input
        var input = GetComponent<PlayerInputHandler>();
        if (input != null)
            input.enabled = true;

        OnRevived?.Invoke();
    }

    /// <summary>
    /// Reset for new round.
    /// </summary>
    public void ResetForRound()
    {
        IsDead = false;
        deathSequenceActive = false;
        gameObject.SetActive(true);

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;

        foreach (var col in colliders)
        {
            if (col != null)
                col.enabled = true;
        }

        var input = GetComponent<PlayerInputHandler>();
        if (input != null)
            input.enabled = true;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath.RemoveListener(OnDied);
    }
}
