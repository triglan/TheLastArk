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
        if (!MatchesCondition(action, context.State)) return;

        if (action.actionType == EnemyPatternActionType.ModifyRuntimeVariable)
        {
            int value = action.runtimeValue + context.State.Get(action.runtimeValueVariableId);
            if (action.setRuntimeValue) context.State.Set(action.runtimeVariableId, value);
            else context.State.Add(action.runtimeVariableId, value);
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

        if (action.actionType == EnemyPatternActionType.BreakShield)
        {
            foreach (BattleCharacter target in context.Targets) BreakShield(target, action.effect.fixedValue);
            return;
        }

        if (action.actionType == EnemyPatternActionType.RemoveStatus)
        {
            foreach (BattleCharacter target in context.Targets)
                target?.status?.activeStatusEffects.RemoveAll(effect => effect != null && effect.effectType == action.effect.type);
            return;
        }

        if (action.actionType == EnemyPatternActionType.TriggerStatus)
        {
            foreach (BattleCharacter target in context.Targets) TriggerStatus(target, action.effect.type);
            return;
        }

        if (action.actionType == EnemyPatternActionType.Summon)
        {
            bool summoned = Summon(context.Actor, action);
            if (!string.IsNullOrWhiteSpace(action.runtimeVariableId)) context.State.Set(action.runtimeVariableId, summoned ? 1 : 0);
            return;
        }

        if (action.actionType == EnemyPatternActionType.SacrificeAllies)
        {
            int total = 0;
            foreach (BattleCharacter target in context.Targets)
            {
                if (target == null || target == context.Actor || target.status == null) continue;
                total += Mathf.RoundToInt(target.ReceiveDamage(target.status.currentHp * action.percent * 0.01f,
                    context.Actor, DamageType.True, false));
            }
            context.State.Add(action.runtimeVariableId, total);
            return;
        }

        if (action.actionType == EnemyPatternActionType.BeginDamageCheck)
        {
            context.State.BeginDamageCheck(context.Targets.Count > 0 ? context.Targets[0] : null);
            return;
        }

        if (action.actionType == EnemyPatternActionType.ResolveDamageCheck)
        {
            float damage = context.State.EndDamageCheck();
            if (damage < context.State.Get(action.conditionVariableId))
            {
                foreach (BattleCharacter target in context.Targets)
                    ExecuteEffect(action, context, new List<BattleCharacter> { target });
                if (action.percent > 0f) context.Actor.ReceiveHeal(action.percent, context.Actor);
                if (action.runtimeValue > 0)
                    context.Actor.status.ApplyStatusEffect(EffectType.Strength, action.runtimeValue, 1, StatusDurationType.Permanent);
            }
            return;
        }

        if (context.Actor == null || context.Targets == null || context.Targets.Count == 0 || action.effect == null) return;
        ExecuteEffect(action, context, context.Targets);
    }

    private static void ExecuteEffect(EnemyPatternAction action, EnemyActionExecutionContext context, List<BattleCharacter> targets)
    {
        if (action.enemyNames != null && action.enemyNames.Length > 0)
            targets = targets.FindAll(target => target?.status?.origin != null
                && System.Array.IndexOf(action.enemyNames, target.status.origin.characterName) >= 0);
        if (targets.Count == 0) return;
        EffectEngine.ProcessSkill(context.Actor, targets, new SkillLevelData
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
        if (action.divisor > 1f) runtimeValue = Mathf.CeilToInt(runtimeValue / action.divisor);
        if (action.maxBonus > 0) runtimeValue = Mathf.Min(runtimeValue, action.maxBonus);
        runtimeValue += state.Get(action.additionalValueVariableId);
        bool usesStatusValue = source.type == EffectType.Bleed || source.type == EffectType.Poison || source.type == EffectType.Burn || source.type == EffectType.Counter;
        var result = new EffectEntry
        {
            type = source.type,
            damageType = source.damageType,
            multiplier = source.multiplier,
            fixedValue = source.fixedValue + runtimeValue,
            useActualResult = source.useActualResult,
            value = usesStatusValue ? source.value + runtimeValue : source.value,
            secondaryValue = source.secondaryValue,
            hitCount = Mathf.Max(1, action.useHitCountVariableAsTotal
                ? state.Get(action.hitCountVariableId) : source.hitCount + state.Get(action.hitCountVariableId)),
            duration = source.duration,
            charges = source.charges,
            durationType = source.durationType,
            skillSlot = source.skillSlot,
            customVfxName = source.customVfxName,
            statusIcon = source.statusIcon
        };
        result.charges = Mathf.Max(1, result.charges + state.Get(action.chargesVariableId));
        return result;
    }

    private static bool MatchesCondition(EnemyPatternAction action, EnemyRuntimeState state)
    {
        if (action.condition == EnemyRuntimeComparison.Always) return true;
        int value = state.Get(action.conditionVariableId);
        return action.condition == EnemyRuntimeComparison.Less ? value < action.conditionValue
            : action.condition == EnemyRuntimeComparison.LessOrEqual ? value <= action.conditionValue
            : action.condition == EnemyRuntimeComparison.Equal ? value == action.conditionValue
            : action.condition == EnemyRuntimeComparison.GreaterOrEqual ? value >= action.conditionValue
            : value > action.conditionValue;
    }

    private static void BreakShield(BattleCharacter target, float amount)
    {
        if (target?.status == null || amount <= 0f) return;
        float remaining = amount;
        for (int i = 0; i < target.status.activeStatusEffects.Count && remaining > 0f; i++)
        {
            ActiveStatusEffect shield = target.status.activeStatusEffects[i];
            if (shield == null || shield.effectType != EffectType.Shield || !shield.IsActive) continue;
            float removed = Mathf.Min(shield.damagePerTurn, remaining);
            shield.damagePerTurn -= removed;
            remaining -= removed;
            if (shield.damagePerTurn <= 0f) target.status.activeStatusEffects.RemoveAt(i--);
        }
        target.view?.UpdateVisual(target.status);
    }

    private static void TriggerStatus(BattleCharacter target, EffectType type)
    {
        ActiveStatusEffect status = target?.status?.GetStatus(type);
        if (status == null) return;
        if (type == EffectType.Bleed || type == EffectType.Poison || type == EffectType.Burn)
            target.ReceiveDamage(status.damagePerTurn, null, DamageType.True, false);
    }

    private static bool Summon(BattleCharacter actor, EnemyPatternAction action)
    {
        BattleManager manager = Object.FindObjectOfType<BattleManager>();
        if (manager == null) return false;
        string[] names = action.enemyNames != null && action.enemyNames.Length > 0 ? action.enemyNames : new[] { action.enemyName };
        int livingMatches = manager.enemyParty.FindAll(enemy => enemy != null && enemy.status?.origin != null
            && System.Array.IndexOf(names, enemy.status.origin.characterName) >= 0 && enemy.status.currentHp > 0f).Count;
        if (action.limit > 0 && livingMatches >= action.limit) return false;

        EnemyBattleCharacter[] slots = Object.FindObjectsOfType<EnemyBattleCharacter>(true);
        EnemyBattleCharacter slot = System.Array.Find(slots, enemy => enemy != null && !enemy.gameObject.activeSelf);
        if (slot == null) return false;

        var candidates = new List<CharacterData>();
        foreach (CharacterData data in Resources.LoadAll<CharacterData>("Characters/Enemy"))
            if (data != null && System.Array.IndexOf(names, data.characterName) >= 0) candidates.Add(data);
        if (candidates.Count == 0) return false;

        slot.AssignAndInitialize(candidates[Random.Range(0, candidates.Count)]);
        BattleCharacter summoned = slot.BattleCharacterComponent;
        if (!manager.enemyParty.Contains(summoned)) manager.enemyParty.Add(summoned);
        EnemyAI ai = summoned.GetComponent<EnemyAI>();
        ai.allyParty = manager.enemyParty;
        ai.targetParty = manager.playerParty;
        return true;
    }
}
