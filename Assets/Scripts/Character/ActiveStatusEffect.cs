using System.Collections.Generic;
using UnityEngine;

// ──────────────────────────────────────────────────────────────────────────────
// CharacterData.cs 에 이미 있는 EffectType에 아래 항목을 추가하세요.
//   public enum EffectType { Damage, Heal, Stun, Bleed, Poison, Strength }
//
// 이 파일은 상태이상 인스턴스를 나타내는 새 클래스입니다.
// CharacterStatus.cs 의 activeStatusEffects 리스트에서 사용합니다.
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 캐릭터에 걸린 상태이상 하나를 나타냅니다.
/// BattleCharacter.ApplyStatusEffect()로 추가하고,
/// StatusEffectPhaseHandler.ProcessAll()에서 매 턴 처리합니다.
/// </summary>
[System.Serializable]
public class ActiveStatusEffect
{
    public EffectType effectType;   // Bleed, Poison, Stun 등
    public float damagePerTurn;     // 매 턴 입힐 피해량 (데미지형만)
    public int remainingTurns;      // 남은 지속 턴 수
    public int remainingCharges;
    public float secondaryValue;
    public int skillSlot = -1;
    public BattleCharacter source;

    public ActiveStatusEffect(EffectType type, float dmg, int turns)
    {
        effectType     = type;
        damagePerTurn  = dmg;
        remainingTurns = turns;
    }
}
