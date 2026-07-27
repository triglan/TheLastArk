using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TheLastArk.UI;

namespace UI
{
    public class SkillGroupUI : MonoBehaviour
    {
        public TextMeshProUGUI charNameText;
        public Image portraitImage;
        public List<SkillSlotUI> slotUIList = new List<SkillSlotUI>();

        public void SetupGroup(List<SkillInfo> skills, BattleCharacter owner)
        {
            if (charNameText == null) charNameText = GetComponentInChildren<TextMeshProUGUI>();
            if (portraitImage == null) portraitImage = GetComponentInChildren<Image>();

            if (slotUIList == null || slotUIList.Count == 0)
            {
                slotUIList = new List<SkillSlotUI>(GetComponentsInChildren<SkillSlotUI>(true));
            }

            if (owner != null && owner.status != null)
            {
                if (charNameText != null)
                {
                    charNameText.text = owner.characterName;
                    charNameText.font = TMPFontManager.MainKoreanFont;
                }
                if (portraitImage != null && owner.status.origin != null && owner.status.origin.portraitSprite != null)
                {
                    portraitImage.sprite = owner.status.origin.portraitSprite;
                    portraitImage.color = Color.white;
                }
            }

            if (skills == null || slotUIList == null) return;

            for (int i = 0; i < slotUIList.Count; i++)
            {
                if (i < skills.Count && skills[i] != null)
                {
                    slotUIList[i].gameObject.SetActive(true);
                    slotUIList[i].SetupSlot(skills[i], owner);
                }
                else
                {
                    slotUIList[i].gameObject.SetActive(false);
                }
            }

            TMPFontManager.ApplyFontToAll(transform);
        }
    }
}