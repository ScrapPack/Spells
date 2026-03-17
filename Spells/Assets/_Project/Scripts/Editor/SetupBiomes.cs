using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Editor tool to create the 5 initial BiomeData assets and configure
/// the ProceduralLevelGenerator on the scene's ArenaSetup object.
///
/// Menu: Spells → Setup Biomes
/// Creates: Assets/_Project/Data/Biomes/*.asset
/// </summary>
public static class SetupBiomes
{
#if UNITY_EDITOR
    [MenuItem("Spells/Setup Biomes", false, 103)]
    public static void Setup()
    {
        if (!EditorUtility.DisplayDialog(
            "Setup Biomes",
            "This will create 5 BiomeData assets in Assets/_Project/Data/Biomes/.\n\n" +
            "Existing assets will NOT be overwritten.",
            "Create", "Cancel"))
            return;

        DoSetup();
    }

    public static void DoSetup()
    {
        string dir = "Assets/_Project/Data/Biomes";
        if (!AssetDatabase.IsValidFolder(dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
                AssetDatabase.CreateFolder("Assets/_Project", "Data");
            AssetDatabase.CreateFolder("Assets/_Project/Data", "Biomes");
        }

        // Forest Temple — multi-level exploration
        CreateBiome(dir, "ForestTemple", biome =>
        {
            biome.biomeName = "Ancient Forest Temple";
            biome.description = "Moss-covered stone platforms among ancient trees. Multi-level terrain connected by ramps and vine-draped walls.";
            biome.structureType = StructureType.MultiLevel;
            biome.arenaBounds = new Vector2(42f, 25f);
            biome.groundCoverage = 0.65f;
            biome.groundGapCount = 1;
            biome.gapWidthRange = new Vector2(3f, 5f);
            biome.groundThickness = 1.2f;
            biome.platformCount = 10;
            biome.platformWidthRange = new Vector2(3f, 7f);
            biome.platformThicknessRange = new Vector2(0.6f, 1f);
            biome.platformLevels = 3;
            biome.maxPlatformHeight = 12f;
            biome.wallCount = 4;
            biome.wallHeightRange = new Vector2(4f, 8f);
            biome.wallThickness = 0.8f;
            biome.rampCount = 3;
            biome.rampAngleRange = new Vector2(20f, 30f);
            biome.rampLengthRange = new Vector2(3f, 5f);
            biome.hasBottomKillZone = false;
            biome.killZoneCount = 0;
            biome.movingPlatformCount = 0;
            biome.monsterCount = 1;
            biome.chestCount = 2;
            biome.groundColor = new Color(0.25f, 0.35f, 0.20f);     // Mossy green-brown
            biome.platformColor = new Color(0.40f, 0.35f, 0.28f);   // Weathered stone
            biome.wallColor = new Color(0.30f, 0.38f, 0.25f);       // Vine-covered stone
            biome.hazardColor = new Color(0.2f, 0.5f, 0.2f, 0.3f);  // Green poison
            biome.backgroundColor = new Color(0.05f, 0.08f, 0.04f); // Deep forest
            biome.hasBoundaryWalls = true;
            biome.hasCeiling = true;
        });

        // Desert Ruins — horizontal traversal
        CreateBiome(dir, "DesertRuins", biome =>
        {
            biome.biomeName = "Desert Ruins";
            biome.description = "Crumbling sandstone bridges span ancient gaps. Long horizontal traversal across a ruined desert temple.";
            biome.structureType = StructureType.Bridge;
            biome.arenaBounds = new Vector2(48f, 22f);  // Wider, shorter
            biome.groundCoverage = 0.55f;
            biome.groundGapCount = 3;
            biome.gapWidthRange = new Vector2(4f, 7f);
            biome.groundThickness = 0.8f;
            biome.platformCount = 8;
            biome.platformWidthRange = new Vector2(4f, 10f);  // Longer platforms
            biome.platformThicknessRange = new Vector2(0.4f, 0.7f);  // Thinner
            biome.platformLevels = 2;
            biome.maxPlatformHeight = 8f;
            biome.wallCount = 2;
            biome.wallHeightRange = new Vector2(3f, 6f);
            biome.wallThickness = 1.2f;
            biome.rampCount = 1;
            biome.rampAngleRange = new Vector2(15f, 25f);
            biome.rampLengthRange = new Vector2(4f, 6f);
            biome.hasBottomKillZone = true;  // Bottomless pit
            biome.killZoneCount = 0;
            biome.movingPlatformCount = 0;
            biome.monsterCount = 1;
            biome.chestCount = 3;
            biome.groundColor = new Color(0.55f, 0.45f, 0.30f);     // Sandy gold
            biome.platformColor = new Color(0.50f, 0.40f, 0.28f);   // Sandstone
            biome.wallColor = new Color(0.45f, 0.38f, 0.25f);       // Desert pillar
            biome.hazardColor = new Color(0.6f, 0.4f, 0.1f, 0.4f);  // Sand pit
            biome.backgroundColor = new Color(0.12f, 0.08f, 0.04f); // Desert night
            biome.hasBoundaryWalls = true;
            biome.hasCeiling = false;  // Open sky
        });

        // Volcanic Caldera — island hopping
        CreateBiome(dir, "VolcanicCaldera", biome =>
        {
            biome.biomeName = "Volcanic Caldera";
            biome.description = "Floating rock islands over a bubbling lava lake. Platforms bob and sway with volcanic tremors.";
            biome.structureType = StructureType.Islands;
            biome.arenaBounds = new Vector2(40f, 28f);  // Taller for vertical play
            biome.groundCoverage = 0f;  // No continuous ground
            biome.groundGapCount = 0;
            biome.groundThickness = 1f;
            biome.platformCount = 12;
            biome.platformWidthRange = new Vector2(3f, 6f);
            biome.platformThicknessRange = new Vector2(0.7f, 1.2f);
            biome.platformLevels = 4;
            biome.maxPlatformHeight = 16f;
            biome.wallCount = 2;
            biome.wallHeightRange = new Vector2(3f, 5f);
            biome.wallThickness = 1f;
            biome.rampCount = 0;
            biome.rampAngleRange = new Vector2(20f, 35f);
            biome.rampLengthRange = new Vector2(3f, 4f);
            biome.hasBottomKillZone = true;   // Lava!
            biome.killZoneCount = 0;
            biome.movingPlatformCount = 4;    // Bobbing platforms
            biome.moveAmplitudeRange = new Vector2(1.5f, 3f);
            biome.moveSpeedRange = new Vector2(0.4f, 0.8f);
            biome.movePlatformAxis = MovePlatformAxis.Vertical;
            biome.monsterCount = 0;
            biome.chestCount = 2;
            biome.groundColor = new Color(0.35f, 0.20f, 0.15f);     // Dark volcanic rock
            biome.platformColor = new Color(0.40f, 0.25f, 0.18f);   // Volcanic stone
            biome.wallColor = new Color(0.30f, 0.18f, 0.12f);       // Obsidian
            biome.hazardColor = new Color(1f, 0.3f, 0.0f, 0.6f);    // Lava glow
            biome.backgroundColor = new Color(0.12f, 0.04f, 0.02f); // Deep magma
            biome.hasBoundaryWalls = true;
            biome.hasCeiling = false;  // Open caldera
        });

        // Crystal Cavern — classic arena
        CreateBiome(dir, "CrystalCavern", biome =>
        {
            biome.biomeName = "Crystal Cavern";
            biome.description = "A subterranean arena of gleaming crystal formations. Clean sightlines and classic platform layout for pure combat.";
            biome.structureType = StructureType.Arena;
            biome.arenaBounds = new Vector2(38f, 22f);
            biome.groundCoverage = 0.75f;
            biome.groundGapCount = 0;
            biome.groundThickness = 1f;
            biome.platformCount = 6;
            biome.platformWidthRange = new Vector2(4f, 8f);
            biome.platformThicknessRange = new Vector2(0.5f, 0.8f);
            biome.platformLevels = 3;
            biome.maxPlatformHeight = 10f;
            biome.wallCount = 4;
            biome.wallHeightRange = new Vector2(3f, 6f);
            biome.wallThickness = 0.7f;
            biome.rampCount = 2;
            biome.rampAngleRange = new Vector2(20f, 30f);
            biome.rampLengthRange = new Vector2(3f, 5f);
            biome.hasBottomKillZone = false;
            biome.killZoneCount = 0;
            biome.movingPlatformCount = 0;
            biome.monsterCount = 2;
            biome.chestCount = 3;
            biome.groundColor = new Color(0.25f, 0.25f, 0.35f);     // Dark crystal
            biome.platformColor = new Color(0.35f, 0.30f, 0.50f);   // Purple crystal
            biome.wallColor = new Color(0.30f, 0.28f, 0.45f);       // Crystal pillar
            biome.hazardColor = new Color(0.5f, 0.3f, 0.8f, 0.3f);  // Crystal glow
            biome.backgroundColor = new Color(0.04f, 0.03f, 0.08f); // Deep underground
            biome.hasBoundaryWalls = true;
            biome.hasCeiling = true;
        });

        // Storm Citadel — vertical tower
        CreateBiome(dir, "StormCitadel", biome =>
        {
            biome.biomeName = "Storm Citadel";
            biome.description = "A towering fortress of narrow ledges and vertical challenges. Climb or be pushed into the storm below.";
            biome.structureType = StructureType.Vertical;
            biome.arenaBounds = new Vector2(30f, 32f);  // Narrow and tall
            biome.groundCoverage = 0.4f;
            biome.groundGapCount = 1;
            biome.gapWidthRange = new Vector2(3f, 5f);
            biome.groundThickness = 1f;
            biome.platformCount = 14;
            biome.platformWidthRange = new Vector2(2.5f, 5f);  // Narrow
            biome.platformThicknessRange = new Vector2(0.4f, 0.7f);
            biome.platformLevels = 5;
            biome.maxPlatformHeight = 20f;
            biome.wallCount = 6;
            biome.wallHeightRange = new Vector2(3f, 7f);
            biome.wallThickness = 0.8f;
            biome.rampCount = 0;
            biome.rampAngleRange = new Vector2(25f, 40f);
            biome.rampLengthRange = new Vector2(2f, 4f);
            biome.hasBottomKillZone = true;   // Storm clouds below
            biome.killZoneCount = 0;
            biome.movingPlatformCount = 2;    // Wind-blown platforms
            biome.moveAmplitudeRange = new Vector2(2f, 4f);
            biome.moveSpeedRange = new Vector2(0.3f, 0.6f);
            biome.movePlatformAxis = MovePlatformAxis.Horizontal;
            biome.monsterCount = 0;
            biome.chestCount = 2;
            biome.groundColor = new Color(0.30f, 0.30f, 0.35f);     // Storm-grey stone
            biome.platformColor = new Color(0.35f, 0.33f, 0.38f);   // Grey ledge
            biome.wallColor = new Color(0.28f, 0.28f, 0.33f);       // Dark rampart
            biome.hazardColor = new Color(0.3f, 0.5f, 0.9f, 0.4f);  // Lightning blue
            biome.backgroundColor = new Color(0.06f, 0.06f, 0.10f); // Stormy sky
            biome.hasBoundaryWalls = true;
            biome.hasCeiling = false;  // Open to storm
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SetupBiomes: Created 5 biome assets in " + dir);
    }

    private static void CreateBiome(string dir, string fileName, System.Action<BiomeData> configure)
    {
        string path = $"{dir}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<BiomeData>(path) != null)
        {
            Debug.Log($"SetupBiomes: {fileName} already exists, skipping.");
            return;
        }

        var biome = ScriptableObject.CreateInstance<BiomeData>();
        configure(biome);
        AssetDatabase.CreateAsset(biome, path);
    }
#endif
}
