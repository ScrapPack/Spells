using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// One-click MVP asset setup for Spells.
/// Creates all ScriptableObject assets, projectile prefabs, and data
/// needed to make the game playable with Wizard + Warrior.
///
/// Menu: Spells → Setup MVP Assets
///
/// Safe to run multiple times — checks for existing assets before creating.
/// </summary>
public class SetupMVPAssets : EditorWindow
{
    private static readonly string DataRoot = "Assets/_Project/Data";
    private static readonly string CardRoot = "Assets/_Project/Resources/Cards";
    private static readonly string PrefabRoot = "Assets/_Project/Prefabs";

    [MenuItem("Spells/Patch Class Abilities", false, 104)]
    public static void PatchClassAbilities()
    {
        int patched = 0;

        // Map class names to their ability class names
        var abilityMap = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Wizard", "WizardFireball" },
            { "Warrior", "ShieldBashAbility" },
        };

        foreach (var kvp in abilityMap)
        {
            string path = $"{DataRoot}/Classes/{kvp.Key}.asset";
            var classData = AssetDatabase.LoadAssetAtPath<ClassData>(path);
            if (classData != null && classData.abilityClassName != kvp.Value)
            {
                classData.abilityClassName = kvp.Value;
                EditorUtility.SetDirty(classData);
                Debug.Log($"[Spells] Patched {kvp.Key} ability → {kvp.Value}");
                patched++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Spells] Patched {patched} class abilities.");
    }

    [MenuItem("Spells/Setup MVP Assets", false, 100)]
    public static void Setup()
    {
        if (!EditorUtility.DisplayDialog(
            "Setup MVP Assets",
            "This will create all ScriptableObject assets and prefabs needed for the MVP.\n\n" +
            "• 2 CombatData (Wizard, Warrior)\n" +
            "• 1 MovementData (Shared)\n" +
            "• 2 ClassData (Wizard, Warrior)\n" +
            "• 1 GameSettings\n" +
            "• 8 Power Cards\n" +
            "• 2 Projectile Prefabs\n\n" +
            "Existing assets will NOT be overwritten.",
            "Create Assets",
            "Cancel"))
        {
            return;
        }

        DoSetup();

        EditorUtility.DisplayDialog("Setup Complete",
            "All MVP assets created successfully.\n\n" +
            "Next step: Use Spells → Setup Test Scene to create a playable arena.",
            "OK");
    }

    /// <summary>
    /// Performs all asset creation without UI dialogs. Safe for batch mode.
    /// </summary>
    public static void DoSetup()
    {
        CreateFolders();

        // Combat Data
        var wizardCombat = CreateCombatData_Wizard();
        var warriorCombat = CreateCombatData_Warrior();

        // Movement Data
        var sharedMovement = CreateMovementData_Shared();

        // Projectile Prefabs
        var wizardProjectile = CreateProjectilePrefab_Wizard();
        var warriorProjectile = CreateProjectilePrefab_Warrior();

        // Class Data
        CreateClassData_Wizard(wizardCombat, wizardProjectile);
        CreateClassData_Warrior(warriorCombat, warriorProjectile);

        // Game Settings
        CreateGameSettings();

        // Power Cards — 4 General + 2 Wizard + 2 Warrior
        CreateCard_StoneSkin();
        CreateCard_Haste();
        CreateCard_VampiricTouch();
        CreateCard_GlassCannon();
        CreateCard_ArcaneBarrage();
        CreateCard_SpellShield();
        CreateCard_HeavyThrow();
        CreateCard_MagneticReturn();
        CreateCard_ChargeShot();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Spells] ✓ MVP asset setup complete! All ScriptableObjects and prefabs created.");
    }

    // ─────────────────────────────── FOLDERS ───────────────────────────────

    private static void CreateFolders()
    {
        EnsureFolder(DataRoot);
        EnsureFolder($"{DataRoot}/Combat");
        EnsureFolder($"{DataRoot}/Movement");
        EnsureFolder($"{DataRoot}/Classes");
        EnsureFolder("Assets/_Project/Resources");
        EnsureFolder(CardRoot);
        EnsureFolder($"{DataRoot}/Settings");
        EnsureFolder($"{PrefabRoot}/Projectiles");
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

    // ─────────────────────────── COMBAT DATA ───────────────────────────────

    private static CombatData CreateCombatData_Wizard()
    {
        string path = $"{DataRoot}/Combat/WizardCombat.asset";
        var existing = AssetDatabase.LoadAssetAtPath<CombatData>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var data = ScriptableObject.CreateInstance<CombatData>();

        // GDD: Wizard — 3 HP, rapid arcane bolts, fast fire rate, moderate damage, straight trajectory
        data.maxHP = 3;
        data.projectileSpeed = 25f;        // Fast bolts
        data.fireCooldown = 0.2f;          // Rapid fire
        data.projectileDamage = 1f;        // Standard 1 HP damage
        data.projectileLifetime = 3f;
        data.projectileRadius = 0.12f;     // Smaller hitbox (precision)

        data.projectileGravity = 0f;       // Straight trajectory
        data.projectileBounces = false;
        data.projectilePierces = false;

        data.knockbackForce = 6f;          // Moderate knockback
        data.hitstunDuration = 0.12f;

        data.parryWindow = 0.117f;         // ~7 frames at 60fps
        data.parryWhiffRecovery = 0.3f;
        data.parryReflectSpeedMult = 1.2f;

        data.invincibilityDuration = 0.5f;

        data.maxAmmo = 0;                  // Unlimited (not axe-based)
        data.retrievableProjectiles = false;

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        return data;
    }

    private static CombatData CreateCombatData_Warrior()
    {
        string path = $"{DataRoot}/Combat/WarriorCombat.asset";
        var existing = AssetDatabase.LoadAssetAtPath<CombatData>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var data = ScriptableObject.CreateInstance<CombatData>();

        // GDD: Warrior — 4 HP, lobbed axes, arcing throw, retrievable, 3 axes
        data.maxHP = 4;
        data.projectileSpeed = 15f;        // Slower, heavier throws
        data.fireCooldown = 0.6f;          // Slow fire rate
        data.projectileDamage = 1f;        // Standard 1 HP damage
        data.projectileLifetime = 4f;      // Longer since axes stick in surfaces
        data.projectileRadius = 0.2f;      // Larger hitbox (axes)

        data.projectileGravity = 2f;       // Arcing trajectory
        data.projectileBounces = false;
        data.projectilePierces = false;

        data.knockbackForce = 12f;         // High knockback (heavy axes)
        data.hitstunDuration = 0.2f;       // Slightly longer stun

        data.parryWindow = 0.117f;         // Same as all classes
        data.parryWhiffRecovery = 0.3f;
        data.parryReflectSpeedMult = 1.2f;

        data.invincibilityDuration = 0.5f;

        data.maxAmmo = 3;                  // 3 axes
        data.retrievableProjectiles = true; // Must pick up axes

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        return data;
    }

    // ─────────────────────────── MOVEMENT DATA ─────────────────────────────

    private static MovementData CreateMovementData_Shared()
    {
        string path = $"{DataRoot}/Movement/SharedMovement.asset";
        var existing = AssetDatabase.LoadAssetAtPath<MovementData>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var data = ScriptableObject.CreateInstance<MovementData>();

        // GDD: All classes share identical movement
        data.moveSpeed = 10f;
        data.acceleration = 100f;
        data.deceleration = 80f;
        data.dashBurstMultiplier = 1.5f;
        data.dashDecayRate = 10f;
        data.dashStillThreshold = 0.15f;

        data.airAcceleration = 65f;
        data.airDeceleration = 50f;

        data.jumpForce = 14f;
        data.jumpCutMultiplier = 0.4f;
        data.coyoteTimeDuration = 0.12f;
        data.jumpBufferDuration = 0.12f;
        data.maxAirJumps = 0;

        data.wallJumpForce = new Vector2(12f, 16f);
        data.wallSlideSpeedMin = 3f;
        data.wallSlideSpeedMax = 15f;
        data.wallSlideAccelTime = 1.2f;
        data.wallJumpLockoutTime = 0.15f;

        data.waveLandSpeedBoost = 1.3f;
        data.waveLandFriction = 30f;
        data.waveLandMinSpeed = 2f;

        data.gravityScale = 3f;
        data.fallGravityMultiplier = 2.5f;
        data.peakGravityMultiplier = 0.4f;
        data.peakVelocityThreshold = 2f;
        data.maxFallSpeed = 20f;

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        return data;
    }

    // ─────────────────────── PROJECTILE PREFABS ────────────────────────────

    private static GameObject CreateProjectilePrefab_Wizard()
    {
        string path = $"{PrefabRoot}/Projectiles/WizardBolt.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var go = new GameObject("WizardBolt");

        // Visual — small blue-purple bolt
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.5f, 0.3f, 1f, 1f); // Purple-blue
        sr.sortingOrder = 5;

        // Physics
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;       // Straight trajectory
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;

        // Collision
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.12f;
        col.isTrigger = true;

        // Projectile behavior
        go.AddComponent<Projectile>();

        // Trail effect
        go.AddComponent<ProjectileTrail>();

        // Tag + layer (can be set up manually later)
        go.tag = "Untagged";

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"[Spells] Created: {path}");
        return prefab;
    }

    private static GameObject CreateProjectilePrefab_Warrior()
    {
        string path = $"{PrefabRoot}/Projectiles/WarriorAxe.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) { Debug.Log($"[Spells] Skipped (exists): {path}"); return existing; }

        var go = new GameObject("WarriorAxe");

        // Visual — larger red-orange axe
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.9f, 0.4f, 0.1f, 1f); // Orange-red
        sr.sortingOrder = 5;

        // Physics — gravity-affected arcing throw
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2f;       // Arcing trajectory
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = false;  // Axes rotate!

        // Collision — larger hitbox
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.2f;
        col.isTrigger = true;

        // Projectile behavior
        go.AddComponent<Projectile>();

        // Trail effect
        go.AddComponent<ProjectileTrail>();

        go.tag = "Untagged";

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"[Spells] Created: {path}");
        return prefab;
    }

    // ──────────────────────────── CLASS DATA ───────────────────────────────

    private static void CreateClassData_Wizard(CombatData combat, GameObject projectile)
    {
        string path = $"{DataRoot}/Classes/Wizard.asset";
        if (AssetDatabase.LoadAssetAtPath<ClassData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var data = ScriptableObject.CreateInstance<ClassData>();
        data.className = "Wizard";
        data.description = "Jack-of-all-trades caster with rapid arcane bolts. " +
            "Fastest fire rate, straight projectiles, broadest card pool.";
        data.combatData = combat;
        data.projectilePrefab = projectile;
        data.cardPoolTags = new string[] { "General", "Wizard" };
        data.classColor = new Color(0.5f, 0.3f, 1f, 1f); // Purple-blue
        data.abilityClassName = "WizardFireball";

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    private static void CreateClassData_Warrior(CombatData combat, GameObject projectile)
    {
        string path = $"{DataRoot}/Classes/Warrior.asset";
        if (AssetDatabase.LoadAssetAtPath<ClassData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var data = ScriptableObject.CreateInstance<ClassData>();
        data.className = "Warrior";
        data.description = "Tanky axe-thrower with resource management. " +
            "4 HP, arcing retrievable axes, high knockback, smallest card pool.";
        data.combatData = combat;
        data.projectilePrefab = projectile;
        data.cardPoolTags = new string[] { "General", "Warrior" };
        data.classColor = new Color(0.9f, 0.4f, 0.1f, 1f); // Orange-red

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    // ────────────────────────── GAME SETTINGS ──────────────────────────────

    private static void CreateGameSettings()
    {
        string path = $"{DataRoot}/Settings/DefaultGameSettings.asset";
        if (AssetDatabase.LoadAssetAtPath<GameSettings>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var data = ScriptableObject.CreateInstance<GameSettings>();

        // GDD: First to 5 wins, 4 players, 30s zoom delay, 90s max round
        data.roundsToWin = 5;
        data.maxPlayers = 4;
        data.zoomDelay = 30f;
        data.maxRoundTime = 90f;
        data.cardOptionsPerPick = 4;
        data.draftTimeLimit = 15f;
        data.generalPoolRatio = 0.4f;
        data.allowDuplicateClasses = true;
        data.bannedCards = new string[0];
        data.spawnDelay = 1f;
        data.spawnProtection = 1.5f;

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    // ─────────────────────────── POWER CARDS ───────────────────────────────

    // ── GENERAL CARDS ──

    private static void CreateCard_StoneSkin()
    {
        string path = $"{CardRoot}/StoneSkin.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = "Stone Skin";
        card.positiveDescription = "✦ +1 max HP";
        card.negativeDescription = "✗ -20% movement speed";
        card.tier = 1;
        card.classTags = new string[] { "General" };
        card.positiveEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.MaxHP, modType = StatModifier.ModType.Additive, value = 1 }
        };
        card.negativeEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.MoveSpeed, modType = StatModifier.ModType.Multiplicative, value = 0.8f }
        };
        card.stackCap = 3; // Reasonable cap
        card.cardColor = new Color(0.6f, 0.6f, 0.6f, 1f); // Stone gray

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    private static void CreateCard_Haste()
    {
        string path = $"{CardRoot}/Haste.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = "Haste";
        card.positiveDescription = "✦ +30% movement speed";
        card.negativeDescription = "✗ Projectiles deal 0.5x damage";
        card.tier = 1;
        card.classTags = new string[] { "General" };
        card.positiveEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.MoveSpeed, modType = StatModifier.ModType.Multiplicative, value = 1.3f }
        };
        card.negativeEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.ProjectileDamage, modType = StatModifier.ModType.Multiplicative, value = 0.5f }
        };
        card.stackCap = 0; // Unlimited
        card.cardColor = new Color(0.3f, 0.9f, 0.5f, 1f); // Speed green

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    private static void CreateCard_VampiricTouch()
    {
        string path = $"{CardRoot}/VampiricTouch.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = "Vampiric Touch";
        card.positiveDescription = "✦ Heal 1 HP per kill";
        card.negativeDescription = "✗ Lose 1 HP at round start";
        card.tier = 1;
        card.classTags = new string[] { "General" };
        card.positiveEffects = new StatModifier[0]; // Special behavior handles healing
        card.negativeEffects = new StatModifier[]
        {
            // The -1 HP at round start is handled by VampiricEffect.OnRoundStart
            new StatModifier { target = StatModifier.Target.MaxHP, modType = StatModifier.ModType.Additive, value = 0 } // Placeholder
        };
        card.stackCap = 3;
        card.hasSpecialBehavior = true;
        card.specialBehaviorID = "vampiric";
        card.cardColor = new Color(0.8f, 0.1f, 0.1f, 1f); // Blood red

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    private static void CreateCard_GlassCannon()
    {
        string path = $"{CardRoot}/GlassCannon.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = "Glass Cannon";
        card.positiveDescription = "✦ +50% projectile speed";
        card.negativeDescription = "✗ -1 max HP";
        card.tier = 1;
        card.classTags = new string[] { "General" };
        card.positiveEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.ProjectileSpeed, modType = StatModifier.ModType.Multiplicative, value = 1.5f }
        };
        card.negativeEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.MaxHP, modType = StatModifier.ModType.Additive, value = -1 }
        };
        card.stackCap = 0;
        card.hasSpecialBehavior = true;
        card.specialBehaviorID = "glass_cannon";
        card.cardColor = new Color(1f, 0.9f, 0.3f, 1f); // Glass gold

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    // ── WIZARD CARDS ──

    private static void CreateCard_ArcaneBarrage()
    {
        string path = $"{CardRoot}/ArcaneBarrage.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = "Arcane Barrage";
        card.positiveDescription = "✦ Fire rate doubled";
        card.negativeDescription = "✗ Each bolt deals half damage";
        card.tier = 1;
        card.classTags = new string[] { "General", "Wizard" };
        card.positiveEffects = new StatModifier[]
        {
            // Halve fire cooldown = double fire rate
            new StatModifier { target = StatModifier.Target.FireCooldown, modType = StatModifier.ModType.Multiplicative, value = 0.5f }
        };
        card.negativeEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.ProjectileDamage, modType = StatModifier.ModType.Multiplicative, value = 0.5f }
        };
        card.stackCap = 3;
        card.cardColor = new Color(0.3f, 0.5f, 1f, 1f); // Arcane blue

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    private static void CreateCard_SpellShield()
    {
        string path = $"{CardRoot}/SpellShield.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = "Spell Shield";
        card.positiveDescription = "✦ Parry window widened (+100ms)";
        card.negativeDescription = "✗ Parry whiff recovery tripled";
        card.tier = 1;
        card.classTags = new string[] { "General", "Wizard" };
        card.positiveEffects = new StatModifier[]
        {
            // +100ms to parry window
            new StatModifier { target = StatModifier.Target.ParryWindow, modType = StatModifier.ModType.Additive, value = 0.1f }
        };
        card.negativeEffects = new StatModifier[]
        {
            // Triple the whiff recovery (miss = very punishing)
            new StatModifier { target = StatModifier.Target.ParryWhiffRecovery, modType = StatModifier.ModType.Multiplicative, value = 3f }
        };
        card.stackCap = 2;
        card.cardColor = new Color(0.4f, 0.7f, 1f, 1f); // Shield blue

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    // ── WARRIOR CARDS ──

    private static void CreateCard_HeavyThrow()
    {
        string path = $"{CardRoot}/HeavyThrow.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = "Heavy Throw";
        card.positiveDescription = "✦ Axes deal 2x damage and pierce through targets";
        card.negativeDescription = "✗ Axes travel 50% slower";
        card.tier = 1;
        card.classTags = new string[] { "General", "Warrior" };
        card.positiveEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.ProjectileDamage, modType = StatModifier.ModType.Multiplicative, value = 2f }
            // Pierce is handled by setting projectilePierces = true (special behavior)
        };
        card.negativeEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.ProjectileSpeed, modType = StatModifier.ModType.Multiplicative, value = 0.5f }
        };
        card.stackCap = 2;
        card.hasSpecialBehavior = true;
        card.specialBehaviorID = "heavy_throw"; // Enables piercing
        card.cardColor = new Color(0.7f, 0.3f, 0.1f, 1f); // Heavy brown

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    private static void CreateCard_MagneticReturn()
    {
        string path = $"{CardRoot}/MagneticReturn.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = "Magnetic Return";
        card.positiveDescription = "✦ Axes automatically return after 3 seconds";
        card.negativeDescription = "✗ Returning axes can hit you";
        card.tier = 1;
        card.classTags = new string[] { "General", "Warrior" };
        card.positiveEffects = new StatModifier[0]; // Special behavior
        card.negativeEffects = new StatModifier[0]; // Special behavior
        card.stackCap = 1; // Binary effect
        card.hasSpecialBehavior = true;
        card.specialBehaviorID = "magnetic_return";
        card.cardColor = new Color(0.5f, 0.5f, 0.8f, 1f); // Magnetic blue

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[Spells] Created: {path}");
    }

    private static void CreateCard_ChargeShot()
    {
        string path = $"{CardRoot}/ChargeShot.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null)
        { Debug.Log($"[Spells] Skipped (exists): {path}"); return; }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = "Charge Shot";
        card.positiveDescription = "✦ Hold shoot to charge — more ammo consumed = bigger, stronger shot";
        card.negativeDescription = "✗ Can't rapid fire — must hold and release";
        card.tier = 1;
        card.classTags = new string[] { "General" };
        card.positiveEffects = new StatModifier[0];
        card.negativeEffects = new StatModifier[0];
        card.stackCap = 1;
        card.hasSpecialBehavior = true;
        card.specialBehaviorID = "charge_shot";
        card.cardColor = new Color(1f, 0.5f, 0f, 1f); // Orange

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[Spells] Created: {path}");
    }
}
