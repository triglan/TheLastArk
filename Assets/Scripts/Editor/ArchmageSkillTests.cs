using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TheLastArk.Battle;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.Skills;

public class ArchmageSkillTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> owned = new List<Object>();
    private object priorResources, priorTrain;
    private Random.State randomState;
    private BattleManager battle;
    private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
    private static FieldInfo Singleton(System.Type type) => type.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);

    [SetUp] public void Setup()
    {
        randomState = Random.state;
        AttackContext.ResetAll();
        priorResources = Singleton(typeof(ResourceManager)).GetValue(null);
        priorTrain = Singleton(typeof(TrainManager)).GetValue(null);
        Singleton(typeof(ResourceManager)).SetValue(null, Own(new GameObject("MerlinTestResources")).AddComponent<ResourceManager>());
        var train = Own(new GameObject("MerlinTestTrain")).AddComponent<TrainManager>();
        train.nexusCar = new TrainCar("넥서스", TrainCarType.Nexus, 0);
        Singleton(typeof(TrainManager)).SetValue(null, train);
        battle = Own(new GameObject("MerlinTestBattle")).AddComponent<BattleManager>();
        battle.playerParty = new List<BattleCharacter>(); battle.enemyParty = new List<BattleCharacter>();
        typeof(BattleManager).GetField("_currentPhase", Private).SetValue(battle, BattlePhase.PlayerTurn);
    }

    [TearDown] public void Cleanup()
    {
        EffectEngine.CurrentCast = null; AttackContext.ResetAll();
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
        Singleton(typeof(ResourceManager)).SetValue(null, priorResources);
        Singleton(typeof(TrainManager)).SetValue(null, priorTrain);
        Random.state = randomState;
    }

    private BattleCharacter Hero(int rank = 0, int mana = 0, bool bloom = false)
    {
        var source = Resources.Load<CharacterData>("Characters/Player/Data/Archmage_Data");
        Assert.NotNull(source);
        var data = Own(Object.Instantiate(source)); data.synergies.Clear(); data.critRate = 0;
        var hero = Own(new GameObject("Merlin")).AddComponent<BattleCharacter>();
        hero.status = new CharacterStatus(data) { skillUpgradeOverride = rank, traitBloomOverride = bloom ? 1 : 0 };
        hero.status.GainPrimalMana(mana);
        hero.status.dynamicActiveSkill.AddRange(data.activeSkills);
        battle.playerParty.Add(hero);
        return hero;
    }

    private BattleCharacter Enemy(float mr = 0, float hp = 1000)
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.isEnemy = true; data.maxHp = hp; data.maxMental = 100; data.magicResist = mr;
        var enemy = Own(new GameObject("Target")).AddComponent<BattleCharacter>();
        enemy.status = new CharacterStatus(data); battle.enemyParty.Add(enemy); return enemy;
    }

    private SkillInfo Skill(BattleCharacter hero, int index) => hero.status.origin.activeSkills[index];
    private void Cast(BattleCharacter hero, int slot, BattleCharacter target, int die = 6)
    {
        var data = Skill(hero, slot).levels[hero.status.SkillLevelIndex];
        EffectEngine.CurrentCast = new SkillCast(); EffectEngine.CurrentCast.EffectValues.Add(die);
        try { EffectEngine.ProcessSkill(hero, new List<BattleCharacter> { target }, data, target); }
        finally { EffectEngine.CurrentCast = null; }
    }

    private ActionDicePool Dice(int value)
    {
        var pool = new ActionDicePool(f => value); pool.BeginTurn(1);
        typeof(BattleManager).GetField("_dicePool", Private).SetValue(battle, pool);
        var selected = (List<int>)typeof(BattleManager).GetField("_selectedDiceIds", Private).GetValue(battle);
        selected.Clear(); selected.Add(pool.Dice[0].Id);
        return pool;
    }

    private void Fire(BattleCharacter hero, int slot, BattleCharacter target)
    {
        var selection = (BattleSelectionState)typeof(BattleManager).GetField("_selection", Private).GetValue(battle);
        selection.Set(Skill(hero, slot), hero);
        typeof(BattleManager).GetMethod("PerformSkillOnTarget", Private).Invoke(battle, new object[] { target });
    }

    [TestCase(false,30)] [TestCase(true,50)]
    public void CapacityAndRunPersistenceFollowBloom(bool bloom,int cap)
    {
        var h=Hero(bloom:bloom);h.status.GainPrimalMana(100);Assert.AreEqual(cap,h.status.PrimalMana);Assert.AreEqual(cap,h.status.PrimalManaCapacity);
        var run=new RunState();run.RememberPrimalMana(h.status);var restored=new CharacterStatus(h.status.origin){traitBloomOverride=bloom?1:0};run.RestorePrimalMana(restored);Assert.AreEqual(cap,restored.PrimalMana);
    }
    [TestCase(0,10,4,11,14)] [TestCase(1,10,4,13,14)] [TestCase(2,30,6,25,30)]
    public void BoltHitsOnceAndGrantsDieManaAfterDamage(int rank,int mana,int die,int damage,int after)
    {var h=Hero(rank,mana);var e=Enemy();int hits=0;e.DamageResolved+=x=>hits++;Cast(h,0,e,die);Assert.AreEqual(1000-damage,e.status.currentHp);Assert.AreEqual(after,h.status.PrimalMana);Assert.AreEqual(1,hits);}
    [TestCase(0,2,4)] [TestCase(1,2,6)] [TestCase(2,2,8)] [TestCase(2,3,8)] [TestCase(2,0,4)]
    public void GatherAddsActiveAndPassiveManaButOnlyDieShield(int rank,int die,int mana)
    {var h=Hero(rank);Cast(h,1,h,die);Assert.AreEqual(mana,h.status.PrimalMana);Assert.AreEqual(die,h.status.GetStatusValue(EffectType.Shield));}
    [TestCase(false,4)] [TestCase(true,5)]
    public void ARepeatedExecutionAwardsPassiveOnlyForTheOriginalDie(bool bloom,int mana)
    {
        var h=Hero(bloom:bloom);var e=Enemy();var cast=new SkillCast();cast.EffectValues.Add(4);
        for(int i=0;i<2;i++){EffectEngine.CurrentCast=cast;EffectEngine.ProcessSkill(h,new List<BattleCharacter>{e},Skill(h,0).levels[0],e);}
        EffectEngine.CurrentCast=null;Assert.AreEqual(mana,h.status.PrimalMana);
    }
    [TestCase(19,26,26,0)] [TestCase(20,17,0,6)] [TestCase(29,26,0,6)]
    public void LandUsesTwentyThresholdBeforePassive(int before,int after,int shield,int damage)
    {var h=Hero(mana:before);var e=Enemy();Cast(h,2,e,2);Assert.AreEqual(after,h.status.PrimalMana);Assert.AreEqual(shield,h.status.GetStatusValue(EffectType.Shield));Assert.AreEqual(1000-damage,e.status.currentHp);}
    [TestCase(false,27,34)] [TestCase(true,33,39)]
    public void CombinedLandGainsFiveThenShieldsThenSpendsFive(bool bloom,int after,int shield)
    {var h=Hero(2,30,bloom);var e=Enemy();Cast(h,2,e,2);Assert.AreEqual(after,h.status.PrimalMana);Assert.AreEqual(shield,h.status.GetStatusValue(EffectType.Shield));Assert.AreEqual(991,e.status.currentHp);}
    [Test] public void RootCannotSpendAnythingBelowThirty()
    {var h=Hero(2,29);var e=Enemy();Cast(h,3,e,6);Assert.AreEqual(29,h.status.PrimalMana);Assert.AreEqual(1000,e.status.currentHp);}
    [TestCase(30,false,22,10,6)] [TestCase(50,true,65,30,7)]
    public void RootHalfManaScalingAndFinalMultiplierApplyAfterEachResistance(int mana,bool bloom,int primary,int secondary,int after)
    {var h=Hero(2,mana,bloom);var e=Enemy(2);var other=Enemy(2);Cast(h,3,e,6);Assert.AreEqual(1000-primary,e.status.currentHp);Assert.AreEqual(1000-secondary,other.status.currentHp);Assert.AreEqual(after,h.status.PrimalMana);}
    [Test] public void RootSoloBonusAndBaseInputRestriction()
    {var h=Hero(mana:30);var e=Enemy();Cast(h,3,e,6);Assert.AreEqual(963,e.status.currentHp);Assert.IsTrue(Skill(h,3).levels[0].diceInput.AcceptsValue(6));Assert.IsFalse(Skill(h,3).levels[0].diceInput.AcceptsValue(1));Assert.IsTrue(Skill(h,3).levels[1].diceInput.AcceptsValue(1));}
    [TestCase(0,7)] [TestCase(2,4)]
    public void ClickRestoreSpendsOnlyManaAndNeedsANewDieToCast(int rank,int cost)
    {
        var h=Hero(rank,10);var e=Enemy();var pool=Dice(4);pool.AddDie(new DieSpec(6));Fire(h,0,e);
        Assert.AreEqual(1,battle.GetSkillUsesThisTurn(h,Skill(h,0)));Assert.AreEqual(14,h.status.PrimalMana);
        int dice=pool.Dice.Count;Assert.IsTrue(battle.TryRestoreSkill(h,Skill(h,0)));Assert.AreEqual(14-cost,h.status.PrimalMana);Assert.AreEqual(dice,pool.Dice.Count);
        Assert.IsFalse(battle.TryRestoreSkill(h,Skill(h,0)));float hp=e.status.currentHp;Fire(h,0,e);Assert.AreEqual(hp,e.status.currentHp);
        Dice(1);Fire(h,0,e);Assert.Less(e.status.currentHp,hp);
    }
    [Test] public void CannotRestoreWithInsufficientManaOrDuringResolution()
    {
        var h=Hero();var e=Enemy();var pool=Dice(1);pool.AddDie(new DieSpec(6));Fire(h,0,e);Assert.IsFalse(battle.TryRestoreSkill(h,Skill(h,0)));
        h.status.GainPrimalMana(10);typeof(BattleManager).GetField("_resolvingSkill",Private).SetValue(battle,true);Assert.IsFalse(battle.TryRestoreSkill(h,Skill(h,0)));
        typeof(BattleManager).GetField("_resolvingSkill",Private).SetValue(battle,false);Assert.IsTrue(battle.TryRestoreSkill(h,Skill(h,0)));
    }
    [Test] public void ChargeInsertionAwardsEffectiveDieManaOnlyOnce()
    {
        var h=Hero(bloom:true);var e=Enemy();var skill=Skill(h,0);skill.levels[0].diceInput.chargeTarget=4;
        Dice(4);typeof(BattleManager).GetMethod("TryInsertChargeDice",Private).Invoke(battle,new object[]{h,skill});Assert.AreEqual(5,h.status.PrimalMana);
        Fire(h,0,e);Assert.AreEqual(5,h.status.PrimalMana);
    }
    [Test] public void PassiveUsesEffectValueAfterBoostFrostAndDepression()
    {
        var h=Hero(bloom:true);var e=Enemy();var pool=Dice(5);pool.AddDie(new DieSpec(6));
        h.status.ApplyStatusEffect(EffectType.Frost,2,1);
        h.status.ApplyStatusEffect(EffectType.DiceBoost,3,1,0,1,-1,h,StatusDurationType.Turns);
        h.status.panic.Begin(PanicType.Depression);
        Fire(h,0,e);Assert.AreEqual(4,h.status.PrimalMana,"floor((5+3-2)/2) + bloom 1");
    }

}
