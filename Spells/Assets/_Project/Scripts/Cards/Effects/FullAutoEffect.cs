/// <summary>
/// Full Auto: hold the shoot button to fire continuously at 3× the normal rate.
/// Each bullet deals 50% damage. Additional stacks restore 10% damage per stack.
/// </summary>
public class FullAutoEffect : SpellEffect
{
    protected override void OnApply()
    {
        if (Spawner == null) return;

        Spawner.FullAutoMode = true;
        Spawner.FullAutoFireCooldownMultiplier = 1f / 3f;

        if (StackCount == 1)
        {
            // First pick: cut damage in half
            Spawner.SpreadDamageMultiplier *= 0.5f;
        }
        else
        {
            // Each additional stack: restore 10% of the original damage
            Spawner.SpreadDamageMultiplier *= 1.1f;
        }
    }

    public override void OnRemove()
    {
        if (Spawner == null) return;
        Spawner.FullAutoMode = false;
        Spawner.FullAutoFireCooldownMultiplier = 1f;
    }
}
