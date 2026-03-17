using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode tests for slope physics — ground detection and jumping on slopes.
/// </summary>
[TestFixture]
public class SlopeTests
{
    private PlayModeTestHelper.TestPlayer player;
    private GameObject slope;
    private GameObject ground;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Flat ground on the left, shallow slope rising to the right.
        // 20° is gentle enough that the player walks onto it naturally.
        // Ground extends from x=-10 to x=0, slope starts at x=0 going right.
        ground = PlayModeTestHelper.CreateGround(new Vector3(-5f, -1f, 0), 10f);
        slope = PlayModeTestHelper.CreateSlope(new Vector3(3f, 0.5f, 0), 20f, 10f); // 20° slope, longer

        player = PlayModeTestHelper.SpawnTestPlayer(new Vector3(-2f, 1f, 0));

        yield return new WaitForSeconds(0.5f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        PlayModeTestHelper.Cleanup(player.gameObject, slope, ground);
        yield return null;
    }

    [UnityTest]
    public IEnumerator CanJumpOnSlope()
    {
        // Walk onto slope — give enough time to reach slope surface
        player.input.SetMove(1f);
        yield return new WaitForSeconds(0.8f);

        // Stop and let physics settle on the slope
        player.input.SetMove(0f);
        yield return new WaitForSeconds(0.3f);

        // Check if grounded on slope
        bool groundedOnSlope = player.physics.IsGrounded;

        if (groundedOnSlope)
        {
            // Jump while on slope
            player.input.PressJump();
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            // Should have upward velocity
            Assert.Greater(player.rb.linearVelocity.y, 5f,
                $"Jump on slope should produce upward velocity (vy={player.rb.linearVelocity.y:F2})");
        }
        else
        {
            Assert.Fail($"Player should be grounded on 20° slope. " +
                $"State: {player.stateMachine.GetStateName()}, " +
                $"pos: {player.gameObject.transform.position}");
        }
    }

    [UnityTest]
    public IEnumerator GroundedStable_OnSlope()
    {
        // Walk onto slope — longer walk to reach slope surface
        player.input.SetMove(1f);
        yield return new WaitForSeconds(0.8f);

        // Stop on slope and let physics settle
        player.input.SetMove(0f);
        yield return new WaitForSeconds(0.5f);

        // Check grounded stability over 30 frames (no flickering)
        int groundedCount = 0;
        int totalFrames = 30;
        for (int i = 0; i < totalFrames; i++)
        {
            yield return new WaitForFixedUpdate();
            if (player.physics.IsGrounded) groundedCount++;
        }

        float stability = (float)groundedCount / totalFrames;
        Assert.Greater(stability, 0.8f,
            $"Grounded should be stable on slope ({stability:P0} of frames grounded, " +
            $"pos: {player.gameObject.transform.position})");
    }
}
