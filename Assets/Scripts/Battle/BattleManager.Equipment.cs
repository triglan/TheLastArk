using System.Collections.Generic;
using TheLastArk.Battle;
using TheLastArk.Data;
using TheLastArk.Skills;
using UnityEngine;

/// <summary>Equipment unique effects that hook into the battle flow (each counted once per wearer).</summary>
public partial class BattleManager
{
    private const float GiantCrushPercent = 0.04f;
    private const float WoundTearBleed = 3f;
    private const float ManaTechRate = 0.2f;
    private const float VampiricRate = 0.2f;
    private const float EvilEyeRate = 0.3f;
    private const int ThoughtActivesPerDie = 2;
    private const int ThoughtDicePerTurn = 2;
    private const float ThornSelfDamage = 0.06f;
    private const float ThornPhysicalBonus = 0.3f;
    private const float TwinStarStep = 0.1f;
    private const int TwinStarMaxStacks = 5;
    private const float PureManaRate = 0.25f;
    private const float DragonScaleRate = 0.1f;
    private const int MoonlightBonus = 2;
    private const float MiasmaPoison = 10f;
    private const int MiasmaRounds = 3;
    private const float RobotBuffPercent = 0.3f;
    private const float RobotBleed = 8f;
    private const int CatalystUses = 7;

    private readonly Dictionary<BattleCharacter, int> _thoughtActives = new Dictionary<BattleCharacter, int>();
    private readonly Dictionary<BattleCharacter, int> _thoughtDice = new Dictionary<BattleCharacter, int>();
    private readonly HashSet<BattleCharacter> _windfeatherUsed = new HashSet<BattleCharacter>();
    private readonly Dictionary<BattleCharacter, BattleCharacter> _twinStarTarget = new Dictionary<BattleCharacter, BattleCharacter>();
    private readonly Dictionary<BattleCharacter, int> _twinStarStacks = new Dictionary<BattleCharacter, int>();

    private static bool Wears(BattleCharacter character, string equipmentId) => character?.status != null && character.status.HasEquipment(equipmentId);

    /// <summary>Battle start: 탐욕의 관 gold snapshot, 독기 구름 poison.</summary>
    private void OnBattleStartEquipment()
    {
        int gold = TheLastArk.Managers.ResourceManager.IsInitialized ? TheLastArk.Managers.ResourceManager.Instance.Gold : 0;
        foreach (var ally in playerParty)
        {
            if (ally?.status == null) continue;
            ally.status.greedGoldSnapshot = gold;
            if (!Wears(ally, "MiasmaCloud") || ally.status.currentHp <= 0f) continue;
            ally.status.ApplyStatusEffect(EffectType.Poison, MiasmaPoison, MiasmaRounds);
            foreach (var enemy in enemyParty)
                if (enemy?.status != null && enemy.status.currentHp > 0f) enemy.status.ApplyStatusEffect(EffectType.Poison, MiasmaPoison, MiasmaRounds);
        }
    }

    private void ResetEquipmentTurnState()
    {
        _thoughtActives.Clear();
        _thoughtDice.Clear();
        _windfeatherUsed.Clear();
        foreach (var ally in playerParty)
            if (ally?.status != null) ally.status.turnAttackSpellPercent = 0f;
    }

    /// <summary>Player turn end: 쌍성의 검 stacks reset.</summary>
    private void ResetTwinStar()
    {
        _twinStarTarget.Clear();
        _twinStarStacks.Clear();
    }

    /// <summary>Ally turn start: 마나 파동, 용의 비늘, 달빛, 자율주행 도우미 로봇.</summary>
    private void ApplyEquipmentTurnStart()
    {
        var enemies = enemyParty.FindAll(e => e?.status != null && e.status.currentHp > 0f);
        foreach (var ally in playerParty)
        {
            if (ally?.status == null || ally.status.currentHp <= 0f) continue;
            if (Wears(ally, "PureManaRing"))
                foreach (var enemy in enemies) enemy.TakeMentalDamage(ally.status.FinalMagicResist * PureManaRate, true);
            if (Wears(ally, "DragonScaleArmor"))
                ally.ReceiveHeal(ValueModifier.Floor((ally.status.FinalMaxHp - ally.status.currentHp) * DragonScaleRate), ally);
            if (Wears(ally, "MoonlightNecklace"))
                ally.status.ApplyStatusEffect(EffectType.DiceBoost, MoonlightBonus, 1, 0f, 1, -1, ally, StatusDurationType.Turns);
            if (Wears(ally, "HelperRobot")) RollHelperRobot(ally, enemies);
        }
    }

    /// <summary>30% stun a random enemy · 30% two D6 · 25% wearer ATK/SP +30% this turn · 10% all enemies bleed 8 · 5% 1 physical damage to the wearer.</summary>
    private void RollHelperRobot(BattleCharacter wearer, List<BattleCharacter> enemies)
    {
        float roll = Random.value;
        if (roll < 0.30f)
        {
            if (enemies.Count > 0) enemies[Random.Range(0, enemies.Count)].status.ApplyStatusEffect(EffectType.Stun, 0f, 1);
        }
        else if (roll < 0.60f)
        {
            if (UsesDice) { _dicePool.AddDie(new DieSpec(ActionDicePool.DefaultFaces)); _dicePool.AddDie(new DieSpec(ActionDicePool.DefaultFaces)); }
        }
        else if (roll < 0.85f) wearer.status.turnAttackSpellPercent = RobotBuffPercent;
        else if (roll < 0.95f)
        {
            foreach (var enemy in enemies) enemy.status.ApplyStatusEffect(EffectType.Bleed, RobotBleed, 3);
        }
        else wearer.ReceiveDamage(1f, null, DamageType.Physical, false);
    }

    /// <summary>Extra physical damage % from gear for this fire (피가 흐르는 단검).</summary>
    private static float GearPhysicalBonus(BattleCharacter actor) => Wears(actor, "BloodDagger") ? ThornPhysicalBonus : 0f;

    /// <summary>쌍성의 검: consecutive attack skills on the same primary target +10% each (max +50%).</summary>
    private float TwinStarBonus(BattleCharacter actor, SkillLevelData level, BattleCharacter primary)
    {
        if (!Wears(actor, "TwinStarSword") || !IsAttackLevel(level)) return 0f;
        bool allTargets = level.targetType == TargetType.AllEnemy || level.targetType == TargetType.AllFriendly || primary == null;
        _twinStarTarget.TryGetValue(actor, out BattleCharacter last);
        _twinStarStacks.TryGetValue(actor, out int stacks);
        stacks = !allTargets && last == primary ? Mathf.Min(TwinStarMaxStacks, stacks + 1) : 0;
        _twinStarTarget[actor] = allTargets ? null : primary;
        _twinStarStacks[actor] = stacks;
        return stacks * TwinStarStep;
    }

    /// <summary>Counts an actual skill use: 연속 전투 촉진기, 사고 가속.</summary>
    private void CountActualSkillUse(BattleCharacter actor)
    {
        _skillsUsedThisBattle++;
        if (TheLastArk.Managers.TrainManager.IsInitialized
            && TheLastArk.Managers.TrainManager.Instance.HasPartEffectInAnyCar(TrainPartEffectType.ContinuousCombatCatalyst)
            && _skillsUsedThisBattle % CatalystUses == 0)
        {
            if (UsesDice) _dicePool.AddDie(new DieSpec(ActionDicePool.DefaultFaces));
            else
            {
                currentAP = Mathf.Min(MaxAP, currentAP + 2);
                UpdateAPUI();
            }
        }

        if (UsesDice && Wears(actor, "GrimoireOfTime"))
        {
            _thoughtActives.TryGetValue(actor, out int actives);
            _thoughtActives[actor] = ++actives;
            _thoughtDice.TryGetValue(actor, out int made);
            if (actives % ThoughtActivesPerDie == 0 && made < ThoughtDicePerTurn)
            {
                _thoughtDice[actor] = made + 1;
                _dicePool.AddDie(new DieSpec(ActionDicePool.DefaultFaces));
            }
        }
    }

    /// <summary>After an actual fire, with the skill's per-target HP damage: gear effects that belong to the same skill.</summary>
    private void ResolveEquipmentAfterSkill(BattleCharacter actor, SkillLevelData level, SkillCast cast)
    {
        if (cast == null || actor?.status == null) return;
        bool attack = IsAttackLevel(level);
        if (attack)
        {
            foreach (var pair in cast.PhysicalHealthDamage)
            {
                var target = pair.Key;
                if (target?.status == null || pair.Value <= 0f) continue;
                if (Wears(actor, "ColossalSword") && target.status.currentHp > 0f)
                {
                    float extra = Mathf.Min(target.status.FinalMaxHp * GiantCrushPercent, actor.status.FinalAttack);
                    cast.HealthDamageDealt += target.ReceiveDamage(extra, actor, DamageType.Physical, false);
                }
                if (Wears(actor, "CheongwoonBlade") && target.status.currentHp > 0f)
                    target.status.ApplyStatusEffect(EffectType.Bleed, WoundTearBleed, 3, 0f, 0, -1, actor);
                if (Wears(actor, "EvilEyeNecklace") && target.status.currentHp > 0f)
                    target.TakeMentalDamage(pair.Value * EvilEyeRate, true);
            }
            if (Wears(actor, "ManaTechRing"))
            {
                float magic = 0f;
                foreach (var pair in cast.MagicHealthDamage) magic += pair.Value;
                if (magic > 0f) actor.ReceiveMentalHeal(ValueModifier.Floor(magic * ManaTechRate));
            }
            if (Wears(actor, "VampiricRing"))
            {
                float physical = 0f;
                foreach (var pair in cast.PhysicalHealthDamage) physical += pair.Value;
                if (physical > 0f) actor.ReceiveHeal(ValueModifier.Floor(physical * VampiricRate), actor);
            }
            if (UsesDice && cast.HadCrit && Wears(actor, "WindfeatherTunic") && _windfeatherUsed.Add(actor))
                _dicePool.AddDie(new DieSpec(ActionDicePool.DefaultFaces));
        }

        // 가시공: self damage after the active's effect (does not trigger linked gear or relics).
        if (Wears(actor, "BloodDagger") && actor.status.currentHp > 0f)
            actor.ReceiveDamage(ValueModifier.Floor(actor.status.currentHp * ThornSelfDamage), null, DamageType.Physical, false);
    }
}
