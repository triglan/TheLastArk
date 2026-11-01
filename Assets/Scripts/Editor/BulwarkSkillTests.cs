using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TheLastArk.Battle;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.Skills;

public class BulwarkSkillTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> owned = new List<Object>();
    private object priorResources, priorTrain, priorBattle;
    private Random.State randomState;
    private BattleManager battle;
    private static FieldInfo Singleton(System.Type type) => type.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
    private static FieldInfo ActiveBattle => typeof(BattleManager).GetField("<Active>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
    private T Own<T>(T obj) where T : Object { owned.Add(obj); return obj; }

    [SetUp] public void Setup()
    {
        randomState = Random.state; AttackContext.ResetAll();
        priorResources = Singleton(typeof(ResourceManager)).GetValue(null);
        priorTrain = Singleton(typeof(TrainManager)).GetValue(null);
        priorBattle = ActiveBattle.GetValue(null);
        Singleton(typeof(ResourceManager)).SetValue(null, Own(new GameObject("FerrumTestResources")).AddComponent<ResourceManager>());
        var train = Own(new GameObject("FerrumTestTrain")).AddComponent<TrainManager>();
        train.nexusCar = new TrainCar("넥서스", TrainCarType.Nexus, 0);
        Singleton(typeof(TrainManager)).SetValue(null, train);
        battle = Own(new GameObject("FerrumTestBattle")).AddComponent<BattleManager>();
        battle.playerParty = new List<BattleCharacter>(); battle.enemyParty = new List<BattleCharacter>();
        ActiveBattle.SetValue(null, battle);
        typeof(BattleManager).GetField("_currentPhase", Private).SetValue(battle, BattlePhase.PlayerTurn);
    }

    [TearDown] public void Cleanup()
    {
        EffectEngine.CurrentCast = null; AttackContext.ResetAll();
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); ActiveBattle.SetValue(null, priorBattle);
        Singleton(typeof(ResourceManager)).SetValue(null, priorResources);
        Singleton(typeof(TrainManager)).SetValue(null, priorTrain); Random.state = randomState;
    }

    private BattleCharacter Hero(int rank = 0, int charge = 0, bool bloom = false)
    {
        var source = Resources.Load<CharacterData>("Characters/Player/Data/Bulwark_Data"); Assert.NotNull(source);
        var data = Own(Object.Instantiate(source)); data.synergies.Clear();
        var hero = Own(new GameObject("Ferrum")).AddComponent<BattleCharacter>();
        hero.status = new CharacterStatus(data) { skillUpgradeOverride = rank, traitBloomOverride = bloom ? 1 : 0 };
        hero.status.GainCapacitor(charge); hero.status.dynamicActiveSkill.AddRange(data.activeSkills);
        battle.playerParty.Add(hero); return hero;
    }

    private BattleCharacter Unit(bool enemy = true, float armor = 0, float mr = 0)
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.isEnemy = enemy; data.maxHp = 1000; data.maxMental = 100; data.armor = armor; data.magicResist = mr;
        var unit = Own(new GameObject(enemy ? "Enemy" : "Ally")).AddComponent<BattleCharacter>();
        unit.status = new CharacterStatus(data); (enemy ? battle.enemyParty : battle.playerParty).Add(unit); return unit;
    }

    private SkillInfo Skill(BattleCharacter hero, int slot) => hero.status.origin.activeSkills[slot];
    private void Cast(BattleCharacter hero, int slot, BattleCharacter target, int die = 6)
    {
        var level = Skill(hero, slot).levels[hero.status.SkillLevelIndex];
        var targets = level.targetType == TargetType.AllEnemy ? new List<BattleCharacter>(battle.enemyParty) : new List<BattleCharacter> { target };
        EffectEngine.CurrentCast = new SkillCast(); EffectEngine.CurrentCast.EffectValues.Add(die);
        EffectEngine.CurrentCast.SelfShield = hero.status.GetStatusValue(EffectType.Shield);
        try { EffectEngine.ProcessSkill(hero, targets, level, target); }
        finally { EffectEngine.CurrentCast = null; }
    }

    [TestCase(false,10)] [TestCase(true,12)]
    public void StartGainsTwoAndBattleResourceResets(bool bloom,int cap)
    {var h=Hero(bloom:bloom);CapacitorRules.OnTurnStart(battle.playerParty);Assert.AreEqual(2,h.status.CapacitorCharge);h.status.GainCapacitor(100);Assert.AreEqual(cap,h.status.CapacitorCharge);h.status.ResetCapacitor();Assert.AreEqual(0,h.status.CapacitorCharge);}
    [TestCase(false,false,4,0)] [TestCase(false,false,5,1)] [TestCase(false,false,25,1)]
    [TestCase(false,true,5,0)] [TestCase(true,true,5,1)]
    public void ShieldAbsorptionAwardsPerHitAndOtherAlliesNeedBloom(bool bloom,bool other,int damage,int gain)
    {var h=Hero(bloom:bloom);var receiver=other?Unit(false):h;receiver.status.origin.armor=0;receiver.status.ApplyStatusEffect(EffectType.Shield,100,1);receiver.ReceiveDamage(damage,Unit(),DamageType.Physical);Assert.AreEqual(gain,h.status.CapacitorCharge);}
    [Test] public void ShieldThresholdUsesActualAbsorptionNotIncomingDamage()
    {var h=Hero();h.status.ApplyStatusEffect(EffectType.Shield,4,1);h.ReceiveDamage(50,Unit(),DamageType.Physical);Assert.AreEqual(0,h.status.CapacitorCharge);}
    [TestCase(0,5,0,1)] [TestCase(0,6,12,1)] [TestCase(1,6,12,2)] [TestCase(2,3,0,2)] [TestCase(2,4,12,2)]
    public void ScreenAddsGuardAndFreeSelfShieldAtThreshold(int rank,int charge,int self,int guards)
    {var h=Hero(rank,charge);var ally=Unit(false);Cast(h,0,ally);Assert.AreEqual(12,ally.status.GetStatusValue(EffectType.Shield));Assert.AreEqual(self,h.status.GetStatusValue(EffectType.Shield));Assert.AreEqual(charge,h.status.CapacitorCharge);Assert.AreEqual(guards,ally.status.GetStatus(EffectType.Guard).remainingCharges);Assert.AreSame(h,ally.status.GetStatus(EffectType.Guard).source);}
    [Test] public void ScreenCannotTargetSelf()
    {var h=Hero(charge:8);Cast(h,0,h);Assert.AreEqual(0,h.status.GetStatusValue(EffectType.Shield));Assert.IsNull(h.status.GetStatus(EffectType.Guard));Assert.AreEqual(8,h.status.CapacitorCharge);}
    [TestCase(0,0,7,0,0)] [TestCase(0,6,13,2,0)] [TestCase(1,4,13,0,1)] [TestCase(2,10,16,4,1)]
    public void ShockConsumesUpToLimitAndDebuffsAfterFour(int rank,int charge,int damage,int left,int debuff)
    {var h=Hero(rank,charge);var e=Unit();Cast(h,2,e,4);Assert.AreEqual(1000-damage,e.status.currentHp);Assert.AreEqual(left,h.status.CapacitorCharge);Assert.AreEqual(debuff,e.status.GetStatusValue(EffectType.Vulnerable));Assert.AreEqual(debuff,e.status.GetStatusValue(EffectType.Corrosion));Assert.IsTrue(Skill(h,2).levels[rank].diceInput.AcceptsValue(1));}
    [TestCase(0,5,8,0)] [TestCase(1,5,9,0)] [TestCase(2,5,9,2)] [TestCase(2,12,16,6)]
    public void ProtocolShieldsAllAlliesAndRefundsHalf(int rank,int charge,int shield,int refund)
    {var h=Hero(rank,charge,rank==2);var ally=Unit(false);Cast(h,3,ally);Assert.AreEqual(shield,h.status.GetStatusValue(EffectType.Shield));Assert.AreEqual(shield,ally.status.GetStatusValue(EffectType.Shield));Assert.AreEqual(refund,h.status.CapacitorCharge);}
    private void Insert(BattleCharacter h,int slot,int die)
    {
        var pool=new ActionDicePool(f=>die);pool.BeginTurn(2);typeof(BattleManager).GetField("_dicePool",Private).SetValue(battle,pool);
        var selected=(List<int>)typeof(BattleManager).GetField("_selectedDiceIds",Private).GetValue(battle);selected.Clear();selected.Add(pool.Dice[0].Id);
        Assert.IsTrue((bool)typeof(BattleManager).GetMethod("TryInsertChargeDice",Private).Invoke(battle,new object[]{h,Skill(h,slot)}));
    }
    private void Fire(BattleCharacter h,int slot,BattleCharacter target)
    {
        var selection=(BattleSelectionState)typeof(BattleManager).GetField("_selection",Private).GetValue(battle);selection.Set(Skill(h,slot),h);
        typeof(BattleManager).GetMethod("PerformSkillOnTarget",Private).Invoke(battle,new object[]{target});
    }
    [TestCase(0,10,4,14)] [TestCase(1,12,4,16)] [TestCase(2,14,8,22)]
    public void FieldChargesBothStagesUsesCapacitorAndStopsAfterTwo(int rank,int shield,int charge,int damage)
    {
        var h=Hero(rank);var e=Unit();var skill=Skill(h,1);Insert(h,1,6);Insert(h,1,4);Insert(h,1,5);
        Assert.IsTrue(battle.IsChargeReady(h,skill));Fire(h,1,h);Assert.AreEqual(shield,h.status.GetStatusValue(EffectType.Shield));Assert.AreEqual(1000,e.status.currentHp);
        Assert.AreEqual(1,battle.GetSkillUsesThisTurn(h,skill));Assert.IsFalse(battle.IsChargeReady(h,skill));
        Insert(h,1,6);Insert(h,1,6);if(!battle.IsChargeReady(h,skill))Insert(h,1,1);
        Fire(h,1,e);Assert.AreEqual(charge,h.status.CapacitorCharge);Assert.AreEqual(1000-damage,e.status.currentHp);Assert.AreEqual(shield,h.status.GetStatusValue(EffectType.Shield));
        Assert.AreEqual(2,battle.GetSkillUsesThisTurn(h,skill));Assert.AreEqual(BattleManager.DiceReasonUsed,battle.GetDiceSkillUnavailableReason(h,skill));
    }
    [Test] public void ChargeRequirementFollowsResourceWithoutLosingStoredCharge()
    {
        var h=Hero();var skill=Skill(h,1);Insert(h,1,6);Assert.AreEqual(15,battle.GetDiceRule(skill,h).chargeTarget);
        h.status.GainCapacitor(10);Assert.IsTrue(battle.IsChargeReady(h,skill));Assert.AreEqual(5,battle.GetDiceRule(skill,h).chargeTarget);
        h.status.SpendCapacitor(8);Assert.IsFalse(battle.IsChargeReady(h,skill));Assert.AreEqual(6,battle.GetDiceCharge(h,skill));Assert.AreEqual(28,battle.GetDiceRule(Skill(h,3),h).chargeTarget);
    }

}
