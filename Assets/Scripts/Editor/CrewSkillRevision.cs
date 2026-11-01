using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TheLastArk.Skills;

namespace TheLastArk.Editor
{
    /// <summary>Explicit application of the October 2026 crew design. Never runs on editor initialization.</summary>
    public static class CrewSkillRevision
    {
        public static void ApplyAll()
        {
            ApplyArbalist(Load("Arbalist"));
            ApplyArchmage(Load("Archmage"));
            ApplyBulwark(Load("Bulwark"));
            var guardian = Load("Guardian"); guardian.armor = 5; EditorUtility.SetDirty(guardian);
            var assassin = Load("Assassin"); assassin.armor = 1;
            foreach (var level in assassin.activeSkills[1].levels)
            {
                level.splitPoolLimit = 10;
                level.description = $"주사위 1개를 {level.specialCount}개로 분할. 합계는 원래 주사위 눈금을 유지하며, 각 결과는 0 이상으로 직접 지정.\n기존 보정은 선택한 결과 1개에만 유지.\n분할 후 보관함의 주사위가 10개를 초과하면 주사위·사용 횟수를 소모하지 않고 사용 불가. 공용 보관함 자체는 무제한.";
            }
            EditorUtility.SetDirty(assassin);
            var pathfinder = Load("Pathfinder");
            for (int r = 0; r < pathfinder.activeSkills[0].levels.Length; r++)
            {
                var level = pathfinder.activeSkills[0].levels[r];
                foreach (var effect in level.effects) if (effect.type == EffectType.Focus) effect.focusOnlyThisSkill = true;
                level.description = $"적 1명에게 주문력 {80 + 40 * r}% 물리 피해 후 사냥 표식 부여.\n파쇄·침식 각각 ⌊눈금 ÷ 2⌋{(r > 0 ? " + " + r : "")} 부여 (1턴)."
                    + (r > 0 ? "\n집중: 이번 공격만 도발 무시." : "");
            }
            EditorUtility.SetDirty(pathfinder);
            AssetDatabase.SaveAssets();
        }

        private static CharacterData Load(string name) => AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Resources/Characters/Player/Data/" + name + "_Data.asset");
        private static SkillLevelData Level(TargetType target, string description, params EffectEntry[] effects)
            => new SkillLevelData { targetType = target, description = description, effects = new List<EffectEntry>(effects) };
        private static EffectEntry Damage(float scale, DamageType type = DamageType.Physical)
            => new EffectEntry { type = EffectType.Damage, damageType = type, multiplier = scale };
        private static EffectEntry Focus(bool temporary) => new EffectEntry { type = EffectType.Focus, duration = 1,
            durationType = StatusDurationType.Turns, targetScope = EffectTargetScope.Self, focusOnlyThisSkill = temporary };

        // Keep each existing skill/icon and the art/VFX attached to corresponding effects.
        private static void Set(SkillInfo skill, int rank, SkillLevelData level)
        {
            var old = skill.levels[rank];
            if (old != null)
            {
                level.customVfxName = old.customVfxName;
                level.visualEffect = old.visualEffect?.Copy();
                foreach (var effect in level.effects)
                {
                    var previous = old.effects?.Find(e => e != null && e.type == effect.type && e.targetScope == effect.targetScope);
                    if (previous == null) continue;
                    effect.customVfxName = previous.customVfxName; effect.statusIcon = previous.statusIcon;
                    effect.visualEffect = previous.visualEffect?.Copy();
                }
            }
            skill.levels[rank] = level;
        }

        public static void ApplyArbalist(CharacterData data)
        {
            for (int r = 0; r < 3; r++)
            {
                Set(data.passiveSkill, r, Level(TargetType.Self,
                    $"치명타 확률 +{(r == 0 ? 25 : 50)}%p.\n치명타 확률 100% 초과분 1%p마다 공격력 +1%."
                    + (r == 0 ? "\n개화: 치명타 확률 +25%p, 치명타 피해 +25%p 추가." : "\n치명타 피해 +25%p."),
                    new EffectEntry { value = r == 0 ? 25 : 50, secondaryValue = r == 0 ? 0 : 25 }));
                var shot = Damage(r == 0 ? 1.5f : 1.8f); shot.fixedPerDie = 1;
                shot.belowQuarterHealthDamageBonusPercent = r == 2 ? 50 : 0;
                var level = Level(TargetType.SingleEnemy, $"4 이상 · 적 1명에게 눈금 + 공격력 {(r == 0 ? 150 : 180)}% 물리 피해.\n집중: 이번 공격은 도발 무시."
                    + (r == 2 ? "\n대상 체력이 25% 미만이면 피해 +50%." : ""), Focus(true), shot);
                level.diceInput.condition = DiceCondition.AtLeast; level.diceInput.value = 4;
                Set(data.activeSkills[0], r, level);

                var blast = Damage(r == 0 ? 1.5f : 1.8f); blast.fixedPerDie = r == 2 ? 1 : .5f;
                blast.splashAdjacentPercent = 100; blast.noAdjacentFinalDamageBonusPercent = 25;
                level = Level(TargetType.SingleEnemy, $"주사위 3개 · 적 1명과 양옆 적에게 각각 공격력 {(r == 0 ? 150 : 180)}% + 눈금 합계{(r == 2 ? "" : " ÷ 2")} 물리 피해.\n양옆에 생존한 적이 없으면 주 대상의 최종 피해 +25%.", blast);
                level.diceInput.diceCount = 3;
                Set(data.activeSkills[1], r, level);

                level = Level(TargetType.SingleEnemy, $"홀수 · 적 1명에게 공격력 {(r == 0 ? 60 : 80)}% 물리 피해.\n다음 자기 턴까지 {(r == 2 ? "모든 피해(정신 피해 포함)" : "물리 피해")}를 ⌊눈금 ÷ 2⌋회 회피.",
                    Damage(r == 0 ? .6f : .8f), new EffectEntry { type = EffectType.Evasion, multiplier = 0, charges = 0,
                        valuePerDieDivisor = 2, secondaryValue = r == 2 ? 1 : 2, targetScope = EffectTargetScope.Self });
                level.diceInput.condition = DiceCondition.Odd;
                Set(data.activeSkills[2], r, level);

                var aimed = Damage(r == 2 ? 3f : 2f); aimed.targetMissingHealthPercent = 30;
                level = Level(TargetType.Self, $"5 이상 · 턴당 2회. 각 시전에 새 주사위 1개 필요.\n첫 시전: 1턴간 모든 스킬에 집중{(r >= 1 ? "·관통" : "")} 적용.\n두 번째 시전: 적 1명에게 공격력 {(r == 2 ? 300 : 200)}% + 대상이 잃은 체력의 30% 물리 피해.", Focus(false), aimed);
                if (r >= 1) level.effects.Insert(1, new EffectEntry { type = EffectType.Pierce, value = 100, duration = 1,
                    durationType = StatusDurationType.Turns, targetScope = EffectTargetScope.Self });
                level.special = SkillSpecial.PreparedShot; level.usesPerTurn = 2;
                level.diceInput.condition = DiceCondition.AtLeast; level.diceInput.value = 5;
                Set(data.activeSkills[3], r, level);
            }
            EditorUtility.SetDirty(data);
        }

        public static void ApplyArchmage(CharacterData data)
        {
            data.activeSkills[0].skillName = "마력탄";
            for (int r = 0; r < 3; r++)
            {
                Set(data.passiveSkill, r, Level(TargetType.Self,
                    $"주사위를 사용할 때, 스킬 효과 처리 후 효과 눈금{(r > 0 ? " +1" : "")}만큼 원초 마력 획득.\n최대 {(r > 0 ? 50 : 30)}. 충전 투입에도 발동하며 전투 종료 후 유지."
                    + (r == 0 ? "\n개화: 주사위당 획득량 +1, 최대치 +20." : ""),
                    new EffectEntry { value = r > 0 ? 1 : 0, secondaryValue = r > 0 ? 20 : 0 }));
                var bolt = Damage(r == 0 ? .4f : .7f, DamageType.Magical); bolt.fixedPerDie = 1; bolt.primalManaScaling = .5f;
                var level = Level(TargetType.SingleEnemy, $"적 1명에게 주문력 {(r == 0 ? 40 : 70)}% + 눈금 + 현재 원초 마력 ÷ 2 마법 피해.\n사용한 슬롯을 클릭하면 마력 {(r == 2 ? 4 : 7)}을 소모하여 복구.\n복구는 주사위를 쓰지 않으며 재시전에는 새 주사위가 필요. 마력이 충분하면 반복 복구 가능.", bolt);
                level.primalManaRestoreCost = r == 2 ? 4 : 7;
                Set(data.activeSkills[0], r, level);
                level = Level(TargetType.Self, "눈금만큼 원초 마력과 1턴 보호막 획득."
                    + (r >= 1 ? "\n원초 마력 2 추가 획득." : "") + (r == 2 ? "\n눈금 2 이하: 원초 마력 2 추가 획득." : ""),
                    new EffectEntry { type = EffectType.Shield, multiplier = 0, fixedPerDie = 1, duration = 1, targetScope = EffectTargetScope.Self });
                level.primalManaGainFromDie = true; level.primalManaGain = r >= 1 ? 2 : 0; level.primalManaLowDieBonus = r == 2 ? 2 : 0;
                Set(data.activeSkills[1], r, level);
                var landDamage = Damage(r == 0 ? .7f : 1f, DamageType.Magical); landDamage.fixedPerDie = 1;
                level = Level(TargetType.Self, $"마력 20 미만: 마력 5 획득 후 주문력 {(r == 0 ? 40 : 60)}% + 현재 마력만큼 1턴 보호막.\n마력 20 이상: 마력 5 소모, 모든 적에게 주문력 {(r == 0 ? 70 : 100)}% + 눈금 마법 피해."
                    + (r == 2 ? "\n사용 직전 마력 30 이상: 마력 +5 → 보호막 → 마력 5 소모 → 전체 공격." : ""),
                    new EffectEntry { type = EffectType.Shield, statSource = EffectStatSource.SpellPower, multiplier = r == 0 ? .4f : .6f,
                        primalManaScaling = 1, duration = 1, targetScope = EffectTargetScope.Self }, landDamage);
                level.special = SkillSpecial.MagicLand; level.primalManaDualEffectThreshold = r == 2 ? 30 : 0;
                Set(data.activeSkills[2], r, level);
                var root = Damage(1.4f, DamageType.Magical); root.spentPrimalManaScaling = .5f;
                root.splashOtherEnemiesPercent = 50; root.loneTargetDamageBonusPercent = 50;
                root.spentPrimalManaBonusThreshold = r == 2 ? 50 : 0; root.spentPrimalManaDamageBonusPercent = r == 2 ? 100 : 0;
                root.spentManaBonusIsFinalDamage = true;
                level = Level(TargetType.SingleEnemy, $"눈금 {(r == 0 ? "6" : "1 또는 6")} · 원초 마력 30 이상 필요.\n모든 마력 소모: 적 1명에게 주문력 140% + 소모 마력 ÷ 2 마법 피해.\n다른 모든 적에게 절반의 피해. 다른 적이 없다면 주 대상 피해 +50%."
                    + (r == 2 ? "\n마력 50 소모 시 최종 피해 +100%." : ""), root);
                level.diceInput.condition = r == 0 ? DiceCondition.Exact : DiceCondition.Either;
                level.diceInput.value = r == 0 ? 6 : 1; level.diceInput.maxValue = 6;
                level.primalManaRequired = 30; level.consumeAllPrimalMana = true;
                Set(data.activeSkills[3], r, level);
            }
            EditorUtility.SetDirty(data);
        }

        public static void ApplyBulwark(CharacterData data)
        {
            for (int r = 0; r < 3; r++)
            {
                Set(data.passiveSkill, r, Level(TargetType.Self,
                    $"자기 진영 턴 시작 시 축전 2 획득. 최대 {(r == 0 ? 10 : 12)}.\n{(r == 0 ? "자신이" : "자신을 포함한 아군이")} 한 번의 피격에서 보호막으로 5 이상 흡수하면 축전 1 획득.\n전투마다 0에서 시작."
                    + (r == 0 ? "\n개화: 다른 아군의 보호막으로도 획득, 최대치 +2." : ""),
                    new EffectEntry { value = 2, charges = r == 0 ? 10 : 12, secondaryValue = 5 }));
                var level = Level(TargetType.Friendly, $"다른 아군 1명에게 보호 {(r == 0 ? 1 : 2)}회, 공격력 60% + 주문력 50% + 눈금만큼 1턴 보호막.\n축전 {(r == 2 ? 4 : 6)} 이상이면 자신도 동일한 보호막 획득. 축전은 소모하지 않음.",
                    new EffectEntry { type = EffectType.Guard, charges = r == 0 ? 1 : 2 },
                    new EffectEntry { type = EffectType.Shield, statSource = EffectStatSource.Attack, multiplier = .6f, additionalSpellPowerMultiplier = .5f, fixedPerDie = 1, duration = 1 },
                    new EffectEntry { type = EffectType.Shield, statSource = EffectStatSource.Attack, multiplier = .6f, additionalSpellPowerMultiplier = .5f, fixedPerDie = 1,
                        duration = 1, targetScope = EffectTargetScope.Self, minCurrentCapacitor = r == 2 ? 4 : 6 });
                level.excludeSelf = true; Set(data.activeSkills[0], r, level);
                level = Level(TargetType.Self, $"충전 15 − 현재 축전 · 턴당 2회, 시전마다 충전 필요.\n각 시전에서 축전 {(r == 2 ? 4 : 2)} 획득.\n첫 시전: 주문력 {(r == 0 ? 140 : 170)}% + 현재 축전만큼 1턴 보호막.\n두 번째 시전: 현재 자신의 보호막 + 축전만큼 모든 적에게 마법 피해.",
                    new EffectEntry { type = EffectType.Shield, statSource = EffectStatSource.SpellPower, multiplier = r == 0 ? 1.4f : 1.7f,
                        capacitorScaling = 1, duration = 1, targetScope = EffectTargetScope.Self },
                    new EffectEntry { type = EffectType.Damage, damageType = DamageType.Magical, statSource = EffectStatSource.CurrentSelfShield, multiplier = 1, capacitorScaling = 1 });
                level.special = SkillSpecial.ElectromagneticField; level.usesPerTurn = 2; level.diceInput.chargeTarget = 15;
                level.chargeReducedByCapacitor = true; level.capacitorGain = r == 2 ? 4 : 2;
                Set(data.activeSkills[1], r, level);
                var shock = Damage(.7f); shock.fixedPerDie = 1; shock.damageBonusPercentPerSpentCapacitor = 20;
                level = Level(TargetType.SingleEnemy, $"적 1명에게 공격력 70% + 눈금 물리 피해.\n축전 최대 {(r == 2 ? 6 : 4)} 소모, 소모한 축전마다 피해 +20%."
                    + (r > 0 ? "\n축전 4 이상 소모: 파쇄 1 · 침식 1 부여 (3턴)." : ""), shock);
                level.capacitorMaxSpend = r == 2 ? 6 : 4;
                if (r > 0) { level.effects.Add(new EffectEntry { type = EffectType.Vulnerable, value = 1, duration = 3, minSpentCapacitor = 4 });
                    level.effects.Add(new EffectEntry { type = EffectType.Corrosion, value = 1, duration = 3, minSpentCapacitor = 4 }); }
                Set(data.activeSkills[2], r, level);
                level = Level(TargetType.AllFriendly, $"충전 30 − 현재 축전.\n축전을 전부 소모하여 모든 아군에게 공격력 {(r == 0 ? 30 : 45)}% + 주문력 {(r == 0 ? 30 : 45)}% + 소모한 축전만큼 1턴 보호막."
                    + (r == 2 ? "\n소모한 축전의 절반을 돌려받음 (버림)." : ""),
                    new EffectEntry { type = EffectType.Shield, statSource = EffectStatSource.Attack, multiplier = r == 0 ? .3f : .45f,
                        additionalSpellPowerMultiplier = r == 0 ? .3f : .45f, spentCapacitorScaling = 1, duration = 1, targetScope = EffectTargetScope.AllAllies });
                level.diceInput.chargeTarget = 30; level.chargeReducedByCapacitor = true; level.consumeAllCapacitor = true; level.capacitorRefundPercent = r == 2 ? 50 : 0;
                Set(data.activeSkills[3], r, level);
            }
            EditorUtility.SetDirty(data);
        }
    }
}
