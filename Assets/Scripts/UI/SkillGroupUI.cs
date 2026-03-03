using UnityEngine;
using System.Collections.Generic;

public class SkillGroupUI : MonoBehaviour
{
    // 프리팹 내부의 자식 슬롯 4개를 인스펙터에서 연결하세요.
    public List<SkillSlotUI> slots;

    public void SetupGroup(SkillData[] skills, BattleCharacter owner)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            // 캐릭터가 가진 activeSkills 4개를 슬롯에 순서대로 배정합니다.
            if (i < skills.Length && skills[i] != null)
            {
                slots[i].SetSlot(skills[i], owner);
            }
            else
            {
                slots[i].SetSlot(null, null); // 스킬이 없으면 빈 슬롯 처리
            }
        }
    }
}