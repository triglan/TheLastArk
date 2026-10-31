using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("대상 파티")]
    public List<BattleCharacter> targetParty;
    public List<BattleCharacter> allyParty;

    private BattleCharacter _self;
    private int _patternIndex;
    private int _preparedPatternIndex = -1;
    private List<BattleCharacter> _preparedTargets;
    private BattleCharacter _preparedPrimary;
    private EnemyPatternData _preparedPattern;
    private EnemyPatternAction _preparedAction;
    private EnemyIntentData _preparedIntent;
    private EnemyRuntimeState _runtimeState;

    public IReadOnlyList<BattleCharacter> PreparedTargets => _preparedTargets;
    public EnemyIntentData PreparedIntent => _preparedIntent;
    public bool HasPreparedTurn => _preparedPattern != null;
    public EnemyRuntimeState RuntimeState => GetRuntimeState();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public int DebugPatternIndex => _patternIndex;

    // Select the next action without resetting counters, remembered targets or boss state.
    public bool DebugSelectPattern(int index)
    {
        if (_self == null) _self = GetComponent<BattleCharacter>();
        var patterns = _self?.status?.origin?.enemyPatterns;
        if (patterns == null || index < 0 || index >= patterns.Count) return false;
        _patternIndex = index;
        return PrepareTurn();
    }
#endif

    private void Awake()
    {
        _self = GetComponent<BattleCharacter>();
    }

    public void ResetForBattle()
    {
        _runtimeState?.EndDamageCheck();
        _runtimeState = null;
        _patternIndex = 0;
        ClearPreparedTurn();
    }

    private void OnDestroy() => _runtimeState?.EndDamageCheck();

    public bool PrepareTurn()
    {
        if (_self == null) _self = GetComponent<BattleCharacter>();
        _preparedTargets = null;
        _preparedPattern = null;
        _preparedAction = null;
        _preparedIntent = null;

        // 기절 상태면 이번 적 턴의 행동을 건너뜁니다.
        if (_self == null || _self.status?.origin == null) return false;
        if (HasStatus(EffectType.Stun))
        {
            Debug.Log($"[EnemyAI] {_self.characterName} 기절로 행동하지 못함");
            return false;
        }

        // 데이터에 등록된 패턴을 순서대로 실행합니다.
        List<EnemyPatternData> patterns = _self.status.origin.enemyPatterns;
        if (patterns == null || patterns.Count == 0)
            return false;

        int patternIndex = SelectPatternIndex(patterns);
        if (patternIndex < 0) return false;
        EnemyPatternData pattern = patterns[patternIndex];
        if (pattern == null || !HasActions(pattern))
            return false;
        _preparedPatternIndex = patternIndex;

        _preparedAction = FindTargetedAction(pattern);
        _preparedTargets = _preparedAction != null
            ? ResolveTargets(_preparedAction.targetType, _preparedAction.targetSelection)
            : ResolveTargets(pattern.targetType, pattern.targetSelection);

        _preparedPattern = pattern;
        _preparedIntent = BuildIntent(pattern);
        return true;
    }

    public void ExecuteTurn()
    {
        if (_preparedTargets == null && !PrepareTurn()) return;
        var attack = TheLastArk.Battle.AttackContext.BeginPattern(_self);
        try
        {
            ApplyHallucination(attack);
            ExecutePreparedPattern();
        }
        finally { attack.End(); }

        // 강박: the first pattern used after the panic began is repeated.
        var panic = _self.status.panic;
        if (panic != null && panic.Type == TheLastArk.Battle.PanicType.Compulsion && panic.LockedPatternIndex < 0)
            panic.LockedPatternIndex = _preparedPatternIndex;

        // 출혈·압박 trigger once per pattern, after its effect.
        _self.ResolveActionStatusEffects();
        _self.view?.UpdateVisual(_self.status);
        AdvancePattern();
        Debug.Log($"[EnemyAI] {_self.characterName} 패턴 실행: {_preparedPattern.patternName}");
        ClearPreparedTurn();
    }

    private void ExecutePreparedPattern()
    {
        if (_preparedPattern.actions != null && _preparedPattern.actions.Count > 0)
        {
            foreach (EnemyPatternAction action in _preparedPattern.actions)
            {
                if (_self.status.currentHp <= 0f) break;
                if (action == null) continue;
                List<BattleCharacter> targets = NeedsTargets(action.actionType)
                    ? GetExecutionTargets(action)
                    : new List<BattleCharacter>();
                EnemyActionExecutor.Execute(action, new EnemyActionExecutionContext(
                    _self, GetRuntimeState(), targets, _preparedPattern.patternName, _preparedPattern.customVfxName));
            }
        }
        else
        {
            if (!_hallucinating) ApplyConfusion(_preparedPattern.targetType, _preparedPattern.targetSelection);
            _preparedTargets.RemoveAll(target => target == null || target.status == null || target.status.currentHp <= 0f);
            if (_preparedTargets.Count > 0)
            {
                EffectEngine.ProcessSkill(_self, _preparedTargets, new SkillLevelData
                {
                    overrideCost = -1,
                    targetType = _preparedPattern.targetType,
                    customVfxName = _preparedPattern.customVfxName,
                    effects = CloneEffects(_preparedPattern.effects)
                }, _preparedTargets.Contains(_preparedPrimary) ? _preparedPrimary : null, _preparedPattern.patternName);
            }
        }
    }

    private static List<EffectEntry> CloneEffects(List<EffectEntry> sourceEffects)
    {
        var runtimeEffects = new List<EffectEntry>(sourceEffects.Count);

        foreach (EffectEntry source in sourceEffects)
        {
            if (source == null) continue;
            var effect = new EffectEntry
            {
                type = source.type,
                damageType = source.damageType,
                multiplier = source.multiplier,
                fixedValue = source.fixedValue,
                useActualResult = source.useActualResult,
                value = source.value,
                secondaryValue = source.secondaryValue,
                hitCount = source.hitCount,
                duration = source.duration,
                charges = source.charges,
                durationType = source.durationType,
                skillSlot = source.skillSlot,
                customVfxName = source.customVfxName,
                statusIcon = source.statusIcon
            };

            runtimeEffects.Add(effect);
        }

        return runtimeEffects;
    }

    private void AdvancePattern()
    {
        int patternCount = _self.status.origin.enemyPatterns.Count;
        _patternIndex = (_patternIndex + 1) % patternCount;
    }

    private bool HasActions(EnemyPatternData pattern)
    {
        return pattern.actions != null && pattern.actions.Exists(action => action != null)
            || pattern.effects != null && pattern.effects.Exists(effect => effect != null);
    }

    private EnemyRuntimeState GetRuntimeState()
    {
        if (_self == null) _self = GetComponent<BattleCharacter>();
        CharacterData source = _self?.status?.origin;
        if (_runtimeState == null || _runtimeState.Source != source)
            _runtimeState = new EnemyRuntimeState(source);
        return _runtimeState;
    }

    private void ClearPreparedTurn()
    {
        _preparedTargets = null;
        _preparedPattern = null;
        _preparedAction = null;
        _preparedIntent = null;
    }

    private EnemyPatternAction FindTargetedAction(EnemyPatternData pattern)
    {
        if (pattern.actions == null) return null;
        return pattern.actions.Find(action => action != null && NeedsTargets(action.actionType)
            && !IsFriendlyTarget(action.targetType))
            ?? pattern.actions.Find(action => action != null && NeedsTargets(action.actionType));
    }

    private static bool NeedsTargets(EnemyPatternActionType type)
    {
        return type == EnemyPatternActionType.Effect || type == EnemyPatternActionType.RememberTarget
            || type == EnemyPatternActionType.BreakShield || type == EnemyPatternActionType.RemoveStatus
            || type == EnemyPatternActionType.TriggerStatus || type == EnemyPatternActionType.SacrificeAllies
            || type == EnemyPatternActionType.BeginDamageCheck || type == EnemyPatternActionType.ResolveDamageCheck;
    }

    private List<BattleCharacter> GetExecutionTargets(EnemyPatternAction action)
    {
        if (action != _preparedAction && (!action.usePreparedTarget || _preparedAction == null
            || action.targetType != _preparedAction.targetType || action.targetSelection == EnemyTargetSelection.Remembered))
            return ResolveTargets(action.targetType, action.targetSelection);

        // 무작위 대상은 의도에 표시한 대상을 유지하고, 조건 대상은 실행 직전에 다시 고릅니다.
        if ((action == _preparedAction || action.usePreparedTarget) && HasLivingPreparedTargets())
            return new List<BattleCharacter>(_preparedTargets);

        return ResolveTargets(action.targetType, action.targetSelection);
    }

    private bool HasLivingPreparedTargets()
    {
        return _preparedTargets != null && _preparedTargets.Exists(target =>
            target != null && target.status != null && target.status.currentHp > 0f);
    }

    private EnemyIntentData BuildIntent(EnemyPatternData pattern)
    {
        var intent = new EnemyIntentData
        {
            PatternName = string.IsNullOrWhiteSpace(pattern.patternName) ? "행동" : pattern.patternName,
            TargetType = _preparedAction != null ? _preparedAction.targetType : pattern.targetType,
            TargetSelection = _preparedAction != null ? _preparedAction.targetSelection : pattern.targetSelection,
            Targets = _preparedTargets == null ? new List<BattleCharacter>() : new List<BattleCharacter>(_preparedTargets)
        };

        if (pattern.actions != null && pattern.actions.Count > 0)
        {
            var previewValues = new Dictionary<string, int>();
            foreach (EnemyPatternAction action in pattern.actions)
            {
                if (action == null) continue;
                intent.Effects.Add(BuildIntentEffect(action, previewValues));
                if (action.actionType != EnemyPatternActionType.ModifyRuntimeVariable
                    || string.IsNullOrWhiteSpace(action.runtimeVariableId)) continue;
                int current = GetIntentVariable(action.runtimeVariableId, previewValues);
                previewValues[action.runtimeVariableId] = action.setRuntimeValue
                    ? action.runtimeValue : current + action.runtimeValue;
            }
        }
        else if (pattern.effects != null)
        {
            foreach (EffectEntry effect in pattern.effects)
            {
                if (effect == null) continue;
                EnemyIntentEffectData effectIntent = BuildIntentEffect(effect, pattern.targetType, pattern.targetSelection, null, null);
                effectIntent.Icon = effect.statusIcon != null ? effect.statusIcon : IconLibrary.FindIcon(effect.type);
                intent.Effects.Add(effectIntent);
            }
        }

        if (pattern.forecastPatternOffset > 0)
        {
            var values = new Dictionary<string, int>();
            List<EnemyPatternData> patterns = _self.status.origin.enemyPatterns;
            for (int offset = 0; offset < pattern.forecastPatternOffset; offset++)
                foreach (EnemyPatternAction action in patterns[(_patternIndex + offset) % patterns.Count].actions)
                {
                    if (action.actionType != EnemyPatternActionType.ModifyRuntimeVariable) continue;
                    int added = action.runtimeValue + GetIntentVariable(action.runtimeValueVariableId, values);
                    int value = action.setRuntimeValue ? added : GetIntentVariable(action.runtimeVariableId, values) + added;
                    var definition = GetRuntimeState().GetDefinition(action.runtimeVariableId);
                    values[action.runtimeVariableId] = definition != null && definition.hasMaxValue ? Mathf.Min(value, definition.maxValue) : value;
                }
            var future = patterns[(_patternIndex + pattern.forecastPatternOffset) % patterns.Count];
            foreach (var action in future.actions)
            {
                if (action.effect == null || (action.effect.type != EffectType.Damage && action.effect.type != EffectType.MentalDamage)
                    || (action.actionType != EnemyPatternActionType.Effect && action.actionType != EnemyPatternActionType.ResolveDamageCheck)) continue;
                var effect = BuildIntentEffect(action, values);
                string kind = effect.EffectType == EffectType.MentalDamage ? "정신" : "물리";
                intent.Forecast = $"예고: {future.patternName} {kind} {effect.Value:0.#} × {effect.HitCount}";
                break;
            }
        }
        intent.VisualType = ResolveVisualType(intent.Effects);
        return intent;
    }

    private EnemyIntentEffectData BuildIntentEffect(EnemyPatternAction action, Dictionary<string, int> previewValues)
    {
        if (action.actionType == EnemyPatternActionType.SetArmor)
            return new EnemyIntentEffectData { ActionType = action.actionType, RuntimeValue = action.runtimeValue };

        if (action.actionType == EnemyPatternActionType.ModifyRuntimeVariable)
        {
            return new EnemyIntentEffectData
            {
                ActionType = action.actionType,
                Icon = action.intentIcon != null ? action.intentIcon : GetRuntimeVariableIcon(action.runtimeVariableId),
                RuntimeVariableId = action.runtimeVariableId,
                RuntimeDisplayName = GetRuntimeVariableName(action.runtimeVariableId),
                RuntimeValue = action.runtimeValue,
                SetRuntimeValue = action.setRuntimeValue
            };
        }

        if (action.actionType == EnemyPatternActionType.RememberTarget)
        {
            return new EnemyIntentEffectData
            {
                ActionType = action.actionType,
                Icon = action.intentIcon,
                TargetType = action.targetType,
                TargetSelection = action.targetSelection
            };
        }

        if (action.actionType == EnemyPatternActionType.StealGold)
        {
            return new EnemyIntentEffectData
            {
                ActionType = action.actionType,
                Icon = action.intentIcon != null ? action.intentIcon : IconLibrary.FindEnhancementIcon(),
                RuntimeVariableId = action.runtimeVariableId,
                RuntimeDisplayName = GetRuntimeVariableName(action.runtimeVariableId),
                RuntimeValue = action.runtimeValue
            };
        }

        if (action.actionType == EnemyPatternActionType.Flee)
        {
            return new EnemyIntentEffectData
            {
                ActionType = action.actionType,
                Icon = action.intentIcon,
                RuntimeValue = action.runtimeValue
            };
        }

        int valueBonus = GetIntentVariable(action.valueVariableId, previewValues);
        if (action.divisor > 1f) valueBonus = Mathf.CeilToInt(valueBonus / action.divisor);
        if (action.maxBonus > 0) valueBonus = Mathf.Min(valueBonus, action.maxBonus);
        valueBonus += GetIntentVariable(action.additionalValueVariableId, previewValues);
        EnemyIntentEffectData result = BuildIntentEffect(action.effect, action.targetType, action.targetSelection,
            null, null, previewValues);
        result.ActionType = action.actionType;
        result.Value = CalculateIntentValue(action.effect, valueBonus);
        if (!string.IsNullOrEmpty(action.valueMultiplierVariableId)) result.Value *= GetIntentVariable(action.valueMultiplierVariableId, previewValues);
        int hitVariable = GetIntentVariable(action.hitCountVariableId, previewValues);
        result.HitCount = Mathf.Max(1, action.useHitCountVariableAsTotal ? hitVariable : action.effect.hitCount + hitVariable);
        result.Icon = action.intentIcon != null ? action.intentIcon
            : action.effect?.statusIcon != null ? action.effect.statusIcon : IconLibrary.FindIcon(result.EffectType);
        return result;
    }

    private Sprite GetRuntimeVariableIcon(string id)
    {
        EnemyRuntimeVariableDefinition definition = GetRuntimeState().GetDefinition(id);
        if (definition == null) return null;
        return definition.displayIcon != null ? definition.displayIcon
            : GetRuntimeState().Source.enemyEnhancementIcon != null ? GetRuntimeState().Source.enemyEnhancementIcon
            : IconLibrary.FindEnhancementIcon();
    }

    private string GetRuntimeVariableName(string id)
    {
        EnemyRuntimeVariableDefinition definition = GetRuntimeState().GetDefinition(id);
        return definition == null || string.IsNullOrWhiteSpace(definition.displayName) ? id : definition.displayName;
    }

    private EnemyIntentEffectData BuildIntentEffect(EffectEntry effect, TargetType targetType,
        EnemyTargetSelection targetSelection, string valueVariableId, string hitCountVariableId,
        Dictionary<string, int> previewValues = null)
    {
        if (effect == null) return new EnemyIntentEffectData();
        return new EnemyIntentEffectData
        {
            ActionType = EnemyPatternActionType.Effect,
            EffectType = effect.type,
            DamageType = effect.damageType,
            TargetType = targetType,
            TargetSelection = targetSelection,
            Value = CalculateIntentValue(effect, GetIntentVariable(valueVariableId, previewValues)),
            HitCount = Mathf.Max(1, effect.hitCount + GetIntentVariable(hitCountVariableId, previewValues)),
            Duration = effect.duration,
            RuntimeVariableId = valueVariableId
        };
    }

    private int GetIntentVariable(string id, Dictionary<string, int> previewValues)
    {
        if (string.IsNullOrWhiteSpace(id)) return 0;
        if (previewValues != null && previewValues.TryGetValue(id, out int value)) return value;
        return GetRuntimeState().Get(id);
    }

    private float CalculateIntentValue(EffectEntry effect, int runtimeValue)
    {
        if (effect.type == EffectType.Counter) return _self.status.FinalAttack + effect.value + runtimeValue;
        if (effect.type == EffectType.Pierce || effect.type == EffectType.Reflect) return effect.value;
        if (effect.type == EffectType.Strength || effect.type == EffectType.Amplification || effect.type == EffectType.Protection || effect.type == EffectType.Vulnerable)
            return (!Mathf.Approximately(effect.value, 0f) ? effect.value : effect.fixedValue) + runtimeValue;
        if (effect.type == EffectType.Bleed || effect.type == EffectType.Poison || effect.type == EffectType.Burn)
            return effect.value + runtimeValue;

        bool usesSpellPower = (effect.type == EffectType.Damage && effect.damageType == DamageType.Magical)
            || effect.type == EffectType.MentalDamage || effect.type == EffectType.MentalHeal;
        bool usesActorStat = effect.type == EffectType.Damage || effect.type == EffectType.Heal
            || effect.type == EffectType.Shield || effect.type == EffectType.MentalDamage
            || effect.type == EffectType.MentalHeal || effect.type == EffectType.Bleed || effect.type == EffectType.Poison;
        float baseValue = !usesActorStat || Mathf.Approximately(effect.multiplier, 0f)
            ? 0f : usesSpellPower ? _self.status.FinalSpellPower : _self.status.FinalAttack;
        float value = (baseValue * effect.multiplier) + effect.fixedValue + runtimeValue;
        if (effect.type == EffectType.Damage && Mathf.Approximately(effect.multiplier, 0f))
        {
            if (effect.damageType == DamageType.Physical) value += _self.status.AttackStatusModifier;
            else if (effect.damageType == DamageType.Magical) value += _self.status.SpellPowerStatusModifier;
        }
        return value;
    }

    private static EnemyIntentVisualType ResolveVisualType(List<EnemyIntentEffectData> effects)
    {
        if (effects.Exists(effect => effect.ActionType == EnemyPatternActionType.Effect
            && (effect.EffectType == EffectType.Damage || effect.EffectType == EffectType.MentalDamage)))
            return EnemyIntentVisualType.Attack;
        if (effects.Exists(effect => effect.ActionType == EnemyPatternActionType.Effect
            && (effect.EffectType == EffectType.Shield || effect.EffectType == EffectType.Protection || effect.EffectType == EffectType.Guard)))
            return EnemyIntentVisualType.Defend;
        if (effects.Exists(effect => effect.ActionType == EnemyPatternActionType.Effect
            && (effect.EffectType == EffectType.Heal || effect.EffectType == EffectType.MentalHeal || effect.EffectType == EffectType.Strength)))
            return EnemyIntentVisualType.Support;
        return EnemyIntentVisualType.Special;
    }

    private bool _hallucinating;

    /// <summary>Pattern for this turn: rotation, or 강박's locked pattern.</summary>
    private int SelectPatternIndex(List<EnemyPatternData> patterns)
    {
        int index = Mathf.Abs(_patternIndex) % patterns.Count;
        var panic = _self.status.panic;
        if (panic == null || !panic.IsActive) return index;
        if (panic.Type == TheLastArk.Battle.PanicType.Compulsion && panic.LockedPatternIndex >= 0)
            return panic.LockedPatternIndex % patterns.Count;
        return index;
    }

    /// <summary>A pattern that damages the enemy side (players).</summary>
    public static bool IsAttackPattern(EnemyPatternData pattern)
    {
        if (pattern == null) return false;
        if (pattern.effects != null && pattern.effects.Exists(e => e != null && e.type == EffectType.Damage)) return true;
        return pattern.actions != null && pattern.actions.Exists(a => a != null && a.actionType == EnemyPatternActionType.Effect
            && a.effect != null && a.effect.type == EffectType.Damage && a.targetType != TargetType.Self
            && a.targetType != TargetType.Friendly && a.targetType != TargetType.AllFriendly);
    }

    /// <summary>환각: a single-target attack pattern goes to a random living combatant (self included); taunt is skipped.</summary>
    private void ApplyHallucination(TheLastArk.Battle.AttackContext attack)
    {
        _hallucinating = false;
        var panic = _self.status.panic;
        if (panic == null || panic.Type != TheLastArk.Battle.PanicType.Hallucination || !IsAttackPattern(_preparedPattern)
            || !TargetResolver.IsTauntAffected(_preparedPattern.targetType)) return;
        var candidates = GetAlive(targetParty);
        candidates.AddRange(GetAlive(allyParty));
        if (candidates.Count == 0) return;
        var main = candidates[Random.Range(0, candidates.Count)];
        _preparedTargets = ResolveTargets(_preparedPattern.targetType, EnemyTargetSelection.Random, main);
        attack.IgnoreTaunt = true;
        _hallucinating = true;
    }

    /// <summary>혼란: summed % chance to re-centre a single-primary pattern on a random valid target at execution.</summary>
    private void ApplyConfusion(TargetType targetType, EnemyTargetSelection selection)
    {
        float chance = _self.status.GetStatusValue(EffectType.Confusion) * 0.01f;
        if (chance <= 0f || targetType == TargetType.Self || targetType == TargetType.AllEnemy || targetType == TargetType.AllFriendly
            || selection == EnemyTargetSelection.Remembered || Random.value >= chance) return;
        var candidates = GetAlive(IsFriendlyTarget(targetType) ? allyParty : targetParty);
        if (candidates.Count == 0) return;
        _preparedTargets = ResolveTargets(targetType, selection, candidates[Random.Range(0, candidates.Count)]);
    }

    private List<BattleCharacter> ResolveTargets(TargetType targetType, EnemyTargetSelection selection, BattleCharacter forcedMain = null)
    {
        var result = new List<BattleCharacter>();
        if (selection == EnemyTargetSelection.Remembered)
        {
            AddIfAlive(result, GetRuntimeState().RememberedTarget);
            return result;
        }
        List<BattleCharacter> party = IsFriendlyTarget(targetType) ? allyParty : targetParty;

        if (targetType == TargetType.Self)
        {
            AddIfAlive(result, _self);
            return result;
        }

        // 도발은 타격마다 BattleCharacter의 피해 경로에서 판정합니다.
        BattleCharacter main = forcedMain != null ? forcedMain : PickTarget(party, selection);
        _preparedPrimary = main;

        switch (targetType)
        {
            case TargetType.SingleEnemy:
            case TargetType.Friendly:
                AddIfAlive(result, main);
                break;
            case TargetType.LeftEnemy:
            case TargetType.FriendlyLeft:
                AddIfAlive(result, main);
                AddNeighbor(result, party, main, -1);
                break;
            case TargetType.RightEnemy:
            case TargetType.FriendlyRight:
                AddIfAlive(result, main);
                AddNeighbor(result, party, main, 1);
                break;
            case TargetType.AdjacentEnemy:
            case TargetType.FriendlyAdjacent:
                AddNeighbor(result, party, main, -1);
                AddIfAlive(result, main);
                AddNeighbor(result, party, main, 1);
                break;
            case TargetType.AllEnemy:
            case TargetType.AllFriendly:
                AddAllAlive(result, party);
                break;
        }

        return result;
    }

    private bool IsFriendlyTarget(TargetType targetType)
    {
        return targetType == TargetType.Friendly
            || targetType == TargetType.AllFriendly
            || targetType == TargetType.FriendlyLeft
            || targetType == TargetType.FriendlyRight
            || targetType == TargetType.FriendlyAdjacent
            || targetType == TargetType.Self;
    }

    private BattleCharacter PickTarget(List<BattleCharacter> party, EnemyTargetSelection selection)
    {
        List<BattleCharacter> alive = GetAlive(party);
        if (alive.Count == 0) return null;

        if (selection == EnemyTargetSelection.Leader)
        {
            BattleCharacter leader = alive.Find(character => character.isLeader);
            return leader != null ? leader : alive[Random.Range(0, alive.Count)];
        }

        if (selection == EnemyTargetSelection.LowestHp || selection == EnemyTargetSelection.HighestHp)
        {
            BattleCharacter selected = alive[0];
            float selectedRatio = GetHpRatio(selected);
            for (int i = 1; i < alive.Count; i++)
            {
                float ratio = GetHpRatio(alive[i]);
                if ((selection == EnemyTargetSelection.LowestHp && ratio < selectedRatio)
                    || (selection == EnemyTargetSelection.HighestHp && ratio > selectedRatio))
                {
                    selected = alive[i];
                    selectedRatio = ratio;
                }
            }
            return selected;
        }

        return alive[Random.Range(0, alive.Count)];
    }

    private float GetHpRatio(BattleCharacter character)
    {
        return character.status.currentHp / Mathf.Max(1f, character.status.FinalMaxHp);
    }

    private List<BattleCharacter> GetAlive(List<BattleCharacter> party)
    {
        // 체력이 남아 있는 캐릭터만 모읍니다.
        var alive = new List<BattleCharacter>();
        if (party == null) return alive;

        foreach (BattleCharacter character in party)
        {
            if (character != null && character.status != null && character.status.currentHp > 0)
                alive.Add(character);
        }

        return alive;
    }

    private void AddAllAlive(List<BattleCharacter> result, List<BattleCharacter> party)
    {
        if (party == null) return;
        foreach (BattleCharacter character in party) AddIfAlive(result, character);
    }

    private void AddNeighbor(List<BattleCharacter> result, List<BattleCharacter> party, BattleCharacter main, int offset)
    {
        // 기준 대상의 왼쪽/오른쪽 이웃을 결과에 추가합니다.
        if (main == null || party == null || party.IndexOf(main) < 0) return;
        int index = party.IndexOf(main) + offset;
        if (index < 0 || index >= party.Count) return;
        AddIfAlive(result, party[index]);
    }

    private void AddIfAlive(List<BattleCharacter> result, BattleCharacter character)
    {
        // 중복 없이 살아있는 캐릭터만 결과에 넣습니다.
        if (character == null || character.status == null || character.status.currentHp <= 0) return;
        if (!result.Contains(character)) result.Add(character);
    }

    private bool HasStatus(EffectType type)
    {
        // 현재 적에게 특정 상태이상이 걸려 있는지 확인합니다.
        if (_self?.status?.activeStatusEffects == null) return false;
        return _self.status.activeStatusEffects.Exists(effect => effect.effectType == type && effect.remainingTurns > 0);
    }
}
