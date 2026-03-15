using UnityEngine;

public interface IInputProvider
{
    Vector2 MoveInput { get; }
    bool JumpPressed { get; }
    bool JumpHeld { get; }
    bool CrouchHeld { get; }
    void ConsumeJump();
}
