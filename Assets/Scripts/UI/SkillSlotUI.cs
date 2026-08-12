using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TheLastArk.UI;

namespace UI
{
    public class SkillSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Component References")]
        public Image iconImage;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI nameText;
        public Button button;

        private SkillInfo currentSkill;
        private BattleCharacter currentActor;

        private void AutoFindComponents()
        {
            if (button == null) button = GetComponent<Button>();

            if (iconImage == null)
            {
                Image[] imgs = GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.gameObject != gameObject) { iconImage = img; break; }
                }
                if (iconImage == null) iconImage = GetComponent<Image>();
            }

            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps.Length > 0)
            {
                foreach (var tmp in tmps)
                {
                    if (tmp.name.ToLower().Contains("cost")) costText = tmp;
                    else if (tmp.name.ToLower().Contains("name") || tmp.name.ToLower().Contains("title")) nameText = tmp;
                }

                if (nameText == null && tmps.Length > 0) nameText = tmps[0];
                if (costText == null && tmps.Length > 1) costText = tmps[1];
            }
        }

        public void SetupSlot(SkillInfo skill, BattleCharacter actor)
        {
            currentSkill = skill;
            currentActor = actor;

            AutoFindComponents();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClickSlot);
            }

            if (skill != null)
            {
                int skillLevelIdx = (actor != null && actor.status != null) ? actor.status.SkillLevelIndex : 0;
                int cost = skill.baseCost;
                if (skill.levels != null && skill.levels.Length > skillLevelIdx && skill.levels[skillLevelIdx] != null && skill.levels[skillLevelIdx].overrideCost >= 0)
                {
                    cost = skill.levels[skillLevelIdx].overrideCost;
                }

                if (nameText != null)
                {
                    nameText.text = skill.skillName;
                    nameText.font = TMPFontManager.MainKoreanFont;
                    nameText.color = Color.white;
                }

                if (costText != null)
                {
                    costText.text = $"{cost}";
                    costText.font = TMPFontManager.MainKoreanFont;
                    costText.color = Color.yellow;
                }

                if (iconImage != null)
                {
                    if (skill.skillIcon != null)
                    {
                        iconImage.sprite = skill.skillIcon;
                        iconImage.color = Color.white;
                    }
                    else
                    {
                        iconImage.sprite = null;
                        iconImage.color = new Color(0.2f, 0.4f, 0.65f, 0.9f); // 아이콘 없을 시 깔끔한 파란 배경
                    }
                }
            }
        }

        public void OnClickSlot()
        {
            if (currentSkill != null && currentActor != null)
            {
                SkillTooltipUI.Instance.HideTooltip();

                var bm = FindObjectOfType<BattleManager>();
                if (bm != null)
                {
                    if (BattleManager.SkillFirstTargeting && button != null) button.Select();
                    bm.SelectSkill(currentSkill, currentActor);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (currentSkill != null)
            {
                SkillTooltipUI.Instance.ShowTooltip(currentSkill, currentActor, GetComponent<RectTransform>());
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SkillTooltipUI.Instance.HideTooltip();
        }

        private void OnDisable()
        {
            if (SkillTooltipUI.HasInstance)
            {
                SkillTooltipUI.Instance.HideTooltip();
            }
        }
    }
}
