using System;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public TargetArrow targetHandler;
    public List<BattleCharacter> enemyParty;
    public List<BattleCharacter> playerParty;

    [Header("Global Resource (Shared AP)")]
    public int maxAP = 10;
    public int currentAP;
    public TextMeshProUGUI apText; // 인스펙터에서 연결 필요

    [Header("Current Selection")]
    public SkillInfo selectedSkill;
    public BattleCharacter selectedActor;

    // [수정]: Start를 삭제하고 GameManager가 호출할 초기화 함수를 만듭니다.
    public void InitializeAP()
    {
        currentAP = maxAP;
        UpdateAPUI();
    }

    public void SelectSkill(SkillInfo skill, BattleCharacter actor)
    {
        selectedSkill = skill;
        selectedActor = actor;
        if (targetHandler.target != null)
        {
            PerformSkill();
        }
        else
        {
            Debug.Log("타겟을 먼저 지정해야합니다.");
            NotificationManager.Instance.ShowMessage("타겟을 먼저 지정해야합니다!", Color.yellow);
        }
    }

    public void PerformSkill()
    {
        if (selectedSkill == null || selectedActor == null || targetHandler.target == null) return;

        BattleCharacter primaryTarget = targetHandler.target.GetComponent<BattleCharacter>();
        if (primaryTarget == null) return;

        // 0, 1강 -> Index 0 / 2강 -> Index 1 / 3, 4강 -> Index 2
        int charLvl = selectedActor.status.charLevel;
        int skillIdx = 0;
        if (charLvl == 2) skillIdx = 1;
        else if (charLvl >= 3) skillIdx = 2;

        SkillLevelData levelData = selectedSkill.levels[Mathf.Clamp(skillIdx, 0, selectedSkill.levels.Length - 1)];

        // 2. 타겟 유효성 검사 (피아 구분)
        if (!IsTargetValidForSkill(primaryTarget, levelData.targetType))
        {
            NotificationManager.Instance.ShowMessage($"[발동 실패] {selectedSkill.skillName}은(는) 해당 타겟에게 사용할 수 없습니다.", Color.red);
            selectedSkill = null; // 선택 해제하여 땜빵 방지
            return;
        }

        int finalCost = (levelData.overrideCost != -1) ? levelData.overrideCost : selectedSkill.baseCost;

        if (currentAP >= finalCost)
        {
            currentAP -= finalCost;
            UpdateAPUI();

            List<BattleCharacter> allTargets = GetFinalTargets(primaryTarget, levelData.targetType);

            // EffectEngine이 status.FinalAttack을 사용하도록 수정되어야 함
            EffectEngine.ProcessSkill(selectedActor, allTargets, levelData);

            selectedSkill = null;
            //targetHandler.Deselect();
        } else {
            Debug.Log("행동력 부족");
            return;
        }
    }

    // 타겟과 스킬 타입이 일치하는지 확인
    private bool IsTargetValidForSkill(BattleCharacter clickedTarget, TargetType skillType)
    {
        bool isTargetEnemy = enemyParty.Contains(clickedTarget);
        bool isTargetAlly = playerParty.Contains(clickedTarget);

        switch (skillType)
        {
            // 아군 전용 타입들
            case TargetType.Friendly:
            case TargetType.AllFriendly:
                return isTargetAlly;

            // 적군 전용 타입들 (나머지 모두)
            default:
                return isTargetEnemy;
        }
    }
    public void UpdateAPUI()
    {
        if (apText != null)
        {
            apText.text = $"AP: {currentAP} / {maxAP}";
            Debug.Log($"AP UI 업데이트됨: {apText.text}"); // 이 로그가 Console에 찍히는지 확인하세요.
        }
        else
        {
            Debug.LogError("BattleManager: apText가 인스펙터에서 연결되지 않았습니다!");
        }
    }

    // [수정]: CharacterData.cs에 정의된 사용자님의 TargetType 명칭을 정확히 참조합니다.
    private List<BattleCharacter> GetFinalTargets(BattleCharacter mainTarget, TargetType type)
    {
        List<BattleCharacter> targets = new List<BattleCharacter>();

        // 클릭된 타겟이 속한 리스트를 찾음
        bool isEnemyTeam = enemyParty.Contains(mainTarget);
        List<BattleCharacter> team = isEnemyTeam ? enemyParty : playerParty;
        int index = team.IndexOf(mainTarget);

        switch (type)
        {
            case TargetType.SingleEnemy:
            case TargetType.Friendly:
                targets.Add(mainTarget);
                break;

            case TargetType.AdjacentEnemy:
                if (index > 0) targets.Add(team[index - 1]);
                targets.Add(mainTarget);
                if (index < team.Count - 1) targets.Add(team[index + 1]);
                break;

            case TargetType.AllEnemy:
                targets.AddRange(enemyParty);
                break;

            case TargetType.AllFriendly:
                targets.AddRange(playerParty);
                break;

            case TargetType.LeftEnemy:
                targets.Add(mainTarget);
                if (index > 0) targets.Add(team[index - 1]);
                break;

            case TargetType.RightEnemy:
                targets.Add(mainTarget);
                if (index < team.Count - 1) targets.Add(team[index + 1]);
                break;
        }
        return targets;
    }
}