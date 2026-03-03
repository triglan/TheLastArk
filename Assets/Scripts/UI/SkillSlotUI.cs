using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image skillIcon;
    public TextMeshProUGUI costText;
    public Button slotButton;

    [Header("Data (Read Only)")]
    public SkillData assignedSkill;
    public BattleCharacter skillOwner;

    // 매니저가 스킬 정보를 주입할 때 사용합니다.
    public void SetSlot(SkillData data, BattleCharacter owner)
    {
        assignedSkill = data;
        skillOwner = owner;

        if (assignedSkill != null)
        {
            gameObject.SetActive(true);
            skillIcon.sprite = assignedSkill.skillIcon;
            costText.text = assignedSkill.baseCost.ToString();
        }
        else
        {
            // 배정된 스킬이 없으면 슬롯을 숨깁니다.
            gameObject.SetActive(false);
        }
    }

    public void OnClickSlot()
    {
        if (assignedSkill == null || skillOwner == null) return;

        // 매니저를 찾아 "이 캐릭터가 이 스킬을 선택했어!"라고 알립니다.
        BattleManager bm = FindAnyObjectByType<BattleManager>();
        if (bm != null) bm.SelectSkill(assignedSkill, skillOwner);

        Debug.Log($"{skillOwner.status.origin.characterName}의 {assignedSkill.skillName} 선택됨!");
    }
}