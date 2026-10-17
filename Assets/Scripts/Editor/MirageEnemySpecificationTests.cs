using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using TheLastArk.Editor;

public class MirageEnemySpecificationTests
{
    private readonly List<Object> owned = new List<Object>();

    private BattleCharacter Character(string name = null, float hp = 1000f, float armor = 0f)
    {
        CharacterData data = name == null ? ScriptableObject.CreateInstance<CharacterData>()
            : Object.Instantiate(Resources.Load<CharacterData>($"Characters/Enemy/Data/{name}_Data"));
        Assert.IsNotNull(data, name);
        owned.Add(data);
        if (name == null) { data.maxHp = hp; data.maxMental = 1000f; data.armor = armor; data.baseAttack = 10f; }
        GameObject root = new GameObject(name ?? "Specification Target");
        owned.Add(root);
        var c = root.AddComponent<BattleCharacter>();
        c.status = new CharacterStatus(data);
        data.characterName = name ?? "Target";
        return c;
    }

    private EnemyAI AI(BattleCharacter c, params BattleCharacter[] targets)
    {
        var ai = c.gameObject.AddComponent<EnemyAI>();
        ai.targetParty = targets.ToList();
        ai.allyParty = new List<BattleCharacter> { c };
        return ai;
    }

    private static void Turn(EnemyAI ai)
    {
        ai.GetComponent<BattleCharacter>().status.RemoveEffectsExpiringAtOwnerPhaseStart();
        Assert.IsTrue(ai.PrepareTurn());
        ai.ExecuteTurn();
    }

    [TearDown]
    public void Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
        owned.Clear();
    }

    [Test]
    public void CollectorHitsThreeTimesAndGrowsAfterAttack()
    {
        var c = Character("징수자"); var t = Character(armor: 1); var ai = AI(c, t);
        Turn(ai); Turn(ai);
        Assert.AreEqual(991f, t.status.currentHp); // (3 + 1 - 1) * 3
        Assert.AreEqual(2f, c.status.GetStatusValue(EffectType.Strength));
    }

    [Test]
    public void BellGrowthAffectsAllThreeAttacks()
    {
        var c = Character("종말의 종지기"); var t = Character(); var ai = AI(c, t);
        for (int i = 0; i < 4; i++) Turn(ai);
        Assert.AreEqual(991f, t.status.currentMental);
    }

    [Test]
    public void DuelistAlternatesCounterAndEightDamage()
    {
        var c = Character("계약 결투사"); var t = Character(); var ai = AI(c, t);
        Turn(ai);
        c.ReceiveDamage(1, t); c.ReceiveDamage(1, t);
        Assert.AreEqual(990f, t.status.currentHp);
        Turn(ai);
        Assert.AreEqual(982f, t.status.currentHp);
        Assert.IsNull(c.status.GetStatus(EffectType.Counter));
        Turn(ai); c.ReceiveDamage(1, t);
        Assert.AreEqual(976f, t.status.currentHp);
    }

    [Test]
    public void CrossbowUsesPreparedTargetAndHalfArmor()
    {
        var c = Character("흑철 쇠뇌수"); var a = Character(armor: 4); var b = Character(armor: 4); var ai = AI(c, a, b);
        Assert.IsTrue(ai.PrepareTurn());
        Assert.That(ai.PreparedIntent.Forecast, Does.Contain("9 × 1"));
        var target = ai.PreparedTargets[0]; ai.ExecuteTurn(); Turn(ai);
        Assert.AreEqual(993f, target.status.currentHp);
        Assert.AreEqual(1993f, a.status.currentHp + b.status.currentHp);
        Assert.IsNull(c.status.GetStatus(EffectType.Pierce));
    }

    [Test]
    public void PowderStartsAtThreeHitsThenFour()
    {
        var c = Character("화약 행상인"); var t = Character(); var ai = AI(c, t);
        ai.PrepareTurn(); Assert.That(ai.PreparedIntent.Forecast, Does.Contain("5 × 3"));
        for (int i = 0; i < 6; i++) Turn(ai);
        Assert.AreEqual(965f, t.status.currentHp);
    }

    [Test]
    public void BeetleMoltsTwiceWithReducedSecondStrength()
    {
        var c = Character("탈피 딱정벌레"); var t = Character(); var ai = AI(c, t);
        Turn(ai); Turn(ai);
        Assert.AreEqual(0f, c.status.FinalArmor); Assert.AreEqual(2f, c.status.GetStatusValue(EffectType.Strength));
        for (int i = 0; i < 4; i++) Turn(ai);
        Assert.AreEqual(3f, c.status.GetStatusValue(EffectType.Strength));
        Assert.AreEqual(2, ai.RuntimeState.Get("molt"));
    }

    [Test]
    public void BloodBladeAttacksTwiceBeforeDetonationWithoutTickingDuration()
    {
        var c = Character("피칼잡이"); var a = Character(); var b = Character(); var ai = AI(c, a, b);
        Turn(ai);
        var hit = a.status.currentHp < 1000 ? a : b;
        Assert.IsNotNull(hit.status.GetStatus(EffectType.Bleed));
        Turn(ai);
        float before = a.status.currentHp + b.status.currentHp;
        int bleeding = new[] { a, b }.Count(x => x.status.GetStatus(EffectType.Bleed) != null);
        Turn(ai);
        Assert.AreEqual(before - 2 * bleeding, a.status.currentHp + b.status.currentHp);
        Assert.AreEqual(3, hit.status.GetStatus(EffectType.Bleed).remainingTurns);
    }

    [Test]
    public void HookBreaksOnlyShieldAndRemembersTheHitTarget()
    {
        var c = Character("갈고리 약탈자"); var a = Character(); var b = Character(); var ai = AI(c, a, b);
        a.status.ApplyStatusEffect(EffectType.Shield, 6, 1); b.status.ApplyStatusEffect(EffectType.Shield, 6, 1);
        Turn(ai); var hit = ai.RuntimeState.RememberedTarget;
        Assert.AreEqual(1000, hit.status.currentHp);
        Assert.AreEqual(0f, hit.status.GetStatusValue(EffectType.Shield));
        Turn(ai); Assert.AreEqual(992, hit.status.currentHp);
    }

    [Test]
    public void RiderDropsArmorOnChargeAndRemountsOnFourthAction()
    {
        var c = Character("흑철 기수"); var t = Character(armor: 4); var ai = AI(c, t);
        Turn(ai); Turn(ai);
        Assert.AreEqual(995f, t.status.currentHp); Assert.AreEqual(0f, c.status.FinalArmor);
        Turn(ai); Assert.AreEqual(0f, c.status.FinalArmor);
        Turn(ai); Assert.AreEqual(2f, c.status.FinalArmor); Assert.AreEqual(1f, c.status.GetStatusValue(EffectType.Strength));
    }

    [Test]
    public void CaptainGuardsOthersAndLowersShieldImmediatelyAfterThreeHits()
    {
        var c = Character("흑철 호위대장"); var ally = Character("흑철 방패병"); var t = Character(); var ai = AI(c, t);
        ai.allyParty.Add(ally);
        Turn(ai); Turn(ai);
        Assert.IsNull(c.status.GetStatus(EffectType.Guard)); Assert.IsNotNull(ally.status.GetStatus(EffectType.Guard));
        Turn(ai); Assert.AreEqual(978f, t.status.currentHp); Assert.AreEqual(0f, c.status.FinalArmor);
        Turn(ai); Assert.AreEqual(5f, c.status.FinalArmor);
    }

    [Test]
    public void RitualConsumesRoundedHealthThroughShieldsAndCapsBonus()
    {
        var c = Character("재의 인도자"); var ally = Character(hp: 21); var t = Character(); var ai = AI(c, t);
        ally.status.ApplyStatusEffect(EffectType.Shield, 100, 1);
        var sacrifice = c.status.origin.enemyPatterns[1].actions[0];
        EnemyActionExecutor.Execute(sacrifice, new EnemyActionExecutionContext(c, ai.RuntimeState, new List<BattleCharacter> { c, ally }, "ritual", null));
        Assert.AreEqual(16f, ally.status.currentHp); Assert.AreEqual(100f, ally.status.GetStatusValue(EffectType.Shield));
        Assert.AreEqual(5, ai.RuntimeState.Get("ritual")); Assert.AreEqual(42f, c.status.currentHp);
        ai.RuntimeState.Set("ritual", 100);
        foreach(var action in c.status.origin.enemyPatterns[2].actions)
            EnemyActionExecutor.Execute(action, new EnemyActionExecutionContext(c, ai.RuntimeState, new List<BattleCharacter> { t }, "release", null));
        Assert.AreEqual(992f, t.status.currentMental); Assert.AreEqual(0, ai.RuntimeState.Get("ritual"));
    }

    [Test]
    public void CamelExcludesExistingMercenariesAndDoesNotSupportItself()
    {
        var c = Character("황금낙타 대상주"); var ally = Character("흑철 방패병"); var t = Character(); var ai = AI(c, t);
        ai.allyParty.Add(ally);
        var summon = c.status.origin.enemyPatterns[1].actions[0];
        Assert.IsFalse(EnemyActionExecutor.GetSummonCandidates(summon, ai.allyParty).Any(x => x.characterName == "흑철 방패병"));
        foreach(var action in c.status.origin.enemyPatterns[2].actions)
            EnemyActionExecutor.Execute(action, new EnemyActionExecutionContext(c, ai.RuntimeState, ai.allyParty, "support", null));
        Assert.AreEqual(0f, c.status.GetStatusValue(EffectType.Shield)); Assert.AreEqual(8f, ally.status.GetStatusValue(EffectType.Shield));
        foreach(var action in c.status.origin.enemyPatterns[3].actions)
            EnemyActionExecutor.Execute(action, new EnemyActionExecutionContext(c, ai.RuntimeState, action.targetType == TargetType.Self ? new List<BattleCharacter> { c } : new List<BattleCharacter> { t }, "magic", null));
        Assert.AreEqual(1f, c.status.GetStatusValue(EffectType.Amplification));
    }

    [Test]
    public void TwinGlaiveGrowsHitCountsAndCapsAtThreeExtraHits()
    {
        var c = Character("쌍월도 대장"); var t = Character(); var ai = AI(c, t);
        Turn(ai); Turn(ai); Turn(ai);
        Assert.AreEqual(966f, t.status.currentHp); Assert.AreEqual(2f, t.status.GetStatusValue(EffectType.Bleed));
        Turn(ai); Assert.AreEqual(0f, c.status.FinalArmor);
        Turn(ai); Assert.AreEqual(950f, t.status.currentHp); Assert.AreEqual(6f, c.status.FinalArmor);
        ai.RuntimeState.Set("attack_bonus", 99); Assert.AreEqual(3, ai.RuntimeState.Get("attack_bonus"));
    }

    [Test]
    public void LastHitBleedIsSkippedWhenCounterInterruptsTheCombo()
    {
        var c = Character("쌍월도 대장"); var t = Character(); var ai = AI(c, t);
        Turn(ai); Turn(ai); t.status.activeStatusEffects.Clear();
        c.status.currentHp = 1;
        t.status.ApplyStatusEffect(EffectType.Counter, 50, 0, 0, 1, -1, t, StatusDurationType.Charges);
        Turn(ai);
        Assert.AreEqual(0f, c.status.currentHp); Assert.IsNull(t.status.GetStatus(EffectType.Bleed));
    }

    [Test]
    public void DevourerTracksItsOwnShieldDamageAndRejectsVictimDamage()
    {
        var c = Character("사막을 삼키는 자"); var t = Character(); var ai = AI(c, t);
        Turn(ai); Turn(ai); Turn(ai);
        Assert.AreSame(c, ai.RuntimeState.TrackedTarget); Assert.AreEqual(0f, c.status.FinalArmor);
        t.ReceiveDamage(20, null, DamageType.True, false); Assert.AreEqual(0, ai.RuntimeState.TrackedDamage);
        c.status.ApplyStatusEffect(EffectType.Shield, 10, 1);
        c.ReceiveDamage(6, t, DamageType.True, false);
        Assert.AreEqual(6f, ai.RuntimeState.TrackedDamage);
        float before = t.status.currentHp; Turn(ai);
        Assert.AreEqual(before, t.status.currentHp); Assert.AreEqual(0f, c.status.GetStatusValue(EffectType.Strength));
        Turn(ai); Assert.AreEqual(10f, c.status.FinalArmor); Assert.AreEqual(8, ai.RuntimeState.Get("threshold"));
    }

    [Test]
    public void CactusReflectsAttackStatAndLanternDealsOneScaledMentalHit()
    {
        var c = Character("가시 선인장"); var t = Character(); var ai = AI(c, t);
        Turn(ai); c.ReceiveDamage(4, t); Assert.AreEqual(995f, t.status.currentHp);
        Assert.AreEqual(2, c.status.GetStatus(EffectType.Reflect).remainingCharges);
        var lantern = Character("영혼등불지기"); var lanternAI = AI(lantern, t);
        for(int i=0;i<6;i++) Turn(lanternAI);
        Assert.AreEqual(990f, t.status.currentMental);
        Assert.AreEqual(1, lantern.status.origin.enemyPatterns[2].actions[0].effect.hitCount);
    }

    [Test]
    public void SupplyAlwaysHasExactlyOneCompanionAndArtNamesNormalize()
    {
        foreach (var formation in Resources.LoadAll<EnemyEncounterData>("Battle/Encounters"))
            if (formation.EnemySlots.Any(x => x != null && x.characterName == "보급 마차꾼"))
                Assert.AreEqual(2, formation.EnemySlots.Count(x => x != null), formation.name);
        Assert.AreEqual(MirageMonsterArtImporter.NormalizeName("무장 상인"), MirageMonsterArtImporter.NormalizeName("무장_상인"));
        Assert.AreEqual(MirageMonsterArtImporter.NormalizeName("무장 상인"), MirageMonsterArtImporter.NormalizeName("무장 상인".Normalize(System.Text.NormalizationForm.FormD)));
    }
}
