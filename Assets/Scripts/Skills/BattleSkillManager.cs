using UnityEngine;
using System.Collections.Generic;

public class BattleSkillManager : MonoBehaviour
{
    public List<BattleCharacter> playerParty;
    public List<SkillGroupUI> skillGroups;

    public void LinkSkillsToUI()
    {
        for (int i = 0; i < skillGroups.Count; i++)
        {
            if (i < playerParty.Count && playerParty[i] != null)
            {
                skillGroups[i].gameObject.SetActive(true);
                // 원본(activeSkills)이 아닌 런타임 결정본(dynamicActiveSkill) 전달
                skillGroups[i].SetupGroup(playerParty[i].status.dynamicActiveSkill, playerParty[i]);
            }
            else { skillGroups[i].gameObject.SetActive(false); }
        }
    }
}