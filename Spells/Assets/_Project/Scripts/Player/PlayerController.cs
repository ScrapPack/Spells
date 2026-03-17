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

    // Wave-land / ground-slide shared state
    public bool IsWaveLanding { get; private set; }
    public bool IsSliding { get; private set; }
    private float slideDirection;

    // Cached component reference for slope detection
    private PhysicsCheck physics;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        physics = GetComponent<PhysicsCheck>();

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
    /// Start a wave-land: convert pre-landing HORIZONTAL momentum into a slide.
    /// Only horizontal velocity matters — vertical fast-fall speed must NOT
    /// convert to horizontal momentum. In Celeste and Rivals of Aether,
    /// fast fall is purely vertical and never feeds into horizontal movement.
    /// </summary>
    public void StartWaveLand(Vector2 preLandingVelocity)
    {
        float absHorizontal = Mathf.Abs(preLandingVelocity.x);

        // Need meaningful horizontal momentum to trigger.
        // The old code checked magnitude (including vertical) which meant
        // fast-falling at (1.5, -30) had magnitude 30 and triggered a massive slide.
        if (absHorizontal < Data.waveLandMinSpeed)
            return;

        IsWaveLanding = true;
        IsSliding = true;
        slideDirection = Mathf.Sign(preLandingVelocity.x);

        // Use ONLY horizontal velocity for the slide speed.
        float boostedSpeed = absHorizontal * Data.waveLandSpeedBoost * slideDirection;
        Rb.linearVelocity = new Vector2(boostedSpeed, 0f);

        // Let the slide exceed normal max speed
        currentMaxSpeed = Mathf.Abs(boostedSpeed);
    }

    /// <summary>
    /// Start a ground slide: crouch while running on the ground.
    /// Uses current horizontal velocity as the slide base.
    /// </summary>
    public void StartGroundSlide()
    {
        float absVelX = Mathf.Abs(Rb.linearVelocity.x);
        if (absVelX < Data.slideMinSpeed) return;

        IsSliding = true;
        slideDirection = Mathf.Sign(Rb.linearVelocity.x);

        // Small boost at slide start for responsiveness
        float boostedSpeed = absVelX * Data.slideBoostMultiplier * slideDirection;
        Rb.linearVelocity = new Vector2(boostedSpeed, Rb.linearVelocity.y);
        currentMaxSpeed = Mathf.Abs(boostedSpeed);
    }

    /// <summary>
    /// Apply slide friction. Call every FixedUpdate during any slide (wave-land or ground slide).
    /// Slope-aware: on slopes, velocity is projected along the ground surface so
    /// downhill slopes accelerate slides and uphill slopes decelerate them.
    /// Returns true if the slide is still active, false if it ended.
    /// </summary>
    public bool UpdateSlide(float input)
    {
        if (!IsSliding) return false;

        float absVelX = Mathf.Abs(Rb.linearVelocity.x);

        // End slide if speed drops below threshold or player reverses input
        bool inputReversing = Mathf.Abs(input) > 0.1f && Mathf.Sign(input) != slideDirection;
        if (absVelX < 1f || inputReversing)
        {
            EndSlide();
            return false;
        }

        // Use wave-land friction for wave-lands, slide friction for ground slides
        float friction = IsWaveLanding ? Data.waveLandFriction : Data.slideFriction;
        float frictionStep = friction * Time.fixedDeltaTime;

        if (physics != null && physics.IsOnSlope)
        {
            // Slope-aware sliding: project velocity along the ground surface.
            // The slope tangent is perpendicular to the ground normal.
            Vector2 normal = physics.GroundNormal;
            Vector2 slopeTangent = new Vector2(normal.y, -normal.x);

            // Ensure tangent points in the slide direction
            if (Vector2.Dot(slopeTangent, Vector2.right * slideDirection) < 0)
                slopeTangent = -slopeTangent;

            // Get current speed along the slope surface
            float slopeSpeed = Vector2.Dot(Rb.linearVelocity, slopeTangent);

            // On downhill slopes, gravity component along slope adds speed.
            // On uphill slopes, gravity component along slope subtracts speed.
            // Gravity is handled by Unity physics — we just apply friction.
            float newSpeed = Mathf.MoveTowards(slopeSpeed, 0f, frictionStep);

            // Set velocity along the slope tangent
            Rb.linearVelocity = slopeTangent * newSpeed;
        }
        else
        {
            // Flat ground: pure horizontal friction
            float newVelX = Mathf.MoveTowards(Rb.linearVelocity.x, 0f, frictionStep);
            Rb.linearVelocity = new Vector2(newVelX, Rb.linearVelocity.y);
        }

        // Decay currentMaxSpeed with the slide
        currentMaxSpeed = Mathf.Max(Data.moveSpeed, absVelX);

        return true;
    }

    /// <summary>
    /// End any active slide. Optionally preserves horizontal momentum for wave-jump.
    /// </summary>
    public void EndSlide(bool preserveMomentum = false)
    {
        float currentVelX = Rb.linearVelocity.x;
        IsWaveLanding = false;
        IsSliding = false;
        if (!preserveMomentum)
            currentMaxSpeed = Data.moveSpeed;
        else
            currentMaxSpeed = Mathf.Max(Data.moveSpeed, Mathf.Abs(currentVelX));
    }

    // Legacy name for backward compat
    public void EndWaveLand() => EndSlide();

    /// <summary>
    /// Update the old-API wave-land call. Delegates to unified UpdateSlide.
    /// </summary>
    public bool UpdateWaveLand(float input) => UpdateSlide(input);

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
