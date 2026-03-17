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

        // Refill dash charges and wall stamina on landing
        ctx.DashesRemaining = ctx.Controller.Data.maxAirDashes;
        ctx.WallStamina = ctx.Controller.Data.wallStaminaMax;

        // Pick the best pre-landing velocity — compare HORIZONTAL speed only.
        // The old code compared .magnitude which picked whichever had more
        // total velocity (including vertical). This caused fast-fall vertical
        // speed to dominate the comparison and feed into wave-land.
        Vector2 bestVel = ctx.PreLandingVelocity;
        Vector2 prevVel = ctx.AirborneState.PreviousVelocity;
        if (Mathf.Abs(prevVel.x) > Mathf.Abs(bestVel.x))
            bestVel = prevVel;

        // Try wave-land immediately on landing
        if (ctx.Input.CrouchHeld)
        {
            ctx.Controller.StartWaveLand(bestVel);
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
                // Use the better horizontal velocity of current or previous frame
                Vector2 bestVel = ctx.PreLandingVelocity;
                Vector2 prevVel = ctx.AirborneState.PreviousVelocity;
                if (Mathf.Abs(prevVel.x) > Mathf.Abs(bestVel.x))
                    bestVel = prevVel;
                ctx.Controller.StartWaveLand(bestVel);
                waveLandGraceTimer = 0f;
            }
        }

        // Ground slide: crouch while running (and not already sliding)
        if (ctx.Input.CrouchHeld && !ctx.Controller.IsSliding && !ctx.Controller.IsWaveLanding)
        {
            ctx.Controller.StartGroundSlide();
        }
        // Release crouch to end ground slide (but not wave-land — wave-land ends on its own)
        else if (!ctx.Input.CrouchHeld && ctx.Controller.IsSliding && !ctx.Controller.IsWaveLanding)
        {
            ctx.Controller.EndSlide();
        }

        // Dash (Celeste)
        if (ctx.Input.DashPressed && ctx.DashesRemaining > 0)
        {
            ctx.ChangeState(ctx.DashState);
            return;
        }

        // Jump — wave-jump preserves slide momentum, normal jump doesn't.
        // Corner correction nudges the player past tight ceiling corners.
        if (ctx.Input.JumpPressed)
        {
            ctx.Input.ConsumeJump();
            if (ctx.Controller.IsSliding)
            {
                // Wave-jump: preserve horizontal momentum through the jump
                ctx.Controller.EndSlide(preserveMomentum: true);
            }
            else
            {
                ctx.Controller.EndSlide();
            }
            ctx.Controller.ApplyJumpForce();
            ctx.Controller.TryCornerCorrect(ctx.Controller.Data.cornerCorrectDistance);
            ctx.ChangeState(ctx.AirborneState);
            return;
        }

        // Fell off edge — transition to airborne with coyote time
        if (!ctx.Physics.IsGrounded)
        {
            ctx.Controller.EndSlide();
            ctx.CoyoteTimer = ctx.Controller.Data.coyoteTimeDuration;
            ctx.ChangeState(ctx.AirborneState);
            return;
        }
    }

    public void FixedExecute()
    {
        float input = ctx.Input.MoveInput.x;

        // Any active slide (wave-land or ground slide) takes priority over normal movement
        if (ctx.Controller.UpdateSlide(input))
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
        // Don't end slide here — wave-jump needs it to persist through state transition
    }
}
