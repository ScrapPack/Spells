using UnityEngine;

/// <summary>
/// Celeste-style 8-directional dash state.
///
/// Flow:
///   Enter  → consume dash charge, snap input to 8-dir, apply velocity,
///            zero gravity, trigger freeze frame
///   FixedExecute → hold velocity constant for the dash duration
///   Execute → count down dash timer; dash-jump (jump during dash) transitions
///              to Airborne with horizontal momentum preserved
///   Exit   → restore gravity, cap horizontal speed unless preserving momentum
///
/// Wavedash is emergent: dash diagonally down while approaching ground →
/// jump immediately in GroundedState → horizontal dash speed carries through.
/// </summary>
public class DashState : IPlayerState
{
    private PlayerStateMachine ctx;
    private float dashTimer;
    private Vector2 dashDirection;

    public void Enter(PlayerStateMachine ctx)
    {
        this.ctx = ctx;

        // Consume the dash charge
        ctx.Input.ConsumeDash();
        ctx.DashesRemaining--;

        // Snap input to nearest of 8 cardinal/diagonal directions.
        // Default to facing direction if no input.
        Vector2 input = ctx.Input.MoveInput;
        dashDirection = input.sqrMagnitude > 0.25f
            ? SnapToEightDir(input)
            : new Vector2(ctx.Controller.FacingDirection, 0f);

        ctx.Controller.ApplyDash(dashDirection);
        ctx.StartFreezeFrame(ctx.Controller.Data.dashFreezeFrameDuration);

        // dashTimer only ticks in Execute(), which is paused during the freeze frame,
        // so the freeze duration does not eat into the dash movement time.
        dashTimer = ctx.Controller.Data.dashDuration;
    }

    public void Execute()
    {
        dashTimer -= Time.deltaTime;

        // Dash-jump: jump during a dash preserves horizontal momentum (wavedash / dash-jump).
        if (ctx.Input.JumpPressed)
        {
            ctx.Input.ConsumeJump();
            bool grounded = ctx.Physics.IsGrounded;
            ctx.Controller.EndDash(preserveHorizontal: true);
            ctx.Controller.ApplyJumpForce();
            // On grounded dash-jump (wavedash), refill dash charge
            if (grounded)
                ctx.DashesRemaining = ctx.Controller.Data.maxAirDashes;
            ctx.ChangeState(ctx.AirborneState);
            return;
        }

        if (dashTimer <= 0f)
        {
            if (ctx.Physics.IsGrounded)
                ctx.ChangeState(ctx.GroundedState);
            else
                ctx.ChangeState(ctx.AirborneState);
        }
    }

    public void FixedExecute()
    {
        // Re-apply velocity every physics step to fight any residual forces.
        ctx.Controller.HoldDashVelocity(dashDirection);
    }

    public void Exit()
    {
        ctx.Controller.EndDash(preserveHorizontal: false);
    }

    // =========================================================
    // Helpers
    // =========================================================

    /// <summary>
    /// Snap a free-form analog input vector to the nearest of the 8 cardinal/diagonal directions.
    /// </summary>
    private static Vector2 SnapToEightDir(Vector2 input)
    {
        float angle = Mathf.Atan2(input.y, input.x);
        float snapped = Mathf.Round(angle / (Mathf.PI * 0.25f)) * (Mathf.PI * 0.25f);
        return new Vector2(Mathf.Cos(snapped), Mathf.Sin(snapped));
    }
}
