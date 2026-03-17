using UnityEngine;

public class WallSlidingState : IPlayerState
{
    private PlayerStateMachine ctx;
    private float slideTimer;

    // Grace period: allow player to release toward-wall briefly without detaching,
    // giving time to change direction and press jump for a wall jump.
    private float wallStickTimer;
    private const float WALL_STICK_DURATION = 0.05f; // ~3 frames at 60fps

    public void Enter(PlayerStateMachine ctx)
    {
        this.ctx = ctx;
        ctx.CoyoteTimer = 0f;
        slideTimer = 0f;
        wallStickTimer = 0f;

        // Spider shoes: immediately enter surface traversal instead of sliding
        if (ctx.HasSpiderShoes)
        {
            ctx.ChangeState(ctx.SurfaceTraversalState);
            return;
        }
    }

    public void Execute()
    {
        // Wall jump — always check first, even during wall-stick grace period
        if (ctx.Input.JumpPressed)
        {
            ctx.Input.ConsumeJump();
            ctx.Controller.ApplyWallJump(ctx.Physics.WallDirection);
            ctx.WallJumpLockoutTimer = ctx.Controller.Data.wallJumpLockoutTime;
            ctx.ChangeState(ctx.AirborneState);
            return;
        }

        // Released wall (stopped holding toward it) — use grace timer
        float inputX = ctx.Input.MoveInput.x;
        bool holdingTowardWall = (ctx.Physics.WallDirection == 1 && inputX > 0.1f)
                              || (ctx.Physics.WallDirection == -1 && inputX < -0.1f);

        if (!holdingTowardWall || !ctx.Physics.IsTouchingWall)
        {
            wallStickTimer += Time.deltaTime;
            if (wallStickTimer >= WALL_STICK_DURATION)
            {
                ctx.ChangeState(ctx.AirborneState);
                return;
            }
        }
        else
        {
            // Reset grace timer while still holding toward wall
            wallStickTimer = 0f;
        }

        // Landed while wall sliding
        if (ctx.Physics.IsGrounded)
        {
            ctx.ChangeState(ctx.GroundedState);
            return;
        }
    }

    public void FixedExecute()
    {
        var data = ctx.Controller.Data;

        // Accelerate slide over time: grip weakens the longer you hold
        slideTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(slideTimer / data.wallSlideAccelTime);

        // Ease-in curve: starts slow, accelerates faster
        float easedT = t * t;

        // Cap wall slide max at maxFallSpeed so it never exceeds normal falling
        float clampedMax = Mathf.Min(data.wallSlideSpeedMax, data.maxFallSpeed);
        float currentSlideSpeed = Mathf.Lerp(data.wallSlideSpeedMin, clampedMax, easedT);

        ctx.Controller.ClampWallSlideVelocity(currentSlideSpeed);

        // Scale gravity reduction based on how fresh the grip is
        float gravityFactor = Mathf.Lerp(0.1f, 0.8f, easedT);
        ctx.Controller.SetGravityScale(data.gravityScale * gravityFactor);
    }

    public void Exit()
    {
        ctx.Controller.SetGravityScale(ctx.Controller.Data.gravityScale);
    }
}
