using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class PowerCardDataTests
{
    private PowerCardData card;

    [SetUp]
    public void SetUp()
    {
        card = ScriptableObject.CreateInstance<PowerCardData>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(card);
    }

    [Test]
    public void Tier1Card_AvailableAtLevel0()
    {
        card.tier = 1;
        card.classTags = new string[] { "General" };

        Assert.IsTrue(card.IsAvailableFor(new string[] { "Wizard" }, 0),
            "Tier 1 General card should be available at level 0");
    }

    [Test]
    public void Tier2Card_NotAvailableAtLevel0()
    {
        card.tier = 2;
        card.classTags = new string[] { "General" };

        Assert.IsFalse(card.IsAvailableFor(new string[] { "Wizard" }, 0),
            "Tier 2 card should NOT be available at level 0");
    }

    [Test]
    public void Tier2Card_AvailableAtLevel1()
    {
        card.tier = 2;
        card.classTags = new string[] { "General" };

        Assert.IsTrue(card.IsAvailableFor(new string[] { "Wizard" }, 1),
            "Tier 2 card should be available at level 1");
    }

    [Test]
    public void Tier3Card_AvailableAtLevel2()
    {
        card.tier = 3;
        card.classTags = new string[] { "General" };

        Assert.IsTrue(card.IsAvailableFor(new string[] { "Wizard" }, 2),
            "Tier 3 card should be available at level 2");
    }

    [Test]
    public void ClassSpecificCard_AvailableForMatchingClass()
    {
        card.tier = 1;
        card.classTags = new string[] { "Wizard" };

        Assert.IsTrue(card.IsAvailableFor(new string[] { "General", "Wizard" }, 0),
            "Wizard card should be available to Wizard class");
    }

    [Test]
    public void ClassSpecificCard_NotAvailableForDifferentClass()
    {
        card.tier = 1;
        card.classTags = new string[] { "Wizard" };

        Assert.IsFalse(card.IsAvailableFor(new string[] { "General", "Warrior" }, 0),
            "Wizard card should NOT be available to Warrior class");
    }

    [Test]
    public void GeneralCard_AvailableToAllClasses()
    {
        card.tier = 1;
        card.classTags = new string[] { "General" };

        Assert.IsTrue(card.IsAvailableFor(new string[] { "Warrior" }, 0),
            "General card should be available to any class");
        Assert.IsTrue(card.IsAvailableFor(new string[] { "Jester" }, 0),
            "General card should be available to any class");
    }

    [Test]
    public void CanStack_UnlimitedByDefault()
    {
        card.stackCap = 0;

        Assert.IsTrue(card.CanStack(0), "Unlimited stack cap allows first");
        Assert.IsTrue(card.CanStack(5), "Unlimited stack cap allows stacking");
        Assert.IsTrue(card.CanStack(100), "Unlimited stack cap allows high stacks");
    }

    [Test]
    public void CanStack_RespectsStackCap()
    {
        card.stackCap = 3;

        Assert.IsTrue(card.CanStack(0), "Under cap: allowed");
        Assert.IsTrue(card.CanStack(2), "At cap-1: allowed");
        Assert.IsFalse(card.CanStack(3), "At cap: blocked");
        Assert.IsFalse(card.CanStack(5), "Over cap: blocked");
    }
}
