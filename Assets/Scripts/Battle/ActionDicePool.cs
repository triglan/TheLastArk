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
            List<DieSpec> specials = null;
            if (specialFaces != null)
            {
                specials = new List<DieSpec>(specialFaces.Count);
                for (int i = 0; i < specialFaces.Count; i++) specials.Add(new DieSpec(specialFaces[i]));
            }
            BeginTurn(normalCount, faces, specials, null);
        }

        /// <summary>
        /// Turn start placement: carried-over dice first (kept as they are), then special dice, then normal dice.
        /// Anything beyond the storage cap is cancelled.
        /// </summary>
        public void BeginTurn(int normalCount, int faces, IReadOnlyList<DieSpec> specials, IReadOnlyList<ActionDie> carried)
        {
            dice.Clear();
            usedThisTurn.Clear();
            GeneratedThisTurn = 0;
            if (carried != null)
                for (int i = 0; i < carried.Count && dice.Count < MaxStoredDice; i++)
                {
                    if (carried[i] == null) continue;
                    carried[i].IsCarried = true;
                    carried[i].IsLocked = false;
                    dice.Add(carried[i]);
                }
            if (specials != null)
                for (int i = 0; i < specials.Count; i++)
                    if (IsValidFaces(specials[i].Faces)) { GeneratedThisTurn++; AddSpec(specials[i], true); }
            Roll(normalCount, faces);
        }

        /// <summary>Removes the given waiting dice from storage (for carry-over) and returns them.</summary>
        public List<ActionDie> TakeForCarry(IReadOnlyList<int> ids)
        {
            var taken = new List<ActionDie>();
            if (ids == null) return taken;
            for (int i = 0; i < ids.Count; i++)
            {
                ActionDie die = Find(ids[i]);
                if (die == null || taken.Contains(die)) continue;
                dice.Remove(die);
                taken.Add(die);
            }
            return taken;
        }

        /// <summary>Adds one die of a given spec mid-turn (storage cap applies).</summary>
        public bool AddDie(DieSpec spec)
        {
            if (!IsValidFaces(spec.Faces)) return false;
            GeneratedThisTurn++;
            return AddSpec(spec, spec.Kind != DieKind.Normal || spec.Faces != DefaultFaces);
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
            if (!die.HasFixedRoll) die.OriginalRoll = roller(die.Faces);
            return true;
        }

        /// <summary>
        /// Player-driven ±delta on one available die. Invalid (no change) when the die is missing/locked
        /// or a decrease would push an actual value of 0 below 0.
        /// </summary>
        public bool TryAdjust(int id, int delta)
        {
            ActionDie die = Find(id);
            if (die == null || !die.IsAvailable || delta == 0) return false;
            if (delta < 0 && die.Value + delta < 0) return false;
            die.Modifier += delta;
            return true;
        }

        /// <summary>Rerolls the given available dice together. Validates all ids first; nothing changes on failure.</summary>
        public bool TryReroll(IReadOnlyList<int> ids)
        {
            if (ids == null || ids.Count == 0) return false;
            for (int i = 0; i < ids.Count; i++)
            {
                ActionDie die = Find(ids[i]);
                if (die == null || !die.IsAvailable) return false;
                for (int j = 0; j < i; j++)
                    if (ids[j] == ids[i]) return false;
            }
            for (int i = 0; i < ids.Count; i++) Reroll(ids[i]);
            return true;
        }

        /// <summary>Rerolls every available (unlocked) die in storage.</summary>
        public bool TryRerollAll()
        {
            bool any = false;
            for (int i = 0; i < dice.Count; i++)
            {
                if (!dice[i].IsAvailable) continue;
                if (!dice[i].HasFixedRoll) dice[i].OriginalRoll = roller(dice[i].Faces);
                any = true;
            }
            return any;
        }

        /// <summary>Adds a die with a fixed actual value (e.g. 흑사's 0 die). Respects the storage cap.</summary>
        public bool AddFixedDie(int faces, int value)
        {
            if (!IsValidFaces(faces)) return false;
            GeneratedThisTurn++;
            if (dice.Count >= MaxStoredDice) return false;
            dice.Add(new ActionDie(nextId++, faces, Math.Max(0, value)));
            return true;
        }

        /// <summary>
        /// Splits an available die into dice with the given actual values (each ≥ 0, summing to the original's actual
        /// value). The kept modifier moves to the result at <paramref name="modifierIndex"/>. Validates everything
        /// (including free storage) before changing anything; the original goes to the usage log.
        /// </summary>
        public bool TrySplit(int id, IReadOnlyList<int> values, int modifierIndex)
        {
            ActionDie die = Find(id);
            if (die == null || !die.IsAvailable || values == null || values.Count < 2) return false;
            if (modifierIndex < 0 || modifierIndex >= values.Count) return false;
            int total = 0;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] < 0) return false;
                total += values[i];
            }
            if (total != die.Value || dice.Count - 1 + values.Count > MaxStoredDice) return false;

            int index = dice.IndexOf(die);
            dice.RemoveAt(index);
            usedThisTurn.Add(die);
            for (int i = 0; i < values.Count; i++)
            {
                bool keepsModifier = i == modifierIndex;
                var part = new ActionDie(nextId++, die.Faces, keepsModifier ? values[i] - die.Modifier : values[i], die.IsSpecial);
                // The chosen result inherits the modifier and the die type (e.g. 북방); the others are plain.
                if (keepsModifier) { part.Modifier = die.Modifier; part.Kind = die.Kind; }
                dice.Insert(index + i, part);
            }
            GeneratedThisTurn += values.Count;
            return true;
        }

        /// <summary>Puts a spent die back into storage unchanged (type, roll, modifier). Storage cap applies.</summary>
        public bool ReturnDie(ActionDie die)
        {
            if (die == null || dice.Contains(die) || dice.Count >= MaxStoredDice) return false;
            usedThisTurn.Remove(die);
            die.IsLocked = false;
            dice.Add(die);
            return true;
        }

        /// <summary>Picks a random index in [0, count) using the pool's roller.</summary>
        public int PickIndex(int count) => count <= 0 ? -1 : Math.Min(count - 1, Math.Max(0, roller(count) - 1));

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

        private bool AddSpec(DieSpec spec, bool special)
        {
            if (dice.Count >= MaxStoredDice) return false;
            var die = new ActionDie(nextId++, spec.Faces, spec.FixedRoll >= 0 ? spec.FixedRoll : roller(spec.Faces), special)
            {
                Kind = spec.Kind,
                HasFixedRoll = spec.FixedRoll >= 0
            };
            dice.Add(die);
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
