using UnityEngine;

/// <summary>
/// Configurable match settings ScriptableObject.
/// Can be modified in Custom Match setup.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "Spells/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Match")]
    [Tooltip("Round wins required to win the match")]
    [Range(1, 10)] public int roundsToWin = 5;
    [Tooltip("Maximum players")]
    [Range(2, 4)] public int maxPlayers = 4;

    [Header("Round Timing")]
    [Tooltip("Seconds before camera zoom begins")]
    [Range(10f, 60f)] public float zoomDelay = 30f;
    [Tooltip("Total round duration before full compression")]
    [Range(30f, 180f)] public float maxRoundTime = 90f;

    [Header("Draft")]
    [Tooltip("Number of card options per pick")]
    [Range(2, 6)] public int cardOptionsPerPick = 4;
    [Tooltip("Seconds allowed per card pick (0 = unlimited)")]
    [Range(0f, 30f)] public float draftTimeLimit = 15f;
    [Tooltip("General pool ratio in draft draws")]
    [Range(0f, 1f)] public float generalPoolRatio = 0.4f;

    [Header("Restrictions")]
    [Tooltip("Allow duplicate class picks")]
    public bool allowDuplicateClasses = true;
    [Tooltip("Cards banned from this match (by card name)")]
    public string[] bannedCards;

    [Header("Respawn")]
    [Tooltip("Seconds before respawning at round start")]
    [Range(0f, 5f)] public float spawnDelay = 1f;
    [Tooltip("Invincibility duration after spawning")]
    [Range(0f, 3f)] public float spawnProtection = 1.5f;
}
