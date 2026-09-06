using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EnemyPatternCombatTests
{
    [Test]
    public void DamageUsesZeroMinimumCeilingAndPerHitDefense()
    {
        var attackerObject = new GameObject("Attacker_Test");
        var targetObject = new GameObject("Target_Test");
        var attackerData = ScriptableObject.CreateInstance<CharacterData>();
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            attackerData.isEnemy = true;
            targetData.maxHp = 20f;
            targetData.armor = 3f;
            targetData.magicResist = 3f;

            var attacker = attackerObject.AddComponent<BattleCharacter>();
            var target = targetObject.AddComponent<BattleCharacter>();
            attacker.status = new CharacterStatus(attackerData);
            target.status = new CharacterStatus(targetData);

            DamageResult blocked = target.ReceiveDamageDetailed(3f, attacker, DamageType.Physical);
            Assert.AreEqual(0f, blocked.HealthDamage);
            Assert.AreEqual(20f, target.status.currentHp);

            DamageResult rounded = target.ReceiveDamageDetailed(3.1f, attacker, DamageType.Physical);
            Assert.AreEqual(1f, rounded.HealthDamage);
            Assert.AreEqual(19f, target.status.currentHp);

            var multiHit = new SkillLevelData
            {
                effects = new List<EffectEntry>
                {
                    new EffectEntry
                    {
                        type = EffectType.Damage,
                        damageType = DamageType.Physical,
                        multiplier = 0f,
                        fixedValue = 4f,
                        hitCount = 2
                    }
                }
            };
            EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, multiHit, "연타 테스트");

            Assert.AreEqual(17f, target.status.currentHp);
        }
        finally
        {
            Object.DestroyImmediate(attackerObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(attackerData);
            Object.DestroyImmediate(targetData);
        }
    }

    [Test]
    public void DetailedDamageSeparatesShieldAndHealthDamage()
    {
        var targetObject = new GameObject("Target_Test");
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            targetData.maxHp = 20f;
            targetData.armor = 0f;
            var target = targetObject.AddComponent<BattleCharacter>();
            target.status = new CharacterStatus(targetData);
            target.status.ApplyStatusEffect(EffectType.Shield, 3f, 1);

            DamageResult result = target.ReceiveDamageDetailed(5f, null, DamageType.Physical, false);

            Assert.AreEqual(5f, result.ResolvedDamage);
            Assert.AreEqual(3f, result.ShieldDamage);
            Assert.AreEqual(2f, result.HealthDamage);
            Assert.AreEqual(18f, target.status.currentHp);
        }
        finally
        {
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(targetData);
        }
    }

    [Test]
    public void MentalDamageIgnoresDefenseRoundsUpAndRaisesDepletedOnce()
    {
        var targetObject = new GameObject("Target_Test");
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            targetData.maxMental = 5f;
            targetData.armor = 99f;
            targetData.magicResist = 99f;
            var target = targetObject.AddComponent<BattleCharacter>();
            target.status = new CharacterStatus(targetData);
            target.status.ApplyStatusEffect(EffectType.Shield, 99f, 1);
            int depletedCount = 0;
            target.MentalDepleted += _ => depletedCount++;

            Assert.AreEqual(2f, target.TakeMentalDamage(1.1f));
            Assert.AreEqual(3f, target.status.currentMental);
            Assert.AreEqual(99f, target.status.GetStatus(EffectType.Shield).damagePerTurn);

            Assert.AreEqual(3f, target.TakeMentalDamage(10f));
            Assert.AreEqual(0f, target.status.currentMental);
            Assert.AreEqual(1, depletedCount);

            Assert.AreEqual(0f, target.TakeMentalDamage(1f));
            Assert.AreEqual(1, depletedCount);
        }
        finally
        {
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(targetData);
        }
    }

    [Test]
    public void MentalEffectsAndEnemyHealingWorkThroughEffectEngine()
    {
        var actorObject = new GameObject("EnemyHealer_Test");
        var targetObject = new GameObject("EnemyTarget_Test");
        var actorData = ScriptableObject.CreateInstance<CharacterData>();
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            actorData.isEnemy = true;
            targetData.isEnemy = true;
            targetData.maxHp = 20f;
            targetData.maxMental = 10f;
            var actor = actorObject.AddComponent<BattleCharacter>();
            var target = targetObject.AddComponent<BattleCharacter>();
            actor.status = new CharacterStatus(actorData);
            target.status = new CharacterStatus(targetData);
            target.status.currentHp = 10f;
            target.status.currentMental = 10f;

            var mentalDamage = new SkillLevelData
            {
                effects = new List<EffectEntry>
                {
                    new EffectEntry { type = EffectType.MentalDamage, multiplier = 0f, fixedValue = 2f, hitCount = 2 }
                }
            };
            EffectEngine.ProcessSkill(actor, new List<BattleCharacter> { target }, mentalDamage, "정신 공격");
            Assert.AreEqual(6f, target.status.currentMental);

            var heal = new SkillLevelData
            {
                effects = new List<EffectEntry>
                {
                    new EffectEntry { type = EffectType.Heal, multiplier = 0f, fixedValue = 6f }
                }
            };
            EffectEngine.ProcessSkill(actor, new List<BattleCharacter> { target }, heal, "적 회복");
            Assert.AreEqual(16f, target.status.currentHp);

            var mentalHeal = new SkillLevelData
            {
                effects = new List<EffectEntry>
                {
                    new EffectEntry { type = EffectType.MentalHeal, multiplier = 0f, fixedValue = 3f }
                }
            };
            EffectEngine.ProcessSkill(actor, new List<BattleCharacter> { target }, mentalHeal, "정신 회복");
            Assert.AreEqual(9f, target.status.currentMental);
        }
        finally
        {
            Object.DestroyImmediate(actorObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(actorData);
            Object.DestroyImmediate(targetData);
        }
    }

    [Test]
    public void FocusBypassesTauntAndFixedDamageRepeatsPerHit()
    {
        var managerObject = new GameObject("BattleManager_Test");
        var attackerObject = new GameObject("Attacker_Test");
        var targetObject = new GameObject("Target_Test");
        var taunterObject = new GameObject("Taunter_Test");
        var enemyData = ScriptableObject.CreateInstance<CharacterData>();
        var playerData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            enemyData.isEnemy = true;
            enemyData.maxHp = 20f;
            playerData.isEnemy = false;
            playerData.maxHp = 20f;

            var manager = managerObject.AddComponent<BattleManager>();
            var attacker = attackerObject.AddComponent<BattleCharacter>();
            var target = targetObject.AddComponent<BattleCharacter>();
            var taunter = taunterObject.AddComponent<BattleCharacter>();
            attacker.status = new CharacterStatus(enemyData);
            target.status = new CharacterStatus(playerData);
            taunter.status = new CharacterStatus(playerData);
            manager.playerParty = new List<BattleCharacter> { target, taunter };
            manager.enemyParty = new List<BattleCharacter> { attacker };

            taunter.status.ApplyStatusEffect(EffectType.Taunt, 0f, 0, 0f, 2, -1, taunter);
            target.ReceiveDamage(5f, attacker, DamageType.True);

            Assert.AreEqual(20f, target.status.currentHp);
            Assert.AreEqual(15f, taunter.status.currentHp);
            Assert.AreEqual(1, taunter.status.GetStatus(EffectType.Taunt).remainingCharges);

            var pattern = new SkillLevelData
            {
                effects = new List<EffectEntry>
                {
                    new EffectEntry { type = EffectType.Focus, duration = 1 },
                    new EffectEntry { type = EffectType.Damage, damageType = DamageType.True, multiplier = 0f, fixedValue = 4f, hitCount = 2 }
                }
            };
            EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, pattern, "집중 공격");

            Assert.AreEqual(12f, target.status.currentHp);
            Assert.AreEqual(15f, taunter.status.currentHp);
            Assert.AreEqual(1, taunter.status.GetStatus(EffectType.Taunt).remainingCharges);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(attackerObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(taunterObject);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(playerData);
        }
    }
}
