using UnityEngine;

public interface IInputProvider
{
    Vector2 MoveInput { get; }
    bool JumpPressed { get; }
    bool JumpHeld { get; }
    bool CrouchHeld { get; }
    void ConsumeJump();

    // Combat inputs
    bool ShootPressed { get; }
    bool ShootHeld { get; }
    bool ParryPressed { get; }
    Vector2 AimDirection { get; }
    void ConsumeShoot();
    void ConsumeParry();
}
