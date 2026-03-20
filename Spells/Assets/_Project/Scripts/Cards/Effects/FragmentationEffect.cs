/// <summary>
/// Fragmentation: bullets split into 3 fragments on impact.
/// Fragments deal 50% damage and don't split again.
/// Fire-rate penalty lives on the card's negativeEffects array.
/// Stacks add more fragments per split.
/// </summary>
public class FragmentationEffect : SpellEffect
{
    protected override void OnApply()
    {
        var modSystem = GetComponent<ProjectileModifierSystem>();
        if (modSystem == null) return;

        modSystem.AddModifier(new ProjectileModifier
        {
            type                 = ProjectileModifier.ModifierType.Split,
            splitCount           = 3 + (StackCount - 1),
            splitSpreadAngle     = 35f,
            splitDamageMultiplier = 0.5f,
        });
    }
}
