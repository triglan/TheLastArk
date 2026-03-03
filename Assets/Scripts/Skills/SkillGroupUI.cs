using UnityEngine;
using System.Collections.Generic;

public class SkillGroupUI : MonoBehaviour
{
    // 한 명의 캐릭터가 가진 4개의 슬롯 리스트
    public List<SkillSlotUI> slots;

    public void SetupGroup(SkillData[] skills, BattleCharacter owner)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < skills.Length && skills[i] != null)
            {
                slots[i].SetSlot(skills[i], owner);
            }
            else
            {
                slots[i].SetSlot(null, null); // 빈 칸 처리
            }
        }
    }
}