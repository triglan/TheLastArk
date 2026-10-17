using System.Collections.Generic;
using System.Linq;
using TheLastArk.Data;
using UnityEngine;

namespace TheLastArk.UI
{
    public readonly struct EquipmentStatChange
    {
        public readonly string Name, Suffix;
        public readonly float Before, After;
        public float Delta => After - Before;
        public EquipmentStatChange(string name, float before, float after, string suffix = "") { Name = name; Before = before; After = after; Suffix = suffix; }
        public string DeltaText => Mathf.Abs(Delta) < 0.01f ? "변화 없음" : $"<color={(Delta > 0 ? "#92C7CD" : "#DF9B91")}>{Delta:+0.#;-0.#}{Suffix}</color>";
        public string Text => $"{Name}  {Before:0.#} → {After:0.#}{Suffix}  ({DeltaText})";
    }

    public static class EquipmentComparison
    {
        public static List<EquipmentStatChange> Changes(CharacterStatus status, int slot, EquipmentData candidate)
        {
            var preview = status.PreviewEquipment(slot, candidate);
            var result = new List<EquipmentStatChange>
            {
                new EquipmentStatChange("공격", status.FinalAttack, preview.FinalAttack),
                new EquipmentStatChange("주문", status.FinalSpellPower, preview.FinalSpellPower),
                new EquipmentStatChange("최대 체력", status.FinalMaxHp, preview.FinalMaxHp),
                new EquipmentStatChange("최대 정신", status.FinalMaxMental, preview.FinalMaxMental),
                new EquipmentStatChange("방어", status.FinalArmor, preview.FinalArmor),
                new EquipmentStatChange("마법 저항", status.FinalMagicResist, preview.FinalMagicResist),
                new EquipmentStatChange("치명", status.FinalCritRate, preview.FinalCritRate, "%")
            };
            return result;
        }

        public static string Summary(CharacterStatus status, int slot, EquipmentData candidate)
        {
            var changes = Changes(status, slot, candidate).Where(c => Mathf.Abs(c.Delta) >= 0.01f).ToList();
            return changes.Count == 0 ? "능력치 변화 없음" : string.Join("  ·  ", changes.Take(3).Select(c => c.Name + " " + c.DeltaText));
        }
    }
}
