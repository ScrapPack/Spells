using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for MonsterEntity: initialization, damage, death, kill credit.
/// </summary>
[TestFixture]
public class MonsterEntityTests
{
    private MonsterData CreateTestMonsterData()
    {
        var data = ScriptableObject.CreateInstance<MonsterData>();
        data.monsterName = "TestMonster";
        data.baseHP = 3;
        data.baseDamage = 1f;
        data.attackCooldown = 2f;
        data.hpPerLevelPool = 0.5f;
        data.detectionRadius = 10f;
        data.attackTelegraphDuration = 0.5f;
        data.patrolSpeed = 1f;
        return data;
    }

    private MonsterEntity CreateTestMonster(MonsterData data, int levelPool = 0)
    {
        var go = new GameObject("TestMonster");
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<BoxCollider2D>();
        var monster = go.AddComponent<MonsterEntity>();
        monster.Initialize(data, 0, levelPool);
        return monster;
    }

    [Test]
    public void Initialize_SetsHP()
    {
        var data = CreateTestMonsterData();
        var monster = CreateTestMonster(data, 0);

        Assert.AreEqual(3, monster.CurrentHP);
        Assert.AreEqual(3, monster.MaxHP);

        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void Initialize_ScalesWithLevelPool()
    {
        var data = CreateTestMonsterData();
        var monster = CreateTestMonster(data, 10);

        // 3 + RoundToInt(0.5 * 10) = 8
        Assert.AreEqual(8, monster.MaxHP);
        Assert.AreEqual(8, monster.CurrentHP);

        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void TakeDamage_ReducesHP()
    {
        var data = CreateTestMonsterData();
        var monster = CreateTestMonster(data, 0);

        monster.TakeDamage(1, 0);
        Assert.AreEqual(2, monster.CurrentHP);

        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void TakeDamage_MultipleTimes()
    {
        var data = CreateTestMonsterData();
        var monster = CreateTestMonster(data, 0);

        monster.TakeDamage(1, 0);
        monster.TakeDamage(1, 1);
        Assert.AreEqual(1, monster.CurrentHP);

        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void TakeDamage_FiresOnDamagedEvent()
    {
        var data = CreateTestMonsterData();
        var monster = CreateTestMonster(data, 0);

        int eventCurrentHP = -1;
        int eventMaxHP = -1;
        monster.OnDamaged.AddListener((hp, max) => { eventCurrentHP = hp; eventMaxHP = max; });

        monster.TakeDamage(1, 0);

        Assert.AreEqual(2, eventCurrentHP);
        Assert.AreEqual(3, eventMaxHP);

        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void TakeDamage_KillFiresOnMonsterKilled()
    {
        var data = CreateTestMonsterData();
        var monster = CreateTestMonster(data, 0);

        int killerID = -1;
        monster.OnMonsterKilled.AddListener((id) => { killerID = id; });

        monster.TakeDamage(3, 2); // Player 2 deals lethal damage

        Assert.AreEqual(2, killerID);

        // Monster should be destroyed (queued)
        Object.DestroyImmediate(data);
    }

    [Test]
    public void TakeDamage_WhenDead_DoesNothing()
    {
        var data = CreateTestMonsterData();
        var monster = CreateTestMonster(data, 0);

        monster.TakeDamage(3, 0); // Kill it
        // Second damage call should not crash
        monster.TakeDamage(1, 1);

        Object.DestroyImmediate(data);
    }

    [Test]
    public void Initialize_StartsInPatrolState()
    {
        var data = CreateTestMonsterData();
        var monster = CreateTestMonster(data, 0);

        Assert.AreEqual(MonsterEntity.AIState.Patrol, monster.CurrentAIState);

        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(data);
    }
}
