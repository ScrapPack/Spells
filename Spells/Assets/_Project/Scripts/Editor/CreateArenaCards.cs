using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Creates the 8 arena power cards under Assets/_Project/Data/Cards/Arena/.
/// Run via: Tools → Spells → Create Arena Cards
/// Safe to run multiple times — skips cards that already exist.
/// </summary>
public static class CreateArenaCards
{
    private const string OutputFolder = "Assets/_Project/Data/Cards/Arena";

    [MenuItem("Tools/Spells/Create Arena Cards")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            string parent = "Assets/_Project/Data/Cards";
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Cards");
            AssetDatabase.CreateFolder(parent, "Arena");
        }

        CreateCard(new CardDef
        {
            name     = "Iron Skin",
            positive = "Way more health (+80% max HP)",
            negative = "Bullets travel much slower (-40% bullet speed)",
            color    = new Color(0.6f, 0.8f, 0.4f),
            positiveMods = new[] { Multiplicative(StatModifier.Target.MaxHP,           1.8f) },
            negativeMods = new[] { Multiplicative(StatModifier.Target.ProjectileSpeed, 0.6f) },
        });

        CreateCard(new CardDef
        {
            name     = "Hollow Points",
            positive = "Bullets deal 60% more damage",
            negative = "Fire rate is 80% slower",
            color    = new Color(0.9f, 0.4f, 0.2f),
            positiveMods = new[] { Multiplicative(StatModifier.Target.ProjectileDamage, 1.6f) },
            negativeMods = new[] { Multiplicative(StatModifier.Target.FireCooldown,     1.8f) },
        });

        CreateCard(new CardDef
        {
            name     = "Velocity Rounds",
            positive = "Bullets travel 80% faster",
            negative = "Fire rate is 60% slower",
            color    = new Color(0.3f, 0.6f, 1.0f),
            positiveMods = new[] { Multiplicative(StatModifier.Target.ProjectileSpeed, 1.8f) },
            negativeMods = new[] { Multiplicative(StatModifier.Target.FireCooldown,    1.6f) },
        });

        CreateCard(new CardDef
        {
            name     = "Fortified Rounds",
            positive = "+30% max HP and +30% bullet damage",
            negative = "Bullets slower (-20% speed) and slightly longer reload (+25%)",
            color    = new Color(0.7f, 0.5f, 0.9f),
            positiveMods = new[]
            {
                Multiplicative(StatModifier.Target.MaxHP,             1.3f),
                Multiplicative(StatModifier.Target.ProjectileDamage,  1.3f),
            },
            negativeMods = new[]
            {
                Multiplicative(StatModifier.Target.ProjectileSpeed, 0.8f),
                Multiplicative(StatModifier.Target.FireCooldown,    1.25f),
            },
        });

        // ── Cards with SpellEffects (ammo / reload) ───────────────────────────

        CreateCard(new CardDef
        {
            name     = "Hair Trigger",
            positive = "Reload is 40% faster",
            negative = "Magazine holds 2 fewer shots",
            color    = new Color(1.0f, 0.85f, 0.2f),
            hasSpecial  = true,
            specialID   = "hair_trigger",
        });

        CreateCard(new CardDef
        {
            name     = "Extended Clip",
            positive = "3 extra shots per magazine",
            negative = "Reload takes 80% longer",
            color    = new Color(0.4f, 0.9f, 0.5f),
            hasSpecial  = true,
            specialID   = "extended_clip",
        });

        CreateCard(new CardDef
        {
            name     = "Overdrive",
            positive = "Bullets deal 50% more damage and travel 40% faster",
            negative = "Reload takes 150% longer (2.5× normal)",
            color    = new Color(1.0f, 0.4f, 0.1f),
            hasSpecial  = true,
            specialID   = "overdrive",
            positiveMods = new[]
            {
                Multiplicative(StatModifier.Target.ProjectileDamage, 1.5f),
                Multiplicative(StatModifier.Target.ProjectileSpeed,  1.4f),
            },
        });

        CreateCard(new CardDef
        {
            name     = "Quick Draw",
            positive = "Reload is 60% faster",
            negative = "Bullets deal 40% less damage",
            color    = new Color(0.3f, 0.8f, 0.9f),
            hasSpecial  = true,
            specialID   = "quick_draw",
            negativeMods = new[]
            {
                Multiplicative(StatModifier.Target.ProjectileDamage, 0.6f),
            },
        });

        // ── Spread cards ──────────────────────────────────────────────────────

        CreateCard(new CardDef
        {
            name     = "Buckshot",
            positive = "Fires 3 bullets per shot in a wide 20° spread",
            negative = "Each bullet deals only 50% damage — total output cut by 25%",
            color    = new Color(0.9f, 0.6f, 0.2f),
            hasSpecial  = true,
            specialID   = "buckshot",
            negativeMods = new[] { Multiplicative(StatModifier.Target.FireCooldown, 1.3f) },
        });

        CreateCard(new CardDef
        {
            name     = "Twin Barrel",
            positive = "Fires 2 bullets per shot in a tight 6° spread",
            negative = "Each bullet deals only 70% damage, reload takes 25% longer",
            color    = new Color(0.7f, 0.7f, 0.3f),
            hasSpecial  = true,
            specialID   = "twin_barrel",
            negativeMods = new[]
            {
                Multiplicative(StatModifier.Target.FireCooldown, 1.25f),
            },
        });

        // ── Bullet type cards ─────────────────────────────────────────────────

        CreateCard(new CardDef
        {
            name     = "Explosive Rounds",
            positive = "Bullets explode on impact dealing AoE damage",
            negative = "Bullets travel 20% slower, fire rate 25% slower",
            color    = new Color(1.0f, 0.3f, 0.1f),
            hasSpecial  = true,
            specialID   = "explosive_rounds",
            negativeMods = new[]
            {
                Multiplicative(StatModifier.Target.ProjectileSpeed, 0.8f),
                Multiplicative(StatModifier.Target.FireCooldown,    1.25f),
            },
        });

        CreateCard(new CardDef
        {
            name     = "Seeking Rounds",
            positive = "Bullets curve toward the nearest enemy",
            negative = "Bullets travel 30% slower and deal 20% less damage",
            color    = new Color(0.4f, 0.9f, 0.7f),
            hasSpecial  = true,
            specialID   = "seeking_rounds",
            negativeMods = new[]
            {
                Multiplicative(StatModifier.Target.ProjectileSpeed,  0.7f),
                Multiplicative(StatModifier.Target.ProjectileDamage, 0.8f),
            },
        });

        CreateCard(new CardDef
        {
            name     = "Fragmentation",
            positive = "Bullets split into 3 fragments on impact",
            negative = "Fire rate is 40% slower",
            color    = new Color(0.8f, 0.5f, 0.1f),
            hasSpecial  = true,
            specialID   = "fragmentation",
            negativeMods = new[] { Multiplicative(StatModifier.Target.FireCooldown, 1.4f) },
        });

        CreateCard(new CardDef
        {
            name     = "Ricochet Rounds",
            positive = "Bullets redirect toward enemies after bouncing off walls",
            negative = "Bullets deal 20% less damage",
            color    = new Color(0.3f, 0.7f, 1.0f),
            hasSpecial  = true,
            specialID   = "ricochet_rounds",
            negativeMods = new[] { Multiplicative(StatModifier.Target.ProjectileDamage, 0.8f) },
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CreateArenaCards] Done. Cards saved to {OutputFolder}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private struct CardDef
    {
        public string          name;
        public string          positive;
        public string          negative;
        public Color           color;
        public StatModifier[]  positiveMods;
        public StatModifier[]  negativeMods;
        public bool            hasSpecial;
        public string          specialID;
    }

    private static void CreateCard(CardDef def)
    {
        string path = $"{OutputFolder}/{def.name}.asset";
        if (AssetDatabase.LoadAssetAtPath<PowerCardData>(path) != null)
        {
            Debug.Log($"[CreateArenaCards] Skipping '{def.name}' — already exists.");
            return;
        }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName             = def.name;
        card.positiveDescription  = def.positive;
        card.negativeDescription  = def.negative;
        card.cardColor            = def.color;
        card.tier                 = 1;
        card.classTags            = new[] { "General" };
        card.positiveEffects      = def.positiveMods  ?? new StatModifier[0];
        card.negativeEffects      = def.negativeMods  ?? new StatModifier[0];
        card.hasSpecialBehavior   = def.hasSpecial;
        card.specialBehaviorID    = def.specialID ?? "";

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[CreateArenaCards] Created '{def.name}' at {path}");
    }

    private static StatModifier Multiplicative(StatModifier.Target target, float multiplier)
        => new StatModifier { target = target, modType = StatModifier.ModType.Multiplicative, value = multiplier };
}
