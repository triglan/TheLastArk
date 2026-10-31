using System.Collections.Generic;
using TheLastArk.Battle;
using TheLastArk.Character;
using TheLastArk.Data;
using TheLastArk.Skills;
using UnityEngine;

/// <summary>Battle relic rules (numbers from each relic's RelicData.effectValue where it has one).</summary>
public partial class BattleManager
{
    // Hero relic skill names (알렉스 바스티온).
    public const string CleaveSkillName = "가르기";
    public const string RegroupSkillName = "재정비";
    public const string EndlessBattleSkillName = "끊임없는 전투";

    private const int BloodMistThreshold = 6;
    private const int FlameHammerSkills = 3;
    private const int OneMoreDrinkPerTurn = 2;
    private const int VengeancePerTurn = 3;
    private const int MagitechMaxStacks = 5;
    private const int PolyhedronMax = 10;
    private const float PolyhedronBurst = 30f;
    private const float SharpNailDamage = 1f;
    private const float CheongwoonSealDamage = 3f;

    private readonly HashSet<BattleCharacter> _bloodMistTriggered = new HashSet<BattleCharacter>();
    private readonly HashSet<BattleCharacter> _flameHammerTriggered = new HashSet<BattleCharacter>();
    private readonly Dictionary<BattleCharacter, int> _burnSkillCounts = new Dictionary<BattleCharacter, int>();
    private readonly Dictionary<BattleCharacter, int> _vengeanceCounts = new Dictionary<BattleCharacter, int>();
    private readonly Dictionary<BattleCharacter, int> _endlessCleaveUses = new Dictionary<BattleCharacter, int>();
    private int _oneMoreDrinkCount;
    private bool _oiledGearUsed;
    private int _polyhedronValue = 1;
    private int _magitechStacks;

    // ── battle / turn boundaries ───────────────────────────────────────────

    /// <summary>Battle start: 철벽 성채의 조각 protect, per-battle relic counters.</summary>
    private void OnBattleStartRelics()
    {
        _oiledGearUsed = false;
        _polyhedronValue = 1;
        _magitechStacks = 0;
        StatusEffectPhaseHandler.ResetBattleCounters();

        if (RelicRules.Has(RelicEffectType.IronFortressFragment))
        {
            BattleCharacter fortress = null;
            foreach (var ally in playerParty)
                if (ally?.status != null && ally.status.currentHp > 0f && (fortress == null || ally.status.FinalArmor > fortress.status.FinalArmor))
                    fortress = ally;
            if (fortress != null)
                foreach (var ally in playerParty)
                    if (ally != null && ally != fortress && ally.status != null && ally.status.currentHp > 0f)
                        ally.status.ApplyStatusEffect(EffectType.Guard, 0f, 0, 0f, Mathf.Max(1, Mathf.RoundToInt(RelicRules.Value(RelicEffectType.IronFortressFragment))), -1, fortress, StatusDurationType.Charges);
        }
    }

    private void ResetRelicTurnState()
    {
        _bloodMistTriggered.Clear();
        _flameHammerTriggered.Clear();
        _burnSkillCounts.Clear();
        _vengeanceCounts.Clear();
        _endlessCleaveUses.Clear();
        _oneMoreDrinkCount = 0;
    }

    /// <summary>Ally turn start relics: 연합의 문장 sanity.</summary>
    private void ApplyRelicTurnStart()
    {
        if (RelicRules.Has(RelicEffectType.AllianceCrest) && SynergyEffects.Tier(SynergyType.Mirage) > 0
            && SynergyEffects.PartyJobs().Count >= 4)
            foreach (var ally in playerParty)
                if (ally?.status != null && ally.status.currentHp > 0f) ally.ReceiveMentalHeal(2f);
    }

    /// <summary>마도공학의 심장: when 아르키움 extra dice were actually generated, every ally gains permanent 힘·증폭 1 (max 5).</summary>
    private void ApplyMagitech(int extraGenerated)
    {
        if (extraGenerated <= 0 || !RelicRules.Has(RelicEffectType.HeartOfMagitech) || _magitechStacks >= MagitechMaxStacks) return;
        _magitechStacks++;
        foreach (var ally in playerParty)
        {
            if (ally?.status == null || ally.status.currentHp <= 0f) continue;
            ally.status.ApplyStatusEffect(EffectType.Strength, 1f, 0, StatusDurationType.Permanent);
            ally.status.ApplyStatusEffect(EffectType.Amplification, 1f, 0, StatusDurationType.Permanent);
            ally.view?.UpdateVisual(ally.status);
        }
    }

    // ── per-hit relic checks used by effects ───────────────────────────────

    /// <summary>피안개: once per target per ally turn.</summary>
    public bool TryBloodMist(BattleCharacter target) => target != null && _bloodMistTriggered.Add(target);
    public static int BloodMistMinimum => BloodMistThreshold;

    /// <summary>화염 망치: counts skills that burned this target this turn; true once when the 3rd is reached.</summary>
    public bool RegisterBurnSkill(BattleCharacter target)
    {
        if (target == null) return false;
        _burnSkillCounts.TryGetValue(target, out int count);
        _burnSkillCounts[target] = ++count;
        return count >= FlameHammerSkills && _flameHammerTriggered.Add(target);
    }

    /// <summary>복수의 인장: ally hit by a direct attack gains 힘 1 (1 turn), max 3 per side turn.</summary>
    public void TryVengeance(BattleCharacter ally)
    {
        if (!RelicRules.Has(RelicEffectType.SealOfVengeance) || !RelicRules.IsAlly(ally)) return;
        _vengeanceCounts.TryGetValue(ally, out int count);
        if (count >= VengeancePerTurn) return;
        _vengeanceCounts[ally] = count + 1;
        ally.status.ApplyStatusEffect(EffectType.Strength, 1f, 1);
    }

    // ── skill fire ─────────────────────────────────────────────────────────

    /// <summary>
    /// 배반자 (on 공방일체): right before an eligible attack, consume all own shield; 50% (bloom 75%) of it is added
    /// to the primary target's first hit.
    /// </summary>
    private void ApplyTraitorBeforeAttack(BattleCharacter actor, SkillLevelData level, SkillCast cast)
    {
        if (!RelicRules.Has(RelicEffectType.Traitor) || !IsAttackLevel(level)) return;
        EffectEntry offense = actor.status.GetPassiveEntry(PassiveTrait.OffenseDefense);
        if (offense == null || (offense.charges > 0 && _offenseDefenseUsed.Contains(actor))) return;
        float shield = actor.status.GetStatusValue(EffectType.Shield);
        actor.status.activeStatusEffects.RemoveAll(e => e != null && e.effectType == EffectType.Shield);
        cast.FirstHitBonus = shield * (actor.status.IsTraitAwakened ? 0.75f : 0.5f);
    }

    /// <summary>끊임없는 전투: total 가르기 uses this turn after firing it (0 = normal).</summary>
    private int EndlessCleaveUses(BattleCharacter actor, SkillInfo skill)
        => skill != null && skill.skillName == CleaveSkillName && _endlessCleaveUses.TryGetValue(actor, out int uses) ? uses : 0;

    /// <summary>끊임없는 전투 +1/+2: extra 가르기 uses hit 25% harder.</summary>
    private float EndlessCleaveBonus(BattleCharacter actor, SkillInfo skill)
    {
        if (EndlessCleaveUses(actor, skill) <= 0 || UsesOf(actor, skill) < 1) return 0f;
        SkillInfo endless = actor.status.dynamicActiveSkill.Find(s => s != null && s.skillName == EndlessBattleSkillName);
        SkillLevelData level = endless != null ? GetSkillLevel(endless, actor) : null;
        return level != null && actor.status.SkillLevelIndex >= 1 ? RelicRules.Value(RelicEffectType.EndlessBattle) : 0f;
    }

    /// <summary>After an actual fire, before passives: per-target relic resolution and dice relics.</summary>
    private void ResolveRelicsAfterSkill(BattleCharacter actor, SkillInfo skill, SkillLevelData level, SkillCast cast)
    {
        if (cast == null || actor?.status == null || !RelicRules.IsAlly(actor)) return;
        bool attack = IsAttackLevel(level);
        bool charge = GetDiceRule(skill, actor).IsCharge;

        // 정신 분열의 룬: 10% of the magic HP damage per target, once after all hits.
        if (RelicRules.Has(RelicEffectType.MindFractureRune))
            foreach (var pair in cast.MagicHealthDamage)
                if (pair.Key?.status != null && pair.Value > 0f) pair.Key.TakeMentalDamage(pair.Value * 0.1f, true);

        // 청운의 인장: 0~1-dice attacks by 청운 members deal 3 fixed damage to each attacked enemy.
        if (attack && RelicRules.Has(RelicEffectType.CheongwoonRelic) && SynergyEffects.IsMember(actor.status, SynergyType.Cheongwoon)
            && cast.InputValues.Count > 0 && cast.InputValues.TrueForAll(v => v <= 1))
            foreach (var enemy in cast.HitTargets)
                if (enemy?.status != null && enemy.status.currentHp > 0f)
                    cast.HealthDamageDealt += enemy.ReceiveDamage(CheongwoonSealDamage, null, DamageType.True, false);

        // 광전사의 도끼: 전사 heals 15% of the skill's HP damage.
        if (attack && RelicRules.Has(RelicEffectType.BerserkerAxe) && SynergyEffects.IsMember(actor.status, SynergyType.Warrior) && cast.HealthDamageDealt > 0f)
            actor.ReceiveHeal(ValueModifier.Floor(cast.HealthDamageDealt * RelicRules.Value(RelicEffectType.BerserkerAxe)), actor);

        // 그림자 베일: 암살자 kill with this skill → one normal D6.
        if (attack && RelicRules.Has(RelicEffectType.ShadowVeil) && SynergyEffects.IsMember(actor.status, SynergyType.Assassin) && UsesDice)
            foreach (var enemy in cast.HitTargets)
                if (enemy?.status != null && enemy.status.currentHp <= 0f) { _dicePool.AddDie(new DieSpec(ActionDicePool.DefaultFaces)); break; }

        // 순환의 룬: 마법사 skill that put a status on an enemy → random ally +2 sanity.
        if (cast.AppliedStatusToEnemy && RelicRules.Has(RelicEffectType.RuneOfCycle) && SynergyEffects.IsMember(actor.status, SynergyType.Mage))
            EffectEngine.RestoreRandomAllyMental(RelicRules.Value(RelicEffectType.RuneOfCycle));

        // 한 잔 더!: normal active with every input die at 0 → one D6 (party, 2 per ally turn).
        if (!charge && UsesDice && RelicRules.Has(RelicEffectType.OneMoreDrink) && _oneMoreDrinkCount < OneMoreDrinkPerTurn
            && cast.InputValues.Count > 0 && cast.InputValues.TrueForAll(v => v == 0))
        {
            _oneMoreDrinkCount++;
            _dicePool.AddDie(new DieSpec(ActionDicePool.DefaultFaces));
        }

        // 기름칠된 톱니바퀴: the battle's first normal active returns its first die.
        if (!charge && UsesDice && !_oiledGearUsed && RelicRules.Has(RelicEffectType.OiledGear) && cast.FirstDie != null)
        {
            _oiledGearUsed = true;
            _dicePool.ReturnDie(cast.FirstDie);
        }

        // 끊임없는 전투: firing it grants 가르기 uses for this turn.
        if (skill != null && skill.skillName == EndlessBattleSkillName && level != null)
            _endlessCleaveUses[actor] = Mathf.Max(1, level.specialCount);

        RefreshDiceUI();
    }
}
