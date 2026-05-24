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
        // useActualResult가 켜져 있으면 직전 실제 결과값 사용, 꺼져 있으면 캐릭터의 현재 최종 공격력 사용
        float baseValue = effect.useActualResult ? _lastCalculatedValue : actor.status.FinalAttack;

        // 보정치 계산
        float calculatedValue = (baseValue * effect.multiplier) + effect.fixedValue;

        switch (effect.type)
        {
            case EffectType.Damage:
                _lastCalculatedValue = target.ReceiveDamage(calculatedValue, actor);
                break;

            case EffectType.Heal:
                _lastCalculatedValue = target.ReceiveHeal(calculatedValue, actor);
                break;
        }
    }
}