/// <summary>
/// Explosive Rounds: every bullet explodes on impact dealing AoE damage.
/// Speed and fire-rate penalties live on the card's negativeEffects array.
/// Stacks increase explosion radius and knockback.
/// </summary>
public class ExplosiveRoundsEffect : SpellEffect
{
    protected override void OnApply()
    {
        var modSystem = GetComponent<ProjectileModifierSystem>();
        if (modSystem == null) return;

        modSystem.AddModifier(new ProjectileModifier
        {
            type                      = ProjectileModifier.ModifierType.Explosive,
            explosionRadius           = 1.5f + (StackCount - 1) * 0.5f,
            explosionDamageMultiplier = 0.75f,
            explosionKnockback        = 8f + (StackCount - 1) * 2f,
        });
    }
}
