using System.Collections.Generic;
using TheLastArk.Battle;
using TheLastArk.Character;
using TheLastArk.Data;
using TheLastArk.Skills;
using UnityEngine;

/// <summary>Synergy rules that hook into the battle flow (numbers in <see cref="SynergyBalanceConfig"/>).</summary>
public partial class BattleManager
{
    private readonly List<ActionDie> _carriedDice = new List<ActionDie>();
    private readonly HashSet<int> _carryMarks = new HashSet<int>();
    private BattleCharacter _arcadiaMaxActor;
    private readonly HashSet<SkillInfo> _blueTowerSkills = new HashSet<SkillInfo>();
    private int _blueTowerTriggersThisTurn;
    private bool _cheongwoonUsedThisTurn;
    private readonly Dictionary<BattleCharacter, int> _warriorFires = new Dictionary<BattleCharacter, int>();
    private readonly Dictionary<BattleCharacter, int> _supportMentalGiven = new Dictionary<BattleCharacter, int>();
    private bool _goldDieUsedThisTurn;

    private static SynergyBalanceConfig SynergyConfig => SynergyBalanceConfig.Instance;
    private static int Tier(SynergyType type) => SynergyEffects.Tier(type);

    public BattleCharacter ArcadiaMaxValueActor => _arcadiaMaxActor;

    // ── turn start ─────────────────────────────────────────────────────────

    private void ResetSynergyTurnState()
    {
        _blueTowerTriggersThisTurn = 0;
        _cheongwoonUsedThisTurn = false;
        _warriorFires.Clear();
        _supportMentalGiven.Clear();
        _goldDieUsedThisTurn = false;
        _carryMarks.Clear();
        SynergyEffects.Invalidate();
    }

    /// <summary>Normal D6 added by synergies at turn start (아르키움 유니온).</summary>
    private int SynergyExtraNormalDice()
    {
        if (SynergyConfig == null) return 0;
        return SynergyBalanceConfig.At(SynergyConfig.archiumExtraDice, Tier(SynergyType.ArchiumUnion));
    }

    /// <summary>Special dice placed before normal ones: 북방 (라이언하트), 엘븐우드 large dice, 청운 구름.</summary>
    private List<DieSpec> SynergySpecialDice()
    {
        var specials = new List<DieSpec>();
        var config = SynergyConfig;
        if (config == null) return specials;

        int north = SynergyBalanceConfig.At(config.lionheartNorthDice, Tier(SynergyType.Lionheart));
        bool beastSlayer = TheLastArk.Managers.ResourceManager.Instance != null
            && TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(RelicEffectType.BeastSlayer);
        for (int i = 0; i < north; i++) specials.Add(new DieSpec(beastSlayer ? config.beastSlayerNorthFaces : config.northDieFaces, DieKind.North));

        int elven = Tier(SynergyType.Elvenwood);
        for (int i = 0; i < elven && i < config.elvenwoodFaces.Length; i++) specials.Add(new DieSpec(config.elvenwoodFaces[i]));

        int cloudTier = Tier(SynergyType.Cheongwoon);
        int clouds = SynergyBalanceConfig.At(config.cheongwoonCloudCount, cloudTier);
        int cloudValue = SynergyBalanceConfig.At(config.cheongwoonCloudValue, cloudTier);
        for (int i = 0; i < clouds; i++) specials.Add(new DieSpec(ActionDicePool.DefaultFaces, DieKind.Cloud, cloudValue));
        return specials;
    }

    /// <summary>아르키움 유니온: +bonus on the newly rolled die with the highest original roll (earliest on ties).</summary>
    private void ApplyArchiumTopBonus()
    {
        if (SynergyConfig == null) return;
        int bonus = SynergyBalanceConfig.At(SynergyConfig.archiumTopBonus, Tier(SynergyType.ArchiumUnion));
        if (bonus <= 0) return;
        ActionDie best = null;
        foreach (ActionDie die in _dicePool.Dice)
        {
            if (die.IsCarried || die.HasFixedRoll) continue;
            if (best == null || die.OriginalRoll > best.OriginalRoll) best = die;
        }
        if (best != null) _dicePool.AddModifier(best.Id, bonus);
    }

    private List<ActionDie> TakeCarriedDice()
    {
        var carried = new List<ActionDie>(_carriedDice);
        _carriedDice.Clear();
        return carried;
    }

    /// <summary>Immediate dice and support effects at ally turn start (both AP and dice modes).</summary>
    private void ApplySynergyTurnStart()
    {
        var config = SynergyConfig;
        if (config == null) return;
        var allies = playerParty.FindAll(a => a?.status != null && a.status.currentHp > 0f);
        var enemies = enemyParty.FindAll(e => e?.status != null && e.status.currentHp > 0f);

        // 성도 엘리시움: celestial D6 heal everyone, overheal into shields at high tiers.
        int elysium = Tier(SynergyType.Elysium);
        int celestial = SynergyBalanceConfig.At(config.elysiumCelestialDice, elysium);
        if (celestial > 0)
        {
            int sum = 0;
            for (int i = 0; i < celestial; i++) sum += _dicePool.RollImmediate();
            float heal = Mathf.Max(1f, Mathf.Floor(sum / 2f));
            float overhealRate = SynergyBalanceConfig.At(config.elysiumOverhealShieldPercent, elysium) * 0.01f;
            foreach (var ally in allies)
            {
                float healed = ally.ReceiveHeal(heal, ally);
                float overheal = Mathf.Max(0f, heal - healed);
                if (overhealRate > 0f && overheal > 0f) ally.GainShield(overheal * overhealRate, ally);
            }
        }

        // 속삭이는 교단: wriggling D6 resolved at once against enemies.
        int whisper = Tier(SynergyType.WhisperCult);
        int wriggling = SynergyBalanceConfig.At(config.whisperDice, whisper);
        if (wriggling > 0 && enemies.Count > 0)
        {
            int fear = SynergyBalanceConfig.At(config.whisperFear, whisper);
            if (RelicRules.Has(RelicEffectType.WhisperCultRelic))
            {
                ResolvePolyhedron(wriggling, enemies, fear, config.whisperFearRounds);
                wriggling = 0;
            }
            var rolls = new List<int>();
            for (int i = 0; i < wriggling; i++) rolls.Add(_dicePool.RollImmediate());
            if (whisper >= 3)
            {
                int total = 0;
                foreach (int roll in rolls) total += roll;
                foreach (var enemy in enemies) WhisperHit(enemy, total, fear, config.whisperFearRounds);
            }
            else
            {
                foreach (int roll in rolls)
                {
                    var enemy = enemies[Random.Range(0, enemies.Count)];
                    WhisperHit(enemy, whisper == 1 ? Mathf.Max(1, roll - 1) : roll, fear, config.whisperFearRounds);
                }
            }
        }

        // 미라주: support from the real party's jobs.
        var jobs = SynergyEffects.PartyJobs();
        int mirage = Tier(SynergyType.Mirage);
        if (mirage > 0)
        {
            if (jobs.Contains(SynergyType.Guardian))
            {
                float percent = SynergyBalanceConfig.At(config.mirageGuardianShieldPercent, mirage) * 0.01f;
                foreach (var ally in allies) ally.GainShield(ally.status.FinalMaxHp * percent, ally);
            }
            if (jobs.Contains(SynergyType.Support))
            {
                int mental = SynergyBalanceConfig.At(config.mirageSupportMental, mirage);
                foreach (var ally in allies) ally.ReceiveMentalHeal(mental);
            }
        }

        // 침묵의 아르카디아 3: from turn N, one random member uses max-value effects this turn.
        _arcadiaMaxActor = null;
        if (Tier(SynergyType.SilentArcadia) >= 3 && _turnCount >= config.arcadiaMaxValueFromTurn)
        {
            var members = allies.FindAll(a => SynergyEffects.IsMember(a.status, SynergyType.SilentArcadia));
            if (members.Count > 0) _arcadiaMaxActor = members[Random.Range(0, members.Count)];
        }
    }

    /// <summary>
    /// 빛나는 부등변다면체: each die hits every enemy with the shared value as sanity damage, then the value rises by 1.
    /// At 10 it also deals 30 physical and 30 extra sanity damage, then resets to 1.
    /// </summary>
    private void ResolvePolyhedron(int count, List<BattleCharacter> enemies, int fear, int rounds)
    {
        for (int i = 0; i < count; i++)
        {
            int value = _polyhedronValue;
            foreach (var enemy in enemies)
            {
                if (enemy?.status == null || enemy.status.currentHp <= 0f) continue;
                WhisperHit(enemy, value, fear, rounds);
                if (value >= PolyhedronMax)
                {
                    enemy.ReceiveDamage(PolyhedronBurst, null, DamageType.Physical, false);
                    enemy.TakeMentalDamage(PolyhedronBurst, true);
                }
            }
            _polyhedronValue = value >= PolyhedronMax ? 1 : value + 1;
        }
    }

    private static void WhisperHit(BattleCharacter enemy, float amount, int fear, int rounds)
    {
        float bonus = 0f;
        var config = SynergyConfig;
        if (config != null && enemy.status.currentMental <= enemy.status.FinalMaxMental * 0.5f)
            bonus = SynergyBalanceConfig.At(config.whisperLowMentalBonusPercent, Tier(SynergyType.WhisperCult)) * 0.01f;
        enemy.TakeMentalDamage(new ValueModifier().AddIncrease(bonus).Raw(amount), true);
        if (fear > 0) enemy.status.ApplyStatusEffect(EffectType.Fear, fear, rounds);
        enemy.view?.UpdateVisual(enemy.status);
    }

    // ── skill fire hooks ───────────────────────────────────────────────────

    /// <summary>전사: +% to this fire's physical damage based on how many attack skills the warrior already fired this turn.</summary>
    private float WarriorBonusFor(BattleCharacter actor, SkillLevelData level)
    {
        var config = SynergyConfig;
        if (config == null || !IsAttackLevel(level) || !SynergyEffects.IsMember(actor.status, SynergyType.Warrior)) return 0f;
        int tier = Tier(SynergyType.Warrior);
        _warriorFires.TryGetValue(actor, out int fired);
        float percent = fired == 0 ? SynergyBalanceConfig.At(config.warriorFirstSkillPercent, tier)
            : fired == 1 ? SynergyBalanceConfig.At(config.warriorSecondSkillPercent, tier) : 0f;
        return percent * 0.01f;
    }

    private static bool IsAttackLevel(SkillLevelData level)
        => level?.effects != null && level.special != SkillSpecial.SplitDie && level.effects.Exists(e => e != null && e.type == EffectType.Damage);

    /// <summary>After an actual fire (effects done): 라이언하트 shields, 전사 count, 푸른 마탑, 청운.</summary>
    private void AfterSynergySkillFire(BattleCharacter actor, SkillInfo skill, SkillLevelData level, SkillCast cast)
    {
        var config = SynergyConfig;
        if (config == null || actor?.status == null) return;
        GrantNorthShields(actor, cast?.NorthValues);
        bool attack = IsAttackLevel(level);
        if (attack)
        {
            _warriorFires.TryGetValue(actor, out int fired);
            _warriorFires[actor] = fired + 1;
        }

        // 푸른 마탑: distinct skills fired by members; carries across turns, resets on trigger.
        int tower = Tier(SynergyType.BlueTower);
        if (tower > 0 && skill != null && SynergyEffects.IsMember(actor.status, SynergyType.BlueTower)
            && _blueTowerTriggersThisTurn < SynergyBalanceConfig.At(config.blueTowerPerTurn, tower))
        {
            _blueTowerSkills.Add(skill);
            bool grimoire = TheLastArk.Managers.ResourceManager.Instance != null
                && TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(RelicEffectType.GrimoireOfStars);
            int needed = SynergyBalanceConfig.At(grimoire ? config.grimoireDistinctSkills : config.blueTowerDistinctSkills, tower);
            if (needed > 0 && _blueTowerSkills.Count >= needed)
            {
                _blueTowerSkills.Clear();
                _blueTowerTriggersThisTurn++;
                int mental = SynergyBalanceConfig.At(config.blueTowerMental, tower);
                foreach (var member in playerParty)
                    if (member?.status != null && member.status.currentHp > 0f && SynergyEffects.IsMember(member.status, SynergyType.BlueTower))
                        member.ReceiveMentalHeal(mental);
            }
        }

        // 청운: attack using only 0~1 input dice → 쇠약 on attacked enemies, once per turn for the faction.
        int cloud = Tier(SynergyType.Cheongwoon);
        if (cloud > 0 && attack && !_cheongwoonUsedThisTurn && SynergyEffects.IsMember(actor.status, SynergyType.Cheongwoon)
            && cast != null && cast.InputValues.Count > 0 && cast.InputValues.TrueForAll(v => v <= 1))
        {
            _cheongwoonUsedThisTurn = true;
            int frailty = SynergyBalanceConfig.At(config.cheongwoonFrailty, cloud);
            foreach (var enemy in cast.HitTargets)
                if (enemy?.status != null && enemy.status.currentHp > 0f) enemy.status.ApplyStatusEffect(EffectType.Frailty, frailty, 1);
        }
    }

    /// <summary>라이언하트: shield = effect value per consumed 북방 die (×2 at the top tier).</summary>
    private void GrantNorthShields(BattleCharacter actor, List<int> northValues)
    {
        var config = SynergyConfig;
        if (config == null || northValues == null || northValues.Count == 0) return;
        int tier = Tier(SynergyType.Lionheart);
        int multiplier = SynergyBalanceConfig.At(config.lionheartShieldMultiplier, tier);
        if (multiplier <= 0) multiplier = 1;
        foreach (int value in northValues)
            if (value > 0) actor.GainShield(value * multiplier, actor);
    }

    /// <summary>서포터: an ally helped by a 서포터's active gains sanity (per target, per player turn; 힐링 말랑이 2×).</summary>
    public void GrantSupportMental(BattleCharacter target)
    {
        var config = SynergyConfig;
        int tier = Tier(SynergyType.Support);
        if (config == null || tier <= 0 || target?.status == null || target.status.currentHp <= 0f) return;
        bool mallangi = TheLastArk.Managers.ResourceManager.Instance != null
            && TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(RelicEffectType.HealingMallangi);
        _supportMentalGiven.TryGetValue(target, out int given);
        if (given >= (mallangi ? 2 : 1)) return;
        _supportMentalGiven[target] = given + 1;
        target.ReceiveMentalHeal(SynergyBalanceConfig.At(config.supportMental, tier));
    }

    // ── 침묵의 아르카디아 carry-over ──────────────────────────────────────

    public int CarryCapacity => SynergyConfig == null ? 0 : SynergyBalanceConfig.At(SynergyConfig.arcadiaCarry, Tier(SynergyType.SilentArcadia));
    public bool IsMarkedForCarry(int dieId) => _carryMarks.Contains(dieId);

    /// <summary>Marks / unmarks a waiting die to carry into the next turn (total carried dice ≤ capacity).</summary>
    public bool ToggleCarryMark(int dieId)
    {
        if (!UsesDice || !IsPlayerTurn || _dicePool.Find(dieId) == null) return false;
        if (_carryMarks.Remove(dieId)) { RefreshDiceUI(); return true; }
        if (_carryMarks.Count >= CarryCapacity) return false;
        _carryMarks.Add(dieId);
        RefreshDiceUI();
        return true;
    }

    /// <summary>Turn end: chosen dice (or, if none chosen, the highest ones) up to capacity keep type, roll and modifier.</summary>
    private void PrepareCarryOver()
    {
        _carriedDice.Clear();
        int capacity = CarryCapacity;
        if (capacity <= 0) return;
        var ids = new List<int>();
        foreach (ActionDie die in _dicePool.Dice)
            if (_carryMarks.Contains(die.Id) && ids.Count < capacity) ids.Add(die.Id);
        if (ids.Count == 0)
        {
            var waiting = new List<ActionDie>(_dicePool.Dice);
            waiting.Sort((a, b) => b.Value.CompareTo(a.Value));
            foreach (ActionDie die in waiting)
                if (ids.Count < capacity) ids.Add(die.Id);
        }
        _carriedDice.AddRange(_dicePool.TakeForCarry(ids));
        _carryMarks.Clear();
    }

    // ── 미라주 gold die ───────────────────────────────────────────────────

    public bool CanBuyGoldDie => UsesDice && IsPlayerTurn && !_goldDieUsedThisTurn && Tier(SynergyType.Mirage) > 0 && SynergyConfig != null;
    public int GoldDieCost => SynergyConfig != null ? SynergyConfig.mirageGoldDiceCost : 0;

    /// <summary>미라주: spend gold for one extra D6 (once per turn, storage cap applies).</summary>
    public bool TryBuyGoldDie()
    {
        if (!CanBuyGoldDie) return false;
        var resources = TheLastArk.Managers.ResourceManager.Instance;
        if (resources == null || !resources.TrySpendGold(GoldDieCost)) return false;
        _goldDieUsedThisTurn = true;
        _dicePool.AddDie(new DieSpec(ActionDicePool.DefaultFaces));
        RefreshDiceUI();
        RefreshSkillSlots();
        return true;
    }
}
