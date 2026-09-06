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

    [Test]
    public void FixedStatModifiersStackAcrossTimedAndPermanentDurations()
    {
        var attackerObject = new GameObject("Attacker_Test");
        var targetObject = new GameObject("Target_Test");
        var attackerData = ScriptableObject.CreateInstance<CharacterData>();
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            attackerData.isEnemy = true;
            attackerData.maxHp = 20f;
            attackerData.baseAttack = 0f;
            targetData.maxHp = 30f;
            targetData.armor = 3f;

            var attacker = attackerObject.AddComponent<BattleCharacter>();
            var target = targetObject.AddComponent<BattleCharacter>();
            attacker.status = new CharacterStatus(attackerData);
            target.status = new CharacterStatus(targetData);
            attacker.status.ApplyStatusEffect(EffectType.Strength, 1f, 0, StatusDurationType.Permanent);
            attacker.status.ApplyStatusEffect(EffectType.Strength, 2f, 1, StatusDurationType.Turns);

            Assert.AreEqual(3f, attacker.status.AttackStatusModifier);
            Assert.AreEqual(2, attacker.status.activeStatusEffects.Count);
            Assert.AreEqual("힘 +1 · 영구", attacker.status.activeStatusEffects[0].GetDisplayText());

            var attack = new SkillLevelData
            {
                effects = new List<EffectEntry>
                {
                    new EffectEntry
                    {
                        type = EffectType.Damage,
                        damageType = DamageType.Physical,
                        multiplier = 0f,
                        fixedValue = 3f,
                        hitCount = 2
                    }
                }
            };
            EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, attack, "힘 연타");
            Assert.AreEqual(24f, target.status.currentHp);

            StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { attacker });
            Assert.AreEqual(1f, attacker.status.AttackStatusModifier);
            Assert.AreEqual(1, attacker.status.activeStatusEffects.Count);
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
    public void FixedArmorMagicResistAndSpellModifiersUseFlatValues()
    {
        var characterObject = new GameObject("Character_Test");
        var data = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            data.isEnemy = true;
            data.armor = 3f;
            data.magicResist = 3f;
            data.spellPower = 5f;
            var character = characterObject.AddComponent<BattleCharacter>();
            character.status = new CharacterStatus(data);

            character.status.ApplyStatusEffect(EffectType.Protection, 2f, 0, StatusDurationType.Permanent);
            character.status.ApplyStatusEffect(EffectType.Vulnerable, 1f, 2, StatusDurationType.Turns);
            character.status.ApplyStatusEffect(EffectType.MagicGuard, 2f, 0, StatusDurationType.Permanent);
            character.status.ApplyStatusEffect(EffectType.Corrosion, 1f, 2, StatusDurationType.Turns);
            character.status.ApplyStatusEffect(EffectType.Amplification, 2f, 0, StatusDurationType.Permanent);
            character.status.ApplyStatusEffect(EffectType.Frailty, 1f, 2, StatusDurationType.Turns);

            Assert.AreEqual(4f, character.status.FinalArmor);
            Assert.AreEqual(4f, character.status.FinalMagicResist);
            Assert.AreEqual(6f, character.status.FinalSpellPower);
        }
        finally
        {
            Object.DestroyImmediate(characterObject);
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void ShieldExpiresAtOwnerPhaseButExplicitTurnShieldDoesNot()
    {
        var characterObject = new GameObject("Character_Test");
        var data = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            var character = characterObject.AddComponent<BattleCharacter>();
            character.status = new CharacterStatus(data);
            character.status.ApplyStatusEffect(EffectType.Shield, 5f, 3);
            character.status.ApplyStatusEffect(EffectType.Shield, 2f, 2, StatusDurationType.Turns);

            Assert.AreEqual(2, character.status.activeStatusEffects.Count);
            Assert.AreEqual(1, character.status.RemoveEffectsExpiringAtOwnerPhaseStart());
            Assert.AreEqual(2f, character.status.GetStatusValue(EffectType.Shield));
            Assert.AreEqual(StatusDurationType.Turns, character.status.GetStatus(EffectType.Shield).durationType);
        }
        finally
        {
            Object.DestroyImmediate(characterObject);
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void ReflectConsumesPerHitAndDoesNotTriggerRecursiveReflect()
    {
        var attackerObject = new GameObject("Attacker_Test");
        var defenderObject = new GameObject("Defender_Test");
        var attackerData = ScriptableObject.CreateInstance<CharacterData>();
        var defenderData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            attackerData.isEnemy = true;
            attackerData.maxHp = 20f;
            attackerData.baseAttack = 4f;
            defenderData.maxHp = 20f;

            var attacker = attackerObject.AddComponent<BattleCharacter>();
            var defender = defenderObject.AddComponent<BattleCharacter>();
            attacker.status = new CharacterStatus(attackerData);
            defender.status = new CharacterStatus(defenderData);
            attacker.status.ApplyStatusEffect(EffectType.Reflect, 50f, 0, 0f, 2, -1, attacker);
            defender.status.ApplyStatusEffect(EffectType.Reflect, 50f, 0, 0f, 2, -1, defender);

            defender.ReceiveDamage(4f, attacker, DamageType.Physical);
            defender.ReceiveDamage(4f, attacker, DamageType.Physical);

            Assert.AreEqual(12f, defender.status.currentHp);
            Assert.AreEqual(16f, attacker.status.currentHp);
            Assert.IsNull(defender.status.GetStatus(EffectType.Reflect));
            Assert.AreEqual(2, attacker.status.GetStatus(EffectType.Reflect).remainingCharges);
        }
        finally
        {
            Object.DestroyImmediate(attackerObject);
            Object.DestroyImmediate(defenderObject);
            Object.DestroyImmediate(attackerData);
            Object.DestroyImmediate(defenderData);
        }
    }

    [Test]
    public void TauntAndGuardConsumeOneChargePerHit()
    {
        var managerObject = new GameObject("BattleManager_Test");
        var attackerObject = new GameObject("Attacker_Test");
        var targetObject = new GameObject("Target_Test");
        var protectorObject = new GameObject("Protector_Test");
        var taunterObject = new GameObject("Taunter_Test");
        var enemyData = ScriptableObject.CreateInstance<CharacterData>();
        var playerData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            enemyData.isEnemy = true;
            enemyData.maxHp = 20f;
            playerData.maxHp = 20f;
            var manager = managerObject.AddComponent<BattleManager>();
            var attacker = attackerObject.AddComponent<BattleCharacter>();
            var target = targetObject.AddComponent<BattleCharacter>();
            var protector = protectorObject.AddComponent<BattleCharacter>();
            var taunter = taunterObject.AddComponent<BattleCharacter>();
            attacker.status = new CharacterStatus(enemyData);
            target.status = new CharacterStatus(playerData);
            protector.status = new CharacterStatus(playerData);
            taunter.status = new CharacterStatus(playerData);
            manager.playerParty = new List<BattleCharacter> { target, protector, taunter };
            manager.enemyParty = new List<BattleCharacter> { attacker };

            taunter.status.ApplyStatusEffect(EffectType.Taunt, 0f, 0, 0f, 2, -1, taunter);
            var twoHits = new SkillLevelData
            {
                effects = new List<EffectEntry>
                {
                    new EffectEntry
                    {
                        type = EffectType.Damage,
                        damageType = DamageType.True,
                        multiplier = 0f,
                        fixedValue = 2f,
                        hitCount = 2
                    }
                }
            };
            EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, twoHits, "도발 연타");
            Assert.AreEqual(16f, taunter.status.currentHp);
            Assert.IsNull(taunter.status.GetStatus(EffectType.Taunt));

            target.status.ApplyStatusEffect(EffectType.Guard, 0f, 0, 0f, 2, -1, protector);
            EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, twoHits, "보호 연타");
            Assert.AreEqual(20f, target.status.currentHp);
            Assert.AreEqual(16f, protector.status.currentHp);
            Assert.IsNull(target.status.GetStatus(EffectType.Guard));
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(attackerObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(protectorObject);
            Object.DestroyImmediate(taunterObject);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(playerData);
        }
    }

    [Test]
    public void StrengthAppliedAfterAttackOnlyAffectsTheNextAttack()
    {
        var attackerObject = new GameObject("Attacker_Test");
        var targetObject = new GameObject("Target_Test");
        var attackerData = ScriptableObject.CreateInstance<CharacterData>();
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            attackerData.isEnemy = true;
            attackerData.maxHp = 20f;
            targetData.maxHp = 20f;
            var attacker = attackerObject.AddComponent<BattleCharacter>();
            var target = targetObject.AddComponent<BattleCharacter>();
            attacker.status = new CharacterStatus(attackerData);
            target.status = new CharacterStatus(targetData);

            var attackThenGrow = new SkillLevelData
            {
                effects = new List<EffectEntry>
                {
                    new EffectEntry { type = EffectType.Damage, multiplier = 0f, fixedValue = 3f },
                    new EffectEntry
                    {
                        type = EffectType.Strength,
                        value = 1f,
                        durationType = StatusDurationType.Permanent
                    }
                }
            };
            EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, attackThenGrow, "공격 후 성장");
            Assert.AreEqual(17f, target.status.currentHp);

            var nextAttack = new SkillLevelData
            {
                effects = new List<EffectEntry>
                {
                    new EffectEntry { type = EffectType.Damage, multiplier = 0f, fixedValue = 3f }
                }
            };
            EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, nextAttack, "다음 공격");
            Assert.AreEqual(13f, target.status.currentHp);
        }
        finally
        {
            Object.DestroyImmediate(attackerObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(attackerData);
            Object.DestroyImmediate(targetData);
        }
    }
}
