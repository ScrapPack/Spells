using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the 6 Super Cards under Assets/_Project/Data/Cards/Super/.
/// Run via: Tools → Spells → Create Super Cards
/// Safe to run multiple times — skips cards that already exist.
/// </summary>
public static class CreateSuperCards
{
    private const string OutputFolder = "Assets/_Project/Data/Cards/Super";

    [MenuItem("Tools/Spells/Create Super Cards")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            string parent = "Assets/_Project/Data/Cards";
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Cards");
            AssetDatabase.CreateFolder(parent, "Super");
        }

        // ── Full Auto ─────────────────────────────────────────────────────────

        CreateCard(new CardDef
        {
            name        = "Full Auto",
            positive    = "Hold shoot for continuous fire at 3× the normal rate",
            negative    = "Each bullet deals only 50% damage",
            color       = new Color(1.0f, 0.7f, 0.1f),
            hasSpecial  = true,
            specialID   = "full_auto",
        });

        // ── Bullet Storm ──────────────────────────────────────────────────────

        CreateCard(new CardDef
        {
            name        = "Bullet Storm",
            positive    = "Every shot fires 12 bullets in a full 360° ring",
            negative    = "Each bullet deals only 20% damage (total ~2.4× normal)",
            color       = new Color(0.2f, 0.6f, 1.0f),
            hasSpecial  = true,
            specialID   = "bullet_storm",
        });

        // ── Death Blossom ─────────────────────────────────────────────────────

        CreateCard(new CardDef
        {
            name        = "Death Blossom",
            positive    = "Reloading auto-fires a free 16-bullet ring at 30% damage",
            negative    = "Reload takes 80% longer",
            color       = new Color(0.9f, 0.2f, 0.5f),
            hasSpecial  = true,
            specialID   = "death_blossom",
            negativeMods = new[] { Multiplicative(StatModifier.Target.FireCooldown, 1.8f) },
        });

        // ── NUKE ──────────────────────────────────────────────────────────────

        CreateCard(new CardDef
        {
            name        = "NUKE",
            positive    = "1 shot that explodes in a massive 8-unit radius at 2× AoE damage",
            negative    = "Max 1 ammo. Bullets travel 60% slower",
            color       = new Color(1.0f, 0.2f, 0.0f),
            hasSpecial  = true,
            specialID   = "nuke",
        });

        // ── Ricochet Hell ─────────────────────────────────────────────────────

        CreateCard(new CardDef
        {
            name        = "Ricochet Hell",
            positive    = "Bullets bounce 10 times and gain +20% damage per bounce",
            negative    = "Bullets deal 30% less damage on direct hit",
            color       = new Color(0.1f, 0.9f, 0.5f),
            hasSpecial  = true,
            specialID   = "ricochet_hell",
            negativeMods = new[] { Multiplicative(StatModifier.Target.ProjectileDamage, 0.7f) },
        });

        // ── Chain Lightning ───────────────────────────────────────────────────

        CreateCard(new CardDef
        {
            name        = "Chain Lightning",
            positive    = "Every bullet hit chains 50% of its damage to the nearest enemy in 6 units",
            negative    = "Bullets deal 30% less direct damage",
            color       = new Color(0.6f, 0.8f, 1.0f),
            hasSpecial  = true,
            specialID   = "chain_lightning",
            negativeMods = new[] { Multiplicative(StatModifier.Target.ProjectileDamage, 0.7f) },
        });

        // ── Energy Orb ────────────────────────────────────────────────────────

        CreateCard(new CardDef
        {
            name        = "Energy Orb",
            positive    = "Fires a massive orb that bounces endlessly and deals 5× damage — parry it back!",
            negative    = "Only 1 ammo. Orb travels at 25% speed. 2× reload time.",
            color       = new Color(0.4f, 0.9f, 1.0f),
            hasSpecial  = true,
            specialID   = "energy_orb",
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CreateSuperCards] Done. Cards saved to {OutputFolder}");
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
            Debug.Log($"[CreateSuperCards] Skipping '{def.name}' — already exists.");
            return;
        }

        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName             = def.name;
        card.positiveDescription  = def.positive;
        card.negativeDescription  = def.negative;
        card.cardColor            = def.color;
        card.tier                 = 3;
        card.classTags            = new[] { "General" };
        card.positiveEffects      = def.positiveMods ?? new StatModifier[0];
        card.negativeEffects      = def.negativeMods ?? new StatModifier[0];
        card.hasSpecialBehavior   = def.hasSpecial;
        card.specialBehaviorID    = def.specialID ?? "";

        AssetDatabase.CreateAsset(card, path);
        Debug.Log($"[CreateSuperCards] Created '{def.name}' at {path}");
    }

    private static StatModifier Multiplicative(StatModifier.Target target, float multiplier)
        => new StatModifier { target = target, modType = StatModifier.ModType.Multiplicative, value = multiplier };
}
