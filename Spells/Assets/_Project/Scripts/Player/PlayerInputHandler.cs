using Rewired;
using UnityEngine;

/// <summary>
/// Implements IInputProvider by polling a Rewired Player each frame.
/// The Rewired Player index is taken from PlayerIdentity.PlayerID so
/// each player automatically reads the correct device assignment.
///
/// Action names must match those defined in the Rewired Input Manager asset:
///   "Move Horizontal", "Move Vertical"
///   "Jump", "Dash", "Shoot", "Parry", "Special"
///   "Aim Horizontal", "Aim Vertical"
/// </summary>
public class PlayerInputHandler : MonoBehaviour, IInputProvider
{
    // ── Rewired action name constants ─────────────────────────────────────────
    private const string kMoveH   = "Move Horizontal";
    private const string kMoveV   = "Move Vertical";
    private const string kJump    = "Jump";
    private const string kDash    = "Dash";
    private const string kShoot   = "Shoot";
    private const string kParry   = "Parry";
    private const string kSpecial = "Special";
    private const string kAimH    = "Aim Horizontal";
    private const string kAimV    = "Aim Vertical";

    // ── IInputProvider ────────────────────────────────────────────────────────
    public Vector2 MoveInput    { get; private set; }
    public bool    JumpPressed  { get; private set; }
    public bool    JumpHeld     { get; private set; }
    public bool    CrouchHeld   { get; private set; }
    public bool    DashPressed  { get; private set; }
    public bool    DashHeld     { get; private set; }
    public bool    SpecialPressed { get; private set; }
    public bool    ShootPressed { get; private set; }
    public bool    ShootHeld    { get; private set; }
    public bool    ParryPressed { get; private set; }
    public Vector2 AimDirection { get; private set; }

    // ── Consume methods ───────────────────────────────────────────────────────
    public void ConsumeJump()    => JumpPressed    = false;
    public void ConsumeDash()    => DashPressed    = false;
    public void ConsumeSpecial() => SpecialPressed = false;
    public void ConsumeShoot()   => ShootPressed   = false;
    public void ConsumeParry()   => ParryPressed   = false;

    /// <summary>Reset all flags to neutral. Call after death/respawn.</summary>
    public void ClearInputState()
    {
        MoveInput     = Vector2.zero;
        JumpPressed   = false;
        JumpHeld      = false;
        CrouchHeld    = false;
        DashPressed   = false;
        DashHeld      = false;
        SpecialPressed = false;
        ShootPressed  = false;
        ShootHeld     = false;
        ParryPressed  = false;
        AimDirection  = Vector2.zero;
    }

    /// <summary>
    /// When false, all input reads as neutral/zero. Used to freeze players during UI screens.
    /// </summary>
    public bool InputEnabled { get; set; } = true;

    // ── Internal ──────────────────────────────────────────────────────────────
    private Rewired.Player rwPlayer;

    private void Start()
    {
        var identity = GetComponent<PlayerIdentity>();
        int id = identity != null ? identity.PlayerID : 0;
        rwPlayer = ReInput.players.GetPlayer(id);

        if (rwPlayer == null)
            Debug.LogError($"PlayerInputHandler: Rewired player {id} not found!", this);
    }

    private void Update()
    {
        if (rwPlayer == null) return;

        if (!InputEnabled)
        {
            ClearInputState();
            return;
        }

        // Movement
        MoveInput  = new Vector2(rwPlayer.GetAxis(kMoveH), rwPlayer.GetAxis(kMoveV));
        CrouchHeld = MoveInput.y < -0.5f;

        // Jump — set JumpPressed on the frame it goes down; JumpHeld tracks hold state
        if (rwPlayer.GetButtonDown(kJump)) JumpPressed = true;
        JumpHeld = rwPlayer.GetButton(kJump);

        // Dash
        if (rwPlayer.GetButtonDown(kDash)) DashPressed = true;
        DashHeld = rwPlayer.GetButton(kDash);

        // Shoot
        if (rwPlayer.GetButtonDown(kShoot)) ShootPressed = true;
        ShootHeld = rwPlayer.GetButton(kShoot);

        // Parry and Special (press-only; no held state needed)
        if (rwPlayer.GetButtonDown(kParry))   ParryPressed   = true;
        if (rwPlayer.GetButtonDown(kSpecial)) SpecialPressed = true;

        // Aim (gamepad right stick; mouse aim is handled separately in AimController)
        AimDirection = new Vector2(rwPlayer.GetAxis(kAimH), rwPlayer.GetAxis(kAimV));
    }
}
