using System;
using System.Collections.Generic;

namespace TheLastArk.Battle
{
    /// <summary>
    /// Origin dice storage. Holds at most <see cref="MaxStoredDice"/> waiting dice (available + locked).
    /// Spent dice leave storage and are kept in a per-turn usage log. Generation per turn is uncapped;
    /// extra dice only fill free slots and any excess is cancelled.
    /// </summary>
    public sealed class ActionDicePool
    {
        public const int MaxStoredDice = 10;
        public const int DefaultFaces = 6;

        private readonly List<ActionDie> dice = new List<ActionDie>(MaxStoredDice);
        private readonly List<ActionDie> usedThisTurn = new List<ActionDie>();
        private readonly Func<int, int> roller;
        private int nextId;

        public IReadOnlyList<ActionDie> Dice { get; }
        public IReadOnlyList<ActionDie> UsedThisTurn { get; }
        public int GeneratedThisTurn { get; private set; }
        public int FreeSlots => MaxStoredDice - dice.Count;

        /// <param name="roller">Returns a roll in [1, faces]. Defaults to UnityEngine.Random.</param>
        public ActionDicePool(Func<int, int> roller = null)
        {
            this.roller = roller ?? (faces => UnityEngine.Random.Range(1, faces + 1));
            Dice = dice.AsReadOnly();
            UsedThisTurn = usedThisTurn.AsReadOnly();
        }

        /// <summary>Clears storage and the usage log, then rolls the turn's dice.
        /// On overflow, special dice are placed first and normal dice fill the rest.</summary>
        public void BeginTurn(int normalCount, int faces = DefaultFaces, IReadOnlyList<int> specialFaces = null)
        {
            dice.Clear();
            usedThisTurn.Clear();
            GeneratedThisTurn = 0;
            if (specialFaces != null)
                for (int i = 0; i < specialFaces.Count; i++)
                    if (IsValidFaces(specialFaces[i])) { GeneratedThisTurn++; Add(specialFaces[i], true); }
            Roll(normalCount, faces);
        }

        /// <summary>Removes remaining waiting dice at turn end.</summary>
        public void EndTurn() => dice.Clear();

        /// <returns>The number of dice actually stored (limited by free slots).</returns>
        public int Roll(int count, int faces = DefaultFaces)
        {
            if (count <= 0 || !IsValidFaces(faces)) return 0;
            GeneratedThisTurn += count;
            int added = 0;
            for (int i = 0; i < count && Add(faces, false); i++) added++;
            return added;
        }

        /// <summary>Rolls a die that resolves immediately and never enters storage.</summary>
        public int RollImmediate(int faces = DefaultFaces)
        {
            if (!IsValidFaces(faces)) return 0;
            GeneratedThisTurn++;
            return roller(faces);
        }

        public ActionDie Find(int id)
        {
            for (int i = 0; i < dice.Count; i++)
                if (dice[i].Id == id) return dice[i];
            return null;
        }

        public void SetLocked(int id, bool locked)
        {
            ActionDie die = Find(id);
            if (die != null) die.IsLocked = locked;
        }

        /// <summary>Adds a kept external modifier (e.g. +1 / -1) to a stored die.</summary>
        public bool AddModifier(int id, int delta)
        {
            ActionDie die = Find(id);
            if (die == null) return false;
            die.Modifier += delta;
            return true;
        }

        /// <summary>Redraws only the original roll; kept modifiers stay as they are.</summary>
        public bool Reroll(int id)
        {
            ActionDie die = Find(id);
            if (die == null) return false;
            die.OriginalRoll = roller(die.Faces);
            return true;
        }

        /// <summary>Validate the entire selection before consuming anything.</summary>
        public bool TrySpend(IReadOnlyList<int> ids)
        {
            if (ids == null || ids.Count == 0 || ids.Count > dice.Count) return false;
            for (int i = 0; i < ids.Count; i++)
            {
                ActionDie die = Find(ids[i]);
                if (die == null || !die.IsAvailable) return false;
                for (int j = 0; j < i; j++)
                    if (ids[j] == ids[i]) return false;
            }
            for (int i = 0; i < ids.Count; i++)
            {
                ActionDie die = Find(ids[i]);
                dice.Remove(die);
                usedThisTurn.Add(die);
            }
            return true;
        }

        private bool Add(int faces, bool special)
        {
            if (dice.Count >= MaxStoredDice) return false;
            dice.Add(new ActionDie(nextId++, faces, roller(faces), special));
            return true;
        }

        private static bool IsValidFaces(int faces) => faces > 0 && faces != int.MaxValue;
    }
}
