using UnityEngine;

public class AirborneState : IPlayerState
{
    private PlayerStateMachine ctx;
    private int airJumpsUsed;
    private bool jumpCut;
    private bool isFastFalling;

    /// <summary>
    /// Previous frame's velocity — used as fallback for wave-land when
    /// collision resolution zeros the velocity before we can read it.
    /// </summary>
    public Vector2 PreviousVelocity { get; private set; }

    public void Enter(PlayerStateMachine ctx)
    {
        this.ctx = ctx;
        airJumpsUsed = 0;
        jumpCut = false;
        isFastFalling = false;
        PreviousVelocity = Vector2.zero;
    }

    public void Execute()
    {
        // Variable jump height: releasing jump early cuts upward velocity
        if (!jumpCut && !ctx.Input.JumpHeld && ctx.Controller.Rb.linearVelocity.y > 0f)
        {
            ctx.Controller.CutJumpVelocity();
            jumpCut = true;
        }

        // Coyote time jump
        if (ctx.Input.JumpPressed && ctx.CoyoteTimer > 0f)
        {
            ctx.Input.ConsumeJump();
            ctx.CoyoteTimer = 0f;
            ctx.Controller.ApplyJumpForce();
            jumpCut = false;
            return;
        }

        // Air jump (for power cards like "Second Wind")
        if (ctx.Input.JumpPressed && airJumpsUsed < ctx.Controller.Data.maxAirJumps)
        {
            ctx.Input.ConsumeJump();
            airJumpsUsed++;
            ctx.Controller.ApplyJumpForce();
            jumpCut = false;
            return;
        }

        // Consume jump press if nothing used it (prevent it carrying over)
        if (ctx.Input.JumpPressed)
        {
            ctx.Input.ConsumeJump();
        }

        // Landed
        if (ctx.Physics.IsGrounded && ctx.Controller.Rb.linearVelocity.y <= 0.1f)
        {
            ctx.ChangeState(ctx.GroundedState);
            return;
        }

        // Spider shoes: wall or ceiling contact enters surface traversal
        if (ctx.HasSpiderShoes)
        {
            if (ctx.Physics.IsTouchingWall || ctx.Physics.IsOnCeiling)
            {
                ctx.ChangeState(ctx.SurfaceTraversalState);
                return;
            }
        }

        // Wall slide: touching wall + holding toward it + not rising fast
        // Allow wall grab while slightly rising (vy <= 2) so wall-jump chains
        // work — after a wall-jump (vy=16), the player reaches the opposite wall
        // at ~0.42s while still at vy≈3.6. Requiring vy<=0 blocks the chain.
        // On contact, zero vertical velocity for clean wall-slide entry.
        if (ctx.Physics.IsTouchingWall
            && ctx.Controller.Rb.linearVelocity.y <= 2f
            && ctx.WallJumpLockoutTimer <= 0f)
        {
            float inputX = ctx.Input.MoveInput.x;
            bool holdingTowardWall = (ctx.Physics.WallDirection == 1 && inputX > 0.1f)
                                  || (ctx.Physics.WallDirection == -1 && inputX < -0.1f);
            if (holdingTowardWall)
            {
                // Zero vertical velocity on wall grab for clean slide start
                ctx.Controller.Rb.linearVelocity = new Vector2(
                    ctx.Controller.Rb.linearVelocity.x,
                    Mathf.Min(ctx.Controller.Rb.linearVelocity.y, 0f));
                ctx.ChangeState(ctx.WallSlideState);
                return;
            }
        }
    }

    public void FixedExecute()
    {
        // Two-frame velocity buffer for wave-land:
        // Collision resolution can zero velocity in the same FixedUpdate we land,
        // so keep the previous frame's velocity as a fallback.
        Vector2 currentVel = ctx.Controller.Rb.linearVelocity;
        PreviousVelocity = ctx.PreLandingVelocity;
        ctx.PreLandingVelocity = currentVel;

        // Air control (reduced acceleration)
        if (ctx.WallJumpLockoutTimer <= 0f)
        {
            float input = ctx.Input.MoveInput.x;
            ctx.Controller.MoveHorizontal(input, ctx.Controller.Data.airAcceleration, ctx.Controller.Data.airDeceleration);
        }

        // Fast fall: Celeste-style — holding down while falling.
        // Instead of cranking gravity (which felt heavy and clunky), use normal
        // fall gravity and accelerate toward fastFallMaxSpeed with MoveTowards.
        // This gives a snappy, controlled fast-fall that's purely vertical.
        isFastFalling = ctx.Input.CrouchHeld && ctx.Controller.Rb.linearVelocity.y <= 0f;
        if (isFastFalling)
        {
            // Normal three-zone gravity system — no special gravity multiplier
            ctx.Controller.ApplyFallGravity(ctx.Input.JumpHeld);

            // Accelerate toward fastFallMaxSpeed (Celeste does 300 units/s²)
            // Using the fastFallGravityMultiplier as an acceleration scaler
            float fastFallAccel = ctx.Controller.Data.fastFallGravityMultiplier
                                * ctx.Controller.Data.gravityScale * 3f;
            float vy = ctx.Controller.Rb.linearVelocity.y;
            float targetVy = -ctx.Controller.Data.fastFallMaxSpeed;
            float newVy = Mathf.MoveTowards(vy, targetVy, fastFallAccel * Time.fixedDeltaTime);
            ctx.Controller.Rb.linearVelocity = new Vector2(
                ctx.Controller.Rb.linearVelocity.x, newVy);
        }
        else
        {
            // Normal three-zone gravity system: fall / peak / rising
            ctx.Controller.ApplyFallGravity(ctx.Input.JumpHeld);
            ctx.Controller.ClampFallSpeed();
        }
    }

    public void Exit()
    {
        ctx.Controller.SetGravityScale(ctx.Controller.Data.gravityScale);
    }
}
