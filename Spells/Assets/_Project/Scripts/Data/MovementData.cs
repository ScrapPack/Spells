using UnityEngine;

[CreateAssetMenu(fileName = "MovementData", menuName = "Spells/Movement Data")]
public class MovementData : ScriptableObject
{
    [Header("Ground Movement")]
    [Tooltip("Max horizontal speed on ground")]
    [Range(1f, 20f)] public float moveSpeed = 10f;
    [Tooltip("Ground acceleration in units/sec². At 100, takes ~5 frames (0.1s) to reach moveSpeed 10.")]
    [Range(5f, 300f)] public float acceleration = 100f;
    [Tooltip("Ground deceleration in units/sec². Slightly slower than accel gives a micro-slide feel.")]
    [Range(5f, 300f)] public float deceleration = 80f;

    [Header("Dash Burst")]
    [Tooltip("Instant speed multiplier when starting from standstill or reversing direction")]
    [Range(1f, 3f)] public float dashBurstMultiplier = 1.5f;
    [Tooltip("How fast the dash burst decays back to normal moveSpeed")]
    [Range(1f, 30f)] public float dashDecayRate = 10f;
    [Tooltip("Minimum speed (as fraction of moveSpeed) to be considered 'still' for dash trigger")]
    [Range(0f, 0.3f)] public float dashStillThreshold = 0.15f;

    [Header("Air Movement")]
    [Tooltip("Air acceleration — slower than ground for committed jumps (Celeste uses 0.65x ground)")]
    [Range(5f, 300f)] public float airAcceleration = 65f;
    [Tooltip("Air deceleration — momentum carries further in the air")]
    [Range(5f, 300f)] public float airDeceleration = 50f;

    [Header("Jump")]
    [Tooltip("Initial jump velocity. With gravityScale 3, jumpForce 14 gives ~3.3 unit apex height.")]
    [Range(5f, 30f)] public float jumpForce = 14f;
    [Tooltip("Velocity multiplier when releasing jump early (lower = more variable height control)")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.4f;
    [Tooltip("Time after leaving ground where jump still works")]
    [Range(0f, 0.3f)] public float coyoteTimeDuration = 0.12f;
    [Tooltip("Time before landing where jump input is buffered")]
    [Range(0f, 0.3f)] public float jumpBufferDuration = 0.12f;
    [Range(0, 3)] public int maxAirJumps = 0;

    [Header("Wall")]
    public Vector2 wallJumpForce = new Vector2(12f, 16f);
    [Range(1f, 10f)] public float wallSlideSpeedMin = 3f;
    [Range(5f, 25f)] public float wallSlideSpeedMax = 15f;
    [Tooltip("Seconds to accelerate from min to max wall slide speed")]
    [Range(0.1f, 5f)] public float wallSlideAccelTime = 1.2f;
    [Range(0f, 0.5f)] public float wallJumpLockoutTime = 0.15f;

    [Header("Fast Fall")]
    [Tooltip("Gravity multiplier when holding down while airborne and falling")]
    [Range(1f, 8f)] public float fastFallGravityMultiplier = 4f;
    [Tooltip("Terminal velocity during fast fall")]
    [Range(1f, 60f)] public float fastFallMaxSpeed = 30f;

    [Header("Ground Slide")]
    [Tooltip("Minimum horizontal speed to trigger a ground slide")]
    [Range(1f, 8f)] public float slideMinSpeed = 3f;
    [Tooltip("Friction applied during ground slide (higher = shorter slide)")]
    [Range(5f, 60f)] public float slideFriction = 25f;
    [Tooltip("Speed boost multiplier applied at start of ground slide")]
    [Range(1f, 2f)] public float slideBoostMultiplier = 1.15f;

    [Header("Wave-Land")]
    [Tooltip("Speed multiplier applied to horizontal velocity on wave-land")]
    [Range(1f, 2f)] public float waveLandSpeedBoost = 1.3f;
    [Tooltip("Friction during wave-land slide (higher = shorter slide). With zero physics friction, 30 gives ~0.5s slide.")]
    [Range(5f, 60f)] public float waveLandFriction = 30f;
    [Tooltip("Minimum horizontal speed to trigger wave-land")]
    [Range(0.5f, 5f)] public float waveLandMinSpeed = 2f;

    [Header("Gravity")]
    [Tooltip("Base gravity multiplier (Unity gravity is -9.81, so scale 3 = -29.43 m/s²)")]
    [Range(1f, 10f)] public float gravityScale = 3f;
    [Tooltip("Gravity multiplier when falling. Higher = snappier descent. Celeste-like = 2-3x.")]
    [Range(1f, 5f)] public float fallGravityMultiplier = 2.5f;
    [Tooltip("Gravity multiplier at the peak of a jump while holding jump. Lower = more hang time.")]
    [Range(0.1f, 1f)] public float peakGravityMultiplier = 0.4f;
    [Tooltip("Velocity threshold defining the peak-of-jump zone (vy between 0 and this value)")]
    [Range(0.5f, 5f)] public float peakVelocityThreshold = 2f;
    [Tooltip("Terminal fall velocity")]
    [Range(1f, 50f)] public float maxFallSpeed = 20f;

    public MovementData Clone()
    {
        return Instantiate(this);
    }
}
