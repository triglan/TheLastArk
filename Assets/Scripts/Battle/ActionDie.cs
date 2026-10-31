using UnityEngine;

namespace TheLastArk.Battle
{
    /// <summary>Die type: plain, 라이언하트 북방 (shield on use) or 청운 구름 (fixed original roll).</summary>
    public enum DieKind { Normal, North, Cloud }

    /// <summary>A die to place at turn start (special dice are placed before normal ones on overflow).</summary>
    public readonly struct DieSpec
    {
        public readonly int Faces;
        public readonly DieKind Kind;
        /// <summary>Fixed original roll (≥ 0) that rerolls keep, or -1 to roll normally.</summary>
        public readonly int FixedRoll;

        public DieSpec(int faces, DieKind kind = DieKind.Normal, int fixedRoll = -1)
        {
            Faces = faces;
            Kind = kind;
            FixedRoll = fixedRoll;
        }
    }

    /// <summary>
    /// A stored action die. The original roll and kept external modifiers are tracked separately
    /// so rerolls redraw only the roll and never re-apply modifiers.
    /// </summary>
    public sealed class ActionDie
    {
        /// <summary>Unique within a pool; never reused, independent of display position.</summary>
        public int Id { get; }
        public int Faces { get; }
        public int OriginalRoll { get; internal set; }
        /// <summary>Net sum of kept external modifiers.</summary>
        public int Modifier { get; internal set; }
        public bool IsSpecial { get; }
        public DieKind Kind { get; internal set; }
        /// <summary>Original roll stays fixed through rerolls (청운 구름).</summary>
        public bool HasFixedRoll { get; internal set; }
        /// <summary>Carried over from the previous turn (침묵의 아르카디아); not part of this turn's new roll.</summary>
        public bool IsCarried { get; internal set; }
        public bool IsLocked { get; internal set; }
        public bool IsAvailable => !IsLocked;
        /// <summary>Actual value. May exceed <see cref="Faces"/>; never below 0.</summary>
        public int Value => Mathf.Max(0, OriginalRoll + Modifier);

        internal ActionDie(int id, int faces, int originalRoll, bool isSpecial = false)
        {
            Id = id;
            Faces = faces;
            OriginalRoll = originalRoll;
            IsSpecial = isSpecial;
        }

        /// <summary>Value used for effect calculation: on-use bonuses apply here, not to input conditions.</summary>
        public int EffectValue(int onUseBonus) => Value + onUseBonus;

        /// <summary>"Max value" effects use the face count, then add the on-use bonus.</summary>
        public int MaxEffectValue(int onUseBonus) => Faces + onUseBonus;
    }
}
