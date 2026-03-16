using UnityEngine;

/// <summary>
/// Brief state entered when the player takes damage.
/// During hitstun: no movement input, no jumping, no attacking.
/// Transitions to AirborneState or GroundedState when hitstun expires.
/// Duration comes from the projectile's hitstun value (CombatData).
/// </summary>
public class HitstunState : IPlayerState
{
    private PlayerStateMachine ctx;
    private float stunTimer;
    private float stunDuration;

    /// <summary>
    /// Set the stun duration before entering this state.
    /// </summary>
    public void SetDuration(float duration)
    {
        stunDuration = duration;
    }

    public void Enter(PlayerStateMachine ctx)
    {
        this.ctx = ctx;
        stunTimer = stunDuration;

        // Cancel any ongoing actions
        ctx.Controller.EndWaveLand();
        ctx.Controller.ResetDashBurst();
    }

    public void Execute()
    {
        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0f)
        {
            // Transition based on current physical state
            if (ctx.Physics.IsGrounded)
                ctx.ChangeState(ctx.GroundedState);
            else
                ctx.ChangeState(ctx.AirborneState);
        }
    }

    public void FixedExecute()
    {
        // During hitstun: only gravity, no player control
        ctx.Controller.ApplyFallGravity(false);
        ctx.Controller.ClampFallSpeed();
    }

    public void Exit()
    {
        ctx.Controller.SetGravityScale(ctx.Controller.Data.gravityScale);
    }
}
