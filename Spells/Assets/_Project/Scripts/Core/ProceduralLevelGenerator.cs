using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural level generator using a 6-phase compositional pipeline.
///
/// Instead of rigid templates per structure type, this system:
///   1. Generates ground terrain based on biome parameters
///   2. Selects 3-5 architectural features from a weighted pool
///   3. Places features spatially using structure-type layout rules
///   4. Connects features with stepping-stone platforms for reachability
///   5. Perturbs all pieces for organic, hand-crafted variation
///   6. Places spawn points on valid surfaces
///
/// Features are small compositions (2-6 pieces) that create recognizable
/// gameplay moments: overlooks, towers, bridge spans, alcoves, etc.
/// The biome controls aesthetics (colors, sizes). The structure type
/// controls which features appear and where they're placed.
/// </summary>
public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BiomeData[] biomePool;
    [SerializeField] private MovementData movementData;

    [Header("Generation")]
    [Tooltip("Base seed for deterministic generation (0 = random)")]
    [SerializeField] private int baseSeed = 0;
    [Tooltip("Maximum validation retries before accepting")]
    [SerializeField] private int maxRetries = 10;

    // Reachability constants — must be achievable by player physics
    private const float MAX_JUMP_UP = 2.8f;
    private const float MAX_JUMP_HORIZONTAL = 5.0f;
    private const float COMFORTABLE_JUMP_UP = 2.0f;
    private const float COMFORTABLE_JUMP_H = 3.5f;
    private const float MIN_PLATFORM_SPACING = 1.5f;

    private System.Random rng;
    private int lastBiomeIndex = -1;

    /// <summary>The BiomeData used for the most recent generation.</summary>
    public BiomeData LastUsedBiome { get; private set; }

    // =========================================================
    // Feature System
    // =========================================================

    private enum FeatureType
    {
        Overlook,       // Elevated platform backed by wall
        Tower,          // Vertical column of platforms + walls
        BridgeSpan,     // Long narrow platform over open space
        Alcove,         // Platform enclosed on 2 sides
        Pit,            // Platforms around a hazard gap
        FeatureRamp,    // Slope connecting two heights
        Perch,          // Small isolated elevated platform
        StageCenter,    // Wide flat combat zone
        Corridor,       // Narrow passage between walls
        Ascent,         // Staircase of small platforms
        Chasm,          // Moving platform bridging areas
        Shelf           // Narrow ledge extending from wall
    }

    // Weight table: StructureType (row) x FeatureType (column)
    // Each row sums to ~1.0. Weights control probability of selection.
    private static readonly float[,] FeatureWeights = new float[5, 12]
    {
        // Overlook Tower  Bridge Alcove Pit    Ramp   Perch  Stage  Corr   Ascent Chasm  Shelf
        // MultiLevel
        { 0.12f,   0.10f, 0.05f, 0.12f, 0.06f, 0.16f, 0.06f, 0.12f, 0.04f, 0.10f, 0.02f, 0.05f },
        // Islands
        { 0.05f,   0.08f, 0.10f, 0.04f, 0.00f, 0.00f, 0.20f, 0.12f, 0.02f, 0.13f, 0.22f, 0.04f },
        // Bridge
        { 0.08f,   0.03f, 0.24f, 0.08f, 0.08f, 0.08f, 0.08f, 0.10f, 0.05f, 0.05f, 0.08f, 0.05f },
        // Vertical
        { 0.10f,   0.22f, 0.03f, 0.08f, 0.04f, 0.03f, 0.14f, 0.04f, 0.10f, 0.16f, 0.03f, 0.03f },
        // Arena
        { 0.10f,   0.05f, 0.08f, 0.12f, 0.05f, 0.10f, 0.10f, 0.22f, 0.04f, 0.05f, 0.04f, 0.05f }
    };

    // =========================================================
    // Public API
    // =========================================================

    /// <summary>
    /// Generate a level for the given round number. Selects a biome from the pool
    /// (avoiding the same biome twice in a row) and generates a layout.
    /// </summary>
    public ArenaLayoutData GenerateForRound(int roundNumber)
    {
        if (biomePool == null || biomePool.Length == 0)
        {
            Debug.LogError("ProceduralLevelGenerator: No biomes in pool!");
            return null;
        }

        int biomeIndex;
        if (biomePool.Length == 1)
        {
            biomeIndex = 0;
        }
        else
        {
            do
            {
                biomeIndex = (baseSeed == 0)
                    ? Random.Range(0, biomePool.Length)
                    : new System.Random(baseSeed + roundNumber).Next(0, biomePool.Length);
            } while (biomeIndex == lastBiomeIndex && biomePool.Length > 1);
        }
        lastBiomeIndex = biomeIndex;
        LastUsedBiome = biomePool[biomeIndex];

        int seed = (baseSeed == 0) ? System.Environment.TickCount + roundNumber : baseSeed + roundNumber * 1000;
        return Generate(biomePool[biomeIndex], seed);
    }

    /// <summary>
    /// Generate an ArenaLayoutData from a BiomeData and seed.
    /// Retries with different seeds if validation fails.
    /// </summary>
    public ArenaLayoutData Generate(BiomeData biome, int seed)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            rng = new System.Random(seed + attempt);
            var layout = GenerateLayout(biome);

            if (LevelValidator.Validate(layout))
            {
                Debug.Log($"ProceduralLevelGenerator: Generated '{biome.biomeName}' (seed={seed + attempt}, " +
                          $"{layout.pieces.Length} pieces, attempt {attempt + 1})");
                return layout;
            }

            Debug.LogWarning($"ProceduralLevelGenerator: Validation failed (attempt {attempt + 1}), retrying...");
        }

        Debug.LogWarning("ProceduralLevelGenerator: Max retries reached, using last generated layout.");
        rng = new System.Random(seed);
        return GenerateLayout(biome);
    }

    // =========================================================
    // 6-Phase Pipeline
    // =========================================================

    private ArenaLayoutData GenerateLayout(BiomeData biome)
    {
        var layout = ScriptableObject.CreateInstance<ArenaLayoutData>();
        layout.arenaName = $"{biome.biomeName}_{rng.Next(1000, 9999)}";
        layout.description = biome.description;
        layout.arenaBounds = biome.arenaBounds;
        layout.monsterCount = biome.monsterCount;
        layout.chestCount = biome.chestCount;

        var pieces = new List<ArenaPlacement>();
        float halfW = biome.arenaBounds.x / 2f;
        float halfH = biome.arenaBounds.y / 2f;
        float groundY = -halfH + biome.groundThickness / 2f;

        // Phase 1: Ground terrain
        var groundSurfaces = GenerateGround(pieces, biome, halfW, halfH, groundY);

        // Phase 2: Feature selection
        var features = SelectFeatures(biome);

        // Phase 3: Feature placement
        var featureOrigins = PlaceFeatures(pieces, biome, features, groundSurfaces, halfW, halfH, groundY);

        // Phase 4: Connection pass
        ConnectFeatures(pieces, biome, halfW, halfH, groundY);

        // Phase 5: Perturbation
        ApplyPerturbation(pieces);

        // Boundaries (after perturbation so they stay clean)
        if (biome.hasBoundaryWalls)
            GenerateBoundaries(pieces, biome, halfW, halfH);

        // Hazards
        GenerateHazards(pieces, biome, halfW, halfH, groundY);

        // Phase 6: Spawn points
        GenerateSpawnPoints(pieces, biome, halfW, groundY);

        layout.pieces = pieces.ToArray();
        return layout;
    }

    // =========================================================
    // Phase 1: Ground Terrain
    // =========================================================

    /// <summary>
    /// Generate ground segments based on biome parameters and structure type.
    /// Returns list of ground surface positions (top of ground segments).
    /// </summary>
    private List<Vector2> GenerateGround(List<ArenaPlacement> pieces, BiomeData biome,
        float halfW, float halfH, float groundY)
    {
        var surfaces = new List<Vector2>();

        // Islands: no ground at all
        if (biome.structureType == StructureType.Islands || biome.groundCoverage <= 0.05f)
            return surfaces;

        float totalWidth = biome.arenaBounds.x * biome.groundCoverage;
        int gapCount = biome.groundGapCount;

        // For Vertical, narrow ground floor
        if (biome.structureType == StructureType.Vertical)
        {
            totalWidth = Mathf.Min(totalWidth, biome.arenaBounds.x * 0.5f);
            gapCount = Mathf.Min(gapCount, 1);
        }

        if (gapCount <= 0)
        {
            // Single continuous ground segment
            // Add slight asymmetry — offset from center
            float offset = RandomRange(-1.5f, 1.5f);
            AddPlatformPiece(pieces, "Ground", new Vector2(offset, groundY),
                totalWidth, biome.groundThickness, biome.groundColor);
            surfaces.Add(new Vector2(offset, groundY + biome.groundThickness / 2f));

            // For MultiLevel, add 1-2 elevated ground tiers
            if (biome.structureType == StructureType.MultiLevel)
            {
                int extraTiers = 1 + rng.Next(2); // 1-2 extra tiers
                for (int t = 0; t < extraTiers; t++)
                {
                    float tierWidth = totalWidth * RandomRange(0.25f, 0.45f);
                    float tierY = groundY + (t + 1) * (COMFORTABLE_JUMP_UP + RandomRange(0.3f, 1.0f));
                    float tierX = RandomRange(-halfW * 0.4f, halfW * 0.4f);
                    tierX = Mathf.Clamp(tierX, -halfW + tierWidth / 2f + 1f, halfW - tierWidth / 2f - 1f);

                    AddPlatformPiece(pieces, $"GroundTier_{t}", new Vector2(tierX, tierY),
                        tierWidth, biome.groundThickness * RandomRange(0.8f, 1.2f), biome.groundColor);
                    surfaces.Add(new Vector2(tierX, tierY + biome.groundThickness / 2f));
                }
            }
            return surfaces;
        }

        // Multiple ground segments with gaps
        // Calculate gap widths first
        var gapWidths = new List<float>();
        float totalGapWidth = 0f;
        for (int g = 0; g < gapCount; g++)
        {
            float gw = RandRange(biome.gapWidthRange);
            gapWidths.Add(gw);
            totalGapWidth += gw;
        }

        int segments = gapCount + 1;
        float segmentableWidth = Mathf.Max(totalWidth - totalGapWidth, segments * 4f);
        float baseSegWidth = segmentableWidth / segments;

        // Place segments left to right
        float cursor = -totalWidth / 2f - totalGapWidth / 2f;

        for (int i = 0; i < segments; i++)
        {
            // Vary segment width for organic feel
            float segWidth = baseSegWidth * RandomRange(0.75f, 1.25f);
            segWidth = Mathf.Max(segWidth, 3.5f);

            // Height variation for MultiLevel/Bridge
            float segY = groundY;
            if (biome.structureType == StructureType.MultiLevel)
                segY += i * RandomRange(0.5f, 1.5f);
            else if (biome.structureType == StructureType.Bridge)
                segY += RandomRange(-0.3f, 0.3f);

            float x = cursor + segWidth / 2f;
            x = Mathf.Clamp(x, -halfW + segWidth / 2f + 0.5f, halfW - segWidth / 2f - 0.5f);

            AddPlatformPiece(pieces, $"GroundSeg_{i}", new Vector2(x, segY),
                segWidth, biome.groundThickness, biome.groundColor);
            surfaces.Add(new Vector2(x, segY + biome.groundThickness / 2f));

            cursor += segWidth;

            // Add gap
            if (i < gapWidths.Count)
                cursor += gapWidths[i];
        }

        return surfaces;
    }

    // =========================================================
    // Phase 2: Feature Selection
    // =========================================================

    /// <summary>
    /// Select 3-5 features using weighted probabilities based on structure type.
    /// </summary>
    private List<FeatureType> SelectFeatures(BiomeData biome)
    {
        int structIdx = (int)biome.structureType;
        int featureCount = 3 + rng.Next(3); // 3-5 features

        // Scale feature count with platform count
        if (biome.platformCount >= 12) featureCount = Mathf.Max(featureCount, 4);
        if (biome.platformCount <= 6) featureCount = Mathf.Min(featureCount, 3);

        var selected = new List<FeatureType>();
        var usedTypes = new HashSet<FeatureType>();

        // First feature: always pick the highest-weighted one for this structure
        // This anchors the level's identity
        float bestWeight = 0f;
        FeatureType anchor = FeatureType.StageCenter;
        for (int f = 0; f < 12; f++)
        {
            if (FeatureWeights[structIdx, f] > bestWeight)
            {
                bestWeight = FeatureWeights[structIdx, f];
                anchor = (FeatureType)f;
            }
        }
        selected.Add(anchor);
        usedTypes.Add(anchor);

        // Remaining features: weighted random selection
        for (int i = 1; i < featureCount; i++)
        {
            FeatureType pick = WeightedRandomFeature(structIdx, usedTypes);
            selected.Add(pick);
            // Allow some duplicates but not of rare features
            if (FeatureWeights[structIdx, (int)pick] < 0.10f)
                usedTypes.Add(pick);
        }

        return selected;
    }

    private FeatureType WeightedRandomFeature(int structIdx, HashSet<FeatureType> exclude)
    {
        // Build cumulative distribution excluding used types
        float totalWeight = 0f;
        for (int f = 0; f < 12; f++)
        {
            if (!exclude.Contains((FeatureType)f))
                totalWeight += FeatureWeights[structIdx, f];
        }

        if (totalWeight <= 0f)
        {
            // All excluded — allow any
            return (FeatureType)rng.Next(12);
        }

        float roll = (float)rng.NextDouble() * totalWeight;
        float cumulative = 0f;
        for (int f = 0; f < 12; f++)
        {
            if (exclude.Contains((FeatureType)f)) continue;
            cumulative += FeatureWeights[structIdx, f];
            if (roll <= cumulative)
                return (FeatureType)f;
        }

        return FeatureType.StageCenter; // fallback
    }

    // =========================================================
    // Phase 3: Feature Placement
    // =========================================================

    /// <summary>
    /// Place selected features in the arena using structure-type spatial rules.
    /// Returns the origin points where features were placed.
    /// </summary>
    private List<Vector2> PlaceFeatures(List<ArenaPlacement> pieces, BiomeData biome,
        List<FeatureType> features, List<Vector2> groundSurfaces,
        float halfW, float halfH, float groundY)
    {
        var origins = new List<Vector2>();
        float groundTop = groundY + biome.groundThickness / 2f;

        switch (biome.structureType)
        {
            case StructureType.Arena:
                PlaceFeaturesArena(pieces, biome, features, origins, halfW, halfH, groundTop);
                break;
            case StructureType.MultiLevel:
                PlaceFeaturesMultiLevel(pieces, biome, features, origins, groundSurfaces, halfW, halfH, groundTop);
                break;
            case StructureType.Islands:
                PlaceFeaturesIslands(pieces, biome, features, origins, halfW, halfH, groundY);
                break;
            case StructureType.Bridge:
                PlaceFeaturesBridge(pieces, biome, features, origins, groundSurfaces, halfW, halfH, groundTop);
                break;
            case StructureType.Vertical:
                PlaceFeaturesVertical(pieces, biome, features, origins, halfW, halfH, groundTop);
                break;
        }

        return origins;
    }

    /// <summary>Arena: features spread across horizontal space at varied heights around center.</summary>
    private void PlaceFeaturesArena(List<ArenaPlacement> pieces, BiomeData biome,
        List<FeatureType> features, List<Vector2> origins, float halfW, float halfH, float groundTop)
    {
        float usableW = halfW * 0.8f;
        int count = features.Count;

        for (int i = 0; i < count; i++)
        {
            // Distribute horizontally with some randomness
            float t = (float)i / Mathf.Max(1, count - 1); // 0 to 1
            float x = Mathf.Lerp(-usableW, usableW, t);
            x += RandomRange(-2f, 2f); // jitter

            // Height varies: lower near edges, higher near center
            float centerFactor = 1f - Mathf.Abs(t - 0.5f) * 2f; // 0 at edges, 1 at center
            float baseHeight = COMFORTABLE_JUMP_UP + centerFactor * biome.maxPlatformHeight * 0.4f;
            float y = groundTop + baseHeight + RandomRange(-1f, 1.5f);
            y = Mathf.Clamp(y, groundTop + 1f, groundTop + biome.maxPlatformHeight);

            x = Mathf.Clamp(x, -halfW + 3f, halfW - 3f);

            var origin = new Vector2(x, y);
            BuildFeature(pieces, features[i], origin, biome);
            origins.Add(origin);
        }
    }

    /// <summary>MultiLevel: features placed on ascending tiers.</summary>
    private void PlaceFeaturesMultiLevel(List<ArenaPlacement> pieces, BiomeData biome,
        List<FeatureType> features, List<Vector2> origins, List<Vector2> groundSurfaces,
        float halfW, float halfH, float groundTop)
    {
        // Ascending direction — randomize
        bool ascendRight = rng.Next(2) == 0;
        int count = features.Count;
        float usableW = halfW * 0.75f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / Mathf.Max(1, count - 1);
            float progressX = ascendRight ? Mathf.Lerp(-usableW, usableW, t) : Mathf.Lerp(usableW, -usableW, t);
            float progressY = groundTop + t * biome.maxPlatformHeight * 0.7f;

            float x = progressX + RandomRange(-2.5f, 2.5f);
            float y = progressY + RandomRange(-0.5f, 1.5f);

            x = Mathf.Clamp(x, -halfW + 3f, halfW - 3f);
            y = Mathf.Clamp(y, groundTop + 0.5f, groundTop + biome.maxPlatformHeight);

            var origin = new Vector2(x, y);
            BuildFeature(pieces, features[i], origin, biome);
            origins.Add(origin);
        }
    }

    /// <summary>Islands: features scattered across arena as floating clusters.</summary>
    private void PlaceFeaturesIslands(List<ArenaPlacement> pieces, BiomeData biome,
        List<FeatureType> features, List<Vector2> origins, float halfW, float halfH, float groundY)
    {
        float usableW = halfW * 0.75f;
        float usableH = halfH * 0.6f;
        float centerY = groundY + biome.maxPlatformHeight * 0.4f;
        int count = features.Count;

        // First feature near center
        var firstOrigin = new Vector2(RandomRange(-2f, 2f), centerY + RandomRange(-1f, 1f));
        BuildFeature(pieces, features[0], firstOrigin, biome);
        origins.Add(firstOrigin);

        // Remaining features placed at reachable distance from existing
        for (int i = 1; i < count; i++)
        {
            Vector2 anchor = origins[rng.Next(origins.Count)];
            float angle = RandomRange(0f, 360f) * Mathf.Deg2Rad;
            float dist = RandomRange(MAX_JUMP_HORIZONTAL * 0.6f, MAX_JUMP_HORIZONTAL * 1.5f);

            float x = anchor.x + Mathf.Cos(angle) * dist;
            float y = anchor.y + Mathf.Sin(angle) * dist * 0.4f; // bias horizontal

            x = Mathf.Clamp(x, -usableW, usableW);
            y = Mathf.Clamp(y, groundY + 2f, groundY + biome.maxPlatformHeight);

            var origin = new Vector2(x, y);
            BuildFeature(pieces, features[i], origin, biome);
            origins.Add(origin);
        }
    }

    /// <summary>Bridge: features arranged along horizontal axis above/around gaps.</summary>
    private void PlaceFeaturesBridge(List<ArenaPlacement> pieces, BiomeData biome,
        List<FeatureType> features, List<Vector2> origins, List<Vector2> groundSurfaces,
        float halfW, float halfH, float groundTop)
    {
        float usableW = halfW * 0.85f;
        int count = features.Count;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / Mathf.Max(1, count - 1);
            float x = Mathf.Lerp(-usableW, usableW, t);
            x += RandomRange(-3f, 3f);

            // Height: mix of ground-level and elevated
            float y;
            if (i % 3 == 0)
                y = groundTop + RandomRange(0.5f, COMFORTABLE_JUMP_UP); // Near ground
            else
                y = groundTop + COMFORTABLE_JUMP_UP + RandomRange(0f, biome.maxPlatformHeight * 0.4f);

            x = Mathf.Clamp(x, -halfW + 3f, halfW - 3f);
            y = Mathf.Clamp(y, groundTop + 0.5f, groundTop + biome.maxPlatformHeight);

            var origin = new Vector2(x, y);
            BuildFeature(pieces, features[i], origin, biome);
            origins.Add(origin);
        }
    }

    /// <summary>Vertical: features stacked in a narrow column, alternating sides.</summary>
    private void PlaceFeaturesVertical(List<ArenaPlacement> pieces, BiomeData biome,
        List<FeatureType> features, List<Vector2> origins, float halfW, float halfH, float groundTop)
    {
        float usableW = halfW * 0.6f; // Narrower for tower feel
        int count = features.Count;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / Mathf.Max(1, count - 1);

            // Alternate sides
            float side = (i % 2 == 0) ? -1f : 1f;
            float x = side * RandomRange(usableW * 0.15f, usableW * 0.6f);

            // Ascending
            float y = groundTop + COMFORTABLE_JUMP_UP + t * biome.maxPlatformHeight * 0.8f;
            y += RandomRange(-0.5f, 0.5f);

            x = Mathf.Clamp(x, -halfW + 3f, halfW - 3f);
            y = Mathf.Clamp(y, groundTop + 1f, groundTop + biome.maxPlatformHeight);

            var origin = new Vector2(x, y);
            BuildFeature(pieces, features[i], origin, biome);
            origins.Add(origin);
        }
    }

    // =========================================================
    // Feature Builders
    // =========================================================

    /// <summary>Dispatch to the appropriate feature builder method.</summary>
    private void BuildFeature(List<ArenaPlacement> pieces, FeatureType type, Vector2 origin, BiomeData biome)
    {
        switch (type)
        {
            case FeatureType.Overlook: BuildOverlook(pieces, origin, biome); break;
            case FeatureType.Tower: BuildTower(pieces, origin, biome); break;
            case FeatureType.BridgeSpan: BuildBridgeSpan(pieces, origin, biome); break;
            case FeatureType.Alcove: BuildAlcove(pieces, origin, biome); break;
            case FeatureType.Pit: BuildPit(pieces, origin, biome); break;
            case FeatureType.FeatureRamp: BuildFeatureRamp(pieces, origin, biome); break;
            case FeatureType.Perch: BuildPerch(pieces, origin, biome); break;
            case FeatureType.StageCenter: BuildStageCenter(pieces, origin, biome); break;
            case FeatureType.Corridor: BuildCorridor(pieces, origin, biome); break;
            case FeatureType.Ascent: BuildAscent(pieces, origin, biome); break;
            case FeatureType.Chasm: BuildChasm(pieces, origin, biome); break;
            case FeatureType.Shelf: BuildShelf(pieces, origin, biome); break;
        }
    }

    /// <summary>Overlook: elevated wide platform with a wall behind it. Defensive perch.</summary>
    private void BuildOverlook(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        float platWidth = RandRange(biome.platformWidthRange) * RandomRange(1.0f, 1.3f);
        float platThick = RandRange(biome.platformThicknessRange);
        float wallH = RandRange(biome.wallHeightRange);

        // Platform
        AddPlatformPiece(pieces, "Overlook_Plat", origin, platWidth, platThick, biome.platformColor);

        // Wall behind (random side)
        float wallSide = (rng.Next(2) == 0) ? -1f : 1f;
        float wallX = origin.x + wallSide * (platWidth / 2f + biome.wallThickness / 2f);
        float wallY = origin.y + wallH / 2f;

        AddWallPiece(pieces, "Overlook_Wall", new Vector2(wallX, wallY),
            biome.wallThickness, wallH, biome.wallColor);
    }

    /// <summary>Tower: 2-3 stacked platforms with walls for wall-jumping.</summary>
    private void BuildTower(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        int levels = 2 + rng.Next(2); // 2-3 levels
        float platWidth = RandRange(biome.platformWidthRange) * RandomRange(0.7f, 1.0f);
        float platThick = RandRange(biome.platformThicknessRange);
        float spacing = COMFORTABLE_JUMP_UP + RandomRange(0f, 0.5f);

        for (int i = 0; i < levels; i++)
        {
            float y = origin.y + i * spacing;
            // Slight horizontal offset per level for visual interest
            float dx = RandomRange(-0.5f, 0.5f);
            // Platforms narrow slightly as they go up
            float w = platWidth * (1f - i * 0.1f);

            AddPlatformPiece(pieces, $"Tower_Plat_{i}", new Vector2(origin.x + dx, y),
                w, platThick, biome.platformColor);
        }

        // Wall on one or both sides
        float totalHeight = (levels - 1) * spacing + 1f;
        float wallSide = (rng.Next(2) == 0) ? -1f : 1f;
        float wallX = origin.x + wallSide * (platWidth / 2f + biome.wallThickness / 2f + 0.2f);
        float wallY = origin.y + totalHeight / 2f;

        AddWallPiece(pieces, "Tower_Wall", new Vector2(wallX, wallY),
            biome.wallThickness, totalHeight * RandomRange(0.7f, 1.0f), biome.wallColor);

        // Occasional second wall on other side
        if (rng.Next(3) == 0)
        {
            float wallX2 = origin.x - wallSide * (platWidth / 2f + biome.wallThickness / 2f + 0.2f);
            AddWallPiece(pieces, "Tower_Wall2", new Vector2(wallX2, wallY),
                biome.wallThickness, totalHeight * RandomRange(0.5f, 0.8f), biome.wallColor);
        }
    }

    /// <summary>BridgeSpan: long narrow platform for crossing open space.</summary>
    private void BuildBridgeSpan(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        // Extra wide, thinner than normal
        float width = RandRange(biome.platformWidthRange) * RandomRange(1.4f, 2.0f);
        width = Mathf.Min(width, 14f); // cap at reasonable length
        float thick = RandRange(biome.platformThicknessRange) * RandomRange(0.6f, 0.8f);

        AddPlatformPiece(pieces, "BridgeSpan_Plat", origin, width, thick, biome.platformColor);

        // Optional support wall at one end
        if (rng.Next(2) == 0)
        {
            float side = (rng.Next(2) == 0) ? -1f : 1f;
            float wallX = origin.x + side * (width / 2f - 0.5f);
            float wallH = RandomRange(1.5f, 3f);
            AddWallPiece(pieces, "BridgeSpan_Support", new Vector2(wallX, origin.y - wallH / 2f),
                biome.wallThickness * 0.8f, wallH, biome.wallColor);
        }
    }

    /// <summary>Alcove: platform enclosed on two sides by walls. Defensive position.</summary>
    private void BuildAlcove(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        float platWidth = RandRange(biome.platformWidthRange) * RandomRange(0.8f, 1.1f);
        float platThick = RandRange(biome.platformThicknessRange);
        float wallH = RandRange(biome.wallHeightRange) * RandomRange(0.7f, 1.0f);

        // Platform
        AddPlatformPiece(pieces, "Alcove_Plat", origin, platWidth, platThick, biome.platformColor);

        // Walls on both sides
        float wallOffset = platWidth / 2f + biome.wallThickness / 2f;

        AddWallPiece(pieces, "Alcove_WallL",
            new Vector2(origin.x - wallOffset, origin.y + wallH / 2f),
            biome.wallThickness, wallH, biome.wallColor);

        AddWallPiece(pieces, "Alcove_WallR",
            new Vector2(origin.x + wallOffset, origin.y + wallH / 2f),
            biome.wallThickness, wallH, biome.wallColor);
    }

    /// <summary>Pit: platforms around a hazard gap. Risk/reward area.</summary>
    private void BuildPit(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        float pitWidth = RandRange(biome.killZoneSizeRange);
        float platWidth = RandRange(biome.platformWidthRange) * RandomRange(0.7f, 0.9f);
        float platThick = RandRange(biome.platformThicknessRange);

        // Rim platforms on left and right of pit
        float rimOffset = pitWidth / 2f + platWidth / 2f + 0.3f;

        AddPlatformPiece(pieces, "Pit_RimL", new Vector2(origin.x - rimOffset, origin.y),
            platWidth, platThick, biome.platformColor);
        AddPlatformPiece(pieces, "Pit_RimR", new Vector2(origin.x + rimOffset, origin.y),
            platWidth, platThick, biome.platformColor);

        // Optional upper platform over the pit (high-risk combat area)
        if (rng.Next(2) == 0)
        {
            float upperY = origin.y + COMFORTABLE_JUMP_UP + RandomRange(0f, 1f);
            float upperW = pitWidth * RandomRange(0.5f, 0.8f);
            AddPlatformPiece(pieces, "Pit_Upper", new Vector2(origin.x, upperY),
                upperW, platThick * 0.8f, biome.platformColor);
        }

        // Kill zone in the pit (localized)
        AddKillZonePiece(pieces, "Pit_Hazard",
            new Vector2(origin.x, origin.y - 2f), pitWidth, 2.5f, biome.hazardColor);
    }

    /// <summary>FeatureRamp: slope connecting two heights with a landing platform.</summary>
    private void BuildFeatureRamp(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        float rampLen = RandRange(biome.rampLengthRange);
        float rampAngle = RandRange(biome.rampAngleRange);
        float rampH = rampLen * Mathf.Sin(rampAngle * Mathf.Deg2Rad);
        float rotation = (rng.Next(2) == 0) ? 0f : 180f;

        // Ramp
        AddRampPiece(pieces, "Ramp_Slope", origin, rampLen, rampH, rampAngle, rotation, biome.groundColor);

        // Landing platform at top of ramp
        float landingSide = (rotation == 0f) ? -1f : 1f;
        float landingX = origin.x + landingSide * (rampLen / 2f + 1f);
        float landingY = origin.y + rampH / 2f + RandomRange(0f, 0.3f);
        float landingW = RandRange(biome.platformWidthRange) * RandomRange(0.6f, 0.9f);

        AddPlatformPiece(pieces, "Ramp_Landing",
            new Vector2(landingX, landingY), landingW,
            RandRange(biome.platformThicknessRange), biome.platformColor);
    }

    /// <summary>Perch: small isolated platform. Sniper position.</summary>
    private void BuildPerch(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        // Small platform
        float width = biome.platformWidthRange.x * RandomRange(0.6f, 1.0f);
        float thick = RandRange(biome.platformThicknessRange);

        AddPlatformPiece(pieces, "Perch_Plat", origin, width, thick, biome.platformColor);
    }

    /// <summary>StageCenter: wide flat combat zone. The main fighting area.</summary>
    private void BuildStageCenter(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        float width = RandRange(biome.platformWidthRange) * RandomRange(1.5f, 2.2f);
        width = Mathf.Min(width, biome.arenaBounds.x * 0.4f);
        float thick = RandRange(biome.platformThicknessRange) * RandomRange(1.0f, 1.3f);

        AddPlatformPiece(pieces, "Stage_Plat", origin, width, thick, biome.platformColor);

        // Optional low walls at edges (not full enclosure)
        if (rng.Next(3) != 0)
        {
            float wallH = RandRange(biome.wallHeightRange) * RandomRange(0.4f, 0.6f);
            float wallOffset = width / 2f + biome.wallThickness / 2f;

            // Just one wall
            float wallSide = (rng.Next(2) == 0) ? -1f : 1f;
            AddWallPiece(pieces, "Stage_Wall",
                new Vector2(origin.x + wallSide * wallOffset, origin.y + wallH / 2f),
                biome.wallThickness, wallH, biome.wallColor);
        }
    }

    /// <summary>Corridor: narrow passage between two walls.</summary>
    private void BuildCorridor(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        float platWidth = RandRange(biome.platformWidthRange) * RandomRange(0.5f, 0.7f);
        float platThick = RandRange(biome.platformThicknessRange);
        float wallH = RandRange(biome.wallHeightRange) * RandomRange(0.8f, 1.1f);

        // Narrow platform
        AddPlatformPiece(pieces, "Corr_Plat", origin, platWidth, platThick, biome.platformColor);

        // Tall walls on both sides
        float wallOffset = platWidth / 2f + biome.wallThickness / 2f + 0.1f;

        AddWallPiece(pieces, "Corr_WallL",
            new Vector2(origin.x - wallOffset, origin.y + wallH / 2f),
            biome.wallThickness, wallH, biome.wallColor);
        AddWallPiece(pieces, "Corr_WallR",
            new Vector2(origin.x + wallOffset, origin.y + wallH / 2f),
            biome.wallThickness, wallH, biome.wallColor);
    }

    /// <summary>Ascent: 3-4 stepping platforms going upward. Vertical traversal.</summary>
    private void BuildAscent(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        int steps = 3 + rng.Next(2); // 3-4 steps
        float stepHeight = COMFORTABLE_JUMP_UP * RandomRange(0.7f, 1.0f);
        float drift = RandomRange(1f, COMFORTABLE_JUMP_H * 0.7f);
        float driftDir = (rng.Next(2) == 0) ? 1f : -1f;

        for (int i = 0; i < steps; i++)
        {
            float x = origin.x + i * drift * driftDir * RandomRange(0.6f, 1.2f);
            float y = origin.y + i * stepHeight;
            float w = RandRange(biome.platformWidthRange) * RandomRange(0.6f, 0.9f);
            // Steps get slightly smaller as they go up
            w *= (1f - i * 0.08f);
            float t = RandRange(biome.platformThicknessRange);

            AddPlatformPiece(pieces, $"Ascent_Step_{i}", new Vector2(x, y), w, t, biome.platformColor);
        }
    }

    /// <summary>Chasm: a moving platform bridging two areas. Timing challenge.</summary>
    private void BuildChasm(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        float mpWidth = RandRange(biome.platformWidthRange) * RandomRange(0.6f, 0.8f);
        float mpThick = RandRange(biome.platformThicknessRange);
        float amplitude = RandRange(biome.moveAmplitudeRange);
        float speed = RandRange(biome.moveSpeedRange);

        Vector2 moveDir;
        switch (biome.movePlatformAxis)
        {
            case MovePlatformAxis.Horizontal: moveDir = Vector2.right; break;
            case MovePlatformAxis.Both:
                moveDir = (rng.Next(2) == 0) ? Vector2.right : Vector2.up;
                break;
            default: moveDir = Vector2.up; break;
        }

        float phase = RandomRange(0f, 1f);

        AddMovingPlatformPiece(pieces, "Chasm_MovPlat", origin, mpWidth, mpThick,
            moveDir, amplitude, speed, phase, biome.platformColor * 1.15f);

        // Landing platforms on either side of the chasm
        float offset = amplitude + mpWidth / 2f + 2f;
        float landingW = RandRange(biome.platformWidthRange) * RandomRange(0.6f, 0.8f);
        float landingThick = RandRange(biome.platformThicknessRange);

        if (moveDir.x > 0.5f)
        {
            // Horizontal chasm: platforms above and below
            AddPlatformPiece(pieces, "Chasm_LandA",
                new Vector2(origin.x - offset, origin.y + RandomRange(-0.5f, 0.5f)),
                landingW, landingThick, biome.platformColor);
            AddPlatformPiece(pieces, "Chasm_LandB",
                new Vector2(origin.x + offset, origin.y + RandomRange(-0.5f, 0.5f)),
                landingW, landingThick, biome.platformColor);
        }
        else
        {
            // Vertical chasm: platforms left and right
            AddPlatformPiece(pieces, "Chasm_LandA",
                new Vector2(origin.x + RandomRange(-2f, 2f), origin.y - offset),
                landingW, landingThick, biome.platformColor);
            AddPlatformPiece(pieces, "Chasm_LandB",
                new Vector2(origin.x + RandomRange(-2f, 2f), origin.y + offset),
                landingW, landingThick, biome.platformColor);
        }
    }

    /// <summary>Shelf: narrow ledge extending from a wall. Wall-jump target.</summary>
    private void BuildShelf(List<ArenaPlacement> pieces, Vector2 origin, BiomeData biome)
    {
        float platWidth = biome.platformWidthRange.x * RandomRange(0.5f, 0.8f);
        float platThick = RandRange(biome.platformThicknessRange) * 0.7f;
        float wallH = RandRange(biome.wallHeightRange) * RandomRange(0.6f, 0.9f);

        // Wall
        float wallSide = (rng.Next(2) == 0) ? -1f : 1f;
        float wallX = origin.x + wallSide * (platWidth / 2f + biome.wallThickness / 2f);

        AddWallPiece(pieces, "Shelf_Wall", new Vector2(wallX, origin.y - wallH * 0.3f),
            biome.wallThickness, wallH, biome.wallColor);

        // Ledge extending out from the wall
        float shelfX = origin.x - wallSide * (platWidth * 0.2f);
        AddPlatformPiece(pieces, "Shelf_Ledge", new Vector2(shelfX, origin.y),
            platWidth, platThick, biome.platformColor);
    }

    // =========================================================
    // Phase 4: Connection Pass
    // =========================================================

    /// <summary>
    /// Ensure all placed platforms are connected via reachable jumps.
    /// Adds stepping-stone platforms or moving platforms where gaps exist.
    /// </summary>
    private void ConnectFeatures(List<ArenaPlacement> pieces, BiomeData biome,
        float halfW, float halfH, float groundY)
    {
        // Gather all platform-like surfaces
        var platforms = new List<PlatformInfo>();
        foreach (var p in pieces)
        {
            if (p.piece == null) continue;
            var type = p.piece.pieceType;
            if (type == ArenaPieceData.PieceType.Platform ||
                type == ArenaPieceData.PieceType.Bridge ||
                type == ArenaPieceData.PieceType.MovingPlatform)
            {
                platforms.Add(new PlatformInfo
                {
                    center = p.position,
                    width = p.piece.size.x,
                    topY = p.position.y + p.piece.size.y / 2f
                });
            }
        }

        if (platforms.Count < 2) return;

        // Build reachability graph
        int n = platforms.Count;
        var reachable = new bool[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                if (CanReach(platforms[i], platforms[j]))
                {
                    reachable[i, j] = true;
                    reachable[j, i] = true;
                }
            }
        }

        // BFS from platform 0 to find all reachable platforms
        var visited = new bool[n];
        var queue = new Queue<int>();
        queue.Enqueue(0);
        visited[0] = true;
        while (queue.Count > 0)
        {
            int curr = queue.Dequeue();
            for (int j = 0; j < n; j++)
            {
                if (!visited[j] && reachable[curr, j])
                {
                    visited[j] = true;
                    queue.Enqueue(j);
                }
            }
        }

        // For unreachable platforms, add connecting stepping stones
        int movingPlatformsUsed = 0;
        for (int i = 0; i < n; i++)
        {
            if (visited[i]) continue;

            // Find nearest reachable platform
            float bestDist = float.MaxValue;
            int bestReachable = 0;
            for (int j = 0; j < n; j++)
            {
                if (!visited[j]) continue;
                float d = Vector2.Distance(platforms[i].center, platforms[j].center);
                if (d < bestDist) { bestDist = d; bestReachable = j; }
            }

            // Create stepping stone(s) between them
            Vector2 from = platforms[bestReachable].center;
            Vector2 to = platforms[i].center;
            float totalDist = Vector2.Distance(from, to);
            int stepsNeeded = Mathf.CeilToInt(totalDist / MAX_JUMP_HORIZONTAL);
            stepsNeeded = Mathf.Min(stepsNeeded, 4); // cap at 4 steps

            for (int s = 1; s <= stepsNeeded; s++)
            {
                float t = (float)s / (stepsNeeded + 1);
                Vector2 stepPos = Vector2.Lerp(from, to, t);
                stepPos.x += RandomRange(-0.5f, 0.5f);
                stepPos.y += RandomRange(-0.3f, 0.3f);

                // Clamp within bounds
                float stepW = RandRange(biome.platformWidthRange) * 0.6f;
                stepPos.x = Mathf.Clamp(stepPos.x, -halfW + stepW / 2f + 1f, halfW - stepW / 2f - 1f);

                // Use moving platform occasionally for variety
                if (movingPlatformsUsed < biome.movingPlatformCount && rng.Next(3) == 0)
                {
                    float amp = RandRange(biome.moveAmplitudeRange) * 0.5f;
                    float spd = RandRange(biome.moveSpeedRange);
                    Vector2 dir = (rng.Next(2) == 0) ? Vector2.up : Vector2.right;
                    AddMovingPlatformPiece(pieces, $"Connect_MP_{i}_{s}", stepPos,
                        stepW, RandRange(biome.platformThicknessRange),
                        dir, amp, spd, RandomRange(0f, 1f), biome.platformColor * 1.1f);
                    movingPlatformsUsed++;
                }
                else
                {
                    AddPlatformPiece(pieces, $"Connect_Step_{i}_{s}", stepPos,
                        stepW, RandRange(biome.platformThicknessRange), biome.platformColor);
                }
            }

            // Mark as now reachable
            visited[i] = true;
        }

        // Add remaining moving platforms if biome wants more
        while (movingPlatformsUsed < biome.movingPlatformCount && platforms.Count >= 2)
        {
            // Place between two random platforms
            int a = rng.Next(platforms.Count);
            int b = rng.Next(platforms.Count);
            if (a == b) b = (b + 1) % platforms.Count;

            Vector2 mid = (platforms[a].center + platforms[b].center) / 2f;
            float amp = RandRange(biome.moveAmplitudeRange);
            float spd = RandRange(biome.moveSpeedRange);
            Vector2 dir;
            switch (biome.movePlatformAxis)
            {
                case MovePlatformAxis.Horizontal: dir = Vector2.right; break;
                case MovePlatformAxis.Both:
                    dir = (rng.Next(2) == 0) ? Vector2.right : Vector2.up; break;
                default: dir = Vector2.up; break;
            }

            AddMovingPlatformPiece(pieces, $"Extra_MP_{movingPlatformsUsed}", mid,
                RandRange(biome.platformWidthRange) * 0.7f,
                RandRange(biome.platformThicknessRange),
                dir, amp, spd,
                (float)movingPlatformsUsed / Mathf.Max(1, biome.movingPlatformCount),
                biome.platformColor * 1.15f);

            movingPlatformsUsed++;
        }
    }

    private bool CanReach(PlatformInfo a, PlatformInfo b)
    {
        float dx = Mathf.Abs(a.center.x - b.center.x) - (a.width + 2f) / 2f;
        if (dx < 0) dx = 0;
        float dy = b.topY - a.topY;

        // Walk/fall to lower platform
        if (dy < 0 && dx < MAX_JUMP_HORIZONTAL * 2f) return true;
        // Jump up
        if (dy >= 0 && dy <= MAX_JUMP_UP && dx <= MAX_JUMP_HORIZONTAL) return true;
        // Same height jump
        if (Mathf.Abs(dy) < 1f && dx <= MAX_JUMP_HORIZONTAL) return true;

        return false;
    }

    private struct PlatformInfo
    {
        public Vector2 center;
        public float width;
        public float topY;
    }

    // =========================================================
    // Phase 5: Perturbation
    // =========================================================

    /// <summary>
    /// Apply organic variation to all placed pieces.
    /// Shifts positions, varies sizes, and adds color variation.
    /// </summary>
    private void ApplyPerturbation(List<ArenaPlacement> pieces)
    {
        foreach (var placement in pieces)
        {
            if (placement.piece == null) continue;
            var type = placement.piece.pieceType;

            // Don't perturb spawn points or kill zones
            if (type == ArenaPieceData.PieceType.SpawnPoint ||
                type == ArenaPieceData.PieceType.KillZone)
                continue;

            // Position jitter
            float jitterX = RandomRange(-0.5f, 0.5f);
            float jitterY = RandomRange(-0.2f, 0.2f);

            // Less jitter for ground pieces (they should feel stable)
            if (placement.piece.pieceName != null && placement.piece.pieceName.Contains("Ground"))
            {
                jitterX *= 0.3f;
                jitterY *= 0.1f;
            }

            placement.position += new Vector2(jitterX, jitterY);

            // Size variation (±12%)
            if (type == ArenaPieceData.PieceType.Platform ||
                type == ArenaPieceData.PieceType.Bridge ||
                type == ArenaPieceData.PieceType.MovingPlatform)
            {
                float widthMult = RandomRange(0.88f, 1.12f);
                float heightMult = RandomRange(0.90f, 1.10f);
                placement.piece.size = new Vector2(
                    placement.piece.size.x * widthMult,
                    placement.piece.size.y * heightMult);
            }

            // Color variation (±5% brightness)
            float brightness = RandomRange(0.95f, 1.05f);
            Color c = placement.piece.pieceColor;
            placement.piece.pieceColor = new Color(
                Mathf.Clamp01(c.r * brightness),
                Mathf.Clamp01(c.g * brightness),
                Mathf.Clamp01(c.b * brightness),
                c.a);
        }
    }

    // =========================================================
    // Phase 6: Spawns, Hazards, Boundaries (mostly unchanged)
    // =========================================================

    private void GenerateHazards(List<ArenaPlacement> pieces, BiomeData biome,
        float halfW, float halfH, float groundY)
    {
        if (biome.hasBottomKillZone)
        {
            float kzY = -halfH - 1f;
            AddKillZonePiece(pieces, "BottomKillZone",
                new Vector2(0, kzY), biome.arenaBounds.x + 4f, 3f, biome.hazardColor);
        }

        for (int i = 0; i < biome.killZoneCount; i++)
        {
            float kzWidth = RandRange(biome.killZoneSizeRange);
            float x = RandomRange(-halfW + kzWidth, halfW - kzWidth);
            float y = groundY - 1f;
            AddKillZonePiece(pieces, $"KillZone_{i}",
                new Vector2(x, y), kzWidth, 2f, biome.hazardColor);
        }
    }

    private void GenerateBoundaries(List<ArenaPlacement> pieces, BiomeData biome,
        float halfW, float halfH)
    {
        float wallHeight = biome.arenaBounds.y;

        AddWallPiece(pieces, "BoundaryLeft",
            new Vector2(-halfW - 0.5f, 0), 1f, wallHeight, biome.wallColor);
        AddWallPiece(pieces, "BoundaryRight",
            new Vector2(halfW + 0.5f, 0), 1f, wallHeight, biome.wallColor);

        if (biome.hasCeiling)
        {
            AddPlatformPiece(pieces, "Ceiling",
                new Vector2(0, halfH + 0.5f), biome.arenaBounds.x + 2f, 1f, biome.wallColor);
        }
    }

    private void GenerateSpawnPoints(List<ArenaPlacement> pieces, BiomeData biome,
        float halfW, float groundY)
    {
        var surfaces = new List<Vector2>();
        foreach (var p in pieces)
        {
            if (p.piece == null) continue;
            var type = p.piece.pieceType;
            if (type == ArenaPieceData.PieceType.Platform ||
                type == ArenaPieceData.PieceType.Bridge ||
                type == ArenaPieceData.PieceType.MovingPlatform)
            {
                float topY = p.position.y + p.piece.size.y / 2f + 1f;
                surfaces.Add(new Vector2(p.position.x, topY));

                if (p.piece.size.x > 5f)
                {
                    float offset = p.piece.size.x * 0.3f;
                    surfaces.Add(new Vector2(p.position.x - offset, topY));
                    surfaces.Add(new Vector2(p.position.x + offset, topY));
                }
            }
        }

        if (surfaces.Count == 0)
        {
            surfaces.Add(new Vector2(-5, groundY + 2));
            surfaces.Add(new Vector2(5, groundY + 2));
            surfaces.Add(new Vector2(-10, groundY + 2));
            surfaces.Add(new Vector2(10, groundY + 2));
        }

        // Player spawns — prefer lower surfaces
        var lowerSurfaces = surfaces.FindAll(s => s.y < groundY + 6f);
        if (lowerSurfaces.Count < 4) lowerSurfaces = surfaces;
        PlaceSpawns(pieces, lowerSurfaces, ArenaPieceData.SpawnPointType.Player, 4, halfW);

        // Monster spawns — elevated
        var elevated = surfaces.FindAll(s => s.y > groundY + 3f);
        if (elevated.Count < biome.monsterCount) elevated = surfaces;
        PlaceSpawns(pieces, elevated, ArenaPieceData.SpawnPointType.Monster, biome.monsterCount, halfW);

        // Chest spawns
        PlaceSpawns(pieces, surfaces, ArenaPieceData.SpawnPointType.Chest, biome.chestCount, halfW);
    }

    private void PlaceSpawns(List<ArenaPlacement> pieces, List<Vector2> surfaces,
        ArenaPieceData.SpawnPointType spawnType, int count, float halfW)
    {
        if (count <= 0 || surfaces.Count == 0) return;

        surfaces.Sort((a, b) => a.x.CompareTo(b.x));

        float step = (float)surfaces.Count / count;
        for (int i = 0; i < count && i < surfaces.Count; i++)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(i * step), 0, surfaces.Count - 1);
            Vector2 pos = surfaces[index];
            pos.x += RandomRange(-0.5f, 0.5f);

            AddSpawnPiece(pieces, $"{spawnType}Spawn_{i}", pos, spawnType);
        }
    }

    // =========================================================
    // Helpers
    // =========================================================

    private float RandRange(Vector2 range)
    {
        return RandomRange(range.x, range.y);
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    // =========================================================
    // Piece Factory Methods
    // =========================================================

    private void AddPlatformPiece(List<ArenaPlacement> pieces, string name,
        Vector2 position, float width, float height, Color color)
    {
        var piece = ScriptableObject.CreateInstance<ArenaPieceData>();
        piece.pieceName = name;
        piece.pieceType = ArenaPieceData.PieceType.Platform;
        piece.size = new Vector2(width, height);
        piece.pieceColor = color;
        pieces.Add(new ArenaPlacement { piece = piece, position = position, rotation = 0f });
    }

    private void AddWallPiece(List<ArenaPlacement> pieces, string name,
        Vector2 position, float width, float height, Color color)
    {
        var piece = ScriptableObject.CreateInstance<ArenaPieceData>();
        piece.pieceName = name;
        piece.pieceType = ArenaPieceData.PieceType.Wall;
        piece.size = new Vector2(width, height);
        piece.pieceColor = color;
        pieces.Add(new ArenaPlacement { piece = piece, position = position, rotation = 0f });
    }

    private void AddRampPiece(List<ArenaPlacement> pieces, string name,
        Vector2 position, float length, float height, float angle, float rotation, Color color)
    {
        var piece = ScriptableObject.CreateInstance<ArenaPieceData>();
        piece.pieceName = name;
        piece.pieceType = ArenaPieceData.PieceType.Ramp;
        piece.size = new Vector2(length, height);
        piece.rampAngle = angle;
        piece.pieceColor = color;
        pieces.Add(new ArenaPlacement { piece = piece, position = position, rotation = rotation });
    }

    private void AddKillZonePiece(List<ArenaPlacement> pieces, string name,
        Vector2 position, float width, float height, Color color)
    {
        var piece = ScriptableObject.CreateInstance<ArenaPieceData>();
        piece.pieceName = name;
        piece.pieceType = ArenaPieceData.PieceType.KillZone;
        piece.size = new Vector2(width, height);
        piece.killZoneDamage = 100f;
        piece.pieceColor = color;
        pieces.Add(new ArenaPlacement { piece = piece, position = position, rotation = 0f });
    }

    private void AddMovingPlatformPiece(List<ArenaPlacement> pieces, string name,
        Vector2 position, float width, float height, Vector2 moveDir,
        float amplitude, float speed, float phaseOffset, Color color)
    {
        var piece = ScriptableObject.CreateInstance<ArenaPieceData>();
        piece.pieceName = name;
        piece.pieceType = ArenaPieceData.PieceType.MovingPlatform;
        piece.size = new Vector2(width, height);
        piece.moveDirection = moveDir;
        piece.moveAmplitude = amplitude;
        piece.moveSpeed = speed;
        piece.pieceColor = color;
        pieces.Add(new ArenaPlacement { piece = piece, position = position, rotation = 0f });
    }

    private void AddSpawnPiece(List<ArenaPlacement> pieces, string name,
        Vector2 position, ArenaPieceData.SpawnPointType spawnType)
    {
        var piece = ScriptableObject.CreateInstance<ArenaPieceData>();
        piece.pieceName = name;
        piece.pieceType = ArenaPieceData.PieceType.SpawnPoint;
        piece.spawnType = spawnType;
        pieces.Add(new ArenaPlacement { piece = piece, position = position, rotation = 0f });
    }
}
