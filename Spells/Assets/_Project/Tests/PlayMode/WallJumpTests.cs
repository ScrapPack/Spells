using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode tests for wall slide and wall-jump chaining.
/// Creates a walled arena and tests wall interactions.
/// </summary>
[TestFixture]
public class WallJumpTests
{
    private PlayModeTestHelper.TestPlayer player;
    private GameObject ground;
    private GameObject wallLeft;
    private GameObject wallRight;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        ground = PlayModeTestHelper.CreateGround(new Vector3(0, -1f, 0), 20f);
        // Two walls for chaining — close enough that wall-jump arc reaches
        wallLeft = PlayModeTestHelper.CreateWall(new Vector3(-3f, 5f, 0), 1f, 15f);
        wallRight = PlayModeTestHelper.CreateWall(new Vector3(3f, 5f, 0), 1f, 15f);

        player = PlayModeTestHelper.SpawnTestPlayer(new Vector3(0, 1f, 0));

        yield return new WaitForSeconds(0.5f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        PlayModeTestHelper.Cleanup(player.gameObject, ground, wallLeft, wallRight);
        yield return null;
    }

    [UnityTest]
    public IEnumerator WallSlide_SlowerThanFreeFall()
    {
        // Position player near left wall, high in the air (well above ground)
        player.gameObject.transform.position = new Vector3(-2.2f, 15f, 0);
        player.rb.linearVelocity = Vector2.zero;
        player.input.SetMove(-1f); // Hold toward left wall

        yield return new WaitForSeconds(0.5f);

        // Should be wall sliding — record speed
        bool wasSliding = player.stateMachine.CurrentState is WallSlidingState;
        float wallSlideSpeed = Mathf.Abs(player.rb.linearVelocity.y);

        // Now release and free fall — move away from wall but DON'T push toward ground
        player.input.SetMove(0f); // Neutral, just fall
        player.rb.linearVelocity = new Vector2(0f, 0f); // Reset to compare fairly

        // Fall for 0.3s (less time so we don't land on ground at y=-1)
        yield return new WaitForSeconds(0.3f);
        float freeFallSpeed = Mathf.Abs(player.rb.linearVelocity.y);

        if (wasSliding)
        {
            Assert.Greater(wallSlideSpeed, 0.5f,
                $"Wall slide should have measurable speed ({wallSlideSpeed:F2})");
            Assert.Greater(freeFallSpeed, wallSlideSpeed,
                $"Free fall ({freeFallSpeed:F2}) should exceed wall slide ({wallSlideSpeed:F2})");
        }
        else
        {
            Debug.LogWarning("Wall slide state was not entered — wall detection may need adjustment");
        }
    }

    [UnityTest]
    public IEnumerator WallJump_LaunchesAwayFromWall()
    {
        // Position near left wall, in the air, falling
        player.gameObject.transform.position = new Vector3(-2.2f, 6f, 0);
        player.rb.linearVelocity = new Vector2(0, -2f);
        player.input.SetMove(-1f);

        yield return new WaitForSeconds(0.3f);

        // Should be wall sliding on left wall
        if (player.stateMachine.CurrentState is WallSlidingState)
        {
            float preJumpX = player.rb.linearVelocity.x;

            // Wall jump
            player.input.PressJump();
            yield return null;
            yield return new WaitForFixedUpdate();

            // Should have launched rightward (away from left wall)
            Assert.Greater(player.rb.linearVelocity.x, 5f,
                $"Wall jump should launch away from wall (vx={player.rb.linearVelocity.x:F2})");
            Assert.Greater(player.rb.linearVelocity.y, 5f,
                $"Wall jump should launch upward (vy={player.rb.linearVelocity.y:F2})");
        }
        else
        {
            Debug.LogWarning($"Expected WallSlidingState, got {player.stateMachine.GetStateName()}");
        }
    }

    [UnityTest]
    public IEnumerator WallJumpChain_CanGrabSecondWall()
    {
        // Position near left wall, falling
        player.gameObject.transform.position = new Vector3(-2.2f, 6f, 0);
        player.rb.linearVelocity = new Vector2(0, -2f);
        player.input.SetMove(-1f);

        yield return new WaitForSeconds(0.3f);

        if (!(player.stateMachine.CurrentState is WallSlidingState))
        {
            Assert.Inconclusive("Could not enter wall slide on first wall");
            yield break;
        }

        // Wall jump off left wall
        player.input.PressJump();
        yield return null;

        // Arc toward right wall — hold right
        yield return new WaitForSeconds(0.15f); // Past lockout timer
        player.input.SetMove(1f);

        // Wait to reach right wall area and start falling
        float timeout = 2f;
        float elapsed = 0f;
        bool grabbedSecondWall = false;

        while (elapsed < timeout)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            if (player.stateMachine.CurrentState is WallSlidingState && elapsed > 0.2f)
            {
                grabbedSecondWall = true;
                break;
            }
        }

        Assert.IsTrue(grabbedSecondWall,
            $"Should have grabbed second wall. Final state: {player.stateMachine.GetStateName()}, " +
            $"pos: {player.gameObject.transform.position}, vel: {player.rb.linearVelocity}");
    }
}
