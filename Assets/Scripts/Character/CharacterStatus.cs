using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CharacterStatus.cs 변경 사항 요약:
///   1. activeStatusEffects 리스트 추가
///   2. ApplyStatusEffect() 헬퍼 추가
///   (나머지는 기존 코드와 동일)
/// </summary>
[System.Serializable]
public class CharacterStatus
{
    public CharacterData origin;

    // ── 런타임 수치 ──────────────────────────────────────
    public float currentHp;
    public float currentMental;
    public float bonusAttack;

    // ── 레벨 ─────────────────────────────────────────────
    public int charLevel = 0;

    // ── 스킬 ─────────────────────────────────────────────
    public List<SkillInfo> dynamicActiveSkill = new List<SkillInfo>();

    // ── 상태이상 (신규) ───────────────────────────────────
    public List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();

    // ── 파이널 스탯 ───────────────────────────────────────
    public float FinalMaxHp     => origin.maxHp     * (1 + GetMultiplier());
    public float FinalMaxMental => origin.maxMental * (1 + GetMultiplier());
    public float FinalAttack    => (origin.baseAttack * (1 + GetMultiplier())) + bonusAttack;

    public int SkillLevelIndex => charLevel switch
    {
        <= 1 => 0,
        2    => 1,
        _    => 2
    };

    // ── 생성자 ────────────────────────────────────────────
    public CharacterStatus(CharacterData data)
    {
        origin              = data;
        currentHp           = data.maxHp;
        currentMental       = data.maxMental;
        bonusAttack         = 0;
        charLevel           = 0;
        dynamicActiveSkill  = new List<SkillInfo>();
        activeStatusEffects = new List<ActiveStatusEffect>();
    }

    public float GetMultiplier()
    {
        if (origin == null
            || origin.levelStatMultipliers == null
            || charLevel >= origin.levelStatMultipliers.Length)
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
