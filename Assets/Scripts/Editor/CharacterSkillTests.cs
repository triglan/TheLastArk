using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TheLastArk.Battle;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.Skills;
using UnityEngine;

/// <summary>Stage-4 character skills, driven by the real 알렉스·이지스·카에데·세레스 assets.</summary>
public class CharacterSkillTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private const string Alex = "Temp_Data", Aegis = "Guardian_Data", Kaede = "Assassin_Data", Ceres = "Pathfinder_Data";
    private readonly List<Object> _owned = new List<Object>();
    private object _priorTrain, _priorResources;
    private BattleManager _battle;
    private TrainManager _train;

    private T Own<T>(T item) where T : Object { _owned.Add(item); return item; }
    private static FieldInfo StaticField(System.Type type, string name) => type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);

    [SetUp]
    public void Setup()
    {
        AttackContext.ResetAll();
        _priorResources = StaticField(typeof(ResourceManager), "instance").GetValue(null);
        StaticField(typeof(ResourceManager), "instance").SetValue(null, Own(new GameObject("SkillTestResources")).AddComponent<ResourceManager>());
        _priorTrain = StaticField(typeof(TrainManager), "instance").GetValue(null);
        _train = Own(new GameObject("SkillTestTrain")).AddComponent<TrainManager>();
        StaticField(typeof(TrainManager), "instance").SetValue(null, _train);
        _train.nexusCar = new TrainCar("넥서스 칸", TrainCarType.Nexus, 0);
        _battle = Own(new GameObject("SkillTestBattle")).AddComponent<BattleManager>();
        _battle.playerParty = new List<BattleCharacter>();
        _battle.enemyParty = new List<BattleCharacter>();
        typeof(BattleManager).GetField("_currentPhase", Private).SetValue(_battle, BattlePhase.PlayerTurn);
    }

    [TearDown]
    public void Cleanup()
    {
        AttackContext.ResetAll();
        EffectEngine.CurrentCast = null;
        for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
        _owned.Clear();
        StaticField(typeof(TrainManager), "instance").SetValue(null, _priorTrain);
        StaticField(typeof(ResourceManager), "instance").SetValue(null, _priorResources);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private BattleCharacter Hero(string asset, int upgrade = 0, bool bloom = false)
    {
        var source = Resources.Load<CharacterData>($"Characters/Player/Data/{asset}");
        Assert.IsNotNull(source, asset);
        var data = Own(Object.Instantiate(source));
        data.critRate = 0f; // deterministic damage
        var hero = Own(new GameObject(asset)).AddComponent<BattleCharacter>();
        hero.status = new CharacterStatus(data) { skillUpgradeOverride = upgrade, traitBloomOverride = bloom ? 1 : 0 };
        hero.status.dynamicActiveSkill.AddRange(data.activeSkills);
        _battle.playerParty.Add(hero);
        return hero;
    }

    private BattleCharacter Enemy(float hp = 100f, float armor = 0f)
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.isEnemy = true; data.maxHp = hp; data.maxMental = 100f; data.armor = armor; data.baseAttack = 10f;
        var enemy = Own(new GameObject("Enemy")).AddComponent<BattleCharacter>();
        enemy.status = new CharacterStatus(data);
        _battle.enemyParty.Add(enemy);
        return enemy;
    }

    private ActionDicePool Dice(params int[] values)
    {
        int index = 0;
        var pool = new ActionDicePool(f => values[index++ % values.Length]);
        typeof(BattleManager).GetField("_dicePool", Private).SetValue(_battle, pool);
        pool.BeginTurn(values.Length);
        return pool;
    }

    private static SkillInfo Skill(BattleCharacter hero, string name) => hero.status.origin.activeSkills[System.Array.FindIndex(hero.status.origin.activeSkills, s => s.skillName == name)];
    private static SkillLevelData Level(BattleCharacter hero, string name) => Skill(hero, name).levels[hero.status.SkillLevelIndex];

    private void Select(params int[] ids)
    {
        var selected = (List<int>)typeof(BattleManager).GetField("_selectedDiceIds", Private).GetValue(_battle);
        selected.Clear();
        selected.AddRange(ids);
    }

    private void Fire(BattleCharacter hero, string skillName, BattleCharacter target, params int[] dieIds)
    {
        Select(dieIds);
        var selection = (BattleSelectionState)typeof(BattleManager).GetField("_selection", Private).GetValue(_battle);
        selection.Set(Skill(hero, skillName), hero);
        typeof(BattleManager).GetMethod("PerformSkillOnTarget", Private).Invoke(_battle, new object[] { target });
    }

    private void Insert(BattleCharacter hero, string skillName, params int[] dieIds)
    {
        Select(dieIds);
        _battle.SelectSkill(Skill(hero, skillName), hero);
    }

    private void NewTurn()
    {
        foreach (var hero in _battle.playerParty) hero.status.TickSideTurnEnd();
        typeof(BattleManager).GetMethod("ApplyPlayerTurnStartAP", Private).Invoke(_battle, null);
    }

    private bool Accepts(BattleCharacter hero, string skillName, int value) => Level(hero, skillName).diceInput.AcceptsValue(value);

    // ── 알렉스 ──────────────────────────────────────────────────────────────

    [TestCase(0, 93f)]
    [TestCase(1, 91f)]
    public void AlexCleaveScalesWithDie(int upgrade, float expectedHp)
    {
        var alex = Hero(Alex, upgrade); var enemy = Enemy();
        Dice(6);
        Fire(alex, "가르기", enemy, 0);
        Assert.AreEqual(expectedHp, enemy.status.currentHp, "A5 × (30/70 + 6×20)%");
    }

    [Test]
    public void AlexCleaveUpgrade2SplashesNeighbours()
    {
        var alex = Hero(Alex, 2); var left = Enemy(); var mid = Enemy(); var right = Enemy();
        Dice(6);
        Fire(alex, "가르기", mid, 0);
        Assert.AreEqual(91f, mid.status.currentHp);
        Assert.AreEqual(96f, left.status.currentHp, "50% of 9.5 → 4");
        Assert.AreEqual(96f, right.status.currentHp);
    }

    [Test]
    public void AlexShieldBashStunChanceCapsAt100()
    {
        var alex = Hero(Alex, 2); var enemy = Enemy();
        Dice(6);
        Fire(alex, "방패치기", enemy, 0);
        Assert.AreEqual(96f, enemy.status.currentHp, "A5 × (20 + 60)% = 4");
        Assert.IsNotNull(enemy.status.GetStatus(EffectType.Stun), "50 + 6×10 ≥ 100%");
    }

    [Test]
    public void AlexCounterStanceInputRangeAndCounts()
    {
        var alex = Hero(Alex, 0);
        Assert.IsFalse(Accepts(alex, "반격태세", 0));
        Assert.IsTrue(Accepts(alex, "반격태세", 1));
        Assert.IsTrue(Accepts(alex, "반격태세", 2));
        Assert.IsFalse(Accepts(alex, "반격태세", 3));
        alex.status.skillUpgradeOverride = 1;
        Assert.IsTrue(Accepts(alex, "반격태세", 3));
        Assert.IsFalse(Accepts(alex, "반격태세", 4));
        alex.status.skillUpgradeOverride = 2;
        Assert.IsTrue(Accepts(alex, "반격태세", 4));
        Assert.IsFalse(Accepts(alex, "반격태세", 5));

        Enemy(); Dice(4);
        Fire(alex, "반격태세", alex, 0);
        Assert.AreEqual(2, alex.status.GetStatus(EffectType.Taunt).remainingCharges);
        Assert.AreEqual(2, alex.status.GetStatus(EffectType.Counter).remainingCharges);
    }

    [Test]
    public void AlexRegroupChargesThenHealsAndShields()
    {
        var alex = Hero(Alex, 0); Enemy();
        Assert.AreEqual(15, Level(alex, "재정비").diceInput.chargeTarget);
        alex.status.currentHp = 20f;
        Dice(6, 6, 6);
        Insert(alex, "재정비", 0); Insert(alex, "재정비", 1); Insert(alex, "재정비", 2);
        Assert.IsTrue(_battle.IsChargeReady(alex, Skill(alex, "재정비")));
        Fire(alex, "재정비", alex);
        Assert.AreEqual(24f, alex.status.currentHp, "Heal A × 80% = 4");
        Assert.AreEqual(4f, alex.status.GetStatusValue(EffectType.Shield));
        alex.status.skillUpgradeOverride = 2;
        Assert.AreEqual(12, Level(alex, "재정비").diceInput.chargeTarget);
    }

    [Test]
    public void OffenseDefenseShieldsOnlyFirstAttackUnlessBloomed()
    {
        var alex = Hero(Alex, 0); var enemy = Enemy();
        Dice(6, 6);
        Fire(alex, "가르기", enemy, 0);
        Assert.AreEqual(3f, alex.status.GetStatusValue(EffectType.Shield), "50% of 7 HP damage");
        Fire(alex, "방패치기", enemy, 1);
        Assert.AreEqual(3f, alex.status.GetStatusValue(EffectType.Shield), "Only the first attack of the turn");

        alex.status.RemoveAllStatusEffects();
        alex.status.traitBloomOverride = 1;
        NewTurn();
        Dice(6, 6);
        Fire(alex, "가르기", enemy, 0);
        Fire(alex, "방패치기", enemy, 1);
        Assert.AreEqual(7f + 4f, alex.status.GetStatusValue(EffectType.Shield), "Bloom: 100% on every attack (7 + 4)");
    }

    // ── 이지스 ──────────────────────────────────────────────────────────────

    [Test]
    public void AegisSilverWedgeUsesOwnOrPartyShield()
    {
        var aegis = Hero(Aegis, 0); var ally = Hero(Kaede); var enemy = Enemy();
        aegis.status.ApplyStatusEffect(EffectType.Shield, 10f, 1);
        ally.status.ApplyStatusEffect(EffectType.Shield, 6f, 1);
        Dice(4, 4);
        Fire(aegis, "은빛 쐐기", enemy, 0);
        Assert.AreEqual(96f, enemy.status.currentHp, "S10 × 4 × 10%");
        aegis.status.skillUpgradeOverride = 2;
        NewTurn(); Dice(4);
        Fire(aegis, "은빛 쐐기", enemy, 0);
        Assert.AreEqual(87f, enemy.status.currentHp, "T16 × 4 × 15% = 9.6 → 9");
    }

    [Test]
    public void AegisGuardShieldConditionAndOtherAllyOnly()
    {
        var aegis = Hero(Aegis, 0); var ally = Hero(Kaede); Enemy();
        Assert.IsTrue(Accepts(aegis, "수호 방패", 3));
        Assert.IsFalse(Accepts(aegis, "수호 방패", 4));
        Assert.IsTrue(Level(aegis, "수호 방패").excludeSelf, "Other allies only");
        Dice(3);
        Fire(aegis, "수호 방패", ally, 0);
        Assert.AreEqual(2, ally.status.GetStatus(EffectType.Guard).remainingCharges);
        Assert.AreSame(aegis, ally.status.GetStatus(EffectType.Guard).source);
        Assert.AreEqual(15f, aegis.status.GetStatusValue(EffectType.Shield), "D3 × 5 on self");
        aegis.status.skillUpgradeOverride = 2;
        Assert.IsTrue(Accepts(aegis, "수호 방패", 4));
    }

    [Test]
    public void AegisVanguardCondition()
    {
        var aegis = Hero(Aegis, 0);
        Assert.IsFalse(Accepts(aegis, "선봉자", 3));
        Assert.IsTrue(Accepts(aegis, "선봉자", 4));
        aegis.status.skillUpgradeOverride = 1;
        Assert.IsTrue(Accepts(aegis, "선봉자", 3));
    }

    [Test]
    public void AegisGrandChargeConsumesOwnShield()
    {
        var aegis = Hero(Aegis, 0); var ally = Hero(Kaede); var a = Enemy(); var b = Enemy();
        Dice(6, 6, 6, 2);
        for (int i = 0; i < 4; i++) Insert(aegis, "대돌진", i);
        Assert.IsTrue(_battle.IsChargeReady(aegis, Skill(aegis, "대돌진")));
        aegis.status.ApplyStatusEffect(EffectType.Shield, 20f, 1);
        Fire(aegis, "대돌진", a);
        Assert.AreEqual(80f, a.status.currentHp, "S20 × 100% to every enemy");
        Assert.AreEqual(80f, b.status.currentHp);
        Assert.AreEqual(5f, ally.status.GetStatusValue(EffectType.Shield), "S20 × 25% to every ally");
        Assert.AreEqual(5f, aegis.status.GetStatusValue(EffectType.Shield), "Own shield consumed, then gets the ally share");
    }

    [Test]
    public void GuardianOathReducesProtectedDamage()
    {
        var aegis = Hero(Aegis, 0); var ally = Hero(Kaede); var enemy = Enemy();
        ally.status.ApplyStatusEffect(EffectType.Guard, 0f, 0, 0f, 1, -1, aegis);
        float before = aegis.status.currentHp;
        EffectEngine.ProcessSkill(enemy, new List<BattleCharacter> { ally }, new SkillLevelData { effects = new List<EffectEntry> {
            new EffectEntry { type = EffectType.Damage, damageType = DamageType.Physical, multiplier = 0f, fixedValue = 20f } } });
        float armor = aegis.status.FinalArmor;
        Assert.AreEqual(Mathf.Floor((20f - armor) * 0.75f), before - aegis.status.currentHp, "25% protect reduction after the protector's defense");
    }

    // ── 카에데 ──────────────────────────────────────────────────────────────

    [Test]
    public void KaedeStabLimitsInputUsesAndExtraHits()
    {
        var kaede = Hero(Kaede, 1); var enemy = Enemy();
        Assert.IsTrue(Accepts(kaede, "단검 찌르기", 0));
        Assert.IsTrue(Accepts(kaede, "단검 찌르기", 2));
        Assert.IsFalse(Accepts(kaede, "단검 찌르기", 3));
        Dice(2, 2, 2, 2);
        Fire(kaede, "단검 찌르기", enemy, 0);
        Assert.AreEqual(93f, enemy.status.currentHp, "7×(60+20)% = 5 + extra 7×40% = 2");
        Assert.AreEqual(3f, enemy.status.GetStatusValue(EffectType.Bleed));
        Fire(kaede, "단검 찌르기", enemy, 1);
        Fire(kaede, "단검 찌르기", enemy, 2);
        Assert.AreEqual(3, _battle.GetSkillUsesThisTurn(kaede, Skill(kaede, "단검 찌르기")));
        float hpBefore = enemy.status.currentHp;
        Fire(kaede, "단검 찌르기", enemy, 3);
        Assert.AreEqual(hpBefore, enemy.status.currentHp, "+1: 3 uses per turn, the 4th is refused");
        Assert.AreEqual(3, _battle.GetSkillUsesThisTurn(kaede, Skill(kaede, "단검 찌르기")));
    }

    [Test]
    public void BlackDeathMakesZeroDieEveryThreeAttacks()
    {
        var kaede = Hero(Kaede, 0); var enemy = Enemy(1000f);
        var pool = Dice(1, 1, 1, 1, 6);
        Fire(kaede, "단검 찌르기", enemy, 0);
        Fire(kaede, "단검 찌르기", enemy, 1);
        Assert.IsFalse(System.Linq.Enumerable.Any(pool.Dice, d => d.Value == 0));
        Fire(kaede, "절개", enemy, 2, 3);
        Assert.IsTrue(System.Linq.Enumerable.Any(pool.Dice, d => d.Value == 0 && d.OriginalRoll == 0), "3rd actual attack fire generates a 0 die");
    }

    [Test]
    public void SplitDieKeepsTotalCarriesModifierAndRespectsStorage()
    {
        var kaede = Hero(Kaede, 0);
        var pool = Dice(3);
        pool.AddModifier(0, 2);
        var split = Skill(kaede, "연운·안개");
        Assert.IsFalse(_battle.TrySplitDie(kaede, split, 0, new[] { 2, 2 }, 0), "Total must stay 5");
        Assert.IsTrue(_battle.TrySplitDie(kaede, split, 0, new[] { 1, 4 }, 1));
        Assert.AreEqual(2, pool.Dice.Count);
        Assert.AreEqual(1, pool.Dice[0].Value); Assert.AreEqual(0, pool.Dice[0].Modifier);
        Assert.AreEqual(4, pool.Dice[1].Value); Assert.AreEqual(2, pool.Dice[1].Modifier, "Modifier moves to the chosen die");
        Assert.AreEqual(BattleManager.DiceReasonUsed, _battle.GetDiceSkillUnavailableReason(kaede, split));

        NewTurn();
        pool = Dice(1, 1, 1, 1, 1, 1, 1, 1, 1, 6);
        Assert.AreEqual(BattleManager.DiceReasonStorageFull, _battle.GetDiceSkillUnavailableReason(kaede, split));
        Assert.IsFalse(_battle.TrySplitDie(kaede, split, 9, new[] { 3, 3 }, 0));
        Assert.AreEqual(10, pool.Dice.Count, "Nothing consumed");
        Assert.AreEqual(0, _battle.GetSkillUsesThisTurn(kaede, split));
    }

    [Test]
    public void IncisionScalesWithTargetBleed()
    {
        var kaede = Hero(Kaede, 0); var enemy = Enemy();
        enemy.status.ApplyStatusEffect(EffectType.Bleed, 4f, 3);
        Dice(3, 3);
        Fire(kaede, "절개", enemy, 0, 1);
        Assert.AreEqual(80f, enemy.status.currentHp, "7×(120+6×20)% = 16.8, +4×5% → 20.16 → 20");
        Assert.AreEqual(2, enemy.status.activeStatusEffects.FindAll(e => e.effectType == EffectType.Bleed).Count, "Bleed not consumed; new bleed added");
    }

    [Test]
    public void AmbushMatchesSecondUseInputValue()
    {
        var kaede = Hero(Kaede, 0); var enemy = Enemy(1000f);
        Dice(3, 3, 3, 4);
        Fire(kaede, "암습·구름", enemy, 0);
        Assert.AreEqual(996f, enemy.status.currentHp, "One hit of 7×70% = 4");
        Fire(kaede, "암습·구름", enemy, 1);
        Assert.AreEqual(988f, enemy.status.currentHp, "Matched second use: two hits");

        NewTurn(); Dice(3, 4);
        Fire(kaede, "암습·구름", enemy, 0);
        Fire(kaede, "암습·구름", enemy, 1);
        Assert.AreEqual(988f - 4f - 5f, enemy.status.currentHp, "Mismatch: one hit each (D3 → 4, D4 → 7×80% = 5)");
    }

    [Test]
    public void AmbushUpgrade2MatchedHitsIgnoreHalfArmor()
    {
        var kaede = Hero(Kaede, 2); var enemy = Enemy(1000f, armor: 4f);
        Dice(3, 3);
        Fire(kaede, "암습·구름", enemy, 0);
        Assert.AreEqual(1000f - 3f, enemy.status.currentHp, "7×110% = 7.7 − 4 = 3");
        Fire(kaede, "암습·구름", enemy, 1);
        Assert.AreEqual(997f - 5f - 5f, enemy.status.currentHp, "Matched: 7.7 − 2 = 5 per hit");
    }

    // ── 세레스 ──────────────────────────────────────────────────────────────

    [Test]
    public void CeresChanceCaptureMarksAndDebuffs()
    {
        var ceres = Hero(Ceres, 1); var enemy = Enemy();
        Dice(5);
        Fire(ceres, "기회 포착", enemy, 0);
        Assert.AreEqual(93f, enemy.status.currentHp, "M6 × 120% = 7 physical");
        Assert.IsNotNull(enemy.status.GetStatus(EffectType.HuntMark));
        Assert.AreEqual(3f, enemy.status.GetStatusValue(EffectType.Vulnerable), "⌊5÷2⌋ + 1");
        Assert.AreEqual(3f, enemy.status.GetStatusValue(EffectType.Corrosion));
        Assert.IsNull(ceres.status.GetStatus(EffectType.Focus), "+1 focus lasts only for this attack");
    }

    [Test]
    public void SicEmBoostsEffectNotInputAndStacksThenExpires()
    {
        var ceres = Hero(Ceres, 0); var kaede = Hero(Kaede, 0); Enemy();
        Dice(1, 1, 2);
        Fire(ceres, "저쪽을 공격해!", kaede, 0);
        Fire(ceres, "저쪽을 공격해!", kaede, 1);
        Assert.AreEqual(2, kaede.status.activeStatusEffects.FindAll(e => e.effectType == EffectType.DiceBoost).Count, "Re-use on the same ally stacks");
        Assert.AreEqual(2, _battle.GetDiceOnUseBonus(kaede, Skill(kaede, "단검 찌르기")));
        Assert.IsTrue(Accepts(kaede, "단검 찌르기", 2), "Input condition unchanged by the bonus");
        Fire(ceres, "저쪽을 공격해!", kaede, 2);
        Assert.AreEqual(2, kaede.status.activeStatusEffects.FindAll(e => e.effectType == EffectType.DiceBoost).Count, "Third use exceeds 2/turn: nothing applied");

        kaede.status.TickSideTurnEnd();
        Assert.AreEqual(0, _battle.GetDiceOnUseBonus(kaede, null), "Unused bonuses vanish at turn end");
    }

    [Test]
    public void SicEmBonusConsumedPerDieOnSuccessOnly()
    {
        var ceres = Hero(Ceres, 2); var kaede = Hero(Kaede, 0); var enemy = Enemy();
        var pool = Dice(0, 2, 3);
        Fire(ceres, "저쪽을 공격해!", kaede, 0);
        Assert.AreEqual(4, _battle.GetDiceOnUseBonus(kaede, null));
        Fire(kaede, "단검 찌르기", enemy, 2);
        Assert.AreEqual(2, pool.Dice.Count, "Die 3 fails 0~2: nothing consumed");
        Assert.AreEqual(4, _battle.GetDiceOnUseBonus(kaede, null), "Failed use keeps the bonus");
        Fire(kaede, "단검 찌르기", enemy, 1);
        Assert.AreEqual(100f - Mathf.Floor(7f * (0.6f + 0.6f)), enemy.status.currentHp, "D = 2 + 4");
        Assert.AreEqual(4, _battle.GetDiceOnUseBonus(kaede, null), "+2 grants 2 dice; one left");
    }

    [Test]
    public void FrostTrapStunsOnlyFromTwoAtUpgrade2()
    {
        var ceres = Hero(Ceres, 2); var a = Enemy(); var b = Enemy();
        Dice(1, 2);
        Fire(ceres, "빙결 함정", a, 0);
        Assert.IsNull(a.status.GetStatus(EffectType.Stun));
        Assert.AreEqual(2f, a.status.GetStatusValue(EffectType.Weakness), "2 + ⌊1÷2⌋");
        NewTurn(); Dice(2);
        Fire(ceres, "빙결 함정", b, 0);
        Assert.IsNotNull(b.status.GetStatus(EffectType.Stun));
        Assert.AreEqual(3f, b.status.GetStatusValue(EffectType.Frailty));
    }

    [Test]
    public void HowlingBuffsEveryAlly()
    {
        var ceres = Hero(Ceres, 2); var kaede = Hero(Kaede, 0); Enemy();
        Dice(6);
        Fire(ceres, "하울링", ceres, 0);
        foreach (var hero in new[] { ceres, kaede })
        {
            Assert.AreEqual(4f, hero.status.GetStatusValue(EffectType.Strength), "2 + ⌊6÷3⌋");
            Assert.AreEqual(4f, hero.status.GetStatusValue(EffectType.Amplification));
            Assert.AreEqual(2f, hero.status.GetStatusValue(EffectType.Protection));
            Assert.AreEqual(2f, hero.status.GetStatusValue(EffectType.MagicGuard));
        }
    }

    [Test]
    public void HuntMarkReplacesOldestAndBoostsAllyDamage()
    {
        var ceres = Hero(Ceres, 0); var a = Enemy(); var b = Enemy();
        _battle.ApplyHuntMark(ceres, a);
        _battle.ApplyHuntMark(ceres, b);
        Assert.IsNull(a.status.GetStatus(EffectType.HuntMark), "Cap 1: the oldest mark is replaced");
        Assert.AreEqual(20f, b.status.GetStatusValue(EffectType.HuntMark));
        b.ReceiveDamage(10f, ceres, DamageType.True);
        Assert.AreEqual(88f, b.status.currentHp, "+20% direct HP damage");

        ceres.status.traitBloomOverride = 1;
        _battle.ApplyHuntMark(ceres, a);
        Assert.IsNotNull(a.status.GetStatus(EffectType.HuntMark));
        Assert.IsNotNull(b.status.GetStatus(EffectType.HuntMark), "Bloom keeps 2 marks");
    }
}
