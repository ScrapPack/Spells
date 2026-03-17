using UnityEngine;

/// <summary>
/// Defines a biome's structural rules, hazard configuration, and visual identity.
/// ProceduralLevelGenerator reads this to create themed ArenaLayoutData at runtime.
///
/// Each biome creates a distinct gameplay feel:
/// - Forest Temple: multi-level exploration with vine walls
/// - Desert Ruins: horizontal traversal across bridges and gaps
/// - Volcanic Caldera: island hopping over lava with moving platforms
/// - Crystal Cavern: classic arena combat with clear sightlines
/// - Storm Citadel: vertical tower climbing with narrow ledges
/// </summary>
[CreateAssetMenu(fileName = "BiomeData", menuName = "Spells/Biome Data")]
public class BiomeData : ScriptableObject
{
    [Header("Identity")]
    public string biomeName = "New Biome";
    [TextArea(2, 4)]
    public string description = "Biome description.";

    [Header("Structure")]
    [Tooltip("Determines the overall level shape and gameplay dynamics")]
    public StructureType structureType = StructureType.Arena;
    [Tooltip("Total arena bounds (width x height)")]
    public Vector2 arenaBounds = new Vector2(42f, 25f);

    [Header("Ground")]
    [Tooltip("How much of the arena bottom is solid ground (0 = no ground, 1 = full floor)")]
    [Range(0f, 1f)] public float groundCoverage = 0.7f;
    [Tooltip("Number of gaps in the ground floor")]
    [Range(0, 5)] public int groundGapCount = 1;
    [Tooltip("Min/max width of ground gaps")]
    public Vector2 gapWidthRange = new Vector2(3f, 6f);
    [Tooltip("Thickness of ground platforms")]
    [Range(0.5f, 3f)] public float groundThickness = 1f;

    [Header("Platforms")]
    [Tooltip("Target number of floating/elevated platforms")]
    [Range(2, 20)] public int platformCount = 8;
    [Tooltip("Min/max platform width")]
    public Vector2 platformWidthRange = new Vector2(3f, 8f);
    [Tooltip("Min/max platform thickness")]
    public Vector2 platformThicknessRange = new Vector2(0.5f, 1f);
    [Tooltip("How many vertical tiers of platforms (2 = low/high, 4 = very layered)")]
    [Range(2, 5)] public int platformLevels = 3;
    [Tooltip("Maximum height above ground for the highest platform tier")]
    [Range(3f, 18f)] public float maxPlatformHeight = 12f;

    [Header("Walls")]
    [Tooltip("Number of walls for wall-sliding/jumping")]
    [Range(0, 8)] public int wallCount = 2;
    [Tooltip("Min/max wall height")]
    public Vector2 wallHeightRange = new Vector2(3f, 8f);
    [Tooltip("Wall thickness")]
    [Range(0.5f, 2f)] public float wallThickness = 1f;

    [Header("Ramps")]
    [Tooltip("Number of slope ramps")]
    [Range(0, 6)] public int rampCount = 2;
    [Tooltip("Min/max ramp angle in degrees")]
    public Vector2 rampAngleRange = new Vector2(15f, 35f);
    [Tooltip("Min/max ramp length")]
    public Vector2 rampLengthRange = new Vector2(3f, 6f);

    [Header("Hazards")]
    [Tooltip("Full-width kill zone at the bottom (lava, pit, etc.)")]
    public bool hasBottomKillZone = false;
    [Tooltip("Number of additional localized kill zones")]
    [Range(0, 4)] public int killZoneCount = 0;
    [Tooltip("Kill zone size range")]
    public Vector2 killZoneSizeRange = new Vector2(3f, 6f);

    [Header("Moving Platforms")]
    [Tooltip("Number of platforms that oscillate")]
    [Range(0, 6)] public int movingPlatformCount = 0;
    [Tooltip("Min/max movement amplitude (how far they travel)")]
    public Vector2 moveAmplitudeRange = new Vector2(1f, 3f);
    [Tooltip("Min/max movement speed")]
    public Vector2 moveSpeedRange = new Vector2(0.5f, 1.5f);
    [Tooltip("Primary movement axis for moving platforms")]
    public MovePlatformAxis movePlatformAxis = MovePlatformAxis.Vertical;

    [Header("Spawns")]
    [Range(0, 3)] public int monsterCount = 1;
    [Range(0, 5)] public int chestCount = 2;

    [Header("Visual Theme")]
    public Color groundColor = new Color(0.35f, 0.30f, 0.25f);
    public Color platformColor = new Color(0.45f, 0.40f, 0.35f);
    public Color wallColor = new Color(0.30f, 0.30f, 0.35f);
    public Color hazardColor = new Color(0.8f, 0.2f, 0.1f, 0.5f);
    public Color backgroundColor = new Color(0.08f, 0.06f, 0.12f);

    [Header("Boundary")]
    [Tooltip("Add boundary walls on left/right edges")]
    public bool hasBoundaryWalls = true;
    [Tooltip("Add ceiling")]
    public bool hasCeiling = true;
}

public enum StructureType
{
    /// <summary>2-3 ground tiers connected by ramps. Good vertical exploration.</summary>
    MultiLevel,
    /// <summary>Scattered platforms over a kill zone. Requires jumping skill.</summary>
    Islands,
    /// <summary>Long horizontal spans with gaps. Horizontal traversal focused.</summary>
    Bridge,
    /// <summary>Tall arena with platforms going up. Climbing focused.</summary>
    Vertical,
    /// <summary>Classic Smash-style: main platform with floating platforms above.</summary>
    Arena
}

public enum MovePlatformAxis
{
    Vertical,
    Horizontal,
    Both
}
