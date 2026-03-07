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
                // CharacterData 내의 SkillInfo[] 배열(activeSkills)을 직접 전달
                skillGroups[i].SetupGroup(playerParty[i].status.origin.activeSkills, playerParty[i]);
            }
            else 
            { 
                skillGroups[i].gameObject.SetActive(false); 
            }
        }
    }
}