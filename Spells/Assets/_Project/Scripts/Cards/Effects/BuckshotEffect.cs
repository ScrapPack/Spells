/// <summary>
/// Buckshot: fires 3 bullets per shot in a 20° spread.
/// Each bullet deals 50% damage so total DPS stays sane.
/// Fire cooldown penalty lives on the card's negativeEffects StatModifier array.
/// Stacks add another bullet and widen the arc by 5° per stack.
/// </summary>
public class BuckshotEffect : SpellEffect
{
    protected override void OnApply()
    {
        if (Spawner == null) return;
        // First stack: 1→3 bullets, 0→20° arc, 100%→50% per-bullet damage.
        // Subsequent stacks: +1 bullet, +5° arc (damage already halved).
        if (StackCount == 1)
        {
            Spawner.BulletSpreadCount  += 2;
            Spawner.BulletSpreadAngle  += 20f;
            Spawner.SpreadDamageMultiplier *= 0.5f;
        }
        else
        {
            Spawner.BulletSpreadCount  += 1;
            Spawner.BulletSpreadAngle  += 5f;
        }
    }
}
