using UnityEngine;

/// <summary>
/// Simple component tagging a player with a unique ID.
/// Used by projectiles for owner checking and by game systems for player tracking.
/// </summary>
public class PlayerIdentity : MonoBehaviour
{
    public int PlayerID { get; private set; }

    /// <summary>
    /// Set by BoxArenaBuilder on spawn.
    /// </summary>
    public void Initialize(int playerID)
    {
        PlayerID = playerID;
    }
}
