using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TheLastArk.Battle;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.UI;
using TMPro;
using UnityEngine;

public class OriginPartTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly List<Object> _owned = new List<Object>();
    private object _priorResources, _priorRun, _priorTrain;
    private RunManager _run;
    private TrainManager _train;
    private TrainCar _car;
    private BattleManager _battle;
    private OriginPartBalanceConfig _balance;

    private static FieldInfo StaticField(System.Type type, string name) => type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
    private T Own<T>(T value) where T : Object { _owned.Add(value); return value; }

    [SetUp]
    public void Setup()
    {
        _priorResources = StaticField(typeof(ResourceManager), "instance").GetValue(null);
        _priorRun = StaticField(typeof(RunManager), "_instance").GetValue(null);
        _priorTrain = StaticField(typeof(TrainManager), "instance").GetValue(null);
        var go = Own(new GameObject("OriginPartTest")); go.SetActive(false);
        var resources = go.AddComponent<ResourceManager>();
        _run = go.AddComponent<RunManager>();
        _train = Own(new GameObject("OriginPartTrain")).AddComponent<TrainManager>();
        StaticField(typeof(ResourceManager), "instance").SetValue(null, resources);
        StaticField(typeof(RunManager), "_instance").SetValue(null, _run);
        StaticField(typeof(TrainManager), "instance").SetValue(null, _train);
        _car = _train.nexusCar = new TrainCar("넥서스 칸", TrainCarType.Nexus, 0);
        _balance = _train.OriginPartBalance;
        Assert.IsNotNull(_balance, "Resources/Train/OriginPartBalanceConfig must exist");
        _run.State.gold = 0;
    }

    [TearDown]
    public void Cleanup()
    {
        typeof(BattleManager).GetProperty("Active").SetValue(null, null);
        for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
        _owned.Clear();
        StaticField(typeof(ResourceManager), "instance").SetValue(null, _priorResources);
        StaticField(typeof(RunManager), "_instance").SetValue(null, _priorRun);
        StaticField(typeof(TrainManager), "instance").SetValue(null, _priorTrain);
    }

    private void Unlock(string partId, int level, bool equip = true)
    {
        _car.MarkPartCrafted(partId);
        _car.SetPartLevel(partId, level);
        if (equip) Assert.IsTrue(_car.InstallPart(partId));
    }

    private BattleManager Battle(params int[] rolls)
    {
        _battle = Own(new GameObject("OriginPartBattle")).AddComponent<BattleManager>();
        _battle.enabled = false;
        _battle.playerParty = new List<BattleCharacter>();
        _battle.enemyParty = new List<BattleCharacter>();
        typeof(BattleManager).GetField("_currentPhase", Private).SetValue(_battle, BattlePhase.PlayerTurn);
        int index = 0;
        typeof(BattleManager).GetField("_dicePool", Private).SetValue(_battle, new ActionDicePool(f => rolls[index++ % rolls.Length]));
        return _battle;
    }

    private ActionDicePool Pool => (ActionDicePool)typeof(BattleManager).GetField("_dicePool", Private).GetValue(_battle);
    private void StartTurn() => typeof(BattleManager).GetMethod("ApplyPlayerTurnStartAP", Private).Invoke(_battle, null);

    [Test]
    public void BalanceAssetMatchesDesignTable()
    {
        CollectionAssert.AreEqual(new[] { 2, 4, 6, 8 }, _balance.highEnergyBonus);
        CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 }, _balance.acceleratorDiceReduction);
        CollectionAssert.AreEqual(new[] { 20, 30, 40, 50 }, _balance.acceleratorStatPercent);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, _balance.manipulatorUses);
        CollectionAssert.AreEqual(new[] { 1, 1, 2, 3 }, _balance.chanceUses);
        CollectionAssert.AreEqual(new[] { true, false, false, false }, _balance.chanceRerollsAll);
        CollectionAssert.AreEqual(new[] { 3, 3, 2, 2 }, _balance.parasiticDiceReduction);
        CollectionAssert.AreEqual(new[] { 4, 5, 5, 6 }, _balance.parasiticStackCap);
        var craft = new Dictionary<string, int>
        {
            { TrainPartDatabase.HighEnergyId, 80 }, { TrainPartDatabase.EnergyAcceleratorId, 80 },
            { TrainPartDatabase.NumberManipulatorId, 50 }, { TrainPartDatabase.ChanceProtocolId, 50 },
            { TrainPartDatabase.ParasiticOrganId, 100 },
        };
        foreach (var pair in craft)
        {
            Assert.AreEqual(pair.Value, _balance.CraftCost(pair.Key), pair.Key);
            for (int level = 0; level < OriginPartBalanceConfig.MaxPartLevel; level++)
                Assert.AreEqual(100, _balance.UpgradeCost(pair.Key, level), pair.Key);
            Assert.AreEqual(-1, _balance.UpgradeCost(pair.Key, OriginPartBalanceConfig.MaxPartLevel));
            for (int level = 0; level <= OriginPartBalanceConfig.MaxPartLevel; level++)
                Assert.IsNotEmpty(_balance.Describe(pair.Key, level));
        }
        CollectionAssert.AreEquivalent(craft.Keys, TrainPartDatabase.GetPartsForNexusModule(NexusModuleDatabase.OriginId).ConvertAll(p => p.partId));
        Assert.AreEqual(3, _car.MaxPartSlots);
    }

    [Test]
    public void ParasiticAccumulatesToCap()
    {
        _car.level = 4;
        Unlock(TrainPartDatabase.ParasiticOrganId, 3);
        Battle(1);
        var expected = new[] { 5, 6, 7, 8, 9, 10, 10 };
        foreach (int count in expected)
        {
            StartTurn();
            Assert.AreEqual(count, Pool.Dice.Count);
        }
        Assert.AreEqual(6, _battle.ParasiticStacks, "Accumulation stops at the 3강 cap");
    }

    [Test]
    public void DiceRequestSubtractsAcceleratorAndNeverGoesNegative()
    {
        Unlock(TrainPartDatabase.EnergyAcceleratorId, 2);
        Assert.AreEqual(1, TrainManager.OriginNormalDiceRequest(_car, _balance, 0));
        Unlock(TrainPartDatabase.ParasiticOrganId, 0);
        Assert.AreEqual(0, TrainManager.OriginNormalDiceRequest(_car, _balance, 1));
        Assert.AreEqual(0.40f, _train.GetTrainBonusAttackMultiplier(), 1e-4f);
        Assert.AreEqual(0.40f, _train.GetTrainBonusSpellPowerMultiplier(), 1e-4f);
        Assert.AreEqual(0.40f, _train.GetTrainBonusHpMultiplier(), 1e-4f);
    }

    [Test]
    public void HighEnergyBoostsOneFirstBatchDieOnly()
    {
        Unlock(TrainPartDatabase.HighEnergyId, 3);
        Battle(6, 3, 1);
        StartTurn();
        Assert.AreEqual(2, Pool.Dice.Count);
        Assert.AreEqual(14, Pool.Dice[0].Value, "6 + 8 at 3강");
        Assert.AreEqual(0, Pool.Dice[1].Modifier);

        Assert.AreEqual(1, _battle.RollAdditionalDice(1));
        Assert.AreEqual(0, Pool.Dice[2].Modifier, "Later generated dice are excluded");
        Assert.IsTrue(Pool.TrySpend(new[] { Pool.Dice[0].Id }));
        foreach (ActionDie die in Pool.Dice) Assert.AreEqual(0, die.Modifier, "Bonus does not move after its die is consumed");
    }

    [Test]
    public void ManipulatorAndChanceUsesConsumeOnlyOnValidActions()
    {
        Unlock(TrainPartDatabase.NumberManipulatorId, 1);
        Unlock(TrainPartDatabase.ChanceProtocolId, 0);
        Battle(1, 4, 2, 5);
        StartTurn();
        int low = Pool.Dice[0].Id, high = Pool.Dice[1].Id;
        Assert.AreEqual(2, _battle.ManipulateUsesLeft);
        Assert.IsTrue(_battle.TryManipulateDie(low, -1));
        Assert.AreEqual(0, Pool.Find(low).Value);
        Assert.IsFalse(_battle.TryManipulateDie(low, -1));
        Assert.AreEqual(1, _battle.ManipulateUsesLeft, "Invalid 0 -> -1 consumes no use");
        Assert.IsTrue(_battle.TryManipulateDie(high, 1));
        Assert.IsFalse(_battle.TryManipulateDie(high, 1));
        Assert.AreEqual(5, Pool.Find(high).Value);

        Assert.IsTrue(_battle.RerollTargetsAll);
        Assert.AreEqual(1, _battle.RerollUsesLeft);
        Assert.IsTrue(_battle.TryRerollDice(null));
        Assert.AreEqual(0, _battle.RerollUsesLeft);
        Assert.AreEqual(2, Pool.Dice.Count, "Reroll keeps slots and consumes no dice");
        Assert.AreEqual(1, Pool.Find(high).Modifier, "Kept modifier survives reroll");
        Assert.IsFalse(_battle.TryRerollDice(null));

        StartTurn();
        Assert.AreEqual(2, _battle.ManipulateUsesLeft, "Uses reset at player turn start");
        Assert.AreEqual(1, _battle.RerollUsesLeft);
    }

    [Test]
    public void UpgradedChanceRerollsSelectedDiceAsOneUse()
    {
        Unlock(TrainPartDatabase.ChanceProtocolId, 3);
        Battle(1, 2, 3, 4, 5, 6);
        StartTurn();
        Assert.IsFalse(_battle.RerollTargetsAll);
        Assert.AreEqual(3, _battle.RerollUsesLeft);
        Assert.IsFalse(_battle.TryRerollDice(new int[0]));
        Assert.AreEqual(3, _battle.RerollUsesLeft, "No target consumes no use");
        Assert.IsTrue(_battle.TryRerollDice(new[] { Pool.Dice[0].Id, Pool.Dice[1].Id }));
        Assert.AreEqual(2, _battle.RerollUsesLeft, "Several dice at once count as one use");
    }

    [Test]
    public void CraftAndUpgradeSpendGoldOnceAndFailWithoutChange()
    {
        string id = TrainPartDatabase.HighEnergyId;
        Assert.IsFalse(_car.CanInstallPart(id), "Origin parts start locked");
        _run.State.gold = 60;
        Assert.IsFalse(_train.TryCraftOriginPart(_car, id));
        Assert.AreEqual(60, _run.State.gold);
        Assert.IsFalse(_car.IsPartCrafted(id));

        _run.State.gold = 200;
        Assert.IsTrue(_train.TryCraftOriginPart(_car, id));
        Assert.AreEqual(120, _run.State.gold);
        Assert.IsFalse(_train.TryCraftOriginPart(_car, id), "Already crafted");
        Assert.AreEqual(120, _run.State.gold);
        Assert.IsTrue(_train.TryBuyAndInstallPart(_car, id), "Crafted parts equip for free");
        Assert.AreEqual(120, _run.State.gold);
        Assert.IsFalse(_car.InstallPart(id), "Same part can't be equipped twice");

        Assert.IsTrue(_train.TryUpgradeOriginPart(_car, id));
        Assert.AreEqual(20, _run.State.gold);
        Assert.IsFalse(_train.TryUpgradeOriginPart(_car, id));
        Assert.AreEqual(1, _car.GetPartLevel(id));
        Assert.AreEqual(20, _run.State.gold);
        _run.State.gold = 500;
        Assert.IsTrue(_train.TryUpgradeOriginPart(_car, id));
        Assert.IsTrue(_train.TryUpgradeOriginPart(_car, id));
        Assert.IsFalse(_train.TryUpgradeOriginPart(_car, id), "Max level 3");
        Assert.AreEqual(3, _car.GetPartLevel(id));
        Assert.AreEqual(300, _run.State.gold);
    }

    [Test]
    public void TrainEditsAreBlockedDuringBattle()
    {
        string id = TrainPartDatabase.NumberManipulatorId;
        Unlock(id, 0);
        Battle(1);
        typeof(BattleManager).GetProperty("Active").SetValue(null, _battle);
        _run.State.gold = 1000;
        Assert.IsFalse(_train.TryCraftOriginPart(_car, TrainPartDatabase.HighEnergyId));
        Assert.IsFalse(_train.TryUpgradeOriginPart(_car, id));
        Assert.IsFalse(_train.TryUninstallPart(_car, id));
        Assert.IsFalse(_train.TryUpgradeCar(_car));
        Assert.IsFalse(_train.TryChangeNexusModule(NexusModuleDatabase.GambleId));
        Assert.AreEqual(1000, _run.State.gold);
        Assert.IsTrue(_car.HasPart(id));
        Assert.AreEqual(NexusModuleDatabase.OriginId, _car.installedModuleId);

        typeof(BattleManager).GetField("_currentPhase", Private).SetValue(_battle, BattlePhase.BattleEnd);
        Assert.IsTrue(_train.TryUninstallPart(_car, id), "Allowed once the battle has ended");
    }

    [Test]
    public void UnequipAndModuleSwapKeepStateButNewRunResets()
    {
        string id = TrainPartDatabase.ParasiticOrganId;
        Unlock(id, 2);
        Assert.IsTrue(_train.TryUninstallPart(_car, id));
        Assert.IsTrue(_car.IsPartCrafted(id));
        Assert.AreEqual(2, _car.GetPartLevel(id));
        Assert.IsTrue(_train.TryEquipOriginPart(_car, id));

        Assert.IsTrue(_train.TryChangeNexusModule(NexusModuleDatabase.GambleId));
        Assert.IsFalse(_car.HasPart(id));
        Assert.IsTrue(_train.TryChangeNexusModule(NexusModuleDatabase.OriginId));
        Assert.IsTrue(_car.IsPartCrafted(id), "Module swap keeps craft unlock");
        Assert.AreEqual(2, _car.GetPartLevel(id), "Module swap keeps part level");

        Assert.IsTrue(_train.TryEquipOriginPart(_car, id));
        _train.ResetOriginPartsForNewRun();
        Assert.IsFalse(_car.HasPart(id));
        Assert.IsFalse(_car.IsPartCrafted(id));
        Assert.AreEqual(0, _car.GetPartLevel(id));
    }

    [Test]
    public void DiceTrayShowsActualValueAndModifierBadge()
    {
        Unlock(TrainPartDatabase.HighEnergyId, 3);
        Battle(6, 3, 1);
        StartTurn();
        var canvas = Own(new GameObject("DiceUITestCanvas", typeof(RectTransform), typeof(Canvas)));
        var ui = _battle.gameObject.AddComponent<BattleDiceUI>();
        ui.Initialize(_battle, canvas.transform);
        ui.Refresh(Pool, new List<int>(), null);
        Transform tray = canvas.transform.Find("BattleDicePool");
        Assert.IsNotNull(tray);

        Transform boosted = tray.Find("Die_1");
        Assert.AreEqual("14", boosted.Find("Value").GetComponent<TextMeshProUGUI>().text, "Centre shows the actual value");
        Transform badge = boosted.Find("ModifierBadge");
        Assert.IsTrue(badge.gameObject.activeSelf);
        Assert.AreEqual("+8", badge.Find("Text").GetComponent<TextMeshProUGUI>().text);

        Transform plain = tray.Find("Die_2");
        Assert.AreEqual("", plain.Find("Value").GetComponent<TextMeshProUGUI>().text, "1-6 use the face sprite");
        Assert.IsFalse(plain.Find("ModifierBadge").gameObject.activeSelf, "Badge hidden when the modifier is 0");
        Object.DestroyImmediate(ui);
    }
}
