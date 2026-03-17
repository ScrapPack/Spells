using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validates procedurally generated ArenaLayoutData for playability.
/// Checks that all player spawn points are reachable from each other
/// using a graph-based reachability analysis based on player physics.
///
/// Physics constraints:
///   - Max jump height: ~3.0 units (conservative)
///   - Max horizontal jump: ~5.5 units
///   - Max fall horizontal drift: ~10 units
///   - Wall jump: extends reach significantly
/// </summary>
public static class LevelValidator
{
    // Physics-based reachability constants
    private const float MAX_JUMP_UP = 3.0f;
    private const float MAX_JUMP_HORIZONTAL = 5.5f;
    private const float MAX_FALL_HORIZONTAL = 10f;
    private const float MIN_PLAYER_SPAWNS = 2;

    /// <summary>
    /// Validate that a generated layout is playable.
    /// Returns true if all player spawn points are mutually reachable.
    /// </summary>
    public static bool Validate(ArenaLayoutData layout)
    {
        if (layout == null || layout.pieces == null || layout.pieces.Length == 0)
            return false;

        // Collect all platforms (surfaces players can stand on)
        var platforms = new List<PlatformNode>();
        var playerSpawns = new List<Vector2>();

        foreach (var placement in layout.pieces)
        {
            if (placement == null || placement.piece == null) continue;

            var type = placement.piece.pieceType;
            if (type == ArenaPieceData.PieceType.Platform ||
                type == ArenaPieceData.PieceType.Bridge ||
                type == ArenaPieceData.PieceType.MovingPlatform)
            {
                platforms.Add(new PlatformNode
                {
                    center = placement.position,
                    width = placement.piece.size.x,
                    height = placement.piece.size.y,
                    topY = placement.position.y + placement.piece.size.y / 2f
                });
            }

            if (type == ArenaPieceData.PieceType.SpawnPoint &&
                placement.piece.spawnType == ArenaPieceData.SpawnPointType.Player)
            {
                playerSpawns.Add(placement.position);
            }
        }

        // Must have enough player spawns
        if (playerSpawns.Count < MIN_PLAYER_SPAWNS)
        {
            Debug.LogWarning($"LevelValidator: Only {playerSpawns.Count} player spawns (need {MIN_PLAYER_SPAWNS})");
            return false;
        }

        // Must have platforms
        if (platforms.Count == 0)
        {
            Debug.LogWarning("LevelValidator: No platforms found");
            return false;
        }

        // Check spawn points are above platforms (not floating in air or over kill zones)
        foreach (var spawn in playerSpawns)
        {
            if (!IsAbovePlatform(spawn, platforms))
            {
                Debug.LogWarning($"LevelValidator: Spawn at {spawn} is not above any platform");
                return false;
            }
        }

        // Build reachability graph
        var adjacency = BuildReachabilityGraph(platforms);

        // Check all player spawns are in the same connected component
        // Find which platform each spawn is on
        var spawnPlatformIndices = new List<int>();
        foreach (var spawn in playerSpawns)
        {
            int nearest = FindNearestPlatform(spawn, platforms);
            if (nearest >= 0)
                spawnPlatformIndices.Add(nearest);
        }

        if (spawnPlatformIndices.Count < 2)
            return spawnPlatformIndices.Count >= 1;

        // BFS from first spawn's platform to check all others are reachable
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(spawnPlatformIndices[0]);
        visited.Add(spawnPlatformIndices[0]);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (adjacency.ContainsKey(current))
            {
                foreach (int neighbor in adjacency[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // Check all spawn platforms were reached
        foreach (int platformIdx in spawnPlatformIndices)
        {
            if (!visited.Contains(platformIdx))
            {
                Debug.LogWarning($"LevelValidator: Platform {platformIdx} not reachable from spawn platform {spawnPlatformIndices[0]}");
                return false;
            }
        }

        return true;
    }

    // =========================================================
    // Reachability Analysis
    // =========================================================

    private static Dictionary<int, List<int>> BuildReachabilityGraph(List<PlatformNode> platforms)
    {
        var graph = new Dictionary<int, List<int>>();

        for (int i = 0; i < platforms.Count; i++)
        {
            graph[i] = new List<int>();

            for (int j = 0; j < platforms.Count; j++)
            {
                if (i == j) continue;

                if (CanReach(platforms[i], platforms[j]))
                {
                    graph[i].Add(j);
                }
            }
        }

        return graph;
    }

    /// <summary>
    /// Can a player get from platform A to platform B?
    /// Considers jumping up, falling down, and walking across.
    /// </summary>
    private static bool CanReach(PlatformNode from, PlatformNode to)
    {
        // Horizontal distance between platform edges (not centers)
        float edgeDistX = Mathf.Max(0,
            Mathf.Abs(from.center.x - to.center.x) - (from.width + to.width) / 2f);

        // Vertical distance (positive = target is above)
        float dy = to.topY - from.topY;

        // Walking: platforms at same height and overlapping/adjacent
        if (Mathf.Abs(dy) < 0.5f && edgeDistX < 0.5f)
            return true;

        // Jumping up: target is above, within jump reach
        if (dy > 0 && dy <= MAX_JUMP_UP && edgeDistX <= MAX_JUMP_HORIZONTAL)
            return true;

        // Falling down: target is below, can drift to reach it
        if (dy < 0 && edgeDistX <= MAX_FALL_HORIZONTAL)
            return true;

        // Same height jump: target is at similar height, within jump reach
        if (Mathf.Abs(dy) <= 1f && edgeDistX <= MAX_JUMP_HORIZONTAL)
            return true;

        return false;
    }

    // =========================================================
    // Helpers
    // =========================================================

    private static bool IsAbovePlatform(Vector2 point, List<PlatformNode> platforms)
    {
        foreach (var plat in platforms)
        {
            // Point is above this platform if within horizontal bounds
            // and not too far above
            float halfW = plat.width / 2f;
            if (point.x >= plat.center.x - halfW &&
                point.x <= plat.center.x + halfW &&
                point.y >= plat.topY - 1f &&
                point.y <= plat.topY + 5f)
            {
                return true;
            }
        }
        return false;
    }

    private static int FindNearestPlatform(Vector2 point, List<PlatformNode> platforms)
    {
        int nearest = -1;
        float minDist = float.MaxValue;

        for (int i = 0; i < platforms.Count; i++)
        {
            float dist = Vector2.Distance(point, new Vector2(platforms[i].center.x, platforms[i].topY));
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }

        return nearest;
    }

    // =========================================================
    // Data
    // =========================================================

    private struct PlatformNode
    {
        public Vector2 center;
        public float width;
        public float height;
        public float topY;
    }
}
