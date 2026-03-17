using UnityEngine;

/// <summary>
/// Displays the round timer.
/// Set RoundStartTime in OnGUI to show elapsed time.
/// </summary>
public class RoundTimerUI : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Color normalColor  = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.6f, 0f, 1f);
    [SerializeField] private Color dangerColor  = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float warningSeconds = 30f;
    [SerializeField] private float dangerSeconds  = 10f;

    private float startTime = -1f;
    private GUIStyle timerStyle;

    /// <summary>Call when a round begins to start the timer.</summary>
    public void StartTimer() => startTime = Time.time;

    /// <summary>Call when a round ends to stop the timer.</summary>
    public void StopTimer() => startTime = -1f;

    private void OnGUI()
    {
        if (startTime < 0f) return;

        InitStyles();

        float elapsed  = Time.time - startTime;
        int   minutes  = (int)(elapsed / 60f);
        int   seconds  = (int)(elapsed % 60f);
        float remaining = Mathf.Max(0f, warningSeconds - elapsed);

        if (elapsed >= warningSeconds - dangerSeconds)
            timerStyle.normal.textColor = dangerColor;
        else if (elapsed >= warningSeconds - warningSeconds * 0.5f)
            timerStyle.normal.textColor = warningColor;
        else
            timerStyle.normal.textColor = normalColor;

        GUI.Label(new Rect(Screen.width * 0.5f - 40f, 10f, 80f, 30f),
            $"{minutes}:{seconds:D2}", timerStyle);
    }

    private void InitStyles()
    {
        if (timerStyle != null) return;
        timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize   = 22,
            fontStyle  = FontStyle.Bold,
            alignment  = TextAnchor.MiddleCenter
        };
    }
}
