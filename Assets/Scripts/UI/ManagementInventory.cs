using System.Collections.Generic;
using System.Linq;
using TheLastArk.Data;
using UnityEngine;

namespace TheLastArk.UI
{
    public enum InventoryCategory { All, Equipment, Relic, Consumable }
    public enum InventorySort { Category, Name, Rarity, Count }

    public sealed class InventoryEntry
    {
        public string Key, Name, Description, Subtitle;
        public InventoryCategory Category;
        public Sprite Icon;
        public int Rank, Count;
        public EquipmentData Equipment;
        public string CategoryLabel => ManagementInventory.CategoryName(Category);
    }

    public static class ManagementInventory
    {
        public static string CategoryName(InventoryCategory category) => category switch
        { InventoryCategory.Equipment => "장비", InventoryCategory.Relic => "유물", InventoryCategory.Consumable => "소모품", _ => "전체" };
        public static string SortName(InventorySort sort) => sort switch
        { InventorySort.Name => "이름순", InventorySort.Rarity => "높은 등급순", InventorySort.Count => "많은 수량순", _ => "종류순" };

        public static List<InventoryEntry> Build(IEnumerable<EquipmentData> equipment, IEnumerable<RelicData> relics, IEnumerable<ConsumableData> consumables)
        {
            var entries = new Dictionary<string, InventoryEntry>();
            foreach (var item in equipment ?? Enumerable.Empty<EquipmentData>())
                if (item != null) Add(entries, new InventoryEntry { Key = Key(InventoryCategory.Equipment, item.equipmentID, item), Name = item.equipmentName,
                    Category = InventoryCategory.Equipment, Icon = item.icon, Rank = item.starLevel, Equipment = item,
                    Subtitle = $"{item.starLevel}성 · {item.category}", Description = EquipmentDescription(item) });
            foreach (var item in relics ?? Enumerable.Empty<RelicData>())
                if (item != null) Add(entries, new InventoryEntry { Key = Key(InventoryCategory.Relic, item.relicID, item), Name = item.relicName,
                    Category = InventoryCategory.Relic, Icon = item.icon, Rank = item.rarity == RelicRarity.Legendary ? 3 : 1,
                    Subtitle = item.rarity == RelicRarity.Legendary ? "전설 유물" : "일반 유물", Description = item.description });
            foreach (var item in consumables ?? Enumerable.Empty<ConsumableData>())
                if (item != null) Add(entries, new InventoryEntry { Key = Key(InventoryCategory.Consumable, item.consumableID, item), Name = item.consumableName,
                    Category = InventoryCategory.Consumable, Icon = item.icon, Rank = 0, Subtitle = "전투 소모품", Description = item.description });
            return entries.Values.ToList();
        }

        public static List<InventoryEntry> FilterAndSort(IEnumerable<InventoryEntry> entries, InventoryCategory category, InventorySort sort)
        {
            var filtered = entries.Where(e => category == InventoryCategory.All || e.Category == category);
            IOrderedEnumerable<InventoryEntry> ordered = sort switch
            {
                InventorySort.Name => filtered.OrderBy(e => e.Name, System.StringComparer.CurrentCulture),
                InventorySort.Rarity => filtered.OrderByDescending(e => e.Rank).ThenBy(e => e.Category),
                InventorySort.Count => filtered.OrderByDescending(e => e.Count).ThenBy(e => e.Category),
                _ => filtered.OrderBy(e => e.Category).ThenByDescending(e => e.Rank)
            };
            return ordered.ThenBy(e => e.Name, System.StringComparer.CurrentCulture).ThenBy(e => e.Key, System.StringComparer.Ordinal).ToList();
        }

        private static string Key(InventoryCategory category, string id, Object item) => category + ":" + (string.IsNullOrEmpty(id) ? item.GetInstanceID().ToString() : id);
        private static void Add(Dictionary<string, InventoryEntry> entries, InventoryEntry entry)
        {
            if (entries.TryGetValue(entry.Key, out var existing)) existing.Count++;
            else { entry.Count = 1; entries.Add(entry.Key, entry); }
        }

        public static string EquipmentDescription(EquipmentData item)
        {
            var stats = new List<string>();
            AddStat(stats, "공격", item.bonusAttack); AddStat(stats, "주문", item.bonusSpellPower);
            AddStat(stats, "체력", item.bonusHp); AddStat(stats, "정신력", item.bonusMental);
            AddStat(stats, "방어", item.bonusArmor); AddStat(stats, "마법 저항", item.bonusMagicResist);
            if (item.bonusCritRate != 0) stats.Add($"치명 {item.bonusCritRate:+0.#;-0.#}%");
            AddStat(stats, "행동력", item.bonusAP);
            string result = string.Join("  /  ", stats);
            if (!string.IsNullOrWhiteSpace(item.description)) result += "\n" + item.description;
            if (!string.IsNullOrWhiteSpace(item.passiveSkillName)) result += $"\n{item.passiveSkillName} · {item.passiveDescription}";
            return result.Trim();
        }
        private static void AddStat(List<string> stats, string label, float value) { if (value != 0) stats.Add($"{label} {value:+0.#;-0.#}"); }
    }
}
