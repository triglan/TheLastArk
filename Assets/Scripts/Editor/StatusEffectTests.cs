using System.Collections.Generic;
using NUnit.Framework;
using TheLastArk.Battle;
using UnityEngine;

public class StatusEffectTests
{
    private readonly List<Object> _owned = new List<Object>();
    private BattleManager _battle;

    private T Own<T>(T value) where T : Object { _owned.Add(value); return value; }

    [SetUp]
    public void Setup()
    {
        AttackContext.ResetAll();
        _battle = Own(new GameObject("StatusTestBattle")).AddComponent<BattleManager>();
        _battle.playerParty = new List<BattleCharacter>();
        _battle.enemyParty = new List<BattleCharacter>();
    }

    [TearDown]
    public void Cleanup()
    {
        AttackContext.ResetAll();
        for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
        _owned.Clear();
    }

    private BattleCharacter Unit(bool enemy, float hp = 100f, float attack = 10f, float armor = 0f)
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.isEnemy = enemy; data.maxHp = hp; data.maxMental = 100f; data.baseAttack = attack; data.armor = armor;
        var unit = Own(new GameObject(enemy ? "Enemy" : "Ally")).AddComponent<BattleCharacter>();
        unit.status = new CharacterStatus(data);
        (enemy ? _battle.enemyParty : _battle.playerParty).Add(unit);
        return unit;
    }

    private static SkillLevelData Hits(int hits, float damage, TargetType type = TargetType.SingleEnemy)
    {
        return new SkillLevelData { targetType = type, effects = new List<EffectEntry> {
            new EffectEntry { type = EffectType.Damage, damageType = DamageType.Physical, multiplier = 0f, fixedValue = damage, hitCount = hits } } };
    }

    [Test]
    public void InstancesExpireSeparatelyAndMaxTypeChanges()
    {
        var unit = Unit(false);
        unit.status.ApplyStatusEffect(EffectType.Bleed, 5f, 2);
        unit.status.ApplyStatusEffect(EffectType.Bleed, 8f, 1);
        Assert.AreEqual(8f, unit.status.GetStatusValue(EffectType.Bleed), "Max-type uses the largest instance");
        StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { unit });
        Assert.AreEqual(92f, unit.status.currentHp, "Bleed deals the largest value once at round end");
        Assert.AreEqual(5f, unit.status.GetStatusValue(EffectType.Bleed), "The 1-turn instance expired; the max changes");
        StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { unit });
        Assert.AreEqual(87f, unit.status.currentHp);
        Assert.IsNull(unit.status.GetStatus(EffectType.Bleed));
    }

    [Test]
    public void SumTypeBreakdownAndSideTurnEndTicking()
    {
        var unit = Unit(false);
        unit.status.ApplyStatusEffect(EffectType.Strength, 5f, 0, StatusDurationType.Permanent);
        unit.status.ApplyStatusEffect(EffectType.Strength, 3f, 3);
        unit.status.ApplyStatusEffect(EffectType.Strength, 2f, 1);
        Assert.AreEqual(10f, unit.status.GetStatusValue(EffectType.Strength));
        var summary = unit.status.GetStatusSummaries().Find(s => s.type == EffectType.Strength);
        StringAssert.StartsWith("힘 10 = 5 (영구) + 3 (3턴) + 2 (1턴)", summary.breakdown);

        StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { unit });
        Assert.AreEqual(10f, unit.status.GetStatusValue(EffectType.Strength), "Stats do not tick at round end");
        unit.status.TickSideTurnEnd();
        Assert.AreEqual(8f, unit.status.GetStatusValue(EffectType.Strength));
        unit.status.TickSideTurnEnd(); unit.status.TickSideTurnEnd();
        Assert.AreEqual(5f, unit.status.GetStatusValue(EffectType.Strength), "Permanent stays until battle end");
        unit.status.RemoveAllStatusEffects();
        Assert.AreEqual(0f, unit.status.GetStatusValue(EffectType.Strength));
    }

    [Test]
    public void NegativeStatsFloorAtZero()
    {
        var unit = Unit(false, attack: 4f);
        unit.status.ApplyStatusEffect(EffectType.Weakness, 10f, 3);
        Assert.AreEqual(0f, unit.status.FinalAttack);
    }

    [Test]
    public void RefreshRulesForTimeCountAndReflect()
    {
        var unit = Unit(false);
        unit.status.ApplyStatusEffect(EffectType.Stun, 0f, 1);
        unit.status.ApplyStatusEffect(EffectType.Stun, 0f, 3);
        unit.status.ApplyStatusEffect(EffectType.Stun, 0f, 2);
        Assert.AreEqual(1, unit.status.activeStatusEffects.FindAll(e => e.effectType == EffectType.Stun).Count);
        Assert.AreEqual(3, unit.status.GetStatus(EffectType.Stun).remainingTurns);

        unit.status.ApplyStatusEffect(EffectType.Taunt, 0f, 0, 0f, 3, -1, unit);
        unit.status.ApplyStatusEffect(EffectType.Taunt, 0f, 0, 0f, 1, -1, unit);
        Assert.AreEqual(3, unit.status.GetStatus(EffectType.Taunt).remainingCharges);

        unit.status.ApplyStatusEffect(EffectType.Reflect, 10f, 0, 0f, 3, -1, unit);
        unit.status.ApplyStatusEffect(EffectType.Reflect, 20f, 0, 0f, 1, -1, unit);
        Assert.AreEqual(30f, unit.status.GetStatusValue(EffectType.Reflect), "Reflect ratios are summed");
        Assert.AreEqual(3, unit.status.GetStatus(EffectType.Reflect).remainingCharges, "Reflect count is the max");
    }

    [Test]
    public void PoisonDecaysPerInstanceAndBurnHalvesHealing()
    {
        var unit = Unit(false, hp: 200f);
        unit.status.ApplyStatusEffect(EffectType.Poison, 20f, 5);
        StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { unit });
        Assert.AreEqual(180f, unit.status.currentHp);
        Assert.AreEqual(18f, unit.status.GetStatusValue(EffectType.Poison), 1e-3f);
        StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { unit });
        Assert.AreEqual(162f, unit.status.currentHp);
        Assert.AreEqual(16.2f, unit.status.GetStatusValue(EffectType.Poison), 1e-3f, "Decimals kept internally");
        StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { unit });
        Assert.AreEqual(146f, unit.status.currentHp, "Dealt damage is floored (16.2 -> 16)");

        unit.status.RemoveAllStatusEffects();
        unit.status.ApplyStatusEffect(EffectType.Burn, 5f, 2);
        unit.status.ApplyStatusEffect(EffectType.Burn, 5f, 2);
        StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { unit });
        Assert.AreEqual(126f, unit.status.currentHp, "Burn % summed: 10% of 200");
        Assert.AreEqual(10f, unit.ReceiveHeal(21f, unit), "Healing −50% then floored");
    }

    [Test]
    public void FearScalesWithLostSanityAndDespairReducesSanityHealing()
    {
        var unit = Unit(false);
        unit.status.currentMental = 50f;
        unit.status.ApplyStatusEffect(EffectType.Fear, 4f, 2);
        unit.status.ApplyStatusEffect(EffectType.Fear, 2f, 2);
        StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { unit });
        Assert.AreEqual(44f, unit.status.currentMental, "4 x (1 + 50/100) = 6");

        unit.status.ApplyStatusEffect(EffectType.Despair, 30f, 2);
        unit.status.ApplyStatusEffect(EffectType.Despair, 20f, 2);
        Assert.AreEqual(5f, unit.ReceiveMentalHeal(11f), "50% summed, floored (5.5 -> 5)");
    }

    [Test]
    public void ShieldAbsorbsDotAndExpiresAtOwnerPhase()
    {
        var unit = Unit(false);
        unit.status.ApplyStatusEffect(EffectType.Shield, 3f, 1);
        unit.status.ApplyStatusEffect(EffectType.Shield, 4f, 1);
        unit.status.ApplyStatusEffect(EffectType.Bleed, 5f, 2);
        StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { unit });
        Assert.AreEqual(100f, unit.status.currentHp);
        Assert.AreEqual(2f, unit.status.GetStatusValue(EffectType.Shield));
        unit.status.RemoveEffectsExpiringAtOwnerPhaseStart();
        Assert.AreEqual(0f, unit.status.GetStatusValue(EffectType.Shield));
    }

    [Test]
    public void CounterAndReflectResolveOnceAfterAllHits()
    {
        var attacker = Unit(true, attack: 10f);
        var defender = Unit(false, attack: 7f);
        defender.status.ApplyStatusEffect(EffectType.Counter, 0f, 0, 0f, 2, -1, defender);
        defender.status.ApplyStatusEffect(EffectType.Reflect, 50f, 0, 0f, 2, -1, defender);
        EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { defender }, Hits(3, 4f));
        Assert.AreEqual(88f, defender.status.currentHp);
        Assert.AreEqual(100f - 7f - 5f, attacker.status.currentHp, "One counter (own ATK) then one reflect (attacker ATK x 50%)");
        Assert.AreEqual(1, defender.status.GetStatus(EffectType.Counter).remainingCharges);
        Assert.AreEqual(1, defender.status.GetStatus(EffectType.Reflect).remainingCharges);
    }

    [Test]
    public void ResponsesTriggerOnZeroDamageButNotWhenStunned()
    {
        var attacker = Unit(true, attack: 10f);
        var defender = Unit(false, attack: 7f, armor: 50f);
        defender.status.ApplyStatusEffect(EffectType.Counter, 0f, 0, 0f, 1, -1, defender);
        EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { defender }, Hits(1, 4f));
        Assert.AreEqual(100f, defender.status.currentHp);
        Assert.AreEqual(93f, attacker.status.currentHp, "Counter fires even when HP damage was 0");

        defender.status.ApplyStatusEffect(EffectType.Counter, 0f, 0, 0f, 1, -1, defender);
        defender.status.ApplyStatusEffect(EffectType.Stun, 0f, 1);
        EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { defender }, Hits(1, 4f));
        Assert.AreEqual(93f, attacker.status.currentHp, "Stunned characters can't counter");
        Assert.AreEqual(1, defender.status.GetStatus(EffectType.Counter).remainingCharges);
    }

    [Test]
    public void TauntStaysOnTaunterWhenExhaustedMidAttack()
    {
        var attacker = Unit(true);
        var target = Unit(false);
        var taunter = Unit(false);
        taunter.status.ApplyStatusEffect(EffectType.Taunt, 0f, 0, 0f, 1, -1, taunter);
        EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, Hits(3, 5f));
        Assert.AreEqual(100f, target.status.currentHp);
        Assert.AreEqual(85f, taunter.status.currentHp, "All three hits stay on the taunter");
        Assert.IsNull(taunter.status.GetStatus(EffectType.Taunt));
    }

    [Test]
    public void AllTargetAttacksIgnoreTaunt()
    {
        var attacker = Unit(true);
        var a = Unit(false);
        var taunter = Unit(false);
        taunter.status.ApplyStatusEffect(EffectType.Taunt, 0f, 0, 0f, 2, -1, taunter);
        EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { a, taunter }, Hits(1, 5f, TargetType.AllEnemy));
        Assert.AreEqual(95f, a.status.currentHp);
        Assert.AreEqual(95f, taunter.status.currentHp);
        Assert.AreEqual(2, taunter.status.GetStatus(EffectType.Taunt).remainingCharges);
    }

    [Test]
    public void ProtectorTakesHitsAfterOwnDefenseReductionAndShield()
    {
        var attacker = Unit(true);
        var ally = Unit(false);
        var protector = Unit(false, armor: 2f);
        ally.status.ApplyStatusEffect(EffectType.Guard, 0f, 0, 50f, 1, -1, protector);
        protector.status.ApplyStatusEffect(EffectType.Shield, 3f, 1);
        protector.status.ApplyStatusEffect(EffectType.Stun, 0f, 1);
        EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { ally }, Hits(2, 12f));
        Assert.AreEqual(100f, ally.status.currentHp);
        // Each hit: 12 − 2 armor = 10 → −50% = 5; first 3 go to the shield.
        Assert.AreEqual(93f, protector.status.currentHp, "Protect works while stunned and covers the whole skill");
        Assert.IsNull(ally.status.GetStatus(EffectType.Guard));
    }

    [Test]
    public void BleedExtraAndPressureFireAfterAction()
    {
        var unit = Unit(false);
        unit.status.ApplyStatusEffect(EffectType.Bleed, 5f, 2);
        unit.status.ApplyStatusEffect(EffectType.Pressure, 3f, 2);
        unit.status.ApplyStatusEffect(EffectType.Pressure, 4f, 1);
        unit.ResolveActionStatusEffects();
        Assert.AreEqual(98f, unit.status.currentHp, "Extra bleed n/2 floored");
        Assert.AreEqual(96f, unit.status.currentMental, "Pressure uses the largest instance");
    }

    [Test]
    public void PlayerOnlyLimitsSkipEnemies()
    {
        var caster = Unit(false);
        var enemy = Unit(true);
        var data = new SkillLevelData { targetType = TargetType.SingleEnemy, effects = new List<EffectEntry> {
            new EffectEntry { type = EffectType.Frost, value = 2f, duration = 2 },
            new EffectEntry { type = EffectType.Blockade, duration = 2 },
            new EffectEntry { type = EffectType.Weakness, value = 1f } } };
        EffectEngine.ProcessSkill(caster, new List<BattleCharacter> { enemy }, data);
        Assert.IsNull(enemy.status.GetStatus(EffectType.Frost));
        Assert.IsNull(enemy.status.GetStatus(EffectType.Blockade));
        Assert.AreEqual(StatusRules.DefaultStatTurns, enemy.status.GetStatus(EffectType.Weakness).remainingTurns, "Stats default to 3 turns");
    }
}
