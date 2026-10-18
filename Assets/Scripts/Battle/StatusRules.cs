namespace TheLastArk.Battle
{
    public enum StatusStacking
    {
        /// <summary>Each application is its own instance with its own duration.</summary>
        Instance,
        /// <summary>Time-only effect: one instance, remaining = max(remaining, new).</summary>
        TimeOnly,
        /// <summary>Count effect: one instance, count = max(remaining, new).</summary>
        Count,
        /// <summary>반사: ratios summed, count = max.</summary>
        ReflectMerge
    }

    /// <summary>Status-effect classification shared by application, ticking and display.</summary>
    public static class StatusRules
    {
        public const int DefaultStatTurns = 3;
        public const float PoisonDecay = 0.9f;
        public const float BurnHealDecrease = 0.5f;

        public static StatusStacking Stacking(EffectType type)
        {
            switch (type)
            {
                case EffectType.Stun:
                case EffectType.Fatigue:
                case EffectType.Focus:
                case EffectType.Blockade:
                    return StatusStacking.TimeOnly;
                case EffectType.Taunt:
                case EffectType.Counter:
                case EffectType.Guard:
                    return StatusStacking.Count;
                case EffectType.Reflect:
                    return StatusStacking.ReflectMerge;
                default:
                    return StatusStacking.Instance;
            }
        }

        /// <summary>Max-type: the largest active instance applies. Everything else sums.</summary>
        public static bool IsMaxType(EffectType type)
        {
            return type == EffectType.Bleed || type == EffectType.Fear
                || type == EffectType.Pressure || type == EffectType.Frost;
        }

        /// <summary>Applied at round end, then decremented. Other turn effects tick at their side's turn end.</summary>
        public static bool TicksAtRoundEnd(EffectType type)
        {
            return type == EffectType.Bleed || type == EffectType.Poison
                || type == EffectType.Burn || type == EffectType.Fear;
        }

        public static bool IsStat(EffectType type)
        {
            return type == EffectType.Strength || type == EffectType.Weakness
                || type == EffectType.Amplification || type == EffectType.Frailty
                || type == EffectType.Protection || type == EffectType.Vulnerable
                || type == EffectType.MagicGuard || type == EffectType.Corrosion;
        }

        /// <summary>Player-only action limits; enemy patterns ignore them.</summary>
        public static bool IsPlayerOnly(EffectType type)
        {
            return type == EffectType.Blockade || type == EffectType.Fatigue || type == EffectType.Frost;
        }
    }
}
