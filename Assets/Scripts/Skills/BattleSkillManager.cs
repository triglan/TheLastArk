using UnityEngine;
using System.Collections.Generic;

public class BattleSkillManager : MonoBehaviour
{
    public List<BattleCharacter> playerParty;
    public List<SkillGroupUI> skillGroups; // 이제 16개 슬롯 대신 4개 그룹을 관리합니다.

    public void LinkSkillsToUI()
    {
        for (int i = 0; i < skillGroups.Count; i++)
        {
            if (i < playerParty.Count && playerParty[i] != null)
            {
                skillGroups[i].gameObject.SetActive(true);
                // 자동 생성된 activeSkills 배열을 그룹에 전달합니다.
                skillGroups[i].SetupGroup(playerParty[i].status.origin.activeSkills, playerParty[i]);
            }
            else
            {
                // 파티원이 4명보다 적으면 해당 그룹 UI를 끕니다.
                skillGroups[i].gameObject.SetActive(false);
            }
        }
    }
}