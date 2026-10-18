using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterStatus : ISerializationCallbackReceiver
{
    public const int EquipmentSlotCount = 2;

    public CharacterData origin; // 원본 캐릭터 데이터입니다.

    // 전투 중 계속 바뀌는 현재 수치입니다.
    public float currentHp;
    public float currentMental;
    [System.NonSerialized] public TheLastArk.Battle.PanicState panic = new TheLastArk.Battle.PanicState();
    public float bonusAttack; // 버프로 추가된 공격력입니다.

    // 아군 강화 단계입니다. 적은 0으로 고정합니다.
    public int charLevel = 0;

    // 아군 4개 스킬 중 영입/마을에서 선택된 2개 스킬의 인덱스
    public List<int> selectedActiveSkillIndices = new List<int>();

    // 리더 지정 시 고정 추가 선택되는 3번째 스킬의 인덱스
    public int leaderExtraSkillIndex = -1;

    public int EnsureLeaderExtraSkill()
    {
        if (origin == null || origin.activeSkills == null || origin.activeSkills.Length == 0) return -1;

        if (leaderExtraSkillIndex < 0 || leaderExtraSkillIndex >= origin.activeSkills.Length)
        {
            List<int> unchosen = new List<int>();
            for (int i = 0; i < origin.activeSkills.Length; i++)
            {
                if (!selectedActiveSkillIndices.Contains(i))
                {
                    unchosen.Add(i);
                }
            }

            if (unchosen.Count > 0)
            {
                leaderExtraSkillIndex = unchosen[UnityEngine.Random.Range(0, unchosen.Count)];
            }
            else
            {
                leaderExtraSkillIndex = 0;
            }
        }

        return leaderExtraSkillIndex;
    }

    // 전투 시작 시 드래프트된 아군 액티브 스킬 목록입니다.
    public List<SkillInfo> dynamicActiveSkill = new List<SkillInfo>();
    [System.NonSerialized] public SkillInfo lastUsedSkill;

    // 현재 걸려 있는 상태이상 목록입니다.
    public List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();

    // 캐릭터당 최대 2개 장비 장착 슬롯
    public TheLastArk.Data.EquipmentData[] equippedItems = new TheLastArk.Data.EquipmentData[EquipmentSlotCount];

    public float EquipmentBonusAttack => GetEquipmentBonus(EquipmentStat.Attack);
    public float EquipmentBonusSpellPower => GetEquipmentBonus(EquipmentStat.SpellPower);
    public float EquipmentBonusHp => GetEquipmentBonus(EquipmentStat.Hp);
    public float EquipmentBonusMental => GetEquipmentBonus(EquipmentStat.Mental);
    public float EquipmentBonusArmor => GetEquipmentBonus(EquipmentStat.Armor);
    public float EquipmentBonusMagicResist => GetEquipmentBonus(EquipmentStat.MagicResist);
    public float EquipmentBonusCritRate => GetEquipmentBonus(EquipmentStat.CritRate);

    private float TrainBonusHpMultiplier => (origin != null && !origin.isEnemy && TheLastArk.Managers.TrainManager.IsInitialized)
        ? TheLastArk.Managers.TrainManager.Instance.GetTrainBonusHpMultiplier() : 0f;

    private float TrainBonusMentalMultiplier => (origin != null && !origin.isEnemy && TheLastArk.Managers.TrainManager.IsInitialized)
        ? TheLastArk.Managers.TrainManager.Instance.GetTrainBonusMentalMultiplier() : 0f;

    private float TrainBonusAttackMultiplier => (origin != null && !origin.isEnemy && TheLastArk.Managers.TrainManager.IsInitialized)
        ? TheLastArk.Managers.TrainManager.Instance.GetTrainBonusAttackMultiplier() : 0f;

    private float TrainBonusSpellPowerMultiplier => (origin != null && !origin.isEnemy && TheLastArk.Managers.TrainManager.IsInitialized)
        ? TheLastArk.Managers.TrainManager.Instance.GetTrainBonusSpellPowerMultiplier() : 0f;

    public float CombatEnhancementMultiplier
    {
        get
        {
            if (origin == null || origin.isEnemy || !TheLastArk.Managers.TrainManager.IsInitialized) return 0f;
            var combatCar = TheLastArk.Managers.TrainManager.Instance.GetCarOfType(TheLastArk.Data.TrainCarType.CombatEnhancement);
            if (combatCar == null || combatCar.level <= 0) return 0f;

            float bonus = combatCar.level * 0.05f;

            // [과적합 회로] 체력 50% 미만 시 효과 25% 추가 증가
            if (combatCar.HasPartEffect(TheLastArk.Data.TrainPartEffectType.OverfitCircuit) && FinalMaxHp > 0 && (currentHp / FinalMaxHp) < 0.50f)
            {
                bonus *= 1.25f;
            }

            // [완전한 준비] 체력 100% 시 효과 40% 추가 증가
            if (combatCar.HasPartEffect(TheLastArk.Data.TrainPartEffectType.FullPreparation) && currentHp >= FinalMaxHp)
            {
                bonus *= 1.40f;
            }

            return bonus;
        }
    }

    public float FinalMaxHp => origin != null ? (origin.maxHp * (1 + GetMultiplier() + GetSynergyHpMultiplier() + TrainBonusHpMultiplier)) + GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusMaxHP) + EquipmentBonusHp : EquipmentBonusHp;
    public float FinalMaxMental => origin != null ? (origin.maxMental * (1 + GetMultiplier() + GetSynergyMentalMultiplier() + TrainBonusMentalMultiplier)) + GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusMaxMental) + EquipmentBonusMental : EquipmentBonusMental;
    public float AttackStatusModifier => GetStatusValue(EffectType.Strength) - GetStatusValue(EffectType.Weakness);
    public float SpellPowerStatusModifier => GetStatusValue(EffectType.Amplification) - GetStatusValue(EffectType.Frailty);
    public float ArmorStatusModifier => GetStatusValue(EffectType.Protection) - GetStatusValue(EffectType.Vulnerable);
    public float MagicResistStatusModifier => GetStatusValue(EffectType.MagicGuard) - GetStatusValue(EffectType.Corrosion);

    public float FinalAttack => Mathf.Max(0f, RawAttack);
    public float FinalSpellPower => Mathf.Max(0f, RawSpellPower);
    public float FinalArmor => Mathf.Max(0f, PatternArmorOverride ?? RawArmor);
    public float FinalMagicResist => Mathf.Max(0f, RawMagicResist);

    private float RawAttack => origin != null ? (origin.baseAttack * (1 + GetMultiplier() + GetSynergyAttackMultiplier() + TrainBonusAttackMultiplier + CombatEnhancementMultiplier)) + AttackStatusModifier + bonusAttack + GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusAttack) + EquipmentBonusAttack + MegalithShieldBonusAttack : AttackStatusModifier + bonusAttack + EquipmentBonusAttack;
    private float RawSpellPower => origin != null ? (origin.spellPower * (1 + GetMultiplier() + GetSynergySpellPowerMultiplier() + TrainBonusSpellPowerMultiplier + CombatEnhancementMultiplier)) + SpellPowerStatusModifier + EquipmentBonusSpellPower + MegalithShieldBonusSpellPower : SpellPowerStatusModifier + EquipmentBonusSpellPower;
    // Pattern-driven forms set an exact armor value until their next transition.
    public float? PatternArmorOverride { get; set; }
    private float RawArmor => origin != null ? (origin.armor * (1 + GetMultiplier())) + ArmorStatusModifier + EquipmentBonusArmor : ArmorStatusModifier + EquipmentBonusArmor;
    private float RawMagicResist => origin != null ? (origin.magicResist * (1 + GetMultiplier())) + MagicResistStatusModifier + EquipmentBonusMagicResist : MagicResistStatusModifier + EquipmentBonusMagicResist;
    public float FinalCritRate => Mathf.Min(MaxCritRate, origin != null
        ? origin.critRate + EquipmentBonusCritRate + AllianceCrestCritBonus + (CanUsePlayerSynergy() ? TheLastArk.Character.SynergyEffects.CritBonus(this) : 0f)
        : EquipmentBonusCritRate);
    public const float MaxCritRate = 100f;

    public bool HasSynergy(TheLastArk.Data.SynergyType type)
    {
        if (origin == null) return false;
        if (origin.synergies != null && origin.synergies.Contains(type)) return true;
        if (!string.IsNullOrEmpty(origin.jobName) && origin.jobName.Equals(type.ToString(), System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private float MegalithShieldBonusAttack
    {
        get
        {
            if (origin == null || origin.isEnemy) return 0f;
            if (!HasSynergy(TheLastArk.Data.SynergyType.Guardian)) return 0f;
            if (TheLastArk.Managers.ResourceManager.Instance == null || !TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.MegalithShield)) return 0f;
            
            if (origin.baseAttack >= origin.spellPower)
            {
                return Mathf.Floor(FinalMaxHp * 0.10f);
            }
            return 0f;
        }
    }

    private float MegalithShieldBonusSpellPower
    {
        get
        {
            if (origin == null || origin.isEnemy) return 0f;
            if (!HasSynergy(TheLastArk.Data.SynergyType.Guardian)) return 0f;
            if (TheLastArk.Managers.ResourceManager.Instance == null || !TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.MegalithShield)) return 0f;
            
            if (origin.spellPower > origin.baseAttack)
            {
                return Mathf.Floor(FinalMaxHp * 0.10f);
            }
            return 0f;
        }
    }

    private float AllianceCrestCritBonus
    {
        get
        {
            if (origin == null || origin.isEnemy) return 0f;
            if (TheLastArk.Managers.ResourceManager.Instance == null || !TheLastArk.Managers.ResourceManager.Instance.HasRelicEffect(TheLastArk.Data.RelicEffectType.AllianceCrest)) return 0f;
            
            // 연합의 문장 no longer grants crit (now: sanity at turn start, see BattleManager.Relics).
            return 0f;
        }
    }

    [Tooltip("디버그/데이터용 스킬 강화 단계 강제 (0/1/2, -1 = 카드 수 기준)")]
    public int skillUpgradeOverride = -1;
    [Tooltip("디버그/데이터용 패시브 개화 강제 (1 = 개화, 0 = 미개화, -1 = 카드 수 기준)")]
    public int traitBloomOverride = -1;

    public bool IsTraitUnlocked => charLevel >= 1;
    public bool IsTraitAwakened => traitBloomOverride >= 0 ? traitBloomOverride == 1 : charLevel >= 4;

    /// <summary>The passive's numbers for its trait: levels[1] when bloomed, otherwise levels[0].</summary>
    public EffectEntry GetPassiveEntry(PassiveTrait trait)
    {
        SkillInfo passive = origin != null ? origin.passiveSkill : null;
        if (trait == PassiveTrait.None || passive == null || passive.passiveTrait != trait || passive.levels == null || passive.levels.Length == 0) return null;
        SkillLevelData level = passive.levels[Mathf.Clamp(IsTraitAwakened ? 1 : 0, 0, passive.levels.Length - 1)];
        return level?.effects != null && level.effects.Count > 0 ? level.effects[0] : null;
    }

    public string LevelTitle => charLevel switch
    {
        0 => "잊혀짐",
        1 => "희미함",
        2 => "선명함",
        3 => "깨어남",
        _ => "각성"
    };

    private float GetRelicBonus(TheLastArk.Data.RelicEffectType type)
    {
        if (origin == null || origin.isEnemy || !Application.isPlaying)
            return 0f;

        if (TheLastArk.Managers.ResourceManager.Instance != null)
            return TheLastArk.Managers.ResourceManager.Instance.GetRelicBonus(type);
        return 0f;
    }

    private bool CanUsePlayerSynergy()
    {
        return origin != null && !origin.isEnemy && Application.isPlaying;
    }

    private float GetSynergyHpMultiplier()
    {
        return CanUsePlayerSynergy() ? TheLastArk.Character.SynergyCalculator.GetTotalSynergyHpMultiplier() + TheLastArk.Character.SynergyEffects.HpBonus(this) : 0f;
    }

    private float GetSynergyMentalMultiplier()
    {
        return CanUsePlayerSynergy() ? TheLastArk.Character.SynergyCalculator.GetTotalSynergyMentalMultiplier() + TheLastArk.Character.SynergyEffects.MentalBonus(this) : 0f;
    }

    private float GetSynergyAttackMultiplier()
    {
        return CanUsePlayerSynergy() ? TheLastArk.Character.SynergyCalculator.GetTotalSynergyAttackMultiplier() + TheLastArk.Character.SynergyEffects.AttackBonus(this) : 0f;
    }

    private float GetSynergySpellPowerMultiplier()
    {
        return CanUsePlayerSynergy() ? TheLastArk.Character.SynergyCalculator.GetTotalSynergySpellPowerMultiplier() + TheLastArk.Character.SynergyEffects.SpellPowerBonus(this) : 0f;
    }

    public int SkillLevelIndex => skillUpgradeOverride >= 0 ? Mathf.Clamp(skillUpgradeOverride, 0, 2) : charLevel switch
    {
        0 => 0,
        1 => 0,
        2 => 1,
        3 => 2,
        _ => 2
    };

    public CharacterStatus(CharacterData data)
    {
        EnsureEquipmentSlots();
        origin = data;
        currentHp = data != null ? data.maxHp : 0f;
        currentMental = data != null ? data.maxMental : 0f;
        bonusAttack = 0;

        // 4개 스킬 중 무작위로 2개 스킬 선택 (중복 없음)
        List<int> pool = new List<int> { 0, 1, 2, 3 };
        int idx1 = pool[UnityEngine.Random.Range(0, pool.Count)];
        pool.Remove(idx1);
        int idx2 = pool[UnityEngine.Random.Range(0, pool.Count)];
        selectedActiveSkillIndices = new List<int> { idx1, idx2 };
        
        if (data != null && !data.isEnemy && Application.isPlaying && TheLastArk.Managers.ResourceManager.Instance != null)
        {
            charLevel = TheLastArk.Managers.ResourceManager.Instance.GetCharacterLevelFromCards(
                TheLastArk.Managers.ResourceManager.Instance.GetCardCount(data.DataId)
            );
            if (charLevel < 0) charLevel = 0;
        }
        else
        {
            charLevel = 0;
        }

        dynamicActiveSkill = new List<SkillInfo>();
    }

    public TheLastArk.Data.EquipmentData GetEquippedItem(int slotIndex)
    {
        EnsureEquipmentSlots();
        return IsValidEquipmentSlot(slotIndex) ? equippedItems[slotIndex] : null;
    }

    public bool SetEquippedItem(int slotIndex, TheLastArk.Data.EquipmentData equipment)
    {
        EnsureEquipmentSlots();
        if (!IsValidEquipmentSlot(slotIndex)) return false;

        equippedItems[slotIndex] = equipment;
        return true;
    }

    public CharacterStatus PreviewEquipment(int slotIndex, TheLastArk.Data.EquipmentData equipment)
    {
        EnsureEquipmentSlots();
        var preview = (CharacterStatus)MemberwiseClone();
        preview.equippedItems = (TheLastArk.Data.EquipmentData[])equippedItems.Clone();
        preview.SetEquippedItem(slotIndex, equipment);
        preview.currentHp = Mathf.Min(preview.currentHp, preview.FinalMaxHp);
        preview.currentMental = Mathf.Min(preview.currentMental, preview.FinalMaxMental);
        return preview;
    }

    public void EnsureEquipmentSlots()
    {
        if (equippedItems != null && equippedItems.Length == EquipmentSlotCount) return;

        TheLastArk.Data.EquipmentData[] resized = new TheLastArk.Data.EquipmentData[EquipmentSlotCount];
        if (equippedItems != null)
            System.Array.Copy(equippedItems, resized, System.Math.Min(equippedItems.Length, EquipmentSlotCount));

        equippedItems = resized;
    }

    public void OnBeforeSerialize()
    {
        EnsureEquipmentSlots();
    }

    public void OnAfterDeserialize()
    {
        EnsureEquipmentSlots();
    }

    private bool IsValidEquipmentSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < EquipmentSlotCount;
    }

    private float GetEquipmentBonus(EquipmentStat stat)
    {
        EnsureEquipmentSlots();

        float total = 0f;
        for (int i = 0; i < EquipmentSlotCount; i++)
        {
            TheLastArk.Data.EquipmentData equipment = equippedItems[i];
            if (equipment == null) continue;

            switch (stat)
            {
                case EquipmentStat.Attack: total += equipment.bonusAttack; break;
                case EquipmentStat.SpellPower: total += equipment.bonusSpellPower; break;
                case EquipmentStat.Hp: total += equipment.bonusHp; break;
                case EquipmentStat.Mental: total += equipment.bonusMental; break;
                case EquipmentStat.Armor: total += equipment.bonusArmor; break;
                case EquipmentStat.MagicResist: total += equipment.bonusMagicResist; break;
                case EquipmentStat.CritRate: total += equipment.bonusCritRate; break;
            }
        }

        return total;
    }

    private enum EquipmentStat
    {
        Attack,
        SpellPower,
        Hp,
        Mental,
        Armor,
        MagicResist,
        CritRate
    }

    public float GetMultiplier()
    {
        if (origin == null || origin.isEnemy || origin.levelStatMultipliers == null || charLevel >= origin.levelStatMultipliers.Length)
            return 0f;
        return origin.levelStatMultipliers[charLevel];
    }

    /// <summary>상태이상을 추가합니다. 같은 타입이 이미 있으면 턴 수를 갱신합니다.</summary>
    public void ApplyStatusEffect(EffectType type, float dmgPerTurn, int turns)
    {
        ApplyStatusEffect(type, dmgPerTurn, turns, 0f, 0, -1, null, StatusDurationType.Default);
    }

    public void ApplyStatusEffect(EffectType type, float value, int turns, StatusDurationType durationType)
    {
        ApplyStatusEffect(type, value, turns, 0f, 0, -1, null, durationType);
    }

    public ActiveStatusEffect GetStatus(EffectType type)
    {
        return activeStatusEffects.Find(e => e != null && e.effectType == type && e.IsActive);
    }

    public float GetStatusPercent(EffectType type)
    {
        return GetStatusValue(type) * 0.01f;
    }

    /// <summary>Effective value of all active instances: largest for max-type effects, sum otherwise.</summary>
    public float GetStatusValue(EffectType type)
    {
        bool max = TheLastArk.Battle.StatusRules.IsMaxType(type);
        float total = 0f;
        for (int i = 0; i < activeStatusEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeStatusEffects[i];
            if (effect == null || effect.effectType != type || !effect.IsActive) continue;
            total = max ? Mathf.Max(total, effect.damagePerTurn) : total + effect.damagePerTurn;
        }
        return total;
    }

    /// <summary>Decrements turn-based effects at this side's turn end (round-end DoTs tick elsewhere).</summary>
    public void TickSideTurnEnd()
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect effect = activeStatusEffects[i];
            if (effect == null) { activeStatusEffects.RemoveAt(i); continue; }
            if (ResolveDurationType(effect.effectType, effect.durationType) != StatusDurationType.Turns
                || TheLastArk.Battle.StatusRules.TicksAtRoundEnd(effect.effectType)) continue;
            effect.remainingTurns--;
            if (effect.remainingTurns <= 0) activeStatusEffects.RemoveAt(i);
        }
    }

    /// <summary>One line per effect type: final value, and the instance breakdown for hover.</summary>
    public List<(EffectType type, string summary, string breakdown)> GetStatusSummaries()
    {
        var result = new List<(EffectType, string, string)>();
        var seen = new HashSet<EffectType>();
        foreach (ActiveStatusEffect effect in activeStatusEffects)
        {
            if (effect == null || !effect.IsActive || !seen.Add(effect.effectType)) continue;
            EffectType type = effect.effectType;
            var parts = new List<string>();
            int count = 0;
            foreach (ActiveStatusEffect instance in activeStatusEffects)
            {
                if (instance == null || instance.effectType != type || !instance.IsActive) continue;
                count++;
                parts.Add(instance.damagePerTurn > 0f
                    ? $"{instance.damagePerTurn:0.##} ({instance.GetDurationText()})"
                    : instance.GetDurationText());
            }
            float value = GetStatusValue(type);
            string name = ActiveStatusEffect.GetDisplayName(type);
            string summary = value > 0f ? $"{name} {Mathf.Floor(value + 1e-4f):0}" : $"{name} · {effect.GetDurationText()}";
            string rule = TheLastArk.Battle.StatusRules.IsMaxType(type) ? " (가장 큰 값 적용)" : "";
            string breakdown = count > 1 && value > 0f
                ? $"{summary} = {string.Join(" + ", parts)}{rule}"
                : $"{name} {string.Join(", ", parts)}";
            result.Add((type, summary, breakdown));
        }
        return result;
    }

    public void ConsumeStatusCharge(EffectType type)
    {
        var effect = GetStatus(type);
        if (effect == null) return;
        effect.remainingCharges--;
        if (effect.remainingCharges <= 0) activeStatusEffects.Remove(effect);
    }

    public void ApplyStatusEffect(EffectType type, float value, int turns, float secondaryValue, int charges, int skillSlot, BattleCharacter source)
    {
        ApplyStatusEffect(type, value, turns, secondaryValue, charges, skillSlot, source, StatusDurationType.Default);
    }

    public void ApplyStatusEffect(EffectType type, float value, int turns, float secondaryValue, int charges, int skillSlot, BattleCharacter source, StatusDurationType requestedDuration)
    {
        StatusDurationType duration = ResolveDurationType(type, requestedDuration);
        var stacking = TheLastArk.Battle.StatusRules.Stacking(type);
        // Each application is its own instance, except time-only / count effects (max refresh) and 반사 (ratios summed).
        ActiveStatusEffect existing = stacking == TheLastArk.Battle.StatusStacking.Instance ? null
            : activeStatusEffects.Find(e => e != null && e.effectType == type && e.IsActive
                && (type != EffectType.Blockade || e.skillSlot == skillSlot));
        if (existing == null)
        {
            existing = new ActiveStatusEffect(type, value, turns, duration);
            existing.remainingCharges = charges;
            activeStatusEffects.Add(existing);
        }
        else
        {
            existing.remainingTurns = Mathf.Max(existing.remainingTurns, turns);
            existing.remainingCharges = Mathf.Max(existing.remainingCharges, charges);
            existing.damagePerTurn = stacking == TheLastArk.Battle.StatusStacking.ReflectMerge
                ? existing.damagePerTurn + value
                : Mathf.Max(existing.damagePerTurn, value);
            if (duration == StatusDurationType.Permanent) existing.durationType = duration;
        }

        if (existing.displayIcon == null) existing.displayIcon = IconLibrary.FindIcon(type);
        existing.secondaryValue = secondaryValue;
        existing.skillSlot = skillSlot;
        // A new protector / taunter source replaces the old one.
        existing.source = source;
    }

    public int RemoveEffectsExpiringAtOwnerPhaseStart()
    {
        return activeStatusEffects.RemoveAll(effect => effect != null
            && ResolveDurationType(effect.effectType, effect.durationType) == StatusDurationType.UntilOwnerPhase);
    }

    public static StatusDurationType ResolveDurationType(EffectType type, StatusDurationType requested)
    {
        if (requested != StatusDurationType.Default) return requested;
        if (type == EffectType.Shield) return StatusDurationType.UntilOwnerPhase;
        if (type == EffectType.Taunt || type == EffectType.Counter || type == EffectType.Guard || type == EffectType.Reflect)
            return StatusDurationType.Charges;
        return StatusDurationType.Turns;
    }

    public void RemoveAllStatusEffects()
    {
        activeStatusEffects.Clear();
    }
}

