using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// OnGUI display for character selection screen.
/// Shows available classes, current selection per player,
/// ready status, and countdown to match start.
///
/// Reads from CharacterSelectManager for state,
/// passes input events back to it.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterSelectManager selectManager;

    [Header("Layout")]
    [SerializeField] private float panelWidth = 200f;
    [SerializeField] private float panelHeight = 280f;
    [SerializeField] private float panelSpacing = 20f;

    private GUIStyle headerStyle;
    private GUIStyle classNameStyle;
    private GUIStyle readyStyle;
    private GUIStyle descStyle;

    private void OnGUI()
    {
        if (selectManager == null || !selectManager.IsActive) return;

        InitStyles();

        // Title
        GUI.Label(
            new Rect(0, 20, Screen.width, 40),
            "SELECT YOUR CLASS",
            headerStyle
        );

        // Player panels
        var selections = selectManager.GetAllSelections();
        int playerCount = selections.Count;
        float totalWidth = playerCount * panelWidth + (playerCount - 1) * panelSpacing;
        float startX = (Screen.width - totalWidth) * 0.5f;

        int panelIndex = 0;
        var sortedKeys = new List<int>(selections.Keys);
        sortedKeys.Sort();

        foreach (int playerID in sortedKeys)
        {
            var classData = selections[playerID];
            float x = startX + panelIndex * (panelWidth + panelSpacing);
            float y = 80f;

            DrawPlayerPanel(x, y, playerID, classData);
            panelIndex++;
        }
    }

    private void DrawPlayerPanel(float x, float y, int playerID, ClassData classData)
    {
        // Background
        GUI.Box(new Rect(x - 5, y - 5, panelWidth + 10, panelHeight + 10), "");

        // Player label
        Color playerColor = classData != null ? classData.classColor : Color.white;
        GUIStyle playerLabelStyle = new GUIStyle(classNameStyle);
        playerLabelStyle.normal.textColor = playerColor;

        GUI.Label(new Rect(x, y, panelWidth, 25), $"Player {playerID + 1}", playerLabelStyle);
        y += 30;

        if (classData != null)
        {
            // Class icon placeholder (colored box)
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = classData.classColor;
            GUI.Box(new Rect(x + panelWidth * 0.5f - 30, y, 60, 60), "");
            GUI.backgroundColor = prevBg;
            y += 70;

            // Class name
            GUI.Label(new Rect(x, y, panelWidth, 25), classData.className, classNameStyle);
            y += 28;

            // Description
            if (!string.IsNullOrEmpty(classData.description))
            {
                GUI.Label(new Rect(x, y, panelWidth, 40), classData.description, descStyle);
                y += 45;
            }

            // HP and projectile info
            if (classData.combatData != null)
            {
                string infoText = $"HP: {classData.combatData.maxHP}  |  " +
                    $"DMG: {classData.combatData.projectileDamage:F0}";
                GUI.Label(new Rect(x, y, panelWidth, 20), infoText, descStyle);
                y += 22;
            }
        }
        else
        {
            GUI.Label(new Rect(x, y, panelWidth, 25), "No class selected", classNameStyle);
            y += 30;
        }

        // Navigation arrows
        y = 80 + panelHeight - 60;
        if (GUI.Button(new Rect(x, y, 40, 30), "◄"))
        {
            selectManager.CycleClass(playerID, -1);
        }
        if (GUI.Button(new Rect(x + panelWidth - 40, y, 40, 30), "►"))
        {
            selectManager.CycleClass(playerID, 1);
        }

        // Ready button
        y += 35;
        string readyText = "READY";
        if (GUI.Button(new Rect(x + 20, y, panelWidth - 40, 25), readyText))
        {
            selectManager.ReadyUp(playerID);
        }
    }

    private void InitStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = Color.white;
        }

        if (classNameStyle == null)
        {
            classNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (readyStyle == null)
        {
            readyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            readyStyle.normal.textColor = Color.green;
        }

        if (descStyle == null)
        {
            descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
        }
    }
}
