/// <summary>
/// Seeking Rounds: bullets gently curve toward the nearest enemy.
/// Speed and damage penalties live on the card's negativeEffects array.
/// Stacks increase turn rate so bullets curve more aggressively.
/// </summary>
public class SeekingRoundsEffect : SpellEffect
{
    protected override void OnApply()
    {
        var modSystem = GetComponent<ProjectileModifierSystem>();
        if (modSystem == null) return;

        modSystem.AddModifier(new ProjectileModifier
        {
            type           = ProjectileModifier.ModifierType.Homing,
            homingStrength = 80f + (StackCount - 1) * 30f,
            homingRadius   = 6f,
        });
    }
}
