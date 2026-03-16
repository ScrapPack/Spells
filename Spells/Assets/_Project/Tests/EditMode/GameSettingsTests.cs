using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class GameSettingsTests
{
    private GameSettings settings;

    [SetUp]
    public void SetUp()
    {
        settings = ScriptableObject.CreateInstance<GameSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(settings);
    }

    [Test]
    public void DefaultRoundsToWin_IsFive()
    {
        Assert.AreEqual(5, settings.roundsToWin,
            "Default match should be first to 5 per GDD");
    }

    [Test]
    public void DefaultMaxPlayers_IsFour()
    {
        Assert.AreEqual(4, settings.maxPlayers,
            "Spells is a 4-player game");
    }

    [Test]
    public void ZoomDelay_LessThanMaxRoundTime()
    {
        Assert.Less(settings.zoomDelay, settings.maxRoundTime,
            "Zoom should start before round ends");
    }

    [Test]
    public void CardOptions_IsReasonable()
    {
        Assert.GreaterOrEqual(settings.cardOptionsPerPick, 2,
            "Need at least 2 options for meaningful choice");
        Assert.LessOrEqual(settings.cardOptionsPerPick, 6,
            "Too many options causes decision paralysis");
    }

    [Test]
    public void DraftTimeLimit_MatchesGDD()
    {
        // GDD says ~15 seconds per pick
        Assert.AreEqual(15f, settings.draftTimeLimit, 1f,
            "Draft time should be around 15 seconds per GDD");
    }

    [Test]
    public void DuplicateClasses_AllowedByDefault()
    {
        Assert.IsTrue(settings.allowDuplicateClasses,
            "GDD: duplicates allowed — 4 Jesters should be possible");
    }

    [Test]
    public void GeneralPoolRatio_MatchesGDD()
    {
        // GDD: ~40% general, ~60% class-specific
        Assert.AreEqual(0.4f, settings.generalPoolRatio, 0.05f,
            "General pool should be ~40% per GDD");
    }
}
