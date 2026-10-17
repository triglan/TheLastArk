using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.UI;
using UnityEngine;

public class InventoryTransactionTests
{
    private readonly List<Object> owned = new List<Object>();
    private ResourceManager resources;
    private RunManager run;
    private object priorResources, priorRun, priorTrain;
    private Random.State random;
    private static FieldInfo StaticField(System.Type type, string name) => type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
    private T Own<T>(T value) where T : Object { owned.Add(value); return value; }

    [SetUp]
    public void Setup()
    {
        random = Random.state;
        priorResources = StaticField(typeof(ResourceManager), "instance").GetValue(null);
        priorRun = StaticField(typeof(RunManager), "_instance").GetValue(null);
        priorTrain = StaticField(typeof(TrainManager), "instance").GetValue(null);
        var go = Own(new GameObject("InventoryTransactionTest")); go.SetActive(false);
        resources = go.AddComponent<ResourceManager>(); run = go.AddComponent<RunManager>();
        StaticField(typeof(ResourceManager), "instance").SetValue(null, resources);
        StaticField(typeof(RunManager), "_instance").SetValue(null, run);
        StaticField(typeof(TrainManager), "instance").SetValue(null, null);
        run.State.gold = 200;
    }

    [TearDown]
    public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        StaticField(typeof(ResourceManager), "instance").SetValue(null, priorResources);
        StaticField(typeof(RunManager), "_instance").SetValue(null, priorRun);
        StaticField(typeof(TrainManager), "instance").SetValue(null, priorTrain);
        Random.state = random;
    }

    private CharacterStatus Crew(string id)
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.characterId = id; data.characterName = id;
        var status = new CharacterStatus(data);
        run.State.partyStatuses.Add(status); run.State.partyDataIDs.Add(id);
        return status;
    }

    [Test]
    public void FullConsumablePurchaseDoesNotChargeOrPublishEvents()
    {
        var item = Own(ScriptableObject.CreateInstance<ConsumableData>());
        resources.Consumables.AddRange(new[] { item, item, item });
        int events = 0;
        resources.OnGoldChanged += () => events++;
        resources.OnConsumablesChanged += () => events++;
        Assert.IsFalse(resources.TryPurchaseConsumable(item, 50, out _));
        Assert.AreEqual(200, resources.Gold); Assert.AreEqual(3, resources.Consumables.Count); Assert.AreEqual(0, events);
    }

    [Test]
    public void SuccessfulPurchasePublishesOnlyAfterGoldAndItemAreCommitted()
    {
        var item = Own(ScriptableObject.CreateInstance<ConsumableData>());
        int events = 0;
        resources.OnGoldChanged += () => { Assert.AreEqual(150, resources.Gold); Assert.AreSame(item, resources.Consumables.Single()); events++; };
        resources.OnConsumablesChanged += () => events++;
        Assert.IsTrue(resources.TryPurchaseConsumable(item, 50, out _));
        Assert.AreEqual(2, events);
    }

    [Test]
    public void DuplicateRelicAndMissingProductsDoNotCharge()
    {
        var relic = Own(ScriptableObject.CreateInstance<RelicData>()); relic.relicID = "owned";
        resources.Relics.Add(relic);
        Assert.IsFalse(resources.TryPurchaseRelic(relic, 50, out _));
        Assert.IsFalse(resources.TryPurchaseRelic(null, 50, out _));
        Assert.IsFalse(resources.TryPurchaseConsumable(null, 50, out _));
        Assert.IsFalse(resources.TryPurchaseEquipment(null, 50, out _));
        Assert.AreEqual(200, resources.Gold); Assert.AreEqual(1, resources.Relics.Count);
    }

    [Test]
    public void InsufficientGoldRejectsAndFreePurchaseWorks()
    {
        var item = EquipmentDatabase.GetEquipment("Longsword");
        run.State.gold = 0;
        Assert.IsFalse(resources.TryPurchaseEquipment(item, 1, out _));
        Assert.IsFalse(resources.TryPurchaseEquipment(item, -1, out _));
        Assert.IsTrue(resources.TryPurchaseEquipment(item, 0, out _));
        Assert.AreEqual(0, resources.Gold); Assert.AreEqual(1, resources.Equipments.Count);
    }

    [Test]
    public void OneCopyCannotOccupyTwoSlotsOrTwoCharacters()
    {
        var first = Crew("A"); var second = Crew("B"); var item = EquipmentDatabase.GetEquipment("Longsword");
        resources.Equipments.Add(item);
        Assert.IsTrue(resources.TryEquip(first, 0, item, out _));
        Assert.IsFalse(resources.TryEquip(first, 1, item, out _));
        Assert.IsFalse(resources.TryEquip(second, 0, item, out _));
        Assert.AreEqual(0, resources.EquipmentLoadouts.AvailableCount(resources.Equipments, item));
        Assert.IsTrue(resources.TryEquip(first, 0, item, out _));
        Assert.AreEqual(1, resources.EquipmentLoadouts.AssignedCount(item));
    }

    [Test]
    public void ExtraCopiesAndReplacementReleaseTheCorrectCounts()
    {
        var first = Crew("A"); var second = Crew("B"); var sword = EquipmentDatabase.GetEquipment("Longsword");
        var staff = EquipmentDatabase.GetEquipment("Staff");
        resources.Equipments.AddRange(new[] { sword, sword, staff });
        Assert.IsTrue(resources.TryEquip(first, 0, sword, out _));
        Assert.IsTrue(resources.TryEquip(second, 0, sword, out _));
        Assert.IsTrue(resources.TryEquip(first, 0, staff, out _));
        Assert.AreEqual(1, resources.EquipmentLoadouts.AvailableCount(resources.Equipments, sword));
        Assert.AreSame(sword, second.GetEquippedItem(0));
        Assert.IsTrue(resources.TryEquip(second, 0, null, out _));
        Assert.AreEqual(2, resources.EquipmentLoadouts.AvailableCount(resources.Equipments, sword));
        Assert.AreEqual(3, resources.Equipments.Count);
    }

    [Test]
    public void BenchingAndRejoiningPreserveAssignmentsAndReservations()
    {
        var status = Crew("A"); var sword = EquipmentDatabase.GetEquipment("Longsword"); resources.Equipments.Add(sword);
        Assert.IsTrue(resources.TryEquip(status, 0, sword, out _));
        Assert.IsTrue(run.RemovePartyMember("A"));
        Assert.AreEqual(0, resources.EquipmentLoadouts.AvailableCount(resources.Equipments, sword));
        run.AddPartyMember(status.origin);
        Assert.AreSame(sword, run.State.partyStatuses.Single().GetEquippedItem(0));
        var newBattleStatus = new CharacterStatus(status.origin);
        resources.EquipmentLoadouts.ApplyTo(newBattleStatus);
        Assert.AreSame(sword, newBattleStatus.GetEquippedItem(0));
    }

    [Test]
    public void UnownedEquipmentAndInvalidSlotsAreRejected()
    {
        var status = Crew("A"); var sword = EquipmentDatabase.GetEquipment("Longsword");
        Assert.IsFalse(resources.TryEquip(status, 0, sword, out _));
        resources.Equipments.Add(sword);
        Assert.IsFalse(resources.TryEquip(status, -1, sword, out _));
        Assert.IsFalse(resources.TryEquip(status, CharacterStatus.EquipmentSlotCount, sword, out _));
        Assert.IsNull(status.GetEquippedItem(0));
    }

    [Test]
    public void EquippedMaterialCannotBeSynthesizedButTwoSpareCopiesCan()
    {
        var status = Crew("A"); var sword = EquipmentDatabase.GetEquipment("Longsword");
        resources.Equipments.AddRange(new[] { sword, sword, sword });
        Assert.IsTrue(resources.TryEquip(status, 0, sword, out _));
        Assert.IsFalse(resources.TrySynthesize(0, 1, out _, out _));
        Assert.AreEqual(200, resources.Gold); Assert.AreEqual(3, resources.Equipments.Count);
        Assert.IsTrue(resources.TrySynthesize(1, 2, out var result, out _));
        Assert.AreEqual(2, result.starLevel); Assert.AreEqual(sword.category, result.category);
        Assert.AreEqual(150, resources.Gold); Assert.AreEqual(2, resources.Equipments.Count);
        Assert.AreSame(sword, status.GetEquippedItem(0));
        Assert.AreEqual(0, resources.EquipmentLoadouts.AvailableCount(resources.Equipments, sword));
    }

    [TestCase("Longsword", "Staff")]
    [TestCase("Longsword", "Greatsword")]
    [TestCase("ColossalSword", "ColossalSword")]
    public void InvalidSynthesisRecipesDoNotConsumeGoldOrMaterials(string first, string second)
    {
        var a = EquipmentDatabase.GetEquipment(first); var b = EquipmentDatabase.GetEquipment(second);
        Assert.NotNull(a); Assert.NotNull(b);
        resources.Equipments.AddRange(new[] { a, b });
        Assert.IsFalse(resources.TrySynthesize(0, 1, out _, out _));
        Assert.AreEqual(200, resources.Gold); CollectionAssert.AreEqual(new[] { a, b }, resources.Equipments);
    }

    [Test]
    public void FailedSynthesisDoesNotRollRandomOrConsumeMaterials()
    {
        var sword = EquipmentDatabase.GetEquipment("Longsword"); resources.Equipments.AddRange(new[] { sword, sword });
        run.State.gold = 49; var before = Random.state;
        Assert.IsFalse(resources.TrySynthesize(0, 1, out _, out _));
        Assert.AreEqual(JsonUtility.ToJson(before), JsonUtility.ToJson(Random.state));
        Assert.IsFalse(resources.TrySynthesize(0, 0, out _, out _));
        Assert.AreEqual(49, resources.Gold); Assert.AreEqual(2, resources.Equipments.Count);
    }

    [Test]
    public void ComparisonDoesNotMutateStatusOrRerollSkillsAndMatchesCommittedStats()
    {
        var status = Crew("A"); var sword = EquipmentDatabase.GetEquipment("Longsword"); var great = EquipmentDatabase.GetEquipment("Greatsword");
        resources.Equipments.AddRange(new[] { sword, great }); resources.TryEquip(status, 0, sword, out _);
        var beforeRandom = Random.state; var skills = status.selectedActiveSkillIndices.ToArray();
        var changes = EquipmentComparison.Changes(status, 0, great);
        Assert.AreSame(sword, status.GetEquippedItem(0));
        CollectionAssert.AreEqual(skills, status.selectedActiveSkillIndices);
        Assert.AreEqual(JsonUtility.ToJson(beforeRandom), JsonUtility.ToJson(Random.state));
        resources.TryEquip(status, 0, great, out _);
        Assert.AreEqual(status.FinalAttack, changes.Single(c => c.Name == "공격").After);
        Assert.AreEqual(status.FinalMaxHp, changes.Single(c => c.Name == "최대 체력").After);
    }
}
