using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TheLastArk.Skills;

public class SkillDebugTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
    private BattleManager manager;
    private BattleCharacter actor, enemy;

    [SetUp] public void Setup()
    {
        manager = Own(new GameObject("Skill debug manager")).AddComponent<BattleManager>();
        actor = Character(false); enemy = Character(true);
        manager.playerParty = new List<BattleCharacter> { actor };
        manager.enemyParty = new List<BattleCharacter> { enemy };
        typeof(BattleManager).GetField("_currentPhase", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, BattlePhase.PlayerTurn);
    }
    private BattleCharacter Character(bool isEnemy)
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.characterName = isEnemy ? "Target" : "Caster";
        data.isEnemy = isEnemy; data.maxHp = 100; data.maxMental = 30;
        data.activeSkills = new SkillInfo[4];
        for (int i = 0; i < 4; i++)
        {
            data.activeSkills[i] = new SkillInfo { skillName = "Skill " + i, levels = new SkillLevelData[3] };
            for (int r = 0; r < 3; r++)
                data.activeSkills[i].levels[r] = new SkillLevelData { targetType = TargetType.SingleEnemy,
                    effects = new List<EffectEntry> { new EffectEntry { type = EffectType.Damage, damageType = DamageType.True, multiplier = 0, fixedValue = 4 + 4 * r } } };
        }
        var character = Own(new GameObject(data.characterName)).AddComponent<BattleCharacter>();
        character.status = new CharacterStatus(data);
        character.status.dynamicActiveSkill.Clear();
        character.status.dynamicActiveSkill.Add(data.activeSkills[0]);
        return character;
    }
    [TearDown] public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear(); EffectEngine.CurrentCast = null; TheLastArk.Battle.AttackContext.ResetAll();
    }
    [Test] public void UnequippedFourthSkillUsesSelectedRankWithoutChangingLoadout()
    {
        int rank = actor.status.SkillLevelIndex;
        Assert.IsTrue(manager.DebugPlaySkill(actor, 3, 2, false, enemy, 6, true));
        Assert.AreEqual(88, enemy.status.currentHp);
        Assert.AreEqual(1, actor.status.dynamicActiveSkill.Count);
        Assert.AreSame(actor.status.origin.activeSkills[0], actor.status.dynamicActiveSkill[0]);
        Assert.AreEqual(rank, actor.status.SkillLevelIndex);
        Assert.IsTrue(manager.IsPlayerTurn);
        Assert.IsTrue(manager.DebugHoldsBattleOpen);
        Assert.IsFalse(manager.DebugActionRunning);
    }
    [Test] public void VisualOnlyDoesNotDamageSpendResourcesOrHoldBattleOpen()
    {
        var level = actor.status.origin.activeSkills[3].levels[0];
        level.primalManaRequired = 15; level.consumeAllPrimalMana = true;
        actor.status.GainPrimalMana(20);
        Assert.IsTrue(manager.DebugPlaySkill(actor, 3, 0, false, enemy, 6, false));
        Assert.AreEqual(100, enemy.status.currentHp);
        Assert.AreEqual(20, actor.status.PrimalMana);
        Assert.IsFalse(manager.DebugHoldsBattleOpen);
    }
    [Test] public void PreviewAllowsResourceGatedArtButActualEffectsRequireMana()
    {
        actor.status.origin.activeSkills[0].levels[0].primalManaRequired = 15;
        Assert.IsNull(manager.DebugSkillReason(actor, 0, 0, false, enemy, false));
        Assert.IsNotNull(manager.DebugSkillReason(actor, 0, 0, false, enemy, true));
        Assert.IsFalse(manager.DebugPlaySkill(actor, 0, 0, false, enemy, 6, true));
        Assert.IsTrue(manager.DebugFillSkillResources(actor));
        Assert.IsNull(manager.DebugSkillReason(actor, 0, 0, false, enemy, true));
        manager.DebugFillSkillResources(actor, true);
        Assert.AreEqual(0, actor.status.PrimalMana);
    }
    [Test] public void FriendlyExcludeSelfAndBusyPhaseAreRejected()
    {
        var level = actor.status.origin.activeSkills[0].levels[0];
        level.targetType = TargetType.Friendly; level.excludeSelf = true;
        Assert.IsNotNull(manager.DebugSkillReason(actor, 0, 0, false, actor, false));
        Assert.IsNotNull(manager.DebugSkillReason(actor, 0, 0, false, enemy, false));
        typeof(BattleManager).GetField("_currentPhase", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, BattlePhase.EnemyTurn);
        Assert.IsFalse(manager.DebugPlaySkill(actor, 0, 0, false, enemy, 6, false));
    }
    [Test] public void FirstAndRepeatStageDoNotMutateAuthoringData()
    {
        var level = actor.status.origin.activeSkills[0].levels[0];
        level.special = SkillSpecial.ElectromagneticField;
        level.effects.Add(new EffectEntry { type = EffectType.Shield, value = 10 });
        var first = manager.DebugSkillLevel(actor, 0, 0, false);
        var second = manager.DebugSkillLevel(actor, 0, 0, true);
        Assert.AreEqual(TargetType.Self, first.targetType);
        Assert.AreEqual(TargetType.AllEnemy, second.targetType);
        Assert.AreEqual(1, first.effects.Count); Assert.AreEqual(1, second.effects.Count);
        Assert.AreEqual(SkillSpecial.ElectromagneticField, level.special);
        Assert.AreEqual(2, level.effects.Count);
    }
    [Test] public void MaterialPreservationOptionCopiesIndependently()
    {
        var settings = new SkillVfxSettings { preservePrefabMaterials = true };
        var copy = settings.Copy();
        Assert.IsTrue(copy.preservePrefabMaterials);
        copy.preservePrefabMaterials = false;
        Assert.IsTrue(settings.preservePrefabMaterials);
    }
}
