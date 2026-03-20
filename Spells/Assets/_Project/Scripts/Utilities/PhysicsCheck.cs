using UnityEngine;

public class PhysicsCheck : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.25f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Check")]
    [SerializeField] private float wallCheckDistance = 0.55f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Ceiling Check")]
    [SerializeField] private Vector2 ceilingCheckOffset = new Vector2(0f, 0.5f);
    [SerializeField] private Vector2 ceilingCheckSize = new Vector2(0.4f, 0.04f);

    [Header("Slope Detection")]
    [SerializeField] private float groundRayDistance = 0.9f;

    public bool IsGrounded { get; private set; }
    /// <summary>True when standing on top of another player (separate from terrain grounding).</summary>
    public bool IsOnPlayerHead { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public int WallDirection { get; private set; }
    public bool IsOnCeiling { get; private set; }

    /// <summary>The layer mask used for ground/platform detection. Exposed for corner correction.</summary>
    public LayerMask GroundLayerMask => groundLayer;

    /// <summary>
    /// Clears the grounded frame buffer so IsGrounded becomes false immediately.
    /// Call this when jumping to prevent the buffer from keeping the player grounded.
    /// </summary>
    public void ClearGroundBuffer()
    {
        groundedFrameBuffer = 0;
        IsGrounded = false;
    }

    /// <summary>
    /// The normal of the ground surface directly below the player.
    /// (0,1) on flat ground, angled on slopes. Zero if not grounded.
    /// </summary>
    public Vector2 GroundNormal { get; private set; }

    /// <summary>
    /// The angle of the ground in degrees (0 = flat, positive = sloped).
    /// </summary>
    public float GroundAngle { get; private set; }

    /// <summary>
    /// True if the player is standing on a slope (angle > 1 degree).
    /// </summary>
    public bool IsOnSlope { get; private set; }

    // Grounded frame buffer: prevents flickering on slopes and uneven surfaces.
    // Stays grounded for a few physics frames after losing contact.
    private int groundedFrameBuffer;
    private const int GROUND_BUFFER_FRAMES = 3; // ~0.06s at 50Hz physics

    // Separate layer mask for player-on-player head detection
    private int playerOnlyLayerMask;

    private void Awake()
    {
        // Auto-detect layers if not assigned in inspector
        if (groundLayer == 0)
        {
            int ground = LayerMask.NameToLayer("Ground");
            int wall = LayerMask.NameToLayer("Wall");
            int platform = LayerMask.NameToLayer("Platform");
            int player = LayerMask.NameToLayer("Player");

            if (ground >= 0) groundLayer |= (1 << ground);
            if (wall >= 0) groundLayer |= (1 << wall);
            if (platform >= 0) groundLayer |= (1 << platform);
            // Player layer NOT in groundLayer — self-detection made IsGrounded always true.
            // Player-on-player head detection is handled separately via IsOnPlayerHead.

            if (player >= 0) playerOnlyLayerMask = (1 << player);

            if (groundLayer == 0)
            {
                Debug.LogWarning("PhysicsCheck: No Ground/Wall/Platform layers found! Ground detection will not work.");
            }
        }

        if (wallLayer == 0)
        {
            // Wall slide should ONLY work on Wall layer, not platforms/ground edges
            int wall = LayerMask.NameToLayer("Wall");

            if (wall >= 0) wallLayer |= (1 << wall);

            if (wallLayer == 0)
            {
                Debug.LogWarning("PhysicsCheck: No Wall layer found! Wall slide detection will not work.");
            }
        }

    }

    private void FixedUpdate()
    {
        CheckGround();
        CheckWalls();
        CheckCeiling();
    }

    private void CheckGround()
    {
        Vector2 origin = (Vector2)transform.position + groundCheckOffset;
        bool boxHit = Physics2D.OverlapBox(origin, groundCheckSize, 0f, groundLayer) != null;

        // Primary raycast straight down
        Vector2 rayOrigin = (Vector2)transform.position;
        float rayGroundThreshold = Mathf.Abs(groundCheckOffset.y) + groundCheckSize.y * 0.5f + 0.20f;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundRayDistance, groundLayer);
        bool rayHit = hit.collider != null && hit.distance <= rayGroundThreshold;

        // Angled raycasts to catch slopes the vertical ray misses.
        if (!rayHit)
        {
            Vector2 leftAngle = new Vector2(-0.36f, -1f).normalized;   // ~20° left
            Vector2 rightAngle = new Vector2(0.36f, -1f).normalized;   // ~20° right
            var leftRay = Physics2D.Raycast(rayOrigin, leftAngle, groundRayDistance, groundLayer);
            var rightRay = Physics2D.Raycast(rayOrigin, rightAngle, groundRayDistance, groundLayer);

            if (leftRay.collider != null && leftRay.distance <= rayGroundThreshold)
            {
                rayHit = true;
                hit = leftRay;
            }
            else if (rightRay.collider != null && rightRay.distance <= rayGroundThreshold)
            {
                rayHit = true;
                hit = rightRay;
            }
        }

        bool directContact = boxHit || rayHit;

        // Grounded frame buffer: stay grounded for a few frames after losing contact.
        if (directContact)
        {
            groundedFrameBuffer = GROUND_BUFFER_FRAMES;
        }
        else if (groundedFrameBuffer > 0)
        {
            groundedFrameBuffer--;
        }

        IsGrounded = groundedFrameBuffer > 0;

        // Separate player-on-player head check: overlap box on Player layer only.
        // Uses OverlapBoxAll to avoid self-collider shadowing the other player.
        IsOnPlayerHead = false;
        if (!IsGrounded && playerOnlyLayerMask != 0)
        {
            var hits = Physics2D.OverlapBoxAll(origin, groundCheckSize, 0f, playerOnlyLayerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject != gameObject)
                {
                    IsOnPlayerHead = true;
                    break;
                }
            }
        }

        // Get ground normal for slope detection
        if (IsGrounded && hit.collider != null)
        {
            GroundNormal = hit.normal;
            GroundAngle = Vector2.Angle(Vector2.up, GroundNormal);
            IsOnSlope = GroundAngle > 1f;
        }
        else if (IsGrounded)
        {
            GroundNormal = Vector2.up;
            GroundAngle = 0f;
            IsOnSlope = false;
        }
        else
        {
            GroundNormal = Vector2.zero;
            GroundAngle = 0f;
            IsOnSlope = false;
        }
    }

    private void CheckWalls()
    {
        Vector2 origin = transform.position;

        var rightHit = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer);
        var leftHit = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer);

        if (rightHit.collider != null)
        {
            IsTouchingWall = true;
            WallDirection = 1;
        }
        else if (leftHit.collider != null)
        {
            IsTouchingWall = true;
            WallDirection = -1;
        }
        else
        {
            IsTouchingWall = false;
            WallDirection = 0;
        }
    }

    private void CheckCeiling()
    {
        Vector2 origin = (Vector2)transform.position + ceilingCheckOffset;
        IsOnCeiling = Physics2D.OverlapBox(origin, ceilingCheckSize, 0f, groundLayer) != null;
    }

    private void OnDrawGizmosSelected()
    {
        // Ground check
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Vector2 groundOrigin = (Vector2)transform.position + groundCheckOffset;
        Gizmos.DrawWireCube(groundOrigin, groundCheckSize);

        // Ground normal rays (center + angled for slope detection)
        Gizmos.color = Color.cyan;
        Vector2 rayOrigin = (Vector2)transform.position;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * groundRayDistance);
        Vector2 leftAngle = new Vector2(-0.36f, -1f).normalized;
        Vector2 rightAngle = new Vector2(0.36f, -1f).normalized;
        Gizmos.color = new Color(0f, 0.8f, 0.8f, 0.5f);
        Gizmos.DrawLine(rayOrigin, rayOrigin + leftAngle * groundRayDistance);
        Gizmos.DrawLine(rayOrigin, rayOrigin + rightAngle * groundRayDistance);
        if (IsGrounded && GroundNormal != Vector2.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rayOrigin, rayOrigin + GroundNormal * 0.5f);
        }

        // Wall checks
        Gizmos.color = IsTouchingWall ? Color.green : Color.red;
        Vector2 origin = transform.position;
        Gizmos.DrawLine(origin, origin + Vector2.right * wallCheckDistance);
        Gizmos.DrawLine(origin, origin + Vector2.left * wallCheckDistance);

        // Ceiling check
        Gizmos.color = IsOnCeiling ? Color.green : Color.red;
        Vector2 ceilingOrigin = (Vector2)transform.position + ceilingCheckOffset;
        Gizmos.DrawWireCube(ceilingOrigin, ceilingCheckSize);
    }
}
