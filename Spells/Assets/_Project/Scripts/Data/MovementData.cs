using UnityEngine;

[CreateAssetMenu(fileName = "MovementData", menuName = "Spells/Movement Data")]
public class MovementData : ScriptableObject
{
    [Header("Ground Movement")]
    [Range(1f, 20f)] public float moveSpeed = 8f;
    [Range(5f, 100f)] public float acceleration = 50f;
    [Range(5f, 100f)] public float deceleration = 50f;

    [Header("Dash Burst")]
    [Tooltip("Instant speed multiplier when first pressing a direction from standstill or reversing")]
    [Range(1f, 3f)] public float dashBurstMultiplier = 1.6f;
    [Tooltip("How fast the dash burst decays back to normal moveSpeed")]
    [Range(1f, 30f)] public float dashDecayRate = 8f;
    [Tooltip("Minimum speed (as fraction of moveSpeed) required to be considered 'still' for dash trigger")]
    [Range(0f, 0.3f)] public float dashStillThreshold = 0.15f;

    [Header("Air Movement")]
    [Range(5f, 100f)] public float airAcceleration = 30f;
    [Range(5f, 100f)] public float airDeceleration = 20f;

    [Header("Jump")]
    [Range(5f, 30f)] public float jumpForce = 16f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    [Range(0f, 0.3f)] public float coyoteTimeDuration = 0.1f;
    [Range(0f, 0.3f)] public float jumpBufferDuration = 0.1f;
    [Range(0, 3)] public int maxAirJumps = 0;

    [Header("Wall")]
    public Vector2 wallJumpForce = new Vector2(12f, 16f);
    [Range(1f, 10f)] public float wallSlideSpeedMin = 3f;
    [Range(5f, 25f)] public float wallSlideSpeedMax = 15f;
    [Tooltip("Seconds to accelerate from min to max wall slide speed")]
    [Range(0.1f, 5f)] public float wallSlideAccelTime = 1.2f;
    [Range(0f, 0.5f)] public float wallJumpLockoutTime = 0.15f;

    [Header("Wave-Land")]
    [Tooltip("Speed multiplier applied to horizontal velocity on wave-land")]
    [Range(1f, 2f)] public float waveLandSpeedBoost = 1.3f;
    [Tooltip("Friction deceleration during wave-land slide (lower = longer slide)")]
    [Range(1f, 30f)] public float waveLandFriction = 6f;
    [Tooltip("Minimum horizontal speed to trigger wave-land (prevents standing wave-land)")]
    [Range(0.5f, 5f)] public float waveLandMinSpeed = 2f;

    [Header("Gravity")]
    [Range(1f, 10f)] public float gravityScale = 3f;
    [Range(1f, 3f)] public float fallGravityMultiplier = 1.5f;
    [Range(1f, 50f)] public float maxFallSpeed = 25f;

    public MovementData Clone()
    {
        return Instantiate(this);
    }
}
