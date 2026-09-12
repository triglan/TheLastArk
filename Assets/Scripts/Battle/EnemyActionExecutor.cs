using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Managers;

public sealed class EnemyActionExecutionContext
{
    public BattleCharacter Actor { get; }
    public EnemyRuntimeState State { get; }
    public List<BattleCharacter> Targets { get; }
    public string PatternName { get; }
    public string CustomVfxName { get; }

    public EnemyActionExecutionContext(BattleCharacter actor, EnemyRuntimeState state, List<BattleCharacter> targets, string patternName, string customVfxName)
    {
        Actor = actor;
        State = state;
        Targets = targets;
        PatternName = patternName;
        CustomVfxName = customVfxName;
    }
}

public static class EnemyActionExecutor
{
    public static void Execute(EnemyPatternAction action, EnemyActionExecutionContext context)
    {
        if (action == null || context?.State == null) return;

        if (action.actionType == EnemyPatternActionType.ModifyRuntimeVariable)
        {
            if (action.setRuntimeValue) context.State.Set(action.runtimeVariableId, action.runtimeValue);
            else context.State.Add(action.runtimeVariableId, action.runtimeValue);
            return;
        }

        if (action.actionType == EnemyPatternActionType.RememberTarget)
        {
            context.State.Remember(context.Targets != null && context.Targets.Count > 0 ? context.Targets[0] : null);
            return;
        }

        if (action.actionType == EnemyPatternActionType.StealGold)
        {
            int taken = ResourceManager.Instance.TakeGoldUpTo(action.runtimeValue);
            context.State.Add(action.runtimeVariableId, taken);
            return;
        }

        if (action.actionType == EnemyPatternActionType.Flee)
        {
            context.State.MarkFled();
            EnemyBattleCharacter enemy = context.Actor != null ? context.Actor.GetComponent<EnemyBattleCharacter>() : null;
            if (enemy != null) enemy.FleeBattle();
            else if (context.Actor?.status != null) context.Actor.status.currentHp = 0f;
            return;
        }

        if (context.Actor == null || context.Targets == null || context.Targets.Count == 0 || action.effect == null) return;
        EffectEngine.ProcessSkill(context.Actor, context.Targets, new SkillLevelData
        {
            overrideCost = -1,
            targetType = action.targetType,
            customVfxName = context.CustomVfxName,
            effects = new List<EffectEntry> { BuildEffect(action, context.State) }
        }, context.PatternName);
    }

    private static EffectEntry BuildEffect(EnemyPatternAction action, EnemyRuntimeState state)
    {
        EffectEntry source = action.effect;
        int runtimeValue = state.Get(action.valueVariableId);
        bool usesStatusValue = source.type == EffectType.Bleed || source.type == EffectType.Poison || source.type == EffectType.Burn;
        return new EffectEntry
        {
            type = source.type,
            damageType = source.damageType,
            multiplier = source.multiplier,
            fixedValue = source.fixedValue + runtimeValue,
            useActualResult = source.useActualResult,
            value = usesStatusValue ? source.value + runtimeValue : source.value,
            secondaryValue = source.secondaryValue,
            hitCount = Mathf.Max(1, source.hitCount + state.Get(action.hitCountVariableId)),
            duration = source.duration,
            charges = source.charges,
            durationType = source.durationType,
            skillSlot = source.skillSlot,
            customVfxName = source.customVfxName,
            statusIcon = source.statusIcon
        };
    }
}
