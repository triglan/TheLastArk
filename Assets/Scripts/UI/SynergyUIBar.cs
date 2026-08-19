using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TheLastArk.Character;
using TheLastArk.Data;

namespace TheLastArk.UI
{
    public class SynergyUIBar : MonoBehaviour
    {
        private const int PageSize = 5;

        private RectTransform barRect;
        private HorizontalLayoutGroup layoutGroup;
        private List<SynergyItemUI> itemSlotPool = new List<SynergyItemUI>();

        private Button prevButton;
        private Button nextButton;
        private TextMeshProUGUI pageIndicatorText;

        private int currentPage = 0;
        private int lastTotalCount = 0;

        public void Initialize(Transform parent)
        {
            if (parent == null) return;

            GameObject barObj = new GameObject("SynergyUIBar");
            barObj.transform.SetParent(parent, false);

            // 화면 우측 상단 고정 (상단 자원바 아래)
            barRect = barObj.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(1f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 1f);
            barRect.anchoredPosition = new Vector2(-15f, -75f);
            barRect.sizeDelta = new Vector2(340f, 54f);

            Image bg = barObj.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);
            bg.raycastTarget = false;

            layoutGroup = barObj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.padding = new RectOffset(8, 8, 5, 5);
            layoutGroup.spacing = 6;
            layoutGroup.childAlignment = TextAnchor.MiddleRight;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;

            // 1. 이전 페이지 화살표 버튼 [<]
            prevButton = CreateArrowButton("PrevBtn", "<", OnClickPrevPage);

            // 2. 최대 5개 정사각형 시너지 아이템 슬롯 생성
            for (int i = 0; i < PageSize; i++)
            {
                SynergyItemUI itemScript = CreateSquareItemObject();
                itemSlotPool.Add(itemScript);
            }

            // 3. 다음 페이지 화살표 버튼 [>]
            nextButton = CreateArrowButton("NextBtn", ">", OnClickNextPage);

            RefreshDisplay();
        }

        private Button CreateArrowButton(string name, string symbol, UnityEngine.Events.UnityAction onClickAction)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(barRect, false);

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(28f, 44f);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.22f, 0.32f, 0.9f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClickAction);

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = symbol;
            tmp.fontSize = 16;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.font = TMPFontManager.MainKoreanFont;
            tmp.raycastTarget = false;

            return btn;
        }

        private SynergyItemUI CreateSquareItemObject()
        {
            GameObject itemObj = new GameObject("SquareSynergyItem", typeof(RectTransform));
            itemObj.transform.SetParent(barRect, false);

            RectTransform rect = itemObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(44f, 44f); // 정사각형 모양

            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.18f, 0.28f, 0.85f);

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredWidth = 44f;
            le.preferredHeight = 44f;

            // Centered Large Icon Text
            GameObject iconObj = new GameObject("IconText", typeof(RectTransform));
            iconObj.transform.SetParent(itemObj.transform, false);
            RectTransform iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = Vector2.one;
            iRect.offsetMin = new Vector2(2, 6);
            iRect.offsetMax = new Vector2(-2, 0);

            TextMeshProUGUI iTmp = iconObj.AddComponent<TextMeshProUGUI>();
            iTmp.fontSize = 22;
            iTmp.alignment = TextAlignmentOptions.Center;
            iTmp.font = TMPFontManager.MainKoreanFont;
            iTmp.raycastTarget = false;

            // Bottom-Right Small Badge Text
            GameObject badgeObj = new GameObject("BadgeText", typeof(RectTransform));
            badgeObj.transform.SetParent(itemObj.transform, false);
            RectTransform bRect = badgeObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.2f, 0f);
            bRect.anchorMax = new Vector2(1f, 0.45f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = new Vector2(-3, 2);

            TextMeshProUGUI bTmp = badgeObj.AddComponent<TextMeshProUGUI>();
            bTmp.fontSize = 11;
            bTmp.alignment = TextAlignmentOptions.BottomRight;
            bTmp.font = TMPFontManager.MainKoreanFont;
            bTmp.raycastTarget = false;

            SynergyItemUI itemScript = itemObj.AddComponent<SynergyItemUI>();
            itemScript.iconText = iTmp;
            itemScript.badgeText = bTmp;
            itemScript.bgImage = bg;

            return itemScript;
        }

        private void OnClickPrevPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                RefreshDisplay();
            }
        }

        private void OnClickNextPage()
        {
            if ((currentPage + 1) * PageSize < lastTotalCount)
            {
                currentPage++;
                RefreshDisplay();
            }
        }

        public void RefreshDisplay()
        {
            if (barRect == null || layoutGroup == null) return;

            try
            {
                var rawSynergies = SynergyCalculator.CalculateActiveSynergies();
                if (rawSynergies == null) rawSynergies = new Dictionary<SynergyType, int>();

                // 요구사항: 시너지는 인원수 많은 순으로 정렬 (내림차순)
                var sortedSynergies = rawSynergies
                    .Where(kvp => kvp.Value > 0)
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenBy(kvp => kvp.Key.ToString())
                    .ToList();

                lastTotalCount = sortedSynergies.Count;

                // Clamp page range
                int maxPages = Mathf.Max(1, Mathf.CeilToInt((float)lastTotalCount / PageSize));
                currentPage = Mathf.Clamp(currentPage, 0, maxPages - 1);

                // Slice current page items
                var pageItems = sortedSynergies.Skip(currentPage * PageSize).Take(PageSize).ToList();

                // Update 5 Square Slots
                for (int i = 0; i < PageSize; i++)
                {
                    if (i < pageItems.Count)
                    {
                        itemSlotPool[i].gameObject.SetActive(true);
                        itemSlotPool[i].SetupItem(pageItems[i].Key, pageItems[i].Value);
                    }
                    else
                    {
                        itemSlotPool[i].gameObject.SetActive(false);
                    }
                }

                // Update Arrow Visibility (5개 초과 시에만 화살표 활성화)
                bool showPagination = (lastTotalCount > PageSize);
                if (prevButton != null)
                {
                    prevButton.gameObject.SetActive(showPagination && currentPage > 0);
                }
                if (nextButton != null)
                {
                    nextButton.gameObject.SetActive(showPagination && (currentPage + 1) * PageSize < lastTotalCount);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SynergyUIBar] Synergy refresh warning: {ex.Message}");
            }
        }

        private void Update()
        {
            RefreshDisplay();
        }
    }
}
