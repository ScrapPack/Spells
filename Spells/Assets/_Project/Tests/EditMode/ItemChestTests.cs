using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for ItemChest: initialization, weighted selection, level pool filtering.
/// </summary>
[TestFixture]
public class ItemChestTests
{
    [Test]
    public void ItemData_IsAvailableAtLevelPool_MinZero()
    {
        var item = ScriptableObject.CreateInstance<ItemData>();
        item.minLevelPool = 0;

        Assert.IsTrue(item.IsAvailableAtLevelPool(0));
        Assert.IsTrue(item.IsAvailableAtLevelPool(99));

        Object.DestroyImmediate(item);
    }

    [Test]
    public void ItemData_IsAvailableAtLevelPool_MinFive()
    {
        var item = ScriptableObject.CreateInstance<ItemData>();
        item.minLevelPool = 5;

        Assert.IsFalse(item.IsAvailableAtLevelPool(0));
        Assert.IsFalse(item.IsAvailableAtLevelPool(4));
        Assert.IsTrue(item.IsAvailableAtLevelPool(5));
        Assert.IsTrue(item.IsAvailableAtLevelPool(10));

        Object.DestroyImmediate(item);
    }

    [Test]
    public void ItemData_DropWeight_Default()
    {
        var item = ScriptableObject.CreateInstance<ItemData>();
        Assert.AreEqual(1f, item.dropWeight, 0.01f);
        Object.DestroyImmediate(item);
    }

    [Test]
    public void ItemBehaviorRegistry_ResolvesKnownBehaviors()
    {
        Assert.IsNotNull(ItemBehaviorRegistry.GetBehaviorType("spider_shoes"));
        Assert.IsNotNull(ItemBehaviorRegistry.GetBehaviorType("fire_wand"));
        Assert.IsNotNull(ItemBehaviorRegistry.GetBehaviorType("hitscan_gun"));
    }

    [Test]
    public void ItemBehaviorRegistry_ReturnsNullForUnknown()
    {
        Assert.IsNull(ItemBehaviorRegistry.GetBehaviorType("nonexistent_item"));
    }

    [Test]
    public void ItemBehaviorRegistry_IsRegistered()
    {
        Assert.IsTrue(ItemBehaviorRegistry.IsRegistered("spider_shoes"));
        Assert.IsFalse(ItemBehaviorRegistry.IsRegistered("fake_item"));
    }
}
