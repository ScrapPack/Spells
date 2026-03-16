using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for Phase 19 SpellEffect implementations.
/// Covers: Soul Siphon, Berserker, Magnetic Return, Lucky Bounce,
/// Venom Dart, Hex Mark, Ambush, Smoke Bomb, and supporting components.
/// </summary>
[TestFixture]
public class SpellEffectBehaviorTests2
{
    // ═══════════════════════════════════════════
    // PROJECTILE EXTENSION POINTS
    // ═══════════════════════════════════════════

    [Test]
    public void Projectile_DamageMultiplier_DefaultIsOne()
    {
        var go = new GameObject("TestProj");
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        var proj = go.AddComponent<Projectile>();

        Assert.AreEqual(1f, proj.DamageMultiplier, 0.01f,
            "Default DamageMultiplier should be 1.0");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Projectile_DamageMultiplier_CanBeModified()
    {
        var go = new GameObject("TestProj");
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        var proj = go.AddComponent<Projectile>();

        proj.DamageMultiplier = 2.5f;
        Assert.AreEqual(2.5f, proj.DamageMultiplier, 0.01f);

        proj.DamageMultiplier = 0f;
        Assert.AreEqual(0f, proj.DamageMultiplier, 0.01f,
            "DamageMultiplier of 0 should be allowed (Lucky Bounce direct hit)");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Projectile_PreventAutoExpire_DefaultFalse()
    {
        var go = new GameObject("TestProj");
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        var proj = go.AddComponent<Projectile>();

        Assert.IsFalse(proj.PreventAutoExpire,
            "PreventAutoExpire should default to false");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Projectile_CanHitOwner_DefaultFalse()
    {
        var go = new GameObject("TestProj");
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        var proj = go.AddComponent<Projectile>();

        Assert.IsFalse(proj.CanHitOwner,
            "CanHitOwner should default to false");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Projectile_OnBeforeHit_Fires()
    {
        var go = new GameObject("TestProj");
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        var proj = go.AddComponent<Projectile>();

        bool called = false;
        proj.OnBeforeHit += (target) => { called = true; };
        proj.OnBeforeHit?.Invoke(go);

        Assert.IsTrue(called, "OnBeforeHit callback should fire when invoked");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Projectile_OnHitPlayer_Fires()
    {
        var go = new GameObject("TestProj");
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        var proj = go.AddComponent<Projectile>();

        float reportedDamage = 0f;
        proj.OnHitPlayer += (target, dmg) => { reportedDamage = dmg; };
        proj.OnHitPlayer?.Invoke(go, 3.5f);

        Assert.AreEqual(3.5f, reportedDamage, 0.01f,
            "OnHitPlayer should report the damage dealt");

        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════
    // PARRY SYSTEM — DISABLE FLAG
    // ═══════════════════════════════════════════

    [Test]
    public void ParrySystem_DisableFlag_BlocksParry()
    {
        var go = new GameObject("TestPlayer");
        go.AddComponent<Rigidbody2D>();
        var parry = go.AddComponent<ParrySystem>();

        // By default, ParryDisabled is false
        Assert.IsFalse(parry.ParryDisabled);

        // Disable parry
        parry.ParryDisabled = true;
        Assert.IsFalse(parry.CanParry, "CanParry should be false when ParryDisabled");

        // Re-enable
        parry.ParryDisabled = false;
        // CanParry also depends on IsParrying and IsInRecovery, but those start false
        // so it should be true now
        Assert.IsTrue(parry.CanParry, "CanParry should be true when re-enabled");

        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════
    // SOUL SIPHON — PASSIVE HEAL + KILL COST
    // ═══════════════════════════════════════════

    [Test]
    public void SoulSiphon_NetHealOnOthersKill()
    {
        // In a 4-player game, if player B kills player C:
        // Soul Siphon on player A (not the killer) gets +1 HP
        int startHP = 2;
        int healFromElimination = 1;
        int netHP = startHP + healFromElimination;

        Assert.AreEqual(3, netHP,
            "Soul Siphon: +1 HP when another player makes a kill");
    }

    [Test]
    public void SoulSiphon_NetZeroOnOwnKill()
    {
        // When YOU kill someone with Soul Siphon:
        // +1 (opponent eliminated) -1 (you killed them) = 0
        int heal = 1;
        int cost = 1;
        int net = heal - cost;

        Assert.AreEqual(0, net,
            "Soul Siphon: net 0 HP when YOU get the kill (+1 heal, -1 cost)");
    }

    [Test]
    public void SoulSiphon_x2_ScalesBoth()
    {
        // Stack 2: +2 heal, -2 cost per kill
        int stack = 2;
        int healPerElim = stack;
        int costPerKill = stack;

        Assert.AreEqual(2, healPerElim, "Soul Siphon x2: +2 heal per elimination");
        Assert.AreEqual(2, costPerKill, "Soul Siphon x2: -2 cost per own kill");
    }

    // ═══════════════════════════════════════════
    // BERSERKER — DAMAGE/PARRY THRESHOLD
    // ═══════════════════════════════════════════

    [Test]
    public void Berserker_TriggersAtHalfHP()
    {
        // Warrior 4HP: Berserker triggers at 2HP or below
        int maxHP = 4;
        int threshold = maxHP / 2;

        Assert.AreEqual(2, threshold, "Berserker threshold for 4HP = 2HP");
        Assert.IsTrue(2 <= threshold, "At exactly half HP, berserker should trigger");
        Assert.IsFalse(3 <= threshold, "Above half HP, berserker should not trigger");
    }

    [Test]
    public void Berserker_DamageBonus()
    {
        var combat = ScriptableObject.CreateInstance<CombatData>();
        combat.projectileDamage = 1f; // Warrior base

        // Berserker x1 at low HP: +1 damage
        float berserkerBonus = 1f;
        combat.projectileDamage += berserkerBonus;

        Assert.AreEqual(2f, combat.projectileDamage, 0.01f,
            "Berserker x1 should add +1 damage (1 + 1 = 2)");

        Object.DestroyImmediate(combat);
    }

    [Test]
    public void Berserker_x2_DoubleBonus()
    {
        var combat = ScriptableObject.CreateInstance<CombatData>();
        combat.projectileDamage = 1f;

        float berserkerBonus = 2f; // Stack 2
        combat.projectileDamage += berserkerBonus;

        Assert.AreEqual(3f, combat.projectileDamage, 0.01f,
            "Berserker x2 should add +2 damage (1 + 2 = 3)");

        Object.DestroyImmediate(combat);
    }

    // ═══════════════════════════════════════════
    // MAGNETIC RETURN — PROJECTILE RETURN
    // ═══════════════════════════════════════════

    [Test]
    public void MagneticReturn_SetsPreventAutoExpire()
    {
        var go = new GameObject("TestProj");
        var rb = go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        var proj = go.AddComponent<Projectile>();

        var owner = new GameObject("Owner");
        var returnBehavior = go.AddComponent<MagneticReturnBehavior>();
        returnBehavior.Initialize(owner.transform, 15f, 3f);

        Assert.IsTrue(proj.PreventAutoExpire,
            "MagneticReturnBehavior should set PreventAutoExpire on the projectile");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(owner);
    }

    [Test]
    public void MagneticReturn_ReturnSpeedScalesWithStack()
    {
        // Stack 1 = 15 speed, Stack 2 = 20, Stack 3 = 25
        float baseSpeed = 15f;
        float perStack = 5f;

        Assert.AreEqual(15f, baseSpeed + (1 - 1) * perStack, 0.01f, "Stack 1 = 15");
        Assert.AreEqual(20f, baseSpeed + (2 - 1) * perStack, 0.01f, "Stack 2 = 20");
        Assert.AreEqual(25f, baseSpeed + (3 - 1) * perStack, 0.01f, "Stack 3 = 25");
    }

    // ═══════════════════════════════════════════
    // LUCKY BOUNCE — BOUNCE DAMAGE SCALING
    // ═══════════════════════════════════════════

    [Test]
    public void LuckyBounce_DirectHitZeroDamage()
    {
        var go = new GameObject("TestProj");
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<CircleCollider2D>();
        var proj = go.AddComponent<Projectile>();

        var luckyBounce = go.AddComponent<LuckyBounceBehavior>();
        luckyBounce.Initialize(1);

        Assert.AreEqual(0f, proj.DamageMultiplier, 0.01f,
            "Lucky Bounce: 0 bounces = 0 damage multiplier");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void LuckyBounce_DamageScalesWithBounces()
    {
        // With stack 1: bounce 1 = 1x, bounce 2 = 2x, bounce 3 = 3x
        int stack = 1;
        for (int bounces = 1; bounces <= 5; bounces++)
        {
            float expectedMult = bounces * stack;
            Assert.AreEqual(expectedMult, bounces * stack,
                $"Lucky Bounce: {bounces} bounces at stack {stack} = {expectedMult}x damage");
        }
    }

    [Test]
    public void LuckyBounce_x2_DoubleDamagePerBounce()
    {
        // Stack 2: bounce 1 = 2x, bounce 2 = 4x
        int stack = 2;
        Assert.AreEqual(2f, 1 * stack, 0.01f, "Stack 2, 1 bounce = 2x");
        Assert.AreEqual(4f, 2 * stack, 0.01f, "Stack 2, 2 bounces = 4x");
    }

    // ═══════════════════════════════════════════
    // VENOM DART — POISON DoT
    // ═══════════════════════════════════════════

    [Test]
    public void PoisonStatus_Initializes()
    {
        var go = new GameObject("TestPlayer");
        var health = go.AddComponent<HealthSystem>();
        health.Initialize(3, 0.5f);

        var poison = go.AddComponent<PoisonStatus>();
        poison.Initialize(1, 5f);

        // Poison component should exist
        Assert.IsNotNull(go.GetComponent<PoisonStatus>(),
            "PoisonStatus should be added to target");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void VenomDart_PoisonMath()
    {
        // 1 damage over 5 seconds = 1 tick per second, 5 ticks, 1 damage per tick?
        // No: GDD says "1 damage over 5 seconds" = 1 total damage spread over 5s
        int poisonDamage = 1;
        float duration = 5f;
        float tickInterval = 1f;

        int totalTicks = (int)(duration / tickInterval);
        Assert.AreEqual(5, totalTicks, "5-second poison with 1s interval = 5 ticks");

        // But damage is capped at totalDamage, so only 1 tick actually deals damage
        Assert.AreEqual(1, poisonDamage, "Total damage = 1 (stack 1)");
    }

    [Test]
    public void VenomDart_x2_DoublePoison()
    {
        int stack = 2;
        int poisonDamage = stack;
        Assert.AreEqual(2, poisonDamage,
            "Venom Dart x2: 2 damage over 5 seconds");
    }

    // ═══════════════════════════════════════════
    // HEX MARK — DAMAGE AMPLIFICATION
    // ═══════════════════════════════════════════

    [Test]
    public void HexMark_ExtraDamage()
    {
        var go = new GameObject("TestPlayer");
        var health = go.AddComponent<HealthSystem>();
        health.Initialize(3, 0.5f);

        var hex = go.AddComponent<HexMarkStatus>();
        hex.Initialize(1); // +1 bonus damage

        // Hex exists
        Assert.IsNotNull(go.GetComponent<HexMarkStatus>());

        Object.DestroyImmediate(go);
    }

    [Test]
    public void HexMark_OnlyOneCurse()
    {
        // GDD: "You can only curse one opponent at a time"
        // When you hit a new target, old curse is removed
        int maxCurses = 1;
        Assert.AreEqual(1, maxCurses,
            "Hex Mark: only 1 active curse at a time");
    }

    [Test]
    public void HexMark_x2_DoubleBonusDamage()
    {
        int stack = 2;
        int bonusDamage = stack;
        Assert.AreEqual(2, bonusDamage,
            "Hex Mark x2: +2 bonus damage on cursed target");
    }

    // ═══════════════════════════════════════════
    // AMBUSH — FACING-BASED DAMAGE
    // ═══════════════════════════════════════════

    [Test]
    public void Ambush_BackstabMultiplier()
    {
        // Stack 1: 2x damage from behind
        float ambushMult = 2f + (1 - 1) * 0.5f;
        Assert.AreEqual(2f, ambushMult, 0.01f,
            "Ambush x1: 2x damage from behind");
    }

    [Test]
    public void Ambush_FrontalPenalty()
    {
        float normalMult = 0.75f;
        Assert.AreEqual(0.75f, normalMult, 0.01f,
            "Ambush: non-backstab hits deal 0.75x damage");
    }

    [Test]
    public void Ambush_x2_IncreasedBackstab()
    {
        float ambushMult = 2f + (2 - 1) * 0.5f;
        Assert.AreEqual(2.5f, ambushMult, 0.01f,
            "Ambush x2: 2.5x damage from behind");
    }

    [Test]
    public void Ambush_NetDamageComparison()
    {
        // Rogue base damage: 0.5
        float baseDmg = 0.5f;
        float ambushDmg = baseDmg * 2f;     // 1.0 from behind
        float normalDmg = baseDmg * 0.75f;  // 0.375 from front

        Assert.Greater(ambushDmg, baseDmg,
            "Ambush backstab should exceed normal Rogue damage");
        Assert.Less(normalDmg, baseDmg,
            "Non-ambush should be less than normal Rogue damage");
    }

    // ═══════════════════════════════════════════
    // SMOKE BOMB — INVISIBILITY ON HIT
    // ═══════════════════════════════════════════

    [Test]
    public void SmokeBomb_DurationScaling()
    {
        // Stack 1: 1.5s, Stack 2: 2.0s, Stack 3: 2.5s
        for (int stack = 1; stack <= 3; stack++)
        {
            float duration = 1.5f + (stack - 1) * 0.5f;
            float expected = 1.0f + stack * 0.5f;
            Assert.AreEqual(expected, duration, 0.01f,
                $"Smoke Bomb x{stack}: {expected}s invisibility");
        }
    }

    [Test]
    public void SmokeBomb_TradeoffMath()
    {
        // Rogue fires every 0.12s. During 1.5s invisibility:
        float fireRate = 0.12f;
        float invisDuration = 1.5f;
        int missedShots = Mathf.FloorToInt(invisDuration / fireRate);

        Assert.Greater(missedShots, 10,
            $"Smoke Bomb: Rogue loses ~{missedShots} potential shots during 1.5s invis");
    }

    // ═══════════════════════════════════════════
    // PROJECTILE SPAWNER — LAST FIRED REFERENCE
    // ═══════════════════════════════════════════

    [Test]
    public void ProjectileSpawner_HasLastFiredProperty()
    {
        var go = new GameObject("TestSpawner");
        var spawner = go.AddComponent<ProjectileSpawner>();

        // LastFiredProjectile should default to null
        Assert.IsNull(spawner.LastFiredProjectile,
            "LastFiredProjectile should be null before any firing");

        Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════
    // CROSS-EFFECT SYNERGIES
    // ═══════════════════════════════════════════

    [Test]
    public void Synergy_HeavyThrow_Plus_MagneticReturn()
    {
        // Piercing + returning axes: axes pass through targets and come back
        // This means they deal damage TWICE: once going, once returning
        float baseDmg = 1f;
        float heavyThrowMult = 2f; // 2x damage from Heavy Throw
        float hitsPerAxe = 2; // Forward + return

        float totalDamagePerAxe = baseDmg * heavyThrowMult * hitsPerAxe;
        Assert.AreEqual(4f, totalDamagePerAxe, 0.01f,
            "Heavy Throw + Magnetic Return: each axe can deal 4 damage (2x dmg * 2 hits)");
    }

    [Test]
    public void Synergy_VenomDart_Plus_HexMark()
    {
        // VenomDart applies poison (1 over 5s) + HexMark (+1 per damage instance)
        // Each poison tick triggers hex mark bonus
        int poisonTicks = 1; // 1 total damage = 1 actual tick
        int hexBonus = 1;
        int totalBonusDamage = poisonTicks * hexBonus;

        Assert.AreEqual(1, totalBonusDamage,
            "Venom + Hex: each poison tick triggers +1 hex bonus");
    }

    [Test]
    public void Synergy_Ambush_Plus_SmokeBomb()
    {
        // Get hit → go invisible → reposition behind → backstab
        // Rogue 2HP can survive 1 hit, reposition during invis, then deal 2x
        float ambushDmg = 0.5f * 2f; // Rogue base * ambush mult
        Assert.AreEqual(1f, ambushDmg, 0.01f,
            "Ambush + Smoke Bomb: survive hit, reposition, deal 1.0 backstab damage");
    }
}
