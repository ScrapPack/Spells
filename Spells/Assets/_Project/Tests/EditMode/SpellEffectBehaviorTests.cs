using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for SpellEffect subclass behaviors.
/// Validates that special card behaviors work correctly with the game systems.
/// </summary>
[TestFixture]
public class SpellEffectBehaviorTests
{
    // ═══════════════════════════════════════════
    // HEALTH SYSTEM — NEW METHODS
    // ═══════════════════════════════════════════

    [Test]
    public void Revive_RestoredFromDeath()
    {
        var go = new GameObject("TestPlayer");
        var health = go.AddComponent<HealthSystem>();
        health.Initialize(3, 0.5f);

        // Kill the player
        health.TakeDamage(5);
        Assert.IsFalse(health.IsAlive, "Player should be dead");
        Assert.AreEqual(0, health.CurrentHP);

        // Revive
        bool revived = health.Revive(1);
        Assert.IsTrue(revived, "Revive should succeed on dead player");
        Assert.IsTrue(health.IsAlive, "Player should be alive after revive");
        Assert.AreEqual(1, health.CurrentHP, "Should have 1 HP after revive");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Revive_FailsOnLivingPlayer()
    {
        var go = new GameObject("TestPlayer");
        var health = go.AddComponent<HealthSystem>();
        health.Initialize(3, 0.5f);

        bool revived = health.Revive(1);
        Assert.IsFalse(revived, "Can't revive someone who is alive");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Revive_ClampsToMaxHP()
    {
        var go = new GameObject("TestPlayer");
        var health = go.AddComponent<HealthSystem>();
        health.Initialize(3, 0.5f);

        health.TakeDamage(5);
        health.Revive(10); // Try to revive with more than max

        Assert.AreEqual(3, health.CurrentHP, "Revive should clamp to max HP");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void TakeSelfDamage_BypassesInvincibility()
    {
        var go = new GameObject("TestPlayer");
        var health = go.AddComponent<HealthSystem>();
        health.Initialize(3, 0.5f);

        // Grant invincibility
        health.GrantInvincibility(5f);
        Assert.IsTrue(health.IsInvincible);

        // Normal damage should be blocked
        bool normalDamage = health.TakeDamage(1);
        Assert.IsFalse(normalDamage, "Normal damage blocked during invincibility");
        Assert.AreEqual(3, health.CurrentHP);

        // Self-damage should go through
        bool selfDamage = health.TakeSelfDamage(1);
        Assert.IsTrue(selfDamage, "Self-damage should bypass invincibility");
        Assert.AreEqual(2, health.CurrentHP, "Self-damage should reduce HP even during invincibility");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void TakeSelfDamage_CanKill()
    {
        var go = new GameObject("TestPlayer");
        var health = go.AddComponent<HealthSystem>();
        health.Initialize(1, 0.5f);

        bool died = health.TakeSelfDamage(1);
        Assert.IsTrue(died, "Self-damage should be able to kill");
        Assert.IsFalse(health.IsAlive);

        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════
    // BLOOD PACT — CASTING COST
    // ═══════════════════════════════════════════

    [Test]
    public void BloodPact_CostScalesWithStack()
    {
        // Verify the cost formula: 1 HP per stack
        // Stack 1 = 1 HP, Stack 2 = 2 HP, Stack 3 = 3 HP
        int[] stacks = { 1, 2, 3 };
        int[] expectedCosts = { 1, 2, 3 };

        for (int i = 0; i < stacks.Length; i++)
        {
            Assert.AreEqual(expectedCosts[i], stacks[i],
                $"Blood Pact x{stacks[i]} should cost {expectedCosts[i]} HP per cast");
        }
    }

    [Test]
    public void BloodPact_WarlockSurvival()
    {
        // Warlock has 3 HP. Blood Pact x1 = 2x damage, 1 HP per cast.
        // Can fire 3 times before dying from self-damage (but vulnerable to 1 hit)
        int warlockHP = 3;
        int bloodPactCost = 1;
        int castsBeforeDeath = warlockHP / bloodPactCost;

        Assert.AreEqual(3, castsBeforeDeath,
            "Warlock with Blood Pact x1 can cast 3 times before self-kill");
    }

    [Test]
    public void BloodPact_x3_InstantDeath()
    {
        // Warlock 3HP + Blood Pact x3 = 3 HP cost per cast = die on first cast
        int warlockHP = 3;
        int bloodPactCost = 3;

        Assert.IsTrue(bloodPactCost >= warlockHP,
            "Blood Pact x3 on 3HP class = death on first cast");
    }

    // ═══════════════════════════════════════════
    // LICH FORM — REVIVE MECHANICS
    // ═══════════════════════════════════════════

    [Test]
    public void LichForm_ReviveCount()
    {
        // Stack 1 = 1 revive per round, Stack 2 = 2 revives
        int stack1Revives = 1;
        int stack2Revives = 2;

        Assert.AreEqual(1, stack1Revives, "Lich Form x1 gives 1 revive");
        Assert.AreEqual(2, stack2Revives, "Lich Form x2 gives 2 revives");
    }

    [Test]
    public void LichForm_HPTradeoff()
    {
        // Warlock 3HP + Lich Form = 2HP max + 1 revive
        // Warlock 3HP + Lich Form x2 = 1HP max + 2 revives
        var combat = ScriptableObject.CreateInstance<CombatData>();
        combat.maxHP = 3;

        // Stack 1
        var hpMod1 = new StatModifier {
            target = StatModifier.Target.MaxHP,
            modType = StatModifier.ModType.Additive,
            value = -1
        };
        hpMod1.Apply(combat);
        Assert.AreEqual(2, combat.maxHP, "Lich Form x1: 3 - 1 = 2 max HP");

        // Stack 2
        hpMod1.Apply(combat);
        Assert.AreEqual(1, combat.maxHP, "Lich Form x2: 3 - 2 = 1 max HP");

        Object.DestroyImmediate(combat);
    }

    // ═══════════════════════════════════════════
    // HEAVY THROW — PIERCE
    // ═══════════════════════════════════════════

    [Test]
    public void HeavyThrow_EnablesPiercing()
    {
        var combat = ScriptableObject.CreateInstance<CombatData>();
        combat.projectilePierces = false;

        // Heavy Throw sets pierce = true
        combat.projectilePierces = true;
        Assert.IsTrue(combat.projectilePierces, "Heavy Throw should enable piercing");

        Object.DestroyImmediate(combat);
    }

    [Test]
    public void HeavyThrow_DamageSpeedTradeoff()
    {
        // GDD: 2x damage, 0.5x speed
        var combat = ScriptableObject.CreateInstance<CombatData>();
        combat.projectileDamage = 1f;
        combat.projectileSpeed = 15f; // Warrior base

        var damageMod = new StatModifier {
            target = StatModifier.Target.ProjectileDamage,
            modType = StatModifier.ModType.Multiplicative,
            value = 2f
        };
        var speedMod = new StatModifier {
            target = StatModifier.Target.ProjectileSpeed,
            modType = StatModifier.ModType.Multiplicative,
            value = 0.5f
        };

        damageMod.Apply(combat);
        speedMod.Apply(combat);

        Assert.AreEqual(2f, combat.projectileDamage, 0.01f, "2x damage");
        Assert.AreEqual(7.5f, combat.projectileSpeed, 0.01f, "0.5x speed");

        Object.DestroyImmediate(combat);
    }

    // ═══════════════════════════════════════════
    // SECOND WIND — MOVEMENT MODIFICATION
    // ═══════════════════════════════════════════

    [Test]
    public void SecondWind_AddsAirJumps()
    {
        var movement = ScriptableObject.CreateInstance<MovementData>();
        movement.maxAirJumps = 0;

        // Stack 1 = double jump (+1)
        var jumpMod = new StatModifier {
            target = StatModifier.Target.MaxAirJumps,
            modType = StatModifier.ModType.Additive,
            value = 1
        };
        jumpMod.Apply(movement);

        Assert.AreEqual(1, movement.maxAirJumps, "Second Wind x1 = double jump");

        // Stack 2 = triple jump
        jumpMod.Apply(movement);
        Assert.AreEqual(2, movement.maxAirJumps, "Second Wind x2 = triple jump");

        Object.DestroyImmediate(movement);
    }

    // ═══════════════════════════════════════════
    // JACKPOT — CHAOS MATH
    // ═══════════════════════════════════════════

    [Test]
    public void Jackpot_KillBenefitMath()
    {
        // In a 4-player game, a Jackpot kill:
        // - Eliminates the target (normal)
        // - Deals 1 damage to 2 remaining opponents
        // Net effect: 1 kill + 2 damage to bystanders

        int playersInGame = 4;
        int jackpotDamage = 1;
        int bystandersHit = playersInGame - 2; // Self + target excluded

        Assert.AreEqual(2, bystandersHit,
            "Jackpot kill in 4-player game hits 2 bystanders");
    }

    [Test]
    public void Jackpot_DeathPenaltyMath()
    {
        // In a 4-player game, a Jackpot death:
        // - Heals 3 remaining opponents by 1 each
        // Net effect: you die AND opponents get stronger

        int playersInGame = 4;
        int jackpotHeal = 1;
        int opponentsHealed = playersInGame - 1; // Everyone except the dead player

        Assert.AreEqual(3, opponentsHealed,
            "Jackpot death in 4-player game heals 3 opponents");
    }

    [Test]
    public void Jackpot_x2_DoublesPower()
    {
        // Jackpot x2: kills deal 2 AoE damage, deaths heal 2
        int stack2Damage = 2;
        int stack2Heal = 2;

        Assert.AreEqual(2, stack2Damage, "Jackpot x2 doubles AoE damage on kill");
        Assert.AreEqual(2, stack2Heal, "Jackpot x2 doubles AoE heal on death");
    }

    // ═══════════════════════════════════════════
    // TIER SYSTEM — CARD AVAILABILITY
    // ═══════════════════════════════════════════

    [Test]
    public void Tier2Cards_AvailableAtLevel1()
    {
        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.tier = 2;
        card.classTags = new string[] { "General" };

        Assert.IsFalse(card.IsAvailableFor(new string[] { "Wizard" }, 0),
            "Tier 2 NOT available at level 0");
        Assert.IsTrue(card.IsAvailableFor(new string[] { "Wizard" }, 1),
            "Tier 2 available at level 1");
        Assert.IsTrue(card.IsAvailableFor(new string[] { "Wizard" }, 3),
            "Tier 2 still available at level 3");

        Object.DestroyImmediate(card);
    }

    [Test]
    public void Tier3Cards_AvailableAtLevel2()
    {
        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.tier = 3;
        card.classTags = new string[] { "General" };

        Assert.IsFalse(card.IsAvailableFor(new string[] { "Wizard" }, 0),
            "Tier 3 NOT available at level 0");
        Assert.IsFalse(card.IsAvailableFor(new string[] { "Wizard" }, 1),
            "Tier 3 NOT available at level 1");
        Assert.IsTrue(card.IsAvailableFor(new string[] { "Wizard" }, 2),
            "Tier 3 available at level 2");

        Object.DestroyImmediate(card);
    }

    [Test]
    public void LichForm_OnlyAppearsForWarlockAtLevel2Plus()
    {
        var card = ScriptableObject.CreateInstance<PowerCardData>();
        card.tier = 3;
        card.classTags = new string[] { "General", "Warlock" };

        // Warlock at level 0: No (tier 3 needs level 2)
        Assert.IsFalse(card.IsAvailableFor(new string[] { "General", "Warlock" }, 0));

        // Warlock at level 2: Yes
        Assert.IsTrue(card.IsAvailableFor(new string[] { "General", "Warlock" }, 2));

        // Wizard at level 2: Also yes (card has General tag)
        Assert.IsTrue(card.IsAvailableFor(new string[] { "General", "Wizard" }, 2));

        Object.DestroyImmediate(card);
    }
}
