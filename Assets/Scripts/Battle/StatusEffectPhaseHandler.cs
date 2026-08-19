using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;
using TheLastArk.Managers;

/// <summary>
/// 상태이상 페이즈에서 호출됩니다.
/// 모든 캐릭터의 activeStatusEffects를 순회해 발동하고 카운트다운합니다.
/// </summary>
public static class StatusEffectPhaseHandler
{
    private static float _accumulatedPoisonDamage = 0f;

    /// <summary>
    /// 파티 전체에 상태이상을 적용합니다. BattleManager.OnEnterStatusEffectPhase()에서 호출하세요.
    /// </summary>
    public static void ProcessAll(List<BattleCharacter> allCombatants)
    {
        foreach (var character in allCombatants)
        {
            if (character == null || character.status == null) continue;
            if (character.status.currentHp <= 0) continue;

            ProcessCharacter(character);
        }
    }

    private static void ProcessCharacter(BattleCharacter character)
    {
        var effects = character.status.activeStatusEffects;

        // 역순 순회 — 처리 중 리스트에서 제거해도 안전
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];

            switch (effect.effectType)
            {
                case EffectType.Bleed:
                    if (effect.damagePerTurn > 0)
                    {
                        character.ReceiveDamage(effect.damagePerTurn, null, DamageType.True);
                        Debug.Log($"[StatusEffect] {character.characterName} 출혈 {effect.damagePerTurn} 피해 (남은 턴: {effect.remainingTurns - 1})");
                    }
                    break;

                case EffectType.Poison:
                    if (effect.damagePerTurn > 0)
                    {
                        float dealt = character.ReceiveDamage(effect.damagePerTurn, null, DamageType.True);
                        Debug.Log($"[StatusEffect] {character.characterName} 독 {effect.damagePerTurn} 피해 (남은 턴: {effect.remainingTurns - 1})");

                        // [정신 흡혈 거머리] 독으로 10 피해를 입힐 때마다 무작위 아군 정신력 1 회복
                        if (character.status.origin != null && character.status.origin.isEnemy)
                        {
                            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.MindLeech))
                            {
                                _accumulatedPoisonDamage += dealt;
                                while (_accumulatedPoisonDamage >= 10f)
                                {
                                    _accumulatedPoisonDamage -= 10f;
                                    RestoreRandomAllyMental(1f);
                                }
                            }
                        }
                        effect.damagePerTurn *= 0.9f;
                    }
                    break;

                case EffectType.Burn:
                    if (effect.damagePerTurn > 0)
                    {
                        float burnDmg = character.status.FinalMaxHp * effect.damagePerTurn * 0.01f;

                        // [호롱불] 적 화상 피해 +25%, 아군 화상 피해 -25%
                        if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.Lantern))
                        {
                            float lanternVal = ResourceManager.Instance.GetRelicBonus(RelicEffectType.Lantern);
                            if (character.status.origin != null && character.status.origin.isEnemy)
                            {
                                burnDmg *= (1f + lanternVal);
                            }
                            else
                            {
                                burnDmg *= Mathf.Max(0f, 1f - lanternVal);
                            }
                        }

                        character.ReceiveDamage(burnDmg, null, DamageType.True);
                        Debug.Log($"[StatusEffect] {character.characterName} 화상 {burnDmg} 피해 (남은 턴: {effect.remainingTurns - 1})");
                    }
                    break;

                case EffectType.Fear:
                    float missingMentalRatio = 1f - (character.status.currentMental / Mathf.Max(1f, character.status.FinalMaxMental));
                    character.TakeMentalDamage(effect.damagePerTurn * (1f + missingMentalRatio));
                    break;

                case EffectType.Stun:
                case EffectType.Shield:
                case EffectType.Taunt:
                case EffectType.Counter:
                case EffectType.Guard:
                    // 지속 턴 차감만 수행
                    break;
            }

            if (effect.effectType == EffectType.Taunt || effect.effectType == EffectType.Counter || effect.effectType == EffectType.Guard)
                continue;

            effect.remainingTurns--;
            if (effect.remainingTurns <= 0)
            {
                effects.RemoveAt(i);
                Debug.Log($"[StatusEffect] {character.characterName}의 {effect.effectType} 종료");
            }
        }
    }

    private static void RestoreRandomAllyMental(float amount)
    {
        var bm = Object.FindObjectOfType<BattleManager>();
        if (bm == null || bm.playerParty == null || bm.playerParty.Count == 0) return;

        List<BattleCharacter> aliveAllies = new List<BattleCharacter>();
        foreach (var ally in bm.playerParty)
        {
            if (ally != null && ally.status != null && ally.status.currentHp > 0)
            {
                aliveAllies.Add(ally);
            }
        }

        if (aliveAllies.Count > 0)
        {
            var target = aliveAllies[Random.Range(0, aliveAllies.Count)];
            target.ReceiveMentalHeal(amount);
            Debug.Log($"[정신 흡혈 거머리] {target.characterName} 정신력 {amount} 회복! (현재: {target.status.currentMental})");
        }
    }
}
