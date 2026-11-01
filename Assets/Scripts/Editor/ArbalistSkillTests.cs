using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TheLastArk.Battle;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.Skills;

public class ArbalistSkillTests
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private readonly List<Object> owned=new List<Object>();
    private object priorResources,priorTrain;
    private BattleManager battle;
    private T Own<T>(T value) where T:Object {owned.Add(value);return value;}
    private static FieldInfo Singleton(System.Type type)=>type.GetField("instance",BindingFlags.Static|BindingFlags.NonPublic);
    [SetUp] public void Setup()
    {
        AttackContext.ResetAll();
        priorResources=Singleton(typeof(ResourceManager)).GetValue(null);
        priorTrain=Singleton(typeof(TrainManager)).GetValue(null);
        Singleton(typeof(ResourceManager)).SetValue(null,Own(new GameObject("ArbalistTestResources")).AddComponent<ResourceManager>());
        var train=Own(new GameObject("ArbalistTestTrain")).AddComponent<TrainManager>();
        train.nexusCar=new TrainCar("넥서스",TrainCarType.Nexus,0);
        Singleton(typeof(TrainManager)).SetValue(null,train);
        battle=Own(new GameObject("ArbalistTestBattle")).AddComponent<BattleManager>();
        battle.playerParty=new List<BattleCharacter>(); battle.enemyParty=new List<BattleCharacter>();
        typeof(BattleManager).GetField("_currentPhase",Private).SetValue(battle,BattlePhase.PlayerTurn);
    }
    [TearDown] public void Cleanup()
    {
        EffectEngine.CurrentCast=null; AttackContext.ResetAll();
        for(int i=owned.Count-1;i>=0;i--) if(owned[i]!=null)Object.DestroyImmediate(owned[i]);
        owned.Clear();
        Singleton(typeof(ResourceManager)).SetValue(null,priorResources);
        Singleton(typeof(TrainManager)).SetValue(null,priorTrain);
    }
    private BattleCharacter Hero(int rank=0,bool passive=false)
    {
        var source=Resources.Load<CharacterData>("Characters/Player/Data/Arbalist_Data");
        Assert.NotNull(source);
        var data=Own(Object.Instantiate(source)); data.synergies.Clear();
        if(!passive){data.critRate=0;data.passiveSkill.passiveTrait=PassiveTrait.None;}
        var hero=Own(new GameObject("Markus")).AddComponent<BattleCharacter>();
        hero.status=new CharacterStatus(data){skillUpgradeOverride=rank,traitBloomOverride=0};
        hero.status.dynamicActiveSkill.AddRange(data.activeSkills); battle.playerParty.Add(hero); return hero;
    }
    private BattleCharacter Enemy(float armor=0,float hp=1000)
    {
        var data=Own(ScriptableObject.CreateInstance<CharacterData>());
        data.isEnemy=true;data.maxHp=hp;data.maxMental=100;data.armor=armor;data.baseAttack=10;
        var enemy=Own(new GameObject("Target")).AddComponent<BattleCharacter>();
        enemy.status=new CharacterStatus(data);battle.enemyParty.Add(enemy);return enemy;
    }
    private void Cast(BattleCharacter hero,int slot,BattleCharacter target,int die=0)
    {
        var data=hero.status.origin.activeSkills[slot].levels[hero.status.SkillLevelIndex];
        EffectEngine.CurrentCast=new SkillCast(); EffectEngine.CurrentCast.EffectValues.Add(die);
        try {EffectEngine.ProcessSkill(hero,new List<BattleCharacter>{target},data,target);}
        finally {EffectEngine.CurrentCast=null;}
    }
    [Test] public void PassiveStacksBloomAndConvertsOnlyExcessCrit()
    {
        var h=Hero(passive:true); Assert.AreEqual(75,h.status.FinalCritRate); Assert.AreEqual(7,h.status.FinalAttack);
        h.status.traitBloomOverride=1; Assert.AreEqual(100,h.status.FinalCritRate); Assert.AreEqual(25,h.status.PassiveCritDamageBonus);
        var item=Own(ScriptableObject.CreateInstance<EquipmentData>()); item.equipmentID="CritTest"; item.bonusCritRate=40; h.status.equippedItems[0]=item;
        Assert.AreEqual(140,h.status.UncappedCritRate); Assert.AreEqual(100,h.status.FinalCritRate); Assert.AreEqual(9.8f,h.status.FinalAttack,.001f);
    }
    [TestCase(0,1000,14)] [TestCase(1,1000,16)] [TestCase(2,250,16)] [TestCase(2,249,24)]
    public void PiercingShotStrictQuarterHealthThreshold(int rank,int hp,int damage)
    {var h=Hero(rank);var e=Enemy(); e.status.currentHp=hp;Cast(h,0,e,4);Assert.AreEqual(hp-damage,e.status.currentHp);Assert.IsNull(h.status.GetStatus(EffectType.Focus));}
    [Test] public void TemporaryFocusPreservesPreparedFocus()
    {var h=Hero();h.status.ApplyStatusEffect(EffectType.Focus,0,1);Cast(h,0,Enemy(),4);Assert.NotNull(h.status.GetStatus(EffectType.Focus));}
    [Test] public void BuiltInFocusBypassesTauntDuringRealTargetValidation()
    {
        var h=Hero();var taunter=Enemy();var e=Enemy();taunter.status.ApplyStatusEffect(EffectType.Taunt,0,0,0,1,-1,taunter,StatusDurationType.Charges);
        Fire(h,0,e,4);Assert.AreEqual(986,e.status.currentHp);Assert.AreEqual(1000,taunter.status.currentHp);Assert.IsNull(h.status.GetStatus(EffectType.Focus));
    }
    [TestCase(0,16,15)] [TestCase(1,18,17)] [TestCase(2,25,24)]
    public void ExplosionHitsNeighboursIndependentlyOfPrimaryShield(int rank,int primary,int side)
    {
        var h=Hero(rank);var left=Enemy(3);var mid=Enemy(2);var right=Enemy(3);
        mid.status.ApplyStatusEffect(EffectType.Shield,100,1);Cast(h,1,mid,15);
        Assert.AreEqual(1000,mid.status.currentHp);Assert.AreEqual(100-primary,mid.status.GetStatusValue(EffectType.Shield));
        Assert.AreEqual(1000-side,left.status.currentHp);Assert.AreEqual(1000-side,right.status.currentHp);
    }
    [Test] public void IsolatedExplosionBonusIsAppliedAfterArmor()
    {var h=Hero();var e=Enemy(2);Cast(h,1,e,15);Assert.AreEqual(980,e.status.currentHp);}
    [TestCase(0,3,1)] [TestCase(1,5,2)] [TestCase(2,5,2)]
    public void RetreatOddInputAndFlooredEvasion(int rank,int die,int count)
    {var h=Hero(rank);Cast(h,2,Enemy(),die);Assert.AreEqual(count,h.status.GetStatus(EffectType.Evasion).remainingCharges);Assert.IsTrue(h.status.origin.activeSkills[2].levels[rank].diceInput.AcceptsValue(die));}
    [Test] public void BaseRetreatOnlyEvadesPhysicalDamage()
    {
        var h=Hero();var e=Enemy();Cast(h,2,e,3);float before=h.status.currentHp;
        h.ReceiveDamage(5,e,DamageType.Magical);Assert.Less(h.status.currentHp,before);Assert.AreEqual(1,h.status.GetStatus(EffectType.Evasion).remainingCharges);
        h.TakeMentalDamage(5);Assert.AreEqual(1,h.status.GetStatus(EffectType.Evasion).remainingCharges);
        before=h.status.currentHp;h.ReceiveDamage(50,e,DamageType.Physical);Assert.AreEqual(before,h.status.currentHp);Assert.IsNull(h.status.GetStatus(EffectType.Evasion));
    }
    [Test] public void UpgradedRetreatSharesChargesWithMentalDamage()
    {var h=Hero(2);var e=Enemy();Cast(h,2,e,5);h.TakeMentalDamage(5);h.ReceiveDamage(50,e,DamageType.Magical);Assert.AreEqual(30,h.status.currentMental);Assert.AreEqual(30,h.status.currentHp);Assert.IsNull(h.status.GetStatus(EffectType.Evasion));}
    private void Fire(BattleCharacter h,int slot,BattleCharacter target,int die)
    {
        var pool=new ActionDicePool(f=>die);pool.BeginTurn(2);typeof(BattleManager).GetField("_dicePool",Private).SetValue(battle,pool);
        var selected=(List<int>)typeof(BattleManager).GetField("_selectedDiceIds",Private).GetValue(battle);selected.Clear();selected.Add(pool.Dice[0].Id);
        var selection=(BattleSelectionState)typeof(BattleManager).GetField("_selection",Private).GetValue(battle);selection.Set(h.status.origin.activeSkills[slot],h);
        typeof(BattleManager).GetMethod("PerformSkillOnTarget",Private).Invoke(battle,new object[]{target});
    }
    [TestCase(0,12)] [TestCase(1,14)] [TestCase(2,21)]
    public void AimedShotPreparesThenAttacksAndStopsAtTwo(int rank,int damage)
    {
        var h=Hero(rank);var e=Enemy(2);var skill=h.status.origin.activeSkills[3];Fire(h,3,h,5);
        Assert.AreEqual(1000,e.status.currentHp);Assert.AreEqual(1,battle.GetSkillUsesThisTurn(h,skill));Assert.NotNull(h.status.GetStatus(EffectType.Focus));
        Assert.AreEqual(rank>0,h.status.GetStatus(EffectType.Pierce)!=null);
        Fire(h,3,e,6);Assert.AreEqual(1000-damage,e.status.currentHp);Assert.AreEqual(2,battle.GetSkillUsesThisTurn(h,skill));
        Fire(h,3,e,6);Assert.AreEqual(1000-damage,e.status.currentHp);
    }
    [Test] public void AimedShotUsesMissingHpWithoutOldHalfHealthBonus()
    {var h=Hero(2);var e=Enemy();e.status.currentHp=400;Fire(h,3,h,5);Fire(h,3,e,5);Assert.AreEqual(199,e.status.currentHp);}
    [Test] public void InputConditionsMatchLatestDesign()
    {
        var h=Hero();for(int r=0;r<3;r++) {var skills=h.status.origin.activeSkills;
            Assert.AreEqual(3,skills[1].levels[r].diceInput.diceCount);Assert.IsFalse(skills[1].levels[r].diceInput.IsCharge);
            Assert.IsFalse(skills[3].levels[r].diceInput.IsCharge);Assert.IsFalse(skills[3].levels[r].diceInput.AcceptsValue(4));Assert.IsTrue(skills[3].levels[r].diceInput.AcceptsValue(5));}
    }

}
