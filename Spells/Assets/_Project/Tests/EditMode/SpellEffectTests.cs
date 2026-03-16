using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for SpellEffect system — registry, application, stacking.
/// Tests data structures and ScriptableObject logic (no runtime needed).
/// </summary>
[TestFixture]
public class SpellEffectTests
{
    private PowerCardData vampiricCard;
    private PowerCardData glassCannonCard;
    private PowerCardData normalCard;

    [SetUp]
    public void SetUp()
    {
        vampiricCard = ScriptableObject.CreateInstance<PowerCardData>();
        vampiricCard.cardName = "Vampiric";
        vampiricCard.hasSpecialBehavior = true;
        vampiricCard.specialBehaviorID = "vampiric";
        vampiricCard.tier = 2;
        vampiricCard.classTags = new string[] { "General" };
        vampiricCard.stackCap = 3;
        vampiricCard.positiveEffects = new StatModifier[0];
        vampiricCard.negativeEffects = new StatModifier[0];

        glassCannonCard = ScriptableObject.CreateInstance<PowerCardData>();
        glassCannonCard.cardName = "Glass Cannon";
        glassCannonCard.hasSpecialBehavior = true;
        glassCannonCard.specialBehaviorID = "glass_cannon";
        glassCannonCard.tier = 3;
        glassCannonCard.classTags = new string[] { "General" };
        glassCannonCard.stackCap = 0; // unlimited
        glassCannonCard.positiveEffects = new StatModifier[0];
        glassCannonCard.negativeEffects = new StatModifier[0];

        normalCard = ScriptableObject.CreateInstance<PowerCardData>();
        normalCard.cardName = "Speed Boost";
        normalCard.hasSpecialBehavior = false;
        normalCard.specialBehaviorID = "";
        normalCard.tier = 1;
        normalCard.classTags = new string[] { "General" };
        normalCard.positiveEffects = new StatModifier[0];
        normalCard.negativeEffects = new StatModifier[0];
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(vampiricCard);
        Object.DestroyImmediate(glassCannonCard);
        Object.DestroyImmediate(normalCard);
    }

    [Test]
    public void VampiricCard_HasSpecialBehavior()
    {
        Assert.IsTrue(vampiricCard.hasSpecialBehavior);
        Assert.AreEqual("vampiric", vampiricCard.specialBehaviorID);
    }

    [Test]
    public void GlassCannonCard_HasSpecialBehavior()
    {
        Assert.IsTrue(glassCannonCard.hasSpecialBehavior);
        Assert.AreEqual("glass_cannon", glassCannonCard.specialBehaviorID);
    }

    [Test]
    public void NormalCard_HasNoSpecialBehavior()
    {
        Assert.IsFalse(normalCard.hasSpecialBehavior);
        Assert.AreEqual("", normalCard.specialBehaviorID);
    }

    [Test]
    public void VampiricCard_Tier2_NotAvailableAtLevel0()
    {
        string[] tags = new string[] { "General" };
        Assert.IsFalse(vampiricCard.IsAvailableFor(tags, 0));
    }

    [Test]
    public void VampiricCard_Tier2_AvailableAtLevel1()
    {
        string[] tags = new string[] { "General" };
        Assert.IsTrue(vampiricCard.IsAvailableFor(tags, 1));
    }

    [Test]
    public void GlassCannonCard_Tier3_NotAvailableAtLevel1()
    {
        string[] tags = new string[] { "General" };
        Assert.IsFalse(glassCannonCard.IsAvailableFor(tags, 1));
    }

    [Test]
    public void GlassCannonCard_Tier3_AvailableAtLevel2()
    {
        string[] tags = new string[] { "General" };
        Assert.IsTrue(glassCannonCard.IsAvailableFor(tags, 2));
    }

    [Test]
    public void VampiricCard_CanStackUpTo3()
    {
        Assert.IsTrue(vampiricCard.CanStack(0));
        Assert.IsTrue(vampiricCard.CanStack(1));
        Assert.IsTrue(vampiricCard.CanStack(2));
        Assert.IsFalse(vampiricCard.CanStack(3));
    }

    [Test]
    public void GlassCannonCard_UnlimitedStacking()
    {
        Assert.IsTrue(glassCannonCard.CanStack(0));
        Assert.IsTrue(glassCannonCard.CanStack(10));
        Assert.IsTrue(glassCannonCard.CanStack(100));
    }

    [Test]
    public void SpecialCard_CanHaveBothModifiersAndBehavior()
    {
        // A card can have stat modifiers AND a special behavior
        var hybridCard = ScriptableObject.CreateInstance<PowerCardData>();
        hybridCard.hasSpecialBehavior = true;
        hybridCard.specialBehaviorID = "vampiric";
        hybridCard.positiveEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.MaxHP, modType = StatModifier.ModType.Additive, value = 1 }
        };
        hybridCard.negativeEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.MoveSpeed, modType = StatModifier.ModType.Multiplicative, value = 0.9f }
        };

        Assert.IsTrue(hybridCard.hasSpecialBehavior);
        Assert.AreEqual(1, hybridCard.positiveEffects.Length);
        Assert.AreEqual(1, hybridCard.negativeEffects.Length);

        Object.DestroyImmediate(hybridCard);
    }
}
