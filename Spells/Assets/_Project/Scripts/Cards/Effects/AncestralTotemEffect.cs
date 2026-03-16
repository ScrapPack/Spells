using UnityEngine;

/// <summary>
/// Ancestral Totem (Shaman, Tier 1): Drop a totem that shoots at nearby enemies.
/// Totem also shoots at you if you're closest.
///
/// GDD: "Drop a totem that shoots at nearby enemies.
/// Totem also shoots at you if you're closest."
///
/// Creates a stationary turret at the player's position at round start.
/// The totem auto-targets the nearest player — including the owner.
/// Positioning matters: stay away from your own totem, and try to lure
/// enemies near it.
///
/// Stacking: Each stack increases totem fire rate.
/// </summary>
public class AncestralTotemEffect : SpellEffect
{
    private int stackLevel;
    private GameObject activeTotem;

    protected override void OnApply()
    {
        stackLevel = StackCount;
        SpawnTotem();
    }

    public override void OnRoundStart()
    {
        // Destroy old totem and spawn fresh
        if (activeTotem != null)
            Object.Destroy(activeTotem);

        SpawnTotem();
    }

    private void SpawnTotem()
    {
        if (Identity == null) return;

        // Create totem at player's current position
        activeTotem = new GameObject($"AncestralTotem_P{Identity.PlayerID}");
        activeTotem.transform.position = transform.position;

        var totem = activeTotem.AddComponent<TotemEntity>();

        // Get the projectile prefab from the class data
        GameObject projPrefab = null;
        if (Class != null && Class.CurrentClass != null)
            projPrefab = Class.CurrentClass.projectilePrefab;

        totem.Initialize(
            Identity.PlayerID,
            projPrefab,
            true // Can target owner (the downside!)
        );

        totem.FireRate = 0.5f + (stackLevel - 1) * 0.25f; // Faster with stacks
        totem.DetectionRadius = 8f;
        totem.Damage = 1f;
        totem.ProjectileSpeed = 12f;
    }

    public override void OnRemove()
    {
        if (activeTotem != null)
            Object.Destroy(activeTotem);
    }
}
