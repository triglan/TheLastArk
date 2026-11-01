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
        var hero=Hero(passive:true);Assert.AreEqual(75,hero.status.FinalCritRate);Assert.AreEqual(7,hero.status.FinalAttack);
        hero.status.traitBloomOverride=1;Assert.AreEqual(100,hero.status.FinalCritRate);Assert.AreEqual(25,hero.status.PassiveCritDamageBonus);
        var item=Own(ScriptableObject.CreateInstance<EquipmentData>());item.equipmentID="CritTest";item.bonusCritRate=40;
        hero.status.equippedItems[0]=item;
        Assert.AreEqual(140,hero.status.UncappedCritRate);Assert.AreEqual(100,hero.status.FinalCritRate);
        Assert.AreEqual(9.8f,hero.status.FinalAttack,.001f);
        hero.status.equippedItems[0]=null;Assert.AreEqual(7,hero.status.FinalAttack);
    }
    [Test] public void BloomAddsCritDamageToActualHit()
    {
        var hero=Hero(passive:true);hero.status.traitBloomOverride=1;hero.status.origin.baseAttack=100;
        var target=Enemy();Cast(hero,0,target,4);
        Assert.AreEqual(1000-Mathf.Floor(154*(TheLastArk.Character.SynergyEffects.CritDamagePercent(hero.status)+25)/100f),target.status.currentHp);
    }
    [TestCase(0,14)] [TestCase(1,16)] [TestCase(2,24)]
    public void PiercingShotDiceAndFullHealthBonus(int rank,int damage)
    {var hero=Hero(rank);var target=Enemy();Cast(hero,0,target,4);Assert.AreEqual(1000-damage,target.status.currentHp);Assert.IsNull(hero.status.GetStatus(EffectType.Focus));}
    [Test] public void FullHealthBonusDoesNotApplyToInjuredTarget()
    {var hero=Hero(2);var target=Enemy();target.status.currentHp=999;Cast(hero,0,target,4);Assert.AreEqual(983,target.status.currentHp);}
    [Test] public void ExplosionUsesActualPrimaryDamageAndSeparateSplashDefenses()
    {
        var hero=Hero();hero.status.origin.baseAttack=20;
        var left=Enemy(2);var main=Enemy(10);var right=Enemy(2);var distant=Enemy();Cast(hero,1,main);
        Assert.AreEqual(980,main.status.currentHp);Assert.AreEqual(992,left.status.currentHp);Assert.AreEqual(992,right.status.currentHp);Assert.AreEqual(1000,distant.status.currentHp);
    }
    [Test] public void ExplosionUpgradeAddsAllEnemyWaveFromPrimaryDamageOnly()
    {
        var hero=Hero(2);hero.status.origin.baseAttack=20;
        var left=Enemy(2);var main=Enemy(10);var right=Enemy(2);var distant=Enemy(1);Cast(hero,1,main);
        Assert.AreEqual(974,main.status.currentHp);Assert.AreEqual(985,left.status.currentHp);Assert.AreEqual(985,right.status.currentHp);Assert.AreEqual(995,distant.status.currentHp);
    }
    [Test] public void FullyShieldedPrimaryDoesNotCreateExplosionDamage()
    {
        var hero=Hero(2);var main=Enemy();var side=Enemy();main.GainShield(100,null);Cast(hero,1,main);
        Assert.AreEqual(1000,side.status.currentHp);
    }
    [Test] public void OverkillExplosionUsesOnlyHealthActuallyLost()
    {
        var hero=Hero();hero.status.origin.baseAttack=100;var side=Enemy();var main=Enemy(0,3);Cast(hero,1,main);
        Assert.AreEqual(0,main.status.currentHp);Assert.AreEqual(999,side.status.currentHp);
    }
    [TestCase(0,2,1)] [TestCase(0,4,2)] [TestCase(2,6,3)]
    public void RetreatGrantsDiceBasedEvasion(int rank,int die,int count)
    {
        var hero=Hero(rank);var target=Enemy();Cast(hero,2,target,die);
        Assert.AreEqual(count,hero.status.GetStatus(EffectType.Evasion).remainingCharges);
        hero.status.TickSideTurnEnd(); // Remains available during the enemy phase.
        for(int i=0;i<count;i++)Assert.AreEqual(0,hero.ReceiveDamage(10,target,DamageType.True));
        Assert.IsNull(hero.status.GetStatus(EffectType.Evasion));Assert.AreEqual(10,hero.ReceiveDamage(10,target,DamageType.True));
    }
    [Test] public void BaseRetreatDoesNotEvadeMentalDamageOrSpendItsCount()
    {var hero=Hero();var target=Enemy();Cast(hero,2,target,4);Assert.AreEqual(5,hero.TakeMentalDamage(5));Assert.AreEqual(2,hero.status.GetStatus(EffectType.Evasion).remainingCharges);}
    [Test] public void UpgradedRetreatSharesChargesAcrossHealthAndMentalAndExpires()
    {
        var hero=Hero(2);var target=Enemy();Cast(hero,2,target,6);
        Assert.AreEqual(0,hero.TakeMentalDamage(5));Assert.AreEqual(0,hero.ReceiveDamage(10,target,DamageType.Magical));
        Assert.AreEqual(1,hero.status.GetStatus(EffectType.Evasion).remainingCharges);
        hero.status.RemoveEffectsExpiringAtOwnerPhaseStart();Assert.IsNull(hero.status.GetStatus(EffectType.Evasion));Assert.AreEqual(5,hero.TakeMentalDamage(5));
    }
    [TestCase(500,180)] [TestCase(499,270)]
    public void FinisherUsesMissingHealthAndStrictHalfThreshold(float hp,int damage)
    {var hero=Hero(2);hero.status.origin.baseAttack=10;var target=Enemy(100);target.status.currentHp=hp;Cast(hero,3,target);Assert.AreEqual(hp-damage,target.status.currentHp);Assert.AreEqual(100,target.status.FinalArmor);}
    [Test] public void FocusBypassesTauntButKeepsProtection()
    {
        var hero=Hero();var taunter=Enemy();var target=Enemy();var guard=Enemy();
        taunter.status.ApplyStatusEffect(EffectType.Taunt,0,0,0,3,-1,taunter,StatusDurationType.Charges);
        target.status.ApplyStatusEffect(EffectType.Guard,0,0,0,1,-1,guard,StatusDurationType.Charges);
        Cast(hero,0,target,4);Assert.AreEqual(1000,taunter.status.currentHp);Assert.AreEqual(1000,target.status.currentHp);Assert.AreEqual(986,guard.status.currentHp);
    }
    [Test] public void InputConditionsAndChargeTargetsAreAuthoredForEveryRank()
    {
        var hero=Hero();var skills=hero.status.origin.activeSkills;
        for(int rank=0;rank<3;rank++)
        {
            Assert.IsFalse(skills[0].levels[rank].diceInput.AcceptsValue(3));Assert.IsTrue(skills[0].levels[rank].diceInput.AcceptsValue(4));
            Assert.AreEqual(20,skills[1].levels[rank].diceInput.chargeTarget);
            Assert.IsFalse(skills[2].levels[rank].diceInput.AcceptsValue(3));Assert.IsTrue(skills[2].levels[rank].diceInput.AcceptsValue(4));
            var finisher=skills[3].levels[rank].diceInput;Assert.AreEqual(40,finisher.chargeTarget);
            Assert.AreEqual(rank>0,finisher.AcceptsValue(4));Assert.IsTrue(finisher.AcceptsValue(5));
        }
    }
    [Test] public void ChargedFinisherConsumesRealDiceThenFiresOnlyOnce()
    {
        var hero=Hero(1);var target=Enemy();var skill=hero.status.origin.activeSkills[3];
        var pool=new ActionDicePool(f=>5);typeof(BattleManager).GetField("_dicePool",Private).SetValue(battle,pool);pool.BeginTurn(9);
        var selected=(List<int>)typeof(BattleManager).GetField("_selectedDiceIds",Private).GetValue(battle);
        for(int i=0;i<8;i++){selected.Clear();selected.Add(i);battle.SelectSkill(skill,hero);Assert.AreEqual(i==7,battle.IsChargeReady(hero,skill));}
        Assert.AreEqual(1000,target.status.currentHp);
        selected.Clear();var selection=(BattleSelectionState)typeof(BattleManager).GetField("_selection",Private).GetValue(battle);selection.Set(skill,hero);
        typeof(BattleManager).GetMethod("PerformSkillOnTarget",Private).Invoke(battle,new object[]{target});
        Assert.AreEqual(979,target.status.currentHp);Assert.IsFalse(battle.IsChargeReady(hero,skill));
    }
}
