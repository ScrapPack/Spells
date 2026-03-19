using Rewired;
using UnityEngine;

/// <summary>
/// Processes raw aim input into a normalized direction and draws a LineRenderer indicator.
/// Priority: gamepad right stick → mouse (if this Rewired player has a mouse) → facing direction.
/// Other systems (ProjectileSpawner) read AimDirection from here instead of raw input.
/// </summary>
public class AimController : MonoBehaviour
{
    [Header("Indicator")]
    [SerializeField] private float lineLength = 1.5f;
    [SerializeField] private float lineWidth  = 0.03f;
    [SerializeField] private Color lineColor  = Color.red;

    /// <summary>Processed aim direction (normalized). Never zero.</summary>
    public Vector2 AimDirection { get; private set; } = Vector2.right;

    private IInputProvider   input;
    private PlayerController playerController;
    private BoxCollider2D    col;
    private LineRenderer     line;
    private Rewired.Player   rwPlayer;
    private bool             usingStick;        // true while gamepad stick is active
    private Vector2          lastMousePos;       // track mouse movement

    private void Start()
    {
        input            = GetComponent<IInputProvider>();
        playerController = GetComponent<PlayerController>();
        col              = GetComponent<BoxCollider2D>();

        var identity = GetComponent<PlayerIdentity>();
        int id = identity != null ? identity.PlayerID : 0;
        rwPlayer = ReInput.players.GetPlayer(id);

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

        line.material   = new Material(Shader.Find("Sprites/Default"));
        line.startColor = lineColor;
        line.endColor   = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);
    }

    private void Update()
    {
        UpdateAimDirection();
        UpdateIndicator();
    }

    private void UpdateAimDirection()
    {
        // 1. Gamepad right stick — comes through Rewired aim axes via IInputProvider
        //    Dead zone: sqrMagnitude > 0.09 = magnitude > 0.3 (30%).
        //    Prevents brief opposite-direction reads when flicking the stick hard.
        Vector2 raw = input?.AimDirection ?? Vector2.zero;
        if (raw.sqrMagnitude > 0.09f)
        {
            AimDirection = raw.normalized;
            usingStick = true;
            return;
        }

        // 2. Mouse aim — only switch from stick to mouse when the mouse actually moves.
        //    Prevents aim snapping to mouse cursor when the stick enters dead zone.
        if (rwPlayer != null && rwPlayer.controllers.hasMouse)
        {
            Vector2 currentMousePos = (Vector2)Input.mousePosition;
            bool mouseMoved = (currentMousePos - lastMousePos).sqrMagnitude > 4f;
            lastMousePos = currentMousePos;

            // Only use mouse if: we weren't using stick, or the mouse actually moved
            if (!usingStick || mouseMoved)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 worldPos = cam.ScreenToWorldPoint(
                        new Vector3(currentMousePos.x, currentMousePos.y, 0f));
                    Vector2 dir = (Vector2)worldPos - (Vector2)transform.position;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        AimDirection = dir.normalized;
                        usingStick = false;
                        return;
                    }
                }
            }

            // Stick was active but now in dead zone — keep last aim direction
            if (usingStick)
                return;
        }

        // 3. Fallback: player facing direction
        if (playerController != null)
            AimDirection = new Vector2(playerController.FacingDirection, 0f);
    }

    private void UpdateIndicator()
    {
        if (line == null) return;

        Vector2 center = col != null
            ? (Vector2)col.bounds.center
            : (Vector2)transform.position;

        // Project AimDirection onto collider half-extents to find the edge distance
        float offset = 0f;
        if (col != null)
        {
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
