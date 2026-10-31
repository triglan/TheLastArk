using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TheLastArk.Battle;
using TheLastArk.Character;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.Skills;
using UnityEngine;

/// <summary>Stage 5: synergies, relics, equipment and the train catalyst.</summary>
public class Stage5RuleTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly List<Object> _owned = new List<Object>();
    private object _priorTrain, _priorResources, _priorRun;
    private BattleManager _battle;
    private TrainManager _train;
    private ResourceManager _resources;
    private RunManager _run;

    private T Own<T>(T item) where T : Object { _owned.Add(item); return item; }
    private static FieldInfo StaticField(System.Type type, string name) => type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);

    [SetUp]
    public void Setup()
    {
        AttackContext.ResetAll();
        _priorResources = StaticField(typeof(ResourceManager), "instance").GetValue(null);
        _priorRun = StaticField(typeof(RunManager), "_instance").GetValue(null);
        _priorTrain = StaticField(typeof(TrainManager), "instance").GetValue(null);
        var holder = Own(new GameObject("Stage5Managers")); holder.SetActive(false);
        _resources = holder.AddComponent<ResourceManager>();
        _run = holder.AddComponent<RunManager>();
        StaticField(typeof(ResourceManager), "instance").SetValue(null, _resources);
        StaticField(typeof(RunManager), "_instance").SetValue(null, _run);
        _train = Own(new GameObject("Stage5Train")).AddComponent<TrainManager>();
        StaticField(typeof(TrainManager), "instance").SetValue(null, _train);
        _train.nexusCar = new TrainCar("넥서스 칸", TrainCarType.Nexus, 0);
        _battle = Own(new GameObject("Stage5Battle")).AddComponent<BattleManager>();
        _battle.playerParty = new List<BattleCharacter>();
        _battle.enemyParty = new List<BattleCharacter>();
        typeof(BattleManager).GetField("_currentPhase", Private).SetValue(_battle, BattlePhase.PlayerTurn);
        SynergyEffects.OverrideCounts = new Dictionary<SynergyType, int>();
        SynergyEffects.OverridePartyJobs = new HashSet<SynergyType>();
    }

    [TearDown]
    public void Cleanup()
    {
        AttackContext.ResetAll();
        EffectEngine.CurrentCast = null;
        SynergyEffects.OverrideCounts = null;
        SynergyEffects.OverridePartyJobs = null;
        for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
        _owned.Clear();
        StaticField(typeof(ResourceManager), "instance").SetValue(null, _priorResources);
        StaticField(typeof(RunManager), "_instance").SetValue(null, _priorRun);
        StaticField(typeof(TrainManager), "instance").SetValue(null, _priorTrain);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private BattleCharacter Unit(bool enemy, float hp = 100f, float attack = 10f, float armor = 0f, float magicResist = 0f,
        float spell = 0f, params SynergyType[] synergies)
    {
        var data = Own(ScriptableObject.CreateInstance<CharacterData>());
        data.isEnemy = enemy; data.maxHp = hp; data.maxMental = 100f; data.baseAttack = attack; data.armor = armor;
        data.magicResist = magicResist; data.spellPower = spell; data.critRate = 0f;
        data.synergies = new List<SynergyType>(synergies);
        var unit = Own(new GameObject(enemy ? "Enemy" : "Ally")).AddComponent<BattleCharacter>();
        unit.status = new CharacterStatus(data);
        (enemy ? _battle.enemyParty : _battle.playerParty).Add(unit);
        return unit;
    }

    private SkillInfo Attack(BattleCharacter actor, float damage = 5f, int charge = 0, TargetType type = TargetType.SingleEnemy)
    {
        var skill = new SkillInfo { skillName = "테스트 공격" + actor.status.dynamicActiveSkill.Count, levels = new[] {
            new SkillLevelData { targetType = type, diceInput = new DiceInputRule { condition = DiceCondition.Any, chargeTarget = charge },
                effects = new List<EffectEntry> { new EffectEntry { type = EffectType.Damage, damageType = DamageType.Physical, multiplier = 0f, fixedValue = damage } } } } };
        actor.status.dynamicActiveSkill.Add(skill);
        return skill;
    }

    private ActionDicePool Dice(int[] values, IReadOnlyList<DieSpec> specials = null)
    {
        int index = 0;
        var pool = new ActionDicePool(f => values[index++ % values.Length]);
        typeof(BattleManager).GetField("_dicePool", Private).SetValue(_battle, pool);
        pool.BeginTurn(specials == null ? values.Length : 0, 6, specials, null);
        return pool;
    }

    private ActionDicePool Pool => (ActionDicePool)typeof(BattleManager).GetField("_dicePool", Private).GetValue(_battle);

    private void Select(params int[] ids)
    {
        var selected = (List<int>)typeof(BattleManager).GetField("_selectedDiceIds", Private).GetValue(_battle);
        selected.Clear(); selected.AddRange(ids);
    }

    private void Fire(BattleCharacter actor, SkillInfo skill, BattleCharacter target, params int[] ids)
    {
        Select(ids);
        ((BattleSelectionState)typeof(BattleManager).GetField("_selection", Private).GetValue(_battle)).Set(skill, actor);
        typeof(BattleManager).GetMethod("PerformSkillOnTarget", Private).Invoke(_battle, new object[] { target });
    }

    private void Call(string method, params object[] args) => typeof(BattleManager).GetMethod(method, Private).Invoke(_battle, args);

    private RelicData Relic(RelicEffectType type, float value = 0f)
    {
        var relic = Own(ScriptableObject.CreateInstance<RelicData>());
        relic.relicID = "Test_" + type + _resources.Relics.Count;
        relic.effectType = type;
        relic.effectValue = value;
        _resources.Relics.Add(relic);
        return relic;
    }

    private static void Equip(BattleCharacter unit, params string[] ids)
    {
        for (int i = 0; i < ids.Length; i++) unit.status.SetEquippedItem(i, EquipmentDatabase.GetEquipment(ids[i]));
    }

    // ── synergies ───────────────────────────────────────────────────────────

    [Test]
    public void HighestTierOnlyAndConfigValues()
    {
        SynergyEffects.OverrideCounts[SynergyType.ArchiumUnion] = 5;
        Assert.AreEqual(2, SynergyEffects.Tier(SynergyType.ArchiumUnion));
        var config = SynergyBalanceConfig.Instance;
        Assert.IsNotNull(config, "Resources/Synergies/SynergyBalanceConfig must exist");
        Assert.AreEqual(1, SynergyBalanceConfig.At(config.archiumExtraDice, 2));
        Assert.AreEqual(2, SynergyBalanceConfig.At(config.archiumTopBonus, 2));
        SynergyEffects.OverrideCounts[SynergyType.SilentArcadia] = 3;
        Assert.AreEqual(3, SynergyEffects.Tier(SynergyType.SilentArcadia));
        SynergyEffects.OverrideCounts[SynergyType.Support] = 1;
        Assert.AreEqual(15f, SynergyEffects.SupportReceiveBonus(Unit(false).status) * 100f, 1e-3f);
    }

    [Test]
    public void ElysiumIgnoresJobsButStopsWithAnotherFaction()
    {
        SynergyEffects.OverrideCounts[SynergyType.Elysium] = 2;
        SynergyEffects.OverrideCounts[SynergyType.Guardian] = 4;
        Assert.AreEqual(1, SynergyEffects.Tier(SynergyType.Elysium), "Job synergies don't block it");
        SynergyEffects.OverrideCounts[SynergyType.Lionheart] = 3;
        Assert.AreEqual(0, SynergyEffects.Tier(SynergyType.Elysium));
        SynergyEffects.OverrideCounts[SynergyType.Lionheart] = 2;
        Assert.AreEqual(1, SynergyEffects.Tier(SynergyType.Elysium), "An inactive faction doesn't count");
    }

    [Test]
    public void BadgesCountAsVirtualMembersUpToTwo()
    {
        SynergyEffects.OverrideCounts = null;
        var badge = Own(ScriptableObject.CreateInstance<RelicData>());
        badge.relicID = "Badge_Test"; badge.effectType = RelicEffectType.SynergyBadge; badge.targetSynergy = SynergyType.Elysium;
        _resources.Relics.Add(badge); _resources.Relics.Add(badge); _resources.Relics.Add(badge);
        Assert.AreEqual(2, SynergyCalculator.CalculateActiveSynergies()[SynergyType.Elysium], "Same badge sums up to 2");
        Assert.IsFalse(_resources.CanAcquireRelic(badge));
    }

    [TestCase(2, false, 90f)]
    [TestCase(2, true, 81f)]
    [TestCase(4, false, 85f)]
    [TestCase(4, true, 68f)]
    public void GuardianReductionsMultiplyInTurn(int count, bool redirected, float expected)
    {
        SynergyEffects.OverrideCounts[SynergyType.Guardian] = count;
        var guardian = Unit(false, synergies: SynergyType.Guardian);
        var modifier = new ValueModifier();
        SynergyEffects.AddGuardianReductions(guardian.status, redirected, modifier);
        Assert.AreEqual(expected, modifier.Apply(100f), "Team reduction, then the protect/taunt reduction (19% / 32% total)");
    }

    [Test]
    public void LionheartShieldOnChargeInsert()
    {
        SynergyEffects.OverrideCounts[SynergyType.Lionheart] = 3;
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var actor = Unit(false); Unit(true);
        var skill = Attack(actor, charge: 20);
        Dice(new[] { 4 }, new[] { new DieSpec(6, DieKind.North) });
        Select(Pool.Dice[0].Id);
        _battle.SelectSkill(skill, actor);
        Assert.AreEqual(4, _battle.GetDiceCharge(actor, skill));
        Assert.AreEqual(4f, actor.status.GetStatusValue(EffectType.Shield), "북방 D on insert");
    }

    [Test]
    public void ArchiumBonusGoesToHighestNewRoll()
    {
        SynergyEffects.OverrideCounts[SynergyType.ArchiumUnion] = 4;
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        Unit(false);
        Dice(new[] { 3, 5, 5 });
        Call("ApplyPlayerTurnStartAP");
        Assert.AreEqual(3, Pool.Dice.Count, "2 base + 1 extra D6");
        Assert.AreEqual(0, Pool.Dice[0].Modifier);
        Assert.AreEqual(2, Pool.Dice[1].Modifier, "Highest original roll, earliest on ties");
        Assert.AreEqual(0, Pool.Dice[2].Modifier);
    }

    [Test]
    public void ArcadiaCarryCapAndMaxValueFromTurnSix()
    {
        SynergyEffects.OverrideCounts[SynergyType.SilentArcadia] = 2;
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var member = Unit(false, synergies: SynergyType.SilentArcadia);
        var pool = Dice(new[] { 2, 6, 5 });
        Assert.AreEqual(2, _battle.CarryCapacity);
        Assert.IsTrue(_battle.ToggleCarryMark(pool.Dice[0].Id));
        Assert.IsTrue(_battle.ToggleCarryMark(pool.Dice[1].Id));
        Assert.IsFalse(_battle.ToggleCarryMark(pool.Dice[2].Id), "Cap 2");
        Call("PrepareCarryOver");
        Call("ApplyPlayerTurnStartAP");
        Assert.IsTrue(Pool.Dice[0].IsCarried && Pool.Dice[1].IsCarried);
        Assert.AreEqual(2, Pool.Dice[0].Value); Assert.AreEqual(6, Pool.Dice[1].Value);

        SynergyEffects.OverrideCounts[SynergyType.SilentArcadia] = 3;
        typeof(BattleManager).GetField("_turnCount", Private).SetValue(_battle, 5);
        Call("ApplySynergyTurnStart");
        Assert.IsNull(_battle.ArcadiaMaxValueActor, "Turn 5: not yet");
        typeof(BattleManager).GetField("_turnCount", Private).SetValue(_battle, 6);
        Call("ApplySynergyTurnStart");
        Assert.AreSame(member, _battle.ArcadiaMaxValueActor);
    }

    [Test]
    public void CheongwoonFrailtyOnlyForZeroOrOneDiceOncePerTurn()
    {
        SynergyEffects.OverrideCounts[SynergyType.Cheongwoon] = 3;
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var member = Unit(false, synergies: SynergyType.Cheongwoon); var enemy = Unit(true, 1000f);
        var skill = Attack(member); var second = Attack(member);
        Dice(new[] { 2, 1, 0 });
        Fire(member, skill, enemy, Pool.Dice[0].Id);
        Assert.IsNull(enemy.status.GetStatus(EffectType.Frailty), "Die 2 doesn't qualify");
        Fire(member, second, enemy, Pool.Dice[0].Id);
        Assert.AreEqual(1f, enemy.status.GetStatusValue(EffectType.Frailty));
    }

    // ── relics ──────────────────────────────────────────────────────────────

    [Test]
    public void RenamedRelicsLoad()
    {
        Assert.AreEqual("청운의 인장", Resources.Load<RelicData>("Relics/Relic_Cheongwoon").relicName);
        Assert.AreEqual("거석의 맹세", Resources.Load<RelicData>("Relics/Relic_MegalithShield").relicName);
    }

    [TestCase(90f, 10f)]
    [TestCase(91f, 0f)]
    public void ShatterScrollBoundary(float damage, float expectedMental)
    {
        Relic(RelicEffectType.ShatterScroll, 0.1f);
        var enemy = Unit(true);
        enemy.TakeMentalDamage(damage, true);
        Assert.AreEqual(expectedMental, enemy.status.currentMental, "Below 10% (not equal) drops to 0");
        var other = Unit(true);
        other.TakeMentalDamage(91f);
        Assert.AreEqual(9f, other.status.currentMental, "Only sanity damage from allies");
    }

    [Test]
    public void TwilightBonusFromHealerAndTarget()
    {
        Assert.AreEqual(0.5f, RelicRules.TwilightBonus(new CharacterStatus(Own(ScriptableObject.CreateInstance<CharacterData>())) { currentHp = 0f }), 1e-4f);
        Relic(RelicEffectType.FlashOfTwilight, 0.5f);
        var target = Unit(false, 200f); var healer = Unit(false, 100f);
        target.status.currentHp = 110f;  // 45% lost → +25%
        healer.status.currentHp = 10f;   // 90% lost → +50%
        Assert.AreEqual(17f, target.ReceiveHeal(10f, healer), "10 × (1 + 0.25 + 0.5) = 17.5 → 17");
        Assert.AreEqual(1f, healer.ReceiveHeal(1f, healer), "Self heal counts once: 1 × 1.5 → 1");
    }

    [Test]
    public void OiledGearReturnsTheFirstDieOnce()
    {
        Relic(RelicEffectType.OiledGear, 1f);
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var actor = Unit(false); var enemy = Unit(true, 1000f);
        var skill = Attack(actor); var second = Attack(actor);
        var pool = Dice(new[] { 4, 3 });
        int first = pool.Dice[0].Id;
        Fire(actor, skill, enemy, first);
        Assert.AreEqual(2, pool.Dice.Count, "Spent die returned");
        Assert.IsNotNull(pool.Find(first));
        Fire(actor, second, enemy, first);
        Assert.AreEqual(1, pool.Dice.Count, "Only the battle's first normal active");
    }

    [Test]
    public void OneMoreDrinkUsesFrostAdjustedInput()
    {
        Relic(RelicEffectType.OneMoreDrink, 1f);
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var actor = Unit(false); var enemy = Unit(true, 1000f);
        var skill = Attack(actor); var second = Attack(actor);
        var pool = Dice(new[] { 1, 1 });
        pool.AddModifier(pool.Dice[1].Id, -1);
        actor.status.ApplyStatusEffect(EffectType.Frost, 2f, 2);
        Fire(actor, skill, enemy, pool.Dice[0].Id);
        Assert.AreEqual(1, pool.Dice.Count, "Die 1 with 냉기 2 still inputs 1: no D6");
        Fire(actor, second, enemy, pool.Dice[0].Id);
        Assert.AreEqual(1, pool.Dice.Count, "Input 0 → one new D6");
    }

    [Test]
    public void TraitorAddsShieldToFirstHitOnlyNotSplash()
    {
        Relic(RelicEffectType.Traitor, 0.5f);
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var source = Resources.Load<CharacterData>("Characters/Player/Data/Temp_Data");
        var data = Own(Object.Instantiate(source)); data.critRate = 0f;
        var alex = Own(new GameObject("Alex")).AddComponent<BattleCharacter>();
        alex.status = new CharacterStatus(data) { skillUpgradeOverride = 2, traitBloomOverride = 0 };
        alex.status.dynamicActiveSkill.AddRange(data.activeSkills);
        _battle.playerParty.Add(alex);
        var left = Unit(true); var mid = Unit(true); var right = Unit(true);
        alex.status.ApplyStatusEffect(EffectType.Shield, 10f, 1);
        Dice(new[] { 6 });
        Fire(alex, System.Array.Find(data.activeSkills, s => s.skillName == "가르기"), mid, Pool.Dice[0].Id);
        Assert.AreEqual(100f - 14f, mid.status.currentHp, "9.5 + 50% of 10 = 14.5 → 14");
        Assert.AreEqual(96f, left.status.currentHp, "Splash from the pre-bonus value (9.5 × 50%)");
        Assert.AreEqual(96f, right.status.currentHp);
        Assert.AreEqual(11f, alex.status.GetStatusValue(EffectType.Shield), "Old shield consumed; 50% of 22 HP damage (14 + splash 4 + 4)");
    }

    [Test]
    public void EndlessBattleGrantsCleaveUses()
    {
        Relic(RelicEffectType.EndlessBattle, 0.25f);
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var source = Resources.Load<CharacterData>("Characters/Player/Data/Temp_Data");
        var data = Own(Object.Instantiate(source)); data.critRate = 0f;
        var alex = Own(new GameObject("Alex")).AddComponent<BattleCharacter>();
        alex.status = new CharacterStatus(data) { skillUpgradeOverride = 2 };
        alex.status.dynamicActiveSkill.AddRange(data.activeSkills);
        _battle.playerParty.Add(alex);
        Unit(true);
        var regroup = System.Array.Find(data.activeSkills, s => s.skillName == "재정비");
        var endless = (SkillInfo)typeof(BattleCharacter).GetMethod("CreateEndlessBattleSkill", Private).Invoke(alex, new object[] { regroup });
        alex.status.dynamicActiveSkill[alex.status.dynamicActiveSkill.IndexOf(regroup)] = endless;
        Assert.AreEqual(8, endless.levels[2].diceInput.chargeTarget);
        var cleave = System.Array.Find(data.activeSkills, s => s.skillName == "가르기");
        Assert.AreEqual(1, _battle.GetSkillUsesPerTurn(alex, cleave));
        Dice(new[] { 6, 2 });
        Select(Pool.Dice[0].Id); _battle.SelectSkill(endless, alex);
        Select(Pool.Dice[0].Id); _battle.SelectSkill(endless, alex);
        Assert.IsTrue(_battle.IsChargeReady(alex, endless));
        Fire(alex, endless, alex);
        Assert.AreEqual(3, _battle.GetSkillUsesPerTurn(alex, cleave), "+2: 가르기 3 uses this turn");
    }

    // ── equipment ───────────────────────────────────────────────────────────

    [Test]
    public void DuplicateGearSumsStatsButUniqueEffectOnce()
    {
        var unit = Unit(false);
        Equip(unit, "TowerShield", "TowerShield");
        Assert.AreEqual(32f * 1.2f, unit.status.FinalArmor, 1e-3f, "16 + 16 armor, +20% only once");
    }

    [Test]
    public void SpecialGearOnePerPartyAndNoSynthesis()
    {
        var loadouts = new EquipmentLoadouts();
        var a = Unit(false); var b = Unit(false);
        a.status.origin.characterId = "01"; b.status.origin.characterId = "02";
        loadouts.Register(a.status); loadouts.Register(b.status);
        var dagger = EquipmentDatabase.GetEquipment("BloodDagger");
        var inventory = new List<EquipmentData> { dagger, dagger };
        Assert.IsTrue(loadouts.TryEquip(inventory, "01", 0, dagger, out _));
        Assert.IsFalse(loadouts.CanEquip(inventory, "02", 0, dagger, out string reason));
        StringAssert.Contains("특수", reason);
        Assert.AreEqual(0, EquipmentDatabase.GetSynthesisCandidates(dagger, dagger).Count);
    }

    [Test]
    public void SynthesisNeedsTwoOfTheSameNormalItem()
    {
        var longsword = EquipmentDatabase.GetEquipment("Longsword");
        var ids = EquipmentDatabase.GetSynthesisCandidates(longsword, longsword).ConvertAll(e => e.equipmentID);
        CollectionAssert.AreEquivalent(new[] { "Greatsword", "Hwando" }, ids);
        Assert.AreEqual(0, EquipmentDatabase.GetSynthesisCandidates(longsword, EquipmentDatabase.GetEquipment("Staff")).Count);
        var odachi = EquipmentDatabase.GetEquipment("Odachi");
        Assert.AreEqual(0, EquipmentDatabase.GetSynthesisCandidates(odachi, odachi).Count, "3-star ends synthesis");
    }

    [Test]
    public void RuneGreatswordAndRuneShieldPickTheirSide()
    {
        var unit = Unit(false, attack: 20f, armor: 3f, magicResist: 5f, spell: 5f);
        float atk = unit.status.FinalAttack, sp = unit.status.FinalSpellPower;
        Equip(unit, "RuneGreatsword");
        Assert.AreEqual(atk + 9f + 9f, unit.status.FinalAttack, "Item ATK 9 + 마검사 9 on the higher stat");
        Assert.AreEqual(sp + 9f, unit.status.FinalSpellPower, "Only the item's SP 9");
        Equip(unit, "RuneShield");
        Assert.AreEqual(3f + 10f, unit.status.FinalArmor, 1e-3f, "Armor 13 vs MR 5: +7 goes to the lower (MR)");
        Assert.AreEqual(5f + 7f, unit.status.FinalMagicResist, 1e-3f);
    }

    [TestCase(39f, 14f)]
    [TestCase(40f, 28f)]
    public void MegalithHalvesPhysicalDamageBelowTwentyPercent(float hp, float expectedDamage)
    {
        var wearer = Unit(false, 200f); var enemy = Unit(true);
        Equip(wearer, "MegalithShield");
        wearer.status.currentHp = hp;
        wearer.ReceiveDamage(50f, enemy, DamageType.Physical);
        Assert.AreEqual(expectedDamage, hp - wearer.status.currentHp, "(50 − 22 armor) = 28, ×0.5 only while HP < 20% of 200");
    }

    [Test]
    public void MagicShellNegatesFirstDirectHitBeforeShield()
    {
        var wearer = Unit(false); var enemy = Unit(true);
        Equip(wearer, "AegisArmor");
        wearer.status.ApplyStatusEffect(EffectType.Bleed, 3f, 2);
        StatusEffectPhaseHandler.ProcessAll(new List<BattleCharacter> { wearer });
        Assert.IsFalse(wearer.magicShellUsed, "DoT doesn't use it");
        wearer.status.ApplyStatusEffect(EffectType.Shield, 5f, 1);
        float hp = wearer.status.currentHp;
        wearer.ReceiveDamage(10f, enemy, DamageType.True);
        Assert.AreEqual(hp, wearer.status.currentHp);
        Assert.AreEqual(5f, wearer.status.GetStatusValue(EffectType.Shield), "Negated before the shield");
        wearer.ReceiveDamage(10f, enemy, DamageType.True);
        Assert.AreEqual(hp - 5f, wearer.status.currentHp, "Second hit: shield 5 then HP 5");
    }

    [Test]
    public void MoonlightBoostsNextDieButNotReadyChargeFire()
    {
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var wearer = Unit(false); var enemy = Unit(true, 1000f);
        Equip(wearer, "MoonlightNecklace");
        Call("ApplyEquipmentTurnStart");
        Assert.AreEqual(2, _battle.GetDiceOnUseBonus(wearer, null));
        var charge = Attack(wearer, charge: 1);
        var pool = Dice(new[] { 3, 3 });
        typeof(BattleManager).GetField("_diceCharge", Private).GetValue(_battle).GetType().GetMethod("set_Item")
            .Invoke(typeof(BattleManager).GetField("_diceCharge", Private).GetValue(_battle), new object[] { (wearer, charge), 1 });
        Fire(wearer, charge, enemy);
        Assert.AreEqual(2, _battle.GetDiceOnUseBonus(wearer, null), "Ready charge fire uses no dice and keeps 달빛");
        var hit = Attack(wearer, 0f);
        hit.levels[0].effects[0].fixedPerDie = 1f;
        Fire(wearer, hit, enemy, pool.Dice[0].Id);
        Assert.AreEqual(0, _battle.GetDiceOnUseBonus(wearer, null), "Consumed by the first die used");
    }

    [Test]
    public void TwinStarStacksOnSameTargetAndResets()
    {
        var wearer = Unit(false); var a = Unit(true); var b = Unit(true);
        Equip(wearer, "TwinStarSword");
        var level = new SkillLevelData { targetType = TargetType.SingleEnemy, effects = new List<EffectEntry> { new EffectEntry { type = EffectType.Damage } } };
        float Bonus(BattleCharacter target, SkillLevelData l = null) => (float)typeof(BattleManager).GetMethod("TwinStarBonus", Private).Invoke(_battle, new object[] { wearer, l ?? level, target });
        Assert.AreEqual(0f, Bonus(a));
        Assert.AreEqual(0.1f, Bonus(a), 1e-4f);
        Assert.AreEqual(0.2f, Bonus(a), 1e-4f);
        Assert.AreEqual(0f, Bonus(b), "Different primary target resets");
        var all = new SkillLevelData { targetType = TargetType.AllEnemy, effects = level.effects };
        Assert.AreEqual(0f, Bonus(b, all), "All-target attack resets");
        Assert.AreEqual(0f, Bonus(b));
        for (int i = 0; i < 7; i++) Bonus(b);
        Assert.AreEqual(0.5f, Bonus(b), 1e-4f, "Max +50%");
    }

    [Test]
    public void ThornSelfDamageDoesNotTriggerLinkedEffects()
    {
        Relic(RelicEffectType.SealOfVengeance, 1f);
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var wearer = Unit(false, 200f); var enemy = Unit(true, 1000f);
        Equip(wearer, "BloodDagger", "AegisArmor");
        float before = wearer.status.currentHp;
        var skill = Attack(wearer);
        Dice(new[] { 3 });
        Fire(wearer, skill, enemy, Pool.Dice[0].Id);
        Assert.Less(wearer.status.currentHp, before, "6% physical self damage");
        Assert.IsFalse(wearer.magicShellUsed, "Self damage doesn't use 마법 보호장");
        Assert.IsNull(wearer.status.GetStatus(EffectType.Strength), "Nor 복수의 인장");
    }

    [Test]
    public void ElementalStaffAmplifiesSkillDotsWithFloor()
    {
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        var wearer = Unit(false); var enemy = Unit(true);
        Equip(wearer, "ElementalStaff");
        var skill = new SkillInfo { skillName = "출혈", levels = new[] { new SkillLevelData { targetType = TargetType.SingleEnemy,
            effects = new List<EffectEntry> { new EffectEntry { type = EffectType.Bleed, value = 3f, duration = 3 } } } } };
        wearer.status.dynamicActiveSkill.Add(skill);
        Dice(new[] { 2 });
        Fire(wearer, skill, enemy, Pool.Dice[0].Id);
        Assert.AreEqual(4f, enemy.status.GetStatusValue(EffectType.Bleed), "3 × 1.5 = 4.5 → 4");
    }

    [Test]
    public void CatalystMakesADieEverySevenActualUses()
    {
        _train.nexusCar.installedModuleId = NexusModuleDatabase.OriginId;
        _train.optionalCar1 = new TrainCar("전투 강화소", TrainCarType.CombatEnhancement, 0);
        _train.optionalCar1.installedParts.Add("Part_ContinuousCombatCatalyst");
        var actor = Unit(false);
        var pool = Dice(new[] { 1 });
        for (int i = 0; i < 6; i++) Call("CountActualSkillUse", actor);
        Assert.AreEqual(1, pool.Dice.Count);
        Call("CountActualSkillUse", actor);
        Assert.AreEqual(2, pool.Dice.Count, "7th actual use → one D6");
    }
}
