/// <summary>
/// Twin Barrel: fires 2 bullets per shot in a tight 6° spread.
/// Each bullet deals 70% damage. Feels like doubling up without
/// losing too much accuracy. Reload penalty on the card's negativeEffects.
/// Stacks widen the spread by 4° and reduce damage 10% further per stack.
/// </summary>
public class TwinBarrelEffect : SpellEffect
{
    protected override void OnApply()
    {
        if (Spawner == null) return;
        if (StackCount == 1)
        {
            Spawner.BulletSpreadCount  += 1;
            Spawner.BulletSpreadAngle  += 6f;
            Spawner.SpreadDamageMultiplier *= 0.7f;
        }
        else
        {
            Spawner.BulletSpreadAngle  += 4f;
            Spawner.SpreadDamageMultiplier *= 0.9f;
        }
    }
}
