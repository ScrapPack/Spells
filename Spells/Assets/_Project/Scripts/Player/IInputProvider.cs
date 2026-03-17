using UnityEngine;

public interface IInputProvider
{
    Vector2 MoveInput { get; }
    bool JumpPressed { get; }
    bool JumpHeld { get; }
    bool CrouchHeld { get; }
    void ConsumeJump();

    // Dash input
    bool DashPressed { get; }
    bool DashHeld { get; }
    void ConsumeDash();

    // Combat inputs
    bool ShootPressed { get; }
    bool ShootHeld { get; }
    bool ParryPressed { get; }
    Vector2 AimDirection { get; }
    void ConsumeShoot();
    void ConsumeParry();
}
