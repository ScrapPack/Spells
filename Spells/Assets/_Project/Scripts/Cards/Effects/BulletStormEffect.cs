/// <summary>
/// Bullet Storm: every shot fires a 12-bullet 360° ring for massive area coverage.
/// Each bullet deals only 20% damage — total burst damage equals ~2.4× normal.
/// Stacks add 4 more bullets per stack.
/// </summary>
public class BulletStormEffect : SpellEffect
{
    protected override void OnApply()
    {
        if (Spawner == null) return;

        if (StackCount == 1)
        {
            Spawner.BulletSpreadCount  = 12;
            Spawner.BulletSpreadAngle  = 360f;
            Spawner.SpreadDamageMultiplier *= 0.2f;
        }
        else
        {
            // Each extra stack adds 4 more bullets; damage stays the same
            Spawner.BulletSpreadCount += 4;
        }
    }
}
