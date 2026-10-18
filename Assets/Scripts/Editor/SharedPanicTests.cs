using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TheLastArk.Battle;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.Skills;
using UnityEngine;

/// <summary>Stage 6: the six shared panics for players and enemies.</summary>
public class SharedPanicTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private readonly List<Object> _owned = new List<Object>();
    private object _priorTrain, _priorResources;
    private BattleManager _battle;
    private ResourceManager _resources;
    private Random.State _random;

    private T Own<T>(T item) where T : Object { _owned.Add(item); return item; }
    private static FieldInfo StaticField(System.Type type, string name) => type.GetField(name, PrivateStatic);

    [SetUp]
    public void Setup()
    {
        _random = Random.state;
        AttackContext.ResetAll();
        _priorResources = StaticField(typeof(ResourceManager), "instance").GetValue(null);
        _resources = Own(new GameObject("PanicResources")).AddComponent<ResourceManager>();
        StaticField(typeof(ResourceManager), "instance").SetValue(null, _resources);
        _priorTrain = StaticField(typeof(TrainManager), "instance").GetValue(null);
        var train = Own(new GameObject("PanicTrain")).AddComponent<TrainManager>();
        StaticField(typeof(TrainManager), "instance").SetValue(null, train);
        train.nexusCar = new TrainCar("넥서스 칸", TrainCarType.Nexus, 0);
        _battle = Own(new GameObject("PanicBattle")).AddComponent<BattleManager>();
        _battle.playerParty = new List<BattleCharacter>();
        _battle.enemyParty = new List<BattleCharacter>();
        typeof(BattleManager).GetField("_currentPhase", Private).SetValue(_battle, BattlePhase.PlayerTurn);
    }

    [TearDown]
    public void Cleanup()
    {
        AttackContext.ResetAll();
        EffectEngine.CurrentCast = null;
        for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
        _owned.Clear();
        StaticField(typeof(TrainManager), "instance").SetValue(null, _priorTrain);
        StaticField(typeof(ResourceManager), "instance").SetValue(null, _priorResources);
        Random.state = _random;
    }

    private BattleCharacter Unit(bool enemy, float hp = 100f, float attack = 10f)
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.isEnemy = enemy; data.maxHp = hp; data.maxMental = 100f; data.baseAttack = attack; data.armor = 0f; data.critRate = 0f;
        var unit = Own(new GameObject(enemy ? "Enemy" : "Ally")).AddComponent<BattleCharacter>();
        unit.status = new CharacterStatus(data);
        (enemy ? _battle.enemyParty : _battle.playerParty).Add(unit);
        return unit;
    }

    private static void Panic(BattleCharacter unit, PanicType type)
    {
        unit.status.currentMental = 0f;
        unit.status.panic.Begin(type);
    }

    private SkillInfo Skill(BattleCharacter actor, bool attack = true, int charge = 0, bool byCount = false)
    {
        var effect = attack
            ? new EffectEntry { type = EffectType.Damage, damageType = DamageType.Physical, multiplier = 0f, fixedValue = 5f }
            : new EffectEntry { type = EffectType.Strength, value = 1f, duration = 1 };
        var skill = new SkillInfo { skillName = "패닉 테스트 " + actor.status.dynamicActiveSkill.Count, levels = new[] {
            new SkillLevelData { targetType = attack ? TargetType.SingleEnemy : TargetType.Self,
                diceInput = new DiceInputRule { condition = DiceCondition.Any, chargeTarget = charge, chargeByDiceCount = byCount },
                effects = new List<EffectEntry> { effect } } } };
        actor.status.dynamicActiveSkill.Add(skill);
        return skill;
    }

    private ActionDicePool Dice(params int[] values)
    {
        ((TrainManager)StaticField(typeof(TrainManager), "instance").GetValue(null)).nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        int index = 0;
        var pool = new ActionDicePool(f => values[index++ % values.Length]);
        typeof(BattleManager).GetField("_dicePool", Private).SetValue(_battle, pool);
        pool.BeginTurn(values.Length);
        return pool;
    }

    private void Select(params int[] ids)
    {
        var selected = (List<int>)typeof(BattleManager).GetField("_selectedDiceIds", Private).GetValue(_battle);
        selected.Clear(); selected.AddRange(ids);
    }

    private void Fire(BattleCharacter actor, SkillInfo skill, BattleCharacter target, params int[] ids)
    {
        Select(ids);
        ((BattleSelectionState)typeof(BattleManager).GetField("_selection", Private).GetValue(_battle)).Set(skill, actor);
        typeof(BattleManager).GetMethod("PerformSkillOnTarget", Private).Invoke(_battle, new object[] { target });
    }

    // ── common rules ────────────────────────────────────────────────────────

    [Test]
    public void RollIsUniformWithFixedSeed()
    {
        Random.InitState(1234);
        var counts = new int[PanicState.TypeCount + 1];
        for (int i = 0; i < 6000; i++) counts[(int)PanicState.Roll()]++;
        Assert.AreEqual(0, counts[0]);
        for (int t = 1; t <= PanicState.TypeCount; t++) Assert.That(counts[t], Is.InRange(850, 1150), ((PanicType)t).ToString());
    }

    [Test]
    public void ZeroSanityStartsHealingEndsAndItCanRecur()
    {
        var ally = Unit(false);
        int changes = 0; ally.PanicChanged += _ => changes++;
        ally.TakeMentalDamage(100f);
        Assert.IsTrue(ally.status.panic.IsActive);
        var type = ally.status.panic.Type;
        ally.TakeMentalDamage(10f);
        Assert.AreEqual(type, ally.status.panic.Type, "No stacking or reroll");
        Assert.AreEqual(1, changes);
        ally.ReceiveMentalHeal(1f);
        Assert.IsFalse(ally.status.panic.IsActive, "Real healing above 0 ends it");
        ally.TakeMentalDamage(100f);
        Assert.IsTrue(ally.status.panic.IsActive, "Can happen again in the same battle");
        Assert.AreEqual(3, changes);

        var enemy = Unit(true);
        enemy.TakeMentalDamage(100f);
        Assert.IsTrue(enemy.status.panic.IsActive, "Enemies panic too");
    }

    [Test]
    public void ShatterScrollPanicsEnemies()
    {
        var relic = Own(ScriptableObject.CreateInstance<RelicData>());
        relic.relicID = "Shatter"; relic.effectType = RelicEffectType.ShatterScroll; relic.effectValue = 0.1f;
        _resources.Relics.Add(relic);
        var enemy = Unit(true);
        enemy.TakeMentalDamage(91f, true);
        Assert.AreEqual(0f, enemy.status.currentMental);
        Assert.IsTrue(enemy.status.panic.IsActive);
    }

    // ── 환각 ────────────────────────────────────────────────────────────────

    [Test]
    public void HallucinationPoolIncludesSelfAndBothSides()
    {
        var actor = Unit(false); var ally = Unit(false); var enemy = Unit(true);
        var dead = Unit(true); dead.status.currentHp = 0f;
        var seen = new HashSet<BattleCharacter>();
        Random.InitState(7);
        for (int i = 0; i < 200; i++) seen.Add(_battle.HallucinationTarget(actor));
        CollectionAssert.AreEquivalent(new[] { actor, ally, enemy }, seen);
    }

    [Test]
    public void HallucinationOnlyForSingleTargetAttacks()
    {
        var actor = Unit(false);
        Panic(actor, PanicType.Hallucination);
        var method = typeof(BattleManager).GetMethod("IsHallucinatingAttack", PrivateStatic);
        var single = Skill(actor).levels[0];
        var all = new SkillLevelData { targetType = TargetType.AllEnemy, effects = single.effects };
        var support = Skill(actor, attack: false).levels[0];
        Assert.IsTrue((bool)method.Invoke(null, new object[] { actor, single }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { actor, all }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { actor, support }));
    }

    [Test]
    public void HallucinatedHitIgnoresTauntButProtectStillApplies()
    {
        var actor = Unit(false); var ally = Unit(false); var protector = Unit(false);
        var taunter = Unit(true); var other = Unit(true);
        taunter.status.ApplyStatusEffect(EffectType.Taunt, 0f, 0, 0f, 2, -1, taunter);
        var level = new SkillLevelData { targetType = TargetType.SingleEnemy, effects = new List<EffectEntry> {
            new EffectEntry { type = EffectType.Damage, damageType = DamageType.True, multiplier = 0f, fixedValue = 5f } } };
        EffectEngine.CurrentCast = new SkillCast { IgnoreTaunt = true };
        EffectEngine.ProcessSkill(actor, new List<BattleCharacter> { other }, level, other, "환각");
        Assert.AreEqual(95f, other.status.currentHp, "The hallucinated target is final");
        Assert.AreEqual(100f, taunter.status.currentHp);
        Assert.AreEqual(2, taunter.status.GetStatus(EffectType.Taunt).remainingCharges);

        ally.status.ApplyStatusEffect(EffectType.Guard, 0f, 0, 0f, 1, -1, protector);
        EffectEngine.ProcessSkill(actor, new List<BattleCharacter> { ally }, level, ally, "환각");
        Assert.AreEqual(100f, ally.status.currentHp);
        Assert.AreEqual(95f, protector.status.currentHp, "An ally target's protector takes the hit");
    }

    // ── 우울 ────────────────────────────────────────────────────────────────

    [Test]
    public void DepressionHalvesEffectValueAfterBonusAndFrost()
    {
        var actor = Unit(false); Unit(true);
        var pool = Dice(0, 1, 6, 6);
        Panic(actor, PanicType.Depression);
        Assert.AreEqual(0, _battle.GetDieEffectValue(actor, pool.Dice[0], 0), "0 stays 0");
        Assert.AreEqual(3, _battle.GetDieEffectValue(actor, pool.Dice[2], 0), "6 → 3");
        Assert.AreEqual(4, _battle.GetDieEffectValue(actor, pool.Dice[2], 2), "(6 + 2) / 2");
        actor.status.ApplyStatusEffect(EffectType.Frost, 2f, 2);
        Assert.AreEqual(0, _battle.GetDieEffectValue(actor, pool.Dice[1], 0), "1 − 냉기 2 → 0: 냉기 floor ignored");
        Assert.AreEqual(1, _battle.GetDieInputValue(actor, pool.Dice[1]), "Input uses the normal (냉기) value");
        StringAssert.Contains("우울", _battle.GetDiePreviewText(pool.Dice[2].Id, actor));
    }

    [Test]
    public void DepressionHalvesSumChargeButNotCountCharge()
    {
        var actor = Unit(false); Unit(true);
        var sum = Skill(actor, charge: 20);
        var count = Skill(actor, charge: 5, byCount: true);
        var pool = Dice(6, 6);
        Panic(actor, PanicType.Depression);
        Select(pool.Dice[0].Id); _battle.SelectSkill(sum, actor);
        Assert.AreEqual(3, _battle.GetDiceCharge(actor, sum));
        Select(pool.Dice[0].Id); _battle.SelectSkill(count, actor);
        Assert.AreEqual(1, _battle.GetDiceCharge(actor, count));
    }

    [Test]
    public void DepressedEnemyDealsHalfDamage()
    {
        var enemy = Unit(true); var ally = Unit(false);
        Panic(enemy, PanicType.Depression);
        var level = new SkillLevelData { targetType = TargetType.SingleEnemy, effects = new List<EffectEntry> {
            new EffectEntry { type = EffectType.Damage, damageType = DamageType.True, multiplier = 0f, fixedValue = 10f } } };
        EffectEngine.ProcessSkill(enemy, new List<BattleCharacter> { ally }, level);
        Assert.AreEqual(95f, ally.status.currentHp);
    }

    // ── 강박 ────────────────────────────────────────────────────────────────

    [Test]
    public void CompulsionLocksFirstFiredSkillNotChargeInsert()
    {
        var actor = Unit(false); var enemy = Unit(true, 1000f);
        var charge = Skill(actor, charge: 20);
        var first = Skill(actor);
        var other = Skill(actor);
        var pool = Dice(3, 3, 3);
        Panic(actor, PanicType.Compulsion);
        Select(pool.Dice[0].Id); _battle.SelectSkill(charge, actor);
        Assert.IsNull(actor.status.panic.LockedSkill, "Charge insertion doesn't lock");
        Fire(actor, first, enemy, pool.Dice[0].Id);
        Assert.AreSame(first, actor.status.panic.LockedSkill);
        StringAssert.Contains("강박", _battle.GetDiceSkillUnavailableReason(actor, other));
        StringAssert.Contains("강박", _battle.GetDiceSkillUnavailableReason(actor, charge));
        Assert.AreEqual(BattleManager.DiceReasonUsed, _battle.GetDiceSkillUnavailableReason(actor, first), "Per-turn limit still applies");
        actor.ReceiveMentalHeal(5f);
        Assert.IsNull(_battle.GetPanicSkillUnavailableReason(actor, other), "Healing releases the lock");
    }

    // ── 무기력 ──────────────────────────────────────────────────────────────

    [Test]
    public void LethargyBlocksAttacksKeepsReadyCharge()
    {
        var actor = Unit(false); Unit(true);
        var attack = Skill(actor);
        var support = Skill(actor, attack: false);
        var charge = Skill(actor, charge: 3);
        var pool = Dice(6);
        Select(pool.Dice[0].Id); _battle.SelectSkill(charge, actor);
        Assert.IsTrue(_battle.IsChargeReady(actor, charge));
        Panic(actor, PanicType.Lethargy);
        StringAssert.Contains("무기력", _battle.GetDiceSkillUnavailableReason(actor, attack));
        StringAssert.Contains("무기력", _battle.GetDiceSkillUnavailableReason(actor, charge), "Ready attack charge can't fire");
        Assert.AreEqual(3, _battle.GetDiceCharge(actor, charge), "Charge kept");
        Assert.IsNull(_battle.GetPanicSkillUnavailableReason(actor, support));
    }

    // ── 발작 / 동요 ─────────────────────────────────────────────────────────

    [Test]
    public void SeizureDrainsOthersAndChainsWithoutReroll()
    {
        var seizing = Unit(false); var normal = Unit(false); var panicked = Unit(false); var fragile = Unit(false);
        Panic(seizing, PanicType.Seizure);
        Panic(panicked, PanicType.Agitation);
        fragile.status.currentMental = 5f;
        typeof(BattleManager).GetMethod("ApplySeizure", PrivateStatic).Invoke(null, new object[] { _battle.playerParty });
        Assert.AreEqual(95f, normal.status.currentMental);
        Assert.AreEqual(0f, seizing.status.currentMental, "Not itself");
        Assert.AreEqual(PanicType.Agitation, panicked.status.panic.Type, "Already panicked: no reroll");
        Assert.AreEqual(0f, fragile.status.currentMental);
        Assert.IsTrue(fragile.status.panic.IsActive, "Hitting 0 starts a new panic");
    }

    [Test]
    public void EnemySeizureDrainsOtherEnemies()
    {
        var seizing = Unit(true); var other = Unit(true); var ally = Unit(false);
        Panic(seizing, PanicType.Seizure);
        typeof(BattleManager).GetMethod("ApplySeizure", PrivateStatic).Invoke(null, new object[] { _battle.enemyParty });
        Assert.AreEqual(95f, other.status.currentMental);
        Assert.AreEqual(100f, ally.status.currentMental);
    }

    [Test]
    public void AgitationMultipliesHealthDamageOnly()
    {
        var unit = Unit(false); var enemy = Unit(true);
        Panic(unit, PanicType.Agitation);
        unit.ReceiveDamage(7f, enemy, DamageType.True);
        Assert.AreEqual(90f, unit.status.currentHp, "7 × 1.5 = 10.5 → 10");
        unit.status.currentMental = 50f;
        unit.TakeMentalDamage(7f);
        Assert.AreEqual(43f, unit.status.currentMental, "Sanity damage unchanged");
    }

    // ── enemy patterns ─────────────────────────────────────────────────────

    private EnemyAI EnemyWithPatterns(out BattleCharacter enemy, params bool[] attackPatterns)
    {
        enemy = Unit(true);
        var patterns = new List<EnemyPatternData>();
        foreach (bool attack in attackPatterns)
            patterns.Add(new EnemyPatternData
            {
                patternName = attack ? "공격" : "강화",
                targetType = attack ? TargetType.SingleEnemy : TargetType.Self,
                effects = new List<EffectEntry> { attack
                    ? new EffectEntry { type = EffectType.Damage, damageType = DamageType.True, multiplier = 0f, fixedValue = 3f }
                    : new EffectEntry { type = EffectType.Strength, value = 1f, duration = 1 } }
            });
        enemy.status.origin.enemyPatterns = patterns;
        var ai = enemy.gameObject.AddComponent<EnemyAI>();
        ai.targetParty = _battle.playerParty;
        ai.allyParty = _battle.enemyParty;
        return ai;
    }

    private static int PreparedIndex(EnemyAI ai) => (int)typeof(EnemyAI).GetField("_preparedPatternIndex", Private).GetValue(ai);

    [Test]
    public void EnemyCompulsionRepeatsFirstPattern()
    {
        Unit(false);
        var ai = EnemyWithPatterns(out var enemy, true, false);
        Panic(enemy, PanicType.Compulsion);
        Assert.IsTrue(ai.PrepareTurn());
        Assert.AreEqual(0, PreparedIndex(ai));
        ai.ExecuteTurn();
        Assert.AreEqual(0, enemy.status.panic.LockedPatternIndex);
        Assert.IsTrue(ai.PrepareTurn());
        Assert.AreEqual(0, PreparedIndex(ai), "Rotation would pick pattern 1; 강박 repeats 0");
    }

    [Test]
    public void EnemyLethargySkipsAttackPatternsOrIdles()
    {
        Unit(false);
        var ai = EnemyWithPatterns(out var enemy, true, false);
        Panic(enemy, PanicType.Lethargy);
        Assert.IsTrue(ai.PrepareTurn());
        Assert.AreEqual(1, PreparedIndex(ai), "Uses the non-attack pattern");

        var idle = EnemyWithPatterns(out var attacker, true, true);
        Panic(attacker, PanicType.Lethargy);
        Assert.IsFalse(idle.PrepareTurn(), "No usable pattern → no action");
    }

    [Test]
    public void EnemyHallucinationIgnoresTaunt()
    {
        var player = Unit(false);
        var taunter = Unit(false);
        taunter.status.ApplyStatusEffect(EffectType.Taunt, 0f, 0, 0f, 5, -1, taunter);
        var ai = EnemyWithPatterns(out var enemy, true);
        Panic(enemy, PanicType.Hallucination);
        int tauntHits = 0, others = 0;
        Random.InitState(3);
        for (int i = 0; i < 30; i++)
        {
            float before = taunter.status.currentHp;
            Assert.IsTrue(ai.PrepareTurn());
            ai.ExecuteTurn();
            if (taunter.status.currentHp < before) tauntHits++; else others++;
            taunter.status.currentHp = 100f; player.status.currentHp = 100f; enemy.status.currentHp = 100f;
        }
        Assert.Greater(others, 0, "Hits land on non-taunters too");
        Assert.AreEqual(5, taunter.status.GetStatus(EffectType.Taunt).remainingCharges, "Taunt never consumed");
    }
}
