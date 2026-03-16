using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays elimination announcements during a round.
/// Tracks who eliminated whom and shows a scrolling feed.
/// Rendered with OnGUI for simplicity (can be replaced with UI Canvas later).
/// </summary>
public class KillFeed : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private float displayDuration = 4f;
    [SerializeField] private int maxVisible = 5;

    private struct KillEntry
    {
        public string message;
        public float timestamp;
        public Color color;
    }

    private readonly List<KillEntry> entries = new List<KillEntry>();
    private GUIStyle feedStyle;

    /// <summary>
    /// Add a kill/elimination entry to the feed.
    /// </summary>
    public void AddEntry(string message, Color color)
    {
        entries.Add(new KillEntry
        {
            message = message,
            timestamp = Time.time,
            color = color
        });

        // Trim old entries
        while (entries.Count > maxVisible * 2)
            entries.RemoveAt(0);
    }

    /// <summary>
    /// Add a player elimination entry.
    /// </summary>
    public void AddElimination(string eliminatedName, string eliminatorName, Color eliminatorColor)
    {
        string msg = eliminatorName != null
            ? $"{eliminatorName} eliminated {eliminatedName}"
            : $"{eliminatedName} was eliminated";
        AddEntry(msg, eliminatorColor);
    }

    /// <summary>
    /// Add a round winner entry.
    /// </summary>
    public void AddRoundWin(string winnerName, int roundNumber, Color winnerColor)
    {
        AddEntry($"{winnerName} wins Round {roundNumber}!", winnerColor);
    }

    private void OnGUI()
    {
        if (feedStyle == null)
        {
            feedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.UpperRight,
                fontStyle = FontStyle.Bold
            };
        }

        float x = Screen.width - 310;
        float y = 10;
        float lineHeight = 22f;
        float now = Time.time;

        int shown = 0;
        for (int i = entries.Count - 1; i >= 0 && shown < maxVisible; i--)
        {
            var entry = entries[i];
            float age = now - entry.timestamp;
            if (age > displayDuration) continue;

            // Fade out in last second
            float alpha = age > displayDuration - 1f ? (displayDuration - age) : 1f;
            feedStyle.normal.textColor = new Color(entry.color.r, entry.color.g, entry.color.b, alpha);

            GUI.Label(new Rect(x, y + shown * lineHeight, 300, lineHeight), entry.message, feedStyle);
            shown++;
        }
    }
}
