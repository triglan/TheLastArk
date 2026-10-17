using System;
using System.Collections.Generic;
using NUnit.Framework;
using TheLastArk.Battle;
using TheLastArk.Skills;

public class ActionDicePoolTests
{
    /// <summary>Deterministic roller cycling through the given values.</summary>
    private static Func<int, int> Seq(params int[] values)
    {
        int index = 0;
        return faces => values[index++ % values.Length];
    }

    private static List<int> Ids(ActionDicePool pool)
    {
        var ids = new List<int>();
        foreach (ActionDie die in pool.Dice) ids.Add(die.Id);
        return ids;
    }

    [Test]
    public void StorageCapsWaitingDiceButNotGeneration()
    {
        var pool = new ActionDicePool(Seq(3));
        pool.BeginTurn(12);
        Assert.AreEqual(ActionDicePool.MaxStoredDice, pool.Dice.Count);
        Assert.AreEqual(12, pool.GeneratedThisTurn);
        Assert.AreEqual(0, pool.FreeSlots);
        Assert.AreEqual(0, pool.Roll(3), "Excess generation is cancelled, not carried");
        Assert.AreEqual(15, pool.GeneratedThisTurn);

        pool.SetLocked(pool.Dice[0].Id, true);
        Assert.AreEqual(ActionDicePool.MaxStoredDice, pool.Dice.Count, "Locked dice still occupy storage");
    }

    [Test]
    public void SpendingFreesSlotsAndRefillUsesNewIds()
    {
        var pool = new ActionDicePool(Seq(4));
        pool.BeginTurn(10);
        var before = Ids(pool);
        int first = before[0], second = before[1];
        Assert.IsTrue(pool.TrySpend(new[] { first, second }));
        Assert.AreEqual(8, pool.Dice.Count);
        Assert.AreEqual(2, pool.UsedThisTurn.Count);
        Assert.IsNull(pool.Find(first));

        Assert.AreEqual(2, pool.Roll(5), "Only the freed slots are refilled");
        var after = Ids(pool);
        Assert.AreEqual(after.Count, new HashSet<int>(after).Count, "Ids stay unique after reclaim");
        Assert.IsFalse(after.Contains(first));
        Assert.IsFalse(after.Contains(second));
        Assert.IsFalse(pool.TrySpend(new[] { first }), "A spent id never matches a regenerated die");
    }

    [Test]
    public void SpecialDiceArePlacedFirstOnOverflow()
    {
        var pool = new ActionDicePool(Seq(2));
        pool.BeginTurn(10, 6, new[] { 8, 8 });
        Assert.AreEqual(ActionDicePool.MaxStoredDice, pool.Dice.Count);
        Assert.IsTrue(pool.Dice[0].IsSpecial && pool.Dice[0].Faces == 8);
        Assert.IsTrue(pool.Dice[1].IsSpecial && pool.Dice[1].Faces == 8);
        int specials = 0;
        foreach (ActionDie die in pool.Dice) if (die.IsSpecial) specials++;
        Assert.AreEqual(2, specials);
        Assert.AreEqual(12, pool.GeneratedThisTurn);
    }

    [Test]
    public void ValueKeepsOriginalAndModifierSeparately()
    {
        var pool = new ActionDicePool(Seq(5, 2));
        pool.BeginTurn(1);
        ActionDie die = pool.Dice[0];
        Assert.AreEqual(5, die.OriginalRoll);
        Assert.IsTrue(pool.AddModifier(die.Id, 3));
        Assert.AreEqual(8, die.Value, "Value may exceed the face count");
        Assert.AreEqual(3, die.Modifier);

        Assert.IsTrue(pool.Reroll(die.Id));
        Assert.AreEqual(2, die.OriginalRoll, "Reroll redraws only the original roll");
        Assert.AreEqual(3, die.Modifier, "Kept modifiers are not reset or re-applied");
        Assert.AreEqual(5, die.Value);

        pool.AddModifier(die.Id, -10);
        Assert.AreEqual(0, die.Value, "Actual value never drops below 0");
        Assert.AreEqual(-7, die.Modifier);
        Assert.IsFalse(pool.AddModifier(999, 1));
        Assert.IsFalse(pool.Reroll(999));
    }

    [Test]
    public void OnUseBonusAffectsEffectsAndMaxUsesFaces()
    {
        var pool = new ActionDicePool(Seq(3));
        pool.BeginTurn(1);
        ActionDie die = pool.Dice[0];
        Assert.AreEqual(5, die.EffectValue(2));
        Assert.AreEqual(8, die.MaxEffectValue(2));

        var rule = new DiceInputRule { condition = DiceCondition.AtLeast, value = 4 };
        Assert.IsFalse(rule.AcceptsValue(die.Value), "On-use bonus is not part of input conditions");
    }

    [Test]
    public void ImmediateDiceNeverEnterStorage()
    {
        var pool = new ActionDicePool(Seq(6));
        pool.BeginTurn(0);
        Assert.AreEqual(6, pool.RollImmediate());
        Assert.AreEqual(0, pool.Dice.Count);
        Assert.AreEqual(1, pool.GeneratedThisTurn);
        Assert.AreEqual(0, pool.RollImmediate(0));
    }

    [Test]
    public void InvalidSelectionDoesNotSpendAndTurnBoundariesReset()
    {
        var pool = new ActionDicePool(Seq(1, 2, 3));
        pool.BeginTurn(3);
        var ids = Ids(pool);
        Assert.IsFalse(pool.TrySpend(new[] { ids[0], 99 }));
        Assert.IsFalse(pool.TrySpend(new int[0]));
        Assert.IsFalse(pool.TrySpend(null));
        Assert.IsFalse(pool.TrySpend(new[] { ids[0], ids[0] }));
        Assert.AreEqual(3, pool.Dice.Count);
        pool.SetLocked(ids[1], true);
        Assert.IsFalse(pool.TrySpend(new[] { ids[0], ids[1] }));
        Assert.AreEqual(3, pool.Dice.Count);
        Assert.AreEqual(0, pool.UsedThisTurn.Count);
        pool.SetLocked(ids[1], false);
        Assert.IsTrue(pool.TrySpend(new[] { ids[0], ids[1] }));
        Assert.AreEqual(1, pool.UsedThisTurn[0].Value);

        pool.EndTurn();
        Assert.AreEqual(0, pool.Dice.Count, "Remaining storage dice are removed at turn end");
        pool.BeginTurn(1);
        Assert.AreEqual(1, pool.Dice.Count);
        Assert.AreEqual(0, pool.UsedThisTurn.Count, "Usage log is per turn");
        Assert.AreEqual(1, pool.GeneratedThisTurn);
        Assert.AreEqual(0, pool.Roll(1, 0));
        Assert.AreEqual(0, pool.Roll(-1));
    }

    [Test]
    public void DiceRulesAndChargeContribution()
    {
        var rule = new DiceInputRule { diceCount = 2, condition = DiceCondition.AtLeast, value = 3 };
        Assert.IsTrue(rule.Accepts(new[] { 3, 5 }));
        Assert.IsFalse(rule.Accepts(new[] { 3, 2 }));
        Assert.AreEqual(8, rule.ChargeFrom(new[] { 3, 5 }));
        Assert.AreEqual(12, rule.ChargeFrom(new[] { 3, 5 }, 2), "On-use bonus adds to sum-type charge");
        Assert.AreEqual(0, rule.ChargeFrom(new[] { 0 }));
        rule.chargeByDiceCount = true;
        Assert.AreEqual(2, rule.ChargeFrom(new[] { 3, 5 }, 2));
        Assert.AreEqual(1, rule.ChargeFrom(new[] { 0 }), "A 0 die still counts as 1 for count-type");

        rule.diceCount = 1;
        rule.condition = DiceCondition.Any;
        Assert.IsTrue(rule.Accepts(new[] { 0 }));
        rule.condition = DiceCondition.Odd;
        Assert.IsTrue(rule.Accepts(new[] { 5 }));
        Assert.IsFalse(rule.Accepts(new[] { 4 }));
        Assert.AreEqual("1개 · 홀수", rule.ShortLabel());
        rule.condition = DiceCondition.Even;
        Assert.IsTrue(rule.Accepts(new[] { 4 }));
        Assert.IsTrue(rule.Accepts(new[] { 0 }));
        Assert.IsFalse(rule.Accepts(new[] { 5 }));
    }
}
