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
    public WallSlidingState WallSlideState { get; private set; }
    public HitstunState HitstunState { get; private set; }
    public SurfaceTraversalState SurfaceTraversalState { get; private set; }
    public DashState DashState { get; private set; }

    /// <summary>
    /// Set by SpiderShoesItem. When true, wall/ceiling contact can trigger SurfaceTraversalState.
    /// </summary>
    public bool HasSpiderShoes { get; set; }

    // Celeste dash / wall stamina
    public int DashesRemaining { get; set; }
    public float WallStamina { get; set; }

    // Shared timers accessible by states
    public float JumpBufferTimer { get; set; }
    public float CoyoteTimer { get; set; }
    public float WallJumpLockoutTimer { get; set; }

    // Freeze frame: pauses state execution for a short duration (e.g. at dash start)
    private float freezeFrameTimer;

    // Prevents the jump buffer from being refreshed every frame while JumpPressed lingers
    private bool jumpAlreadyBuffered;

    /// <summary>
    /// Velocity captured each FixedUpdate while airborne, BEFORE collision resolution.
    /// Used by wave-land so landing doesn't eat all the momentum before we can use it.
    /// </summary>
    public Vector2 PreLandingVelocity { get; set; }

    /// <summary>True while the player's class ability is active (e.g. shield). Blocks jumping.</summary>
    public bool IsAbilityActive => classAbility != null && classAbility.IsActive;

    private ClassAbility classAbility;
    private bool initialized = false;

    private void Awake()
    {
        GroundedState = new GroundedState();
        AirborneState = new AirborneState();
        WallSlideState = new WallSlidingState();
        HitstunState = new HitstunState();
        SurfaceTraversalState = new SurfaceTraversalState();
        DashState = new DashState();
    }

    private void Start()
    {
        // Try interface lookup first, fall back to concrete type
        Input = GetComponent<IInputProvider>();
        if (Input == null)
            Input = GetComponent<PlayerInputHandler>();

        Controller    = GetComponent<PlayerController>();
        Physics       = GetComponent<PhysicsCheck>();
        classAbility  = GetComponent<ClassAbility>();

        if (Input == null) Debug.LogError("PlayerStateMachine: No IInputProvider found! Ensure PlayerInputHandler is on this GameObject.", this);
        if (Controller == null) Debug.LogError("PlayerStateMachine: No PlayerController found!", this);
        if (Physics == null) Debug.LogError("PlayerStateMachine: No PhysicsCheck found!", this);

        if (Input != null && Controller != null && Controller.Data != null && Physics != null)
        {
            DashesRemaining = Controller.Data.maxAirDashes;
            WallStamina = Controller.Data.wallStaminaMax;
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

        // Freeze frame: pause all state execution for a short duration (hitstop at dash start)
        if (freezeFrameTimer > 0f)
        {
            freezeFrameTimer -= Time.unscaledDeltaTime;
            return;
        }

        // Tick shared timers
        if (JumpBufferTimer > 0f) JumpBufferTimer -= Time.deltaTime;
        if (CoyoteTimer > 0f) CoyoteTimer -= Time.deltaTime;
        if (WallJumpLockoutTimer > 0f) WallJumpLockoutTimer -= Time.deltaTime;

        // Track jump buffer: only on the actual frame the button goes down.
        // We detect a fresh press by checking JumpPressed before the state
        // consumes it. To avoid re-buffering a stale (already-consumed) press
        // we track whether we already buffered this press cycle.
        if (Input.JumpPressed && !jumpAlreadyBuffered)
        {
            JumpBufferTimer = Controller.Data.jumpBufferDuration;
            jumpAlreadyBuffered = true;
        }
        else if (!Input.JumpPressed)
        {
            jumpAlreadyBuffered = false;
        }

        CurrentState?.Execute();
    }

    private void FixedUpdate()
    {
        if (!initialized || Input == null) return;
        if (freezeFrameTimer > 0f) return; // Paused during freeze frame

        CurrentState?.FixedExecute();
    }

    /// <summary>
    /// Pause state execution for <paramref name="duration"/> seconds (unscaled).
    /// Used for the 2-frame hitstop at dash start.
    /// </summary>
    public void StartFreezeFrame(float duration)
    {
        freezeFrameTimer = duration;
    }

    /// <summary>
    /// Enter hitstun state. Called by HealthSystem when damage is taken.
    /// </summary>
    public void EnterHitstun(float duration)
    {
        if (!initialized) return;
        HitstunState.SetDuration(duration);
        ChangeState(HitstunState);
    }

    public string GetStateName()
    {
        if (CurrentState == null) return "None";
        return CurrentState.GetType().Name.Replace("State", "");
    }
}
