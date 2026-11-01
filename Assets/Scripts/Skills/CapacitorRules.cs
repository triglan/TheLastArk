using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Skills
{
    /// <summary>Ferrum's battle-only resource. Gaining a shield never counts as absorbing damage.</summary>
    public static class CapacitorRules
    {
        public static void OnTurnStart(List<BattleCharacter> party)
        {
            if (party == null) return;
            foreach (var member in party)
            {
                if (member == null || member.status == null || member.status.currentHp <= 0f) continue;
                var passive = member.status.GetPassiveEntry(PassiveTrait.IronCapacitor);
                if (passive == null) continue;
                member.status.GainCapacitor(Mathf.RoundToInt(passive.value));
                member.view?.UpdateVisual(member.status);
            }
        }

        public static void OnShieldAbsorbed(BattleCharacter receiver, float absorbed)
        {
            if (absorbed <= 0f || receiver?.status?.origin == null) return;
            var battle = BattleManager.Active;
            if (battle == null) return;
            var party = receiver.status.origin.isEnemy ? battle.enemyParty : battle.playerParty;
            if (party == null || !party.Contains(receiver)) return;
            foreach (var member in party)
            {
                if (member == null || member.status == null || member.status.currentHp <= 0f) continue;
                var passive = member.status.GetPassiveEntry(PassiveTrait.IronCapacitor);
                if (passive == null || absorbed < Mathf.Max(1f, passive.secondaryValue)) continue;
                if (member != receiver && !member.status.IsTraitAwakened) continue;
                member.status.GainCapacitor(1); // One per hit, even when more than 5 is absorbed.
                member.view?.UpdateVisual(member.status);
                battle.RefreshSkillSlots();
            }
        }

        public static int SpendAmount(CharacterStatus actor, SkillLevelData level)
        {
            if (actor == null || level == null || actor.CapacitorCharge < level.capacitorSpendThreshold) return 0;
            return level.consumeAllCapacitor ? actor.CapacitorCharge : Mathf.Min(actor.CapacitorCharge, level.capacitorMaxSpend);
        }

        public static void BeforeSkill(CharacterStatus actor, SkillLevelData level, SkillCast cast)
        {
            actor.GainCapacitor(level.capacitorGain);
            int spent = actor.SpendCapacitor(SpendAmount(actor, level));
            if (cast != null) cast.SpentCapacitor = spent;
        }

        public static void AfterSkill(CharacterStatus actor, SkillLevelData level, SkillCast cast)
            => actor.GainCapacitor(Mathf.FloorToInt((cast?.SpentCapacitor ?? 0) * level.capacitorRefundPercent * .01f));
    }
}
