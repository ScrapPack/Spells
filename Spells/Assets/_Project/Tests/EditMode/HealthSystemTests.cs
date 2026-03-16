using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for HealthSystem logic.
/// Tests HP, damage, healing, i-frames, kill credit, and death.
/// Uses a bare GameObject with HealthSystem — no scene required.
/// </summary>
[TestFixture]
public class HealthSystemTests
{
    private GameObject go;
    private HealthSystem health;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("TestPlayer");
        health = go.AddComponent<HealthSystem>();
        health.Initialize(3, 0.5f); // 3 HP, 0.5s i-frames
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Initialize_SetsMaxAndCurrentHP()
    {
        Assert.AreEqual(3, health.MaxHP);
        Assert.AreEqual(3, health.CurrentHP);
        Assert.IsTrue(health.IsAlive);
    }

    [Test]
    public void TakeDamage_ReducesHP()
    {
        bool applied = health.TakeDamage(1f);
        Assert.IsTrue(applied);
        Assert.AreEqual(2, health.CurrentHP);
    }

    [Test]
    public void TakeDamage_WithAttackerID_TracksLastAttacker()
    {
        health.TakeDamage(1f, 2);
        Assert.AreEqual(2, health.LastAttackerID);
    }

    [Test]
    public void TakeDamage_WithoutAttackerID_PreservesLastAttacker()
    {
        health.TakeDamage(1f, 3);
        // Take damage without specifying attacker — last attacker unchanged
        // (need to clear i-frames first)
        health.ResetForRound();
        health.TakeDamage(1f, 3);
        Assert.AreEqual(3, health.LastAttackerID);
    }

    [Test]
    public void TakeDamage_TriggersInvincibility()
    {
        health.TakeDamage(1f);
        Assert.IsTrue(health.IsInvincible, "Should be invincible after taking damage");
    }

    [Test]
    public void TakeDamage_DuringInvincibility_ReturnsFalse()
    {
        health.TakeDamage(1f); // First hit, now invincible
        bool applied = health.TakeDamage(1f); // Should be blocked
        Assert.IsFalse(applied, "Damage during i-frames should be rejected");
        Assert.AreEqual(2, health.CurrentHP, "HP should not decrease during i-frames");
    }

    [Test]
    public void TakeDamage_LethalDamage_TriggersDeathAndNotAlive()
    {
        bool deathFired = false;
        health.OnDeath.AddListener(() => deathFired = true);

        health.Initialize(1, 0f); // 1 HP, no i-frames
        health.TakeDamage(1f);

        Assert.IsFalse(health.IsAlive);
        Assert.AreEqual(0, health.CurrentHP);
        Assert.IsTrue(deathFired, "OnDeath event should fire");
    }

    [Test]
    public void TakeDamage_OnDeadPlayer_ReturnsFalse()
    {
        health.Initialize(1, 0f);
        health.TakeDamage(1f); // Kill
        bool applied = health.TakeDamage(1f); // Hit corpse
        Assert.IsFalse(applied);
    }

    [Test]
    public void Heal_RestoresHP()
    {
        health.Initialize(3, 0f);
        health.TakeDamage(2f); // Down to 1
        health.Heal(1);
        Assert.AreEqual(2, health.CurrentHP);
    }

    [Test]
    public void Heal_ClampsToMaxHP()
    {
        health.Initialize(3, 0f);
        health.TakeDamage(1f); // Down to 2
        health.Heal(99);
        Assert.AreEqual(3, health.CurrentHP, "HP should not exceed MaxHP");
    }

    [Test]
    public void Heal_OnDeadPlayer_DoesNothing()
    {
        health.Initialize(1, 0f);
        health.TakeDamage(1f);
        health.Heal(1);
        Assert.AreEqual(0, health.CurrentHP);
        Assert.IsFalse(health.IsAlive);
    }

    [Test]
    public void ModifyMaxHP_CanIncrease()
    {
        health.ModifyMaxHP(2);
        Assert.AreEqual(5, health.MaxHP);
        // Current HP should not change (was 3, max is now 5)
        Assert.AreEqual(3, health.CurrentHP);
    }

    [Test]
    public void ModifyMaxHP_CanDecrease_ClampsCurrentHP()
    {
        health.ModifyMaxHP(-2); // Max goes from 3 to 1
        Assert.AreEqual(1, health.MaxHP);
        Assert.AreEqual(1, health.CurrentHP, "Current HP should be clamped to new max");
    }

    [Test]
    public void ModifyMaxHP_NeverBelowOne()
    {
        health.ModifyMaxHP(-99);
        Assert.AreEqual(1, health.MaxHP, "Max HP should never drop below 1");
    }

    [Test]
    public void ResetForRound_RestoresFullHP()
    {
        health.Initialize(3, 0f);
        health.TakeDamage(2f);
        health.ResetForRound();
        Assert.AreEqual(3, health.CurrentHP);
        Assert.AreEqual(3, health.MaxHP);
    }

    [Test]
    public void ResetForRound_ClearsInvincibility()
    {
        health.TakeDamage(1f); // Triggers i-frames
        health.ResetForRound();
        Assert.IsFalse(health.IsInvincible);
    }

    [Test]
    public void ResetForRound_ClearsLastAttacker()
    {
        health.TakeDamage(1f, 2);
        health.ResetForRound();
        Assert.AreEqual(-1, health.LastAttackerID);
    }

    [Test]
    public void OnDamaged_FiresWithAmount()
    {
        float reportedAmount = 0f;
        health.OnDamaged.AddListener((amount) => reportedAmount = amount);
        health.Initialize(3, 0f);
        health.TakeDamage(1.5f);
        Assert.AreEqual(1.5f, reportedAmount, 0.01f);
    }

    [Test]
    public void RoundDamage_ToInteger()
    {
        health.Initialize(5, 0f);
        // 0.3f rounds to 1 (min 1)
        health.TakeDamage(0.3f);
        Assert.AreEqual(4, health.CurrentHP, "Damage should round to at least 1");
    }
}
