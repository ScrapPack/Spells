using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for CombatAnalytics.PlayerStats data.
/// Tests stat tracking, KD ratio, parry rate, and reset logic.
/// </summary>
[TestFixture]
public class CombatAnalyticsTests
{
    private CombatAnalytics.PlayerStats stats;

    [SetUp]
    public void SetUp()
    {
        stats = new CombatAnalytics.PlayerStats();
    }

    [Test]
    public void NewStats_AllZero()
    {
        Assert.AreEqual(0, stats.kills);
        Assert.AreEqual(0, stats.deaths);
        Assert.AreEqual(0, stats.parriesSucceeded);
        Assert.AreEqual(0, stats.parriesFailed);
        Assert.AreEqual(0, stats.projectilesFired);
        Assert.AreEqual(0f, stats.damageDealt);
        Assert.AreEqual(0f, stats.damageReceived);
        Assert.AreEqual(0, stats.cardsPicked);
        Assert.AreEqual(0, stats.roundsWon);
    }

    [Test]
    public void KDRatio_NoDeaths_ReturnsKills()
    {
        stats.kills = 5;
        stats.deaths = 0;
        Assert.AreEqual(5f, stats.KDRatio);
    }

    [Test]
    public void KDRatio_WithDeaths_ReturnsRatio()
    {
        stats.kills = 10;
        stats.deaths = 5;
        Assert.AreEqual(2f, stats.KDRatio, 0.01f);
    }

    [Test]
    public void KDRatio_NegativeKD()
    {
        stats.kills = 2;
        stats.deaths = 4;
        Assert.AreEqual(0.5f, stats.KDRatio, 0.01f);
    }

    [Test]
    public void ParryRate_NoParries_ReturnsZero()
    {
        Assert.AreEqual(0f, stats.ParryRate);
    }

    [Test]
    public void ParryRate_AllSuccesses_ReturnsOne()
    {
        stats.parriesSucceeded = 10;
        stats.parriesFailed = 0;
        Assert.AreEqual(1f, stats.ParryRate, 0.01f);
    }

    [Test]
    public void ParryRate_Mixed_ReturnsCorrectRate()
    {
        stats.parriesSucceeded = 3;
        stats.parriesFailed = 7;
        Assert.AreEqual(0.3f, stats.ParryRate, 0.01f);
    }

    [Test]
    public void Reset_ClearsAllStats()
    {
        stats.kills = 5;
        stats.deaths = 3;
        stats.damageDealt = 100f;
        stats.parriesSucceeded = 10;
        stats.cardsPicked = 4;
        stats.roundsWon = 2;

        stats.Reset();

        Assert.AreEqual(0, stats.kills);
        Assert.AreEqual(0, stats.deaths);
        Assert.AreEqual(0f, stats.damageDealt);
        Assert.AreEqual(0, stats.parriesSucceeded);
        Assert.AreEqual(0, stats.cardsPicked);
        Assert.AreEqual(0, stats.roundsWon);
    }
}
