using UnityEngine;

/// <summary>
/// Oscillating platform that moves along an axis using sin wave motion.
/// Used by volcanic/citadel biomes for dynamic platforming challenges.
///
/// Uses Rigidbody2D.MovePosition for smooth physics interaction —
/// kinematic bodies that move via MovePosition properly push players
/// standing on them (unlike direct transform manipulation).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Tooltip("Direction of oscillation (normalized internally)")]
    public Vector2 moveDirection = Vector2.up;

    [Tooltip("Distance from center to peak of oscillation")]
    [Range(0.5f, 5f)] public float amplitude = 2f;

    [Tooltip("Oscillation speed (higher = faster)")]
    [Range(0.1f, 3f)] public float speed = 1f;

    [Tooltip("Phase offset (0-1) to desync multiple platforms")]
    [Range(0f, 1f)] public float phaseOffset = 0f;

    private Vector2 startPos;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        startPos = transform.position;
    }

    private void FixedUpdate()
    {
        float t = Time.time * speed + phaseOffset * Mathf.PI * 2f;
        float offset = Mathf.Sin(t) * amplitude;
        Vector2 targetPos = startPos + moveDirection.normalized * offset;
        rb.MovePosition(targetPos);
    }
}
