using UnityEngine;

/// <summary>
/// Mock input provider for PlayMode tests.
/// Attach to a test player GameObject BEFORE PlayerStateMachine —
/// the state machine finds IInputProvider via GetComponent in Start().
/// </summary>
public class TestInputProvider : MonoBehaviour, IInputProvider
{
    public Vector2 MoveInput { get; set; }
    public bool JumpPressed { get; set; }
    public bool JumpHeld { get; set; }
    public bool CrouchHeld { get; set; }
    public bool DashPressed { get; set; }
    public bool DashHeld { get; set; }
    public bool ShootPressed { get; set; }
    public bool ShootHeld { get; set; }
    public bool ParryPressed { get; set; }
    public Vector2 AimDirection { get; set; }

    public void ConsumeJump() => JumpPressed = false;
    public void ConsumeDash() => DashPressed = false;
    public void ConsumeShoot() => ShootPressed = false;
    public void ConsumeParry() => ParryPressed = false;

    /// <summary>Press jump for one frame (state machine will consume it).</summary>
    public void PressJump()
    {
        JumpPressed = true;
        JumpHeld = true;
    }

    public void ReleaseJump()
    {
        JumpPressed = false;
        JumpHeld = false;
    }

    public void SetMove(float x, float y = 0f)
    {
        MoveInput = new Vector2(x, y);
        CrouchHeld = y < -0.5f;
    }

    public void PressShoot()
    {
        ShootPressed = true;
        ShootHeld = true;
    }

    public void PressDash()
    {
        DashPressed = true;
        DashHeld = true;
    }

    public void ReleaseDash()
    {
        DashPressed = false;
        DashHeld = false;
    }

    public void Reset()
    {
        MoveInput = Vector2.zero;
        JumpPressed = false;
        JumpHeld = false;
        CrouchHeld = false;
        DashPressed = false;
        DashHeld = false;
        ShootPressed = false;
        ShootHeld = false;
        ParryPressed = false;
        AimDirection = Vector2.zero;
    }
}
