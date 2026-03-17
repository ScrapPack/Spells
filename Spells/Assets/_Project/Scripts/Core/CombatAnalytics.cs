using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks per-player combat statistics across a match.
/// Subscribes to HealthSystem and ProjectileSpawner events.
/// Data used by scoreboard, post-match summary, and for GDD balance tuning.
///
/// Stats reset each match, accumulate across rounds.
/// </summary>
public class CombatAnalytics : MonoBehaviour
{
    [System.Serializable]
    public class PlayerStats
    {
        public int kills;
        public int deaths;
        public int parriesSucceeded;
        public int parriesFailed;
        public int projectilesFired;
        public float damageDealt;
        public float damageReceived;
        public int cardsPicked;
        public int roundsWon;

        /// <summary>
        /// Kill/Death ratio. Returns kills if deaths is 0.
        /// </summary>
        public float KDRatio => deaths > 0 ? (float)kills / deaths : kills;

        /// <summary>
        /// Parry success rate (0-1).
        /// </summary>
        public float ParryRate
        {
            get
            {
                int total = parriesSucceeded + parriesFailed;
                return total > 0 ? (float)parriesSucceeded / total : 0f;
            }
        }

        public void Reset()
        {
            kills = 0;
            deaths = 0;
            parriesSucceeded = 0;
            parriesFailed = 0;
            projectilesFired = 0;
            damageDealt = 0f;
            damageReceived = 0f;
            cardsPicked = 0;
            roundsWon = 0;
        }
    }

    [Header("Events")]
    public UnityEvent<int, PlayerStats> OnStatsUpdated; // (playerID, stats)

    private readonly Dictionary<int, PlayerStats> allStats = new Dictionary<int, PlayerStats>();

    /// <summary>
    /// Get stats for a specific player. Creates entry if missing.
    /// </summary>
    public PlayerStats GetStats(int playerID)
    {
        if (!allStats.ContainsKey(playerID))
            allStats[playerID] = new PlayerStats();
        return allStats[playerID];
    }

    /// <summary>
    /// Get all player stats (for end-of-match display).
    /// </summary>
    public Dictionary<int, PlayerStats> GetAllStats()
    {
        return new Dictionary<int, PlayerStats>(allStats);
    }

    /// <summary>
    /// Reset all stats for a new match.
    /// </summary>
    public void ResetMatch()
    {
        foreach (var kvp in allStats)
            kvp.Value.Reset();
    }

    // ==================================================
    // Recording methods — called by other systems
    // ==================================================

    public void RecordKill(int killerID)
    {
        GetStats(killerID).kills++;
        NotifyUpdate(killerID);
    }

    public void RecordDeath(int playerID)
    {
        GetStats(playerID).deaths++;
        NotifyUpdate(playerID);
    }

    public void RecordDamageDealt(int attackerID, float amount)
    {
        GetStats(attackerID).damageDealt += amount;
        NotifyUpdate(attackerID);
    }

    public void RecordDamageReceived(int playerID, float amount)
    {
        GetStats(playerID).damageReceived += amount;
        NotifyUpdate(playerID);
    }

    public void RecordParrySuccess(int playerID)
    {
        GetStats(playerID).parriesSucceeded++;
        NotifyUpdate(playerID);
    }

    public void RecordParryFail(int playerID)
    {
        GetStats(playerID).parriesFailed++;
        NotifyUpdate(playerID);
    }

    public void RecordProjectileFired(int playerID)
    {
        GetStats(playerID).projectilesFired++;
        // Don't fire event for every shot (too noisy)
    }

    public void RecordCardPicked(int playerID)
    {
        GetStats(playerID).cardsPicked++;
        NotifyUpdate(playerID);
    }

    public void RecordRoundWin(int playerID)
    {
        GetStats(playerID).roundsWon++;
        NotifyUpdate(playerID);
    }

    private void NotifyUpdate(int playerID)
    {
        OnStatsUpdated?.Invoke(playerID, GetStats(playerID));
    }
}
