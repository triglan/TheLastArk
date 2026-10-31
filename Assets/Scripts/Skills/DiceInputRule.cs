using System;
using System.Collections.Generic;

namespace TheLastArk.Skills
{
    public enum DiceCondition { Any, Exact, AtLeast, AtMost, Odd, Even, Range }

    [Serializable]
    public sealed class DiceInputRule
    {
        public int diceCount = 1;
        public DiceCondition condition;
        public int value = 1;
        [UnityEngine.Tooltip("Range 조건의 최댓값 (value ~ maxValue)")]
        public int maxValue = 6;
        public int chargeTarget;
        public bool chargeByDiceCount;

        public bool Accepts(IReadOnlyList<int> values)
        {
            if (values == null || values.Count != Math.Max(1, diceCount)) return false;
            foreach (int die in values)
                if (!AcceptsValue(die)) return false;
            return true;
        }

        public bool AcceptsValue(int die)
        {
            return condition == DiceCondition.Any
                || condition == DiceCondition.Exact && die == value
                || condition == DiceCondition.AtLeast && die >= value
                || condition == DiceCondition.AtMost && die <= value
                || condition == DiceCondition.Odd && die % 2 == 1
                || condition == DiceCondition.Even && die % 2 == 0
                || condition == DiceCondition.Range && die >= value && die <= maxValue;
        }

        public bool IsCharge => chargeTarget > 0;

        /// <summary>Charge contribution. On-use bonus adds to sum-type contribution only;
        /// count-type counts each die (including value 0) as 1.</summary>
        public int ChargeFrom(IReadOnlyList<int> values, int onUseBonus = 0)
        {
            if (values == null) return 0;
            if (chargeByDiceCount) return values.Count;
            int total = 0;
            foreach (int die in values) total += Math.Max(0, die + onUseBonus);
            return total;
        }

        public string ShortLabel() => $"{Math.Max(1, diceCount)}개 · {Requirement()}";

        private string Requirement() => condition == DiceCondition.Exact ? $"{value}"
            : condition == DiceCondition.AtLeast ? $"{value} 이상"
            : condition == DiceCondition.AtMost ? $"{value} 이하"
            : condition == DiceCondition.Odd ? "홀수"
            : condition == DiceCondition.Even ? "짝수"
            : condition == DiceCondition.Range ? $"{value}~{maxValue}" : "자유";

        public string Describe()
        {
            return $"주사위 {Math.Max(1, diceCount)}개 ({Requirement()})";
        }
    }
}
