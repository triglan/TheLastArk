using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image skillIcon;
    public TextMeshProUGUI costText;
    public Button slotButton; // 인스펙터에서 버튼 컴포넌트 연결

    [Header("Data")]
    public SkillInfo assignedSkill;
    public BattleCharacter skillOwner;

    public void SetSlot(SkillInfo data, BattleCharacter owner)
    {
        assignedSkill = data;
        skillOwner = owner;

        if (assignedSkill != null)
        {
            gameObject.SetActive(true);
            if (skillIcon != null) skillIcon.sprite = assignedSkill.skillIcon;
            if (costText != null) costText.text = assignedSkill.baseCost.ToString();
        }
        else 
        { 
            gameObject.SetActive(false); 
        }
    }

    public void OnClickSlot()
    {
        if (assignedSkill == null || skillOwner == null) return;

        BattleManager bm = FindAnyObjectByType<BattleManager>();
        if (bm != null) bm.SelectSkill(assignedSkill, skillOwner);
    }
}