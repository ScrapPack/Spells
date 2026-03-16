using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for ProjectileModifier data structures.
/// Tests default values, serialization, and type logic.
/// (Behavior components like HomingBehavior need PlayMode tests.)
/// </summary>
[TestFixture]
public class ProjectileModifierTests
{
    [Test]
    public void DefaultSplit_HasReasonableValues()
    {
        var mod = new ProjectileModifier { type = ProjectileModifier.ModifierType.Split };
        Assert.AreEqual(3, mod.splitCount);
        Assert.Greater(mod.splitSpreadAngle, 0f);
        Assert.LessOrEqual(mod.splitDamageMultiplier, 1f,
            "Split fragments should deal less damage than original");
    }

    [Test]
    public void DefaultHoming_HasReasonableValues()
    {
        var mod = new ProjectileModifier { type = ProjectileModifier.ModifierType.Homing };
        Assert.Greater(mod.homingStrength, 0f, "Homing must have positive turn rate");
        Assert.Greater(mod.homingRadius, 0f, "Homing must have positive detection radius");
    }

    [Test]
    public void DefaultExplosive_HasReasonableValues()
    {
        var mod = new ProjectileModifier { type = ProjectileModifier.ModifierType.Explosive };
        Assert.Greater(mod.explosionRadius, 0f);
        Assert.LessOrEqual(mod.explosionDamageMultiplier, 1f,
            "AoE should deal less damage than direct hit");
        Assert.Greater(mod.explosionKnockback, 0f);
    }

    [Test]
    public void DefaultRicochet_AimAssistAngle_IsReasonable()
    {
        var mod = new ProjectileModifier { type = ProjectileModifier.ModifierType.Ricochet };
        Assert.Greater(mod.ricochetAimAssist, 0f);
        Assert.LessOrEqual(mod.ricochetAimAssist, 90f,
            "Aim assist > 90° would be too generous");
    }

    [Test]
    public void ModifierType_AllTypesExist()
    {
        // Verify all 4 modifier types are defined
        Assert.AreEqual(4, System.Enum.GetValues(typeof(ProjectileModifier.ModifierType)).Length);
    }

    [Test]
    public void SplitDamageMultiplier_PreventsPowerCreep()
    {
        var mod = new ProjectileModifier { type = ProjectileModifier.ModifierType.Split };
        // Total damage output: splitCount * splitDamageMultiplier
        // Should be <= 2x original to prevent exponential power scaling
        float totalDamageFactor = mod.splitCount * mod.splitDamageMultiplier;
        Assert.LessOrEqual(totalDamageFactor, 2f,
            "Split total damage should not exceed 2x original (prevent power creep)");
    }
}
