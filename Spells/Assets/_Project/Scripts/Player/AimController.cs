using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Processes raw aim input (right stick or mouse) into a normalized direction.
/// Draws a red LineRenderer indicator showing the current aim direction.
/// Other systems (ProjectileSpawner) read AimDirection instead of raw input.
///
/// Mouse aim reads Mouse.current directly rather than going through the Input System's
/// per-player Aim action, because keyboard control schemes don't pair the Mouse device
/// and the binding never fires.
/// </summary>
public class AimController : MonoBehaviour
{
    [Header("Indicator")]
    [SerializeField] private float lineLength = 1.5f;
    [SerializeField] private float lineWidth  = 0.03f;
    [SerializeField] private Color lineColor  = Color.red;

    /// <summary>Processed aim direction (normalized). Never zero.</summary>
    public Vector2 AimDirection { get; private set; } = Vector2.right;

    private IInputProvider input;
    private PlayerController playerController;
    private BoxCollider2D col;
    private LineRenderer line;
    private bool isKeyboardPlayer;

    private void Start()
    {
        input            = GetComponent<IInputProvider>();
        playerController = GetComponent<PlayerController>();
        col              = GetComponent<BoxCollider2D>();

        var playerInput  = GetComponent<PlayerInput>();
        isKeyboardPlayer = playerInput != null && playerInput.currentControlScheme == "KeyboardWASD";

        CreateLineRenderer();
    }

    private void CreateLineRenderer()
    {
        var indicatorGo = new GameObject("AimIndicator");
        indicatorGo.transform.SetParent(transform, false);

        line = indicatorGo.AddComponent<LineRenderer>();
        line.positionCount    = 2;
        line.startWidth       = lineWidth;
        line.endWidth         = lineWidth;
        line.useWorldSpace    = true;
        line.sortingLayerName = "Players";
        line.sortingOrder     = -1;

        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = lineColor;
        line.endColor   = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);
    }

    private void Update()
    {
        if (input == null) return;

        UpdateAimDirection();
        UpdateIndicator();
    }

    private void UpdateAimDirection()
    {
        // Gamepad right stick — comes through Input System (paired device)
        Vector2 raw = input.AimDirection;
        if (raw.sqrMagnitude > 0.01f && raw.sqrMagnitude <= 1.5f)
        {
            AimDirection = raw.normalized;
            return;
        }

        // Mouse aim — read Mouse.current directly because keyboard control schemes
        // don't include Mouse in their device list, so the Aim action binding never fires.
        if (isKeyboardPlayer && Mouse.current != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));
                Vector2 dir = (Vector2)worldPos - (Vector2)transform.position;
                if (dir.sqrMagnitude > 0.01f)
                {
                    AimDirection = dir.normalized;
                    return;
                }
            }
        }

        // Fallback: facing direction
        if (playerController != null)
            AimDirection = new Vector2(playerController.FacingDirection, 0f);
    }

    private void UpdateIndicator()
    {
        if (line == null) return;

        // Start the line at the outer edge of the player's collider
        // so the player body covers the line behind them.
        Vector2 center = col != null
            ? (Vector2)col.bounds.center
            : (Vector2)transform.position;

        float offset = 0f;
        if (col != null)
        {
            // Project aim direction onto collider half-extents to find edge distance
            Vector2 halfExt = col.bounds.extents;
            float ax = Mathf.Abs(AimDirection.x);
            float ay = Mathf.Abs(AimDirection.y);
            if (ax > 0.001f || ay > 0.001f)
                offset = Mathf.Min(
                    ax > 0.001f ? halfExt.x / ax : float.MaxValue,
                    ay > 0.001f ? halfExt.y / ay : float.MaxValue);
        }

        Vector2 origin = center + AimDirection * offset;
        line.SetPosition(0, origin);
        line.SetPosition(1, origin + AimDirection * lineLength);
    }
}
