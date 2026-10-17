using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheLastArk.Data;
using TheLastArk.UI;
using UnityEngine;
using UnityEngine.UI;

public class ManagementInventoryTests
{
    [Test]
    public void GroupsCopiesWithoutCombiningCategoriesOrChangingInventory()
    {
        var sword = ScriptableObject.CreateInstance<EquipmentData>();
        var copy = ScriptableObject.CreateInstance<EquipmentData>();
        var relic = ScriptableObject.CreateInstance<RelicData>();
        try
        {
            sword.equipmentID = copy.equipmentID = relic.relicID = "same-id";
            sword.equipmentName = copy.equipmentName = "Sword";
            var source = new List<EquipmentData> { sword, null, copy, sword };
            var before = source.ToArray();
            var entries = ManagementInventory.Build(source, new[] { relic }, null);
            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual(3, entries.Single(e => e.Category == InventoryCategory.Equipment).Count);
            Assert.AreEqual(1, entries.Single(e => e.Category == InventoryCategory.Relic).Count);
            CollectionAssert.AreEqual(before, source);
        }
        finally { Object.DestroyImmediate(sword); Object.DestroyImmediate(copy); Object.DestroyImmediate(relic); }
    }

    [Test]
    public void ItemsWithoutIdsRemainDistinctAndNullSourcesAreSafe()
    {
        var first = ScriptableObject.CreateInstance<EquipmentData>();
        var second = ScriptableObject.CreateInstance<EquipmentData>();
        try
        {
            var entries = ManagementInventory.Build(new[] { first, second, first }, null, null);
            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual(3, entries.Sum(e => e.Count));
            Assert.IsEmpty(ManagementInventory.Build(null, null, null));
        }
        finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
    }

    [TestCase(InventorySort.Category, "B,C,A")]
    [TestCase(InventorySort.Name, "A,B,C")]
    [TestCase(InventorySort.Rarity, "C,B,A")]
    [TestCase(InventorySort.Count, "A,B,C")]
    public void SortsInRequestedOrderWithoutReorderingSource(InventorySort sort, string expected)
    {
        var entries = new List<InventoryEntry>
        {
            new InventoryEntry { Key = "C", Name = "C", Category = InventoryCategory.Relic, Rank = 3, Count = 1 },
            new InventoryEntry { Key = "A", Name = "A", Category = InventoryCategory.Consumable, Rank = 0, Count = 5 },
            new InventoryEntry { Key = "B", Name = "B", Category = InventoryCategory.Equipment, Rank = 1, Count = 2 }
        };
        var result = ManagementInventory.FilterAndSort(entries, InventoryCategory.All, sort);
        Assert.AreEqual(expected, string.Join(",", result.Select(e => e.Key)));
        Assert.AreEqual("C,A,B", string.Join(",", entries.Select(e => e.Key)));
        var filtered = ManagementInventory.FilterAndSort(entries, InventoryCategory.Equipment, sort);
        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual("B", filtered[0].Key);
    }

    [TestCase(860, 4)]
    [TestCase(530, 3)]
    [TestCase(360, 2)]
    [TestCase(230, 1)]
    public void GridKeepsCellsWithinAvailableWidth(float width, int expectedColumns)
    {
        var go = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        try
        {
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 500);
            go.AddComponent<ManagementGridLayout>().Configure(165, 178, 4);
            var grid = go.GetComponent<GridLayoutGroup>();
            Assert.AreEqual(expectedColumns, grid.constraintCount);
            float occupied = grid.cellSize.x * grid.constraintCount + grid.spacing.x * (grid.constraintCount - 1) + grid.padding.horizontal;
            Assert.LessOrEqual(occupied, width + 0.01f);
            Assert.Greater(grid.cellSize.x, 0);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
