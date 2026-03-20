using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps specialBehaviorID strings to SpellEffect component types.
/// When a card with hasSpecialBehavior is picked, this registry
/// creates (or stacks) the correct SpellEffect on the player.
///
/// Lives as a singleton — BoxArenaBuilder creates it at runtime.
/// </summary>
public class SpellEffectRegistry : MonoBehaviour
{
    public static SpellEffectRegistry Instance { get; private set; }

    /// <summary>
    /// Map of behavior ID → SpellEffect component type.
    /// Registered at startup.
    /// </summary>
    private readonly Dictionary<string, Type> effectTypes = new Dictionary<string, Type>();

    private void Awake()
    {
        Instance = this;
        RegisterBuiltInEffects();
    }

    /// <summary>
    /// Register all known special behaviors. Add new ones here.
    /// </summary>
    private void RegisterBuiltInEffects()
    {
        // General cards
        Register("vampiric",       typeof(VampiricEffect));
        Register("glass_cannon",   typeof(GlassCannonEffect));
        Register("second_wind",    typeof(SecondWindEffect));
        Register("hair_trigger",       typeof(HairTriggerEffect));
        Register("extended_clip",     typeof(ExtendedClipEffect));
        Register("overdrive",         typeof(OverdriveEffect));
        Register("quick_draw",        typeof(QuickDrawEffect));
        // Spread cards
        Register("buckshot",          typeof(BuckshotEffect));
        Register("twin_barrel",       typeof(TwinBarrelEffect));
        // Bullet type cards
        Register("explosive_rounds",  typeof(ExplosiveRoundsEffect));
        Register("seeking_rounds",    typeof(SeekingRoundsEffect));
        Register("fragmentation",     typeof(FragmentationEffect));
        Register("ricochet_rounds",   typeof(RicochetRoundsEffect));

        // Warlock cards
        Register("blood_pact", typeof(BloodPactEffect));
        Register("soul_siphon", typeof(SoulSiphonEffect));
        Register("lich_form", typeof(LichFormEffect));

        // Warrior cards
        Register("heavy_throw", typeof(HeavyThrowEffect));
        Register("magnetic_return", typeof(MagneticReturnEffect));
        Register("berserker", typeof(BerserkerEffect));

        // Jester cards
        Register("jackpot", typeof(JackpotEffect));
        Register("lucky_bounce", typeof(LuckyBounceEffect));

        // Witch Doctor cards
        Register("venom_dart", typeof(VenomDartEffect));
        Register("hex_mark", typeof(HexMarkEffect));

        // Alchemist cards
        Register("sticky_brew", typeof(StickyBrewEffect));
        Register("volatile_mix", typeof(VolatileMixEffect));

        // Shaman cards
        Register("spirit_bond", typeof(SpiritBondEffect));
        Register("ancestral_totem", typeof(AncestralTotemEffect));

        // Rogue cards
        Register("ambush", typeof(AmbushEffect));
        Register("smoke_bomb", typeof(SmokeBombEffect));

        // Warlock Tier 2
        Register("dark_tether", typeof(DarkTetherEffect));

        // General — Charge Shot
        Register("charge_shot", typeof(ChargeShotEffect));

        // Super Cards
        Register("full_auto",        typeof(FullAutoEffect));
        Register("bullet_storm",     typeof(BulletStormEffect));
        Register("death_blossom",    typeof(DeathBlossomEffect));
        Register("nuke",             typeof(NukeEffect));
        Register("ricochet_hell",    typeof(RicochetHellEffect));
        Register("chain_lightning",  typeof(ChainLightningEffect));
        Register("energy_orb",       typeof(EnergyOrbEffect));
    }

    /// <summary>
    /// Register a behavior ID → type mapping.
    /// </summary>
    public void Register(string behaviorID, Type effectType)
    {
        if (!typeof(SpellEffect).IsAssignableFrom(effectType))
        {
            Debug.LogError($"SpellEffectRegistry: {effectType.Name} does not inherit from SpellEffect!");
            return;
        }
        effectTypes[behaviorID] = effectType;
    }

    /// <summary>
    /// Apply a special card behavior to a player.
    /// If the player already has this effect, increments its stack count.
    /// Returns true if successfully applied.
    /// </summary>
    public bool ApplyEffect(GameObject player, PowerCardData card)
    {
        if (!card.hasSpecialBehavior || string.IsNullOrEmpty(card.specialBehaviorID))
            return false;

        if (!effectTypes.ContainsKey(card.specialBehaviorID))
        {
            Debug.LogWarning($"SpellEffectRegistry: Unknown behavior '{card.specialBehaviorID}'");
            return false;
        }

        Type effectType = effectTypes[card.specialBehaviorID];

        // Check if already has this effect (stacking)
        var existing = player.GetComponent(effectType) as SpellEffect;
        if (existing != null)
        {
            // Re-initialize with incremented stack count
            existing.Initialize(card, existing.StackCount + 1);
            return true;
        }

        // Add new effect
        var effect = player.AddComponent(effectType) as SpellEffect;
        if (effect != null)
        {
            effect.Initialize(card, 1);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Remove all spell effects from a player. Called on match reset.
    /// </summary>
    public void RemoveAllEffects(GameObject player)
    {
        var effects = player.GetComponents<SpellEffect>();
        foreach (var effect in effects)
        {
            effect.OnRemove();
            Destroy(effect);
        }
    }

    /// <summary>
    /// Notify all spell effects on a player that a new round is starting.
    /// </summary>
    public void NotifyRoundStart(GameObject player)
    {
        var effects = player.GetComponents<SpellEffect>();
        foreach (var effect in effects)
            effect.OnRoundStart();
    }

    /// <summary>
    /// Notify all spell effects on a player that the round has ended.
    /// </summary>
    public void NotifyRoundEnd(GameObject player)
    {
        var effects = player.GetComponents<SpellEffect>();
        foreach (var effect in effects)
            effect.OnRoundEnd();
    }
}
