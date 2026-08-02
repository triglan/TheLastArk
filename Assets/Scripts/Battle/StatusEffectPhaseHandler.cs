using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상태이상 페이즈에서 호출됩니다.
/// 모든 캐릭터의 activeStatusEffects를 순회해 발동하고 카운트다운합니다.
/// </summary>
public static class StatusEffectPhaseHandler
{
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
                case EffectType.Stun:   // Stun: 데미지 없이 행동 불능만 (향후 확장)
                    if (effect.damagePerTurn > 0)
                    {
                        character.ReceiveDamage(effect.damagePerTurn, null, DamageType.True);
                        Debug.Log($"[StatusEffect] {character.characterName} {effect.effectType} {effect.damagePerTurn} 피해 (남은 턴: {effect.remainingTurns - 1})");
                    }
                    break;
            }

            effect.remainingTurns--;
            if (effect.remainingTurns <= 0)
            {
                effects.RemoveAt(i);
                Debug.Log($"[StatusEffect] {character.characterName}의 {effect.effectType} 종료");
            }
        }
    }
}
