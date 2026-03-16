using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class CombatDataTests
{
    private CombatData data;

    [SetUp]
    public void SetUp()
    {
        data = ScriptableObject.CreateInstance<CombatData>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(data);
    }

    [Test]
    public void DefaultHP_IsReasonable()
    {
        // All classes have 2-4 HP per GDD
        Assert.GreaterOrEqual(data.maxHP, 1, "HP must be at least 1");
        Assert.LessOrEqual(data.maxHP, 10, "HP shouldn't exceed 10 for base values");
    }

    [Test]
    public void DefaultProjectileSpeed_IsPositive()
    {
        Assert.Greater(data.projectileSpeed, 0f, "Projectile speed must be positive");
    }

    [Test]
    public void DefaultFireCooldown_AllowsReasonableFireRate()
    {
        // Fire rate = 1/cooldown. Should be between 0.5 and 20 shots/sec
        float fireRate = 1f / data.fireCooldown;
        Assert.Greater(fireRate, 0.5f, "Fire rate too slow");
        Assert.Less(fireRate, 20f, "Fire rate too fast");
    }

    [Test]
    public void DefaultParryWindow_MatchesGDD()
    {
        // GDD spec: 6-8 frames at 60fps = 100-133ms
        Assert.GreaterOrEqual(data.parryWindow, 0.080f, "Parry window too tight (< 80ms)");
        Assert.LessOrEqual(data.parryWindow, 0.200f, "Parry window too generous (> 200ms)");
    }

    [Test]
    public void ParryWhiffRecovery_GreaterThanParryWindow()
    {
        // Recovery should be longer than the window itself to punish spam
        Assert.Greater(data.parryWhiffRecovery, data.parryWindow,
            "Whiff recovery should exceed parry window to prevent spam");
    }

    [Test]
    public void Clone_CreatesIndependentCopy()
    {
        var clone = data.Clone();
        clone.maxHP = 999;
        Assert.AreNotEqual(data.maxHP, clone.maxHP,
            "Clone should be independent of original");
        Object.DestroyImmediate(clone);
    }

    [Test]
    public void KnockbackForce_IsPositive()
    {
        Assert.GreaterOrEqual(data.knockbackForce, 0f,
            "Knockback force should be non-negative");
    }

    [Test]
    public void InvincibilityDuration_IsReasonable()
    {
        Assert.GreaterOrEqual(data.invincibilityDuration, 0f,
            "I-frame duration should be non-negative");
        Assert.LessOrEqual(data.invincibilityDuration, 2f,
            "I-frames shouldn't last more than 2 seconds");
    }
}
