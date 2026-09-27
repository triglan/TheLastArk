using NUnit.Framework;
using TheLastArk.Battle;
using TheLastArk.Skills;

public class ActionDicePoolTests
{
    [Test]
    public void DiceRulesAndSpending()
    {
        var pool = new ActionDicePool();
        pool.BeginTurn(4);
        Assert.AreEqual(4, pool.CreatedThisTurn);
        Assert.AreEqual(6, pool.Roll(20, 6));
        Assert.AreEqual(ActionDicePool.MaxDicePerTurn, pool.CreatedThisTurn);
        Assert.IsFalse(pool.TrySpend(new[] { 0, 0 }));
        Assert.IsTrue(pool.Find(0).IsAvailable);
        pool.SetLocked(1, true);
        Assert.IsFalse(pool.TrySpend(new[] { 0, 1 }));
        Assert.IsTrue(pool.TrySpend(new[] { 0 }));
        Assert.IsFalse(pool.Find(0).IsAvailable);

        var rule = new DiceInputRule { diceCount = 2, condition = DiceCondition.AtLeast, value = 3 };
        Assert.IsTrue(rule.Accepts(new[] { 3, 5 }));
        Assert.IsFalse(rule.Accepts(new[] { 3, 2 }));
        Assert.AreEqual(8, rule.ChargeFrom(new[] { 3, 5 }));
        rule.chargeByDiceCount = true;
        Assert.AreEqual(2, rule.ChargeFrom(new[] { 3, 5 }));

        rule.diceCount = 1;
        rule.condition = DiceCondition.Odd;
        Assert.IsTrue(rule.Accepts(new[] { 5 }));
        Assert.IsFalse(rule.Accepts(new[] { 4 }));
        Assert.AreEqual("1개 · 홀수", rule.ShortLabel());
        rule.condition = DiceCondition.Even;
        Assert.IsTrue(rule.Accepts(new[] { 4 }));
        Assert.IsFalse(rule.Accepts(new[] { 5 }));
    }
}
