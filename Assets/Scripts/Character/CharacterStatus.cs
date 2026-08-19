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

    public float FinalMaxHp => origin != null ? (origin.maxHp * (1 + GetMultiplier() + TheLastArk.Character.SynergyCalculator.GetTotalSynergyHpMultiplier() + TrainBonusHpMultiplier)) + GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusMaxHP) + EquipmentBonusHp : EquipmentBonusHp;
    public float FinalMaxMental => origin != null ? (origin.maxMental * (1 + GetMultiplier() + TheLastArk.Character.SynergyCalculator.GetTotalSynergyMentalMultiplier() + TrainBonusMentalMultiplier)) + GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusMaxMental) + EquipmentBonusMental : EquipmentBonusMental;
    public float FinalAttack => origin != null ? (origin.baseAttack * (1 + GetMultiplier() + TheLastArk.Character.SynergyCalculator.GetTotalSynergyAttackMultiplier() + TrainBonusAttackMultiplier + CombatEnhancementMultiplier + GetStatusPercent(EffectType.Strength) - GetStatusPercent(EffectType.Weakness))) + bonusAttack + GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusAttack) + EquipmentBonusAttack + MegalithShieldBonusAttack : bonusAttack + EquipmentBonusAttack;
    public float FinalSpellPower => origin != null ? (origin.spellPower * (1 + GetMultiplier() + TheLastArk.Character.SynergyCalculator.GetTotalSynergySpellPowerMultiplier() + TrainBonusSpellPowerMultiplier + CombatEnhancementMultiplier)) + EquipmentBonusSpellPower + MegalithShieldBonusSpellPower : EquipmentBonusSpellPower;
    public float FinalArmor => origin != null ? (origin.armor * (1 + GetMultiplier() + GetStatusPercent(EffectType.Protection) - GetStatusPercent(EffectType.Vulnerable))) + EquipmentBonusArmor : EquipmentBonusArmor;
    public float FinalMagicResist => origin != null ? (origin.magicResist * (1 + GetMultiplier())) + EquipmentBonusMagicResist : EquipmentBonusMagicResist;
    public float FinalCritRate => origin != null ? origin.critRate + EquipmentBonusCritRate + AllianceCrestCritBonus : EquipmentBonusCritRate;

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
                return FinalMaxHp * 0.10f;
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
                return FinalMaxHp * 0.10f;
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
            
            if (HasSynergy(TheLastArk.Data.SynergyType.Assassin) || HasSynergy(TheLastArk.Data.SynergyType.Ranger) || HasSynergy(TheLastArk.Data.SynergyType.Mage))
            {
                return 10f;
            }
            return 0f;
        }
    }

    public bool IsTraitUnlocked => charLevel >= 1;
    public bool IsTraitAwakened => charLevel >= 4;

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
        if (TheLastArk.Managers.ResourceManager.Instance != null)
            return TheLastArk.Managers.ResourceManager.Instance.GetRelicBonus(type);
        return 0f;
    }

    public int SkillLevelIndex => charLevel switch
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
        
        if (data != null && !data.isEnemy && TheLastArk.Managers.ResourceManager.Instance != null)
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
        ApplyStatusEffect(type, dmgPerTurn, turns, 0f, 0, -1, null);
    }

    public ActiveStatusEffect GetStatus(EffectType type)
    {
        return activeStatusEffects.Find(e => e.effectType == type && (e.remainingTurns > 0 || e.remainingCharges > 0));
    }

    public float GetStatusPercent(EffectType type)
    {
        var effect = GetStatus(type);
        return effect != null ? effect.damagePerTurn * 0.01f : 0f;
    }

    public void ApplyStatusEffect(EffectType type, float value, int turns, float secondaryValue, int charges, int skillSlot, BattleCharacter source)
    {
        var existing = activeStatusEffects.Find(e => e.effectType == type && (type != EffectType.Blockade || e.skillSlot == skillSlot));
        if (existing == null)
        {
            existing = new ActiveStatusEffect(type, value, turns);
            activeStatusEffects.Add(existing);
        }
        else
        {
            existing.remainingTurns = Mathf.Max(existing.remainingTurns, turns);
            existing.remainingCharges = Mathf.Max(existing.remainingCharges, charges);
            existing.damagePerTurn = type == EffectType.Poison ? existing.damagePerTurn + value : Mathf.Max(existing.damagePerTurn, value);
        }

        existing.secondaryValue = secondaryValue;
        existing.remainingCharges = Mathf.Max(existing.remainingCharges, charges);
        existing.skillSlot = skillSlot;
        existing.source = source;
    }

    public void RemoveAllStatusEffects()
    {
        activeStatusEffects.Clear();
    }
}

