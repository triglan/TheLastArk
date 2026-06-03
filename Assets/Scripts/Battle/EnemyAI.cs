using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("대상 파티")]
    public List<BattleCharacter> targetParty;
    public List<BattleCharacter> allyParty;

    private BattleCharacter _self;
    private int _patternIndex;

    private void Awake()
    {
        _self = GetComponent<BattleCharacter>();
    }

    public void ExecuteTurn()
    {
        // 기절 상태면 이번 적 턴의 행동을 건너뜁니다.
        if (_self == null || _self.status?.origin == null) return;
        if (targetParty == null || targetParty.Count == 0) return;
        if (HasStatus(EffectType.Stun))
        {
            Debug.Log($"[EnemyAI] {_self.characterName} 기절로 행동하지 못함");
            return;
        }

        // 데이터에 등록된 패턴을 순서대로 실행합니다.
        List<EnemyPatternData> patterns = _self.status.origin.enemyPatterns;
        if (patterns == null || patterns.Count == 0)
        {
            UseBasicAttack();
            return;
        }

        EnemyPatternData pattern = patterns[Mathf.Abs(_patternIndex) % patterns.Count];
        _patternIndex = (_patternIndex + 1) % patterns.Count;

        if (pattern == null || pattern.effects == null || pattern.effects.Count == 0)
        {
            UseBasicAttack();
            return;
        }

        List<BattleCharacter> targets = ResolveTargets(pattern.targetType);
        if (targets.Count == 0) return;

        SkillLevelData runtimePattern = new SkillLevelData
        {
            overrideCost = -1,
            targetType = pattern.targetType,
            effects = pattern.effects
        };

        EffectEngine.ProcessSkill(_self, targets, runtimePattern);
        Debug.Log($"[EnemyAI] {_self.characterName} 패턴 실행: {pattern.patternName}");
    }

    private void UseBasicAttack()
    {
        // 패턴이 없거나 비어 있으면 기본 공격을 사용합니다.
        BattleCharacter target = PickRandomAlive(targetParty);
        if (target == null) return;

        float rawDmg = _self.status.FinalAttack;
        target.ReceiveDamage(rawDmg, _self);
        Debug.Log($"[EnemyAI] {_self.characterName} -> {target.characterName} 기본 공격 {rawDmg}");
    }

    private List<BattleCharacter> ResolveTargets(TargetType targetType)
    {
        // 패턴의 대상 타입을 실제 살아있는 전투 캐릭터 목록으로 바꿉니다.
        var result = new List<BattleCharacter>();
        var enemies = GetAlive(targetParty);
        var allies = GetAlive(allyParty);
        if (_self != null && _self.status != null && _self.status.currentHp > 0 && !allies.Contains(_self))
            allies.Add(_self);

        BattleCharacter mainEnemy = PickRandomAlive(enemies);
        BattleCharacter mainAlly = PickRandomAlive(allies);

        switch (targetType)
        {
            case TargetType.SingleEnemy:
                AddIfAlive(result, mainEnemy);
                break;
            case TargetType.LeftEnemy:
                AddNeighbor(result, enemies, mainEnemy, -1);
                break;
            case TargetType.RightEnemy:
                AddNeighbor(result, enemies, mainEnemy, 1);
                break;
            case TargetType.AdjacentEnemy:
                AddNeighbor(result, enemies, mainEnemy, -1);
                AddIfAlive(result, mainEnemy);
                AddNeighbor(result, enemies, mainEnemy, 1);
                break;
            case TargetType.AllEnemy:
                result.AddRange(enemies);
                break;
            case TargetType.Friendly:
                AddIfAlive(result, mainAlly);
                break;
            case TargetType.AllFriendly:
                result.AddRange(allies);
                break;
        }

        return result;
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

    private BattleCharacter PickRandomAlive(List<BattleCharacter> party)
    {
        // 살아있는 대상 중 하나를 무작위로 고릅니다.
        List<BattleCharacter> alive = GetAlive(party);
        if (alive.Count == 0) return null;
        return alive[Random.Range(0, alive.Count)];
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
