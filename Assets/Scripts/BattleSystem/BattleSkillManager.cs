using UnityEngine;
using System.Collections.Generic;

public class BattleSkillManager : MonoBehaviour
{
    [Header("References")]
    public List<BattleCharacter> playerParty;

    // 이제 16개를 하나로 담지 않고, 캐릭터별 '그룹' 단위로 관리합니다.
    // 각 리스트 안에는 4개의 슬롯이 들어있어야 합니다.
    public List<SkillGroupUI> skillGroups;

    public void LinkSkillsToUI()
    {
        RefreshSkillGrid();
        Debug.Log("모든 아군 스킬을 UI 슬롯에 연결했습니다.");
    }

    public void RefreshSkillGrid()
    {
        // 1. 모든 캐릭터 그룹을 순회 (최대 4개 그룹)
        for (int i = 0; i < skillGroups.Count; i++)
        {
            // 해당 순서에 캐릭터가 있다면 스킬을 채우고, 없으면 그룹 전체를 끕니다.
            if (i < playerParty.Count && playerParty[i] != null)
            {
                skillGroups[i].gameObject.SetActive(true);
                // 에러 해결: .skills를 .activeSkills로 변경
                skillGroups[i].SetupGroup(playerParty[i].status.origin.activeSkills, playerParty[i]);
            }
            else
            {
                skillGroups[i].gameObject.SetActive(false);
            }
        }
    }
}