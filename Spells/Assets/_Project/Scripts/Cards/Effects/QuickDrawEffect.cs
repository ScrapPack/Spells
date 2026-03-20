/// <summary>
/// Quick Draw: reload is 60% faster, but bullets deal 40% less damage.
/// Aggressive harassment card — constant pressure with lighter hits.
/// The damage penalty lives on the card's negativeEffects array;
/// this effect only handles the reload bonus.
/// </summary>
public class QuickDrawEffect : SpellEffect
{
    private const float ReloadMultiplier = 0.4f; // 60% faster each stack

    protected override void OnApply()
    {
        if (Spawner == null) return;
        Spawner.RefillTime *= ReloadMultiplier;
    }
}
