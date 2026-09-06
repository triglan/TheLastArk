using System.Collections.Generic;

/// <summary>
/// TargetType에 따라 실제 타겟 목록을 계산합니다.
/// BattleManager.GetFinalTargets() 와 IsTargetValidForSkill() 을
/// 여기로 옮겨서 단독으로 테스트할 수 있게 합니다.
/// </summary>
public static class TargetResolver
{
    /// <summary>
    /// 클릭된 캐릭터가 해당 스킬의 TargetType에 유효한 대상인지 검사합니다.
    /// </summary>
    public static bool IsValid(
        BattleCharacter clicked,
        TargetType skillType,
        List<BattleCharacter> playerParty,
        List<BattleCharacter> enemyParty)
    {
        bool isAlly = playerParty.Contains(clicked);
        bool hasCorruption = TheLastArk.Managers.TrainManager.IsInitialized && 
                             TheLastArk.Managers.TrainManager.Instance.HasPartEffectInAnyCar(TheLastArk.Data.TrainPartEffectType.CorruptionModule);

        BattleCharacter taunter = FindTauntTarget(enemyParty);
        if (IsTauntAffected(skillType) && taunter != null)
            return clicked == taunter;

        return skillType switch
        {
            TargetType.Friendly    => isAlly || hasCorruption,
            TargetType.AllFriendly => isAlly,
            _                      => enemyParty.Contains(clicked)
        };
    }

    /// <summary>
    /// TargetType과 주 타겟을 기반으로 실제 피해를 입힐 캐릭터 목록을 반환합니다.
    /// </summary>
    public static List<BattleCharacter> Resolve(
        BattleCharacter mainTarget,
        TargetType type,
        List<BattleCharacter> playerParty,
        List<BattleCharacter> enemyParty)
    {
        var result = new List<BattleCharacter>();

        if (IsTauntAffected(type))
        {
            BattleCharacter taunter = FindTauntTarget(enemyParty);
            if (taunter != null) mainTarget = taunter;
        }

        bool isEnemy = enemyParty.Contains(mainTarget);
        var team     = isEnemy ? enemyParty : playerParty;
        int idx      = team.IndexOf(mainTarget);

        switch (type)
        {
            case TargetType.SingleEnemy:
            case TargetType.Friendly:
            case TargetType.Self:
                result.Add(mainTarget);
                break;

            case TargetType.LeftEnemy:
                result.Add(mainTarget);
                if (idx > 0) result.Add(team[idx - 1]);
                break;

            case TargetType.RightEnemy:
                result.Add(mainTarget);
                if (idx < team.Count - 1) result.Add(team[idx + 1]);
                break;

            case TargetType.AdjacentEnemy:
                if (idx > 0) result.Add(team[idx - 1]);
                result.Add(mainTarget);
                if (idx < team.Count - 1) result.Add(team[idx + 1]);
                break;

            case TargetType.AllEnemy:
                result.AddRange(enemyParty);
                break;

            case TargetType.AllFriendly:
                result.AddRange(playerParty);
                break;
        }

        return result;
    }

    public static bool IsTauntAffected(TargetType type)
    {
        return type == TargetType.SingleEnemy || type == TargetType.LeftEnemy
            || type == TargetType.RightEnemy || type == TargetType.AdjacentEnemy;
    }

    public static BattleCharacter FindTauntTarget(List<BattleCharacter> party)
    {
        return party?.Find(character => character != null && character.status != null
            && character.status.currentHp > 0f && character.status.GetStatus(EffectType.Taunt) != null);
    }
}
