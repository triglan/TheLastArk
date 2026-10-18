using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Battle
{
    /// <summary>
    /// Shared order for damage, heal, shield and debuff amounts:
    /// flat additions → sum of % increases → each % decrease multiplied in turn → multipliers (×) → floor.
    /// Example: 20, +30% +50%, −10% −25% → 20 × 1.8 × 0.9 × 0.75 = 24.3 → 24.
    /// </summary>
    public sealed class ValueModifier
    {
        private float _flat;
        private float _increase;
        private readonly List<float> _decreases = new List<float>(4);
        private float _multiplier = 1f;

        /// <summary>Flat addition (may be negative).</summary>
        public ValueModifier AddFlat(float value) { _flat += value; return this; }

        /// <summary>Percent increase such as 0.3 for +30%. Increases are summed.</summary>
        public ValueModifier AddIncrease(float ratio) { if (ratio > 0f) _increase += ratio; return this; }

        /// <summary>Percent decrease such as 0.25 for −25%. Each decrease is multiplied separately.</summary>
        public ValueModifier AddDecrease(float ratio) { if (ratio > 0f) _decreases.Add(Mathf.Clamp01(ratio)); return this; }

        /// <summary>Plain multiplier (×), e.g. a critical hit.</summary>
        public ValueModifier AddMultiplier(float factor) { _multiplier *= Mathf.Max(0f, factor); return this; }

        public float Raw(float baseValue)
        {
            float value = (baseValue + _flat) * (1f + _increase);
            for (int i = 0; i < _decreases.Count; i++) value *= 1f - _decreases[i];
            return Mathf.Max(0f, value * _multiplier);
        }

        /// <summary>Final value floored and never negative.</summary>
        public float Apply(float baseValue) => Floor(Raw(baseValue));

        /// <summary>Floor that tolerates float noise (24.999999 → 25).</summary>
        public static float Floor(float value) => Mathf.Max(0f, Mathf.Floor(value + 1e-4f));
    }
}
