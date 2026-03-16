using UnityEngine;

/// <summary>
/// Manages item chest spawning and despawning per round.
/// Spawns chests at designated points with items scaled by total level pool.
/// Called by MatchManager in StartNextRound() and OnRoundEnded().
/// </summary>
public class ChestSpawnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DraftManager draftManager;

    [Header("Chest Prefab")]
    [SerializeField] private GameObject chestPrefab;

    private GameObject[] activeChests;

    /// <summary>
    /// Spawn chests for a round based on arena layout configuration.
    /// </summary>
    public void SpawnChests(ArenaLayoutData arenaLayout, Transform[] spawnPoints)
    {
        if (arenaLayout == null || spawnPoints == null || spawnPoints.Length == 0) return;

        int levelPool = draftManager != null ? draftManager.GetTotalLevelPool() : 0;
        int count = Mathf.Min(arenaLayout.chestCount, spawnPoints.Length);

        // Filter available items by level pool
        var availableItems = new System.Collections.Generic.List<ItemData>();
        if (arenaLayout.chestItemPool != null)
        {
            foreach (var item in arenaLayout.chestItemPool)
            {
                if (item != null && item.IsAvailableAtLevelPool(levelPool))
                    availableItems.Add(item);
            }
        }

        if (availableItems.Count == 0) return;

        activeChests = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            if (chestPrefab != null)
            {
                activeChests[i] = Instantiate(chestPrefab, spawnPoints[i].position, Quaternion.identity);
            }
            else
            {
                // Create a simple chest object
                var go = new GameObject($"Chest_{i}");
                go.transform.position = spawnPoints[i].position;

                // Add trigger collider
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1f, 1f);

                // Add visual
                var spriteRenderer = go.AddComponent<SpriteRenderer>();
                var tex = new Texture2D(32, 32);
                var pixels = new Color[32 * 32];
                for (int p = 0; p < pixels.Length; p++) pixels[p] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
                spriteRenderer.color = new Color(1f, 0.85f, 0.2f);

                // Add ItemChest component
                var chest = go.AddComponent<ItemChest>();
                chest.Initialize(availableItems.ToArray(), levelPool);

                activeChests[i] = go;
            }
        }

        Debug.Log($"ChestSpawnManager: Spawned {count} chests with {availableItems.Count} available items " +
                  $"at level pool {levelPool}");
    }

    /// <summary>
    /// Despawn all active chests (called at round end).
    /// </summary>
    public void DespawnAll()
    {
        if (activeChests == null) return;

        foreach (var chest in activeChests)
        {
            if (chest != null)
                Destroy(chest);
        }

        activeChests = null;
    }
}
