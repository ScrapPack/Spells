using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    public IInputProvider Input { get; private set; }
    public PlayerController Controller { get; private set; }
    public PhysicsCheck Physics { get; private set; }

    public IPlayerState CurrentState { get; private set; }

    // Pre-allocated states
    public GroundedState GroundedState { get; private set; }
    public AirborneState AirborneState { get; private set; }
    public WallSlideState WallSlideState { get; private set; }

    // Shared timers accessible by states
    public float JumpBufferTimer { get; set; }
    public float CoyoteTimer { get; set; }
    public float WallJumpLockoutTimer { get; set; }

    /// <summary>
    /// Velocity captured each FixedUpdate while airborne, BEFORE collision resolution.
    /// Used by wave-land so landing doesn't eat all the momentum before we can use it.
    /// </summary>
    public Vector2 PreLandingVelocity { get; set; }

    private bool initialized = false;

    private void Awake()
    {
        GroundedState = new GroundedState();
        AirborneState = new AirborneState();
        WallSlideState = new WallSlideState();
    }

    private void Start()
    {
        // Try interface lookup first, fall back to concrete type
        Input = GetComponent<IInputProvider>();
        if (Input == null)
            Input = GetComponent<PlayerInputHandler>();

        Controller = GetComponent<PlayerController>();
        Physics = GetComponent<PhysicsCheck>();

        if (Input == null) Debug.LogError("PlayerStateMachine: No IInputProvider found! Ensure PlayerInputHandler is on this GameObject.", this);
        if (Controller == null) Debug.LogError("PlayerStateMachine: No PlayerController found!", this);
        if (Physics == null) Debug.LogError("PlayerStateMachine: No PhysicsCheck found!", this);

        if (Input != null && Controller != null && Controller.Data != null && Physics != null)
        {
            ChangeState(AirborneState);
            initialized = true;
        }
        else
        {
            Debug.LogError("PlayerStateMachine: Missing dependencies — player will not function. Check that MovementData is assigned on PlayerController.", this);
        }
    }

    public void ChangeState(IPlayerState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter(this);
    }

    private void Update()
    {
        if (!initialized || Input == null) return;

        // Tick shared timers
        if (JumpBufferTimer > 0f) JumpBufferTimer -= Time.deltaTime;
        if (CoyoteTimer > 0f) CoyoteTimer -= Time.deltaTime;
        if (WallJumpLockoutTimer > 0f) WallJumpLockoutTimer -= Time.deltaTime;

        // Track jump buffer: if jump pressed, start the buffer timer
        if (Input.JumpPressed)
        {
            JumpBufferTimer = Controller.Data.jumpBufferDuration;
        }

        CurrentState?.Execute();
    }

    private void FixedUpdate()
    {
        if (!initialized || Input == null) return;

        CurrentState?.FixedExecute();
    }

    public string GetStateName()
    {
        if (CurrentState == null) return "None";
        return CurrentState.GetType().Name.Replace("State", "");
    }
}
