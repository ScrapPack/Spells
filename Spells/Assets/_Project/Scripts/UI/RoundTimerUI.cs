using UnityEngine;

/// <summary>
/// Countdown timer displayed at the top center of the screen.
/// Counts down from matchDuration showing seconds and milliseconds.
/// Always displayed in red.
/// </summary>
public class RoundTimerUI : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private float matchDuration = 60f;

    [Header("Display")]
    [SerializeField] private Color timerColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private int fontSize = 28;

    /// <summary>Time remaining in seconds. 0 when expired.</summary>
    public float TimeRemaining { get; private set; }

    private float startTime = -1f;
    private GUIStyle timerStyle;
    private string cachedDisplay = "60:00";
    private int lastSecs = -1;
    private int lastMs   = -1;

    /// <summary>Call when a round begins to start the countdown.</summary>
    public void StartTimer()
    {
        startTime = Time.time;
        TimeRemaining = matchDuration;
    }

    /// <summary>Call when a round ends to stop the countdown.</summary>
    public void StopTimer() => startTime = -1f;

    private void OnGUI()
    {
        if (startTime < 0f) return;

        InitStyles();

        float elapsed = Time.time - startTime;
        TimeRemaining = Mathf.Max(0f, matchDuration - elapsed);

        int secs = (int)TimeRemaining;
        int ms   = (int)((TimeRemaining - secs) * 100f);

        // Rebuild the string only when the displayed value changes (≤100×/sec),
        // not every OnGUI call (which fires 3–5× per frame).
        if (secs != lastSecs || ms != lastMs)
        {
            cachedDisplay = $"{secs:D2}:{ms:D2}";
            lastSecs = secs;
            lastMs   = ms;
        }

        GUI.Label(new Rect(Screen.width * 0.5f - 60f, 8f, 120f, 36f),
            cachedDisplay, timerStyle);
    }

    private void InitStyles()
    {
        if (timerStyle != null) return;
        timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        timerStyle.normal.textColor = timerColor;
    }
}
