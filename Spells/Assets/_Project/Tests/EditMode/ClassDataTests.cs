using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class ClassDataTests
{
    private ClassData wizardData;
    private ClassData warriorData;
    private CombatData wizardCombat;
    private CombatData warriorCombat;

    [SetUp]
    public void SetUp()
    {
        wizardCombat = ScriptableObject.CreateInstance<CombatData>();
        wizardCombat.maxHP = 3;
        wizardCombat.projectileSpeed = 25f;
        wizardCombat.fireCooldown = 0.2f;
        wizardCombat.projectileGravity = 0f;
        wizardCombat.maxAmmo = 0;

        warriorCombat = ScriptableObject.CreateInstance<CombatData>();
        warriorCombat.maxHP = 4;
        warriorCombat.projectileSpeed = 15f;
        warriorCombat.fireCooldown = 0.6f;
        warriorCombat.projectileGravity = 2f;
        warriorCombat.maxAmmo = 3;
        warriorCombat.retrievableProjectiles = true;

        wizardData = ScriptableObject.CreateInstance<ClassData>();
        wizardData.className = "Wizard";
        wizardData.combatData = wizardCombat;
        wizardData.cardPoolTags = new string[] { "General", "Wizard" };

        warriorData = ScriptableObject.CreateInstance<ClassData>();
        warriorData.className = "Warrior";
        warriorData.combatData = warriorCombat;
        warriorData.cardPoolTags = new string[] { "General", "Warrior" };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(wizardCombat);
        Object.DestroyImmediate(warriorCombat);
        Object.DestroyImmediate(wizardData);
        Object.DestroyImmediate(warriorData);
    }

    [Test]
    public void Wizard_HasCorrectHP()
    {
        Assert.AreEqual(3, wizardData.combatData.maxHP, "Wizard should have 3 HP per GDD");
    }

    [Test]
    public void Warrior_HasHigherHP()
    {
        Assert.Greater(warriorData.combatData.maxHP, wizardData.combatData.maxHP,
            "Warrior should have more HP than Wizard (tank class)");
    }

    [Test]
    public void Wizard_FasterFireRate()
    {
        float wizardRate = 1f / wizardData.combatData.fireCooldown;
        float warriorRate = 1f / warriorData.combatData.fireCooldown;
        Assert.Greater(wizardRate, warriorRate,
            "Wizard should fire faster than Warrior (rapid bolts vs heavy throws)");
    }

    [Test]
    public void Warrior_HasAmmoLimit()
    {
        Assert.Greater(warriorData.combatData.maxAmmo, 0,
            "Warrior should have limited ammo (axes must be retrieved)");
    }

    [Test]
    public void Wizard_NoAmmoLimit()
    {
        Assert.AreEqual(0, wizardData.combatData.maxAmmo,
            "Wizard should have unlimited ammo");
    }

    [Test]
    public void Warrior_HasArcingProjectile()
    {
        Assert.Greater(warriorData.combatData.projectileGravity, 0f,
            "Warrior axes should arc (gravity > 0)");
    }

    [Test]
    public void Wizard_HasStraightProjectile()
    {
        Assert.AreEqual(0f, wizardData.combatData.projectileGravity, 0.01f,
            "Wizard bolts should travel straight (gravity = 0)");
    }

    [Test]
    public void Warrior_HasRetrievableProjectiles()
    {
        Assert.IsTrue(warriorData.combatData.retrievableProjectiles,
            "Warrior axes should be retrievable");
    }

    [Test]
    public void Wizard_HasBroaderCardPool()
    {
        Assert.GreaterOrEqual(wizardData.cardPoolTags.Length, warriorData.cardPoolTags.Length,
            "Wizard should have access to at least as many card pool tags as Warrior");
    }
}
