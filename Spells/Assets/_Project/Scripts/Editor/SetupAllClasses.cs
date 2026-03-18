using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Creates all 8 GDD classes beyond the MVP pair (Wizard + Warrior).
/// Adds: Warlock, Alchemist, Shaman, Jester, Witch Doctor, Rogue.
///
/// Each class gets: CombatData, ClassData, Projectile Prefab, and 2 class-specific cards.
///
/// Menu: Spells → Setup All 8 Classes
///
/// Prerequisites: Run "Setup MVP Assets" first (creates shared assets and folder structure).
/// Safe to run multiple times.
/// </summary>
public class SetupAllClasses : Editor
{
    private static readonly string DataRoot = "Assets/_Project/Data";
    private static readonly string PrefabRoot = "Assets/_Project/Prefabs";

    [MenuItem("Spells/Setup All 8 Classes", false, 103)]
    public static void Setup()
    {
        // Check prerequisites
        if (!AssetDatabase.IsValidFolder($"{DataRoot}/Combat"))
        {
            EditorUtility.DisplayDialog("Missing Folders",
                "Run 'Spells → Setup MVP Assets' first to create the folder structure.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Setup All 8 Classes",
            "This will create data for all 8 GDD classes:\n\n" +
            "Already exist: Wizard, Warrior\n" +
            "Will create: Warlock, Alchemist, Shaman, Jester, Witch Doctor, Rogue\n\n" +
            "Each gets: CombatData + ClassData + Projectile Prefab + 2 Cards\n\n" +
            "Existing assets will NOT be overwritten.",
            "Create All Classes",
            "Cancel"))
        {
            return;
        }

        int totalCreated = 0;

        // ── WARLOCK ──
        var warlockCombat = CreateCombat("WarlockCombat", cb => {
            // GDD: 3 HP, slow heavy dark orbs, low fire rate, high knockback, larger hitbox
            cb.maxHP = 3;
            cb.projectileSpeed = 12f;       // Slow
            cb.fireCooldown = 0.8f;         // Low fire rate
            cb.projectileDamage = 1f;
            cb.projectileLifetime = 4f;
            cb.projectileRadius = 0.25f;    // Larger hitbox

            cb.projectileGravity = 0f;      // Straight (heavy but not arcing)
            cb.knockbackForce = 14f;        // High knockback
            cb.hitstunDuration = 0.2f;
        }, ref totalCreated);

        var warlockProj = CreateProjectile("WarlockOrb",
            new Color(0.4f, 0.1f, 0.5f), 0f, 0.25f, true, ref totalCreated);

        CreateClass("Warlock", "Dark pact caster who trades HP for devastating power. " +
            "Slow heavy orbs with high knockback. Risk-reward specialist.",
            warlockCombat, warlockProj,
            new string[] { "General", "Warlock" },
            new Color(0.4f, 0.1f, 0.5f), ref totalCreated);

        // Warlock Cards
        CreateCard("BloodPact", "Blood Pact",
            "✦ Projectile damage doubled", "✗ Casting costs 1 HP",
            1, new string[] { "General", "Warlock" }, 3,
            new StatModifier[] {
                Mod(StatModifier.Target.ProjectileDamage, StatModifier.ModType.Multiplicative, 2f)
            },
            new StatModifier[0], // HP cost handled by special behavior
            true, "blood_pact",
            new Color(0.6f, 0f, 0f), ref totalCreated);

        CreateCard("SoulSiphon", "Soul Siphon",
            "✦ Gain 1 HP when any opponent is eliminated",
            "✗ Lose 1 HP when you eliminate someone directly",
            1, new string[] { "General", "Warlock" }, 2,
            new StatModifier[0], new StatModifier[0],
            true, "soul_siphon",
            new Color(0.5f, 0f, 0.6f), ref totalCreated);

        // ── ALCHEMIST ──
        var alchemistCombat = CreateCombat("AlchemistCombat", cb => {
            // GDD: 3 HP, potion lobs — arcing, leave lingering zones
            cb.maxHP = 3;
            cb.projectileSpeed = 14f;       // Moderate
            cb.fireCooldown = 0.5f;         // Moderate fire rate
            cb.projectileDamage = 1f;
            cb.projectileLifetime = 3f;
            cb.projectileRadius = 0.2f;

            cb.projectileGravity = 3f;      // Strong arc (lob)
            cb.knockbackForce = 5f;         // Low knockback (zone control, not burst)
            cb.hitstunDuration = 0.1f;
        }, ref totalCreated);

        var alchemistProj = CreateProjectile("AlchemistPotion",
            new Color(0.2f, 0.9f, 0.3f), 3f, 0.2f, false, ref totalCreated);

        CreateClass("Alchemist", "Zone control specialist with arcing potion lobs. " +
            "Lingering areas deny space. Strong on platforms, weak in open.",
            alchemistCombat, alchemistProj,
            new string[] { "General", "Alchemist" },
            new Color(0.2f, 0.9f, 0.3f), ref totalCreated);

        // Alchemist Cards
        CreateCard("StickyBrew", "Sticky Brew",
            "✦ Potion zones last twice as long", "✗ Potion zones are half the size",
            1, new string[] { "General", "Alchemist" }, 3,
            new StatModifier[] {
                Mod(StatModifier.Target.ProjectileLifetime, StatModifier.ModType.Multiplicative, 2f)
            },
            new StatModifier[0], // Zone size reduction handled by special behavior
            true, "sticky_brew",
            new Color(0.3f, 0.7f, 0.2f), ref totalCreated);

        CreateCard("VolatileMix", "Volatile Mix",
            "✦ Potion zones explode when an opponent steps in",
            "✗ You also trigger your own zones",
            1, new string[] { "General", "Alchemist" }, 2,
            new StatModifier[0], new StatModifier[0],
            true, "volatile_mix",
            new Color(0.9f, 0.5f, 0.1f), ref totalCreated);

        // ── SHAMAN ──
        var shamanCombat = CreateCombat("ShamanCombat", cb => {
            // GDD: 2 HP, standard spirit bolt
            cb.maxHP = 2;
            cb.projectileSpeed = 20f;       // Standard
            cb.fireCooldown = 0.35f;        // Moderate
            cb.projectileDamage = 1f;
            cb.projectileLifetime = 3f;
            cb.projectileRadius = 0.15f;

            cb.projectileGravity = 0f;      // Straight
            cb.knockbackForce = 8f;         // Standard
            cb.hitstunDuration = 0.15f;
        }, ref totalCreated);

        var shamanProj = CreateProjectile("ShamanBolt",
            new Color(0.3f, 0.8f, 0.9f), 0f, 0.15f, true, ref totalCreated);

        CreateClass("Shaman", "Summoner with action economy advantage. " +
            "Standard spirit bolt. Low HP forces careful positioning. " +
            "Builds accumulate spirit allies and totems.",
            shamanCombat, shamanProj,
            new string[] { "General", "Shaman" },
            new Color(0.3f, 0.8f, 0.9f), ref totalCreated);

        // Shaman Cards
        CreateCard("SpiritBond", "Spirit Bond",
            "✦ Summons mirror your movement", "✗ Summons share your damage (you take hits when they do)",
            1, new string[] { "General", "Shaman" }, 2,
            new StatModifier[0], new StatModifier[0],
            true, "spirit_bond",
            new Color(0.3f, 0.7f, 0.8f), ref totalCreated);

        CreateCard("AncestralTotem", "Ancestral Totem",
            "✦ Drop a totem that shoots at nearby enemies",
            "✗ Totem also shoots at you if you're closest",
            1, new string[] { "General", "Shaman" }, 2,
            new StatModifier[0], new StatModifier[0],
            true, "ancestral_totem",
            new Color(0.4f, 0.6f, 0.3f), ref totalCreated);

        // ── JESTER ──
        var jesterCombat = CreateCombat("JesterCombat", cb => {
            // GDD: 2 HP, bouncing trick shots
            cb.maxHP = 2;
            cb.projectileSpeed = 18f;       // Moderate
            cb.fireCooldown = 0.4f;         // Moderate
            cb.projectileDamage = 1f;
            cb.projectileLifetime = 5f;     // Long lifetime for bounces
            cb.projectileRadius = 0.15f;

            cb.projectileGravity = 0f;      // Straight (bouncing handles angles)
            cb.projectileBounces = true;    // KEY: bouncing projectiles
            cb.maxBounces = 5;              // Multiple bounces
            cb.knockbackForce = 7f;
            cb.hitstunDuration = 0.12f;
        }, ref totalCreated);

        var jesterProj = CreateProjectile("JesterTrickShot",
            new Color(1f, 0.8f, 0.2f), 0f, 0.15f, true, ref totalCreated);

        CreateClass("Jester", "The wild card. Bouncing trick shots ricochet off walls " +
            "and platforms unpredictably. Thrives in enclosed spaces. " +
            "2 HP glass cannon with high chaos potential.",
            jesterCombat, jesterProj,
            new string[] { "General", "Jester" },
            new Color(1f, 0.8f, 0.2f), ref totalCreated);

        // Jester Cards
        CreateCard("LuckyBounce", "Lucky Bounce",
            "✦ Bouncing projectiles gain damage with each bounce",
            "✗ First hit (direct, no bounce) deals zero damage",
            1, new string[] { "General", "Jester" }, 3,
            new StatModifier[0], new StatModifier[0],
            true, "lucky_bounce",
            new Color(1f, 0.9f, 0.3f), ref totalCreated);

        CreateCard("Jackpot", "Jackpot",
            "✦ When you get a kill, all opponents also take 1 damage",
            "✗ When you die, all opponents heal 1 HP",
            2, new string[] { "General", "Jester" }, 1,
            new StatModifier[0], new StatModifier[0],
            true, "jackpot",
            new Color(1f, 0.6f, 0f), ref totalCreated);

        // ── WITCH DOCTOR ──
        var witchDoctorCombat = CreateCombat("WitchDoctorCombat", cb => {
            // GDD: 3 HP, curse darts — moderate speed, apply debuffs
            cb.maxHP = 3;
            cb.projectileSpeed = 18f;       // Moderate
            cb.fireCooldown = 0.4f;         // Moderate
            cb.projectileDamage = 1f;
            cb.projectileLifetime = 3f;
            cb.projectileRadius = 0.12f;    // Thin darts

            cb.projectileGravity = 0f;      // Straight darts
            cb.knockbackForce = 4f;         // Low knockback (debuffs are the threat)
            cb.hitstunDuration = 0.1f;
        }, ref totalCreated);

        var witchDoctorProj = CreateProjectile("WitchDoctorDart",
            new Color(0.5f, 0.8f, 0.2f), 0f, 0.12f, true, ref totalCreated);

        CreateClass("Witch Doctor", "Attrition specialist who wears opponents down through " +
            "stacking debuffs. Curse darts apply effects on hit. " +
            "Wins long fights, vulnerable to burst damage.",
            witchDoctorCombat, witchDoctorProj,
            new string[] { "General", "WitchDoctor" },
            new Color(0.5f, 0.8f, 0.2f), ref totalCreated);

        // Witch Doctor Cards
        CreateCard("VenomDart", "Venom Dart",
            "✦ Hits apply poison (1 damage over 5 seconds)",
            "✗ Your projectiles move 20% slower",
            1, new string[] { "General", "WitchDoctor" }, 3,
            new StatModifier[0], // Poison handled by special behavior
            new StatModifier[] {
                Mod(StatModifier.Target.ProjectileSpeed, StatModifier.ModType.Multiplicative, 0.8f)
            },
            true, "venom_dart",
            new Color(0.3f, 0.7f, 0.1f), ref totalCreated);

        CreateCard("HexMark", "Hex Mark",
            "✦ Cursed opponents take +1 damage from all sources",
            "✗ You can only curse one opponent at a time",
            1, new string[] { "General", "WitchDoctor" }, 2,
            new StatModifier[0], new StatModifier[0],
            true, "hex_mark",
            new Color(0.6f, 0.2f, 0.6f), ref totalCreated);

        // ── ROGUE ──
        var rogueCombat = CreateCombat("RogueCombat", cb => {
            // GDD: 2 HP, throwing knives — fast burst of 2-3, short range, high close DPS
            cb.maxHP = 2;
            cb.projectileSpeed = 22f;       // Fast
            cb.fireCooldown = 0.12f;        // Very fast burst
            cb.projectileDamage = 0.5f;     // Lower per-hit (burst makes up for it)
            cb.projectileLifetime = 1.5f;   // Short range (lifetime limits distance)
            cb.projectileRadius = 0.1f;     // Small knives

            cb.projectileGravity = 0f;      // Straight
            cb.knockbackForce = 3f;         // Low (keep them close)
            cb.hitstunDuration = 0.05f;     // Minimal stun (allows combos)
        }, ref totalCreated);

        var rogueProj = CreateProjectile("RogueKnife",
            new Color(0.7f, 0.7f, 0.7f), 0f, 0.1f, true, ref totalCreated);

        CreateClass("Rogue", "Aggressive glass cannon with burst damage. " +
            "Fast throwing knives at close range. 2 HP demands aggression — " +
            "you die fast but kill faster.",
            rogueCombat, rogueProj,
            new string[] { "General", "Rogue" },
            new Color(0.3f, 0.3f, 0.3f), ref totalCreated);

        // Rogue Cards
        CreateCard("Ambush", "Ambush",
            "✦ First hit on unaware opponent (facing away) deals 2x damage",
            "✗ Non-ambush hits deal 0.75x damage",
            1, new string[] { "General", "Rogue" }, 2,
            new StatModifier[0], new StatModifier[0],
            true, "ambush",
            new Color(0.2f, 0.2f, 0.3f), ref totalCreated);

        CreateCard("SmokeBomb", "Smoke Bomb",
            "✦ On taking damage, become invisible for 1.5 seconds",
            "✗ While invisible, you can't attack",
            1, new string[] { "General", "Rogue" }, 2,
            new StatModifier[0], new StatModifier[0],
            true, "smoke_bomb",
            new Color(0.5f, 0.5f, 0.5f), ref totalCreated);

        // ── TIER 2/3 CARDS (Cross-class, game-changing) ──

        // Second Wind: T2, General — double jump, no wall slide
        CreateCard("SecondWind", "Second Wind",
            "✦ Double jump (extra air jump)", "✗ Can't wall slide",
            2, new string[] { "General" }, 3,
            new StatModifier[] {
                Mod(StatModifier.Target.MaxAirJumps, StatModifier.ModType.Additive, 1)
            },
            new StatModifier[0], // Wall slide disabled by special behavior
            true, "second_wind",
            new Color(0.6f, 0.9f, 1f), ref totalCreated);

        // Lich Form: T3, Warlock — revive once per round, -1 max HP
        CreateCard("LichForm", "Lich Form",
            "✦ Revive once per round with 1 HP",
            "✗ Permanent -1 max HP for the rest of the match",
            3, new string[] { "General", "Warlock" }, 2,
            new StatModifier[0], // Revive handled by special behavior
            new StatModifier[] {
                Mod(StatModifier.Target.MaxHP, StatModifier.ModType.Additive, -1)
            },
            true, "lich_form",
            new Color(0.2f, 0.8f, 0.3f), ref totalCreated);

        // Berserker: T2, Warrior — +1 damage below half HP, can't parry below half HP
        CreateCard("Berserker", "Berserker",
            "✦ +1 damage when below half HP",
            "✗ Can't parry when below half HP",
            2, new string[] { "General", "Warrior" }, 2,
            new StatModifier[0], new StatModifier[0],
            true, "berserker",
            new Color(0.9f, 0.2f, 0.1f), ref totalCreated);

        // Dark Tether: T2, Warlock — orbs home slightly toward nearest opponent
        CreateCard("DarkTether", "Dark Tether",
            "✦ Your orbs home slightly toward nearest opponent",
            "✗ Your orbs also home slightly toward you on return",
            2, new string[] { "General", "Warlock" }, 2,
            new StatModifier[0], new StatModifier[0],
            true, "dark_tether",
            new Color(0.3f, 0f, 0.4f), ref totalCreated);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Spells] ✓ All 8 classes setup complete! Created {totalCreated} new assets.");
        EditorUtility.DisplayDialog("All Classes Created",
            $"Created {totalCreated} new assets for 6 additional classes.\n\n" +
            "All 8 GDD classes now have:\n" +
            "• CombatData with GDD-accurate stats\n" +
            "• ClassData with pool tags and colors\n" +
            "• Projectile prefab with correct physics\n" +
            "• 2+ class-specific power cards\n\n" +
            "Plus Tier 2/3 cards: Second Wind, Lich Form, Berserker, Dark Tether\n\n" +
            "Total: 8 classes, 24 cards, 8 projectile prefabs.",
            "OK");
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS — Reduce boilerplate for repetitive asset creation
    // ═══════════════════════════════════════════════════════════

    private static CombatData CreateCombat(string name, System.Action<CombatData> configure, ref int count)
    {
        string path = $"{DataRoot}/Combat/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<CombatData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<CombatData>();

        // Universal defaults (same for all classes)
        data.parryWindow = 0.117f;
        data.parryWhiffRecovery = 0.3f;
        data.parryReflectSpeedMult = 1.2f;
        data.invincibilityDuration = 0.5f;
        data.maxAmmo = 0;
        data.retrievableProjectiles = false;

        configure(data);

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        count++;
        return data;
    }

    private static GameObject CreateProjectile(string name, Color color,
        float gravity, float radius, bool freezeRotation, ref int count)
    {
        string path = $"{PrefabRoot}/Projectiles/{name}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = new GameObject(name);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = color;
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = gravity;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = freezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = radius;
        col.isTrigger = true;

        go.AddComponent<Projectile>();
        go.AddComponent<ProjectileTrail>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"[Spells] Created: {path}");
        count++;
        return prefab;
    }

    private static void CreateClass(string className, string desc,
        CombatData combat, GameObject projectile, string[] tags, Color color, ref int count,
        string abilityClassName = null)
    {
        string path = $"{DataRoot}/Classes/{className}.asset";
        if (AssetDatabase.LoadAssetAtPath<ClassData>(path) != null) return;

        var data = ScriptableObject.CreateInstance<ClassData>();
        data.className = className;
        data.description = desc;
        data.combatData = combat;
        data.projectilePrefab = projectile;
        data.cardPoolTags = tags;
        data.classColor = color;
        data.abilityClassName = abilityClassName ?? "";

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Spells] Created: {path}");
        count++;
    }

    private static void CreateCard(string fileName, string cardName,
        string posDesc, string negDesc,
        int tier, string[] tags, int stackCap,
        StatModifier[] positives, StatModifier[] negatives,
        bool hasSpecial, string behaviorID,
        Color color, ref int count)
    {
        string path = $"{DataRoot}/Cards/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null) return;

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = cardName;
        card.positiveDescription = $"✦ {posDesc.TrimStart('✦').Trim()}";
        card.negativeDescription = $"✗ {negDesc.TrimStart('✗').Trim()}";
        card.tier = tier;
        card.classTags = tags;
        card.stackCap = stackCap;
        card.positiveEffects = positives;
        card.negativeEffects = negatives;
        card.hasSpecialBehavior = hasSpecial;
        card.specialBehaviorID = behaviorID;
        card.cardColor = color;

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[Spells] Created: {path}");
        count++;
    }

    private static StatModifier Mod(StatModifier.Target target, StatModifier.ModType modType, float value)
    {
        return new StatModifier { target = target, modType = modType, value = value };
    }
}
