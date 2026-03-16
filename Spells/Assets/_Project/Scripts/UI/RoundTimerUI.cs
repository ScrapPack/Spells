using UnityEngine;

/// <summary>
/// Displays the round timer and zoom progress indicator.
/// Shows time elapsed, and when zoom compression begins,
/// shows a visual warning that the arena is shrinking.
///
/// Rendered with OnGUI (consistent with other UI).
/// </summary>
public class RoundTimerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundManager roundManager;

    [Header("Display")]
    [SerializeField] private float zoomWarningThreshold = 0.3f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.6f, 0f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.2f, 1f);

    private GUIStyle timerStyle;
    private GUIStyle zoomBarStyle;
    private GUIStyle zoomFillStyle;

    private void OnGUI()
    {
        if (roundManager == null || !roundManager.RoundActive) return;

        InitStyles();

        float timer = roundManager.RoundTimer;
        float zoom = roundManager.ZoomProgress;

        // Timer display (top center)
        int minutes = (int)(timer / 60f);
        int seconds = (int)(timer % 60f);
        string timeText = $"{minutes}:{seconds:D2}";

        // Color based on zoom state
        if (zoom > 0.7f)
            timerStyle.normal.textColor = dangerColor;
        else if (zoom > zoomWarningThreshold)
            timerStyle.normal.textColor = warningColor;
        else
            timerStyle.normal.textColor = normalColor;

        Rect timerRect = new Rect(Screen.width * 0.5f - 40f, 10f, 80f, 30f);
        GUI.Label(timerRect, timeText, timerStyle);

        // Zoom progress bar (only shown after zoom begins)
        if (zoom > 0f)
        {
            float barWidth = 120f;
            float barHeight = 8f;
            float barX = Screen.width * 0.5f - barWidth * 0.5f;
            float barY = 42f;

            // Background
            GUI.Box(new Rect(barX, barY, barWidth, barHeight), "", zoomBarStyle);

            // Fill
            Color fillColor = Color.Lerp(warningColor, dangerColor, zoom);
            zoomFillStyle.normal.background = MakeTex(1, 1, fillColor);
            GUI.Box(new Rect(barX + 1, barY + 1, (barWidth - 2) * zoom, barHeight - 2), "", zoomFillStyle);

            // Pulse effect at high zoom
            if (zoom > 0.7f)
            {
                float pulse = Mathf.PingPong(Time.time * 3f, 1f);
                float alpha = Mathf.Lerp(0.3f, 1f, pulse);
                timerStyle.normal.textColor = new Color(dangerColor.r, dangerColor.g, dangerColor.b, alpha);
            }
        }

        // Alive player count
        string aliveText = $"Alive: {roundManager.AliveCount}";
        Rect aliveRect = new Rect(Screen.width * 0.5f - 30f, 54f, 60f, 20f);
        GUIStyle aliveStyle = new GUIStyle(timerStyle) { fontSize = 12 };
        aliveStyle.normal.textColor = normalColor;
        GUI.Label(aliveRect, aliveText, aliveStyle);
    }

    private void InitStyles()
    {
        if (timerStyle == null)
        {
            timerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (zoomBarStyle == null)
        {
            zoomBarStyle = new GUIStyle(GUI.skin.box);
            zoomBarStyle.normal.background = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f, 0.8f));
        }

        if (zoomFillStyle == null)
        {
            zoomFillStyle = new GUIStyle();
        }
    }

    private static Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = color;
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
}
