using NUnit.Framework;
using TheLastArk.Battle;

public class ValueModifierTests
{
    [Test]
    public void DesignExampleFloorsTo24()
    {
        var modifier = new ValueModifier().AddIncrease(0.3f).AddIncrease(0.5f).AddDecrease(0.1f).AddDecrease(0.25f);
        Assert.AreEqual(24.3f, modifier.Raw(20f), 1e-3f, "20 x 1.8 x 0.9 x 0.75");
        Assert.AreEqual(24f, modifier.Apply(20f));
    }

    [Test]
    public void FlatComesFirstAndMultipliersLast()
    {
        Assert.AreEqual(18f, new ValueModifier().AddFlat(5f).AddIncrease(0.2f).Apply(10f), "(10 + 5) x 1.2");
        Assert.AreEqual(7f, new ValueModifier().AddDecrease(0.5f).AddMultiplier(1.5f).Apply(10f), "10 x 0.5 x 1.5 = 7.5 -> 7");
        Assert.AreEqual(4.5f, new ValueModifier().AddDecrease(0.5f).AddDecrease(0.1f).Raw(10f), 1e-3f, "Decreases multiply one by one");
        Assert.AreEqual(15f, new ValueModifier().AddIncrease(0.25f).AddIncrease(0.25f).Apply(10f), "Increases are summed");
    }

    [Test]
    public void NeverNegativeAndToleratesFloatNoise()
    {
        Assert.AreEqual(0f, new ValueModifier().AddFlat(-20f).Apply(10f));
        Assert.AreEqual(0f, new ValueModifier().AddDecrease(1f).Apply(10f));
        Assert.AreEqual(3f, ValueModifier.Floor(3f * 0.1f * 10f));
        Assert.AreEqual(24f, ValueModifier.Floor(24.3f));
        Assert.AreEqual(0f, ValueModifier.Floor(-3f));
    }

    [Test]
    public void StatusClassification()
    {
        Assert.IsTrue(StatusRules.IsMaxType(EffectType.Bleed));
        Assert.IsTrue(StatusRules.IsMaxType(EffectType.Frost));
        Assert.IsFalse(StatusRules.IsMaxType(EffectType.Poison));
        Assert.IsFalse(StatusRules.IsMaxType(EffectType.Strength));
        Assert.AreEqual(StatusStacking.TimeOnly, StatusRules.Stacking(EffectType.Stun));
        Assert.AreEqual(StatusStacking.Count, StatusRules.Stacking(EffectType.Taunt));
        Assert.AreEqual(StatusStacking.ReflectMerge, StatusRules.Stacking(EffectType.Reflect));
        Assert.AreEqual(StatusStacking.Instance, StatusRules.Stacking(EffectType.Shield));
        Assert.IsTrue(StatusRules.TicksAtRoundEnd(EffectType.Fear));
        Assert.IsFalse(StatusRules.TicksAtRoundEnd(EffectType.Pressure));
        Assert.IsTrue(StatusRules.IsPlayerOnly(EffectType.Blockade));
    }
}
