using UnityEngine;
using System.Collections.Generic;

public class SkillGroupUI : MonoBehaviour
{
    public List<SkillSlotUI> slots;

    public void SetupGroup(List<SkillInfo> skills, BattleCharacter owner)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            // 리스트의 Count를 기준으로 슬롯 활성화
            if (i < skills.Count && skills[i] != null)
            {
                slots[i].gameObject.SetActive(true);
                slots[i].SetSlot(skills[i], owner);
            }
            else
            {
                slots[i].gameObject.SetActive(false); // 남는 슬롯은 끔
            }
        }
    }
}