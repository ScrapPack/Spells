using UnityEngine;

/// <summary>
/// Warrior class ability: close-range shield bash.
/// Short dash forward that stuns any player hit and deals light damage.
/// Reflects projectiles in path (like a moving parry).
///
/// GDD: "Warrior has 4HP, slower fire rate, arcing retrievable axes,
/// and shield bash as class ability."
/// </summary>
public class ShieldBashAbility : ClassAbility
{
    [Header("Shield Bash Settings")]
    [Tooltip("Dash distance")]
    [SerializeField] private float dashDistance = 3f;
    [Tooltip("Dash speed (units/sec)")]
    [SerializeField] private float dashSpeed = 20f;
    [Tooltip("Damage dealt on contact")]
    [SerializeField] private float bashDamage = 1f;
    [Tooltip("Hitstun duration applied to hit players")]
    [SerializeField] private float stunDuration = 0.5f;
    [Tooltip("Knockback force applied to hit players")]
    [SerializeField] private float knockback = 10f;

    private Vector2 dashDirection;
    private float dashRemaining;
    private bool hasHitPlayer;

    protected override void Start()
    {
        base.Start();
        abilityName = "Shield Bash";
        cooldownDuration = 6f;
    }

    protected override void Activate()
    {
        if (Input == null || Rb == null) return;

        // Dash direction = aim or facing
        dashDirection = Input.AimDirection;
        if (dashDirection.sqrMagnitude < 0.01f)
        {
            float facing = Mathf.Sign(Input.MoveInput.x);
            if (Mathf.Abs(facing) < 0.1f) facing = 1f;
            dashDirection = new Vector2(facing, 0f);
        }
        dashDirection.Normalize();

        dashRemaining = dashDistance;
        hasHitPlayer = false;
        IsActive = true;

        // Set velocity for dash
        Rb.linearVelocity = dashDirection * dashSpeed;
    }

    protected override void Tick()
    {
        if (Rb == null)
        {
            EndAbility();
            return;
        }

        float moved = dashSpeed * Time.deltaTime;
        dashRemaining -= moved;

        // Maintain dash velocity
        Rb.linearVelocity = dashDirection * dashSpeed;

        // Check for player hits during dash
        CheckHits();

        // End dash when distance covered
        if (dashRemaining <= 0f)
        {
            Rb.linearVelocity = dashDirection * 2f; // Small residual momentum
            EndAbility();
        }
    }

    private void CheckHits()
    {
        if (hasHitPlayer) return; // Only hit one player per bash

        // OverlapCircle at current position
        var hits = Physics2D.OverlapCircleAll(Rb.position, 0.8f);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue; // Don't hit self

            // Hit player: damage + stun
            var otherHealth = hit.GetComponent<HealthSystem>();
            var otherIdentity = hit.GetComponent<PlayerIdentity>();
            if (otherHealth != null && otherIdentity != null && otherHealth.IsAlive)
            {
                otherHealth.TakeDamage(bashDamage, Identity != null ? Identity.PlayerID : -1);

                // Hitstun
                var otherSM = hit.GetComponent<PlayerStateMachine>();
                if (otherSM != null)
                    otherSM.EnterHitstun(stunDuration);

                // Knockback
                var otherRb = hit.GetComponent<Rigidbody2D>();
                if (otherRb != null)
                    otherRb.linearVelocity = dashDirection * knockback;

                hasHitPlayer = true;

                // Game feel
                if (Hitstop.Instance != null)
                    Hitstop.Instance.StopOnParry(); // Same weight as parry
                if (ScreenShake.Instance != null)
                    ScreenShake.Instance.ShakeOnParry();

                break;
            }

            // Hit projectile: reflect it
            var projectile = hit.GetComponent<Projectile>();
            if (projectile != null && Identity != null)
            {
                if (projectile.OwnerPlayerID != Identity.PlayerID)
                {
                    projectile.Reflect(Identity.PlayerID, dashDirection, 1.2f);
                }
            }
        }
    }
}
