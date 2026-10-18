using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.Battle;

/// <summary>
/// 상태이상 페이즈에서 호출됩니다.
/// 모든 캐릭터의 activeStatusEffects를 순회해 발동하고 카운트다운합니다.
/// </summary>
public static class StatusEffectPhaseHandler
{
    private static float _accumulatedPoisonDamage = 0f;

    /// <summary>Battle start: 정신 흡혈 거머리 progress restarts (it carries over within one battle only).</summary>
    public static void ResetBattleCounters() => _accumulatedPoisonDamage = 0f;

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

    /// <summary>
    /// Round end: 출혈 (largest), 독 (sum, then each instance −10%), 화상 (sum % of max HP), 공포 (largest)
    /// take effect once, then those instances decrement. Other turn effects tick at their side's turn end.
    /// </summary>
    private static void ProcessCharacter(BattleCharacter character)
    {
        var status = character.status;

        float bleed = status.GetStatusValue(EffectType.Bleed);
        if (bleed > 0f)
        {
            character.ReceiveDamage(ValueModifier.Floor(bleed), null, DamageType.True, false);
            Debug.Log($"[StatusEffect] {character.characterName} 출혈 {ValueModifier.Floor(bleed)} 피해");
        }

        float poison = status.GetStatusValue(EffectType.Poison);
        if (poison > 0f)
        {
            float dealt = character.ReceiveDamage(ValueModifier.Floor(poison), null, DamageType.True, false);
            Debug.Log($"[StatusEffect] {character.characterName} 독 {ValueModifier.Floor(poison)} 피해");

            // [정신 흡혈 거머리] 독으로 10 피해를 입힐 때마다 무작위 아군 정신력 1 회복
            if (status.origin != null && status.origin.isEnemy && ResourceManager.Instance != null
                && ResourceManager.Instance.HasRelicEffect(RelicEffectType.MindLeech))
            {
                _accumulatedPoisonDamage += dealt;
                while (_accumulatedPoisonDamage >= 10f)
                {
                    _accumulatedPoisonDamage -= 10f;
                    RestoreRandomAllyMental(1f);
                }
            }
            // Decimals are kept internally; only the dealt damage is floored.
            foreach (var effect in status.activeStatusEffects)
                if (effect != null && effect.effectType == EffectType.Poison && effect.IsActive)
                    effect.damagePerTurn *= StatusRules.PoisonDecay;
        }

        float burnPercent = status.GetStatusValue(EffectType.Burn);
        if (burnPercent > 0f)
        {
            var burn = new ValueModifier();
            // [호롱불] 적 화상 피해 +25%, 아군 화상 피해 -25%
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.Lantern))
            {
                float lanternVal = ResourceManager.Instance.GetRelicBonus(RelicEffectType.Lantern);
                if (status.origin != null && status.origin.isEnemy) burn.AddIncrease(lanternVal);
                else burn.AddDecrease(lanternVal);
            }
            float burnDmg = burn.Apply(status.FinalMaxHp * burnPercent * 0.01f);
            character.ReceiveDamage(burnDmg, null, DamageType.True, false);
            Debug.Log($"[StatusEffect] {character.characterName} 화상 {burnDmg} 피해");
        }

        float fear = status.GetStatusValue(EffectType.Fear);
        if (fear > 0f)
        {
            float lostRatio = 1f - (status.currentMental / Mathf.Max(1f, status.FinalMaxMental));
            character.TakeMentalDamage(ValueModifier.Floor(fear * (1f + Mathf.Clamp01(lostRatio))));
        }

        var effects = status.activeStatusEffects;
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            if (effect == null || !StatusRules.TicksAtRoundEnd(effect.effectType)) continue;
            if (CharacterStatus.ResolveDurationType(effect.effectType, effect.durationType) != StatusDurationType.Turns) continue;
            effect.remainingTurns--;
            if (effect.remainingTurns <= 0) effects.RemoveAt(i);
        }

        if (character.view != null)
            character.view.UpdateVisual(character.status);
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
