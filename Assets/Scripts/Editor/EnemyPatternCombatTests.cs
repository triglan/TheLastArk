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
            attackerData.armor = 0f;
            defenderData.armor = 0f;

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
            targetData.armor = 0f;
            var attacker = attackerObject.AddComponent<BattleCharacter>();
            var target = targetObject.AddComponent<BattleCharacter>();
            attacker.status = new CharacterStatus(attackerData);
            target.status = new CharacterStatus(targetData);

            var attackThenGrow = new SkillLevelData
            {
                effects = new List<EffectEntry>
                {
                    new EffectEntry { type = EffectType.Damage, multiplier = 0f, fixedValue = 3f }
                }
            };
            EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, attackThenGrow, "공격 후 성장");
            Assert.AreEqual(17f, target.status.currentHp);
            // ProcessSkill applies every entry to its provided targets; apply the self buff explicitly.
            attacker.status.ApplyStatusEffect(EffectType.Strength, 1f, 1, StatusDurationType.Permanent);

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

    [Test]
    public void EnemyRuntimeStateCopiesDefinitionsAndClampsValues()
    {
        var data = ScriptableObject.CreateInstance<CharacterData>();
        try
        {
            data.enemyRuntimeVariables = new List<EnemyRuntimeVariableDefinition>
            {
                new EnemyRuntimeVariableDefinition { id = "ammo", initialValue = 1, hasMaxValue = true, maxValue = 3 }
            };
            var state = new EnemyRuntimeState(data);

            Assert.AreEqual(1, state.Get("ammo"));
            Assert.IsTrue(state.Add("ammo", 10));
            Assert.AreEqual(3, state.Get("ammo"));
            Assert.AreEqual(1, data.enemyRuntimeVariables[0].initialValue);
            Assert.IsFalse(state.Set("missing", 1));
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void EnemyActionsRunInOrderAndUseRuntimeVariables()
    {
        var enemyObject = new GameObject("Enemy_Test");
        var targetObject = new GameObject("Target_Test");
        var enemyData = ScriptableObject.CreateInstance<CharacterData>();
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            enemyData.isEnemy = true;
            enemyData.baseAttack = 99f;
            enemyData.maxHp = 20f;
            enemyData.enemyRuntimeVariables = new List<EnemyRuntimeVariableDefinition>
            {
                new EnemyRuntimeVariableDefinition { id = "shots", initialValue = 1 }
            };
            enemyData.enemyPatterns = new List<EnemyPatternData>
            {
                new EnemyPatternData
                {
                    patternName = "연타 후 강화",
                    actions = new List<EnemyPatternAction>
                    {
                        new EnemyPatternAction { actionType = EnemyPatternActionType.RememberTarget },
                        new EnemyPatternAction
                        {
                            targetSelection = EnemyTargetSelection.Remembered,
                            effect = new EffectEntry { type = EffectType.Damage, damageType = DamageType.Physical, multiplier = 0f, fixedValue = 3f, hitCount = 1 },
                            hitCountVariableId = "shots"
                        },
                        new EnemyPatternAction
                        {
                            targetType = TargetType.Self,
                            effect = new EffectEntry { type = EffectType.Strength, value = 1f, durationType = StatusDurationType.Permanent }
                        },
                        new EnemyPatternAction
                        {
                            actionType = EnemyPatternActionType.ModifyRuntimeVariable,
                            runtimeVariableId = "shots",
                            runtimeValue = 1
                        }
                    }
                }
            };
            targetData.maxHp = 20f;
            targetData.armor = 0f;

            var enemy = enemyObject.AddComponent<BattleCharacter>();
            var ai = enemyObject.AddComponent<EnemyAI>();
            var target = targetObject.AddComponent<BattleCharacter>();
            enemy.status = new CharacterStatus(enemyData);
            target.status = new CharacterStatus(targetData);
            ai.targetParty = new List<BattleCharacter> { target };
            ai.allyParty = new List<BattleCharacter> { enemy };

            Assert.IsTrue(ai.PrepareTurn());
            ai.ExecuteTurn();

            Assert.AreEqual(14f, target.status.currentHp);
            Assert.AreEqual(1f, enemy.status.AttackStatusModifier);
            Assert.AreEqual(2, ai.RuntimeState.Get("shots"));

            Assert.IsTrue(ai.PrepareTurn());
            ai.ExecuteTurn();
            Assert.AreEqual(2f, target.status.currentHp);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(targetData);
        }
    }

    [Test]
    public void LegacyEnemyPatternsIgnoreCycleGrowth()
    {
        var enemyObject = new GameObject("Enemy_Test");
        var targetObject = new GameObject("Target_Test");
        var enemyData = ScriptableObject.CreateInstance<CharacterData>();
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            enemyData.isEnemy = true;
            enemyData.baseAttack = 99f;
            enemyData.damageBonusPerCycle = 50f;
            enemyData.enemyPatterns = new List<EnemyPatternData>
            {
                new EnemyPatternData
                {
                    effects = new List<EffectEntry>
                    {
                        new EffectEntry { type = EffectType.Damage, damageType = DamageType.Physical, multiplier = 0f, fixedValue = 3f }
                    }
                }
            };
            targetData.maxHp = 20f;
            targetData.armor = 0f;

            var enemy = enemyObject.AddComponent<BattleCharacter>();
            var ai = enemyObject.AddComponent<EnemyAI>();
            var target = targetObject.AddComponent<BattleCharacter>();
            enemy.status = new CharacterStatus(enemyData);
            target.status = new CharacterStatus(targetData);
            ai.targetParty = new List<BattleCharacter> { target };
            ai.allyParty = new List<BattleCharacter> { enemy };

            ai.PrepareTurn();
            ai.ExecuteTurn();
            ai.PrepareTurn();
            ai.ExecuteTurn();

            Assert.AreEqual(14f, target.status.currentHp);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(targetData);
        }
    }

    [Test]
    public void ActualResultDoesNotLeakBetweenEffectExecutions()
    {
        var attackerObject = new GameObject("Attacker_Test");
        var targetObject = new GameObject("Target_Test");
        var attackerData = ScriptableObject.CreateInstance<CharacterData>();
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            targetData.maxHp = 20f;
            targetData.armor = 0f;
            var attacker = attackerObject.AddComponent<BattleCharacter>();
            var target = targetObject.AddComponent<BattleCharacter>();
            attacker.status = new CharacterStatus(attackerData);
            target.status = new CharacterStatus(targetData);

            EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, new SkillLevelData
            {
                effects = new List<EffectEntry> { new EffectEntry { type = EffectType.Damage, multiplier = 0f, fixedValue = 4f } }
            });
            EffectEngine.ProcessSkill(attacker, new List<BattleCharacter> { target }, new SkillLevelData
            {
                effects = new List<EffectEntry> { new EffectEntry { type = EffectType.Damage, multiplier = 1f, useActualResult = true } }
            });

            Assert.AreEqual(16f, target.status.currentHp);
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
    public void EnemyIntentUsesPreparedPatternAndStructuredEffectData()
    {
        var enemyObject = new GameObject("Enemy_Intent_Test");
        var targetObject = new GameObject("Target_Intent_Test");
        var enemyData = ScriptableObject.CreateInstance<CharacterData>();
        var targetData = ScriptableObject.CreateInstance<CharacterData>();
        var intentIcon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        try
        {
            enemyData.isEnemy = true;
            enemyData.enemyRuntimeVariables = new List<EnemyRuntimeVariableDefinition>
            {
                new EnemyRuntimeVariableDefinition { id = "shots", initialValue = 1 }
            };
            enemyData.enemyPatterns = new List<EnemyPatternData>
            {
                new EnemyPatternData
                {
                    patternName = "연타",
                    actions = new List<EnemyPatternAction>
                    {
                        new EnemyPatternAction
                        {
                            actionType = EnemyPatternActionType.ModifyRuntimeVariable,
                            runtimeVariableId = "shots",
                            runtimeValue = 1
                        },
                        new EnemyPatternAction
                        {
                            targetType = TargetType.SingleEnemy,
                            targetSelection = EnemyTargetSelection.Leader,
                            intentIcon = intentIcon,
                            effect = new EffectEntry { type = EffectType.Damage, damageType = DamageType.Physical, multiplier = 0f, fixedValue = 3f, hitCount = 1 },
                            hitCountVariableId = "shots"
                        }
                    }
                }
            };

            var enemy = enemyObject.AddComponent<BattleCharacter>();
            var ai = enemyObject.AddComponent<EnemyAI>();
            var target = targetObject.AddComponent<BattleCharacter>();
            target.isLeader = true;
            enemy.status = new CharacterStatus(enemyData);
            target.status = new CharacterStatus(targetData);
            ai.targetParty = new List<BattleCharacter> { target };
            ai.allyParty = new List<BattleCharacter> { enemy };

            Assert.IsTrue(ai.PrepareTurn());
            Assert.AreEqual("연타", ai.PreparedIntent.PatternName);
            Assert.AreEqual(EnemyIntentVisualType.Attack, ai.PreparedIntent.VisualType);
            Assert.AreSame(target, ai.PreparedIntent.Targets[0]);
            Assert.AreEqual(3f, ai.PreparedIntent.Effects[1].Value);
            // Initial 1 + this action's increment 1 + the base hit 1.
            Assert.AreEqual(3, ai.PreparedIntent.Effects[1].HitCount);
            Assert.AreSame(intentIcon, ai.PreparedIntent.Effects[1].Icon);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(targetData);
            Object.DestroyImmediate(intentIcon);
        }
    }

    [Test]
    public void HidingEnemyIntentDoesNotDisableEnemyObject()
    {
        var enemyObject = new GameObject("Enemy_Intent_UI_Test", typeof(RectTransform));

        try
        {
            var ui = enemyObject.AddComponent<TheLastArk.UI.EnemyIntentUI>();
            ui.Show(new EnemyIntentData { PatternName = "Test" });
            ui.Hide();

            Assert.IsTrue(enemyObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
        }
    }

    [Test]
    public void EnemyEnhancementIconsSupportMoreThanTenEntries()
    {
        var enemyObject = new GameObject("Enemy_Enhancement_UI_Test", typeof(RectTransform));
        var enemyData = ScriptableObject.CreateInstance<CharacterData>();
        var icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        try
        {
            enemyData.isEnemy = true;
            enemyData.enemyRuntimeVariables = new List<EnemyRuntimeVariableDefinition>();
            for (int i = 0; i < 11; i++)
                enemyData.enemyRuntimeVariables.Add(new EnemyRuntimeVariableDefinition
                {
                    id = $"value_{i}", displayName = $"Value {i}", initialValue = 1, showInUi = true, displayIcon = icon
                });

            BattleCharacter enemy = enemyObject.AddComponent<BattleCharacter>();
            enemy.status = new CharacterStatus(enemyData);
            enemyObject.AddComponent<EnemyAI>();
            var ui = enemyObject.AddComponent<TheLastArk.UI.EnemyEnhancementUI>();
            typeof(TheLastArk.UI.EnemyEnhancementUI).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(ui, null);

            var images = enemyObject.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            Assert.AreEqual(11, System.Array.FindAll(images, image => image.name == "Enhancement Icon" && image.sprite == icon).Length);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(icon);
        }
    }

    [Test]
    public void IconLibraryReturnsConfiguredEffectIcon()
    {
        var library = ScriptableObject.CreateInstance<IconLibrary>();
        var icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        try
        {
            library.SetIcon(EffectType.Poison, icon);
            Assert.AreSame(icon, library.GetIcon(EffectType.Poison));
        }
        finally
        {
            Object.DestroyImmediate(library);
            Object.DestroyImmediate(icon);
        }
    }

    [Test]
    public void PoisonEnemyIntentUsesPoisonAndAppliesIt()
    {
        var enemyObject = new GameObject("Poison_Enemy_Test");
        var targetObject = new GameObject("Poison_Target_Test");
        var enemyData = ScriptableObject.CreateInstance<CharacterData>();
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            enemyData.isEnemy = true;
            enemyData.enemyPatterns = new List<EnemyPatternData>
            {
                new EnemyPatternData
                {
                    patternName = "Poison Test",
                    actions = new List<EnemyPatternAction>
                    {
                        new EnemyPatternAction
                        {
                            targetType = TargetType.SingleEnemy,
                            targetSelection = EnemyTargetSelection.Leader,
                            effect = new EffectEntry { type = EffectType.Poison, value = 2f, duration = 2 }
                        }
                    }
                }
            };

            BattleCharacter enemy = enemyObject.AddComponent<BattleCharacter>();
            EnemyAI ai = enemyObject.AddComponent<EnemyAI>();
            BattleCharacter target = targetObject.AddComponent<BattleCharacter>();
            target.isLeader = true;
            enemy.status = new CharacterStatus(enemyData);
            target.status = new CharacterStatus(targetData);
            ai.targetParty = new List<BattleCharacter> { target };
            ai.allyParty = new List<BattleCharacter> { enemy };

            Assert.IsTrue(ai.PrepareTurn());
            Assert.AreEqual(EffectType.Poison, ai.PreparedIntent.Effects[0].EffectType);
            ai.ExecuteTurn();
            Assert.AreEqual(2f, target.status.GetStatus(EffectType.Poison).damagePerTurn);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(targetData);
        }
    }
    [Test]
    public void EnemyRuntimeVariableIncreasesPoisonIntentAndApplication()
    {
        var enemyObject = new GameObject("Poison_Growth_Enemy_Test");
        var targetObject = new GameObject("Poison_Growth_Target_Test");
        var enemyData = ScriptableObject.CreateInstance<CharacterData>();
        var targetData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            enemyData.isEnemy = true;
            enemyData.enemyRuntimeVariables = new List<EnemyRuntimeVariableDefinition>
            {
                new EnemyRuntimeVariableDefinition { id = "poison_bonus", initialValue = 1 }
            };
            enemyData.enemyPatterns = new List<EnemyPatternData>
            {
                new EnemyPatternData
                {
                    targetType = TargetType.SingleEnemy,
                    actions = new List<EnemyPatternAction>
                    {
                        new EnemyPatternAction
                        {
                            targetType = TargetType.SingleEnemy,
                            valueVariableId = "poison_bonus",
                            effect = new EffectEntry { type = EffectType.Poison, value = 2f, duration = 3 }
                        }
                    }
                }
            };

            BattleCharacter enemy = enemyObject.AddComponent<BattleCharacter>();
            EnemyAI ai = enemyObject.AddComponent<EnemyAI>();
            BattleCharacter target = targetObject.AddComponent<BattleCharacter>();
            enemy.status = new CharacterStatus(enemyData);
            target.status = new CharacterStatus(targetData);
            ai.targetParty = new List<BattleCharacter> { target };
            ai.allyParty = new List<BattleCharacter> { enemy };

            Assert.IsTrue(ai.PrepareTurn());
            Assert.AreEqual(3f, ai.PreparedIntent.Effects[0].Value);
            ai.ExecuteTurn();
            Assert.AreEqual(3f, target.status.GetStatus(EffectType.Poison).damagePerTurn);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(enemyData);
            Object.DestroyImmediate(targetData);
        }
    }

    [Test]
    public void FleeActionRemovesOnlyItsEnemyFromBattle()
    {
        var enemyObject = new GameObject("Flee_Enemy_Test");
        var enemyData = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            enemyData.isEnemy = true;
            enemyData.enemyPatterns = new List<EnemyPatternData>
            {
                new EnemyPatternData
                {
                    targetType = TargetType.Self,
                    actions = new List<EnemyPatternAction>
                    {
                        new EnemyPatternAction { actionType = EnemyPatternActionType.Flee }
                    }
                }
            };

            BattleCharacter enemy = enemyObject.AddComponent<BattleCharacter>();
            EnemyAI ai = enemyObject.AddComponent<EnemyAI>();
            EnemyBattleCharacter enemyBattleCharacter = enemyObject.AddComponent<EnemyBattleCharacter>();
            enemyBattleCharacter.enemyData = enemyData;
            enemy.status = new CharacterStatus(enemyData);
            ai.targetParty = new List<BattleCharacter>();
            ai.allyParty = new List<BattleCharacter> { enemy };

            Assert.IsTrue(ai.PrepareTurn());
            ai.ExecuteTurn();

            Assert.IsTrue(ai.RuntimeState.HasFled);
            Assert.AreEqual(0f, enemy.status.currentHp);
            Assert.IsFalse(enemyObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(enemyData);
        }
    }

    [Test]
    public void EnemyViewCreatesDefenseValuesBelowHealthBar()
    {
        var root = new GameObject("Enemy_Defense_UI_Test", typeof(RectTransform));
        var data = ScriptableObject.CreateInstance<CharacterData>();

        try
        {
            new GameObject("Standing Illust", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image)).transform.SetParent(root.transform, false);
            var hp = new GameObject("HP Bar", typeof(RectTransform), typeof(UnityEngine.UI.Slider));
            hp.transform.SetParent(root.transform, false);
            var mental = new GameObject("Mental Bar", typeof(RectTransform), typeof(UnityEngine.UI.Slider));
            mental.transform.SetParent(root.transform, false);
            var hpText = new GameObject("HP Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();
            hpText.transform.SetParent(root.transform, false);
            var mentalText = new GameObject("Mental Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();
            mentalText.transform.SetParent(root.transform, false);

            var bars = root.AddComponent<BarUI>();
            bars.hpSlider = hp.GetComponent<UnityEngine.UI.Slider>();
            bars.mentalSlider = mental.GetComponent<UnityEngine.UI.Slider>();
            bars.hpText = hpText;
            bars.mentalText = mentalText;
            var view = root.AddComponent<CharacterView>();
            view.barUI = bars;

            data.isEnemy = true;
            data.armor = 7f;
            data.magicResist = 5f;
            view.UpdateVisual(new CharacterStatus(data));

            Transform defense = root.transform.Find("Defense Stats");
            Assert.IsNotNull(defense);
            var values = defense.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            Assert.IsTrue(System.Array.Exists(values, text => text.text == "7"));
            Assert.IsTrue(System.Array.Exists(values, text => text.text == "5"));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(data);
        }
    }
}
