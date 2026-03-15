using UnityEngine;
using UnityEngine.InputSystem;

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private bool showDebug = true;

    private PlayerStateMachine stateMachine;
    private PlayerController controller;
    private PhysicsCheck physicsCheck;

    private GUIStyle labelStyle;
    private GUIStyle bgStyle;

    private void Awake()
    {
        stateMachine = GetComponent<PlayerStateMachine>();
        controller = GetComponent<PlayerController>();
        physicsCheck = GetComponent<PhysicsCheck>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            showDebug = !showDebug;
    }

    private void OnGUI()
    {
        if (!showDebug || stateMachine == null || controller == null) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            bgStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(1, 1, new Color(0, 0, 0, 0.7f)) }
            };
        }

        // Position debug window based on player index in hierarchy
        int playerIndex = transform.GetSiblingIndex();
        float x = 10 + playerIndex * 220;
        float y = 10;
        float w = 210;
        float h = 200;

        GUI.Box(new Rect(x, y, w, h), "", bgStyle);

        float lineHeight = 18f;
        float cx = x + 5;
        float cy = y + 5;

        GUI.Label(new Rect(cx, cy, w, lineHeight), $"Player {playerIndex + 1}", labelStyle);
        cy += lineHeight;

        GUI.Label(new Rect(cx, cy, w, lineHeight), $"State: {stateMachine.GetStateName()}", labelStyle);
        cy += lineHeight;

        var vel = controller.Rb.linearVelocity;
        GUI.Label(new Rect(cx, cy, w, lineHeight), $"Vel: ({vel.x:F1}, {vel.y:F1}) spd:{vel.magnitude:F1}", labelStyle);
        cy += lineHeight;

        GUI.Label(new Rect(cx, cy, w, lineHeight), $"Grounded: {physicsCheck.IsGrounded}", labelStyle);
        cy += lineHeight;

        GUI.Label(new Rect(cx, cy, w, lineHeight), $"Slope: {physicsCheck.IsOnSlope} ({physicsCheck.GroundAngle:F1}°)", labelStyle);
        cy += lineHeight;

        GUI.Label(new Rect(cx, cy, w, lineHeight), $"WaveLand: {controller.IsWaveLanding}", labelStyle);
        cy += lineHeight;

        GUI.Label(new Rect(cx, cy, w, lineHeight), $"Wall: {physicsCheck.IsTouchingWall} (dir: {physicsCheck.WallDirection})", labelStyle);
        cy += lineHeight;

        GUI.Label(new Rect(cx, cy, w, lineHeight), $"Coyote: {stateMachine.CoyoteTimer:F3}", labelStyle);
        cy += lineHeight;

        GUI.Label(new Rect(cx, cy, w, lineHeight), $"JumpBuf: {stateMachine.JumpBufferTimer:F3}", labelStyle);
        cy += lineHeight;

        GUI.Label(new Rect(cx, cy, w, lineHeight), $"WallLock: {stateMachine.WallJumpLockoutTimer:F3}", labelStyle);
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        var tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
