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
