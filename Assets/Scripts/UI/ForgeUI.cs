using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TheLastArk.Data;
using TheLastArk.Managers;

namespace TheLastArk.UI
{
    public class ForgeUI : MonoBehaviour
    {
        private GameObject popupPanel;
        private TMP_FontAsset mainFont;

        private enum TabType { BuyEquipment, Synthesize }
        private TabType currentTab = TabType.BuyEquipment;

        private Transform contentArea;

        // Buy Equipment Data
        private class ForgeItemSlot
        {
            public EquipmentData equipment;
            public int price;
            public bool sold;
        }
        private List<ForgeItemSlot> forgeSlots = new List<ForgeItemSlot>();

        // Synthesize Data
        private int selectedIndex1 = -1;
        private int selectedIndex2 = -1;

        public void Show()
        {
            if (popupPanel == null)
            {
                CreateUI();
            }

            GenerateForgeItems();
            SwitchTab(TabType.BuyEquipment);
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
            popupPanel = new GameObject("ForgePopup");
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

            // Title Header (대장간 - 장비 구매 & 합성)
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(popupPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.88f);
            titleRect.anchorMax = new Vector2(0.9f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            CreateTextUI(titleObj.transform, "대장간 (장비 구매 및 합성)", 36, Color.yellow);

            // Tabs Container (장비 구매 / 장비 합성)
            GameObject tabsObj = new GameObject("ForgeTabsArea");
            tabsObj.transform.SetParent(popupPanel.transform, false);
            RectTransform tabsRect = tabsObj.AddComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0.1f, 0.80f);
            tabsRect.anchorMax = new Vector2(0.9f, 0.86f);
            tabsRect.offsetMin = Vector2.zero;
            tabsRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup tabLayout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 20;

            CreateTabButton(tabsObj.transform, "장비 구매", () => SwitchTab(TabType.BuyEquipment));
            CreateTabButton(tabsObj.transform, "장비 합성", () => SwitchTab(TabType.Synthesize));

            // Content Area Container
            GameObject contentObj = new GameObject("ForgeContentArea");
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

        private void GenerateForgeItems()
        {
            forgeSlots.Clear();

            var all1Star = EquipmentDatabase.GetEquipmentsByStar(1);
            all1Star = all1Star.OrderBy(x => Random.value).ToList();

            int count = Mathf.Min(4, all1Star.Count);
            for (int i = 0; i < count; i++)
            {
                forgeSlots.Add(new ForgeItemSlot
                {
                    equipment = all1Star[i],
                    price = 50 + (i * 20),
                    sold = false
                });
            }
        }

        private void SwitchTab(TabType tab)
        {
            currentTab = tab;
            selectedIndex1 = -1;
            selectedIndex2 = -1;
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

            if (currentTab == TabType.BuyEquipment)
            {
                BuildBuyEquipmentView();
            }
            else
            {
                BuildSynthesizeView();
            }

            if (popupPanel != null)
            {
                TMPFontManager.ApplyFontToAll(popupPanel.transform);
            }
        }

        private void BuildBuyEquipmentView()
        {
            GridLayoutGroup grid = contentArea.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(360, 260);
            grid.spacing = new Vector2(30, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            for (int i = 0; i < forgeSlots.Count; i++)
            {
                int index = i;
                ForgeItemSlot slot = forgeSlots[i];

                GameObject cardObj = new GameObject($"EquipCard_{i}");
                cardObj.transform.SetParent(contentArea, false);

                Image bg = cardObj.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

                // Equipment Tooltip Hover Event
                AddTooltipTrigger(cardObj, slot.equipment);

                VerticalLayoutGroup layout = cardObj.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(16, 16, 14, 14);
                layout.spacing = 8;

                string starText = $"[{slot.equipment.starLevel}성] [{slot.equipment.category}]";
                CreateTextUI(cardObj.transform, starText, 16, Color.yellow);

                string nameText = slot.equipment != null ? slot.equipment.equipmentName : "강철 장비";
                CreateTextUI(cardObj.transform, nameText, 22, Color.cyan);

                string statText = GetEquipmentSummaryText(slot.equipment);
                CreateTextUI(cardObj.transform, statText, 15, Color.white);

                GameObject buyBtnObj = new GameObject("BuyButton");
                buyBtnObj.transform.SetParent(cardObj.transform, false);
                LayoutElement bLe = buyBtnObj.AddComponent<LayoutElement>();
                bLe.preferredHeight = 44;

                Image bImg = buyBtnObj.AddComponent<Image>();
                bImg.color = slot.sold ? Color.gray : new Color(0.2f, 0.65f, 0.3f, 1f);

                Button buyBtn = buyBtnObj.AddComponent<Button>();
                buyBtn.interactable = !slot.sold;

                string btnLabel = slot.sold ? "품절" : $"{slot.price} G 구매";
                CreateTextUI(buyBtnObj.transform, btnLabel, 18, Color.white);

                buyBtn.onClick.AddListener(() =>
                {
                    if (!ResourceManager.Instance.TrySpendGold(slot.price))
                    {
                        NotificationManager.Instance?.ShowMessage("골드가 부족합니다!", Color.red);
                        return;
                    }

                    if (slot.equipment != null) ResourceManager.Instance?.AddEquipment(slot.equipment);

                    NotificationManager.Instance?.ShowMessage($"장비 [{nameText}] 구매 완료!", Color.green);
                    slot.sold = true;
                    RefreshUI();
                });
            }
        }

        private string GetEquipmentSummaryText(EquipmentData eq)
        {
            if (eq == null) return "";
            List<string> stats = new List<string>();
            if (eq.bonusAttack > 0) stats.Add($"공격력 +{eq.bonusAttack}");
            if (eq.bonusSpellPower > 0) stats.Add($"주문력 +{eq.bonusSpellPower}");
            if (eq.bonusHp > 0) stats.Add($"체력 +{eq.bonusHp}");
            if (eq.bonusMental > 0) stats.Add($"정신력 +{eq.bonusMental}");
            if (eq.bonusArmor > 0) stats.Add($"방어력 +{eq.bonusArmor}");
            if (eq.bonusMagicResist > 0) stats.Add($"마법저항력 +{eq.bonusMagicResist}");
            if (eq.bonusCritRate > 0) stats.Add($"치명 +{eq.bonusCritRate}%");

            string str = string.Join("\n", stats);
            if (!string.IsNullOrEmpty(eq.passiveSkillName))
            {
                str += $"\n<color=gold>[{eq.passiveSkillName}]</color>";
            }
            return str;
        }

        private void BuildSynthesizeView()
        {
            VerticalLayoutGroup vLayout = contentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 15, 15);
            vLayout.spacing = 15;

            GameObject tipObj = new GameObject("SynthTipText");
            tipObj.transform.SetParent(contentArea, false);
            LayoutElement tipLe = tipObj.AddComponent<LayoutElement>();
            tipLe.preferredHeight = 30;
            CreateTextUI(tipObj.transform, "동일/같은 계열 장비 2개를 선택하여 50% 확률로 상위 계열 장비 1종으로 무작위 합성합니다.", 20, Color.yellow);

            var inventoryEquips = ResourceManager.Instance != null ? ResourceManager.Instance.Equipments : new List<EquipmentData>();

            if (inventoryEquips == null || inventoryEquips.Count < 2)
            {
                GameObject emptyObj = new GameObject("EmptyText");
                emptyObj.transform.SetParent(contentArea, false);
                LayoutElement empLe = emptyObj.AddComponent<LayoutElement>();
                empLe.preferredHeight = 40;
                CreateTextUI(emptyObj.transform, "합성에 필요한 장비가 2개 이상 필요합니다. (대장간에서 1성 장비를 구매해 보세요!)", 20, Color.gray);
                return;
            }

            // Index bounds check for selections
            if (selectedIndex1 >= inventoryEquips.Count) selectedIndex1 = -1;
            if (selectedIndex2 >= inventoryEquips.Count) selectedIndex2 = -1;

            EquipmentData eq1 = (selectedIndex1 >= 0 && selectedIndex1 < inventoryEquips.Count) ? inventoryEquips[selectedIndex1] : null;
            EquipmentData eq2 = (selectedIndex2 >= 0 && selectedIndex2 < inventoryEquips.Count) ? inventoryEquips[selectedIndex2] : null;

            // Inventory Equipment Grid Container
            GameObject equipsObj = new GameObject("EquipListArea");
            equipsObj.transform.SetParent(contentArea, false);
            LayoutElement eqLe = equipsObj.AddComponent<LayoutElement>();
            eqLe.preferredHeight = 320;

            GridLayoutGroup eGrid = equipsObj.AddComponent<GridLayoutGroup>();
            eGrid.cellSize = new Vector2(240, 95);
            eGrid.spacing = new Vector2(15, 15);
            eGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            eGrid.constraintCount = 4;

            for (int i = 0; i < inventoryEquips.Count; i++)
            {
                int itemIndex = i;
                var eq = inventoryEquips[itemIndex];
                if (eq == null) continue;

                bool isSelected = (selectedIndex1 == itemIndex || selectedIndex2 == itemIndex);

                GameObject cardObj = new GameObject($"InvEquip_{itemIndex}");
                cardObj.transform.SetParent(equipsObj.transform, false);

                Image bg = cardObj.AddComponent<Image>();
                bg.color = isSelected ? new Color(0.2f, 0.65f, 0.45f, 0.95f) : new Color(0.15f, 0.18f, 0.25f, 0.85f);

                // Tooltip trigger
                AddTooltipTrigger(cardObj, eq);

                Button btn = cardObj.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    if (selectedIndex1 == itemIndex) selectedIndex1 = -1;
                    else if (selectedIndex2 == itemIndex) selectedIndex2 = -1;
                    else if (selectedIndex1 == -1) selectedIndex1 = itemIndex;
                    else if (selectedIndex2 == -1) selectedIndex2 = itemIndex;
                    else selectedIndex1 = itemIndex;

                    RefreshUI();
                });

                string label = $"{(isSelected ? "[선택] " : "")}[{eq.starLevel}성] {eq.equipmentName}\n<size=15><color=gray>({eq.category})</color></size>";
                CreateTextUI(cardObj.transform, label, 18, isSelected ? Color.green : Color.white);
            }

            // Status Banner
            GameObject statusObj = new GameObject("SynthStatusText");
            statusObj.transform.SetParent(contentArea, false);
            LayoutElement stLe = statusObj.AddComponent<LayoutElement>();
            stLe.preferredHeight = 40;

            string synthStatusStr = $"선택 1: <color=#FFD700>{(eq1 != null ? eq1.equipmentName : "미선택")}</color>   +   선택 2: <color=#FFD700>{(eq2 != null ? eq2.equipmentName : "미선택")}</color>";
            CreateTextUI(statusObj.transform, synthStatusStr, 22, Color.cyan);

            // Execute Synthesis Button
            GameObject doSynthBtnObj = new GameObject("DoSynthButton");
            doSynthBtnObj.transform.SetParent(contentArea, false);
            LayoutElement bLe = doSynthBtnObj.AddComponent<LayoutElement>();
            bLe.preferredHeight = 56;

            bool canSynthesize = (eq1 != null && eq2 != null && selectedIndex1 != selectedIndex2);

            Image bImg = doSynthBtnObj.AddComponent<Image>();
            bImg.color = canSynthesize ? new Color(0.85f, 0.45f, 0.1f, 1f) : new Color(0.3f, 0.3f, 0.35f, 0.7f);

            Button doSynthBtn = doSynthBtnObj.AddComponent<Button>();
            doSynthBtn.interactable = canSynthesize;

            CreateTextUI(doSynthBtnObj.transform, "장비 합성 시작 (50 Gold)", 24, Color.white);
            doSynthBtn.onClick.AddListener(ExecuteSynthesis);
        }

        private void ExecuteSynthesis()
        {
            var inventoryEquips = ResourceManager.Instance != null ? ResourceManager.Instance.Equipments : null;
            if (inventoryEquips == null) return;
            if (selectedIndex1 < 0 || selectedIndex1 >= inventoryEquips.Count) return;
            if (selectedIndex2 < 0 || selectedIndex2 >= inventoryEquips.Count) return;
            if (selectedIndex1 == selectedIndex2) return;

            EquipmentData eq1 = inventoryEquips[selectedIndex1];
            EquipmentData eq2 = inventoryEquips[selectedIndex2];
            if (eq1 == null || eq2 == null) return;

            // Synthesize Equipment
            EquipmentData result = EquipmentDatabase.SynthesizeEquipments(eq1, eq2);
            if (result == null)
            {
                NotificationManager.Instance?.ShowMessage("더 이상 합성할 수 없는 장비 조합입니다.", Color.yellow);
                return;
            }

            if (!ResourceManager.Instance.TrySpendGold(50))
            {
                NotificationManager.Instance?.ShowMessage("합성 비용(50 Gold)이 부족합니다!", Color.red);
                return;
            }

            if (ResourceManager.Instance != null)
            {
                int firstRemove = Mathf.Max(selectedIndex1, selectedIndex2);
                int secondRemove = Mathf.Min(selectedIndex1, selectedIndex2);
                ResourceManager.Instance.Equipments.RemoveAt(firstRemove);
                ResourceManager.Instance.Equipments.RemoveAt(secondRemove);
                ResourceManager.Instance.Equipments.Add(result);
            }

            NotificationManager.Instance?.ShowMessage($"장비 합성 성공! [{result.starLevel}성] [{result.equipmentName}] 획득!", Color.cyan);

            selectedIndex1 = -1;
            selectedIndex2 = -1;
            RefreshUI();
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

        private void AddTooltipTrigger(GameObject obj, EquipmentData eq)
        {
            if (obj == null || eq == null) return;
            var trigger = obj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = obj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((_) =>
            {
                if (EquipmentTooltipUI.Instance != null)
                {
                    EquipmentTooltipUI.Instance.ShowTooltip(eq);
                }
            });
            trigger.triggers.Add(entryEnter);

            var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            entryExit.callback.AddListener((_) =>
            {
                if (EquipmentTooltipUI.Instance != null)
                {
                    EquipmentTooltipUI.Instance.HideTooltip();
                }
            });
            trigger.triggers.Add(entryExit);
        }
    }
}
