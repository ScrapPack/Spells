using UnityEngine;

/// <summary>
/// Movement state for spider shoes: walk on walls and ceilings.
/// Overrides gravity to stick to surfaces.
///
/// Wall: input.x controls vertical movement along the wall
/// Ceiling: input.x controls horizontal movement (inverted gravity feel)
/// Jump: detach from surface, push away from surface normal
/// Falls off if surface contact is lost → AirborneState
/// </summary>
public class SurfaceTraversalState : IPlayerState
{
    public enum Surface { Wall, Ceiling }

    private PlayerStateMachine ctx;
    private Surface currentSurface;
    private int wallDirection; // 1 = right wall, -1 = left wall

    public void Enter(PlayerStateMachine ctx)
    {
        this.ctx = ctx;

        // Disable gravity while traversing
        ctx.Controller.SetGravityScale(0f);
        ctx.Controller.Rb.linearVelocity = Vector2.zero;

        // Determine which surface we're on
        if (ctx.Physics.IsOnCeiling)
        {
            currentSurface = Surface.Ceiling;
        }
        else if (ctx.Physics.IsTouchingWall)
        {
            currentSurface = Surface.Wall;
            wallDirection = ctx.Physics.WallDirection;
        }
    }

    public void Execute()
    {
        // Jump: detach from surface
        if (ctx.Input.JumpPressed)
        {
            ctx.Input.ConsumeJump();
            Detach();
            return;
        }

        // Check if still on a surface
        if (currentSurface == Surface.Wall && !ctx.Physics.IsTouchingWall)
        {
            // Check if transitioned to ceiling
            if (ctx.Physics.IsOnCeiling)
            {
                currentSurface = Surface.Ceiling;
            }
            else
            {
                ctx.ChangeState(ctx.AirborneState);
                return;
            }
        }
        else if (currentSurface == Surface.Ceiling && !ctx.Physics.IsOnCeiling)
        {
            // Check if transitioned to wall
            if (ctx.Physics.IsTouchingWall)
            {
                currentSurface = Surface.Wall;
                wallDirection = ctx.Physics.WallDirection;
            }
            else
            {
                ctx.ChangeState(ctx.AirborneState);
                return;
            }
        }

        // Lost spider shoes while traversing
        if (!ctx.HasSpiderShoes)
        {
            ctx.ChangeState(ctx.AirborneState);
            return;
        }
    }

    public void FixedExecute()
    {
        float moveInput = ctx.Input.MoveInput.x;
        float speed = ctx.Controller.Data.moveSpeed * 0.8f; // Slightly slower on surfaces

        // Apply force toward surface to maintain contact
        float stickForce = 15f;

        switch (currentSurface)
        {
            case Surface.Wall:
                // On wall: input.x moves vertically
                ctx.Controller.Rb.linearVelocity = new Vector2(0f, moveInput * speed);

                // Push toward wall
                ctx.Controller.Rb.AddForce(new Vector2(wallDirection * stickForce, 0f));
                break;

            case Surface.Ceiling:
                // On ceiling: input.x moves horizontally (inverted feel)
                ctx.Controller.Rb.linearVelocity = new Vector2(moveInput * speed, 0f);

                // Push toward ceiling
                ctx.Controller.Rb.AddForce(new Vector2(0f, stickForce));
                break;
        }
    }

    public void Exit()
    {
        // Restore normal gravity
        ctx.Controller.SetGravityScale(ctx.Controller.Data.gravityScale);
    }

    private void Detach()
    {
        Vector2 pushDir;
        float pushForce = ctx.Controller.Data.jumpForce * 0.7f;

        switch (currentSurface)
        {
            case Surface.Wall:
                // Push away from wall
                pushDir = new Vector2(-wallDirection, 1f).normalized;
                break;
            case Surface.Ceiling:
                // Push down and away from ceiling
                pushDir = Vector2.down;
                break;
            default:
                pushDir = Vector2.up;
                break;
        }

        ctx.Controller.Rb.linearVelocity = pushDir * pushForce;
        ctx.ChangeState(ctx.AirborneState);
    }
}
