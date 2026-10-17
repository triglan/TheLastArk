using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TheLastArk.Data;
using TheLastArk.Managers;

namespace TheLastArk.UI
{
    public partial class ForgeUI : MonoBehaviour
    {
        private GameObject popupPanel;
        private TextMeshProUGUI balanceText;
        private TextMeshProUGUI inventoryText;
        private Button buyTabButton, synthesisTabButton;

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
        private ResourceManager observedResources;

        private void RefreshVisibleForge()
        {
            if (popupPanel != null && popupPanel.activeInHierarchy) RefreshUI();
        }

        private void InventoryChanged()
        {
            selectedIndex1 = selectedIndex2 = -1;
            RefreshVisibleForge();
        }

        private void OnDestroy()
        {
            if (observedResources == null) return;
            observedResources.OnGoldChanged -= RefreshVisibleForge;
            observedResources.OnEquipmentsChanged -= InventoryChanged;
        }

        public void Show()
        {
            if (observedResources == null)
            {
                observedResources = ResourceManager.Instance;
                observedResources.OnGoldChanged += RefreshVisibleForge;
                observedResources.OnEquipmentsChanged += InventoryChanged;
            }
            if (popupPanel == null)
            {
                CreateUI();
            }

            GenerateForgeItems();
            SwitchTab(TabType.BuyEquipment);
            popupPanel.transform.SetAsLastSibling();
            popupPanel.SetActive(true);
        }

        public void Hide()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
        }

        private void CreateUI()
        {
            popupPanel = ManagementDialog.Create(HudCanvasUtility.FindOverlayCanvas(), "ForgePopup", "마을 대장간", "FORGE  /  장비 구매 · 합성", Hide, 0.08f, out var window);
            var toolbar = ManagementDialog.Toolbar(window);
            balanceText = ManagementTheme.Label(toolbar, "", 25, ManagementTheme.Gold);
            ManagementTheme.Place(balanceText.rectTransform, Vector2.zero, new Vector2(0.28f, 1), new Vector2(18, 0), Vector2.zero);
            inventoryText = ManagementTheme.Label(toolbar, "", 19, ManagementTheme.Muted);
            ManagementTheme.Place(inventoryText.rectTransform, new Vector2(0.28f, 0), new Vector2(0.55f, 1));
            var tabs = ManagementTheme.Rect("ForgeTabsArea", toolbar);
            ManagementTheme.Place(tabs, new Vector2(0.58f, 0), Vector2.one, new Vector2(0, 7), new Vector2(-8, -7));
            ManagementTheme.RowLayout(tabs, 8);
            buyTabButton = ManagementTheme.Button(tabs, "Tab_장비 구매", "01   장비 구매", () => SwitchTab(TabType.BuyEquipment));
            synthesisTabButton = ManagementTheme.Button(tabs, "Tab_장비 합성", "02   장비 합성", () => SwitchTab(TabType.Synthesize));
            contentArea = ManagementDialog.Body(window);
        }

        private void UpdateTabStyle(Button button, bool selected)
        {
            button.GetComponent<Image>().color = selected ? ManagementTheme.Raised : ManagementTheme.Surface;
            button.GetComponentInChildren<TextMeshProUGUI>().color = selected ? ManagementTheme.Gold : ManagementTheme.Muted;
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
            foreach (Transform child in contentArea) { child.gameObject.SetActive(false); Destroy(child.gameObject); }

            var resources = ResourceManager.Instance;
            balanceText.text = $"보유 골드   {resources.Gold:N0} G";
            inventoryText.text = $"보유 장비  {resources.Equipments.Count}개";
            UpdateTabStyle(buyTabButton, currentTab == TabType.BuyEquipment);
            UpdateTabStyle(synthesisTabButton, currentTab == TabType.Synthesize);

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
            var root = ManagementTheme.Rect("EquipmentShop", contentArea);
            var tip = ManagementTheme.Label(root, "새로운 장비로 다음 전투를 준비하세요. 구매한 장비는 승무원 창에서 장착할 수 있습니다.", 20, ManagementTheme.Muted);
            ManagementTheme.Top(tip.rectTransform, 0, 32);
            var scroll = ManagementTheme.Scroll(root, "ForgeProducts", out var list);
            ManagementTheme.Place(scroll, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -44));
            list.gameObject.AddComponent<GridLayoutGroup>();
            list.gameObject.AddComponent<ManagementGridLayout>().Configure(500, 286, 2);
            var resources = ResourceManager.Instance;
            for (int i = 0; i < forgeSlots.Count; i++)
            {
                var slot = forgeSlots[i];
                var item = slot.equipment;
                string name = item != null ? item.equipmentName : "장비";
                string description = GetEquipmentSummaryText(item);
                if (item != null && !string.IsNullOrEmpty(item.passiveDescription)) description += "\n" + item.passiveDescription;
                string unavailable = item == null ? "상품 정보를 확인할 수 없습니다." : !resources.CanAfford(slot.price) ? "골드가 부족합니다." : "";
                ManagementDialog.Product(list, $"EquipCard_{i}", InventoryCategory.Equipment,
                    item != null ? $"{item.starLevel}성 · {item.category}" : "장비", name, description,
                    item != null ? item.icon : null, slot.price, slot.sold, unavailable, () =>
                {
                    if (slot.sold) return;
                    slot.sold = true;
                    if (!resources.TryPurchaseEquipment(item, slot.price, out string reason))
                    {
                        slot.sold = false;
                        NotificationManager.Instance?.ShowMessage(reason, ManagementTheme.Gold);
                        RefreshUI(); return;
                    }
                    NotificationManager.Instance?.ShowMessage($"장비 [{name}] 구매 완료!", ManagementTheme.Cyan);
                    RefreshUI();
                });
            }
            if (forgeSlots.Count == 0) ManagementTheme.Label(list, "지금 구매할 수 있는 장비가 없습니다.", 24, ManagementTheme.Muted);
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

            string str = string.Join("   ·   ", stats);
            if (!string.IsNullOrEmpty(eq.passiveSkillName))
            {
                str += $"\n<color=gold>[{eq.passiveSkillName}]</color>";
            }
            return str;
        }

        private void BuildSynthesizeView()
        {
            BuildProtectedSynthesisView();
        }

        private void ExecuteSynthesis()
        {
            if (!ResourceManager.Instance.TrySynthesize(selectedIndex1, selectedIndex2, out var result, out string reason))
            {
                NotificationManager.Instance?.ShowMessage(reason, ManagementTheme.Gold);
                RefreshUI();
                return;
            }
            NotificationManager.Instance?.ShowMessage($"합성 완료! [{result.starLevel}성] {result.equipmentName}", ManagementTheme.Cyan);
            selectedIndex1 = selectedIndex2 = -1;
            RefreshUI();
        }

    }
}
