using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// One-click PvE asset setup for Spells.
/// Creates MonsterData, ItemData, ArenaPieceData, ArenaLayoutData, and prefabs
/// needed for the PvE layer (monsters, chests, modular arenas).
///
/// Menu: Spells → Setup PvE Assets
///
/// Safe to run multiple times — checks for existing assets before creating.
/// </summary>
public class SetupPvEAssets : EditorWindow
{
    private static readonly string DataRoot = "Assets/_Project/Data";
    private static readonly string PrefabRoot = "Assets/_Project/Prefabs";

    [MenuItem("Spells/Setup PvE Assets", false, 200)]
    public static void Setup()
    {
        if (!EditorUtility.DisplayDialog(
            "Setup PvE Assets",
            "This will create all PvE assets:\n\n" +
            "• 1 MonsterData (Stone Golem)\n" +
            "• 3 ItemData (Spider Shoes, Fire Wand, Hitscan Gun)\n" +
            "• 8+ ArenaPieceData (platforms, bridges, walls, ramps, spawns, kill zones)\n" +
            "• 1 ArenaLayoutData (Disjointed Bridge)\n" +
            "• 1 Monster Projectile Prefab\n\n" +
            "Existing assets will NOT be overwritten.",
            "Create Assets",
            "Cancel"))
        {
            return;
        }

        CreateFolders();

        // Monster
        var monsterProjectile = CreateMonsterProjectilePrefab();
        var stoneGolem = CreateMonsterData_StoneGolem(monsterProjectile);

        // Items
        var spiderShoes = CreateItemData_SpiderShoes();
        var fireWand = CreateItemData_FireWand();
        var hitscanGun = CreateItemData_HitscanGun();
        var allItems = new ItemData[] { spiderShoes, fireWand, hitscanGun };

        // Arena Pieces
        var platformWide = CreateArenaPiece("Platform_Wide", ArenaPieceData.PieceType.Platform,
            new Vector2(8f, 1f), new Color(0.35f, 0.35f, 0.4f));
        var platformNarrow = CreateArenaPiece("Platform_Narrow", ArenaPieceData.PieceType.Platform,
            new Vector2(4f, 0.5f), new Color(0.4f, 0.38f, 0.35f));
        var bridgeSegment = CreateArenaPiece("Bridge_Segment", ArenaPieceData.PieceType.Bridge,
            new Vector2(6f, 0.5f), new Color(0.45f, 0.35f, 0.3f));
        var wallTall = CreateArenaPiece("Wall_Tall", ArenaPieceData.PieceType.Wall,
            new Vector2(1f, 20f), new Color(0.22f, 0.22f, 0.28f));
        var rampLeft = CreateArenaPiece("Ramp_Left", ArenaPieceData.PieceType.Ramp,
            new Vector2(5f, 2.5f), new Color(0.35f, 0.3f, 0.28f), 30f);
        var rampRight = CreateArenaPiece("Ramp_Right", ArenaPieceData.PieceType.Ramp,
            new Vector2(5f, 2.5f), new Color(0.35f, 0.3f, 0.28f), 30f);
        var gap = CreateArenaPiece("Gap_Bridge", ArenaPieceData.PieceType.Gap,
            new Vector2(4f, 1f), Color.clear);
        var killZone = CreateArenaPiece("KillZone_Pit", ArenaPieceData.PieceType.KillZone,
            new Vector2(40f, 3f), new Color(1f, 0.2f, 0.2f, 0.3f));

        // Spawn points
        var playerSpawn = CreateSpawnPiece("PlayerSpawn", ArenaPieceData.SpawnPointType.Player);
        var monsterSpawn = CreateSpawnPiece("MonsterSpawn", ArenaPieceData.SpawnPointType.Monster);
        var chestSpawn = CreateSpawnPiece("ChestSpawn", ArenaPieceData.SpawnPointType.Chest);

        // Arena Layout: Disjointed Bridge
        CreateArenaLayout_DisjointedBridge(stoneGolem, allItems,
            platformWide, platformNarrow, bridgeSegment, wallTall,
            rampLeft, rampRight, gap, killZone,
            playerSpawn, monsterSpawn, chestSpawn);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Spells] PvE assets created successfully!");
    }

    // ─────────────────────────────── FOLDERS ───────────────────────────────

    private static void CreateFolders()
    {
        EnsureFolder($"{DataRoot}/Monsters");
        EnsureFolder($"{DataRoot}/Items");
        EnsureFolder($"{DataRoot}/ArenaPieces");
        EnsureFolder($"{DataRoot}/ArenaLayouts");
        EnsureFolder($"{PrefabRoot}/Monsters");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }

    // ─────────────────────────── MONSTER DATA ───────────────────────────────

    private static MonsterData CreateMonsterData_StoneGolem(GameObject projectilePrefab)
    {
        string path = $"{DataRoot}/Monsters/StoneGolem.asset";
        var existing = AssetDatabase.LoadAssetAtPath<MonsterData>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var data = ScriptableObject.CreateInstance<MonsterData>();

        data.monsterName = "Stone Golem";
        data.description = "A slow-moving golem that fires spread projectiles. Scales with total player level pool.";

        data.baseHP = 3;
        data.baseDamage = 1f;
        data.attackCooldown = 2f;
        data.detectionRadius = 12f;

        data.hpPerLevelPool = 0.5f;
        data.damagePerLevelPool = 0.1f;
        data.cooldownReductionPerPool = 0.03f;
        data.minAttackCooldown = 0.8f;

        data.projectilesPerAttack = 3;
        data.spreadAngle = 30f;
        data.projectileSpeed = 12f;
        data.projectileLifetime = 3f;

        data.attackTelegraphDuration = 0.8f;

        data.patrolSpeed = 1.5f;
        data.pursuePlayers = false;

        data.monsterColor = new Color(0.6f, 0.6f, 0.6f);
        data.projectilePrefab = projectilePrefab;

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        return data;
    }

    private static GameObject CreateMonsterProjectilePrefab()
    {
        string path = $"{PrefabRoot}/Monsters/MonsterProjectile.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var go = new GameObject("MonsterProjectile");

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.15f;
        col.isTrigger = true;

        go.AddComponent<Projectile>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.8f, 0.3f, 0.3f); // Red-ish monster projectile

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.2f;
        trail.endWidth = 0f;
        trail.startColor = new Color(0.8f, 0.3f, 0.3f, 0.8f);
        trail.endColor = new Color(0.8f, 0.3f, 0.3f, 0f);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"[Spells] Created: {path}");
        return prefab;
    }

    // ─────────────────────────── ITEM DATA ───────────────────────────────

    private static ItemData CreateItemData_SpiderShoes()
    {
        string path = $"{DataRoot}/Items/SpiderShoes.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var data = ScriptableObject.CreateInstance<ItemData>();
        data.itemName = "Spider Shoes";
        data.description = "Walk on walls and ceilings. Lost on death.";
        data.behaviorID = "spider_shoes";
        data.ammo = 0; // Unlimited duration
        data.minLevelPool = 0;
        data.dropWeight = 1f;
        data.itemColor = new Color(0.5f, 0.8f, 0.3f);

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        return data;
    }

    private static ItemData CreateItemData_FireWand()
    {
        string path = $"{DataRoot}/Items/FireWand.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var data = ScriptableObject.CreateInstance<ItemData>();
        data.itemName = "Fire Wand";
        data.description = "Rapid-fire short-range flamethrower. Lost on death.";
        data.behaviorID = "fire_wand";
        data.ammo = 0; // Unlimited (lost on death only)
        data.minLevelPool = 2;
        data.dropWeight = 0.8f;
        data.itemColor = new Color(1f, 0.5f, 0.1f);

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        return data;
    }

    private static ItemData CreateItemData_HitscanGun()
    {
        string path = $"{DataRoot}/Items/HitscanGun.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var data = ScriptableObject.CreateInstance<ItemData>();
        data.itemName = "Hitscan Gun";
        data.description = "Near-instant projectile. 3 shots then it's gone.";
        data.behaviorID = "hitscan_gun";
        data.ammo = 3;
        data.minLevelPool = 4;
        data.dropWeight = 0.6f;
        data.itemColor = new Color(0.3f, 0.6f, 1f);

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        return data;
    }

    // ─────────────────────────── ARENA PIECES ───────────────────────────────

    private static ArenaPieceData CreateArenaPiece(string name, ArenaPieceData.PieceType type,
        Vector2 size, Color color, float rampAngle = 0f)
    {
        string path = $"{DataRoot}/ArenaPieces/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ArenaPieceData>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var data = ScriptableObject.CreateInstance<ArenaPieceData>();
        data.pieceName = name;
        data.pieceType = type;
        data.size = size;
        data.pieceColor = color;
        data.rampAngle = rampAngle;

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        return data;
    }

    private static ArenaPieceData CreateSpawnPiece(string name, ArenaPieceData.SpawnPointType spawnType)
    {
        string path = $"{DataRoot}/ArenaPieces/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ArenaPieceData>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var data = ScriptableObject.CreateInstance<ArenaPieceData>();
        data.pieceName = name;
        data.pieceType = ArenaPieceData.PieceType.SpawnPoint;
        data.spawnType = spawnType;

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        return data;
    }

    // ─────────────────────────── ARENA LAYOUT ───────────────────────────────

    private static void CreateArenaLayout_DisjointedBridge(
        MonsterData monsterData, ItemData[] items,
        ArenaPieceData platformWide, ArenaPieceData platformNarrow,
        ArenaPieceData bridgeSegment, ArenaPieceData wallTall,
        ArenaPieceData rampLeft, ArenaPieceData rampRight,
        ArenaPieceData gap, ArenaPieceData killZone,
        ArenaPieceData playerSpawn, ArenaPieceData monsterSpawn, ArenaPieceData chestSpawn)
    {
        string path = $"{DataRoot}/ArenaLayouts/DisjointedBridge.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ArenaLayoutData>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var layout = ScriptableObject.CreateInstance<ArenaLayoutData>();
        layout.arenaName = "Disjointed Bridge";
        layout.description = "Broken bridge segments with gaps. Pit below with kill zone. " +
                             "Side platforms and ramps create vertical play.";
        layout.arenaBounds = new Vector2(42f, 25f);
        layout.arenaCenter = Vector2.zero;

        layout.monsterData = monsterData;
        layout.monsterCount = 1;
        layout.chestCount = 2;
        layout.chestItemPool = items;

        // Compose the arena from pieces
        layout.pieces = new ArenaPlacement[]
        {
            // === BRIDGE SEGMENTS (the main broken bridge) ===
            // Left bridge section
            P(bridgeSegment, -14f, 0f),
            // Center-left bridge section
            P(bridgeSegment, -4f, 0f),
            // Center-right bridge section
            P(bridgeSegment, 4f, 0f),
            // Right bridge section
            P(bridgeSegment, 14f, 0f),

            // === GAPS (between bridge segments) ===
            P(gap, -9f, 0f),   // Gap between left and center-left
            P(gap, 0f, 0f),     // Gap in the center
            P(gap, 9f, 0f),     // Gap between center-right and right

            // === SIDE PLATFORMS (elevated) ===
            P(platformNarrow, -16f, 4f),
            P(platformNarrow, 16f, 4f),
            P(platformNarrow, -8f, 6f),
            P(platformNarrow, 8f, 6f),
            P(platformNarrow, 0f, 9f),    // High center platform

            // === LOWER PLATFORMS (below bridge) ===
            P(platformNarrow, -12f, -4f),
            P(platformNarrow, 12f, -4f),

            // === RAMPS (connecting levels) ===
            P(rampLeft, -18f, 2f),   // Left side ramp going up
            P(rampRight, 18f, 2f),   // Right side ramp going up

            // === WALLS (boundaries) ===
            P(wallTall, -21f, 5f),    // Left boundary
            P(wallTall, 21f, 5f),     // Right boundary

            // === CEILING ===
            P(platformWide, -10f, 16f),
            P(platformWide, 10f, 16f),
            P(platformWide, 0f, 16f),

            // === KILL ZONE (pit below bridge) ===
            P(killZone, 0f, -10f),

            // === PLAYER SPAWNS (on bridge segments) ===
            P(playerSpawn, -14f, 1f),
            P(playerSpawn, -4f, 1f),
            P(playerSpawn, 4f, 1f),
            P(playerSpawn, 14f, 1f),

            // === MONSTER SPAWN (center high platform) ===
            P(monsterSpawn, 0f, 10f),

            // === CHEST SPAWNS (on side platforms) ===
            P(chestSpawn, -16f, 5f),
            P(chestSpawn, 16f, 5f),
        };

        AssetDatabase.CreateAsset(layout, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    /// <summary>
    /// Helper to create an ArenaPlacement inline.
    /// </summary>
    private static ArenaPlacement P(ArenaPieceData piece, float x, float y, float rotation = 0f)
    {
        return new ArenaPlacement
        {
            piece = piece,
            position = new Vector2(x, y),
            rotation = rotation
        };
    }
}
