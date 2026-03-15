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

    // Slope handling
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

            // Apply a physics material with friction to help grip slopes
            var physicsMat = new PhysicsMaterial2D("PlayerMaterial");
            physicsMat.friction = 0.4f;
            physicsMat.bounciness = 0f;
            Rb.sharedMaterial = physicsMat;
        }
    }

    // =========================================================
    // Horizontal Movement
    // =========================================================

    public void MoveHorizontal(float input, float accel, float decel)
    {
        float targetSpeed = input * currentMaxSpeed;
        float speedDiff = targetSpeed - Rb.linearVelocity.x;
        float rate = Mathf.Abs(targetSpeed) > 0.01f ? accel : decel;
        float movement = speedDiff * rate * Time.fixedDeltaTime;
        Rb.linearVelocity += new Vector2(movement, 0f);
    }

    /// <summary>
    /// Move along the slope surface direction instead of pure horizontal.
    /// This prevents the player from fighting gravity on slopes.
    /// </summary>
    public void MoveOnSlope(float input, float accel, float decel, Vector2 groundNormal)
    {
        // Get the direction along the slope surface
        // Perpendicular to the normal, pointing right = (normal.y, -normal.x)
        Vector2 slopeDir = new Vector2(groundNormal.y, -groundNormal.x);

        // Project current velocity onto slope direction to get current slope speed
        float currentSlopeSpeed = Vector2.Dot(Rb.linearVelocity, slopeDir);

        float targetSpeed = input * currentMaxSpeed;
        float speedDiff = targetSpeed - currentSlopeSpeed;
        float rate = Mathf.Abs(targetSpeed) > 0.01f ? accel : decel;
        float movement = speedDiff * rate * Time.fixedDeltaTime;

        // Apply movement along the slope direction
        Rb.linearVelocity += slopeDir * movement;
    }

    // =========================================================
    // Slope Anti-Slide
    // =========================================================

    /// <summary>
    /// Counteract gravity pulling the player down slopes when standing still or moving.
    /// Call every FixedUpdate while grounded on a slope.
    /// </summary>
    public void CounterSlopeGravity(Vector2 groundNormal)
    {
        // Calculate the gravity force component along the slope
        Vector2 gravity = UnityEngine.Physics2D.gravity * Rb.gravityScale;
        Vector2 slopeDir = new Vector2(groundNormal.y, -groundNormal.x);

        // Project gravity onto slope direction — this is the force pulling the player downhill
        float gravityAlongSlope = Vector2.Dot(gravity, slopeDir);

        // Apply an equal and opposite force to cancel the slide
        Rb.linearVelocity -= slopeDir * gravityAlongSlope * Time.fixedDeltaTime;

        // Also clamp tiny vertical velocity to prevent jittering on slopes
        if (Mathf.Abs(Rb.linearVelocity.y) < 0.5f && Mathf.Abs(Rb.linearVelocity.x) < 0.5f)
        {
            Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, Mathf.Min(Rb.linearVelocity.y, 0f));
        }
    }

    // =========================================================
    // Dash Burst
    // =========================================================

    /// <summary>
    /// Call from GroundedState each FixedUpdate to handle dash burst logic.
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
    /// Start a wave-land: boost speed along the surface and begin sliding.
    /// Call on landing when crouch is held and player has momentum.
    /// Uses pre-landing velocity (captured before collision ate the speed).
    /// </summary>
    public void StartWaveLand(Vector2 preLandingVelocity)
    {
        // Use the pre-landing velocity since collision resolution has already
        // killed most of the velocity by the time we enter GroundedState
        float totalSpeed = preLandingVelocity.magnitude;
        float absHorizontal = Mathf.Abs(preLandingVelocity.x);

        // Need SOME momentum to trigger
        bool hasHorizontalMomentum = absHorizontal >= Data.waveLandMinSpeed;
        bool hasTotalMomentum = totalSpeed >= Data.waveLandMinSpeed;

        if (!hasHorizontalMomentum && !hasTotalMomentum)
        {
            Debug.Log($"WaveLand: REJECTED — speed too low (horizontal={absHorizontal:F1}, total={totalSpeed:F1}, min={Data.waveLandMinSpeed})");
            return;
        }

        IsWaveLanding = true;

        // Determine slide direction from horizontal velocity or slope
        if (absHorizontal > 0.5f)
        {
            waveLandDirection = Mathf.Sign(preLandingVelocity.x);
        }
        else
        {
            // Falling mostly straight down — slide in the direction of input, or downhill on slopes
            if (physics != null && physics.IsOnSlope && physics.GroundNormal != Vector2.zero)
            {
                Vector2 slopeDir = new Vector2(physics.GroundNormal.y, -physics.GroundNormal.x);
                waveLandDirection = Mathf.Sign(-slopeDir.x); // Downhill direction
            }
            else
            {
                // Not on slope and no horizontal velocity — need at least a direction
                // Can't slide without a direction, so reject
                IsWaveLanding = false;
                return;
            }
        }

        // Convert total pre-landing momentum into horizontal slide speed
        float boostedSpeed = totalSpeed * Data.waveLandSpeedBoost * waveLandDirection;
        Rb.linearVelocity = new Vector2(boostedSpeed, 0f);

        // Set max speed higher so MoveHorizontal doesn't clamp the slide
        currentMaxSpeed = Mathf.Abs(boostedSpeed);

        Debug.Log($"WaveLand: STARTED — preLandVel=({preLandingVelocity.x:F1},{preLandingVelocity.y:F1}), totalSpeed={totalSpeed:F1}, boosted={boostedSpeed:F1}");
    }

    /// <summary>
    /// Apply wave-land friction. Call every FixedUpdate during a wave-land.
    /// Returns true if the slide is still active, false if it has ended.
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

    public void ApplyFallGravity()
    {
        if (Rb.linearVelocity.y < 0f)
            Rb.gravityScale = Data.gravityScale * Data.fallGravityMultiplier;
        else
            Rb.gravityScale = Data.gravityScale;
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
