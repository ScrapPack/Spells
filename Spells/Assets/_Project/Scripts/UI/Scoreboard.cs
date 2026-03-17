using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays round wins per player. Rendered with OnGUI.
/// Shows player color, name/class, and win count as filled pips.
/// Call UpdateScores() each time the score changes.
/// </summary>
public class Scoreboard : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int winsToWin = 5;

    [Header("Colors")]
    [SerializeField] private Color[] playerColors = new Color[]
    {
        new Color(0.2f, 0.5f, 1f),
        new Color(1f, 0.3f, 0.3f),
        new Color(0.3f, 1f, 0.3f),
        new Color(1f, 0.9f, 0.2f),
    };

    private Dictionary<int, int> scores = new Dictionary<int, int>();
    private GUIStyle labelStyle;
    private Texture2D fullTex;
    private Texture2D emptyTex;

    public void SetWinsToWin(int roundsToWin)
    {
        winsToWin = roundsToWin;
    }

    /// <summary>
    /// Push a fresh score snapshot. Call this whenever round wins change.
    /// </summary>
    public void UpdateScores(Dictionary<int, int> newScores)
    {
        scores = new Dictionary<int, int>(newScores);
    }

    private void OnGUI()
    {
        if (scores.Count == 0) return;

        // Lazy-init styles
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            fullTex  = MakeTex(1, 1, Color.white);
            emptyTex = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        }

        float x      = Screen.width / 2f - 150;
        float y      = 8;
        float lineH  = 28f;
        float pipSize = 16f;
        float pipGap  = 4f;

        var sortedKeys = new List<int>(scores.Keys);
        sortedKeys.Sort();

        foreach (int playerID in sortedKeys)
        {
            int wins = scores[playerID];

            Color pColor = playerID < playerColors.Length ? playerColors[playerID] : Color.white;
            labelStyle.normal.textColor = pColor;

            GUI.Label(new Rect(x, y, 100, lineH), $"P{playerID + 1}", labelStyle);

            for (int i = 0; i < winsToWin; i++)
            {
                Rect pipRect = new Rect(x + 60 + i * (pipSize + pipGap), y + 6, pipSize, pipSize);
                if (i < wins)
                {
                    GUI.color = pColor;
                    GUI.DrawTexture(pipRect, fullTex);
                }
                else
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(pipRect, emptyTex);
                }
            }
            GUI.color = Color.white;

            y += lineH;
        }
    }

    private Texture2D MakeTex(int w, int h, Color color)
    {
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
