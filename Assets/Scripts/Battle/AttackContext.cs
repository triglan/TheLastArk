using System.Collections.Generic;

namespace TheLastArk.Battle
{
    /// <summary>
    /// One attack skill (or enemy pattern) from first to last hit. Tracks taunt lock, once-per-skill protect,
    /// and defers 반격/반사 until all hits are done.
    /// </summary>
    public sealed class AttackContext
    {
        private static readonly Stack<AttackContext> Active = new Stack<AttackContext>();

        public static AttackContext Current => Active.Count > 0 ? Active.Peek() : null;

        public BattleCharacter Attacker { get; }
        public BattleCharacter PrimaryTarget { get; private set; }
        /// <summary>Single-primary-target attack (incl. adjacent); all-target attacks ignore taunt.</summary>
        public bool TauntAffected { get => _tauntAffected && !IgnoreTaunt; private set => _tauntAffected = value; }
        private bool _tauntAffected;
        /// <summary>환각 picks a final target: taunt is skipped (protect still applies).</summary>
        public bool IgnoreTaunt { get; set; }
        /// <summary>An enemy pattern: all of its actions form one attack for protect and responses.</summary>
        public bool IsPatternScope { get; private set; }
        /// <summary>Once a hit lands on a taunter, the rest of this attack stays on it.</summary>
        public BattleCharacter LockedTaunter { get; set; }
        /// <summary>요새화 results: one resist roll per (target, status type) per attack.</summary>
        public readonly Dictionary<(BattleCharacter, EffectType), bool> ResistRolls = new Dictionary<(BattleCharacter, EffectType), bool>();

        /// <summary>Share of the target's defense ignored by the hit currently resolving (0–1).</summary>
        public float ArmorIgnore { get; set; }

        private readonly Dictionary<BattleCharacter, BattleCharacter> _guardians = new Dictionary<BattleCharacter, BattleCharacter>();
        private readonly Dictionary<BattleCharacter, float> _guardReductions = new Dictionary<BattleCharacter, float>();
        private readonly List<BattleCharacter> _defenders = new List<BattleCharacter>();
        private readonly List<DamageType> _defenderDamageTypes = new List<DamageType>();

        private AttackContext(BattleCharacter attacker, BattleCharacter primary, bool tauntAffected)
        {
            Attacker = attacker;
            PrimaryTarget = primary;
            TauntAffected = tauntAffected;
        }

        public static AttackContext Begin(BattleCharacter attacker, BattleCharacter primary, TargetType targetType)
        {
            var context = new AttackContext(attacker, primary, TargetResolver.IsTauntAffected(targetType));
            Active.Push(context);
            return context;
        }

        public static AttackContext BeginPattern(BattleCharacter attacker)
        {
            var context = new AttackContext(attacker, null, false) { IsPatternScope = true };
            Active.Push(context);
            return context;
        }

        /// <summary>Inside a pattern scope, each action sets its own primary target; the taunt lock restarts.</summary>
        public void Retarget(BattleCharacter primary, TargetType targetType)
        {
            PrimaryTarget = primary;
            TauntAffected = TargetResolver.IsTauntAffected(targetType);
            LockedTaunter = null;
        }

        /// <summary>Ends the attack and resolves responses: per defender, 반격 first, then 반사 if the attacker lives.</summary>
        public void End()
        {
            if (Active.Count > 0 && Active.Peek() == this) Active.Pop();
            for (int i = 0; i < _defenders.Count; i++)
                _defenders[i].ResolveAttackResponses(Attacker, _defenderDamageTypes[i]);
        }

        /// <summary>Protector already chosen for this ally in this attack (one protect count per attack skill).</summary>
        public BattleCharacter GetGuardian(BattleCharacter protectedAlly)
            => protectedAlly != null && _guardians.TryGetValue(protectedAlly, out var protector) ? protector : null;

        public void SetGuardian(BattleCharacter protectedAlly, BattleCharacter protector, float damageReduction)
        {
            _guardians[protectedAlly] = protector;
            _guardReductions[protectedAlly] = damageReduction;
        }

        public float GuardReduction(BattleCharacter protectedAlly)
            => protectedAlly != null && _guardReductions.TryGetValue(protectedAlly, out float reduction) ? reduction : 0f;

        public void RecordHit(BattleCharacter defender, DamageType damageType)
        {
            if (defender == null) return;
            int index = _defenders.IndexOf(defender);
            if (index >= 0) { _defenderDamageTypes[index] = damageType; return; }
            _defenders.Add(defender);
            _defenderDamageTypes.Add(damageType);
        }

        /// <summary>Test / safety hook: drops any contexts left open by an exception.</summary>
        public static void ResetAll() => Active.Clear();
    }
}
