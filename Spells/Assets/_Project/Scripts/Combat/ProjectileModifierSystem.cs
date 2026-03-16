using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active projectile modifiers for a player.
/// Sits on the player and processes each projectile after spawn.
/// Modifiers are added by SpellEffects (card behaviors) and
/// persist across rounds within a match.
///
/// Modifier stacking: multiple modifiers of the same type increase
/// their values (e.g., two Homing modifiers double the turn rate).
/// </summary>
public class ProjectileModifierSystem : MonoBehaviour
{
    private readonly List<ProjectileModifier> activeModifiers = new List<ProjectileModifier>();
    private PlayerIdentity identity;

    private void Awake()
    {
        identity = GetComponent<PlayerIdentity>();
    }

    /// <summary>
    /// Add a modifier. Called by SpellEffects when cards are picked.
    /// </summary>
    public void AddModifier(ProjectileModifier modifier)
    {
        activeModifiers.Add(modifier);
    }

    /// <summary>
    /// Remove all modifiers. Called on match reset.
    /// </summary>
    public void ClearModifiers()
    {
        activeModifiers.Clear();
    }

    /// <summary>
    /// Check if any modifiers of a given type are active.
    /// </summary>
    public bool HasModifier(ProjectileModifier.ModifierType type)
    {
        foreach (var mod in activeModifiers)
        {
            if (mod.type == type) return true;
        }
        return false;
    }

    /// <summary>
    /// Apply all active modifiers to a newly spawned projectile.
    /// Called by ProjectileSpawner after instantiation.
    /// </summary>
    public void ProcessProjectile(GameObject projectileObj)
    {
        if (activeModifiers.Count == 0) return;

        foreach (var mod in activeModifiers)
        {
            switch (mod.type)
            {
                case ProjectileModifier.ModifierType.Homing:
                    ApplyHoming(projectileObj, mod);
                    break;
                case ProjectileModifier.ModifierType.Explosive:
                    ApplyExplosive(projectileObj, mod);
                    break;
                case ProjectileModifier.ModifierType.Ricochet:
                    ApplyRicochet(projectileObj, mod);
                    break;
                // Split is handled on impact, not on spawn
                case ProjectileModifier.ModifierType.Split:
                    ApplySplit(projectileObj, mod);
                    break;
            }
        }
    }

    // =============================================
    // Modifier Application
    // =============================================

    private void ApplyHoming(GameObject projectileObj, ProjectileModifier mod)
    {
        var homing = projectileObj.AddComponent<HomingBehavior>();
        homing.Initialize(mod.homingStrength, mod.homingRadius, identity != null ? identity.PlayerID : -1);
    }

    private void ApplyExplosive(GameObject projectileObj, ProjectileModifier mod)
    {
        var explosive = projectileObj.AddComponent<ExplosiveBehavior>();
        explosive.Initialize(mod.explosionRadius, mod.explosionDamageMultiplier, mod.explosionKnockback);
    }

    private void ApplyRicochet(GameObject projectileObj, ProjectileModifier mod)
    {
        var ricochet = projectileObj.AddComponent<RicochetBehavior>();
        ricochet.Initialize(mod.ricochetAimAssist, identity != null ? identity.PlayerID : -1);
    }

    private void ApplySplit(GameObject projectileObj, ProjectileModifier mod)
    {
        var split = projectileObj.AddComponent<SplitBehavior>();
        split.Initialize(mod.splitCount, mod.splitSpreadAngle, mod.splitDamageMultiplier);
    }
}
