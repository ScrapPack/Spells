using UnityEngine;

/// <summary>
/// Defines a complete arena layout as a composition of ArenaPieceData entries.
/// Each arena is built from modular pieces placed at specific positions and rotations.
/// ModularArenaBuilder reads this asset and instantiates the geometry at runtime.
///
/// Supports all 6 GDD arenas through different piece arrangements.
/// Monster and chest spawn points are embedded as SpawnPoint pieces.
/// </summary>
[CreateAssetMenu(fileName = "ArenaLayoutData", menuName = "Spells/Arena Layout Data")]
public class ArenaLayoutData : ScriptableObject
{
    [Header("Identity")]
    public string arenaName = "New Arena";
    [TextArea(2, 4)]
    public string description = "Arena layout description.";

    [Header("Layout")]
    [Tooltip("Array of piece placements that compose this arena")]
    public ArenaPlacement[] pieces;

    [Header("Bounds")]
    [Tooltip("Total arena bounds (for camera and ArenaZone)")]
    public Vector2 arenaBounds = new Vector2(42f, 25f);
    [Tooltip("Arena center point")]
    public Vector2 arenaCenter = Vector2.zero;

    [Header("Monster Config")]
    [Tooltip("MonsterData to spawn in this arena (null = no monster)")]
    public MonsterData monsterData;
    [Tooltip("Number of monsters to spawn per round")]
    [Range(0, 3)] public int monsterCount = 1;

    [Header("Chest Config")]
    [Tooltip("Number of item chests to spawn per round")]
    [Range(0, 5)] public int chestCount = 2;
    [Tooltip("Available items for chests in this arena")]
    public ItemData[] chestItemPool;

    /// <summary>
    /// Get all spawn points of a given type from the layout.
    /// </summary>
    public ArenaPlacement[] GetSpawnPoints(ArenaPieceData.SpawnPointType spawnType)
    {
        if (pieces == null) return new ArenaPlacement[0];

        var results = new System.Collections.Generic.List<ArenaPlacement>();
        foreach (var placement in pieces)
        {
            if (placement.piece == null) continue;
            if (placement.piece.pieceType == ArenaPieceData.PieceType.SpawnPoint
                && placement.piece.spawnType == spawnType)
            {
                results.Add(placement);
            }
        }
        return results.ToArray();
    }
}

/// <summary>
/// A single piece placement within an arena layout.
/// Combines the piece definition with its position and rotation.
/// </summary>
[System.Serializable]
public class ArenaPlacement
{
    [Tooltip("The arena piece to place")]
    public ArenaPieceData piece;
    [Tooltip("World position for this piece")]
    public Vector2 position;
    [Tooltip("Rotation in degrees")]
    public float rotation;
}
