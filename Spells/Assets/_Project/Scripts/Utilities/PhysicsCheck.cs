using UnityEngine;

public class PhysicsCheck : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.4f, 0.04f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Check")]
    [SerializeField] private float wallCheckDistance = 0.55f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Ceiling Check")]
    [SerializeField] private Vector2 ceilingCheckOffset = new Vector2(0f, 0.5f);
    [SerializeField] private Vector2 ceilingCheckSize = new Vector2(0.4f, 0.04f);

    [Header("Slope Detection")]
    [SerializeField] private float groundRayDistance = 0.7f;

    public bool IsGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public int WallDirection { get; private set; }
    public bool IsOnCeiling { get; private set; }

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

    private void Awake()
    {
        // Auto-detect layers if not assigned in inspector
        if (groundLayer == 0)
        {
            int ground = LayerMask.NameToLayer("Ground");
            int wall = LayerMask.NameToLayer("Wall");
            int platform = LayerMask.NameToLayer("Platform");

            if (ground >= 0) groundLayer |= (1 << ground);
            if (wall >= 0) groundLayer |= (1 << wall);
            if (platform >= 0) groundLayer |= (1 << platform);

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
        IsGrounded = Physics2D.OverlapBox(origin, groundCheckSize, 0f, groundLayer) != null;

        // Raycast downward to get the ground surface normal for slope detection
        if (IsGrounded)
        {
            Vector2 rayOrigin = (Vector2)transform.position;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundRayDistance, groundLayer);

            if (hit.collider != null)
            {
                GroundNormal = hit.normal;
                GroundAngle = Vector2.Angle(Vector2.up, GroundNormal);
                IsOnSlope = GroundAngle > 1f;
            }
            else
            {
                // Fallback: grounded but ray missed (edge case), assume flat
                GroundNormal = Vector2.up;
                GroundAngle = 0f;
                IsOnSlope = false;
            }
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

        // Ground normal ray
        Gizmos.color = Color.cyan;
        Vector2 rayOrigin = (Vector2)transform.position;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * groundRayDistance);
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
