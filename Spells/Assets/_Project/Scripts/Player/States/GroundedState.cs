using UnityEngine;

public class GroundedState : IPlayerState
{
    private PlayerStateMachine ctx;

    // Grace period: allow wave-land to trigger within a few frames of landing
    // Handles timing issues where CrouchHeld might update 1 frame late
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

        // Wave-land slide takes priority over normal movement
        if (ctx.Controller.UpdateWaveLand(input))
            return;

        // Dash burst + normal ground movement
        ctx.Controller.UpdateDashBurst(input);
        ctx.Controller.MoveHorizontal(input, ctx.Controller.Data.acceleration, ctx.Controller.Data.deceleration);

        // Simple slope anti-slide: when standing still on a slope with zero friction,
        // gravity would pull the player downhill. Instead of the old complex
        // CounterSlopeGravity system (which fought the velocity code), just zero
        // velocity when idle on slopes. Clean and reliable.
        if (Mathf.Abs(input) < 0.1f && ctx.Physics.IsOnSlope)
        {
            Vector2 vel = ctx.Controller.Rb.linearVelocity;
            if (vel.magnitude < 1.5f)
            {
                ctx.Controller.Rb.linearVelocity = Vector2.zero;
            }
        }
    }

    public void Exit()
    {
        ctx.Controller.ResetDashBurst();
    }
}
