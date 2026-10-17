using System.Collections.Generic;
using System.Linq;
using TheLastArk.Data;
using TheLastArk.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastArk.UI
{
    public partial class ManagementUIManager
    {
        private void BuildEquipmentSelector(CharacterData character, CharacterStatus status, int slot)
        {
            if (equipSelectModalPanel != null) { equipSelectModalPanel.SetActive(false); Destroy(equipSelectModalPanel); }
            var resources = ResourceManager.Instance;
            var loadouts = resources.EquipmentLoadouts;
            loadouts.Register(status);
            var root = ManagementTheme.Panel("EquipSelectModal", popupPanel.transform, ManagementTheme.Background, true);
            ManagementTheme.Place(root, Vector2.zero, Vector2.one, new Vector2(20, 32), new Vector2(-20, -158));
            equipSelectModalPanel = root.gameObject;
            var title = ManagementTheme.Label(root, $"{character.DisplayName}  /  장비 슬롯 {slot + 1}", 25, ManagementTheme.Gold);
            ManagementTheme.Top(title.rectTransform, 12, 40, 14, 110);
            var close = ManagementTheme.Button(root, "CloseEquipment", "닫기", () => root.gameObject.SetActive(false));
            ManagementTheme.Place((RectTransform)close.transform, Vector2.one, Vector2.one, new Vector2(-90, -52), new Vector2(-14, -12));
            var current = status.GetEquippedItem(slot);
            var currentText = ManagementTheme.Label(root, "현재 장착  ·  " + (current != null ? current.equipmentName : "없음"), 18, ManagementTheme.Muted);
            ManagementTheme.Top(currentText.rectTransform, 62, 36, 14, 170);
            var scroll = ManagementTheme.Scroll(root, "EquipmentScroll", out var content);
            ManagementTheme.Place(scroll, Vector2.zero, Vector2.one, new Vector2(14, 278), new Vector2(-14, -112));
            content.gameObject.AddComponent<GridLayoutGroup>();
            content.gameObject.AddComponent<ManagementGridLayout>().Configure(230, 146, 3);
            var detail = ManagementTheme.Panel("EquipmentComparison", root, ManagementTheme.Surface, true);
            ManagementTheme.Place(detail, Vector2.zero, new Vector2(1, 0), new Vector2(14, 12), new Vector2(-14, 264));
            var selectedTitle = ManagementTheme.Label(detail, "장비를 선택하세요", 23, ManagementTheme.Gold);
            ManagementTheme.Top(selectedTitle.rectTransform, 12, 34, 16, 175);
            var ownerText = ManagementTheme.Label(detail, "장착 전에 능력치 변화를 확인할 수 있습니다.", 16, ManagementTheme.Muted);
            ManagementTheme.Top(ownerText.rectTransform, 52, 36, 16, 16);
            ownerText.overflowMode = TextOverflowModes.Ellipsis;
            var ownerTooltip = ownerText.gameObject.AddComponent<BattleHoverTooltip>(); ownerText.raycastTarget = true;
            var stats = new List<TextMeshProUGUI>();
            for (int i = 0; i < 7; i++)
            {
                var row = ManagementTheme.Label(detail, "", 17);
                int column = i % 2, line = i / 2;
                ManagementTheme.Place(row.rectTransform, new Vector2(column * 0.5f, 1), new Vector2((column + 1) * 0.5f, 1),
                    new Vector2(16, -119 - line * 26), new Vector2(-8, -95 - line * 26));
                stats.Add(row);
            }
            var passive = ManagementTheme.Label(detail, "", 16, ManagementTheme.Muted);
            ManagementTheme.Top(passive.rectTransform, 204, 38, 16, 16);
            var cards = new Dictionary<EquipmentData, Image>();
            EquipmentData selected = null;
            bool selectionMade = false;
            var confirm = ManagementTheme.Button(detail, "ConfirmEquipment", "장비 선택", () =>
            {
                if (!selectionMade) return;
                if (!SetManagementEquipment(status, slot, selected)) { BuildEquipmentSelector(character, status, slot); return; }
                root.gameObject.SetActive(false);
                ShowCharacterDetailPopup(character);
            }, true);
            ManagementTheme.Place((RectTransform)confirm.transform, Vector2.one, Vector2.one, new Vector2(-160, -50), new Vector2(-14, -12));
            confirm.interactable = false;

            void Select(EquipmentData item)
            {
                selected = item; selectionMade = true;
                foreach (var pair in cards) pair.Value.color = EquipmentLoadouts.Same(pair.Key, item) ? ManagementTheme.Raised : ManagementTheme.Surface;
                selectedTitle.text = item != null ? item.equipmentName : "장착 해제";
                ownerText.text = item != null ? $"사용 가능 {loadouts.AvailableCount(resources.Equipments, item)} / 보유 {loadouts.OwnedCount(resources.Equipments, item)}  ·  {loadouts.Owners(item)}" : "현재 슬롯의 장비가 사용 가능한 수량으로 돌아갑니다.";
                ownerTooltip.SetContent(ownerText.text);
                var changes = EquipmentComparison.Changes(status, slot, item);
                for (int i = 0; i < stats.Count; i++) stats[i].text = changes[i].Text;
                bool same = current == item || EquipmentLoadouts.Same(current, item);
                bool allowed = loadouts.CanEquip(resources.Equipments, character.DataId, slot, item, out string reason);
                confirm.interactable = !same && allowed;
                confirm.GetComponentInChildren<TextMeshProUGUI>().text = same ? "현재 장착 중" : !allowed ? "사용 가능 0" : item != null ? "장착" : "해제";
                passive.text = !allowed ? reason : item == null ? "장착 해제 후 능력치입니다." : string.IsNullOrEmpty(item.passiveSkillName)
                    ? "현재 능력치를 기준으로 교체 후 변화를 표시합니다." : $"{item.passiveSkillName} · {item.passiveDescription}";
                passive.gameObject.GetComponent<BattleHoverTooltip>()?.SetContent(passive.text);
            }
            passive.gameObject.AddComponent<BattleHoverTooltip>(); passive.raycastTarget = true;
            var clear = ManagementTheme.Button(root, "PreviewUnequip", "장착 해제", () => Select(null));
            ManagementTheme.Place((RectTransform)clear.transform, Vector2.one, Vector2.one, new Vector2(-156, -98), new Vector2(-14, -62));
            clear.interactable = current != null;
            var items = ManagementInventory.FilterAndSort(ManagementInventory.Build(resources.Equipments, null, null), InventoryCategory.Equipment, InventorySort.Rarity);
            foreach (var entry in items)
            {
                var item = entry.Equipment;
                bool equipped = EquipmentLoadouts.Same(item, current);
                int available = loadouts.AvailableCount(resources.Equipments, item);
                var button = ManagementTheme.Button(content, "Equipment_" + entry.Key, "", () => Select(item));
                Destroy(button.GetComponentInChildren<TextMeshProUGUI>().gameObject);
                cards[item] = button.GetComponent<Image>();
                var name = ManagementTheme.Label(button.transform, entry.Name, 20, equipped ? ManagementTheme.Gold : ManagementTheme.Text);
                ManagementTheme.Top(name.rectTransform, 12, 30, 12, 12);
                var quantity = ManagementTheme.Label(button.transform, equipped ? "이 슬롯에 장착 중" : $"사용 가능 {available} / 보유 {entry.Count}", 16, available > 0 ? ManagementTheme.Cyan : ManagementTheme.Muted);
                ManagementTheme.Top(quantity.rectTransform, 46, 22, 12, 12);
                var change = ManagementTheme.Label(button.transform, EquipmentComparison.Summary(status, slot, item), 16, ManagementTheme.Muted);
                ManagementTheme.Top(change.rectTransform, 78, 54, 12, 12);
                button.gameObject.AddComponent<BattleHoverTooltip>().SetContent(entry.Description + "\n" + loadouts.Owners(item));
            }
            if (items.Count == 0) EmptyState(scroll, "보유한 장비가 없습니다", "탐험이나 대장간에서 장비를 획득하세요.");
            if (current != null) Select(current);
        }
    }
}
