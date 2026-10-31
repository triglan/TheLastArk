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
        private TextMeshProUGUI balanceText;
        private TextMeshProUGUI capacityText;

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
        private ResourceManager observedResources;

        private void RefreshVisibleShop()
        {
            if (popupPanel != null && popupPanel.activeInHierarchy) RefreshUI();
        }

        private void OnDestroy()
        {
            if (observedResources == null) return;
            observedResources.OnGoldChanged -= RefreshVisibleShop;
            observedResources.OnConsumablesChanged -= RefreshVisibleShop;
            observedResources.OnRelicsChanged -= RefreshVisibleShop;
        }

        public void Show()
        {
            if (observedResources == null)
            {
                observedResources = ResourceManager.Instance;
                observedResources.OnGoldChanged += RefreshVisibleShop;
                observedResources.OnConsumablesChanged += RefreshVisibleShop;
                observedResources.OnRelicsChanged += RefreshVisibleShop;
            }
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
            popupPanel.transform.SetAsLastSibling();
            popupPanel.SetActive(true);
        }

        public void Hide()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
        }

        private void CreateUI()
        {
            popupPanel = ManagementDialog.Create(HudCanvasUtility.FindOverlayCanvas(), "ShopPopup", "마을 상점", "TRADING POST  /  소모품 · 유물", Hide, 0.08f, out var window);
            var toolbar = ManagementDialog.Toolbar(window);
            balanceText = ManagementTheme.Label(toolbar, "", 25, ManagementTheme.Gold);
            ManagementTheme.Place(balanceText.rectTransform, Vector2.zero, new Vector2(0.28f, 1), new Vector2(18, 0), Vector2.zero);
            capacityText = ManagementTheme.Label(toolbar, "", 19, ManagementTheme.Muted);
            ManagementTheme.Place(capacityText.rectTransform, new Vector2(0.28f, 0), new Vector2(0.7f, 1));
            refreshBtn = ManagementTheme.Button(toolbar, "RefreshButton", "", OnClickRefresh);
            ManagementTheme.Place((RectTransform)refreshBtn.transform, new Vector2(1, 0), Vector2.one, new Vector2(-300, 7), new Vector2(-8, -7));
            refreshBtnText = refreshBtn.GetComponentInChildren<TextMeshProUGUI>();
            var body = ManagementDialog.Body(window);
            ManagementTheme.Scroll(body, "ShopGridArea", out var content);
            content.gameObject.AddComponent<GridLayoutGroup>();
            content.gameObject.AddComponent<ManagementGridLayout>().Configure(380, 310, 3);
            contentArea = content;
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
                .Where(r => r != null && r.rarity == RelicRarity.Legendary && (ResourceManager.Instance == null || ResourceManager.Instance.CanAcquireRelic(r)))
                .OrderBy(x => Random.value)
                .ToList();

            List<RelicData> unownedCommon = allRelics
                .Where(r => r != null && r.rarity == RelicRarity.Common && (ResourceManager.Instance == null || ResourceManager.Instance.CanAcquireRelic(r)))
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
            foreach (Transform child in contentArea)
            {
                child.gameObject.SetActive(false); Destroy(child.gameObject);
            }
            var resources = ResourceManager.Instance;
            balanceText.text = $"보유 골드   {resources.Gold:N0} G";
            capacityText.text = $"소모품  {resources.Consumables.Count} / {ResourceManager.ConsumableCapacity}   ·   유물  {resources.Relics.Count}";
            refreshBtnText.text = remainingRefreshes > 0 ? $"상품 교체 · 무료 {remainingRefreshes}회" : "상품 교체 · 50 G";
            refreshBtn.interactable = remainingRefreshes > 0 || resources.CanAfford(50);
            for (int i = 0; i < currentShopSlots.Count; i++)
            {
                int index = i;
                var slot = currentShopSlots[i];
                string unavailable;
                if (slot.isRelic) resources.CanPurchaseRelic(slot.relic, slot.price, out unavailable);
                else resources.CanPurchaseConsumable(slot.consumable, slot.price, out unavailable);
                string name = slot.isRelic ? slot.relic.relicName : slot.consumable.consumableName;
                string description = slot.isRelic ? slot.relic.description : slot.consumable.description;
                string tag = slot.isRelic ? (slot.relic.rarity == RelicRarity.Legendary ? "전설 유물" : "유물") : "소모품";
                ManagementDialog.Product(contentArea, $"ShopCard_{i}", slot.isRelic ? InventoryCategory.Relic : InventoryCategory.Consumable,
                    tag, name, description, slot.isRelic ? slot.relic.icon : slot.consumable.icon,
                    slot.price, slot.sold, unavailable, () => OnClickBuyItem(index));
            }
            if (currentShopSlots.Count == 0)
                ManagementTheme.Label(contentArea, "지금 판매 중인 상품이 없습니다.", 24, ManagementTheme.Muted);
        }

        private void OnClickBuyItem(int index)
        {
            if (index < 0 || index >= currentShopSlots.Count) return;
            var slot = currentShopSlots[index];
            if (slot.sold) return;
            slot.sold = true;
            string reason;
            bool purchased = slot.isRelic ? ResourceManager.Instance.TryPurchaseRelic(slot.relic, slot.price, out reason)
                : ResourceManager.Instance.TryPurchaseConsumable(slot.consumable, slot.price, out reason);
            if (!purchased)
            {
                slot.sold = false;
                NotificationManager.Instance?.ShowMessage(reason, ManagementTheme.Gold);
            }
            else
            {
                string name = slot.isRelic ? slot.relic.relicName : slot.consumable.consumableName;
                NotificationManager.Instance?.ShowMessage($"[{name}] 구매 완료!", ManagementTheme.Cyan);
            }
            RefreshUI();
        }

    }
}
