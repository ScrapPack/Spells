using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class StatModifierTests
{
    private CombatData combatData;
    private MovementData movementData;

    [SetUp]
    public void SetUp()
    {
        combatData = ScriptableObject.CreateInstance<CombatData>();
        movementData = ScriptableObject.CreateInstance<MovementData>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(combatData);
        Object.DestroyImmediate(movementData);
    }

    [Test]
    public void Additive_MaxHP_Increases()
    {
        // Stone Skin: +1 max HP
        var mod = new StatModifier
        {
            target = StatModifier.Target.MaxHP,
            modType = StatModifier.ModType.Additive,
            value = 1f
        };

        int before = combatData.maxHP;
        mod.Apply(combatData);
        Assert.AreEqual(before + 1, combatData.maxHP, "Additive +1 HP should increase maxHP by 1");
    }

    [Test]
    public void Multiplicative_MoveSpeed_Reduces()
    {
        // Stone Skin: -20% movement speed (multiply by 0.8)
        var mod = new StatModifier
        {
            target = StatModifier.Target.MoveSpeed,
            modType = StatModifier.ModType.Multiplicative,
            value = 0.8f
        };

        float before = movementData.moveSpeed;
        mod.Apply(movementData);
        Assert.AreEqual(before * 0.8f, movementData.moveSpeed, 0.01f,
            "Multiplicative 0.8 should reduce speed by 20%");
    }

    [Test]
    public void Additive_MoveSpeed_Increases()
    {
        // Haste: +30% movement speed (multiply by 1.3)
        var mod = new StatModifier
        {
            target = StatModifier.Target.MoveSpeed,
            modType = StatModifier.ModType.Multiplicative,
            value = 1.3f
        };

        float before = movementData.moveSpeed;
        mod.Apply(movementData);
        Assert.AreEqual(before * 1.3f, movementData.moveSpeed, 0.01f,
            "Multiplicative 1.3 should increase speed by 30%");
    }

    [Test]
    public void Multiplicative_ProjectileDamage_Halves()
    {
        // Haste negative: projectiles deal 0.5x damage
        var mod = new StatModifier
        {
            target = StatModifier.Target.ProjectileDamage,
            modType = StatModifier.ModType.Multiplicative,
            value = 0.5f
        };

        float before = combatData.projectileDamage;
        mod.Apply(combatData);
        Assert.AreEqual(before * 0.5f, combatData.projectileDamage, 0.01f,
            "0.5x damage modifier should halve projectile damage");
    }

    [Test]
    public void Multiplicative_ProjectileDamage_Doubles()
    {
        // Blood Pact positive: 2x damage
        var mod = new StatModifier
        {
            target = StatModifier.Target.ProjectileDamage,
            modType = StatModifier.ModType.Multiplicative,
            value = 2f
        };

        float before = combatData.projectileDamage;
        mod.Apply(combatData);
        Assert.AreEqual(before * 2f, combatData.projectileDamage, 0.01f,
            "2x damage modifier should double projectile damage");
    }

    [Test]
    public void Stacking_BloodPact_CompoundsExponentially()
    {
        // Blood Pact x3: 2x → 4x → 8x damage
        var mod = new StatModifier
        {
            target = StatModifier.Target.ProjectileDamage,
            modType = StatModifier.ModType.Multiplicative,
            value = 2f
        };

        float original = combatData.projectileDamage;
        mod.Apply(combatData); // x2
        mod.Apply(combatData); // x4
        mod.Apply(combatData); // x8

        Assert.AreEqual(original * 8f, combatData.projectileDamage, 0.01f,
            "3x Blood Pact stacking should give 8x damage (2^3)");
    }

    [Test]
    public void AffectsHealth_TrueForMaxHP()
    {
        var mod = new StatModifier { target = StatModifier.Target.MaxHP, value = 1f };
        Assert.IsTrue(mod.AffectsHealth, "MaxHP modifier should report AffectsHealth");
    }

    [Test]
    public void AffectsMovement_TrueForMoveSpeed()
    {
        var mod = new StatModifier { target = StatModifier.Target.MoveSpeed, value = 1f };
        Assert.IsTrue(mod.AffectsMovement, "MoveSpeed modifier should report AffectsMovement");
    }

    [Test]
    public void AffectsMovement_FalseForDamage()
    {
        var mod = new StatModifier { target = StatModifier.Target.ProjectileDamage, value = 1f };
        Assert.IsFalse(mod.AffectsMovement, "Damage modifier should not report AffectsMovement");
    }

    [Test]
    public void Additive_MaxAirJumps_Adds()
    {
        // Second Wind card: adds 1 air jump
        var mod = new StatModifier
        {
            target = StatModifier.Target.MaxAirJumps,
            modType = StatModifier.ModType.Additive,
            value = 1f
        };

        int before = movementData.maxAirJumps;
        mod.Apply(movementData);
        Assert.AreEqual(before + 1, movementData.maxAirJumps,
            "Adding 1 air jump should increment maxAirJumps");
    }

    [Test]
    public void ParryWindow_Multiplicative_Widens()
    {
        // Mirror World: 3x parry window
        var mod = new StatModifier
        {
            target = StatModifier.Target.ParryWindow,
            modType = StatModifier.ModType.Multiplicative,
            value = 3f
        };

        float before = combatData.parryWindow;
        mod.Apply(combatData);
        Assert.AreEqual(before * 3f, combatData.parryWindow, 0.001f,
            "3x parry window modifier should triple the window");
    }
}
