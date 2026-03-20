/// <summary>
/// Hair Trigger: reload is 40% faster, but max ammo is reduced by 2.
/// Forces a trade of staying power for tempo — you reload constantly
/// but each reload fills a smaller magazine.
/// </summary>
public class HairTriggerEffect : SpellEffect
{
    private const float ReloadMultiplier = 0.6f; // 40% faster each stack
    private const int   AmmoDelta        = -2;

    protected override void OnApply()
    {
        if (Spawner == null) return;
        Spawner.RefillTime *= ReloadMultiplier;
        Spawner.AdjustMaxAmmo(AmmoDelta);
    }
}
