using UnityEngine;

/// <summary>
/// Second Wind (General, Tier 2): Grants double jump. Removes wall slide.
///
/// GDD: "Double jump. Can't wall slide."
///
/// This creates a totally different movement feel: more vertical aerial
/// combat but no wall recovery. Great for aggressive airborne play,
/// terrible for defensive wall camping.
///
/// Stacking: Each stack adds +1 more air jump (x1 = double jump,
/// x2 = triple jump). Wall slide remains disabled.
/// </summary>
public class SecondWindEffect : SpellEffect
{
    private PlayerController controller;

    protected override void OnApply()
    {
        controller = GetComponent<PlayerController>();
        if (controller == null || controller.Data == null) return;

        // Add air jumps (1 per stack)
        controller.Data.maxAirJumps = StackCount;

        // Disable wall slide by setting min/max speed very high
        // (player falls too fast to slide)
        controller.Data.wallSlideSpeedMin = 50f;
        controller.Data.wallSlideSpeedMax = 50f;
    }

    public override void OnRoundStart()
    {
        // Re-enforce after potential round resets
        if (controller != null && controller.Data != null)
        {
            controller.Data.maxAirJumps = StackCount;
            controller.Data.wallSlideSpeedMin = 50f;
            controller.Data.wallSlideSpeedMax = 50f;
        }
    }

    public override void OnRemove()
    {
        // On match reset, MovementData is re-cloned from asset
        // so no cleanup needed
    }
}
