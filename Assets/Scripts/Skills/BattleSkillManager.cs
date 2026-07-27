using UnityEngine;
using System.Collections.Generic;
using UI;
using TheLastArk.UI;

public class BattleSkillManager : MonoBehaviour
{
    public List<SkillGroupUI> skillGroups;

    public void LinkSkillsToUI()
    {
        var bm = FindObjectOfType<BattleManager>();
        if (bm == null || bm.playerParty == null || skillGroups == null) return;

        var playerParty = bm.playerParty;
        for (int i = 0; i < skillGroups.Count; i++)
        {
            if (skillGroups[i] == null) continue;

            if (i < playerParty.Count && playerParty[i] != null && playerParty[i].status != null)
            {
                skillGroups[i].gameObject.SetActive(true);
                skillGroups[i].SetupGroup(playerParty[i].status.dynamicActiveSkill, playerParty[i]);
            }
            else
            {
                skillGroups[i].gameObject.SetActive(false);
            }
        }
    }
}