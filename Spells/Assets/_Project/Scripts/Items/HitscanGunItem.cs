using UnityEngine;

/// <summary>
/// Hitscan Gun item: replaces the player's CombatData with a near-hitscan
/// configuration. Very high speed, very short lifetime, 3 ammo.
/// Auto-unequips when ammo runs out.
///
/// Uses ItemData.combatDataOverride if set, otherwise creates a clone
/// with modified values. Restores original CombatData on unequip.
/// </summary>
public class HitscanGunItem : ItemBehavior
{
    private ClassManager classManager;
    private CombatData originalCombatData;
    private ProjectileSpawner spawner;
    private int ammoRemaining;

    public override void OnEquip()
    {
        classManager = GetComponent<ClassManager>();
        spawner = GetComponent<ProjectileSpawner>();
        if (classManager == null) return;

        ammoRemaining = ItemData != null && ItemData.ammo > 0 ? ItemData.ammo : 3;

        CombatData hitscanData;

        if (ItemData != null && ItemData.combatDataOverride != null)
        {
            hitscanData = ItemData.combatDataOverride.Clone();
        }
        else
        {
            hitscanData = classManager.CombatData.Clone();
            hitscanData.projectileSpeed = 200f;         // Near-instant
            hitscanData.projectileLifetime = 0.1f;      // Very short
            hitscanData.projectileDamage = 2f;           // High damage
            hitscanData.fireCooldown = 0.5f;             // Moderate fire rate
            hitscanData.knockbackForce = 15f;            // Heavy knockback
            hitscanData.projectileRadius = 0.08f;        // Thin hitbox
        }

        originalCombatData = classManager.SwapCombatData(hitscanData);

        // Subscribe to projectile fired event to track ammo
        if (spawner != null)
            spawner.OnProjectileFired.AddListener(OnFired);
    }

    public override void OnUnequip()
    {
        if (spawner != null)
            spawner.OnProjectileFired.RemoveListener(OnFired);

        if (classManager != null && originalCombatData != null)
        {
            classManager.SwapCombatData(originalCombatData);
            originalCombatData = null;
        }
    }

    private void OnFired()
    {
        ammoRemaining--;
        if (ammoRemaining <= 0 && Inventory != null)
        {
            Inventory.RemoveItem(ItemData);
        }
    }
}
