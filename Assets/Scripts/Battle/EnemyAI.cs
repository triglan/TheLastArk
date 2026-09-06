using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("대상 파티")]
    public List<BattleCharacter> targetParty;
    public List<BattleCharacter> allyParty;

    private BattleCharacter _self;
    private int _patternIndex;
    private int _completedCycles;
    private List<BattleCharacter> _preparedTargets;
    private EnemyPatternData _preparedPattern;

    public IReadOnlyList<BattleCharacter> PreparedTargets => _preparedTargets;
    public int CompletedCycles => _completedCycles;

    private void Awake()
    {
        _self = GetComponent<BattleCharacter>();
    }

    public bool PrepareTurn()
    {
        _preparedTargets = null;
        _preparedPattern = null;

        // 기절 상태면 이번 적 턴의 행동을 건너뜁니다.
        if (_self == null || _self.status?.origin == null) return false;
        if (targetParty == null || targetParty.Count == 0) return false;
        if (HasStatus(EffectType.Stun))
        {
            Debug.Log($"[EnemyAI] {_self.characterName} 기절로 행동하지 못함");
            return false;
        }

        // 데이터에 등록된 패턴을 순서대로 실행합니다.
        List<EnemyPatternData> patterns = _self.status.origin.enemyPatterns;
        if (patterns == null || patterns.Count == 0)
            return false;

        EnemyPatternData pattern = patterns[Mathf.Abs(_patternIndex) % patterns.Count];
        if (pattern == null || pattern.effects == null || pattern.effects.Count == 0)
            return false;

        _preparedTargets = ResolveTargets(pattern.targetType, pattern.targetSelection);
        if (_preparedTargets.Count == 0) return false;

        _preparedPattern = pattern;
        return true;
    }

    public void ExecuteTurn()
    {
        if (_preparedTargets == null && !PrepareTurn()) return;
        _preparedTargets.RemoveAll(target => target == null || target.status == null || target.status.currentHp <= 0f);
        if (_preparedTargets.Count == 0)
        {
            ClearPreparedTurn();
            return;
        }

        if (_preparedPattern == null)
        {
            BattleCharacter target = _preparedTargets[0];
            float rawDmg = _self.status.FinalAttack;
            VFXManager.Instance?.PlayDefaultEffect(EffectType.Damage, DamageType.Physical, target);
            target.ReceiveDamage(rawDmg, _self, DamageType.Physical);
            Debug.Log($"[EnemyAI] {_self.characterName} -> {target.characterName} 기본 공격 {rawDmg}");
            _self.ResolveActionStatusEffects();
            ClearPreparedTurn();
            return;
        }

        SkillLevelData runtimePattern = new SkillLevelData
        {
            overrideCost = -1,
            targetType = _preparedPattern.targetType,
            customVfxName = _preparedPattern.customVfxName,
            effects = BuildRuntimeEffects(_preparedPattern.effects)
        };

        EffectEngine.ProcessSkill(_self, _preparedTargets, runtimePattern, _preparedPattern.patternName);
        _self.ResolveActionStatusEffects();
        AdvancePattern();
        Debug.Log($"[EnemyAI] {_self.characterName} 패턴 실행: {_preparedPattern.patternName} (완료 사이클 {_completedCycles})");
        ClearPreparedTurn();
    }

    private List<EffectEntry> BuildRuntimeEffects(List<EffectEntry> sourceEffects)
    {
        var runtimeEffects = new List<EffectEntry>(sourceEffects.Count);
        float damageBonus = _self.status.origin.damageBonusPerCycle * _completedCycles;
        float bleedBonus = _self.status.origin.bleedBonusPerCycle * _completedCycles;

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
                customVfxName = source.customVfxName
            };

            if (effect.type == EffectType.Damage)
            {
                effect.fixedValue += damageBonus;
            }
            else if (effect.type == EffectType.Bleed && bleedBonus > 0f)
            {
                float baseBleed = effect.value > 0f
                    ? effect.value
                    : _self.status.FinalAttack * effect.multiplier;
                effect.value = baseBleed + bleedBonus;
                effect.multiplier = 0f;
            }

            runtimeEffects.Add(effect);
        }

        return runtimeEffects;
    }

    private void AdvancePattern()
    {
        int patternCount = _self.status.origin.enemyPatterns.Count;
        _patternIndex = (_patternIndex + 1) % patternCount;
        if (_patternIndex == 0) _completedCycles++;
    }

    private void ClearPreparedTurn()
    {
        _preparedTargets = null;
        _preparedPattern = null;
    }

    private List<BattleCharacter> ResolveTargets(TargetType targetType, EnemyTargetSelection selection)
    {
        var result = new List<BattleCharacter>();
        List<BattleCharacter> party = IsFriendlyTarget(targetType) ? allyParty : targetParty;

        if (targetType == TargetType.Self)
        {
            AddIfAlive(result, _self);
            return result;
        }

        // 도발은 타격마다 BattleCharacter의 피해 경로에서 판정합니다.
        BattleCharacter main = PickTarget(party, selection);

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
        if (main == null || party == null) return;
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
