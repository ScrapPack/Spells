using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple component tagging a player with a unique ID.
/// Used by projectiles for owner checking and by game systems for player tracking.
/// </summary>
public class PlayerIdentity : MonoBehaviour
{
    /// <summary>All currently active PlayerIdentity instances. Zero allocation, no scene scan.</summary>
    public static readonly List<PlayerIdentity> All = new List<PlayerIdentity>();

    public int PlayerID { get; private set; }

    private void OnEnable()  => All.Add(this);
    private void OnDisable() => All.Remove(this);

    /// <summary>Set by BoxArenaBuilder on spawn.</summary>
    public void Initialize(int playerID)
    {
        PlayerID = playerID;
    }
}
