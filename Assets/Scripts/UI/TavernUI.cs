using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TheLastArk.Data;
using TheLastArk.Managers;
using TheLastArk.Character;

namespace TheLastArk.UI
{
    public class TavernUI : MonoBehaviour
    {
        private GameObject popupPanel;
        private TMP_FontAsset mainFont;

        private enum TabType { HireMercenary, SkillChange }
        private TabType currentTab = TabType.HireMercenary;

        private Transform contentArea;

        // Hire Mercenaries Data
        private List<CharacterData> availableMercenaries = new List<CharacterData>();
        private List<int> mercenaryPrices = new List<int>();
        private List<bool> mercenaryHired = new List<bool>();

        // Skill Change Data
        private CharacterData selectedCharacter;

        public void Show()
        {
            if (popupPanel == null)
            {
                CreateUI();
            }

            GenerateMercenaries();
            SwitchTab(TabType.HireMercenary);
            popupPanel.SetActive(true);
        }

        public void Hide()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
        }

        private void CreateUI()
        {
            mainFont = Resources.Load<TMP_FontAsset>("Fonts/Main_Fonts");

            Canvas canvas = FindObjectOfType<Canvas>();
            popupPanel = new GameObject("TavernPopup");
            popupPanel.transform.SetParent(canvas.transform, false);
            popupPanel.transform.SetAsLastSibling();

            RectTransform rect = popupPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = popupPanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

            // 닫기 버튼
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(popupPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-40, -40);
            closeRect.sizeDelta = new Vector2(100, 48);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);

            CreateTextUI(closeBtnObj.transform, "닫기", 24, Color.white);

            // Title Header (주점 - 용병 구매 & 스킬 변경)
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(popupPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.88f);
            titleRect.anchorMax = new Vector2(0.9f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            CreateTextUI(titleObj.transform, "🍺 주점 (용병 구매 및 스킬 변경)", 36, Color.yellow);

            // Tabs Container
            GameObject tabsObj = new GameObject("TavernTabsArea");
            tabsObj.transform.SetParent(popupPanel.transform, false);
            RectTransform tabsRect = tabsObj.AddComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0.1f, 0.80f);
            tabsRect.anchorMax = new Vector2(0.9f, 0.86f);
            tabsRect.offsetMin = Vector2.zero;
            tabsRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup tabLayout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 20;

            CreateTabButton(tabsObj.transform, "⚔️ 용병 고용", () => SwitchTab(TabType.HireMercenary));
            CreateTabButton(tabsObj.transform, "📜 스킬 변경", () => SwitchTab(TabType.SkillChange));

            // Content Area Container
            GameObject contentObj = new GameObject("TavernContentArea");
            contentObj.transform.SetParent(popupPanel.transform, false);
            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.1f, 0.08f);
            cRect.anchorMax = new Vector2(0.9f, 0.76f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            contentArea = contentObj.transform;
        }

        private void CreateTabButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject($"Tab_{text}");
            btnObj.transform.SetParent(parent, false);

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = 220;
            le.preferredHeight = 44;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.35f, 0.55f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(action);

            CreateTextUI(btnObj.transform, text, 20, Color.white);
        }

        private void GenerateMercenaries()
        {
            availableMercenaries.Clear();
            mercenaryPrices.Clear();
            mercenaryHired.Clear();

            var allChars = Resources.LoadAll<CharacterData>("Characters").ToList();
            if (allChars.Count == 0) allChars = Resources.LoadAll<CharacterData>("").ToList();

            List<CharacterData> candidates = new List<CharacterData>();
            foreach (var c in allChars)
            {
                if (c != null && !c.isEnemy) candidates.Add(c);
            }

            candidates = candidates.OrderBy(x => Random.value).ToList();
            int count = Mathf.Min(3, candidates.Count);

            for (int i = 0; i < count; i++)
            {
                availableMercenaries.Add(candidates[i]);
                mercenaryPrices.Add(150 + (i * 50));
                mercenaryHired.Add(false);
            }
        }

        private void SwitchTab(TabType tab)
        {
            currentTab = tab;
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (contentArea == null) return;
            foreach (Transform child in contentArea) Destroy(child.gameObject);

            // Clean up old LayoutGroups immediately
            var oldGrid = contentArea.GetComponent<GridLayoutGroup>();
            if (oldGrid != null) DestroyImmediate(oldGrid);

            var oldV = contentArea.GetComponent<VerticalLayoutGroup>();
            if (oldV != null) DestroyImmediate(oldV);

            if (currentTab == TabType.HireMercenary)
            {
                BuildHireMercenaryView();
            }
            else
            {
                BuildSkillChangeView();
            }

            if (popupPanel != null)
            {
                TMPFontManager.ApplyFontToAll(popupPanel.transform);
            }
        }

        private void BuildHireMercenaryView()
        {
            GridLayoutGroup grid = contentArea.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(460, 260);
            grid.spacing = new Vector2(30, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            for (int i = 0; i < availableMercenaries.Count; i++)
            {
                int index = i;
                CharacterData data = availableMercenaries[i];
                int price = mercenaryPrices[i];
                bool hired = mercenaryHired[i];

                GameObject cardObj = new GameObject($"MercCard_{i}");
                cardObj.transform.SetParent(contentArea, false);

                Image bg = cardObj.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

                VerticalLayoutGroup layout = cardObj.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(16, 16, 14, 14);
                layout.spacing = 10;

                string nameText = data != null ? data.DisplayName : "용병";
                CreateTextUI(cardObj.transform, $"👤 [{nameText}]", 24, Color.yellow);

                string statText = data != null ? $"체력: {data.maxHp} | 정신력: {data.maxMental}\n공격력: {data.baseAttack} | 방어력: {data.armor}" : "기본 능력치";
                CreateTextUI(cardObj.transform, statText, 16, Color.white);

                GameObject hireBtnObj = new GameObject("HireButton");
                hireBtnObj.transform.SetParent(cardObj.transform, false);
                LayoutElement bLe = hireBtnObj.AddComponent<LayoutElement>();
                bLe.preferredHeight = 44;

                Image bImg = hireBtnObj.AddComponent<Image>();
                bImg.color = hired ? Color.gray : new Color(0.2f, 0.65f, 0.3f, 1f);

                Button hireBtn = hireBtnObj.AddComponent<Button>();
                hireBtn.interactable = !hired;

                string btnLabel = hired ? "고용 완료" : $"💰 {price} G 영입";
                CreateTextUI(hireBtnObj.transform, btnLabel, 20, Color.white);

                hireBtn.onClick.AddListener(() =>
                {
                    int gold = RunManager.Instance != null ? RunManager.Instance.State.gold : 0;
                    if (gold < price)
                    {
                        NotificationManager.Instance?.ShowMessage("골드가 부족합니다!", Color.red);
                        return;
                    }

                    if (RunManager.Instance != null)
                    {
                        RunManager.Instance.State.gold -= price;
                        RunManager.Instance.AddPartyMember(data);
                    }

                    if (ResourceManager.Instance != null)
                    {
                        ResourceManager.Instance.AddCharacterCard(data.DataId, 1);
                    }

                    NotificationManager.Instance?.ShowMessage($"용병 [{nameText}] 영입 완료!", Color.cyan);
                    mercenaryHired[index] = true;
                    RefreshUI();
                });
            }
        }

        private void BuildSkillChangeView()
        {
            VerticalLayoutGroup vLayout = contentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(30, 30, 20, 20);
            vLayout.spacing = 15;

            CreateTextUI(contentArea, "📜 캐릭터를 선택하여 전투에 가져갈 2개의 액티브 스킬을 변경합니다.", 22, Color.yellow);

            var partyIDs = RunManager.Instance != null ? RunManager.Instance.State.partyDataIDs : new List<string>();
            var allChars = Resources.LoadAll<CharacterData>("Characters").ToList();
            if (allChars.Count == 0) allChars = Resources.LoadAll<CharacterData>("").ToList();

            List<CharacterData> partyDataList = new List<CharacterData>();
            foreach (var id in partyIDs)
            {
                var match = allChars.FirstOrDefault(c => c.DataId == id);
                if (match != null) partyDataList.Add(match);
            }

            if (partyDataList.Count == 0)
            {
                CreateTextUI(contentArea, "파티에 배치된 캐릭터가 없습니다.", 20, Color.gray);
                return;
            }

            // Party Member Selector Tabs
            GameObject pTabsObj = new GameObject("PartyTabs");
            pTabsObj.transform.SetParent(contentArea, false);
            HorizontalLayoutGroup pLayout = pTabsObj.AddComponent<HorizontalLayoutGroup>();
            pLayout.spacing = 15;

            if (selectedCharacter == null && partyDataList.Count > 0) selectedCharacter = partyDataList[0];

            foreach (var pData in partyDataList)
            {
                var charData = pData;
                GameObject btnObj = new GameObject($"CharTab_{charData.DisplayName}");
                btnObj.transform.SetParent(pTabsObj.transform, false);

                LayoutElement le = btnObj.AddComponent<LayoutElement>();
                le.preferredWidth = 160;
                le.preferredHeight = 40;

                Image img = btnObj.AddComponent<Image>();
                img.color = (selectedCharacter == charData) ? new Color(0.9f, 0.65f, 0.1f, 1f) : new Color(0.2f, 0.35f, 0.5f, 1f);

                Button btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    selectedCharacter = charData;
                    RefreshUI();
                });

                CreateTextUI(btnObj.transform, charData.DisplayName, 18, Color.white);
            }

            // Selected Character Active Skills Display & Reroll Button
            if (selectedCharacter != null)
            {
                CharacterStatus status = null;
                if (RunManager.Instance != null)
                {
                    status = RunManager.Instance.State.partyStatuses.FirstOrDefault(s => s.origin != null && s.origin.DataId == selectedCharacter.DataId);
                }
                if (status == null) status = new CharacterStatus(selectedCharacter);

                // Reroll Button (50 Gold)
                GameObject rerollBtnObj = new GameObject("RerollSkillsButton");
                rerollBtnObj.transform.SetParent(contentArea, false);
                LayoutElement rLe = rerollBtnObj.AddComponent<LayoutElement>();
                rLe.preferredHeight = 54;

                Image rImg = rerollBtnObj.AddComponent<Image>();
                rImg.color = new Color(0.85f, 0.45f, 0.1f, 1f);

                Button rBtn = rerollBtnObj.AddComponent<Button>();
                rBtn.onClick.AddListener(() =>
                {
                    int gold = RunManager.Instance != null ? RunManager.Instance.State.gold : 0;
                    if (gold < 50)
                    {
                        NotificationManager.Instance?.ShowMessage("스킬 변경 비용(50 Gold)이 부족합니다!", Color.red);
                        return;
                    }

                    if (RunManager.Instance != null) RunManager.Instance.State.gold -= 50;

                    List<int> pool = new List<int> { 0, 1, 2, 3 };
                    int idx1 = pool[UnityEngine.Random.Range(0, pool.Count)];
                    pool.Remove(idx1);
                    int idx2 = pool[UnityEngine.Random.Range(0, pool.Count)];
                    status.selectedActiveSkillIndices = new List<int> { idx1, idx2 };

                    NotificationManager.Instance?.ShowMessage($"🎲 [{selectedCharacter.DisplayName}]의 장착 스킬 2종이 무작위 변경되었습니다!", Color.green);
                    RefreshUI();
                });

                CreateTextUI(rerollBtnObj.transform, "🎲 스킬 2종 무작위 변경 (50 Gold)", 22, Color.white);

                GameObject skillsObj = new GameObject("SkillTogglesArea");
                skillsObj.transform.SetParent(contentArea, false);
                GridLayoutGroup sGrid = skillsObj.AddComponent<GridLayoutGroup>();
                sGrid.cellSize = new Vector2(400, 80);
                sGrid.spacing = new Vector2(20, 15);
                sGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                sGrid.constraintCount = 2;

                for (int i = 0; i < selectedCharacter.activeSkills.Length; i++)
                {
                    int skillIdx = i;
                    var skill = selectedCharacter.activeSkills[i];
                    if (skill == null) continue;

                    bool isSelected = status.selectedActiveSkillIndices.Contains(skillIdx);

                    GameObject sCard = new GameObject($"SkillCard_{i}");
                    sCard.transform.SetParent(skillsObj.transform, false);

                    Image sBg = sCard.AddComponent<Image>();
                    sBg.color = isSelected ? new Color(0.15f, 0.45f, 0.3f, 0.95f) : new Color(0.15f, 0.18f, 0.25f, 0.85f);

                    string skillLabel = $"{(isSelected ? "✓ [현재 장착]" : "  [미장착]")} {skill.skillName} (Cost: {skill.baseCost})";
                    CreateTextUI(sCard.transform, skillLabel, 18, isSelected ? Color.green : Color.gray);
                }
            }
        }

        private TextMeshProUGUI CreateTextUI(Transform parent, string text, int fontSize, Color color)
        {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(parent, false);
            RectTransform rect = txtObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.font = mainFont != null ? mainFont : TMPFontManager.MainKoreanFont;

            return tmp;
        }
    }
}
