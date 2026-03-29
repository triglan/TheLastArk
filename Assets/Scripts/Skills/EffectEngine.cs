using UnityEngine;
using System.Collections.Generic;

public static class EffectEngine
{
    private static float _lastCalculatedValue = 0f;

    public static void ProcessSkill(BattleCharacter actor, List<BattleCharacter> targets, SkillLevelData data)
    {
        _lastCalculatedValue = 0f;

        // 1. 효과 순차 실행
        foreach (var effect in data.effects)
        {
            foreach (var target in targets)
            {
                ExecuteEffect(effect, actor, target);
            }
        }
    }

    private static void ExecuteEffect(EffectEntry effect, BattleCharacter actor, BattleCharacter target)
    {
        float baseValue = effect.baseOnLastEffect ? _lastCalculatedValue : actor.status.origin.baseAttack;

        switch (effect.type)
        {
            case EffectType.Damage:
                _lastCalculatedValue = (baseValue * effect.multiplier) + effect.fixedValue;
                target.ReceiveDamage(_lastCalculatedValue, actor);
                break;

            case EffectType.Heal:
                _lastCalculatedValue = (baseValue * effect.multiplier) + effect.fixedValue;
                target.ReceiveHeal(_lastCalculatedValue, actor);
                break;
        }
    }
}