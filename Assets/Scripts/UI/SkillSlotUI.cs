using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TheLastArk.UI;

namespace UI
{
    public class SkillSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
    {
        [Header("UI Component References")]
        public Image iconImage;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI nameText;
        public Button button;

        private SkillInfo currentSkill;
        private BattleCharacter currentActor;
        private BattleManager _battle;
        private GameObject _diceBorder;

        private void OnEnable()
        {
            DiceDragSource.HeldDieChanged += UpdateDiceHighlight;
            UpdateDiceHighlight(DiceDragSource.HeldDieId);
        }

        private void UpdateDiceHighlight(int dieId)
        {
            bool visible = dieId >= 0 && _battle != null && _battle.CanAcceptDieOnSkill(dieId, currentActor, currentSkill);
            if (visible && _diceBorder == null)
            {
                _diceBorder = new GameObject("UsableDieBorder", typeof(RectTransform), typeof(LayoutElement));
                _diceBorder.transform.SetParent(transform, false);
                _diceBorder.GetComponent<LayoutElement>().ignoreLayout = true;
                var root = _diceBorder.GetComponent<RectTransform>();
                root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
                root.offsetMin = new Vector2(-3, -3); root.offsetMax = new Vector2(3, 3);
                for (int i = 0; i < 4; i++)
                {
                    var edge = new GameObject("Edge", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    edge.transform.SetParent(root, false);
                    var rect = edge.GetComponent<RectTransform>();
                    rect.anchorMin = i == 0 ? new Vector2(0, 1) : i == 3 ? new Vector2(1, 0) : Vector2.zero;
                    rect.anchorMax = i == 1 ? new Vector2(1, 0) : i == 2 ? new Vector2(0, 1) : Vector2.one;
                    rect.sizeDelta = i < 2 ? new Vector2(0, 3) : new Vector2(3, 0);
                    rect.anchoredPosition = Vector2.zero;
                    var image = edge.GetComponent<UnityEngine.UI.Image>();
                    image.color = new Color(1f, 0.84f, 0.12f);
                    image.raycastTarget = false;
                }
            }
            if (_diceBorder != null) _diceBorder.SetActive(visible);
        }

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

                // Icon-only slots have a cost label but no visible skill name.
                if (costText == null && tmps.Length == 1) costText = tmps[0];
                if (nameText == costText) nameText = null;
            }
        }

        public void SetupSlot(SkillInfo skill, BattleCharacter actor)
        {
            currentSkill = skill;
            currentActor = actor;
            if (_battle == null) _battle = FindObjectOfType<BattleManager>();

            AutoFindComponents();
            UpdateDiceHighlight(DiceDragSource.HeldDieId);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClickSlot);
                BattleManager manager = FindObjectOfType<BattleManager>();
                button.interactable = manager == null || manager.CanUseDiceSkill(actor, skill);
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
                    BattleManager manager = FindObjectOfType<BattleManager>();
                    nameText.color = manager != null && manager.GetDiceSkillUnavailableReason(actor, skill) != null
                        ? new Color(1f, 0.6f, 0.55f) : Color.white;
                }

                if (costText != null)
                {
                    BattleManager manager = FindObjectOfType<BattleManager>();
                    var rule = skill.levels != null && skill.levels.Length > skillLevelIdx
                        ? skill.levels[skillLevelIdx]?.diceInput : null;
                    if (manager != null && manager.UsesDice)
                    {
                        string condition = rule?.ShortLabel() ?? "1개 · 자유";
                        if (rule != null && rule.chargeTarget > 0)
                            condition += $" · {manager.GetDiceCharge(actor, skill)}/{rule.chargeTarget}";
                        string reason = manager.GetDiceSkillUnavailableReason(actor, skill);
                        // Full restrictions stay in the hover detail; the card keeps a readable short status.
                        string shortReason = reason;
                        int separator = reason?.IndexOf('·') ?? -1;
                        if (separator >= 0) shortReason = reason.Substring(0, separator).Trim();
                        costText.text = reason == null ? condition : $"{condition}\n{shortReason}";
                        costText.color = reason == null ? Color.white : new Color(1f, 0.6f, 0.55f);
                        costText.enableAutoSizing = true;
                        costText.fontSizeMin = 12;
                        costText.fontSizeMax = 14;
                        costText.overflowMode = TextOverflowModes.Ellipsis;
                    }
                    else
                    {
                        string reason = manager != null ? manager.GetPanicSkillUnavailableReason(actor, skill) : null;
                        costText.text = reason == null ? $"{cost}" : $"{cost}\n{reason}";
                        costText.color = reason == null ? Color.white : new Color(1f, 0.6f, 0.55f);
                    }
                    costText.font = TMPFontManager.MainKoreanFont;
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
                    if (bm.UsesDice) return;
                    if (BattleManager.SkillFirstTargeting && button != null) button.Select();
                    bm.SelectSkill(currentSkill, currentActor);
                }
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (currentSkill == null || currentActor == null || eventData.pointerDrag == null) return;
            var source = eventData.pointerDrag.GetComponent<DiceDragSource>();
            var battle = FindObjectOfType<BattleManager>();
            if (source != null && battle != null)
                battle.DropDieOnSkill(source.DieId, currentSkill, currentActor);
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
            DiceDragSource.HeldDieChanged -= UpdateDiceHighlight;
            if (_diceBorder != null) _diceBorder.SetActive(false);
            if (SkillTooltipUI.HasInstance)
            {
                SkillTooltipUI.Instance.HideTooltip();
            }
        }
    }
}
