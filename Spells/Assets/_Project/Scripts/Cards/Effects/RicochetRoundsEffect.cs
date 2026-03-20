/// <summary>
/// Ricochet Rounds: bullets redirect toward the nearest enemy after bouncing.
/// Damage penalty lives on the card's negativeEffects array.
/// Stacks widen the aim-assist cone so wilder bounces still connect.
/// </summary>
public class RicochetRoundsEffect : SpellEffect
{
    protected override void OnApply()
    {
        var modSystem = GetComponent<ProjectileModifierSystem>();
        if (modSystem == null) return;

        modSystem.AddModifier(new ProjectileModifier
        {
            type             = ProjectileModifier.ModifierType.Ricochet,
            ricochetAimAssist = 50f + (StackCount - 1) * 15f,
        });
    }
}
