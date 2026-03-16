using UnityEngine;

/// <summary>
/// Heavy Throw (Warrior): Enables projectile piercing.
/// Axes pass through targets instead of stopping on hit.
///
/// GDD: "Axes deal 2x damage and pierce through targets.
/// Axes travel 50% slower."
///
/// The damage and speed modifiers are handled by stat modifiers.
/// This effect handles the pierce flag which is a boolean toggle,
/// not a numeric stat.
///
/// Stacking: piercing is binary (on/off). Additional stacks
/// don't add more piercing but the damage/speed mods compound.
/// </summary>
public class HeavyThrowEffect : SpellEffect
{
    protected override void OnApply()
    {
        if (Class == null || Class.CombatData == null) return;

        // Enable piercing on the cloned combat data
        Class.CombatData.projectilePierces = true;
    }

    public override void OnRoundStart()
    {
        // Re-enforce piercing (CombatData is cloned, persists)
        if (Class != null && Class.CombatData != null)
            Class.CombatData.projectilePierces = true;
    }

    public override void OnRemove()
    {
        // On match reset, combat data is re-cloned from asset
        // so no cleanup needed
    }
}
