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
    public StatusDurationType durationType;
    public float secondaryValue;
    public int skillSlot = -1;
    public BattleCharacter source;

    public bool IsActive
    {
        get
        {
            switch (durationType)
            {
                case StatusDurationType.Permanent:
                case StatusDurationType.UntilOwnerPhase:
                case StatusDurationType.Marker:
                    return true;
                case StatusDurationType.Charges:
                    return remainingCharges > 0;
                default:
                    return remainingTurns > 0;
            }
        }
    }

    public ActiveStatusEffect(EffectType type, float dmg, int turns, StatusDurationType duration = StatusDurationType.Turns)
    {
        effectType     = type;
        damagePerTurn  = dmg;
        remainingTurns = turns;
        durationType   = duration;
    }

    public string GetDisplayText()
    {
        string valueText = GetValueText();
        return $"{GetDisplayName(effectType)}{valueText} · {GetDurationText()}";
    }

    private string GetValueText()
    {
        if (damagePerTurn <= 0f) return "";
        if (effectType == EffectType.Reflect) return $" {damagePerTurn:0.##}%";
        if (effectType == EffectType.Strength || effectType == EffectType.Amplification
            || effectType == EffectType.Protection || effectType == EffectType.MagicGuard)
            return $" +{damagePerTurn:0.##}";
        if (effectType == EffectType.Weakness || effectType == EffectType.Frailty
            || effectType == EffectType.Vulnerable || effectType == EffectType.Corrosion)
            return $" -{damagePerTurn:0.##}";
        return $" {damagePerTurn:0.##}";
    }

    public string GetDurationText()
    {
        switch (durationType)
        {
            case StatusDurationType.Permanent: return "영구";
            case StatusDurationType.Charges: return $"{remainingCharges}회";
            case StatusDurationType.UntilOwnerPhase: return "다음 자기 턴까지";
            case StatusDurationType.Marker: return "마커";
            default: return $"{remainingTurns}턴";
        }
    }

    public static string GetDisplayName(EffectType type)
    {
        switch (type)
        {
            case EffectType.Strength: return "힘";
            case EffectType.Weakness: return "약화";
            case EffectType.Amplification: return "증폭";
            case EffectType.Frailty: return "쇠약";
            case EffectType.Protection: return "견고";
            case EffectType.Vulnerable: return "파쇄";
            case EffectType.MagicGuard: return "항마";
            case EffectType.Corrosion: return "침식";
            case EffectType.Reflect: return "반사";
            case EffectType.Guard: return "보호";
            case EffectType.Shield: return "보호막";
            case EffectType.Counter: return "반격";
            case EffectType.Taunt: return "도발";
            case EffectType.Focus: return "집중";
            default: return type.ToString();
        }
    }
}
