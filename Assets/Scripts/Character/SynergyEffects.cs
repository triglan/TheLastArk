using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;

namespace TheLastArk.Character
{
    /// <summary>
    /// Active synergy tiers and the per-character bonuses they grant. Numbers come from <see cref="SynergyBalanceConfig"/>.
    /// </summary>
    public static class SynergyEffects
    {
        private static Dictionary<SynergyType, int> _cache;
        private static int _cacheFrame = -1;

        /// <summary>Test hook: fixed synergy counts instead of the run's party.</summary>
        public static Dictionary<SynergyType, int> OverrideCounts { get; set; }
        /// <summary>Test hook: fixed real party jobs for 미라주 support.</summary>
        public static HashSet<SynergyType> OverridePartyJobs { get; set; }

        public static SynergyBalanceConfig Config => SynergyBalanceConfig.Instance;

        public static readonly SynergyType[] Jobs =
        {
            SynergyType.Guardian, SynergyType.Warrior, SynergyType.Assassin, SynergyType.Ranger, SynergyType.Mage, SynergyType.Support
        };

        public static Dictionary<SynergyType, int> Counts()
        {
            if (OverrideCounts != null) return OverrideCounts;
            if (_cache != null && Application.isPlaying && _cacheFrame == Time.frameCount) return _cache;
            _cache = SynergyCalculator.CalculateActiveSynergies();
            _cacheFrame = Time.frameCount;
            return _cache;
        }

        public static void Invalidate() => _cacheFrame = -1;

        /// <summary>Highest reached tier (1-based), 0 when inactive.</summary>
        public static int RawTier(SynergyType type)
        {
            if (!Counts().TryGetValue(type, out int count) || count <= 0) return 0;
            var info = SynergyDatabase.GetInfo(type);
            if (info?.tiers == null) return 0;
            int tier = 0;
            for (int i = 0; i < info.tiers.Count; i++)
                if (count >= info.tiers[i].threshold) tier = i + 1;
            return tier;
        }

        /// <summary>Effective tier: 성도 엘리시움 only works while no other faction synergy is active.</summary>
        public static int Tier(SynergyType type)
        {
            int tier = RawTier(type);
            if (type == SynergyType.Elysium && tier > 0 && HasOtherActiveFaction(SynergyType.Elysium)) return 0;
            return tier;
        }

        public static bool HasOtherActiveFaction(SynergyType faction)
        {
            foreach (var pair in Counts())
            {
                if (pair.Key == faction || !SynergyDatabase.GetInfo(pair.Key).isFaction) continue;
                if (RawTier(pair.Key) > 0) return true;
            }
            return false;
        }

        public static int ActiveSynergyCount()
        {
            int active = 0;
            foreach (var pair in Counts())
                if (RawTier(pair.Key) > 0) active++;
            return active;
        }

        public static bool IsAlly(CharacterStatus status) => status?.origin != null && !status.origin.isEnemy;
        public static bool IsMember(CharacterStatus status, SynergyType type) => IsAlly(status) && status.HasSynergy(type);

        /// <summary>Real party jobs (virtual counts excluded) for 미라주 support.</summary>
        public static HashSet<SynergyType> PartyJobs()
        {
            if (OverridePartyJobs != null) return OverridePartyJobs;
            var jobs = new HashSet<SynergyType>();
            var battle = BattleManager.Active != null ? BattleManager.Active : Object.FindObjectOfType<BattleManager>();
            if (battle?.playerParty == null) return jobs;
            foreach (var ally in battle.playerParty)
                foreach (var job in Jobs)
                    if (ally?.status != null && ally.status.HasSynergy(job)) jobs.Add(job);
            return jobs;
        }

        private static int MirageSupportTier(SynergyType job) => PartyJobs().Contains(job) ? Tier(SynergyType.Mirage) : 0;

        /// <summary>Additive attack % (as a fraction) for this character.</summary>
        public static float AttackBonus(CharacterStatus status)
        {
            if (!IsAlly(status) || Config == null) return 0f;
            float percent = SynergyBalanceConfig.At(Config.mirageWarriorAttackPercent, MirageSupportTier(SynergyType.Warrior));
            if (IsMember(status, SynergyType.Assassin)) percent += SynergyBalanceConfig.At(Config.assassinStatPercent, Tier(SynergyType.Assassin));
            return percent * 0.01f;
        }

        /// <summary>Additive spell power % (as a fraction) for this character.</summary>
        public static float SpellPowerBonus(CharacterStatus status)
        {
            if (!IsAlly(status) || Config == null) return 0f;
            float percent = SynergyBalanceConfig.At(Config.mirageMageSpellPercent, MirageSupportTier(SynergyType.Mage));
            if (IsMember(status, SynergyType.Assassin)) percent += SynergyBalanceConfig.At(Config.assassinStatPercent, Tier(SynergyType.Assassin));
            if (IsMember(status, SynergyType.BlueTower)) percent += SynergyBalanceConfig.At(Config.blueTowerSpellPowerPercent, Tier(SynergyType.BlueTower));
            return percent * 0.01f;
        }

        private const float SkyCrossPercent = 25f;

        /// <summary>하늘 십자가: 엘리시움 members' max HP / sanity +25% while no other faction synergy is active.</summary>
        private static float SkyCross(CharacterStatus status)
            => IsMember(status, SynergyType.Elysium) && Battle.RelicRules.Has(RelicEffectType.SkyCross)
               && !HasOtherActiveFaction(SynergyType.Elysium) ? SkyCrossPercent * 0.01f : 0f;

        public static float HpBonus(CharacterStatus status) => SkyCross(status);
        public static float MentalBonus(CharacterStatus status) => SkyCross(status);

        /// <summary>Crit rate bonus in %p.</summary>
        public static float CritBonus(CharacterStatus status)
        {
            if (!IsAlly(status) || Config == null) return 0f;
            return SynergyBalanceConfig.At(Config.rangerCrit, Tier(SynergyType.Ranger))
                + SynergyBalanceConfig.At(Config.mirageAssassinCrit, MirageSupportTier(SynergyType.Assassin));
        }

        /// <summary>Total crit damage % for an attacker (base 180% plus synergy bonuses).</summary>
        public static float CritDamagePercent(CharacterStatus status)
        {
            float percent = Config != null ? Config.baseCritDamagePercent : 180f;
            if (!IsAlly(status) || Config == null) return percent;
            return percent + SynergyBalanceConfig.At(Config.rangerCritDamage, Tier(SynergyType.Ranger))
                + SynergyBalanceConfig.At(Config.mirageRangerCritDamage, MirageSupportTier(SynergyType.Ranger));
        }

        /// <summary>수호자: received HP damage decreases (each multiplied in turn).</summary>
        public static void AddGuardianReductions(CharacterStatus receiver, bool viaProtectOrTaunt, Battle.ValueModifier modifier)
        {
            if (!IsAlly(receiver) || Config == null) return;
            int tier = Tier(SynergyType.Guardian);
            if (tier <= 0) return;
            modifier.AddDecrease(SynergyBalanceConfig.At(Config.guardianTeamReductionPercent, tier) * 0.01f);
            if (viaProtectOrTaunt && receiver.HasSynergy(SynergyType.Guardian))
                modifier.AddDecrease(SynergyBalanceConfig.At(Config.guardianProtectReductionPercent, tier) * 0.01f);
        }

        /// <summary>서포터: received HP healing and shields +%.</summary>
        public static float SupportReceiveBonus(CharacterStatus receiver)
            => IsAlly(receiver) && Config != null ? SynergyBalanceConfig.At(Config.supportReceivePercent, Tier(SynergyType.Support)) * 0.01f : 0f;

        /// <summary>마법사: numeric debuffs allies put on enemies are multiplied.</summary>
        public static float DebuffMultiplier(CharacterStatus actor, CharacterStatus target)
        {
            if (!IsAlly(actor) || target?.origin == null || !target.origin.isEnemy || Config == null) return 1f;
            float multiplier = SynergyBalanceConfig.At(Config.mageDebuffMultiplier, Tier(SynergyType.Mage));
            return multiplier > 0f ? multiplier : 1f;
        }

        /// <summary>암살자 members: +n to each die's effect value (effects and sum charge only).</summary>
        public static int AssassinDiceBonus(CharacterStatus actor)
            => IsMember(actor, SynergyType.Assassin) && Config != null ? SynergyBalanceConfig.At(Config.assassinDiceBonus, Tier(SynergyType.Assassin)) : 0;

        /// <summary>속삭이는 교단: allies' sanity damage on an enemy at ≤ 50% sanity.</summary>
        public static float LowSanityMentalBonus(CharacterStatus actor, CharacterStatus target)
        {
            if (!IsAlly(actor) || target?.origin == null || !target.origin.isEnemy || Config == null) return 0f;
            if (target.currentMental > target.FinalMaxMental * 0.5f) return 0f;
            return SynergyBalanceConfig.At(Config.whisperLowMentalBonusPercent, Tier(SynergyType.WhisperCult)) * 0.01f;
        }
    }
}
