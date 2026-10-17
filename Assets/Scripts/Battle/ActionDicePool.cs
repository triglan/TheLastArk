using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Battle
{
    /// <summary>Turn-local dice. Spent dice remain visible and still count toward the creation cap.</summary>
    public sealed class ActionDicePool
    {
        public const int MaxDicePerTurn = 10;
        private readonly List<ActionDie> dice = new List<ActionDie>(MaxDicePerTurn);
        private readonly IReadOnlyList<ActionDie> readOnlyDice;

        public IReadOnlyList<ActionDie> Dice => readOnlyDice;
        public int CreatedThisTurn => dice.Count;

        public ActionDicePool()
        {
            readOnlyDice = dice.AsReadOnly();
        }

        public void BeginTurn(int count)
        {
            dice.Clear();
            Roll(count);
        }

        /// <returns>The number actually created within this turn's cap.</returns>
        public int Roll(int count, int sides = 6)
        {
            if (count <= 0 || sides <= 0 || sides == int.MaxValue) return 0;
            int added = Mathf.Min(count, MaxDicePerTurn - CreatedThisTurn);
            for (int i = 0; i < added; i++)
                dice.Add(new ActionDie(dice.Count, Random.Range(1, sides + 1)));
            return added;
        }

        public ActionDie Find(int id) => id >= 0 && id < dice.Count ? dice[id] : null;

        public void SetLocked(int id, bool locked)
        {
            ActionDie die = Find(id);
            if (die != null) die.IsLocked = locked;
        }

        /// <summary>Validate the entire selection before consuming anything.</summary>
        public bool TrySpend(IReadOnlyList<int> ids)
        {
            if (ids == null || ids.Count == 0 || ids.Count > MaxDicePerTurn) return false;
            int selectedMask = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                ActionDie die = Find(ids[i]);
                if (die == null || !die.IsAvailable) return false;
                int bit = 1 << die.Id;
                if ((selectedMask & bit) != 0) return false;
                selectedMask |= bit;
            }
            for (int i = 0; i < dice.Count; i++)
                if ((selectedMask & (1 << i)) != 0) dice[i].IsSpent = true;
            return true;
        }
    }
}
