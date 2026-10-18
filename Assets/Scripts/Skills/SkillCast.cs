using System.Collections.Generic;

namespace TheLastArk.Skills
{
    /// <summary>
    /// Inputs of one actual skill fire that effects scale with: dice effect values (D, ΣD),
    /// pre-fire shield snapshots (S, T) and the matched-second-use flag. Also collects the HP damage dealt.
    /// </summary>
    public sealed class SkillCast
    {
        /// <summary>Effect value per die (on-use bonus and 냉기 floor applied).</summary>
        public readonly List<int> EffectValues = new List<int>();
        /// <summary>Input value per die (냉기 applied, no on-use bonus) — used for matching.</summary>
        public readonly List<int> InputValues = new List<int>();
        public float SelfShield;
        public float AllyShieldTotal;
        public bool MatchedSecondUse;
        /// <summary>Actual HP damage dealt by this skill's direct hits (shield absorption excluded).</summary>
        public float HealthDamageDealt;
        /// <summary>Enemies this skill attacked.</summary>
        public readonly HashSet<BattleCharacter> HitTargets = new HashSet<BattleCharacter>();
        /// <summary>Extra physical damage % (as a fraction) for this fire, e.g. 전사.</summary>
        public float PhysicalDamageBonus;
        /// <summary>Effect values of consumed 북방 dice (라이언하트 shields after the effect).</summary>
        public readonly List<int> NorthValues = new List<int>();
        /// <summary>First die consumed by this fire (기름칠된 톱니바퀴 returns it).</summary>
        public TheLastArk.Battle.ActionDie FirstDie;
        /// <summary>배반자: flat damage added once to the primary target's first hit.</summary>
        public float FirstHitBonus;
        public bool FirstHitDone;
        /// <summary>Actual magic HP damage per target (정신 분열의 룬 and similar, resolved once after all hits).</summary>
        public readonly Dictionary<BattleCharacter, float> MagicHealthDamage = new Dictionary<BattleCharacter, float>();
        /// <summary>Actual physical HP damage per target.</summary>
        public readonly Dictionary<BattleCharacter, float> PhysicalHealthDamage = new Dictionary<BattleCharacter, float>();
        /// <summary>"Once per target per skill" bookkeeping (key = effect name).</summary>
        public readonly HashSet<(BattleCharacter, string)> Once = new HashSet<(BattleCharacter, string)>();
        /// <summary>This fire put at least one status effect on an enemy.</summary>
        public bool AppliedStatusToEnemy;
        /// <summary>환각: the chosen target is final, so taunt is not applied.</summary>
        public bool IgnoreTaunt;
        /// <summary>At least one hit of this fire was a critical hit.</summary>
        public bool HadCrit;
        /// <summary>Extra damage % (as a fraction) for every damage type (e.g. 쌍성의 검).</summary>
        public float DamageBonus;

        public static void Add(Dictionary<BattleCharacter, float> map, BattleCharacter key, float value)
        {
            if (key == null || value <= 0f) return;
            map.TryGetValue(key, out float current);
            map[key] = current + value;
        }

        public int DiceSum
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < EffectValues.Count; i++) sum += EffectValues[i];
                return sum;
            }
        }
    }
}
