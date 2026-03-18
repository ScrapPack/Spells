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

    // Dash inputs
    public bool DashPressed { get; private set; }
    public bool DashHeld { get; private set; }

    // Special move input
    public bool SpecialPressed { get; private set; }

    // Combat inputs
    public bool ShootPressed { get; private set; }
    public bool ShootHeld { get; private set; }
    public bool ParryPressed { get; private set; }
    public Vector2 AimDirection { get; private set; }

    public void ConsumeJump()
    {
        JumpPressed = false;
    }

    public void ConsumeDash()
    {
        DashPressed = false;
    }

    public void ConsumeSpecial()
    {
        SpecialPressed = false;
    }

    public void ConsumeShoot()
    {
        ShootPressed = false;
    }

    public void ConsumeParry()
    {
        ParryPressed = false;
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

    // Called by PlayerInput via Send Messages when Shoot action fires
    public void OnShoot(InputValue value)
    {
        if (value.isPressed)
        {
            ShootPressed = true;
            ShootHeld = true;
        }
        else
        {
            ShootHeld = false;
        }
    }

    // Called by PlayerInput via Send Messages when Dash action fires
    public void OnDash(InputValue value)
    {
        if (value.isPressed)
        {
            DashPressed = true;
            DashHeld = true;
        }
        else
        {
            DashHeld = false;
        }
    }

    // Called by PlayerInput via Send Messages when Special action fires
    public void OnSpecial(InputValue value)
    {
        if (value.isPressed)
        {
            SpecialPressed = true;
        }
    }

    // Called by PlayerInput via Send Messages when Parry action fires
    public void OnParry(InputValue value)
    {
        if (value.isPressed)
        {
            ParryPressed = true;
        }
    }

    // Called by PlayerInput via Send Messages when Aim action changes
    public void OnAim(InputValue value)
    {
        AimDirection = value.Get<Vector2>();
    }

    private void Update()
    {
        // Derive crouch from move input every frame (redundant safety — OnMove also sets this)
        CrouchHeld = MoveInput.y < -0.5f;
    }
}
