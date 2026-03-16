using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for DraftManager.GetTotalLevelPool() and GrantLevel().
/// </summary>
[TestFixture]
public class DraftLevelPoolTests
{
    private DraftManager CreateDraftManager()
    {
        var go = new GameObject("TestDraftManager");
        var dm = go.AddComponent<DraftManager>();
        return dm;
    }

    [Test]
    public void GetTotalLevelPool_EmptyMatch_ReturnsZero()
    {
        var dm = CreateDraftManager();
        dm.InitializeMatch(new List<int> { 0, 1 });

        Assert.AreEqual(0, dm.GetTotalLevelPool());

        Object.DestroyImmediate(dm.gameObject);
    }

    [Test]
    public void GetTotalLevelPool_AfterDrafts_SumsLevels()
    {
        var dm = CreateDraftManager();
        dm.InitializeMatch(new List<int> { 0, 1, 2 });

        // Simulate: player 0 wins round (gains level via StartDraft)
        dm.StartDraft(0, new List<int> { 2, 1 });

        // Player 0 should have level 1
        Assert.AreEqual(1, dm.GetPlayerLevel(0));
        Assert.AreEqual(1, dm.GetTotalLevelPool());

        Object.DestroyImmediate(dm.gameObject);
    }

    [Test]
    public void GrantLevel_IncreasesPlayerLevel()
    {
        var dm = CreateDraftManager();
        dm.InitializeMatch(new List<int> { 0, 1 });

        dm.GrantLevel(0);
        Assert.AreEqual(1, dm.GetPlayerLevel(0));
        Assert.AreEqual(0, dm.GetPlayerLevel(1));

        dm.GrantLevel(0);
        Assert.AreEqual(2, dm.GetPlayerLevel(0));

        Object.DestroyImmediate(dm.gameObject);
    }

    [Test]
    public void GrantLevel_UnknownPlayer_DoesNothing()
    {
        var dm = CreateDraftManager();
        dm.InitializeMatch(new List<int> { 0, 1 });

        // Player 99 doesn't exist — should not crash
        dm.GrantLevel(99);
        Assert.AreEqual(0, dm.GetTotalLevelPool());

        Object.DestroyImmediate(dm.gameObject);
    }

    [Test]
    public void GetTotalLevelPool_MultipleGrantsAndWins()
    {
        var dm = CreateDraftManager();
        dm.InitializeMatch(new List<int> { 0, 1, 2 });

        // Player 0 wins a round (level +1 via StartDraft)
        dm.StartDraft(0, new List<int> { 2, 1 });

        // Player 1 kills a monster (level +1 via GrantLevel)
        dm.GrantLevel(1);

        // Player 2 kills a monster
        dm.GrantLevel(2);

        // Total: 1 + 1 + 1 = 3
        Assert.AreEqual(3, dm.GetTotalLevelPool());

        Object.DestroyImmediate(dm.gameObject);
    }
}
