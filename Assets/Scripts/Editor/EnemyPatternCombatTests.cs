using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EnemyPatternCombatTests
{
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
