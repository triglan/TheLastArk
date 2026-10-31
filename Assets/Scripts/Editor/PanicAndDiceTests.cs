using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TheLastArk.Battle;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.Skills;
using TheLastArk.UI;
using UnityEngine;

public class PanicAndDiceTests
{
    private readonly List<Object> _owned = new List<Object>();
    private BattleManager _battle;
    private TrainManager _train;
    private object _previousTrain;
    private object _previousResources;
    private Random.State _random;
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private T Own<T>(T item) where T : Object { _owned.Add(item); return item; }

    [SetUp] public void Setup()
    {
        _random = Random.state;
        var resourcesField = typeof(ResourceManager).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
        _previousResources = resourcesField.GetValue(null);
        resourcesField.SetValue(null, Own(new GameObject("PanicTestResources")).AddComponent<ResourceManager>());
        var field = typeof(TrainManager).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
        _previousTrain = field.GetValue(null);
        field.SetValue(null, null);
        _train = Own(new GameObject("PanicTestTrain")).AddComponent<TrainManager>();
        field.SetValue(null, _train);
        _train.nexusCar = new TrainCar("Test", TrainCarType.Nexus, 0) { installedModuleId = NexusModuleDatabase.GambleId };
        _battle = Own(new GameObject("PanicTestBattle")).AddComponent<BattleManager>();
        _battle.enabled = false;
        _battle.playerParty = new List<BattleCharacter>();
        _battle.enemyParty = new List<BattleCharacter>();
        typeof(BattleManager).GetField("_currentPhase", Private).SetValue(_battle, BattlePhase.PlayerTurn);
        _battle.currentAP = 20;
        Actor(true);
    }

    [TearDown] public void Cleanup()
    {
        for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
        _owned.Clear();
        typeof(TrainManager).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, _previousTrain);
        typeof(ResourceManager).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, _previousResources);
        Random.state = _random;
    }

    private BattleCharacter Actor(bool enemy = false)
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.isEnemy = enemy; data.maxHp = 100; data.maxMental = 100;
        data.baseAttack = 20; data.spellPower = 10; data.armor = 0; data.magicResist = 0;
        var actor = Own(new GameObject("PanicTestActor")).AddComponent<BattleCharacter>();
        actor.status = new CharacterStatus(data);
        (enemy ? _battle.enemyParty : _battle.playerParty).Add(actor);
        return actor;
    }

    private SkillInfo Skill(BattleCharacter actor, int maxDie = 6)
    {
        var skill = new SkillInfo { skillName = "테스트 스킬", baseCost = 1, levels = new[] {
            new SkillLevelData { targetType = TargetType.SingleEnemy,
                diceInput = new DiceInputRule { condition = DiceCondition.AtMost, value = maxDie },
                effects = new List<EffectEntry> { new EffectEntry { type = EffectType.Damage, multiplier = 1f } } } } };
        actor.status.dynamicActiveSkill.Add(skill);
        return skill;
    }

    /// <summary>Replaces the battle's dice pool with a deterministic one.</summary>
    private ActionDicePool Pool(params int[] rolls)
    {
        int index = 0;
        var pool = new ActionDicePool(faces => rolls[index++ % rolls.Length]);
        typeof(BattleManager).GetField("_dicePool", Private).SetValue(_battle, pool);
        return pool;
    }

    private SkillInfo ChargeSkill(BattleCharacter actor, int target, bool byCount = false)
    {
        var skill = new SkillInfo { skillName = "충전 스킬", baseCost = 1, levels = new[] {
            new SkillLevelData { targetType = TargetType.SingleEnemy,
                diceInput = new DiceInputRule { condition = DiceCondition.Any, chargeTarget = target, chargeByDiceCount = byCount },
                effects = new List<EffectEntry> { new EffectEntry { type = EffectType.Damage, multiplier = 1f } } } } };
        actor.status.dynamicActiveSkill.Add(skill);
        return skill;
    }

    private void Select(params int[] ids)
    {
        var selected = (List<int>)typeof(BattleManager).GetField("_selectedDiceIds", Private).GetValue(_battle);
        selected.Clear();
        selected.AddRange(ids);
    }

    private void Fire(BattleCharacter actor, SkillInfo skill, BattleCharacter target)
    {
        var selection = (BattleSelectionState)typeof(BattleManager).GetField("_selection", Private).GetValue(_battle);
        selection.Set(skill, actor);
        typeof(BattleManager).GetMethod("PerformSkillOnTarget", Private).Invoke(_battle, new object[] { target });
    }

    [TestCase(PanicType.Hallucination)] [TestCase(PanicType.Depression)]
    [TestCase(PanicType.Compulsion)] [TestCase(PanicType.Lethargy)]
    [TestCase(PanicType.Seizure)] [TestCase(PanicType.Agitation)]
    public void DebugPanicFiresChangeEventWithoutTouchingAssets(PanicType type)
    {
        var actor = Actor();
        int changes = 0, depleted = 0;
        actor.PanicChanged += _ => changes++;
        actor.MentalDepleted += _ => depleted++;
        Assert.IsTrue(_battle.DebugApplyPanic(actor, type));
        Assert.AreEqual(type, actor.status.panic.Type);
        Assert.IsNull(actor.status.panic.LockedSkill);
        Assert.AreEqual(0, actor.status.currentMental);
        Assert.AreEqual(100, actor.status.currentHp);
        Assert.AreEqual(100, actor.status.origin.maxMental);
        Assert.AreEqual(1, changes);
        Assert.AreEqual(1, depleted);
        actor.ReceiveMentalHeal(1);
        Assert.IsFalse(actor.status.panic.IsActive, "Real healing above 0 ends the panic");
    }

    [Test] public void DebugResetRecoversMentalAndRandomPanicCanBeRepeated()
    {
        var actor = Actor();
        for (int i = 0; i < 12; i++)
        {
            Assert.IsTrue(_battle.DebugRollPanic(actor));
            Assert.That((int)actor.status.panic.Type, Is.InRange(1, PanicState.TypeCount));
            Assert.IsTrue(_battle.DebugClearPanic(actor));
            actor.SynchronizePanic();
            Assert.IsFalse(actor.status.panic.IsActive);
            Assert.AreEqual(actor.status.FinalMaxMental, actor.status.currentMental);
        }
    }

    [Test] public void DebugPanicRejectsInvalidTargetsAndBusyTurns()
    {
        var actor = Actor();
        Assert.IsFalse(_battle.DebugApplyPanic(null, PanicType.Lethargy));
        Assert.IsFalse(_battle.DebugApplyPanic(_battle.enemyParty[0], PanicType.Lethargy));
        Assert.IsFalse(_battle.DebugApplyPanic(actor, PanicType.None));
        Assert.IsFalse(_battle.DebugApplyPanic(actor, (PanicType)999));
        actor.status.currentHp = 0;
        Assert.IsFalse(_battle.DebugRollPanic(actor));
        Assert.IsFalse(_battle.DebugClearPanic(actor));
        actor.status.currentHp = 100;
        actor.gameObject.SetActive(false);
        Assert.IsFalse(_battle.DebugRollPanic(actor));
        actor.gameObject.SetActive(true);
        _battle.playerParty.Remove(actor);
        Assert.IsFalse(_battle.DebugRollPanic(actor));
        _battle.playerParty.Add(actor);
        foreach (var phase in new[] { BattlePhase.EnemyTurn, BattlePhase.BattleEnd })
        {
            typeof(BattleManager).GetField("_currentPhase", Private).SetValue(_battle, phase);
            Assert.IsFalse(_battle.DebugApplyPanic(actor, PanicType.Lethargy));
            Assert.IsFalse(_battle.DebugClearPanic(actor));
        }
        typeof(BattleManager).GetField("_currentPhase", Private).SetValue(_battle, BattlePhase.PlayerTurn);
        Assert.AreEqual(100, actor.status.currentMental);
        Assert.IsFalse(actor.status.panic.IsActive);
    }

    [Test] public void HeldThreeHighlightsMaxFourButFiveAndSpentDiceDoNot()
    {
        var actor = Actor(); var skill = Skill(actor, 4);
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var pool = Pool(3, 5, 1);
        pool.BeginTurn(10);
        Assert.IsTrue(_battle.CanAcceptDieOnSkill(0, actor, skill));
        Assert.IsFalse(_battle.CanAcceptDieOnSkill(1, actor, skill));
        pool.TrySpend(new[] { 0 });
        Assert.IsFalse(_battle.CanAcceptDieOnSkill(0, actor, skill));
        actor.status.panic.Begin(PanicType.Lethargy);
        Assert.IsFalse(_battle.CanAcceptDieOnSkill(2, actor, skill), "무기력 blocks attack skills");
    }

    [Test] public void ManagementPanelKeepsLeftHalfVisible()
    {
        var canvas = Own(new GameObject("TestCanvas", typeof(RectTransform)));
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
        var panel = Own(new GameObject("Panel", typeof(RectTransform))).GetComponent<RectTransform>();
        panel.SetParent(canvas.transform, false);
        ManagementPanelLayout.DockRight(panel);
        var corners = new Vector3[4]; panel.GetWorldCorners(corners);
        Assert.GreaterOrEqual(corners[0].x, 0, "Canvas center is the screen midpoint");
        Assert.Greater(panel.rect.width, 800);
        Assert.Less(corners[2].x, 960);
    }

    [Test] public void BattleCleanupClearsPanicWithoutRefreshingDestroyedUI()
    {
        var actor = Actor();
        actor.status.panic.Begin(PanicType.Lethargy);
        var actors = (List<BattleCharacter>)typeof(BattleManager).GetField("_panicActors", Private).GetValue(_battle);
        actors.Add(actor);
        typeof(BattleManager).GetMethod("ClearBattlePanic", Private).Invoke(_battle, new object[] { false });
        Assert.IsFalse(actor.status.panic.IsActive);
    }

    [Test] public void PresentationIsRootCanvasEvenWhenManagerIsInsideAnotherCanvas()
    {
        var parent = Own(new GameObject("BattleCanvas", typeof(RectTransform), typeof(Canvas)));
        _battle.transform.SetParent(parent.transform, false);
        var ui = _battle.gameObject.AddComponent<PanicPresentationUI>();
        typeof(PanicPresentationUI).GetMethod("EnsureUI", Private).Invoke(ui, null);
        var group = (CanvasGroup)typeof(PanicPresentationUI).GetField("_group", Private).GetValue(ui);
        Own(group.gameObject);
        Assert.IsTrue(group.GetComponent<Canvas>().isRootCanvas);
        Assert.IsNull(group.transform.parent);
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, group.GetComponent<Canvas>().renderMode);
    }

    [Test] public void ChargeBecomesReadyAndFiresManuallyWithoutDice()
    {
        var actor = Actor(); var skill = ChargeSkill(actor, 5);
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var pool = Pool(3, 4, 6, 2);
        pool.BeginTurn(4);

        Select(0);
        _battle.SelectSkill(skill, actor);
        Assert.AreEqual(3, _battle.GetDiceCharge(actor, skill));
        Assert.AreEqual(3, pool.Dice.Count, "Inserted dice are consumed");
        Assert.IsTrue(_battle.CanUseDiceSkill(actor, skill), "Charging alone is not a use");

        Select(1);
        _battle.SelectSkill(skill, actor);
        Assert.AreEqual(5, _battle.GetDiceCharge(actor, skill), "Overflow above the target is discarded");
        Assert.IsTrue(_battle.IsChargeReady(actor, skill));
        Assert.AreEqual(100f, _battle.enemyParty[0].status.currentHp, "Reaching the target never auto-fires");

        Select(2);
        _battle.SelectSkill(skill, actor);
        Assert.AreEqual(2, pool.Dice.Count, "A ready charge accepts no more dice");
        Assert.IsFalse(_battle.CanAcceptDieOnSkill(2, actor, skill));

        Fire(actor, skill, _battle.enemyParty[0]);
        Assert.Less(_battle.enemyParty[0].status.currentHp, 100f);
        Assert.AreEqual(2, pool.Dice.Count, "Firing consumes no dice");
        Assert.AreEqual(0, _battle.GetDiceCharge(actor, skill));
        Assert.IsFalse(_battle.CanUseDiceSkill(actor, skill), "No recharge or refire after firing this turn");
        Select(2);
        _battle.SelectSkill(skill, actor);
        Assert.AreEqual(0, _battle.GetDiceCharge(actor, skill));
        Assert.AreEqual(2, pool.Dice.Count);
    }

    [Test] public void ChargePersistsAcrossTurnsAndCountTypeCountsZero()
    {
        var actor = Actor(); var skill = ChargeSkill(actor, 3, true);
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var pool = Pool(1);
        pool.BeginTurn(2);
        pool.AddModifier(0, -5);
        Assert.AreEqual(0, pool.Find(0).Value);
        Select(0);
        _battle.SelectSkill(skill, actor);
        Assert.AreEqual(1, _battle.GetDiceCharge(actor, skill), "A 0 die counts as 1 and is consumed");
        Assert.IsNull(pool.Find(0));

        typeof(BattleManager).GetMethod("ApplyPlayerTurnStartAP", Private).Invoke(_battle, null);
        Assert.AreEqual(1, _battle.GetDiceCharge(actor, skill), "Charge progress carries to the next turn");
        Assert.AreEqual(2, pool.Dice.Count, "Turn start rolls origin level + 2 dice");
    }

    [Test] public void AutoEndWaitsForReadyChargeAndMismatchedDice()
    {
        bool previous = BattleManager.AutoEndDiceTurn;
        try
        {
            var actor = Actor(); var skill = ChargeSkill(actor, 1);
            var normal = Skill(actor, 2);
            _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
            var pool = Pool(1, 6);
            pool.BeginTurn(2);
            PlayerPrefs.SetInt("Battle.AutoEndDiceTurn", 1);
            Select(0);
            _battle.SelectSkill(skill, actor);
            Assert.IsTrue(_battle.IsChargeReady(actor, skill));
            Assert.AreEqual(1, pool.Dice.Count);
            Assert.IsFalse(normal.levels[0].diceInput.AcceptsValue(pool.Dice[0].Value));
            Assert.IsTrue(_battle.IsPlayerTurn, "A remaining die that fails conditions does not auto-end");

            pool.TrySpend(new[] { pool.Dice[0].Id });
            typeof(BattleManager).GetMethod("TryAutoEndDiceTurn", Private).Invoke(_battle, null);
            Assert.IsTrue(_battle.IsPlayerTurn, "A fireable ready charge blocks auto end");
        }
        finally { PlayerPrefs.SetInt("Battle.AutoEndDiceTurn", previous ? 1 : 0); }
    }

    [Test] public void FrostReducesInputAndSumChargeWithoutDoubleSubtraction()
    {
        var actor = Actor();
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var skill = ChargeSkill(actor, 20);
        skill.levels[0].diceInput.condition = DiceCondition.AtLeast;
        skill.levels[0].diceInput.value = 4;
        var pool = Pool(6, 5, 1);
        pool.BeginTurn(3);
        actor.status.ApplyStatusEffect(EffectType.Frost, 2f, 2);
        actor.status.ApplyStatusEffect(EffectType.Frost, 1f, 2);

        Assert.AreEqual(2, _battle.GetFrost(actor), "Max-type");
        Assert.IsTrue(_battle.TryGetFrostPreview(1, actor, out int from, out int to));
        Assert.AreEqual(5, from); Assert.AreEqual(3, to);
        Assert.AreEqual(5, pool.Find(1).Value, "Storage keeps the original value");
        Assert.IsTrue(_battle.CanAcceptDieOnSkill(0, actor, skill), "6 - 2 = 4 meets 4 이상");
        Assert.IsFalse(_battle.CanAcceptDieOnSkill(1, actor, skill), "5 - 2 = 3 fails");
        Assert.AreEqual(1, _battle.GetDieInputValue(actor, pool.Find(2)), "Never below 1 for a non-zero die");

        Select(0);
        _battle.SelectSkill(skill, actor);
        Assert.AreEqual(4, _battle.GetDiceCharge(actor, skill), "Sum charge adds the frosted effect value once");

        pool.AddModifier(2, -1);
        var zero = pool.Find(2);
        Assert.AreEqual(0, _battle.GetDieInputValue(actor, zero));
        Assert.AreEqual(0, _battle.GetDieEffectValue(actor, zero, 0));
        Assert.AreEqual(1, _battle.GetDieEffectValue(actor, zero, 3), "0 + 3 - 2");
        Assert.AreEqual(4, _battle.GetDieEffectValue(actor, zero, 0, true), "Max-value uses faces: 6 - 2");
        pool.AddModifier(2, 1);
        Assert.AreEqual(1, _battle.GetDieEffectValue(actor, pool.Find(2), 2), "1 & 냉기2 & +2 -> 1");
    }

    [Test] public void FrostDoesNotTouchCountCharge()
    {
        var actor = Actor();
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var skill = ChargeSkill(actor, 3, true);
        var pool = Pool(6);
        pool.BeginTurn(1);
        actor.status.ApplyStatusEffect(EffectType.Frost, 5f, 2);
        Select(0);
        _battle.SelectSkill(skill, actor);
        Assert.AreEqual(1, _battle.GetDiceCharge(actor, skill));
    }

    [Test] public void ChargeInsertSkipsBleedAndPressureButFireTriggersThemOnce()
    {
        var actor = Actor();
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var skill = ChargeSkill(actor, 1);
        var pool = Pool(3);
        pool.BeginTurn(1);
        actor.status.ApplyStatusEffect(EffectType.Bleed, 4f, 3);
        actor.status.ApplyStatusEffect(EffectType.Pressure, 3f, 3);
        Select(0);
        _battle.SelectSkill(skill, actor);
        Assert.IsTrue(_battle.IsChargeReady(actor, skill));
        Assert.AreEqual(100f, actor.status.currentHp, "Insert does not trigger 출혈");
        Assert.AreEqual(100f, actor.status.currentMental, "Insert does not trigger 압박");

        Fire(actor, skill, _battle.enemyParty[0]);
        Assert.AreEqual(98f, actor.status.currentHp, "Fire: extra bleed 4/2");
        Assert.AreEqual(97f, actor.status.currentMental, "Fire: pressure 3");
        Assert.AreSame(skill, actor.status.lastUsedSkill, "Only an actual fire changes the last fired skill");
    }

    [Test] public void BlockadeAndFatigueBlockInsertAndReadyFireButKeepCharge()
    {
        var actor = Actor();
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var skill = ChargeSkill(actor, 4);
        var pool = Pool(3, 3);
        pool.BeginTurn(2);
        Select(0);
        _battle.SelectSkill(skill, actor);
        Assert.AreEqual(3, _battle.GetDiceCharge(actor, skill));
        Assert.IsNull(actor.status.lastUsedSkill, "Insert does not set the last fired skill");

        actor.status.ApplyStatusEffect(EffectType.Blockade, 0f, 1, 0f, 0, 0, null);
        Assert.IsFalse(_battle.CanAcceptDieOnSkill(1, actor, skill));
        Select(1);
        _battle.SelectSkill(skill, actor);
        Assert.AreEqual(3, _battle.GetDiceCharge(actor, skill), "Blocked: no insert, charge kept");
        Assert.AreEqual(1, pool.Dice.Count);

        actor.status.RemoveAllStatusEffects();
        actor.status.ApplyStatusEffect(EffectType.Stun, 0f, 1);
        Assert.AreEqual("기절", _battle.GetDiceSkillUnavailableReason(actor, skill));
        Assert.AreEqual(3, _battle.GetDiceCharge(actor, skill));
    }
}
