using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 캐릭터 한 명의 AI 행동을 정의합니다.
/// BattleCharacter에 붙여 사용하며, EnemyTurnHandler가 순서대로 호출합니다.
///
/// 확장 방법: EnemyAI를 상속해 HealerAI, TankAI 등을 만들 수 있습니다.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("행동 대상")]
    [Tooltip("이 AI가 공격할 아군 파티. BattleManager에서 주입합니다.")]
    public List<BattleCharacter> targetParty;

    private BattleCharacter _self;

    private void Awake()
    {
        _self = GetComponent<BattleCharacter>();
    }

    /// <summary>
    /// 적 턴에 EnemyTurnHandler가 호출합니다.
    /// 현재는 랜덤 대상에게 스킬 0번을 사용하는 기초 AI입니다.
    /// </summary>
    public void ExecuteTurn()
    {
        if (_self == null || _self.status == null) return;
        if (targetParty == null || targetParty.Count == 0) return;

        // 살아있는 대상만 추려 랜덤 선택
        var alive = targetParty.FindAll(c => c.status.currentHp > 0);
        if (alive.Count == 0) return;

        BattleCharacter target = alive[Random.Range(0, alive.Count)];
        UseFirstAvailableSkill(target);
    }

    private void UseFirstAvailableSkill(BattleCharacter target)
    {
        var skills = _self.status.dynamicActiveSkill;
        if (skills == null || skills.Count == 0)
        {
            // 스킬이 없으면 기본 공격 (고정 데미지)
            float rawDmg = _self.status.FinalAttack;
            target.ReceiveDamage(rawDmg, _self);
            Debug.Log($"[EnemyAI] {_self.characterName} → {target.characterName} 기본공격 {rawDmg}");
            return;
        }

        // 첫 번째 스킬 사용
        SkillInfo skill     = skills[0];
        int skillIdx        = _self.status.SkillLevelIndex;
        SkillLevelData data = skill.levels[Mathf.Clamp(skillIdx, 0, skill.levels.Length - 1)];

        // 싱글 타겟만 처리 (적 AI는 단순하게)
        var targets = new System.Collections.Generic.List<BattleCharacter> { target };
        EffectEngine.ProcessSkill(_self, targets, data);

        Debug.Log($"[EnemyAI] {_self.characterName} → {target.characterName} [{skill.skillName}]");
    }
}
