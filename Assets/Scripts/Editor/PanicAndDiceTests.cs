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

    private void RunPanic()
    {
        var routine = (IEnumerator)typeof(BattleManager).GetMethod("ResolvePanicTurnStart", Private).Invoke(_battle, null);
        int guard = 0;
        while (routine.MoveNext()) Assert.Less(++guard, 30);
    }

    [Test] public void MentalZeroRollsOnceAndHealingDoesNotClearPanic()
    {
        var actor = Actor();
        int events = 0; actor.PanicChanged += _ => events++;
        actor.TakeMentalDamage(100);
        var type = actor.status.panic.Type;
        Assert.That((int)type, Is.InRange(1, 6));
        actor.TakeMentalDamage(10);
        actor.ReceiveMentalHeal(100);
        actor.TakeMentalDamage(100);
        Assert.AreEqual(type, actor.status.panic.Type);
        Assert.AreEqual(1, events);
        var enemy = _battle.enemyParty[0];
        enemy.TakeMentalDamage(100);
        Assert.IsFalse(enemy.status.panic.IsActive);
    }

    [TestCase(PanicType.SelfLoathing)] [TestCase(PanicType.Impulse)]
    [TestCase(PanicType.Delirium)] [TestCase(PanicType.ContagiousFear)]
    [TestCase(PanicType.Despair)] [TestCase(PanicType.Compulsion)]
    public void DebugPanicFiresChangeEventAndReplacesTypeWithoutTouchingAssets(PanicType type)
    {
        var actor = Actor();
        actor.status.panic.Begin(PanicType.Compulsion);
        actor.status.panic.RequiredSkill = Skill(actor);
        int changes = 0, depleted = 0;
        actor.PanicChanged += _ => changes++;
        actor.MentalDepleted += _ => depleted++;
        Assert.IsTrue(_battle.DebugApplyPanic(actor, type));
        Assert.AreEqual(type, actor.status.panic.Type);
        Assert.IsNull(actor.status.panic.RequiredSkill);
        Assert.AreEqual(0, actor.status.currentMental);
        Assert.AreEqual(100, actor.status.currentHp);
        Assert.AreEqual(100, actor.status.origin.maxMental);
        Assert.AreEqual(1, changes);
        Assert.AreEqual(1, depleted);
        actor.ReceiveMentalHeal(100);
        Assert.AreEqual(type, actor.status.panic.Type, "Ordinary healing must still preserve panic");
    }

    [Test] public void DebugResetRecoversMentalAndRandomPanicCanBeRepeated()
    {
        var actor = Actor();
        for (int i = 0; i < 12; i++)
        {
            Assert.IsTrue(_battle.DebugRollPanic(actor));
            Assert.That((int)actor.status.panic.Type, Is.InRange(1, 6));
            Assert.IsTrue(_battle.DebugClearPanic(actor));
            actor.SynchronizePanic();
            Assert.IsFalse(actor.status.panic.IsActive);
            Assert.AreEqual(actor.status.FinalMaxMental, actor.status.currentMental);
        }
    }

    [Test] public void DebugPanicRejectsInvalidTargetsAndBusyTurns()
    {
        var actor = Actor();
        Assert.IsFalse(_battle.DebugApplyPanic(null, PanicType.Despair));
        Assert.IsFalse(_battle.DebugApplyPanic(_battle.enemyParty[0], PanicType.Despair));
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
            Assert.IsFalse(_battle.DebugApplyPanic(actor, PanicType.Despair));
            Assert.IsFalse(_battle.DebugClearPanic(actor));
        }
        typeof(BattleManager).GetField("_currentPhase", Private).SetValue(_battle, BattlePhase.PlayerTurn);
        typeof(BattleManager).GetField("_resolvingPanicTurn", Private).SetValue(_battle, true);
        Assert.IsFalse(_battle.DebugRollPanic(actor));
        Assert.AreEqual(100, actor.status.currentMental);
        Assert.IsFalse(actor.status.panic.IsActive);
    }

    [Test] public void DebugPanicDoesNotRepeatAnAlreadyResolvedTurn()
    {
        var actor = Actor();
        Assert.IsTrue(_battle.DebugApplyPanic(actor, PanicType.SelfLoathing));
        RunPanic();
        Assert.AreEqual(90, actor.status.currentHp);
        Assert.IsTrue(_battle.DebugApplyPanic(actor, PanicType.SelfLoathing));
        RunPanic();
        Assert.AreEqual(90, actor.status.currentHp);
        typeof(BattleManager).GetField("_turnCount", Private).SetValue(_battle, 2);
        RunPanic();
        Assert.AreEqual(80, actor.status.currentHp);
    }

    [TestCase(100f, 90f)] [TestCase(5f, 1f)] [TestCase(0.5f, 0.5f)]
    public void SelfLoathingCannotKillOrHeal(float hp, float expected)
    {
        var actor = Actor(); actor.status.currentHp = hp;
        actor.status.panic.Begin(PanicType.SelfLoathing);
        RunPanic();
        Assert.AreEqual(expected, actor.status.currentHp, 0.001);
        RunPanic();
        Assert.AreEqual(expected, actor.status.currentHp, 0.001, "Once per turn only");
    }

    [Test] public void DeliriumHitsOtherAllyUsingPhysicalDamage()
    {
        var actor = Actor(); var victim = Actor();
        actor.status.panic.Begin(PanicType.Delirium);
        RunPanic();
        Assert.AreEqual(91, victim.status.currentHp, 0.001);
        Assert.AreEqual(100, actor.status.currentHp);
    }

    [Test] public void FearUsesMaximumMentalAndDoesNotTriggerNewPanicTurnEffect()
    {
        var actor = Actor(); var victim = Actor();
        actor.status.panic.Begin(PanicType.ContagiousFear);
        victim.status.currentMental = 5;
        victim.PanicChanged += target => target.status.panic.Begin(PanicType.SelfLoathing);
        RunPanic();
        Assert.AreEqual(0, victim.status.currentMental);
        Assert.AreEqual(100, victim.status.currentHp, "New panic waits until next turn");
    }

    [Test] public void DespairBlocksAPAndDiceSkills()
    {
        var actor = Actor(); var skill = Skill(actor);
        actor.status.panic.Begin(PanicType.Despair);
        Assert.IsFalse(_battle.CanUseDiceSkill(actor, skill));
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        Assert.IsFalse(_battle.CanUseDiceSkill(actor, skill));
    }

    [Test] public void CompulsionRequiresChosenSkillAndReleasesWhenUnavailable()
    {
        var actor = Actor(); var first = Skill(actor); var second = Skill(actor);
        actor.status.panic.Begin(PanicType.Compulsion);
        RunPanic();
        var chosen = actor.status.panic.RequiredSkill;
        Assert.IsNotNull(chosen);
        var other = chosen == first ? second : first;
        Assert.IsNull(_battle.GetPanicSkillUnavailableReason(actor, chosen));
        Assert.IsNotNull(_battle.GetPanicSkillUnavailableReason(actor, other));
        actor.status.panic.SkillUsed(chosen);
        Assert.IsNull(_battle.GetPanicSkillUnavailableReason(actor, other));
        actor.status.panic.RequiredSkill = chosen;
        _battle.enemyParty[0].status.currentHp = 0;
        Assert.IsNull(_battle.GetPanicSkillUnavailableReason(actor, other));
    }

    [Test] public void ImpulseExecutesOneValidSkillAndConsumesItsCost()
    {
        var actor = Actor(); Skill(actor);
        actor.status.panic.Begin(PanicType.Impulse);
        RunPanic();
        Assert.Less(_battle.enemyParty[0].status.currentHp, 100);
        Assert.AreEqual(19, _battle.currentAP);
        Assert.AreSame(actor.status.dynamicActiveSkill[0], actor.status.lastUsedSkill);
    }

    [Test] public void HeldThreeHighlightsMaxFourButFiveAndSpentDiceDoNot()
    {
        var actor = Actor(); var skill = Skill(actor, 4);
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var pool = (ActionDicePool)typeof(BattleManager).GetField("_dicePool", Private).GetValue(_battle);
        pool.BeginTurn(10);
        var constructor = typeof(ActionDie).GetConstructor(Private, null, new[] { typeof(int), typeof(int) }, null);
        var die3 = (ActionDie)constructor.Invoke(new object[] { 0, 3 });
        var die5 = (ActionDie)constructor.Invoke(new object[] { 1, 5 });
        var dice = (List<ActionDie>)typeof(ActionDicePool).GetField("dice", Private).GetValue(pool);
        dice[0] = die3; dice[1] = die5;
        Assert.IsTrue(_battle.CanAcceptDieOnSkill(0, actor, skill));
        Assert.IsFalse(_battle.CanAcceptDieOnSkill(1, actor, skill));
        pool.TrySpend(new[] { 0 });
        Assert.IsFalse(_battle.CanAcceptDieOnSkill(0, actor, skill));
        actor.status.panic.Begin(PanicType.Despair);
        Assert.IsFalse(_battle.CanAcceptDieOnSkill(2, actor, skill));
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
        actor.status.panic.Begin(PanicType.Despair);
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

    [Test] public void DiceImpulseConsumesMatchingDieAndMarksSkillUsed()
    {
        var actor = Actor(); var skill = Skill(actor);
        actor.status.panic.Begin(PanicType.Impulse);
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var pool = (ActionDicePool)typeof(BattleManager).GetField("_dicePool", Private).GetValue(_battle);
        pool.BeginTurn(2);
        RunPanic();
        Assert.Less(_battle.enemyParty[0].status.currentHp, 100);
        Assert.AreEqual(1, new List<ActionDie>(pool.Dice).FindAll(d => d.IsSpent).Count);
        Assert.IsFalse(_battle.CanUseDiceSkill(actor, skill));
    }
}
