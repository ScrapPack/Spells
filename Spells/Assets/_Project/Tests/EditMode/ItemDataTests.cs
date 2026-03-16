using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for ItemData defaults and availability filtering.
/// </summary>
[TestFixture]
public class ItemDataTests
{
    [Test]
    public void ItemData_DefaultValues()
    {
        var data = ScriptableObject.CreateInstance<ItemData>();
        Assert.AreEqual("New Item", data.itemName);
        Assert.AreEqual("", data.behaviorID);
        Assert.AreEqual(0, data.ammo);
        Assert.AreEqual(0, data.minLevelPool);
        Assert.AreEqual(1f, data.dropWeight, 0.01f);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void IsAvailableAtLevelPool_ZeroMin_AlwaysAvailable()
    {
        var data = ScriptableObject.CreateInstance<ItemData>();
        data.minLevelPool = 0;
        Assert.IsTrue(data.IsAvailableAtLevelPool(0));
        Assert.IsTrue(data.IsAvailableAtLevelPool(10));
        Object.DestroyImmediate(data);
    }

    [Test]
    public void IsAvailableAtLevelPool_HighMin_NotAvailableBelow()
    {
        var data = ScriptableObject.CreateInstance<ItemData>();
        data.minLevelPool = 5;
        Assert.IsFalse(data.IsAvailableAtLevelPool(4));
        Assert.IsTrue(data.IsAvailableAtLevelPool(5));
        Assert.IsTrue(data.IsAvailableAtLevelPool(10));
        Object.DestroyImmediate(data);
    }

    [Test]
    public void DropWeight_MustBePositive()
    {
        var data = ScriptableObject.CreateInstance<ItemData>();
        // Default should be positive
        Assert.Greater(data.dropWeight, 0f);
        Object.DestroyImmediate(data);
    }
}
