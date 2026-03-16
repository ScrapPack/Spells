using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tests for TemporaryItemInventory: add, remove, clear, death behavior.
/// </summary>
[TestFixture]
public class TemporaryItemTests
{
    private (GameObject go, TemporaryItemInventory inv, HealthSystem health) CreateTestPlayer()
    {
        var go = new GameObject("TestPlayer");
        var health = go.AddComponent<HealthSystem>();
        health.Initialize(3, 0f);
        var inv = go.AddComponent<TemporaryItemInventory>();
        return (go, inv, health);
    }

    private ItemData CreateTestItem(string name, string behaviorID = "")
    {
        var data = ScriptableObject.CreateInstance<ItemData>();
        data.itemName = name;
        data.behaviorID = behaviorID;
        data.ammo = 0;
        return data;
    }

    [Test]
    public void AddItem_IncreasesCount()
    {
        var (go, inv, _) = CreateTestPlayer();
        var item = CreateTestItem("TestItem");

        inv.AddItem(item);
        Assert.AreEqual(1, inv.ItemCount);

        inv.AddItem(item);
        Assert.AreEqual(2, inv.ItemCount);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(item);
    }

    [Test]
    public void HasItem_ReturnsTrueWhenPresent()
    {
        var (go, inv, _) = CreateTestPlayer();
        var item = CreateTestItem("TestItem");

        Assert.IsFalse(inv.HasItem(item));
        inv.AddItem(item);
        Assert.IsTrue(inv.HasItem(item));

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(item);
    }

    [Test]
    public void RemoveItem_DecreasesCount()
    {
        var (go, inv, _) = CreateTestPlayer();
        var item = CreateTestItem("TestItem");

        inv.AddItem(item);
        Assert.AreEqual(1, inv.ItemCount);

        inv.RemoveItem(item);
        Assert.AreEqual(0, inv.ItemCount);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(item);
    }

    [Test]
    public void ClearAll_RemovesAllItems()
    {
        var (go, inv, _) = CreateTestPlayer();
        var item1 = CreateTestItem("Item1");
        var item2 = CreateTestItem("Item2");

        inv.AddItem(item1);
        inv.AddItem(item2);
        Assert.AreEqual(2, inv.ItemCount);

        inv.ClearAll();
        Assert.AreEqual(0, inv.ItemCount);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(item1);
        Object.DestroyImmediate(item2);
    }

    [Test]
    public void AddItem_NullItem_DoesNothing()
    {
        var (go, inv, _) = CreateTestPlayer();
        inv.AddItem(null);
        Assert.AreEqual(0, inv.ItemCount);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HasItemWithBehavior_MatchesBehaviorID()
    {
        var (go, inv, _) = CreateTestPlayer();
        var item = CreateTestItem("SpiderShoes", "spider_shoes");

        Assert.IsFalse(inv.HasItemWithBehavior("spider_shoes"));
        inv.AddItem(item);
        Assert.IsTrue(inv.HasItemWithBehavior("spider_shoes"));
        Assert.IsFalse(inv.HasItemWithBehavior("fire_wand"));

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(item);
    }
}
