using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays post-match summary stats: kills, deaths, KD ratio,
/// parry success rate, damage dealt/received, cards picked.
/// Call ShowSummary(winnerID) to display.
///
/// OnGUI for now — intended to be replaced with proper UI canvas.
/// </summary>
public class PostMatchSummary : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatAnalytics analytics;

    private bool showSummary;
    private int winnerID = -1;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;

    /// <summary>
    /// Show the post-match summary for the given winner.
    /// Call this from BoxArenaBuilder.EndMatch().
    /// </summary>
    public void ShowSummary(int winner)
    {
        winnerID = winner;
        showSummary = true;
    }

    /// <summary>
    /// Close the summary (returns to lobby/rematch).
    /// </summary>
    public void CloseSummary()
    {
        showSummary = false;
    }

    private void OnGUI()
    {
        if (!showSummary || analytics == null) return;

        InitStyles();

        var allStats = analytics.GetAllStats();
        if (allStats.Count == 0) return;

        // Background overlay
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", GUI.skin.box);

        float panelWidth = 600f;
        float panelHeight = 300f;
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = (Screen.height - panelHeight) * 0.5f;

        GUI.Box(new Rect(panelX - 10, panelY - 10, panelWidth + 20, panelHeight + 20), "");

        // Header
        string winnerName = winnerID >= 0 ? $"Player {winnerID + 1}" : "Draw";
        GUI.Label(new Rect(panelX, panelY, panelWidth, 30f),
            $"MATCH OVER — {winnerName} WINS!", headerStyle);

        // Column headers
        float y = panelY + 40f;
        float colWidth = 70f;
        string[] columns = { "Player", "Kills", "Deaths", "K/D", "Parry%", "Dmg Out", "Dmg In", "Cards" };
        for (int c = 0; c < columns.Length; c++)
        {
            GUI.Label(new Rect(panelX + c * colWidth, y, colWidth, 20f), columns[c], labelStyle);
        }

        y += 25f;

        // Player rows
        var sortedKeys = new List<int>(allStats.Keys);
        sortedKeys.Sort();

        foreach (int playerID in sortedKeys)
        {
            var stats = allStats[playerID];
            bool isWinner = playerID == winnerID;
            GUIStyle rowStyle = isWinner ? headerStyle : valueStyle;

            float x = panelX;
            GUI.Label(new Rect(x, y, colWidth, 20f), $"P{playerID + 1}", rowStyle);
            x += colWidth;
            GUI.Label(new Rect(x, y, colWidth, 20f), stats.kills.ToString(), rowStyle);
            x += colWidth;
            GUI.Label(new Rect(x, y, colWidth, 20f), stats.deaths.ToString(), rowStyle);
            x += colWidth;
            GUI.Label(new Rect(x, y, colWidth, 20f), stats.KDRatio.ToString("F1"), rowStyle);
            x += colWidth;
            GUI.Label(new Rect(x, y, colWidth, 20f), (stats.ParryRate * 100f).ToString("F0") + "%", rowStyle);
            x += colWidth;
            GUI.Label(new Rect(x, y, colWidth, 20f), stats.damageDealt.ToString("F0"), rowStyle);
            x += colWidth;
            GUI.Label(new Rect(x, y, colWidth, 20f), stats.damageReceived.ToString("F0"), rowStyle);
            x += colWidth;
            GUI.Label(new Rect(x, y, colWidth, 20f), stats.cardsPicked.ToString(), rowStyle);

            y += 22f;
        }
    }

    private void InitStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = Color.yellow;
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (valueStyle == null)
        {
            valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
