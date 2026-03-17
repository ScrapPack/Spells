using UnityEngine;

public class WallSlidingState : IPlayerState
{
    private PlayerStateMachine ctx;
    private float slideTimer;

    // Grace period for neutral input: lets the player briefly release toward-wall
    // to reposition their thumb for a wall-jump without immediately detaching.
    // Actively pushing AWAY detaches immediately (no grace).
    private float wallStickTimer;
    private const float WALL_STICK_DURATION = 0.05f; // ~3 frames at 60 fps

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
        // Dash — can dash out of wall slide (refills on wall-jump by design)
        if (ctx.Input.DashPressed && ctx.DashesRemaining > 0)
        {
            ctx.ChangeState(ctx.DashState);
            return;
        }

        // Wall jump — always check first so jump timing isn't affected by detach logic
        if (ctx.Input.JumpPressed)
        {
            ctx.Input.ConsumeJump();
            // Wall-jump refills the air dash charge (Celeste behavior)
            ctx.DashesRemaining = ctx.Controller.Data.maxAirDashes;
            ctx.Controller.ApplyWallJump(ctx.Physics.WallDirection);
            ctx.WallJumpLockoutTimer = ctx.Controller.Data.wallJumpLockoutTime;
            ctx.ChangeState(ctx.AirborneState);
            return;
        }

        // Detach conditions:
        //   1. No longer touching the wall
        //   2. Stamina depleted
        //   3. Actively pushing AWAY from wall (immediate detach — no grace)
        float inputX = ctx.Input.MoveInput.x;
        bool holdingAway = (ctx.Physics.WallDirection == 1 && inputX < -0.3f)
                        || (ctx.Physics.WallDirection == -1 && inputX > 0.3f);
        bool holdingToward = (ctx.Physics.WallDirection == 1 && inputX > 0.3f)
                          || (ctx.Physics.WallDirection == -1 && inputX < -0.3f);

        if (!ctx.Physics.IsTouchingWall || holdingAway || ctx.WallStamina <= 0f)
        {
            ctx.ChangeState(ctx.AirborneState);
            return;
        }

        // Brief grace for neutral input: allows repositioning thumb to press jump
        if (!holdingToward)
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

        // Climb up: holding up while stamina remains
        bool holdingUp = ctx.Input.MoveInput.y > 0.5f;
        if (holdingUp && ctx.WallStamina > 0f)
        {
            ctx.Controller.Rb.linearVelocity = new Vector2(ctx.Controller.Rb.linearVelocity.x, data.wallClimbSpeed);
            // Climbing drains stamina at 2x the normal slide rate
            ctx.WallStamina -= data.wallStaminaDrainRate * 2f * Time.fixedDeltaTime;
        }
        else
        {
            // Normal slide: accelerate from min to max slide speed over time
            slideTimer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(slideTimer / data.wallSlideAccelTime);
            float easedT = t * t; // Ease-in: starts slow, accelerates

            float clampedMax = Mathf.Min(data.wallSlideSpeedMax, data.maxFallSpeed);
            float currentSlideSpeed = Mathf.Lerp(data.wallSlideSpeedMin, clampedMax, easedT);

            ctx.Controller.ClampWallSlideVelocity(currentSlideSpeed);

            // Scale gravity reduction based on grip freshness
            float gravityFactor = Mathf.Lerp(0.1f, 0.8f, easedT);
            ctx.Controller.SetGravityScale(data.gravityScale * gravityFactor);

            // Drain stamina while sliding
            ctx.WallStamina -= data.wallStaminaDrainRate * Time.fixedDeltaTime;
            ctx.WallStamina = Mathf.Max(0f, ctx.WallStamina);
        }
    }

    public void Exit()
    {
        ctx.Controller.SetGravityScale(ctx.Controller.Data.gravityScale);
    }
}
