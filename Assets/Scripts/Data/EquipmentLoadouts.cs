using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLastArk.Data
{
    [Serializable]
    public sealed class EquipmentLoadout
    {
        public string characterId;
        public string characterName;
        public EquipmentData[] slots = new EquipmentData[CharacterStatus.EquipmentSlotCount];

        public void EnsureSlots()
        {
            if (slots == null || slots.Length != CharacterStatus.EquipmentSlotCount)
                Array.Resize(ref slots, CharacterStatus.EquipmentSlotCount);
        }
    }

    /// <summary>Assignments persist for benched crew; inventory retains all owned copies.</summary>
    [Serializable]
    public sealed class EquipmentLoadouts
    {
        public List<EquipmentLoadout> characters = new List<EquipmentLoadout>();

        public static bool Same(EquipmentData a, EquipmentData b) => a != null && b != null &&
            (a == b || (!string.IsNullOrEmpty(a.equipmentID) && a.equipmentID == b.equipmentID));

        public EquipmentLoadout Register(CharacterStatus status)
        {
            if (status?.origin == null || status.origin.isEnemy || string.IsNullOrEmpty(status.origin.DataId)) return null;
            var found = characters.Find(c => c.characterId == status.origin.DataId);
            if (found == null)
            {
                found = new EquipmentLoadout { characterId = status.origin.DataId };
                for (int slot = 0; slot < CharacterStatus.EquipmentSlotCount; slot++) found.slots[slot] = status.GetEquippedItem(slot);
                characters.Add(found);
            }
            found.EnsureSlots();
            found.characterName = status.origin.DisplayName;
            return found;
        }

        public void ApplyTo(CharacterStatus status)
        {
            var loadout = Register(status);
            if (loadout == null) return;
            for (int slot = 0; slot < CharacterStatus.EquipmentSlotCount; slot++) status.SetEquippedItem(slot, loadout.slots[slot]);
        }

        public int OwnedCount(IList<EquipmentData> inventory, EquipmentData equipment) => inventory.Count(e => Same(e, equipment));
        public int AssignedCount(EquipmentData equipment) => characters.Sum(c => c.slots.Count(e => Same(e, equipment)));
        public int AvailableCount(IList<EquipmentData> inventory, EquipmentData equipment) => Math.Max(0, OwnedCount(inventory, equipment) - AssignedCount(equipment));

        public string Owners(EquipmentData equipment)
        {
            var names = new List<string>();
            foreach (var owner in characters)
                for (int slot = 0; slot < owner.slots.Length; slot++)
                    if (Same(owner.slots[slot], equipment)) names.Add($"{owner.characterName} · 슬롯 {slot + 1}");
            return names.Count == 0 ? "장착 중인 캐릭터 없음" : string.Join(", ", names);
        }

        public bool CanEquip(IList<EquipmentData> inventory, string characterId, int slot, EquipmentData equipment, out string reason)
        {
            reason = "";
            var owner = characters.Find(c => c.characterId == characterId);
            if (owner == null || slot < 0 || slot >= CharacterStatus.EquipmentSlotCount) { reason = "장비 슬롯을 확인할 수 없습니다."; return false; }
            if (equipment == null || Same(owner.slots[slot], equipment)) return true;
            if (equipment.isSpecial && AssignedCount(equipment) > 0)
            {
                reason = "같은 특수 장비는 파티 전체에 1개만 장착할 수 있습니다.";
                return false;
            }
            if (AvailableCount(inventory, equipment) > 0) return true;
            reason = OwnedCount(inventory, equipment) == 0 ? "보유하지 않은 장비입니다." : "사용 가능한 장비가 없습니다. 다른 슬롯의 장비를 먼저 해제하세요.";
            return false;
        }

        public bool TryEquip(IList<EquipmentData> inventory, string characterId, int slot, EquipmentData equipment, out string reason)
        {
            if (!CanEquip(inventory, characterId, slot, equipment, out reason)) return false;
            characters.Find(c => c.characterId == characterId).slots[slot] = equipment;
            return true;
        }

        // Identical copies are interchangeable. Reserve the first N copies for their owners.
        public bool IsReserved(IList<EquipmentData> inventory, int index)
        {
            if (index < 0 || index >= inventory.Count || inventory[index] == null) return false;
            int ordinal = 0;
            for (int i = 0; i <= index; i++) if (Same(inventory[i], inventory[index])) ordinal++;
            return ordinal <= AssignedCount(inventory[index]);
        }

        public bool CanConsumePair(IList<EquipmentData> inventory, int first, int second, out string reason)
        {
            reason = "";
            if (first < 0 || second < 0 || first >= inventory.Count || second >= inventory.Count || first == second || inventory[first] == null || inventory[second] == null)
            { reason = "합성할 장비 두 개를 선택하세요."; return false; }
            if (IsReserved(inventory, first) || IsReserved(inventory, second))
            { reason = "장착 중인 장비는 합성할 수 없습니다. 먼저 장착을 해제하세요."; return false; }
            return true;
        }
    }
}
