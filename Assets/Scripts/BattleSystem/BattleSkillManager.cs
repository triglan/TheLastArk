using UnityEngine;
using System.Collections.Generic;

public class BattleSkillManager : MonoBehaviour
{
    [Header("References")]
    public List<BattleCharacter> playerParty; // 아군 4명을 드래그해서 넣으세요
    public List<SkillSlotUI> allSlots;       // 16개 슬롯을 드래그해서 넣으세요

    public void LinkSkillsToUI()
    {
        RefreshSkillGrid();
        Debug.Log("모든 아군 스킬을 UI 슬롯에 연결했습니다.");
    }

    public void RefreshSkillGrid()
    {
        int slotIndex = 0;

        // 1. 모든 아군을 순회 (최대 4명)
        foreach (var character in playerParty)
        {
            if (character == null || character.status.origin == null) continue;

            // 2. 해당 캐릭터의 스킬 리스트 순회 (최대 4개)
            foreach (var skill in character.status.origin.skills)
            {
                if (slotIndex < allSlots.Count)
                {
                    // 3. 슬롯에 데이터 주입
                    allSlots[slotIndex].SetSlot(skill, character);
                    slotIndex++;
                }
            }
        }

        // 4. 남은 빈 슬롯들은 비활성화
        for (int i = slotIndex; i < allSlots.Count; i++)
        {
            allSlots[i].SetSlot(null, null);
        }
    }
}