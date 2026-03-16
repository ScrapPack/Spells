using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for draft and card pool filtering logic.
/// Tests PowerCardData.IsAvailableFor with various class tag combinations
/// and tier/level requirements. Does not test DraftManager directly
/// (that needs MonoBehaviour runtime) — tests the data logic it relies on.
/// </summary>
[TestFixture]
public class DraftLogicTests
{
    private PowerCardData generalT1;
    private PowerCardData generalT2;
    private PowerCardData wizardOnly;
    private PowerCardData warriorWizard;

    [SetUp]
    public void SetUp()
    {
        generalT1 = ScriptableObject.CreateInstance<PowerCardData>();
        generalT1.cardName = "General Tier 1";
        generalT1.tier = 1;
        generalT1.classTags = new string[] { "General" };
        generalT1.positiveEffects = new StatModifier[0];
        generalT1.negativeEffects = new StatModifier[0];

        generalT2 = ScriptableObject.CreateInstance<PowerCardData>();
        generalT2.cardName = "General Tier 2";
        generalT2.tier = 2;
        generalT2.classTags = new string[] { "General" };
        generalT2.positiveEffects = new StatModifier[0];
        generalT2.negativeEffects = new StatModifier[0];

        wizardOnly = ScriptableObject.CreateInstance<PowerCardData>();
        wizardOnly.cardName = "Wizard Only";
        wizardOnly.tier = 1;
        wizardOnly.classTags = new string[] { "Wizard" };
        wizardOnly.positiveEffects = new StatModifier[0];
        wizardOnly.negativeEffects = new StatModifier[0];

        warriorWizard = ScriptableObject.CreateInstance<PowerCardData>();
        warriorWizard.cardName = "Warrior+Wizard";
        warriorWizard.tier = 1;
        warriorWizard.classTags = new string[] { "Warrior", "Wizard" };
        warriorWizard.positiveEffects = new StatModifier[0];
        warriorWizard.negativeEffects = new StatModifier[0];
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(generalT1);
        Object.DestroyImmediate(generalT2);
        Object.DestroyImmediate(wizardOnly);
        Object.DestroyImmediate(warriorWizard);
    }

    [Test]
    public void GeneralCard_AvailableToAnyClass()
    {
        Assert.IsTrue(generalT1.IsAvailableFor(new string[] { "Wizard" }, 0));
        Assert.IsTrue(generalT1.IsAvailableFor(new string[] { "Warrior" }, 0));
        Assert.IsTrue(generalT1.IsAvailableFor(new string[] { "Rogue" }, 0));
    }

    [Test]
    public void WizardOnlyCard_NotAvailableToWarrior()
    {
        Assert.IsFalse(wizardOnly.IsAvailableFor(new string[] { "Warrior" }, 0));
    }

    [Test]
    public void WizardOnlyCard_AvailableToWizard()
    {
        Assert.IsTrue(wizardOnly.IsAvailableFor(new string[] { "Wizard" }, 0));
    }

    [Test]
    public void MultiClassCard_AvailableToBothClasses()
    {
        Assert.IsTrue(warriorWizard.IsAvailableFor(new string[] { "Warrior" }, 0));
        Assert.IsTrue(warriorWizard.IsAvailableFor(new string[] { "Wizard" }, 0));
    }

    [Test]
    public void MultiClassCard_NotAvailableToUnlistedClass()
    {
        Assert.IsFalse(warriorWizard.IsAvailableFor(new string[] { "Rogue" }, 0));
    }

    [Test]
    public void Tier2Card_RequiresLevel1()
    {
        Assert.IsFalse(generalT2.IsAvailableFor(new string[] { "General" }, 0));
        Assert.IsTrue(generalT2.IsAvailableFor(new string[] { "General" }, 1));
        Assert.IsTrue(generalT2.IsAvailableFor(new string[] { "General" }, 2));
    }

    [Test]
    public void Tier1Card_AlwaysAvailable()
    {
        Assert.IsTrue(generalT1.IsAvailableFor(new string[] { "General" }, 0));
    }

    [Test]
    public void PlayerWithMultipleTags_MatchesClassCard()
    {
        // A player with multiple pool tags should match if any tag overlaps
        Assert.IsTrue(wizardOnly.IsAvailableFor(new string[] { "Wizard", "Arcane" }, 0));
    }

    [Test]
    public void PlayerWithMultipleTags_NoMatch_ReturnsFalse()
    {
        Assert.IsFalse(wizardOnly.IsAvailableFor(new string[] { "Warrior", "Heavy" }, 0));
    }

    [Test]
    public void StackCap_Zero_MeansUnlimited()
    {
        generalT1.stackCap = 0;
        Assert.IsTrue(generalT1.CanStack(0));
        Assert.IsTrue(generalT1.CanStack(50));
        Assert.IsTrue(generalT1.CanStack(999));
    }

    [Test]
    public void StackCap_Enforced()
    {
        generalT1.stackCap = 2;
        Assert.IsTrue(generalT1.CanStack(0));
        Assert.IsTrue(generalT1.CanStack(1));
        Assert.IsFalse(generalT1.CanStack(2));
    }
}
