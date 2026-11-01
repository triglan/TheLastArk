using NUnit.Framework;
using TheLastArk.Data;
using TheLastArk.Run;
using TheLastArk.Battle;
using UnityEngine;

public class ArkProgressionTests
{
    private ArkProgressionConfig config;
    [SetUp] public void Setup(){config=ScriptableObject.CreateInstance<ArkProgressionConfig>();}
    [TearDown] public void Cleanup(){Object.DestroyImmediate(config);}

    [Test] public void PurchasesAreAtomicAndStopAtMaximum()
    {
        var data=new ArkProgressData();var node=config.Find("leader_health");string message;
        Assert.IsFalse(data.TryPurchase(config,node.id,out message));Assert.AreEqual(0,data.Level(node.id));
        data.AddMemory(380);
        Assert.IsTrue(data.TryPurchase(config,node.id,out message));Assert.AreEqual(355,data.memory);
        Complete(data,node.id);
        Assert.AreEqual(0,data.memory);Assert.AreEqual(5,data.Level(node.id));
        data.AddMemory(100);Assert.IsFalse(data.TryPurchase(config,node.id,out message));Assert.AreEqual(100,data.memory);
    }
    [Test] public void UnknownAndUnfinishedNodesNeverConsumeMemory()
    {
        var data=new ArkProgressData();data.AddMemory(500);string message;
        Assert.IsFalse(data.TryPurchase(config,null,out message));
        for(int i=1;i<=3;i++)Assert.IsFalse(data.TryPurchase(config,"character_"+i,out message));
        Assert.AreEqual(500,data.memory);Assert.AreEqual(0,data.levels.Count);
    }
    [Test] public void MemoryAndUnlocksRoundTripAcrossSessions()
    {
        var data=new ArkProgressData();data.AddMemory(610);string message;
        Complete(data,"provisions");
        Assert.IsTrue(data.TryPurchase(config,"relic_trace",out message));
        Assert.IsTrue(data.TryPurchase(config,ArkProgressionConfig.RelicNode,out message));
        var loaded=JsonUtility.FromJson<ArkProgressData>(JsonUtility.ToJson(data));
        Assert.AreEqual(50,loaded.memory);Assert.AreEqual(1,loaded.Level(ArkProgressionConfig.RelicNode));
        Assert.AreEqual(0,loaded.Level(ArkProgressionConfig.TrainNode));
    }
    [Test] public void SettlementPaysOnlyOnceAndCountsVictoryOncePerNode()
    {
        var data=new ArkProgressData();data.BeginRun();data.RecordVisit(4);
        Assert.IsTrue(data.RecordVictory("2",NodeType.Combat));Assert.IsFalse(data.RecordVictory("2",NodeType.Combat));
        data.RecordVictory("3",NodeType.Elite);data.RecordVictory("4",NodeType.Boss);
        Assert.AreEqual(65,data.Settle(config));Assert.AreEqual(0,data.Settle(config));Assert.AreEqual(65,data.memory);
        data.BeginRun();Assert.AreEqual(0,data.Settle(config));Assert.AreEqual(65,data.memory);
    }
    [Test] public void NewRunKeepsUpgradesButClearsJourneyRecords()
    {
        var data=new ArkProgressData();data.AddMemory(100);string message;data.TryPurchase(config,"leader_health",out message);
        data.BeginRun();data.RecordVisit(3);data.RecordVictory("1",NodeType.Combat);data.Settle(config);data.BeginRun();
        Assert.AreEqual(1,data.Level("leader_health"));Assert.AreEqual(0,data.normalWins);Assert.AreEqual(0,data.furthestFloor);Assert.IsEmpty(data.rewardedBattles);
    }
    [Test] public void ExtraDiceOnlyAffectFirstTurnAndCanExceedTen()
    {
        var benefit=new ArkDiceBenefits(1,1,1);var pool=new ActionDicePool(_=>3);
        pool.BeginTurn(4+benefit.ExtraDiceForTurn(1));Assert.AreEqual(5,pool.Dice.Count);
        pool.BeginTurn(4+benefit.ExtraDiceForTurn(2));Assert.AreEqual(4,pool.Dice.Count);
        pool.BeginTurn(10+benefit.ExtraDiceForTurn(1));Assert.AreEqual(11,pool.Dice.Count);
    }
    [Test] public void InvalidDiceDoNotSpendBattleCharges()
    {
        var benefit=new ArkDiceBenefits(0,1,1);var pool=new ActionDicePool(_=>3);pool.Roll(1);int id=pool.Dice[0].Id;
        pool.SetLocked(id,true);Assert.IsFalse(benefit.TryEnhance(pool,id));Assert.IsFalse(benefit.TryReroll(pool,id));
        Assert.IsFalse(benefit.TryEnhance(pool,999));Assert.AreEqual(1,benefit.EnhancesLeft);Assert.AreEqual(1,benefit.RerollsLeft);
        pool.AddDie(new DieSpec(6,DieKind.Cloud,3));Assert.IsFalse(benefit.TryReroll(pool,pool.Dice[1].Id));Assert.AreEqual(1,benefit.RerollsLeft);
    }
    [Test] public void ChargesPersistAcrossTurnsAndRerollPreservesEnhancement()
    {
        var benefit=new ArkDiceBenefits(0,1,1);int roll=2;var pool=new ActionDicePool(_=>roll++);pool.Roll(1);int id=pool.Dice[0].Id;
        Assert.IsTrue(benefit.TryEnhance(pool,id));Assert.AreEqual(3,pool.Find(id).Value);
        Assert.IsTrue(benefit.TryReroll(pool,id));Assert.AreEqual(4,pool.Find(id).Value);
        pool.BeginTurn(1);Assert.IsFalse(benefit.TryEnhance(pool,pool.Dice[0].Id));Assert.IsFalse(benefit.TryReroll(pool,pool.Dice[0].Id));
        var nextBattle=new ArkDiceBenefits(0,1,1);Assert.IsTrue(nextBattle.TryEnhance(pool,pool.Dice[0].Id));
    }
    [Test] public void LeaderBonusesDoNotStackOrHealByReordering()
    {
        var origin=ScriptableObject.CreateInstance<CharacterData>();origin.maxHp=30;origin.maxMental=20;
        try
        {
            var leader=new CharacterStatus(origin);float hp=leader.FinalMaxHp,mental=leader.FinalMaxMental;
            leader.ApplyArkLeaderBonuses(3,2);leader.ApplyArkLeaderBonuses(3,2);
            Assert.AreEqual(hp+3,leader.FinalMaxHp);Assert.AreEqual(mental+2,leader.FinalMaxMental);
            var ally=new CharacterStatus(origin);Assert.AreEqual(hp,ally.FinalMaxHp);
            leader.currentHp=2;leader.currentMental=0;
            leader.ApplyArkLeaderBonuses(0,0);leader.ApplyArkLeaderBonuses(3,2);
            Assert.AreEqual(2,leader.currentHp);Assert.AreEqual(0,leader.currentMental);
            origin.isEnemy=true;leader.ApplyArkLeaderBonuses(3,2);Assert.AreEqual(0,leader.FinalMaxHp-hp);
        }
        finally{Object.DestroyImmediate(origin);}
    }
    [Test] public void RunSnapshotAppliesOnlyToLeaderWhenBattleCreatesFreshStatuses()
    {
        var leaderData=ScriptableObject.CreateInstance<CharacterData>();leaderData.characterId="ArkLeader";
        var allyData=ScriptableObject.CreateInstance<CharacterData>();allyData.characterId="ArkAlly";
        try
        {
            var run=new RunState { leaderCharacterID=leaderData.DataId, arkLeaderHealth=3, arkLeaderMental=1, arkLeaderArmor=1, arkLeaderMagicResist=1, arkLeaderAttack=3, arkLeaderSpellPower=2 };
            var leader=new CharacterStatus(leaderData);var ally=new CharacterStatus(allyData);
            float leaderHp=leader.FinalMaxHp,leaderMental=leader.FinalMaxMental,allyHp=ally.FinalMaxHp;
            float leaderArmor=leader.FinalArmor,allyArmor=ally.FinalArmor,leaderResist=leader.FinalMagicResist;
            float attack=leader.FinalAttack,spell=leader.FinalSpellPower,allyAttack=ally.FinalAttack,allySpell=ally.FinalSpellPower;
            run.ApplyArkLeaderBonuses(leader);run.ApplyArkLeaderBonuses(ally);
            Assert.AreEqual(leaderHp+3,leader.FinalMaxHp);Assert.AreEqual(leaderMental+1,leader.FinalMaxMental);
            Assert.AreEqual(allyHp,ally.FinalMaxHp);
            Assert.AreEqual(leaderArmor+1,leader.FinalArmor);Assert.AreEqual(allyArmor,ally.FinalArmor);Assert.AreEqual(leaderResist+1,leader.FinalMagicResist);
            Assert.AreEqual(attack+3,leader.FinalAttack);Assert.AreEqual(spell+2,leader.FinalSpellPower);Assert.AreEqual(allyAttack,ally.FinalAttack);Assert.AreEqual(allySpell,ally.FinalSpellPower);
            run.leaderCharacterID=allyData.DataId;
            run.ApplyArkLeaderBonuses(leader);run.ApplyArkLeaderBonuses(ally);
            Assert.AreEqual(leaderHp,leader.FinalMaxHp);Assert.AreEqual(allyHp+3,ally.FinalMaxHp);
            Assert.AreEqual(leaderArmor,leader.FinalArmor);Assert.AreEqual(allyArmor+1,ally.FinalArmor);Assert.AreEqual(leaderResist,leader.FinalMagicResist);
            Assert.AreEqual(attack,leader.FinalAttack);Assert.AreEqual(spell,leader.FinalSpellPower);Assert.AreEqual(allyAttack+3,ally.FinalAttack);Assert.AreEqual(allySpell+2,ally.FinalSpellPower);
        }
        finally{Object.DestroyImmediate(leaderData);Object.DestroyImmediate(allyData);}
    }
    [Test] public void UpperNodeRequiresCompletedParentAndNeverSpendsOnFailure()
    {
        var data=new ArkProgressData();data.AddMemory(1000);string message;
        Assert.IsFalse(data.TryPurchase(config,"leader_mental",out message));Assert.AreEqual(1000,data.memory);
        Assert.IsTrue(data.TryPurchase(config,"leader_health",out message));
        int balance=data.memory;Assert.IsFalse(data.TryPurchase(config,"leader_mental",out message));Assert.AreEqual(balance,data.memory);
        Complete(data,"leader_health");
        Assert.IsTrue(data.TryPurchase(config,"leader_mental",out message));
        Assert.IsTrue(data.TryPurchase(config,"health_2",out message),"A completed parent opens both branches");
        Assert.IsFalse(data.TryPurchase(config,"leader_resist",out message),"Cannot skip a deeper node");
    }

    [Test] public void OtherBranchesRemainIndependentAndCharacterEndpointsStayReserved()
    {
        var data=new ArkProgressData();data.AddMemory(2000);string message;
        Complete(data,"crew_signal");Complete(data,"crew_path_1");
        int balance=data.memory;Assert.IsFalse(data.TryPurchase(config,"character_1",out message));Assert.AreEqual(balance,data.memory);
        Assert.IsFalse(data.TryPurchase(config,"relic_summon",out message));
        Assert.IsTrue(data.TryPurchase(config,"extra_die",out message));
        Assert.IsTrue(data.TryPurchase(config,"reroll",out message));
        Assert.IsTrue(data.TryPurchase(config,"reroll_2",out message));Assert.AreEqual(2,data.Amount(config,ArkNodeKind.Reroll));
        Assert.AreEqual(25,data.Amount(config,ArkNodeKind.StartingGold));Assert.AreEqual(3,data.Amount(config,ArkNodeKind.LeaderAttack));
        Assert.AreEqual(0,data.Level("provisions"));Assert.AreEqual(0,data.Level("leader_health"));
    }

    [Test] public void ExistingPurchasedNodesSurviveNewPrerequisitesAndJsonReload()
    {
        var data=JsonUtility.FromJson<ArkProgressData>("{\"memory\":100,\"levels\":[{\"id\":\"leader_mental\",\"level\":1},{\"id\":\"relic_summon\",\"level\":1}]}");
        string message;Assert.IsTrue(data.TryPurchase(config,"leader_mental",out message));
        Assert.AreEqual(2,data.Amount(config,ArkNodeKind.LeaderMental));Assert.AreEqual(1,data.Level("relic_summon"));
    }

    [Test] public void AdditionalVictoryMemoryIsSnapshottedAndSettlesOnce()
    {
        var data=new ArkProgressData();data.AddMemory(1000);string message;
        Complete(data,"provisions");data.TryPurchase(config,"relic_trace",out message);
        data.BeginRun(config);data.TryPurchase(config,"train_trace",out message);
        data.RecordVictory("a",NodeType.Combat);data.RecordVictory("b",NodeType.Elite);data.RecordVictory("c",NodeType.Boss);
        Assert.AreEqual(53,data.Settle(config));Assert.AreEqual(0,data.Settle(config));
        data.BeginRun(config);data.RecordVictory("a",NodeType.Combat);Assert.AreEqual(7,data.Settle(config));
    }

    [Test] public void ConstellationGraphHasValidAcyclicPathsAndThreeCharacterEndpoints()
    {
        var ids=new System.Collections.Generic.HashSet<string>();int characters=0,roots=0;
        foreach(var node in config.nodes)
        {
            Assert.IsTrue(ids.Add(node.id),"Duplicate node: "+node.id);
            if(node.placeholder){characters++;Assert.AreEqual(ArkNodeKind.Character,node.kind);}
            if(node.prerequisites.Length==0)roots++;
            CheckPath(node,new System.Collections.Generic.HashSet<string>());
        }
        Assert.AreEqual(25,ids.Count);Assert.AreEqual(3,characters);Assert.AreEqual(4,roots);
    }

    private void CheckPath(ArkNodeDefinition node,System.Collections.Generic.HashSet<string> path)
    {
        Assert.IsTrue(path.Add(node.id),"Cycle at "+node.id);
        foreach(string id in node.prerequisites)
        {
            var parent=config.Find(id);Assert.IsNotNull(parent,"Missing predecessor "+id);Assert.IsFalse(parent.placeholder);
            CheckPath(parent,new System.Collections.Generic.HashSet<string>(path));
        }
    }

    private void Complete(ArkProgressData data,string id)
    {
        while(data.Level(id)<config.Find(id).MaxLevel)
        { string message;Assert.IsTrue(data.TryPurchase(config,id,out message),message); }
    }

    [Test] public void StatAndGoldRanksAndDistinctCrewRoutesMatchTheirLimits()
    {
        foreach(var node in config.nodes)
            if(node.kind==ArkNodeKind.LeaderHealth || node.kind==ArkNodeKind.LeaderMental || node.kind==ArkNodeKind.StartingGold)
                Assert.AreEqual(5,node.MaxLevel,node.id);
        Assert.AreEqual(1,config.Find("leader_armor").MaxLevel);Assert.AreEqual(1,config.Find("leader_resist").MaxLevel);
        Assert.AreEqual(ArkNodeKind.LeaderAttack,config.Find("crew_path_1").kind);
        Assert.AreEqual(ArkNodeKind.LeaderSpellPower,config.Find("crew_path_2").kind);
        Assert.AreEqual(ArkNodeKind.VictoryMemory,config.Find("crew_path_3").kind);
        foreach(string id in new[]{"crew_path_1","crew_path_2"})
        {Assert.AreEqual(3,config.Find(id).MaxLevel);Assert.Greater(config.Find(id).CostAt(0),config.Find("leader_health").CostAt(0));}
    }

    [Test] public void ConstellationLayersFitUnityMeshBudget()
    {
        foreach(TheLastArk.UI.ArkVisual type in System.Enum.GetValues(typeof(TheLastArk.UI.ArkVisual)))
        {
            var go=new GameObject("Ark mesh",typeof(RectTransform));var graphic=go.AddComponent<TheLastArk.UI.ArkConstellationGraphic>();
            try
            {
                graphic.rectTransform.sizeDelta=new Vector2(1920,1080);graphic.visual=type;
                using(var helper=new UnityEngine.UI.VertexHelper())
                {
                    typeof(TheLastArk.UI.ArkConstellationGraphic).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance,null,new[]{typeof(UnityEngine.UI.VertexHelper)},null).Invoke(graphic,new object[]{helper});
                    Assert.Greater(helper.currentVertCount,0);Assert.Less(helper.currentVertCount,65000,type.ToString());
                }
                Assert.IsNotNull(go.GetComponent<CanvasRenderer>());
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
