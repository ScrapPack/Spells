using UnityEngine;

/// <summary>
/// Full-screen announcements: "ROUND 1", "FIGHT!", "Player X Wins!", "MATCH OVER!".
/// Call Announce/AnnounceRound/AnnounceRoundWin/AnnounceMatchWin directly.
/// Rendered with OnGUI for now — can be replaced with Canvas animation later.
/// </summary>
public class RoundAnnouncer : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float announceDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private string currentText = "";
    private Color currentColor = Color.white;
    private float announceTimer;
    private GUIStyle style;
    private GUIStyle shadowStyle;

    /// <summary>
    /// Show an announcement on screen.
    /// </summary>
    public void Announce(string text, Color color, float duration = 0f)
    {
        currentText = text;
        currentColor = color;
        announceTimer = duration > 0f ? duration : announceDuration;
    }

    /// <summary>
    /// Announce round start.
    /// </summary>
    public void AnnounceRound(int roundNumber)
    {
        Announce($"ROUND {roundNumber}", Color.white, 1.5f);
    }

    /// <summary>
    /// Announce round winner.
    /// </summary>
    public void AnnounceRoundWin(string winnerName, Color winnerColor)
    {
        Announce($"{winnerName} WINS!", winnerColor);
    }

    /// <summary>
    /// Announce match winner.
    /// </summary>
    public void AnnounceMatchWin(string winnerName, Color winnerColor)
    {
        Announce($"{winnerName} WINS THE MATCH!", winnerColor, 3f);
    }

    private void Update()
    {
        if (announceTimer > 0f)
            announceTimer -= Time.deltaTime;
    }

    private void OnGUI()
    {
        if (announceTimer <= 0f || string.IsNullOrEmpty(currentText)) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 48,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            shadowStyle = new GUIStyle(style);
        }

        // Fade out in last portion
        float alpha = announceTimer < fadeOutDuration
            ? announceTimer / fadeOutDuration
            : 1f;

        // Mutate the cached styles' colors — no allocation
        style.normal.textColor      = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
        shadowStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.8f);

        Rect center = new Rect(0, Screen.height * 0.3f, Screen.width, 60);
        Rect shadow = new Rect(2, Screen.height * 0.3f + 2, Screen.width, 60);

        GUI.Label(shadow, currentText, shadowStyle);
        GUI.Label(center, currentText, style);
    }
}
