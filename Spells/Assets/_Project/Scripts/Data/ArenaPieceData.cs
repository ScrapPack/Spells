using UnityEngine;

/// <summary>
/// Defines a single modular arena piece (platform, bridge, gap, wall, ramp, spawn marker).
/// Arena pieces are composable building blocks that ModularArenaBuilder
/// instantiates based on ArenaLayoutData configurations.
///
/// PieceType determines what geometry gets created:
/// - Platform: rectangular BoxCollider2D surface
/// - Bridge: narrow walkable span between two supports
/// - Wall: vertical barrier
/// - Ramp: angled surface (PolygonCollider2D)
/// - Gap: empty space (no geometry, but defines a hole in the bridge)
/// - SpawnPoint: invisible marker for player/monster/chest spawn positions
/// - KillZone: damage trigger zone (uses EnvironmentHazard)
/// </summary>
[CreateAssetMenu(fileName = "ArenaPieceData", menuName = "Spells/Arena Piece Data")]
public class ArenaPieceData : ScriptableObject
{
    public enum PieceType
    {
        Platform,
        Bridge,
        Wall,
        Ramp,
        Gap,
        SpawnPoint,
        KillZone
    }

    public enum SpawnPointType
    {
        Player,
        Monster,
        Chest
    }

    [Header("Piece Definition")]
    public string pieceName = "Platform";
    public PieceType pieceType = PieceType.Platform;

    [Header("Geometry")]
    [Tooltip("Size of the piece in units (width x height)")]
    public Vector2 size = new Vector2(6f, 1f);
    [Tooltip("Ramp angle in degrees (only used for PieceType.Ramp)")]
    [Range(0f, 60f)] public float rampAngle = 30f;

    [Header("Spawn Point")]
    [Tooltip("Type of spawn point (only used for PieceType.SpawnPoint)")]
    public SpawnPointType spawnType = SpawnPointType.Player;

    [Header("Kill Zone")]
    [Tooltip("Damage dealt by kill zone per tick")]
    [Range(1f, 100f)] public float killZoneDamage = 100f;

    [Header("Visual")]
    public Color pieceColor = new Color(0.4f, 0.4f, 0.4f, 1f);
}
