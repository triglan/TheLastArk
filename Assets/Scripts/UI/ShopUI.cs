using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TheLastArk.Data;
using TheLastArk.Managers;

namespace TheLastArk.UI
{
    public class ShopUI : MonoBehaviour
    {
        private GameObject popupPanel;
        private TMPro.TMP_FontAsset mainFont;

        private Transform contentArea;
        private Transform tabsArea;
        private Button refreshBtn;
        private TextMeshProUGUI refreshBtnText;

        private enum TabType { Consumable, Relic, Equipment }
        private TabType currentTab = TabType.Consumable;

        // Data for current items
        private List<ConsumableData> currentConsumables = new List<ConsumableData>();
        private List<int> consumablePrices = new List<int>();
        private List<bool> consumableSold = new List<bool>();
        private int consumableRefreshes = 1;

        private List<RelicData> currentRelics = new List<RelicData>();
        private List<int> relicPrices = new List<int>();
        private List<bool> relicSold = new List<bool>();
        private int relicRefreshes = 1;

        private int equipmentRefreshes = 1;

        public void Show()
        {
            if (popupPanel == null)
            {
                CreateUI();
            }

            int extraRefresh = 0;
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.ExtraRefresh))
            {
                extraRefresh = 1;
            }

            consumableRefreshes = 1 + extraRefresh;
            relicRefreshes = 1 + extraRefresh;
            equipmentRefreshes = 1 + extraRefresh;

            GenerateConsumables();
            GenerateRelics();
            
            SwitchTab(TabType.Consumable);
            popupPanel.SetActive(true);
        }

        public void Hide()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
        }

        private void CreateUI()
        {
            mainFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/Main_Fonts");

            Canvas canvas = FindObjectOfType<Canvas>();
            popupPanel = new GameObject("ShopPopup");
            popupPanel.transform.SetParent(canvas.transform, false);
            popupPanel.transform.SetAsLastSibling();

            RectTransform rect = popupPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = popupPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.95f);

            // 닫기 버튼
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(popupPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-30, -30);
            closeRect.sizeDelta = new Vector2(100, 50);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);

            CreateTextUI(closeBtnObj.transform, "닫기", 24, Color.white, Vector2.zero, Vector2.zero, Vector2.one);

            // Title
            CreateTextUI(popupPanel.transform, "상점가", 40, Color.white, new Vector2(0, -60), new Vector2(0.5f, 1), new Vector2(0.5f, 1)).rectTransform.sizeDelta = new Vector2(400, 60);

            // Tabs Area
            GameObject tabsObj = new GameObject("TabsArea");
            tabsObj.transform.SetParent(popupPanel.transform, false);
            tabsArea = tabsObj.AddComponent<RectTransform>();
            RectTransform tabsRect = (RectTransform)tabsArea;
            tabsRect.anchorMin = new Vector2(0.1f, 0.8f);
            tabsRect.anchorMax = new Vector2(0.9f, 0.9f);
            tabsRect.offsetMin = Vector2.zero;
            tabsRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup tLayout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            tLayout.spacing = 20;
            tLayout.childControlWidth = true;
            tLayout.childForceExpandWidth = true;

            CreateTabButton(tabsArea, "잡화점 (소모품)", TabType.Consumable);
            CreateTabButton(tabsArea, "고고학자 (유물)", TabType.Relic);
            CreateTabButton(tabsArea, "대장간 (장비)", TabType.Equipment);

            // Refresh Button
            GameObject refBtnObj = new GameObject("RefreshButton");
            refBtnObj.transform.SetParent(popupPanel.transform, false);
            RectTransform refRect = refBtnObj.AddComponent<RectTransform>();
            refRect.anchorMin = new Vector2(0.5f, 0.1f);
            refRect.anchorMax = new Vector2(0.5f, 0.1f);
            refRect.pivot = new Vector2(0.5f, 0);
            refRect.anchoredPosition = new Vector2(0, 0);
            refRect.sizeDelta = new Vector2(300, 60);

            Image refImg = refBtnObj.AddComponent<Image>();
            refImg.color = new Color(0.2f, 0.4f, 0.8f, 1f);

            refreshBtn = refBtnObj.AddComponent<Button>();
            refreshBtn.onClick.AddListener(OnRefreshClicked);

            refreshBtnText = CreateTextUI(refBtnObj.transform, "새로고침", 28, Color.white, Vector2.zero, Vector2.zero, Vector2.one);

            // Content Area
            GameObject contentObj = new GameObject("ContentArea");
            contentObj.transform.SetParent(popupPanel.transform, false);
            contentArea = contentObj.AddComponent<RectTransform>();
            RectTransform cRect = (RectTransform)contentArea;
            cRect.anchorMin = new Vector2(0.1f, 0.2f);
            cRect.anchorMax = new Vector2(0.9f, 0.75f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup cLayout = contentObj.AddComponent<HorizontalLayoutGroup>();
            cLayout.spacing = 40;
            cLayout.childControlWidth = true;
            cLayout.childForceExpandWidth = true;
        }

        private void CreateTabButton(Transform parent, string text, TabType type)
        {
            GameObject btnObj = new GameObject($"Tab_{type}");
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SwitchTab(type));

            CreateTextUI(btnObj.transform, text, 28, Color.white, Vector2.zero, Vector2.zero, Vector2.one);
        }

        private void SwitchTab(TabType type)
        {
            currentTab = type;
            RenderContent();
            UpdateRefreshButton();
        }

        private void UpdateRefreshButton()
        {
            int left = 0;
            if (currentTab == TabType.Consumable) left = consumableRefreshes;
            else if (currentTab == TabType.Relic) left = relicRefreshes;
            else if (currentTab == TabType.Equipment) left = equipmentRefreshes;

            refreshBtnText.text = $"새로고침 (남은 횟수: {left})";
            refreshBtn.interactable = (left > 0);
            refreshBtn.GetComponent<Image>().color = left > 0 ? new Color(0.2f, 0.4f, 0.8f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f);
        }

        private void OnRefreshClicked()
        {
            if (currentTab == TabType.Consumable && consumableRefreshes > 0)
            {
                consumableRefreshes--;
                GenerateConsumables();
            }
            else if (currentTab == TabType.Relic && relicRefreshes > 0)
            {
                relicRefreshes--;
                GenerateRelics();
            }
            else if (currentTab == TabType.Equipment && equipmentRefreshes > 0)
            {
                equipmentRefreshes--;
                // GenerateEquipments(); // no-op
            }
            
            RenderContent();
            UpdateRefreshButton();
        }

        private void RenderContent()
        {
            foreach (Transform child in contentArea) Destroy(child.gameObject);

            if (currentTab == TabType.Consumable)
            {
                for (int i = 0; i < currentConsumables.Count; i++)
                {
                    CreateItemSlot(currentConsumables[i].consumableName, currentConsumables[i].description, currentConsumables[i].icon, consumablePrices[i], consumableSold[i], i);
                }
            }
            else if (currentTab == TabType.Relic)
            {
                for (int i = 0; i < currentRelics.Count; i++)
                {
                    CreateItemSlot(currentRelics[i].relicName, currentRelics[i].description, currentRelics[i].icon, relicPrices[i], relicSold[i], i);
                }
            }
            else if (currentTab == TabType.Equipment)
            {
                for (int i = 0; i < 3; i++)
                {
                    CreateDummySlot();
                }
            }
        }

        private void CreateItemSlot(string name, string desc, Sprite icon, int price, bool isSold, int index)
        {
            GameObject slotObj = new GameObject($"Slot_{index}");
            slotObj.transform.SetParent(contentArea, false);

            Image bg = slotObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.7f);
            iconRect.anchorMax = new Vector2(0.5f, 0.95f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.sizeDelta = new Vector2(0, 0); // use anchors
            iconRect.offsetMin = new Vector2(-60, 0);
            iconRect.offsetMax = new Vector2(60, 0);
            Image img = iconObj.AddComponent<Image>();
            if (icon != null) img.sprite = icon;
            else img.color = Color.gray;
            img.preserveAspect = true;

            // Name
            CreateTextUI(slotObj.transform, name, 28, Color.yellow, Vector2.zero, new Vector2(0, 0.55f), new Vector2(1, 0.65f));

            // Desc
            TextMeshProUGUI descTmp = CreateTextUI(slotObj.transform, desc, 20, Color.white, Vector2.zero, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.5f));
            descTmp.alignment = TextAlignmentOptions.Top;

            // Buy Button
            GameObject buyBtnObj = new GameObject("BuyButton");
            buyBtnObj.transform.SetParent(slotObj.transform, false);
            RectTransform buyRect = buyBtnObj.AddComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.1f, 0.05f);
            buyRect.anchorMax = new Vector2(0.9f, 0.15f);
            buyRect.offsetMin = Vector2.zero;
            buyRect.offsetMax = Vector2.zero;

            Image buyImg = buyBtnObj.AddComponent<Image>();
            Button buyBtn = buyBtnObj.AddComponent<Button>();

            TextMeshProUGUI buyTxt = CreateTextUI(buyBtnObj.transform, isSold ? "SOLD OUT" : $"{price} G", 28, Color.white, Vector2.zero, Vector2.zero, Vector2.one);

            if (isSold)
            {
                buyImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                buyBtn.interactable = false;
            }
            else
            {
                buyImg.color = new Color(0.2f, 0.6f, 0.2f, 1f);
                buyBtn.onClick.AddListener(() => OnBuyClicked(index, price));
            }
        }

        private void CreateDummySlot()
        {
            GameObject slotObj = new GameObject("Slot_Dummy");
            slotObj.transform.SetParent(contentArea, false);

            Image bg = slotObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            CreateTextUI(slotObj.transform, "?", 80, Color.gray, Vector2.zero, new Vector2(0, 0.5f), new Vector2(1, 1));
            CreateTextUI(slotObj.transform, "장비 시스템\n준비 중...", 28, Color.white, Vector2.zero, new Vector2(0, 0), new Vector2(1, 0.5f));
        }

        private void GenerateConsumables()
        {
            currentConsumables.Clear();
            consumablePrices.Clear();
            consumableSold.Clear();

            var all = Resources.LoadAll<ConsumableData>("Consumables").ToList();
            Shuffle(all);

            float discount = 0f;
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.ShopDiscount))
            {
                discount = 0.3f;
            }

            for (int i = 0; i < 3 && i < all.Count; i++)
            {
                currentConsumables.Add(all[i]);
                int basePrice = Random.Range(30, 71); // 30~70 Gold
                int finalPrice = Mathf.RoundToInt(basePrice * (1f - discount));
                consumablePrices.Add(finalPrice);
                consumableSold.Add(false);
            }
        }

        private void GenerateRelics()
        {
            currentRelics.Clear();
            relicPrices.Clear();
            relicSold.Clear();

            var all = Resources.LoadAll<RelicData>("Relics").ToList();
            
            // Remove already owned relics
            if (ResourceManager.Instance != null)
            {
                all.RemoveAll(r => ResourceManager.Instance.HasRelic(r.relicID));
            }

            // Separate pools by rarity
            var commonRelics = all.Where(r => r.rarity == RelicRarity.Common).ToList();
            var legendaryRelics = all.Where(r => r.rarity == RelicRarity.Legendary).ToList();

            Shuffle(commonRelics);
            Shuffle(legendaryRelics);

            float discount = 0f;
            bool forceFirstLegendary = false;

            if (ResourceManager.Instance != null)
            {
                if (ResourceManager.Instance.HasRelicEffect(RelicEffectType.ShopDiscount)) discount = 0.3f;
                if (ResourceManager.Instance.HasRelicEffect(RelicEffectType.ShopFirstLegendary)) forceFirstLegendary = true;
            }

            for (int i = 0; i < 3; i++)
            {
                RelicData selectedRelic = null;

                if (i == 0 && forceFirstLegendary && legendaryRelics.Count > 0)
                {
                    selectedRelic = legendaryRelics[0];
                    legendaryRelics.RemoveAt(0);
                }
                else if (commonRelics.Count > 0)
                {
                    selectedRelic = commonRelics[0];
                    commonRelics.RemoveAt(0);
                }

                if (selectedRelic != null)
                {
                    currentRelics.Add(selectedRelic);
                    int basePrice = Random.Range(120, 171); // 120~170 Gold
                    int finalPrice = Mathf.RoundToInt(basePrice * (1f - discount));
                    relicPrices.Add(finalPrice);
                    relicSold.Add(false);
                }
            }
        }

        private void OnBuyClicked(int index, int price)
        {
            if (ResourceManager.Instance == null) return;

            if (ResourceManager.Instance.SpendGold(price))
            {
                if (currentTab == TabType.Consumable)
                {
                    if (ResourceManager.Instance.AddConsumable(currentConsumables[index]))
                    {
                        consumableSold[index] = true;
                    }
                    else
                    {
                        // Inventory full, refund gold
                        ResourceManager.Instance.AddGold(price);
                        Debug.LogWarning("소모품 인벤토리가 가득 찼습니다.");
                        return;
                    }
                }
                else if (currentTab == TabType.Relic)
                {
                    ResourceManager.Instance.AddRelic(currentRelics[index]);
                    relicSold[index] = true;
                }

                RenderContent();
            }
            else
            {
                Debug.LogWarning("골드가 부족합니다!");
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private TextMeshProUGUI CreateTextUI(Transform parent, string text, int fontSize, Color color, Vector2 anchoredPos, Vector2 minA, Vector2 maxA)
        {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(parent, false);
            RectTransform rect = txtObj.AddComponent<RectTransform>();
            rect.anchorMin = minA;
            rect.anchorMax = maxA;
            rect.anchoredPosition = anchoredPos;
            
            if (minA != maxA)
                rect.sizeDelta = Vector2.zero;
            else
                rect.sizeDelta = new Vector2(300, 50);
            
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            if (mainFont != null) tmp.font = mainFont;

            return tmp;
        }
    }
}
