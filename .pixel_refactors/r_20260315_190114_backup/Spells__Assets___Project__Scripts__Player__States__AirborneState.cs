using UnityEngine;

public class AirborneState : IPlayerState
{
    private PlayerStateMachine ctx;
    private int airJumpsUsed;
    private bool jumpCut;

    public void Enter(PlayerStateMachine ctx)
    {
        this.ctx = ctx;
        airJumpsUsed = 0;
        jumpCut = false;
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

        // Wall slide: touching wall + holding toward it + falling
        if (ctx.Physics.IsTouchingWall
            && ctx.Controller.Rb.linearVelocity.y <= 0f
            && ctx.WallJumpLockoutTimer <= 0f)
        {
            float inputX = ctx.Input.MoveInput.x;
            bool holdingTowardWall = (ctx.Physics.WallDirection == 1 && inputX > 0.1f)
                                  || (ctx.Physics.WallDirection == -1 && inputX < -0.1f);
            if (holdingTowardWall)
            {
                ctx.ChangeState(ctx.WallSlidingState);
                return;
            }
        }
    }

    public void FixedExecute()
    {
        // Record velocity BEFORE physics changes it — used by wave-land on landing
        ctx.PreLandingVelocity = ctx.Controller.Rb.linearVelocity;

        // Air control (reduced acceleration)
        if (ctx.WallJumpLockoutTimer <= 0f)
        {
            float input = ctx.Input.MoveInput.x;
            ctx.Controller.MoveHorizontal(input, ctx.Controller.Data.airAcceleration, ctx.Controller.Data.airDeceleration);
        }

        // Three-zone gravity system: fall / peak / rising
        // Now takes jumpHeld so the peak zone and variable height work correctly
        ctx.Controller.ApplyFallGravity(ctx.Input.JumpHeld);
        ctx.Controller.ClampFallSpeed();
    }

    public void Exit()
    {
        ctx.Controller.SetGravityScale(ctx.Controller.Data.gravityScale);
    }
}
