using System.Linq;
using TheLastArk.Data;
using TheLastArk.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public partial class ForgeUI
    {
        private void BuildProtectedSynthesisView()
        {
            var resources = ResourceManager.Instance;
            var inventory = resources.Equipments;
            var loadouts = resources.EquipmentLoadouts;
            if (selectedIndex1 >= inventory.Count || loadouts.IsReserved(inventory, selectedIndex1)) selectedIndex1 = -1;
            if (selectedIndex2 >= inventory.Count || loadouts.IsReserved(inventory, selectedIndex2)) selectedIndex2 = -1;
            var root = ManagementTheme.Rect("ProtectedSynthesis", contentArea);
            var tip = ManagementTheme.Label(root, "같은 등급·계열의 미장착 장비 2개 → 상위 장비 1개", 21, ManagementTheme.Muted);
            ManagementTheme.Top(tip.rectTransform, 0, 44, 2, 240);
            int selectedCount = (selectedIndex1 >= 0 ? 1 : 0) + (selectedIndex2 >= 0 ? 1 : 0);
            var count = ManagementTheme.Label(root, $"선택한 재료   {selectedCount} / 2", 21, ManagementTheme.Gold, TextAlignmentOptions.MidlineRight);
            ManagementTheme.Place(count.rectTransform, Vector2.one, Vector2.one, new Vector2(-230, -44), Vector2.zero);
            var scroll = ManagementTheme.Scroll(root, "SynthesisInventory", out var list);
            ManagementTheme.Place(scroll, Vector2.zero, Vector2.one, new Vector2(0, 202), new Vector2(0, -58));
            list.gameObject.AddComponent<GridLayoutGroup>();
            list.gameObject.AddComponent<ManagementGridLayout>().Configure(310, 174, 4);
            for (int i = 0; i < inventory.Count; i++)
            {
                int index = i;
                var item = inventory[i];
                if (item == null) continue;
                bool reserved = loadouts.IsReserved(inventory, index);
                bool selected = index == selectedIndex1 || index == selectedIndex2;
                bool maxRank = item.starLevel >= 3;
                string state = reserved ? "장착 중 · 합성 보호" : maxRank ? "최고 등급 · 합성 불가" : selected ? "선택됨" : "합성 가능";
                var button = ManagementTheme.Button(list, "InvEquip_" + index, "", () =>
                {
                    if (selectedIndex1 == index) selectedIndex1 = -1;
                    else if (selectedIndex2 == index) selectedIndex2 = -1;
                    else if (selectedIndex1 == -1) selectedIndex1 = index;
                    else if (selectedIndex2 == -1) selectedIndex2 = index;
                    else selectedIndex1 = index;
                    RefreshUI();
                }, selected);
                var name = button.GetComponentInChildren<TextMeshProUGUI>();
                name.text = item.equipmentName; name.fontSize = 24; name.alignment = TextAlignmentOptions.MidlineLeft;
                ManagementTheme.Top(name.rectTransform, 16, 36, 18, 44);
                var category = ManagementTheme.Label(button.transform, $"{item.starLevel}성 · {item.category}", 18, ManagementTheme.Muted);
                ManagementTheme.Top(category.rectTransform, 58, 28, 18, 18);
                var status = ManagementTheme.Label(button.transform, state, 18, selected ? ManagementTheme.Gold : reserved || maxRank ? ManagementTheme.Muted : ManagementTheme.Cyan);
                ManagementTheme.Top(status.rectTransform, 126, 28, 18, 18);
                if (selected)
                {
                    var order = ManagementTheme.Label(button.transform, index == selectedIndex1 ? "01" : "02", 20, ManagementTheme.Gold, TextAlignmentOptions.MidlineRight);
                    ManagementTheme.Top(order.rectTransform, 16, 36, 18, 18);
                    var accent = ManagementTheme.Panel("SelectionMark", button.transform, ManagementTheme.Gold);
                    ManagementTheme.Place(accent, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(3, 0));
                    accent.GetComponent<Image>().raycastTarget = false;
                }
                button.interactable = !reserved && !maxRank;
                button.gameObject.AddComponent<BattleHoverTooltip>().SetContent(ManagementInventory.EquipmentDescription(item) + "\n" + loadouts.Owners(item));
            }
            if (inventory.Count == 0)
            {
                var empty = ManagementTheme.Label(scroll, "합성할 장비가 없습니다.\n<size=19>장비 구매 탭에서 장비를 구입하거나 탐험 중 획득하세요.</size>", 25, ManagementTheme.Muted, TextAlignmentOptions.Center);
                ManagementTheme.Place(empty.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -20));
            }
            var first = selectedIndex1 >= 0 ? inventory[selectedIndex1] : null;
            var second = selectedIndex2 >= 0 ? inventory[selectedIndex2] : null;
            var summary = ManagementTheme.Panel("SynthesisSummary", root, ManagementTheme.Surface, true);
            ManagementTheme.Place(summary, Vector2.zero, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 182));
            var selectedText = ManagementTheme.Label(summary, $"{(first != null ? first.equipmentName : "첫 번째 장비 선택")}   +   {(second != null ? second.equipmentName : "두 번째 장비 선택")}", 23, ManagementTheme.Gold);
            ManagementTheme.Top(selectedText.rectTransform, 14, 36, 20, 250);
            var candidates = EquipmentDatabase.GetSynthesisCandidates(first, second);
            string result = candidates.Count > 0 ? "결과 후보  ·  " + string.Join(" / ", candidates.Select(c => c.equipmentName)) : "같은 등급·계열의 1~2성 장비를 선택하세요.";
            var results = ManagementTheme.Label(summary, result, 20, ManagementTheme.Text);
            ManagementTheme.Top(results.rectTransform, 56, 40, 20, 250);
            results.raycastTarget = true;
            results.gameObject.AddComponent<BattleHoverTooltip>().SetContent(result);
            bool allowed = resources.CanSynthesize(selectedIndex1, selectedIndex2, out string reason);
            var note = ManagementTheme.Label(summary, allowed ? "합성은 성공하며, 결과 후보 중 하나를 무작위로 획득합니다." : reason, 18, ManagementTheme.Muted);
            ManagementTheme.Top(note.rectTransform, 105, 40, 20, 250);
            var confirm = ManagementTheme.Button(summary, "DoSynthButton", "합성 · 50 G", ExecuteSynthesis, true);
            ManagementTheme.Place((RectTransform)confirm.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-230, -28), new Vector2(-20, 28));
            confirm.interactable = allowed;
        }
    }
}
