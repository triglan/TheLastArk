using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterStatus
{
    public CharacterData origin; // 원본 캐릭터 데이터입니다.

    // 전투 중 계속 바뀌는 현재 수치입니다.
    public float currentHp;
    public float currentMental;
    public float bonusAttack; // 버프로 추가된 공격력입니다.

    // 아군 강화 단계입니다. 적은 0으로 고정합니다.
    public int charLevel = 0;

    // 아군 4개 스킬 중 영입/마을에서 선택된 2개 스킬의 인덱스 (기본: 0, 1)
    public List<int> selectedActiveSkillIndices = new List<int>() { 0, 1 };

    // 전투 시작 시 드래프트된 아군 액티브 스킬 목록입니다.
    public List<SkillInfo> dynamicActiveSkill = new List<SkillInfo>();

    // 현재 걸려 있는 상태이상 목록입니다.
    public List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();

    public float FinalMaxHp => origin != null ? (origin.maxHp * (1 + GetMultiplier() + TheLastArk.Character.SynergyCalculator.GetTotalSynergyHpMultiplier())) + GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusMaxHP) : 0f;
    public float FinalMaxMental => origin != null ? origin.maxMental * (1 + GetMultiplier()) + GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusMaxMental) : 0f;
    public float FinalAttack => origin != null ? (origin.baseAttack * (1 + GetMultiplier() + TheLastArk.Character.SynergyCalculator.GetTotalSynergyAttackMultiplier())) + bonusAttack + GetRelicBonus(TheLastArk.Data.RelicEffectType.BonusAttack) : bonusAttack;
    public float FinalSpellPower => origin != null ? origin.spellPower * (1 + GetMultiplier()) : 0f;
    public float FinalArmor => origin != null ? origin.armor * (1 + GetMultiplier()) : 0f;
    public float FinalMagicResist => origin != null ? origin.magicResist * (1 + GetMultiplier()) : 0f;
    public float FinalCritRate => origin != null ? origin.critRate : 0f;

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
        origin = data;
        currentHp = data != null ? data.maxHp : 0f;
        currentMental = data != null ? data.maxMental : 0f;
        bonusAttack = 0;
        
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

    public float GetMultiplier()
    {
        if (origin == null || origin.isEnemy || origin.levelStatMultipliers == null || charLevel >= origin.levelStatMultipliers.Length)
            return 0f;
        return origin.levelStatMultipliers[charLevel];
    }

    /// <summary>상태이상을 추가합니다. 같은 타입이 이미 있으면 턴 수를 갱신합니다.</summary>
    public void ApplyStatusEffect(EffectType type, float dmgPerTurn, int turns)
    {
        var existing = activeStatusEffects.Find(e => e.effectType == type);
        if (existing != null)
            existing.remainingTurns = Mathf.Max(existing.remainingTurns, turns);
        else
            activeStatusEffects.Add(new ActiveStatusEffect(type, dmgPerTurn, turns));
    }
}

