/// <summary>
/// Fragmentation: fires 3 bullets immediately in a wide 120° spread on each shot.
/// Each fragment deals 50% damage. Additional stacks add one more fragment per stack.
/// Fire-rate penalty lives on the card's negativeEffects array.
/// </summary>
public class FragmentationEffect : SpellEffect
{
    protected override void OnApply()
    {
        if (Spawner == null) return;

        if (StackCount == 1)
        {
            Spawner.BulletSpreadCount      = 3;
            Spawner.BulletSpreadAngle      = 120f;
            Spawner.SpreadDamageMultiplier *= 0.5f;
        }
        else
        {
            // Each extra stack adds another fragment; spread stays 120°
            Spawner.BulletSpreadCount += 1;
        }
    }
}
