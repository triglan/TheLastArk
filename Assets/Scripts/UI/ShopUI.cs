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
        private TMP_FontAsset mainFont;

        private Transform contentArea;
        private Button refreshBtn;
        private TextMeshProUGUI refreshBtnText;

        // Unified 6 Shop Items (Consumables + Relics)
        private class ShopItemSlot
        {
            public bool isRelic; // true: Relic, false: Consumable
            public ConsumableData consumable;
            public RelicData relic;
            public int price;
            public bool sold;
        }

        private List<ShopItemSlot> currentShopSlots = new List<ShopItemSlot>();
        private int remainingRefreshes = 1;

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

            remainingRefreshes = 1 + extraRefresh;
            GenerateShopItems();
            RefreshUI();
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
            popupPanel = new GameObject("ShopPopup");
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

            // Title Header (상점가 - 소모품 & 유물 거래)
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(popupPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.88f);
            titleRect.anchorMax = new Vector2(0.9f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            CreateTextUI(titleObj.transform, "상점가 (소모품 & 유물 거래)", 36, Color.yellow);

            // Refresh Items Button
            GameObject refreshBtnObj = new GameObject("RefreshButton");
            refreshBtnObj.transform.SetParent(popupPanel.transform, false);
            RectTransform rRect = refreshBtnObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0.7f, 0.88f);
            rRect.anchorMax = new Vector2(0.85f, 0.94f);
            rRect.offsetMin = Vector2.zero;
            rRect.offsetMax = Vector2.zero;

            Image rImg = refreshBtnObj.AddComponent<Image>();
            rImg.color = new Color(0.2f, 0.5f, 0.8f, 1f);

            refreshBtn = refreshBtnObj.AddComponent<Button>();
            refreshBtn.onClick.AddListener(OnClickRefresh);

            refreshBtnText = CreateTextUI(refreshBtnObj.transform, "상품 새로고침", 20, Color.white);

            // 6-Item Grid Content Container
            GameObject gridObj = new GameObject("ShopGridArea");
            gridObj.transform.SetParent(popupPanel.transform, false);
            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.1f, 0.1f);
            gridRect.anchorMax = new Vector2(0.9f, 0.82f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(480, 240);
            grid.spacing = new Vector2(40, 30);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3; // 3 Columns x 2 Rows = Total 6 Items
            grid.childAlignment = TextAnchor.MiddleCenter;

            contentArea = gridObj.transform;
        }

        private void GenerateShopItems()
        {
            currentShopSlots.Clear();

            // 1. Generate 3 Consumables
            var allConsumables = Resources.LoadAll<ConsumableData>("Consumables").ToList();
            if (allConsumables.Count == 0)
            {
                allConsumables = Resources.LoadAll<ConsumableData>("").ToList();
            }

            float shopDiscount = 0f;
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.ShopDiscount))
            {
                shopDiscount = ResourceManager.Instance.GetRelicBonus(RelicEffectType.ShopDiscount);
            }

            allConsumables = allConsumables.OrderBy(x => Random.value).ToList();
            int cCount = Mathf.Min(3, allConsumables.Count);
            for (int i = 0; i < cCount; i++)
            {
                int basePrice = 50 + (i * 10);
                currentShopSlots.Add(new ShopItemSlot
                {
                    isRelic = false,
                    consumable = allConsumables[i],
                    price = Mathf.RoundToInt(basePrice * (1f - shopDiscount)),
                    sold = false
                });
            }

            // 2. Generate 3 Relics
            var allRelics = Resources.LoadAll<RelicData>("Relics").ToList();
            if (allRelics.Count == 0)
            {
                allRelics = Resources.LoadAll<RelicData>("").ToList();
            }

            bool hasFirstLegendaryRelic = ResourceManager.Instance != null &&
                                          ResourceManager.Instance.HasRelicEffect(RelicEffectType.ShopFirstLegendary);

            List<RelicData> unownedLegendary = allRelics
                .Where(r => r != null && r.rarity == RelicRarity.Legendary && (ResourceManager.Instance == null || !ResourceManager.Instance.HasRelic(r.relicID)))
                .OrderBy(x => Random.value)
                .ToList();

            List<RelicData> unownedCommon = allRelics
                .Where(r => r != null && r.rarity == RelicRarity.Common && (ResourceManager.Instance == null || !ResourceManager.Instance.HasRelic(r.relicID)))
                .OrderBy(x => Random.value)
                .ToList();

            List<RelicData> chosenRelics = new List<RelicData>();

            for (int i = 0; i < 3; i++)
            {
                RelicData picked = null;
                if (i == 0 && hasFirstLegendaryRelic)
                {
                    // [고고학 투자 증서]: 1번째 유물은 항상 전설 유물 (남은 전설 유물이 없으면 일반 유물)
                    if (unownedLegendary.Count > 0)
                    {
                        picked = unownedLegendary[0];
                        unownedLegendary.RemoveAt(0);
                    }
                    else if (unownedCommon.Count > 0)
                    {
                        picked = unownedCommon[0];
                        unownedCommon.RemoveAt(0);
                    }
                }
                else
                {
                    if (unownedCommon.Count > 0)
                    {
                        picked = unownedCommon[0];
                        unownedCommon.RemoveAt(0);
                    }
                    else if (unownedLegendary.Count > 0)
                    {
                        picked = unownedLegendary[0];
                        unownedLegendary.RemoveAt(0);
                    }
                }

                if (picked != null)
                {
                    chosenRelics.Add(picked);
                }
            }

            for (int i = 0; i < chosenRelics.Count; i++)
            {
                var r = chosenRelics[i];
                int basePrice = r.rarity == RelicRarity.Legendary ? 200 + (i * 30) : 100 + (i * 25);
                currentShopSlots.Add(new ShopItemSlot
                {
                    isRelic = true,
                    relic = r,
                    price = Mathf.RoundToInt(basePrice * (1f - shopDiscount)),
                    sold = false
                });
            }
        }

        private void OnClickRefresh()
        {
            if (remainingRefreshes > 0)
            {
                remainingRefreshes--;
                GenerateShopItems();
                RefreshUI();
            }
            else
            {
                // Buy refresh with gold (50 Gold)
                if (ResourceManager.Instance.TrySpendGold(50))
                {
                    GenerateShopItems();
                    RefreshUI();
                }
                else
                {
                    NotificationManager.Instance?.ShowMessage("새로고침 골드(50G)가 부족합니다.", Color.red);
                }
            }
        }

        private void RefreshUI()
        {
            if (contentArea == null) return;

            // Clear old UI slots
            foreach (Transform child in contentArea)
            {
                Destroy(child.gameObject);
            }

            // Update refresh button text
            if (refreshBtnText != null)
            {
                refreshBtnText.text = remainingRefreshes > 0 ? $"무료 리롤 ({remainingRefreshes})" : "리롤 (50 Gold)";
            }

            // Render 6 Shop Item Cards
            for (int i = 0; i < currentShopSlots.Count; i++)
            {
                int index = i;
                ShopItemSlot slot = currentShopSlots[i];

                GameObject cardObj = new GameObject($"ShopCard_{i}");
                cardObj.transform.SetParent(contentArea, false);

                Image bg = cardObj.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

                VerticalLayoutGroup layout = cardObj.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(16, 16, 14, 14);
                layout.spacing = 10;
                layout.childControlHeight = false;

                // Category Tag (소모품 or 유물)
                string catTag = slot.isRelic ? "[유물]" : "[소모품]";
                Color catColor = slot.isRelic ? new Color(1f, 0.85f, 0.3f) : new Color(0.3f, 0.85f, 1f);
                CreateTextUI(cardObj.transform, catTag, 18, catColor);

                // Item Name
                string nameText = slot.isRelic ? (slot.relic != null ? slot.relic.relicName : "유물") : (slot.consumable != null ? slot.consumable.consumableName : "소모품");
                CreateTextUI(cardObj.transform, nameText, 24, Color.white);

                // Item Description
                string descText = slot.isRelic ? (slot.relic != null ? slot.relic.description : "유물 효과") : (slot.consumable != null ? slot.consumable.description : "소모품 효과");
                CreateTextUI(cardObj.transform, descText, 15, Color.gray);

                // Buy / Sold Out Button
                GameObject buyBtnObj = new GameObject("BuyButton");
                buyBtnObj.transform.SetParent(cardObj.transform, false);
                LayoutElement bLe = buyBtnObj.AddComponent<LayoutElement>();
                bLe.preferredHeight = 44;

                Image bImg = buyBtnObj.AddComponent<Image>();
                bImg.color = slot.sold ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.2f, 0.65f, 0.3f, 1f);

                Button buyBtn = buyBtnObj.AddComponent<Button>();
                buyBtn.interactable = !slot.sold;

                string btnLabel = slot.sold ? "품절" : $"{slot.price} G 구매";
                CreateTextUI(buyBtnObj.transform, btnLabel, 20, Color.white);

                buyBtn.onClick.AddListener(() => OnClickBuyItem(index));
            }
        }

        private void OnClickBuyItem(int index)
        {
            if (index < 0 || index >= currentShopSlots.Count) return;
            ShopItemSlot slot = currentShopSlots[index];
            if (slot.sold) return;

            if (!ResourceManager.Instance.TrySpendGold(slot.price))
            {
                NotificationManager.Instance?.ShowMessage("골드가 부족합니다!", Color.red);
                return;
            }

            // Grant Item
            if (slot.isRelic && slot.relic != null)
            {
                ResourceManager.Instance?.AddRelic(slot.relic.relicID);
                NotificationManager.Instance?.ShowMessage($"유물 [{slot.relic.relicName}] 획득!", Color.cyan);
            }
            else if (!slot.isRelic && slot.consumable != null)
            {
                ResourceManager.Instance?.AddConsumable(slot.consumable);
                NotificationManager.Instance?.ShowMessage($"소모품 [{slot.consumable.consumableName}] 획득!", Color.green);
            }

            slot.sold = true;
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
    }
}
