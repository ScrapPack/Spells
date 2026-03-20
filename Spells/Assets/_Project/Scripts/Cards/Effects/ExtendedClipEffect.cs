/// <summary>
/// Extended Clip: 3 extra shots per magazine, but reload takes 80% longer.
/// A war of attrition card — you can sustain fire longer but getting caught
/// mid-reload is brutal.
/// </summary>
public class ExtendedClipEffect : SpellEffect
{
    private const float ReloadMultiplier = 1.8f; // 80% slower each stack
    private const int   AmmoDelta        = 3;

    protected override void OnApply()
    {
        if (Spawner == null) return;
        Spawner.RefillTime *= ReloadMultiplier;
        Spawner.AdjustMaxAmmo(AmmoDelta);
    }
}
