using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for MonsterData scaling formulas.
/// Verifies HP, damage, and cooldown scale correctly with level pool.
/// </summary>
[TestFixture]
public class MonsterDataTests
{
    private MonsterData CreateTestMonster()
    {
        var data = ScriptableObject.CreateInstance<MonsterData>();
        data.baseHP = 3;
        data.baseDamage = 1f;
        data.attackCooldown = 2f;
        data.hpPerLevelPool = 0.5f;
        data.damagePerLevelPool = 0.1f;
        data.cooldownReductionPerPool = 0.03f;
        data.minAttackCooldown = 0.8f;
        return data;
    }

    [Test]
    public void GetScaledHP_AtZeroPool_ReturnsBaseHP()
    {
        var data = CreateTestMonster();
        Assert.AreEqual(3, data.GetScaledHP(0));
        Object.DestroyImmediate(data);
    }

    [Test]
    public void GetScaledHP_AtPoolTen_ScalesCorrectly()
    {
        var data = CreateTestMonster();
        // 3 + RoundToInt(0.5 * 10) = 3 + 5 = 8
        Assert.AreEqual(8, data.GetScaledHP(10));
        Object.DestroyImmediate(data);
    }

    [Test]
    public void GetScaledHP_NeverBelowOne()
    {
        var data = CreateTestMonster();
        data.baseHP = 0;
        data.hpPerLevelPool = 0f;
        Assert.GreaterOrEqual(data.GetScaledHP(0), 1);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void GetScaledDamage_AtZeroPool_ReturnsBaseDamage()
    {
        var data = CreateTestMonster();
        Assert.AreEqual(1f, data.GetScaledDamage(0), 0.01f);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void GetScaledDamage_ScalesWithPool()
    {
        var data = CreateTestMonster();
        // 1 + 0.1 * 5 = 1.5
        Assert.AreEqual(1.5f, data.GetScaledDamage(5), 0.01f);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void GetScaledCooldown_ReducesWithPool()
    {
        var data = CreateTestMonster();
        // 2.0 - 0.03 * 10 = 1.7
        Assert.AreEqual(1.7f, data.GetScaledCooldown(10), 0.01f);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void GetScaledCooldown_ClampsToMinimum()
    {
        var data = CreateTestMonster();
        // 2.0 - 0.03 * 100 = -1.0 → clamped to 0.8
        Assert.AreEqual(0.8f, data.GetScaledCooldown(100), 0.01f);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void GetScaledCooldown_AtZeroPool_ReturnsBaseCooldown()
    {
        var data = CreateTestMonster();
        Assert.AreEqual(2f, data.GetScaledCooldown(0), 0.01f);
        Object.DestroyImmediate(data);
    }
}
