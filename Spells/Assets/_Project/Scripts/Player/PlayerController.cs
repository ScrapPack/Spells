using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private MovementData baseMovementData;

    public MovementData Data { get; private set; }
    public Rigidbody2D Rb { get; private set; }

    // Dash burst state
    private float currentMaxSpeed;
    private float lastInputDirection;

    // Wave-land state
    public bool IsWaveLanding { get; private set; }
    private float waveLandDirection;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();

        if (baseMovementData != null)
            Data = baseMovementData.Clone();
        else
            Debug.LogError("PlayerController: No MovementData assigned!", this);
    }

    private void Start()
    {
        if (Data != null)
        {
            Rb.gravityScale = Data.gravityScale;
            Rb.freezeRotation = true;
            Rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            currentMaxSpeed = Data.moveSpeed;

            // Zero-friction physics material.
            // All movement control is handled in code — non-zero friction
            // fights the velocity-based system and eats wave-land momentum.
            var physicsMat = new PhysicsMaterial2D("PlayerMaterial");
            physicsMat.friction = 0f;
            physicsMat.bounciness = 0f;
            Rb.sharedMaterial = physicsMat;
        }
    }

    // =========================================================
    // Horizontal Movement
    // =========================================================

    /// <summary>
    /// Frame-rate-independent horizontal movement using MoveTowards.
    /// The old formula (speedDiff * rate * dt) reached max speed in 1 frame
    /// because rate * dt = 50 * 0.02 = 1.0, making it instant convergence.
    /// MoveTowards gives predictable, linear acceleration ramps.
    /// </summary>
    public void MoveHorizontal(float input, float accel, float decel)
    {
        float targetSpeed = input * currentMaxSpeed;
        float rate = Mathf.Abs(targetSpeed) > 0.01f ? accel : decel;
        float newSpeedX = Mathf.MoveTowards(Rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
        Rb.linearVelocity = new Vector2(newSpeedX, Rb.linearVelocity.y);
    }

    // =========================================================
    // Dash Burst
    // =========================================================

    /// <summary>
    /// Call from GroundedState each FixedUpdate.
    /// Gives an instant speed boost when starting from standstill or reversing.
    /// With the fixed MoveTowards acceleration, this actually feels distinct now —
    /// the player briefly overshoots normal max speed then decays back.
    /// </summary>
    public void UpdateDashBurst(float input)
    {
        float inputDir = Mathf.Sign(input);
        float absInput = Mathf.Abs(input);
        float absVelX = Mathf.Abs(Rb.linearVelocity.x);
        float velDir = Mathf.Sign(Rb.linearVelocity.x);

        bool isStill = absVelX < Data.moveSpeed * Data.dashStillThreshold;
        bool isReversing = absInput > 0.1f && absVelX > 0.5f && inputDir != velDir;

        if (absInput > 0.1f && (isStill || isReversing) && inputDir != lastInputDirection)
        {
            currentMaxSpeed = Data.moveSpeed * Data.dashBurstMultiplier;
            lastInputDirection = inputDir;
        }
        else if (absInput < 0.1f)
        {
            lastInputDirection = 0f;
        }

        if (currentMaxSpeed > Data.moveSpeed)
        {
            currentMaxSpeed = Mathf.MoveTowards(currentMaxSpeed, Data.moveSpeed, Data.dashDecayRate * Time.fixedDeltaTime);
        }
    }

    public void ResetDashBurst()
    {
        currentMaxSpeed = Data.moveSpeed;
        lastInputDirection = 0f;
    }

    // =========================================================
    // Wave-Land
    // =========================================================

    /// <summary>
    /// Start a wave-land: convert pre-landing momentum into a horizontal slide.
    /// Uses velocity captured before collision resolution ate the speed.
    /// Simplified from the old version — removed slope direction detection
    /// that was causing confusion and edge cases.
    /// </summary>
    public void StartWaveLand(Vector2 preLandingVelocity)
    {
        float absHorizontal = Mathf.Abs(preLandingVelocity.x);
        float totalSpeed = preLandingVelocity.magnitude;

        // Need some momentum to trigger
        if (absHorizontal < Data.waveLandMinSpeed && totalSpeed < Data.waveLandMinSpeed)
            return;

        // Need a horizontal direction to slide in
        if (absHorizontal < 0.5f)
            return;

        IsWaveLanding = true;
        waveLandDirection = Mathf.Sign(preLandingVelocity.x);

        // Convert total pre-landing momentum into horizontal slide speed
        float boostedSpeed = totalSpeed * Data.waveLandSpeedBoost * waveLandDirection;
        Rb.linearVelocity = new Vector2(boostedSpeed, 0f);

        // Let the slide exceed normal max speed
        currentMaxSpeed = Mathf.Abs(boostedSpeed);
    }

    /// <summary>
    /// Apply wave-land friction. Call every FixedUpdate during a wave-land.
    /// Returns true if the slide is still active, false if it ended.
    /// </summary>
    public bool UpdateWaveLand(float input)
    {
        if (!IsWaveLanding) return false;

        float absVelX = Mathf.Abs(Rb.linearVelocity.x);

        // End slide if speed drops below threshold or player reverses input
        bool inputReversing = Mathf.Abs(input) > 0.1f && Mathf.Sign(input) != waveLandDirection;
        if (absVelX < 1f || inputReversing)
        {
            EndWaveLand();
            return false;
        }

        // Apply friction to slow the slide
        float friction = Data.waveLandFriction * Time.fixedDeltaTime;
        float newVelX = Mathf.MoveTowards(Rb.linearVelocity.x, 0f, friction);
        Rb.linearVelocity = new Vector2(newVelX, Rb.linearVelocity.y);

        // Decay currentMaxSpeed with the slide
        currentMaxSpeed = Mathf.Max(Data.moveSpeed, Mathf.Abs(newVelX));

        return true;
    }

    public void EndWaveLand()
    {
        IsWaveLanding = false;
        currentMaxSpeed = Data.moveSpeed;
    }

    // =========================================================
    // Jump / Wall Jump
    // =========================================================

    public void ApplyJumpForce()
    {
        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, Data.jumpForce);
    }

    public void ApplyWallJump(int wallDirection)
    {
        Vector2 force = new Vector2(-wallDirection * Data.wallJumpForce.x, Data.wallJumpForce.y);
        Rb.linearVelocity = force;
    }

    // =========================================================
    // Wall Slide
    // =========================================================

    public void ClampWallSlideVelocity(float currentSlideSpeed)
    {
        if (Rb.linearVelocity.y < -currentSlideSpeed)
        {
            Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, -currentSlideSpeed);
        }
    }

    // =========================================================
    // Gravity
    // =========================================================

    /// <summary>
    /// Three-zone gravity system inspired by Celeste:
    ///   1. Falling (vy &lt; 0): heavier gravity → snappy descent
    ///   2. Peak of jump (vy near 0, holding jump): lighter gravity → hang time
    ///   3. Rising with jump released: heavier gravity → variable jump height
    ///   4. Rising with jump held: normal gravity
    ///
    /// The old system only had fall vs. normal — no peak zone, no variable height
    /// via gravity (only via velocity cut). This gives much better jump arcs.
    /// </summary>
    public void ApplyFallGravity(bool jumpHeld)
    {
        float vy = Rb.linearVelocity.y;

        if (vy < 0f)
        {
            // Falling — heavier gravity for snappy landing
            Rb.gravityScale = Data.gravityScale * Data.fallGravityMultiplier;
        }
        else if (vy < Data.peakVelocityThreshold && vy >= 0f)
        {
            // Peak of jump — reduced gravity for hang time
            // Only applies if still holding jump; if released, fall fast
            if (jumpHeld)
                Rb.gravityScale = Data.gravityScale * Data.peakGravityMultiplier;
            else
                Rb.gravityScale = Data.gravityScale * Data.fallGravityMultiplier;
        }
        else if (vy > 0f && !jumpHeld)
        {
            // Rising but jump released — heavier gravity for variable height
            Rb.gravityScale = Data.gravityScale * Data.fallGravityMultiplier;
        }
        else
        {
            // Rising with jump held — normal gravity
            Rb.gravityScale = Data.gravityScale;
        }
    }

    public void CutJumpVelocity()
    {
        if (Rb.linearVelocity.y > 0f)
        {
            Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, Rb.linearVelocity.y * Data.jumpCutMultiplier);
        }
    }

    public void ClampFallSpeed()
    {
        if (Rb.linearVelocity.y < -Data.maxFallSpeed)
        {
            Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, -Data.maxFallSpeed);
        }
    }

    public void SetGravityScale(float scale)
    {
        Rb.gravityScale = scale;
    }
}
