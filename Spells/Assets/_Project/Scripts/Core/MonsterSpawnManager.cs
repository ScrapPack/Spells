using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages monster spawning and despawning per round.
/// Reads level pool from DraftManager for scaling.
/// On monster kill: grants level to killer via DraftManager,
/// routes kill event to KillFeed for announcement.
///
/// Called by MatchManager in StartNextRound() and OnRoundEnded().
/// </summary>
public class MonsterSpawnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DraftManager draftManager;
    [SerializeField] private KillFeed killFeed;
    [SerializeField] private CombatAnalytics analytics;

    [Header("Events")]
    public UnityEvent<int, int> OnMonsterKilled; // (monsterID, killerPlayerID)

    private MonsterEntity[] activeMonsters;
    private int nextMonsterID;

    /// <summary>
    /// Spawn monsters for a round based on arena layout configuration.
    /// </summary>
    public void SpawnMonsters(ArenaLayoutData arenaLayout, Transform[] spawnPoints)
    {
        if (arenaLayout == null || arenaLayout.monsterData == null) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        int levelPool = draftManager != null ? draftManager.GetTotalLevelPool() : 0;
        int count = Mathf.Min(arenaLayout.monsterCount, spawnPoints.Length);

        activeMonsters = new MonsterEntity[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Monster_{arenaLayout.monsterData.monsterName}_{nextMonsterID}");
            go.transform.position = spawnPoints[i].position;

            // Add required components
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<BoxCollider2D>();

            var monster = go.AddComponent<MonsterEntity>();
            monster.Initialize(arenaLayout.monsterData, nextMonsterID, levelPool);

            // Subscribe to kill event
            int monsterID = nextMonsterID;
            monster.OnMonsterKilled.AddListener((killerID) => OnMonsterDied(monsterID, killerID));

            activeMonsters[i] = monster;
            nextMonsterID++;
        }

        Debug.Log($"MonsterSpawnManager: Spawned {count} {arenaLayout.monsterData.monsterName}(s) " +
                  $"at level pool {levelPool}");
    }

    /// <summary>
    /// Despawn all active monsters (called at round end).
    /// </summary>
    public void DespawnAll()
    {
        if (activeMonsters == null) return;

        foreach (var monster in activeMonsters)
        {
            if (monster != null)
                Destroy(monster.gameObject);
        }

        activeMonsters = null;
    }

    private void OnMonsterDied(int monsterID, int killerPlayerID)
    {
        // Grant level to killer
        if (draftManager != null && killerPlayerID >= 0)
            draftManager.GrantLevel(killerPlayerID);

        // Record in analytics
        if (analytics != null)
            analytics.RecordRoundWin(killerPlayerID); // Repurpose for now

        // Kill feed announcement
        if (killFeed != null)
        {
            string killerName = $"Player {killerPlayerID + 1}";
            killFeed.AddEntry($"{killerName} slew the Monster", Color.gray);
        }

        OnMonsterKilled?.Invoke(monsterID, killerPlayerID);
    }
}
