using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private MovementData baseMovementData;

    public MovementData Data { get; private set; }
    public Rigidbody2D Rb { get; private set; }

    /// <summary>Direction the player is currently facing: +1 right, -1 left.</summary>
    public int FacingDirection { get; private set; } = 1;

    // Dash burst state
    private float currentMaxSpeed;
    private float lastInputDirection;

    // Wave-land / ground-slide shared state
    public bool IsWaveLanding { get; private set; }
    public bool IsSliding { get; private set; }
    private float slideDirection;

    // Cached component references
    private PhysicsCheck physics;
    private BoxCollider2D col2d;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        physics = GetComponent<PhysicsCheck>();
        col2d = GetComponent<BoxCollider2D>();

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
    /// Uses turnAroundAcceleration when input is opposite to current velocity
    /// for snappy Celeste-style direction changes.
    /// </summary>
    public void MoveHorizontal(float input, float accel, float decel)
    {
        float velX = Rb.linearVelocity.x;
        float targetSpeed = input * currentMaxSpeed;

        bool isReversing = Mathf.Abs(input) > 0.1f
                        && Mathf.Abs(velX) > 0.5f
                        && Mathf.Sign(input) != Mathf.Sign(velX);

        float rate;
        if (isReversing)
            rate = Data.turnAroundAcceleration;
        else
            rate = Mathf.Abs(targetSpeed) > 0.01f ? accel : decel;

        float newSpeedX = Mathf.MoveTowards(velX, targetSpeed, rate * Time.fixedDeltaTime);
        Rb.linearVelocity = new Vector2(newSpeedX, Rb.linearVelocity.y);

        if (Mathf.Abs(input) > 0.1f)
            FacingDirection = (int)Mathf.Sign(input);
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
    // Dash (Celeste)
    // =========================================================

    /// <summary>
    /// Begin a Celeste-style dash in the given normalized 8-directional vector.
    /// Zeroes gravity for the dash duration so the player travels in a straight line.
    /// </summary>
    public void ApplyDash(Vector2 direction)
    {
        Rb.gravityScale = 0f;
        Rb.linearVelocity = direction * Data.dashSpeed;
    }

    /// <summary>
    /// Re-apply dash velocity each FixedUpdate to resist any forces during the dash.
    /// </summary>
    public void HoldDashVelocity(Vector2 direction)
    {
        Rb.linearVelocity = direction * Data.dashSpeed;
    }

    /// <summary>
    /// End the dash and restore normal gravity.
    /// Pass preserveHorizontal=true for wavedash/dash-jump to keep horizontal speed.
    /// </summary>
    public void EndDash(bool preserveHorizontal = false)
    {
        Rb.gravityScale = Data.gravityScale;
        if (preserveHorizontal)
        {
            // Let horizontal speed exceed normal cap temporarily (decays via UpdateDashBurst)
            currentMaxSpeed = Mathf.Max(Data.moveSpeed, Mathf.Abs(Rb.linearVelocity.x));
        }
        else
        {
            float clampedX = Mathf.Clamp(Rb.linearVelocity.x, -Data.moveSpeed, Data.moveSpeed);
            Rb.linearVelocity = new Vector2(clampedX, Rb.linearVelocity.y);
            currentMaxSpeed = Data.moveSpeed;
        }
    }

    // =========================================================
    // Corner Correction
    // =========================================================

    /// <summary>
    /// When the player's corner clips a ceiling edge during an upward jump,
    /// nudge them horizontally up to <paramref name="range"/> units so they
    /// slide past the corner instead of bonking their head.
    /// Returns true if a correction was applied.
    /// </summary>
    public bool TryCornerCorrect(float range)
    {
        if (Rb.linearVelocity.y <= 0f) return false;
        if (col2d == null || physics == null) return false;

        Vector2 halfSize = col2d.size * 0.5f;
        Vector2 center   = (Vector2)transform.position + col2d.offset;
        LayerMask mask   = physics.GroundLayerMask;
        float probeLen   = 0.15f;

        bool centerHit = Physics2D.Raycast(center, Vector2.up, halfSize.y + probeLen, mask).collider != null;
        if (!centerHit) return false;

        bool leftHit  = Physics2D.Raycast(center + new Vector2(-halfSize.x + 0.02f, halfSize.y - 0.02f), Vector2.up, probeLen, mask).collider != null;
        bool rightHit = Physics2D.Raycast(center + new Vector2( halfSize.x - 0.02f, halfSize.y - 0.02f), Vector2.up, probeLen, mask).collider != null;

        if (leftHit && !rightHit)
        {
            // Right side is clear — nudge right
            transform.position += new Vector3(range, 0f, 0f);
            return true;
        }
        if (rightHit && !leftHit)
        {
            // Left side is clear — nudge left
            transform.position -= new Vector3(range, 0f, 0f);
            return true;
        }

        return false;
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
