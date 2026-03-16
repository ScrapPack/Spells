using UnityEngine;

/// <summary>
/// Wizard class ability: short-range teleport (blink).
/// Instantly moves the player in their aim direction.
/// Brief invincibility during the blink. Cannot teleport through walls.
///
/// GDD: "Wizard has 3HP, fastest fire rate, straight projectiles,
/// and teleport as class ability."
/// </summary>
public class TeleportAbility : ClassAbility
{
    [Header("Teleport Settings")]
    [Tooltip("Maximum teleport distance")]
    [SerializeField] private float maxDistance = 4f;
    [Tooltip("Invincibility duration during blink")]
    [SerializeField] private float blinkIFrames = 0.1f;

    private int wallMask;

    protected override void Start()
    {
        base.Start();
        abilityName = "Teleport";
        cooldownDuration = 4f;

        wallMask = LayerMask.GetMask("Ground", "Wall");
    }

    protected override void Activate()
    {
        if (Input == null || Rb == null) return;

        // Get aim direction (or facing direction)
        Vector2 dir = Input.AimDirection;
        if (dir.sqrMagnitude < 0.01f)
        {
            float facing = Mathf.Sign(Input.MoveInput.x);
            if (Mathf.Abs(facing) < 0.1f) facing = 1f;
            dir = new Vector2(facing, 0f);
        }
        dir.Normalize();

        // Raycast to find max distance (don't teleport through walls)
        RaycastHit2D hit = Physics2D.Raycast(
            Rb.position, dir, maxDistance, wallMask
        );

        float actualDistance = hit.collider != null
            ? hit.distance - 0.5f  // Stop just before the wall
            : maxDistance;

        actualDistance = Mathf.Max(0.5f, actualDistance);

        // Teleport
        Vector2 newPos = Rb.position + dir * actualDistance;
        Rb.position = newPos;
        Rb.linearVelocity = Vector2.zero; // Reset momentum

        // Brief invincibility during blink
        if (Health != null && blinkIFrames > 0f)
        {
            Health.GrantInvincibility(blinkIFrames);
        }
    }
}
