using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TheLastArk.Data;
using TheLastArk.Character;

namespace TheLastArk.UI
{
    public class SynergyItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TextMeshProUGUI iconText;
        public TextMeshProUGUI badgeText;
        public Image bgImage;

        private SynergyType currentType;
        private int currentCount;

        public void SetupItem(SynergyType type, int count)
        {
            currentType = type;
            currentCount = count;

            SynergyInfo info = SynergyDatabase.GetInfo(type);
            int nextThresh = info.GetNextThreshold(count);
            bool isActive = (count >= (info.tiers != null && info.tiers.Count > 0 ? info.tiers[0].threshold : 1));

            if (iconText != null)
            {
                iconText.text = info.iconEmoji;
                iconText.font = TMPFontManager.MainKoreanFont;
                iconText.enableAutoSizing = true;
                iconText.fontSizeMin = 9;
                iconText.fontSizeMax = 14;
                iconText.alignment = TextAlignmentOptions.Center;
            }

            if (badgeText != null)
            {
                string countColor = isActive ? "#79FF5B" : "#CCCCCC";
                badgeText.text = $"<color={countColor}>{count}</color>/{nextThresh}";
                badgeText.font = TMPFontManager.MainKoreanFont;
                badgeText.fontSize = 11;
                badgeText.alignment = TextAlignmentOptions.BottomRight;
            }

            if (bgImage != null)
            {
                bgImage.color = isActive ? new Color(0.12f, 0.32f, 0.52f, 0.95f) : new Color(0.12f, 0.14f, 0.18f, 0.75f);
                bgImage.raycastTarget = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (SynergyTooltipUI.Instance != null)
            {
                SynergyTooltipUI.Instance.ShowTooltip(currentType, currentCount);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (SynergyTooltipUI.HasInstance)
            {
                SynergyTooltipUI.Instance.HideTooltip();
            }
        }

        private void OnDisable()
        {
            if (SynergyTooltipUI.HasInstance)
            {
                SynergyTooltipUI.Instance.HideTooltip();
            }
        }
    }
}
