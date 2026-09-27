using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Battle
{
    public sealed class ActionDie
    {
        public int Id { get; }
        public int Value { get; }
        public bool IsSpent { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsAvailable => !IsSpent && !IsLocked;

        public ActionDie(int id, int value)
        {
            Id = id;
            Value = value;
        }

        internal void Spend() => IsSpent = true;
        internal void SetLocked(bool locked) => IsLocked = locked;
    }

    public sealed class ActionDicePool
    {
        public const int MaxDicePerTurn = 10;

        private readonly List<ActionDie> _dice = new List<ActionDie>(MaxDicePerTurn);
        public IReadOnlyList<ActionDie> Dice => _dice;
        public int CreatedThisTurn => _dice.Count;

        public void BeginTurn(int count)
        {
            _dice.Clear();
            Roll(count, 6);
        }

        public int Roll(int count, int sides)
        {
            if (count <= 0 || sides <= 0) return 0;
            int rolled = Mathf.Min(count, MaxDicePerTurn - _dice.Count);
            for (int i = 0; i < rolled; i++)
                _dice.Add(new ActionDie(_dice.Count, Random.Range(1, sides + 1)));
            return rolled;
        }

        public ActionDie Find(int id) => id >= 0 && id < _dice.Count ? _dice[id] : null;

        public void SetLocked(int id, bool locked)
        {
            Find(id)?.SetLocked(locked);
        }

        public bool TrySpend(IReadOnlyList<int> ids)
        {
            if (ids == null || ids.Count == 0) return false;
            var unique = new HashSet<int>();
            foreach (int id in ids)
                if (!unique.Add(id) || Find(id)?.IsAvailable != true) return false;
            foreach (int id in ids) _dice[id].Spend();
            return true;
        }
    }
}
