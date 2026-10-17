using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleDebugTests
{
    private readonly List<Object> owned = new List<Object>();

    [TearDown]
    public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    private BattleCharacter Character()
    {
        var data = ScriptableObject.CreateInstance<CharacterData>();
        owned.Add(data);
        data.isEnemy = true;
        data.maxHp = 100f;
        data.enemyRuntimeVariables = new List<EnemyRuntimeVariableDefinition>
        {
            new EnemyRuntimeVariableDefinition { id = "counter", initialValue = 2 }
        };
        data.enemyPatterns = new List<EnemyPatternData>();
        for (int i = 0; i < 2; i++) data.enemyPatterns.Add(new EnemyPatternData
        {
            patternName = "Pattern " + i,
            targetType = TargetType.Self,
            effects = new List<EffectEntry> { new EffectEntry { type = EffectType.Shield, value = 1 } }
        });
        var root = new GameObject("DebugTestCharacter");
        owned.Add(root);
        var character = root.AddComponent<BattleCharacter>();
        character.status = new CharacterStatus(data);
        return character;
    }

    [Test]
    public void SelectingPatternPreservesRuntimeStateAndAdvancesFromSelection()
    {
        var character = Character();
        var target = Character();
        var ai = character.gameObject.AddComponent<EnemyAI>();
        ai.allyParty = new List<BattleCharacter> { character };
        ai.targetParty = new List<BattleCharacter> { target };
        var runtime = ai.RuntimeState;
        runtime.Set("counter", 7);
        runtime.Remember(target);
        runtime.BeginDamageCheck(target);

        Assert.IsTrue(ai.DebugSelectPattern(1));
        Assert.AreSame(runtime, ai.RuntimeState);
        Assert.AreEqual(7, runtime.Get("counter"));
        Assert.AreSame(target, runtime.RememberedTarget);
        Assert.AreSame(target, runtime.TrackedTarget);
        Assert.AreEqual("Pattern 1", ai.PreparedIntent.PatternName);
        ai.ExecuteTurn();
        Assert.AreEqual(0, ai.DebugPatternIndex);
        Assert.IsFalse(ai.HasPreparedTurn);
    }

    [Test]
    public void InvalidPatternIndexDoesNotReplacePreparedIntent()
    {
        var character = Character();
        var ai = character.gameObject.AddComponent<EnemyAI>();
        ai.allyParty = new List<BattleCharacter> { character };
        ai.targetParty = new List<BattleCharacter>();
        Assert.IsTrue(ai.DebugSelectPattern(0));
        var intent = ai.PreparedIntent;
        Assert.IsFalse(ai.DebugSelectPattern(-1));
        Assert.IsFalse(ai.DebugSelectPattern(2));
        Assert.AreSame(intent, ai.PreparedIntent);
        Assert.AreEqual(0, ai.DebugPatternIndex);
    }

    [Test]
    public void PracticeHoldsWipedPartiesAndCannotChangeDuringEnemyTurn()
    {
        var root = new GameObject("DebugTestManager");
        owned.Add(root);
        var manager = root.AddComponent<BattleManager>();
        manager.enemyParty = new List<BattleCharacter>();
        manager.playerParty = new List<BattleCharacter>();
        var phase = typeof(BattleManager).GetField("_currentPhase", BindingFlags.NonPublic | BindingFlags.Instance);
        phase.SetValue(manager, BattlePhase.PlayerTurn);
        manager.DebugSetPracticeMode(true);
        manager.DebugCheckBattleEnd();
        Assert.IsTrue(manager.IsPlayerTurn);
        phase.SetValue(manager, BattlePhase.EnemyTurn);
        manager.DebugSetPracticeMode(false);
        Assert.IsTrue(manager.DebugHoldsBattleOpen);
        Assert.IsFalse(manager.DebugExecutePattern(null));
    }
}
