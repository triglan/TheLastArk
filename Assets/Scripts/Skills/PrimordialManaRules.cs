using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Skills
{
    public static class PrimordialManaRules
    {
        public const int LandThreshold = 20;

        public static bool UsesBothLandEffects(SkillLevelData level, CharacterStatus actor)
            => level != null && actor != null && level.primalManaDualEffectThreshold > 0
                && actor.PrimalMana >= level.primalManaDualEffectThreshold;

        public static SkillLevelData Resolve(SkillLevelData source, CharacterStatus actor)
        {
            if (source == null || source.special != SkillSpecial.MagicLand || actor == null) return source;
            bool attack = actor.PrimalMana >= LandThreshold;
            bool both = attack && UsesBothLandEffects(source, actor);
            var level = source.RuntimeCopy();
            level.special = SkillSpecial.None; // Freeze the branch before spending mana.
            level.targetType = attack ? TargetType.AllEnemy : TargetType.Self;
            level.primalManaCost = attack ? 5 : 0;
            level.primalManaGain = !attack || both ? 5 : 0;
            level.primalManaGainBeforeEffects = !attack || both;
            level.primalManaCostAfterShield = both;
            level.effects = new List<EffectEntry>();
            // Authoring order cannot move the mana payment ahead of the combined shield.
            if (!attack || both)
                foreach (var effect in source.effects)
                    if (effect != null && effect.type == EffectType.Shield) level.effects.Add(effect);
            if (attack)
                foreach (var effect in source.effects)
                    if (effect != null && effect.type == EffectType.Damage) level.effects.Add(effect);
            return level;
        }

        public static string UnavailableReason(CharacterStatus actor, SkillLevelData level)
        {
            if (actor == null || level == null) return null;
            int required = Mathf.Max(level.primalManaRequired, level.primalManaCost);
            return actor.PrimalMana < required ? $"원초 마력 {required} 필요 · 현재 {actor.PrimalMana}" : null;
        }

        public static void GainActiveMana(CharacterStatus actor, SkillLevelData level, int die)
        {
            int amount = level.primalManaGain + (level.primalManaGainFromDie ? Mathf.Max(0, die) : 0);
            if (die <= 2) amount += level.primalManaLowDieBonus;
            if (amount > 0) actor.GainPrimalMana(amount);
        }

        public static void AfterSkill(CharacterStatus actor, SkillCast cast = null)
        {
            var passive = actor.GetPassiveEntry(PassiveTrait.PrimordialMana);
            if (passive == null || cast == null || cast.ManaAwarded) return;
            cast.ManaAwarded = true;
            foreach (int die in cast.EffectValues)
                actor.GainPrimalMana(Mathf.Max(0, die) + Mathf.RoundToInt(passive.value));
        }
    }
}
