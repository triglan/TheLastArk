using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Skills
{
    /// <summary>Freeze the current stage before committing a use. Shared assets remain untouched.</summary>
    public static class SkillStageRules
    {
        public static SkillLevelData Resolve(SkillLevelData source, int uses)
        {
            if (source == null || (source.special != SkillSpecial.PreparedShot && source.special != SkillSpecial.ElectromagneticField)) return source;
            var level = source.RuntimeCopy();
            bool first = uses == 0;
            level.targetType = first ? TargetType.Self : source.special == SkillSpecial.PreparedShot ? TargetType.SingleEnemy : TargetType.AllEnemy;
            level.effects = new List<EffectEntry>();
            foreach (var effect in source.effects)
                if (effect != null && (effect.type == EffectType.Damage) != first) level.effects.Add(effect);
            level.special = SkillSpecial.None;
            return level;
        }

        public static DiceInputRule DiceRule(SkillLevelData level, CharacterStatus actor)
        {
            var rule = level?.diceInput ?? new DiceInputRule();
            if (level == null || !level.chargeReducedByCapacitor || !rule.IsCharge) return rule;
            return new DiceInputRule { diceCount = rule.diceCount, condition = rule.condition,
                value = rule.value, maxValue = rule.maxValue, chargeByDiceCount = rule.chargeByDiceCount,
                chargeTarget = Mathf.Max(1, rule.chargeTarget - (actor?.CapacitorCharge ?? 0)) };
        }
    }
}
