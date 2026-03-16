using UnityEngine;

/// <summary>
/// Fire Wand item: replaces the player's CombatData with a rapid-fire
/// short-range flamethrower configuration. Very fast fire rate, short lifetime,
/// piercing projectiles, low damage per hit but high DPS.
///
/// Uses ItemData.combatDataOverride if set, otherwise creates a clone
/// with modified values. Restores original CombatData on unequip.
/// </summary>
public class FireWandItem : ItemBehavior
{
    private ClassManager classManager;
    private CombatData originalCombatData;

    public override void OnEquip()
    {
        classManager = GetComponent<ClassManager>();
        if (classManager == null) return;

        CombatData fireWandData;

        if (ItemData != null && ItemData.combatDataOverride != null)
        {
            // Use the pre-configured override from the asset
            fireWandData = ItemData.combatDataOverride.Clone();
        }
        else
        {
            // Create fire wand config from current data
            fireWandData = classManager.CombatData.Clone();
            fireWandData.fireCooldown = 0.06f;       // Very fast fire rate
            fireWandData.projectileLifetime = 0.4f;   // Short range
            fireWandData.projectileDamage = 0.3f;     // Low per-hit
            fireWandData.projectileSpeed = 25f;        // Fast spray
            fireWandData.projectilePierces = true;     // Piercing
            fireWandData.projectileRadius = 0.2f;      // Wider hitbox
            fireWandData.knockbackForce = 2f;          // Low knockback
        }

        originalCombatData = classManager.SwapCombatData(fireWandData);
    }

    public override void OnUnequip()
    {
        if (classManager != null && originalCombatData != null)
        {
            classManager.SwapCombatData(originalCombatData);
            originalCombatData = null;
        }
    }
}
