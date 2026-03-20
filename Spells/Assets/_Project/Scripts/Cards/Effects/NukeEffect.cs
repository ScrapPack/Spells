/// <summary>
/// NUKE: magazine holds only 1 shot. That shot is a massive explosive round —
/// 10-unit blast radius, 2× AoE damage multiplier, huge knockback. The direct hit
/// also deals 2× damage. Bullets travel 60% slower so aiming matters.
/// Stacks increase explosion radius and damage further.
/// </summary>
public class NukeEffect : SpellEffect
{
    protected override void OnApply()
    {
        if (Spawner == null) return;

        if (StackCount == 1)
        {
            // Reduce max ammo to 1
            int delta = -(Spawner.MaxAmmo - 1);
            if (delta < 0) Spawner.AdjustMaxAmmo(delta);

            // Slow bullets by 60%
            Spawner.ProjectileSpeed *= 0.4f;

            // Boost direct hit damage
            Spawner.SpreadDamageMultiplier *= 2f;
        }

        // Each stack (including first): add a massive explosion modifier
        var modSystem = GetComponent<ProjectileModifierSystem>();
        if (modSystem != null)
        {
            modSystem.AddModifier(new ProjectileModifier
            {
                type                      = ProjectileModifier.ModifierType.Explosive,
                explosionRadius           = 8f + (StackCount - 1) * 3f,
                explosionDamageMultiplier = 2f + (StackCount - 1) * 0.5f,
                explosionKnockback        = 20f + (StackCount - 1) * 5f,
            });
        }
    }
}
