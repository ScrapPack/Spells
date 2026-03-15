using UnityEngine;

public class GroundedState : IPlayerState
{
    private PlayerStateMachine ctx;

    // Grace period: allow wave-land to trigger within a few frames of landing
    // This handles timing issues where CrouchHeld might update 1 frame late
    private float waveLandGraceTimer;
    private const float WAVE_LAND_GRACE = 0.1f; // 100ms window after landing

    public void Enter(PlayerStateMachine ctx)
    {
        this.ctx = ctx;
        ctx.Controller.SetGravityScale(ctx.Controller.Data.gravityScale);
        ctx.CoyoteTimer = 0f;
        waveLandGraceTimer = WAVE_LAND_GRACE;

        // Try wave-land immediately on landing
        if (ctx.Input.CrouchHeld)
        {
            ctx.Controller.StartWaveLand(ctx.PreLandingVelocity);
            waveLandGraceTimer = 0f; // Consumed
        }

        // Jump buffer: if jump was pressed just before landing, jump immediately
        if (ctx.JumpBufferTimer > 0f)
        {
            ctx.JumpBufferTimer = 0f;
            ctx.Input.ConsumeJump();
            ctx.Controller.EndWaveLand();
            ctx.Controller.ApplyJumpForce();
            ctx.ChangeState(ctx.AirborneState);
        }
    }

    public void Execute()
    {
        // Wave-land grace period: if crouch detected within the grace window after landing,
        // trigger wave-land (handles Update execution order timing issues)
        if (waveLandGraceTimer > 0f && !ctx.Controller.IsWaveLanding)
        {
            waveLandGraceTimer -= Time.deltaTime;
            if (ctx.Input.CrouchHeld)
            {
                ctx.Controller.StartWaveLand(ctx.PreLandingVelocity);
                waveLandGraceTimer = 0f;
            }
        }

        // Jump (also cancels wave-land)
        if (ctx.Input.JumpPressed)
        {
            ctx.Input.ConsumeJump();
            ctx.Controller.EndWaveLand();
            ctx.Controller.ApplyJumpForce();
            ctx.ChangeState(ctx.AirborneState);
            return;
        }

        // Fell off edge — transition to airborne with coyote time
        if (!ctx.Physics.IsGrounded)
        {
            ctx.Controller.EndWaveLand();
            ctx.CoyoteTimer = ctx.Controller.Data.coyoteTimeDuration;
            ctx.ChangeState(ctx.AirborneState);
            return;
        }
    }

    public void FixedExecute()
    {
        float input = ctx.Input.MoveInput.x;
        bool onSlope = ctx.Physics.IsOnSlope;
        Vector2 groundNormal = ctx.Physics.GroundNormal;

        // Counteract gravity on slopes to prevent sliding when standing still
        if (onSlope)
        {
            ctx.Controller.CounterSlopeGravity(groundNormal);
        }

        // Wave-land slide takes priority over normal movement
        if (ctx.Controller.UpdateWaveLand(input))
        {
            // During wave-land on a slope, also counter gravity so player doesn't
            // decelerate going uphill or accelerate going downhill unnaturally
            if (onSlope)
            {
                ctx.Controller.CounterSlopeGravity(groundNormal);
            }
            return;
        }

        // Normal ground movement
        ctx.Controller.UpdateDashBurst(input);

        if (onSlope)
        {
            // Move along the slope surface instead of pure horizontal
            ctx.Controller.MoveOnSlope(input, ctx.Controller.Data.acceleration, ctx.Controller.Data.deceleration, groundNormal);
        }
        else
        {
            ctx.Controller.MoveHorizontal(input, ctx.Controller.Data.acceleration, ctx.Controller.Data.deceleration);
        }
    }

    public void Exit()
    {
        ctx.Controller.ResetDashBurst();
    }
}
