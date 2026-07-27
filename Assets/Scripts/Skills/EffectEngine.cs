using UnityEngine;
using System.Collections.Generic;

public static class EffectEngine
{
    // 직전 효과가 실제로 처리한 값을 저장합니다.
    private static float _lastCalculatedValue = 0f;

    public static void ProcessSkill(BattleCharacter actor, List<BattleCharacter> targets, SkillLevelData data)
    {
        // 스킬 또는 패턴의 효과를 대상들에게 순서대로 적용합니다.
        if (actor == null || actor.status == null || targets == null || data?.effects == null) return;

        _lastCalculatedValue = 0f;
        foreach (EffectEntry effect in data.effects)
        {
            foreach (BattleCharacter target in targets)
            {
                if (target == null || target.status == null) continue;
                ExecuteEffect(effect, actor, target);
            }
        }
    }

    private static void ExecuteEffect(EffectEntry effect, BattleCharacter actor, BattleCharacter target)
    {
        // 기본값은 시전자의 최종 공격력이고, 옵션에 따라 직전 결과를 다시 씁니다.
        float baseValue = effect.useActualResult ? _lastCalculatedValue : actor.status.FinalAttack;
        float calculatedValue = (baseValue * effect.multiplier) + effect.fixedValue;

        switch (effect.type)
        {
            case EffectType.Damage:
                _lastCalculatedValue = target.ReceiveDamage(calculatedValue, actor);
                break;

            case EffectType.Heal:
                _lastCalculatedValue = target.ReceiveHeal(calculatedValue, actor);
                break;

            case EffectType.Bleed:
                // 출혈은 고정값을 지속 턴 수로 쓰고, 배율 계산값을 턴당 피해로 씁니다.
                int turns = Mathf.Max(1, Mathf.RoundToInt(effect.fixedValue));
                float damagePerTurn = baseValue * effect.multiplier;
                target.status.ApplyStatusEffect(effect.type, damagePerTurn, turns);
                _lastCalculatedValue = damagePerTurn;
                break;

            case EffectType.Stun:
                // 기절은 고정값을 지속 턴 수로 쓰고 피해는 주지 않습니다.
                int stunTurns = Mathf.Max(1, Mathf.RoundToInt(effect.fixedValue));
                target.status.ApplyStatusEffect(effect.type, 0f, stunTurns);
                _lastCalculatedValue = 0f;
                break;

            case EffectType.Buff:
                // 버프는 현재 구조에서 임시 공격력 보너스로 처리합니다.
                target.status.bonusAttack += calculatedValue;
                _lastCalculatedValue = calculatedValue;
                if (target.view != null) target.view.UpdateVisual(target.status);
                break;

            case EffectType.Taunt:
                int tauntTurns = Mathf.Max(1, Mathf.RoundToInt(calculatedValue));
                target.status.ApplyStatusEffect(effect.type, 0f, tauntTurns);
                _lastCalculatedValue = tauntTurns;
                Debug.Log($"🛡️ {target.characterName} 도발 {tauntTurns}회 획득!");
                break;

            case EffectType.Counter:
                int counterTurns = Mathf.Max(1, Mathf.RoundToInt(calculatedValue));
                target.status.ApplyStatusEffect(effect.type, 0f, counterTurns);
                _lastCalculatedValue = counterTurns;
                Debug.Log($"⚔️ {target.characterName} 반격 {counterTurns}회 획득!");
                break;

            case EffectType.Shield:
                int shieldTurns = Mathf.Max(1, Mathf.RoundToInt(effect.fixedValue > 0 ? effect.fixedValue : 1));
                target.status.ApplyStatusEffect(effect.type, calculatedValue, shieldTurns);
                _lastCalculatedValue = calculatedValue;
                Debug.Log($"🛡️ {target.characterName} 보호막 {calculatedValue} 획득!");
                break;

            case EffectType.Resurrection:
                target.TryTriggerResurrectionTrait();
                _lastCalculatedValue = 1f;
                break;
        }
    }
}
