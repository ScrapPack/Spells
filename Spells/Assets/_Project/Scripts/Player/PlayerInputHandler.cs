using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads input from Unity's PlayerInput component using Send Messages mode.
/// Implements IInputProvider so the state machine is decoupled from the Input System.
/// PlayerInput Behavior must be set to "Send Messages" (the default).
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour, IInputProvider
{
    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool CrouchHeld { get; private set; }

    public void ConsumeJump()
    {
        JumpPressed = false;
    }

    // Called by PlayerInput via Send Messages when Move action value changes
    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
        // Update crouch immediately on input change too (not just in Update)
        CrouchHeld = MoveInput.y < -0.5f;
    }

    // Called by PlayerInput via Send Messages when Jump action fires
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            JumpPressed = true;
            JumpHeld = true;
        }
        else
        {
            JumpHeld = false;
        }
    }

    private void Update()
    {
        // Derive crouch from move input every frame (redundant safety — OnMove also sets this)
        CrouchHeld = MoveInput.y < -0.5f;
    }
}
