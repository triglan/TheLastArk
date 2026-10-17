using UnityEngine;

namespace TheLastArk.Battle
{
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
