using UnityEngine;

/// <summary>
/// Glass Cannon: sets max HP to 1 but doubles projectile damage.
/// Stacking further multiplies damage (2x per stack) but HP stays at 1.
///
/// This is a dramatic high-risk card — one hit kills you, but your
/// projectiles become devastating. Taking it 3 times = 8x damage at 1 HP.
/// </summary>
public class GlassCannonEffect : SpellEffect
{
    private int originalMaxHP;
    private bool hasStoredOriginalHP;

    protected override void OnApply()
    {
        if (Class == null || Class.CombatData == null) return;

        // Store original HP only on first application
        if (!hasStoredOriginalHP && Health != null)
        {
            originalMaxHP = Health.MaxHP;
            hasStoredOriginalHP = true;
        }

        // Set HP to 1
        if (Health != null)
        {
            int delta = 1 - Health.MaxHP;
            Health.ModifyMaxHP(delta);
        }

        // Double damage per stack (applied multiplicatively each time)
        var combatData = Class.CombatData;
        combatData.projectileDamage *= 2f;
    }

    public override void OnRemove()
    {
        // Restore original HP on match reset
        // (CombatData is cloned per player, so damage changes die with it)
    }

    public override void OnRoundStart()
    {
        // Enforce 1 HP at round start even after ResetForRound restores MaxHP
        if (Health != null && Health.MaxHP > 1)
        {
            int delta = 1 - Health.MaxHP;
            Health.ModifyMaxHP(delta);
        }
    }
}
