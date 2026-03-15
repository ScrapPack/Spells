using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class MovementDataTests
{
    private MovementData data;

    [SetUp]
    public void SetUp()
    {
        data = ScriptableObject.CreateInstance<MovementData>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(data);
    }

    [Test]
    public void DefaultJumpForce_GivesReasonableApexHeight()
    {
        // Apex height formula: h = v² / (2 * g)
        // where g = gravityScale * 9.81
        float g = data.gravityScale * 9.81f;
        float apex = (data.jumpForce * data.jumpForce) / (2f * g);

        // Platform fighter sweet spot: 2-4 units apex height
        Assert.Greater(apex, 2f, "Jump apex too low for platform fighter");
        Assert.Less(apex, 5f, "Jump apex too high for platform fighter");
    }

    [Test]
    public void FallGravityMultiplier_GreaterThanOne()
    {
        // Fall gravity must be heavier than base for snappy descent
        Assert.Greater(data.fallGravityMultiplier, 1f,
            "Fall gravity should be heavier than base gravity");
    }

    [Test]
    public void PeakGravityMultiplier_LessThanOne()
    {
        // Peak gravity must be lighter for hang time
        Assert.Less(data.peakGravityMultiplier, 1f,
            "Peak gravity should be lighter than base for hang time");
    }

    [Test]
    public void MaxFallSpeed_CapsTerminalVelocity()
    {
        // Max fall speed should be reachable but not instant
        float g = data.gravityScale * 9.81f * data.fallGravityMultiplier;
        float timeToTerminal = data.maxFallSpeed / g;

        Assert.Greater(timeToTerminal, 0.1f, "Reaches terminal velocity too fast");
        Assert.Less(timeToTerminal, 3f, "Takes too long to reach terminal velocity");
    }

    [Test]
    public void CoyoteTime_WithinAcceptableRange()
    {
        // Standard coyote time: 60-150ms
        Assert.GreaterOrEqual(data.coyoteTimeDuration, 0.05f,
            "Coyote time too short to be useful");
        Assert.LessOrEqual(data.coyoteTimeDuration, 0.2f,
            "Coyote time too generous — feels floaty");
    }

    [Test]
    public void WaveLandSpeedBoost_IsPositiveMultiplier()
    {
        Assert.GreaterOrEqual(data.waveLandSpeedBoost, 1f,
            "Wave-land should boost speed, not reduce it");
    }

    [Test]
    public void Clone_CreatesIndependentCopy()
    {
        var clone = data.Clone();

        // Modify clone
        clone.jumpForce = 999f;

        // Original unchanged
        Assert.AreNotEqual(data.jumpForce, clone.jumpForce,
            "Clone should be independent of original");

        Object.DestroyImmediate(clone);
    }
}
