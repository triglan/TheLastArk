using UnityEngine;
using System.Collections.Generic;

public class SkillGroupUI : MonoBehaviour
{
    public List<SkillSlotUI> slots;

    public void SetupGroup(SkillInfo[] skills, BattleCharacter owner)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < skills.Length && skills[i] != null)
            {
                slots[i].SetSlot(skills[i], owner);
            }
            else 
            { 
                slots[i].SetSlot(null, null); 
            }
        }
    }
}