/// <summary>
/// Overdrive: bullets deal 50% more damage and travel 40% faster,
/// but reload takes 150% longer (2.5× the normal time).
/// High burst potential with a massive vulnerability window between magazines.
/// The stat modifiers for damage/speed live on the card's positiveEffects array;
/// this effect only handles the reload penalty.
/// </summary>
public class OverdriveEffect : SpellEffect
{
    private const float ReloadMultiplier = 2.5f; // 150% slower each stack

    protected override void OnApply()
    {
        if (Spawner == null) return;
        Spawner.RefillTime *= ReloadMultiplier;
    }
}
