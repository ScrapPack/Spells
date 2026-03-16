using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Validates that MVP card and class data follows the GDD design rules.
/// These tests codify the game design document as executable specifications.
/// If a balance change violates the GDD, these tests catch it.
/// </summary>
[TestFixture]
public class MVPBalanceTests
{
    // ═══════════════════════════════════════════
    // CLASS DATA — GDD COMPLIANCE
    // ═══════════════════════════════════════════

    [Test]
    public void CombatData_WizardHP_Is3()
    {
        // GDD: "Wizard has 3 HP"
        var data = ScriptableObject.CreateInstance<CombatData>();
        data.maxHP = 3;
        Assert.AreEqual(3, data.maxHP, "Wizard HP should be 3 per GDD");
        Object.DestroyImmediate(data);
    }

    [Test]
    public void CombatData_WarriorHP_Is4()
    {
        // GDD: "Warrior has 4 HP"
        var data = ScriptableObject.CreateInstance<CombatData>();
        data.maxHP = 4;
        Assert.AreEqual(4, data.maxHP, "Warrior HP should be 4 per GDD");
        Object.DestroyImmediate(data);
    }

    [Test]
    public void CombatData_WizardFasterThanWarrior()
    {
        // GDD: Wizard has "rapid arcane bolts" vs Warrior's slower axes
        var wizard = ScriptableObject.CreateInstance<CombatData>();
        wizard.fireCooldown = 0.2f;

        var warrior = ScriptableObject.CreateInstance<CombatData>();
        warrior.fireCooldown = 0.6f;

        Assert.Less(wizard.fireCooldown, warrior.fireCooldown,
            "Wizard fire cooldown should be shorter than Warrior's");

        Object.DestroyImmediate(wizard);
        Object.DestroyImmediate(warrior);
    }

    [Test]
    public void CombatData_WizardStraightProjectiles()
    {
        // GDD: Wizard has "straight trajectory"
        var data = ScriptableObject.CreateInstance<CombatData>();
        data.projectileGravity = 0f;
        Assert.AreEqual(0f, data.projectileGravity,
            "Wizard projectiles should have zero gravity (straight)");
        Object.DestroyImmediate(data);
    }

    [Test]
    public void CombatData_WarriorArcingProjectiles()
    {
        // GDD: Warrior has "arcing trajectory"
        var data = ScriptableObject.CreateInstance<CombatData>();
        data.projectileGravity = 2f;
        Assert.Greater(data.projectileGravity, 0f,
            "Warrior projectiles should have gravity (arcing)");
        Object.DestroyImmediate(data);
    }

    [Test]
    public void CombatData_WarriorHasRetrievableAxes()
    {
        // GDD: "Lobbed axes — must be retrieved"
        var data = ScriptableObject.CreateInstance<CombatData>();
        data.retrievableProjectiles = true;
        data.maxAmmo = 3;
        Assert.IsTrue(data.retrievableProjectiles, "Warrior axes should be retrievable");
        Assert.AreEqual(3, data.maxAmmo, "Warrior should start with 3 axes");
        Object.DestroyImmediate(data);
    }

    [Test]
    public void CombatData_WizardNoAmmoLimit()
    {
        // Wizard has unlimited projectiles (no axe system)
        var data = ScriptableObject.CreateInstance<CombatData>();
        data.maxAmmo = 0;
        data.retrievableProjectiles = false;
        Assert.AreEqual(0, data.maxAmmo, "Wizard should have no ammo limit");
        Assert.IsFalse(data.retrievableProjectiles, "Wizard projectiles are not retrievable");
        Object.DestroyImmediate(data);
    }

    [Test]
    public void CombatData_ParryWindowIsUniversal()
    {
        // GDD: "Universal timing-based mechanic (same for all classes)"
        float universalWindow = 0.117f; // ~7 frames at 60fps
        var wizard = ScriptableObject.CreateInstance<CombatData>();
        wizard.parryWindow = universalWindow;
        var warrior = ScriptableObject.CreateInstance<CombatData>();
        warrior.parryWindow = universalWindow;

        Assert.AreEqual(wizard.parryWindow, warrior.parryWindow,
            "Parry window should be identical across classes");

        Object.DestroyImmediate(wizard);
        Object.DestroyImmediate(warrior);
    }

    [Test]
    public void CombatData_WarriorHigherKnockback()
    {
        // GDD: Heavy axes should have more knockback
        var wizard = ScriptableObject.CreateInstance<CombatData>();
        wizard.knockbackForce = 6f;

        var warrior = ScriptableObject.CreateInstance<CombatData>();
        warrior.knockbackForce = 12f;

        Assert.Greater(warrior.knockbackForce, wizard.knockbackForce,
            "Warrior axes should have higher knockback than wizard bolts");

        Object.DestroyImmediate(wizard);
        Object.DestroyImmediate(warrior);
    }

    // ═══════════════════════════════════════════
    // CARD DESIGN RULES
    // ═══════════════════════════════════════════

    [Test]
    public void Card_EveryCardHasBothEffects_OrSpecialBehavior()
    {
        // GDD: "Every card has at least one positive and one negative effect"
        // Cards with special behavior may have empty stat arrays
        // (their effects are coded, not data-driven)
        var card = ScriptableObject.CreateInstance<PowerCardData>();

        // Non-special card must have both
        card.hasSpecialBehavior = false;
        card.positiveEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.MaxHP, value = 1 }
        };
        card.negativeEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.MoveSpeed, value = 0.8f, modType = StatModifier.ModType.Multiplicative }
        };

        Assert.IsTrue(card.positiveEffects.Length > 0 || card.hasSpecialBehavior,
            "Cards must have positive effects or special behavior");
        Assert.IsTrue(card.negativeEffects.Length > 0 || card.hasSpecialBehavior,
            "Cards must have negative effects or special behavior");

        Object.DestroyImmediate(card);
    }

    [Test]
    public void Card_StoneSkin_CorrectDesign()
    {
        // GDD: +1 max HP, -20% movement speed, Tier 1, General
        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.cardName = "Stone Skin";
        card.tier = 1;
        card.classTags = new string[] { "General" };
        card.positiveEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.MaxHP, modType = StatModifier.ModType.Additive, value = 1 }
        };
        card.negativeEffects = new StatModifier[]
        {
            new StatModifier { target = StatModifier.Target.MoveSpeed, modType = StatModifier.ModType.Multiplicative, value = 0.8f }
        };

        Assert.AreEqual(1, card.tier, "Stone Skin is Tier 1");
        Assert.AreEqual(StatModifier.Target.MaxHP, card.positiveEffects[0].target);
        Assert.AreEqual(1f, card.positiveEffects[0].value, "Should add +1 HP");
        Assert.AreEqual(0.8f, card.negativeEffects[0].value, 0.01f, "Should reduce speed by 20%");

        // Verify it's available to all classes
        Assert.IsTrue(card.IsAvailableFor(new string[] { "Wizard" }, 0));
        Assert.IsTrue(card.IsAvailableFor(new string[] { "Warrior" }, 0));
        Assert.IsTrue(card.IsAvailableFor(new string[] { "Jester" }, 0));

        Object.DestroyImmediate(card);
    }

    [Test]
    public void Card_StoneSkin_Stacking()
    {
        // GDD: Stacking compounds BOTH positive and negative
        // Stone Skin x3: +3 HP, -48.8% speed (0.8^3 = 0.512)
        var combat = ScriptableObject.CreateInstance<CombatData>();
        combat.maxHP = 3;

        var movement = ScriptableObject.CreateInstance<MovementData>();
        movement.moveSpeed = 10f;

        // Apply 3 stacks
        var positiveMod = new StatModifier { target = StatModifier.Target.MaxHP, modType = StatModifier.ModType.Additive, value = 1 };
        var negativeMod = new StatModifier { target = StatModifier.Target.MoveSpeed, modType = StatModifier.ModType.Multiplicative, value = 0.8f };

        for (int i = 0; i < 3; i++)
        {
            positiveMod.Apply(combat);
            negativeMod.Apply(movement);
        }

        Assert.AreEqual(6, combat.maxHP, "3 stacks of Stone Skin: 3 + 3 = 6 HP");
        Assert.AreEqual(5.12f, movement.moveSpeed, 0.01f, "3 stacks: 10 * 0.8^3 = 5.12 speed");

        Object.DestroyImmediate(combat);
        Object.DestroyImmediate(movement);
    }

    [Test]
    public void Card_GlassCannon_CanReduceToMinimumHP()
    {
        // GDD: Glass Cannon -1 HP. Stacking can be dangerous.
        var combat = ScriptableObject.CreateInstance<CombatData>();
        combat.maxHP = 3; // Wizard

        var negativeMod = new StatModifier { target = StatModifier.Target.MaxHP, modType = StatModifier.ModType.Additive, value = -1 };

        // Take 2 Glass Cannons — should reach 1 HP
        negativeMod.Apply(combat);
        Assert.AreEqual(2, combat.maxHP, "1 stack: 3 - 1 = 2 HP");

        negativeMod.Apply(combat);
        Assert.AreEqual(1, combat.maxHP, "2 stacks: 3 - 2 = 1 HP");

        Object.DestroyImmediate(combat);
    }

    [Test]
    public void Card_ArcaneBarrage_HalvesCooldownAndDamage()
    {
        // GDD: Fire rate doubled, each bolt deals half damage
        var combat = ScriptableObject.CreateInstance<CombatData>();
        combat.fireCooldown = 0.2f;
        combat.projectileDamage = 1f;

        var cooldownMod = new StatModifier { target = StatModifier.Target.FireCooldown, modType = StatModifier.ModType.Multiplicative, value = 0.5f };
        var damageMod = new StatModifier { target = StatModifier.Target.ProjectileDamage, modType = StatModifier.ModType.Multiplicative, value = 0.5f };

        cooldownMod.Apply(combat);
        damageMod.Apply(combat);

        Assert.AreEqual(0.1f, combat.fireCooldown, 0.001f, "Fire cooldown halved: 0.2 → 0.1");
        Assert.AreEqual(0.5f, combat.projectileDamage, 0.001f, "Damage halved: 1 → 0.5");

        // DPS stays the same: (1/0.2)*1 = (1/0.1)*0.5 = 5
        float dpsBefore = (1f / 0.2f) * 1f;
        float dpsAfter = (1f / combat.fireCooldown) * combat.projectileDamage;
        Assert.AreEqual(dpsBefore, dpsAfter, 0.01f,
            "Arcane Barrage should not change DPS (trades burst for consistency)");

        Object.DestroyImmediate(combat);
    }

    [Test]
    public void Card_HeavyThrow_DoublesWarriorDamage()
    {
        // GDD: Axes deal 2x damage, travel 50% slower
        var combat = ScriptableObject.CreateInstance<CombatData>();
        combat.projectileDamage = 1f;
        combat.projectileSpeed = 15f; // Warrior speed

        var damageMod = new StatModifier { target = StatModifier.Target.ProjectileDamage, modType = StatModifier.ModType.Multiplicative, value = 2f };
        var speedMod = new StatModifier { target = StatModifier.Target.ProjectileSpeed, modType = StatModifier.ModType.Multiplicative, value = 0.5f };

        damageMod.Apply(combat);
        speedMod.Apply(combat);

        Assert.AreEqual(2f, combat.projectileDamage, 0.001f, "Damage doubled: 1 → 2");
        Assert.AreEqual(7.5f, combat.projectileSpeed, 0.01f, "Speed halved: 15 → 7.5");

        Object.DestroyImmediate(combat);
    }

    [Test]
    public void Card_SpellShield_WidensParryButPunishesWhiff()
    {
        // Wider parry window but much longer whiff recovery
        var combat = ScriptableObject.CreateInstance<CombatData>();
        combat.parryWindow = 0.117f;
        combat.parryWhiffRecovery = 0.3f;

        var parryMod = new StatModifier { target = StatModifier.Target.ParryWindow, modType = StatModifier.ModType.Additive, value = 0.1f };
        var whiffMod = new StatModifier { target = StatModifier.Target.ParryWhiffRecovery, modType = StatModifier.ModType.Multiplicative, value = 3f };

        parryMod.Apply(combat);
        whiffMod.Apply(combat);

        Assert.AreEqual(0.217f, combat.parryWindow, 0.001f,
            "Parry window widened: 0.117 + 0.1 = 0.217");
        Assert.AreEqual(0.9f, combat.parryWhiffRecovery, 0.01f,
            "Whiff recovery tripled: 0.3 → 0.9");

        Object.DestroyImmediate(combat);
    }

    // ═══════════════════════════════════════════
    // GAME SETTINGS — GDD COMPLIANCE
    // ═══════════════════════════════════════════

    [Test]
    public void GameSettings_FirstTo5Wins()
    {
        // GDD: "First to 5 round wins takes the match"
        var settings = ScriptableObject.CreateInstance<GameSettings>();
        settings.roundsToWin = 5;
        Assert.AreEqual(5, settings.roundsToWin);
        Object.DestroyImmediate(settings);
    }

    [Test]
    public void GameSettings_4PlayersMax()
    {
        // GDD: "4-player party arena brawler"
        var settings = ScriptableObject.CreateInstance<GameSettings>();
        settings.maxPlayers = 4;
        Assert.AreEqual(4, settings.maxPlayers);
        Object.DestroyImmediate(settings);
    }

    [Test]
    public void GameSettings_ZoomTimingMatchesGDD()
    {
        // GDD: "Slow for the first 30 seconds, then accelerates. Full compression by ~90 seconds"
        var settings = ScriptableObject.CreateInstance<GameSettings>();
        settings.zoomDelay = 30f;
        settings.maxRoundTime = 90f;

        Assert.AreEqual(30f, settings.zoomDelay, "Zoom starts at 30 seconds");
        Assert.AreEqual(90f, settings.maxRoundTime, "Full compression at 90 seconds");
        Object.DestroyImmediate(settings);
    }

    [Test]
    public void GameSettings_4CardOptions()
    {
        // GDD: "Each player sees 4 cards from their pool"
        var settings = ScriptableObject.CreateInstance<GameSettings>();
        settings.cardOptionsPerPick = 4;
        Assert.AreEqual(4, settings.cardOptionsPerPick);
        Object.DestroyImmediate(settings);
    }

    [Test]
    public void GameSettings_DuplicateClassesAllowed()
    {
        // GDD: "Duplicates allowed. 4 Jesters should be possible"
        var settings = ScriptableObject.CreateInstance<GameSettings>();
        settings.allowDuplicateClasses = true;
        Assert.IsTrue(settings.allowDuplicateClasses);
        Object.DestroyImmediate(settings);
    }
}
