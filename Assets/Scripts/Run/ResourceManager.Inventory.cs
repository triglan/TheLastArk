using System.Collections.Generic;
using TheLastArk.Data;
using UnityEngine;

namespace TheLastArk.Managers
{
    public partial class ResourceManager
    {
        public const int ConsumableCapacity = 3;
        public const int SynthesisCost = 50;

        public EquipmentLoadouts EquipmentLoadouts
        {
            get
            {
                var state = RunManager.Instance.State;
                if (state.equipmentLoadouts == null) state.equipmentLoadouts = new EquipmentLoadouts();
                foreach (var status in state.partyStatuses) state.equipmentLoadouts.Register(status);
                var battle = FindObjectOfType<BattleManager>();
                if (battle != null) foreach (var actor in battle.playerParty) if (actor != null) state.equipmentLoadouts.Register(actor.status);
                return state.equipmentLoadouts;
            }
        }

        public bool CanAfford(int price) => price >= 0 && (long)Gold - price >= (HasRelicEffect(RelicEffectType.CreditLedger) ? -300 : 0);

        public bool CanPurchaseConsumable(ConsumableData item, int price, out string reason)
        {
            reason = item == null ? "상품 정보를 확인할 수 없습니다." : Consumables.Count >= ConsumableCapacity
                ? $"소모품 칸이 가득 찼습니다. ({Consumables.Count}/{ConsumableCapacity})" : !CanAfford(price) ? "골드가 부족합니다." : "";
            return reason.Length == 0;
        }

        public bool CanPurchaseRelic(RelicData item, int price, out string reason)
        {
            reason = item == null ? "상품 정보를 확인할 수 없습니다." : HasRelic(item.relicID)
                ? "이미 보유한 유물입니다." : !CanAfford(price) ? "골드가 부족합니다." : "";
            return reason.Length == 0;
        }

        // Commit both sides before notifying UI listeners, so a failed purchase cannot charge gold.
        public bool TryPurchaseConsumable(ConsumableData item, int price, out string reason)
        {
            if (!CanPurchaseConsumable(item, price, out reason)) return false;
            RunManager.Instance.State.gold -= price;
            Consumables.Add(item);
            OnGoldChanged?.Invoke(); OnConsumablesChanged?.Invoke();
            return true;
        }

        public bool TryPurchaseRelic(RelicData item, int price, out string reason)
        {
            if (!CanPurchaseRelic(item, price, out reason)) return false;
            RunManager.Instance.State.gold -= price;
            Relics.Add(item);
            OnGoldChanged?.Invoke(); OnRelicsChanged?.Invoke();
            return true;
        }

        public bool TryPurchaseEquipment(EquipmentData item, int price, out string reason)
        {
            reason = item == null ? "상품 정보를 확인할 수 없습니다." : !CanAfford(price) ? "골드가 부족합니다." : "";
            if (reason.Length > 0) return false;
            RunManager.Instance.State.gold -= price;
            Equipments.Add(item);
            OnGoldChanged?.Invoke(); OnEquipmentsChanged?.Invoke();
            return true;
        }

        public bool TryEquip(CharacterStatus status, int slot, EquipmentData equipment, out string reason)
        {
            var loadouts = EquipmentLoadouts;
            var owner = loadouts.Register(status);
            if (owner == null) { reason = "캐릭터 정보를 확인할 수 없습니다."; return false; }
            if (!loadouts.TryEquip(Equipments, owner.characterId, slot, equipment, out reason)) return false;
            ApplyEquipment(status, loadouts);
            foreach (var stored in RunManager.Instance.State.partyStatuses)
                if (stored?.origin != null && stored.origin.DataId == owner.characterId) ApplyEquipment(stored, loadouts);
            var battle = FindObjectOfType<BattleManager>();
            if (battle != null)
                foreach (var actor in battle.playerParty)
                    if (actor?.status?.origin != null && actor.status.origin.DataId == owner.characterId)
                    {
                        ApplyEquipment(actor.status, loadouts);
                        if (actor.view != null) actor.view.UpdateVisual(actor.status);
                    }
            OnEquipmentsChanged?.Invoke();
            return true;
        }

        private static void ApplyEquipment(CharacterStatus status, EquipmentLoadouts loadouts)
        {
            loadouts.ApplyTo(status);
            status.currentHp = Mathf.Min(status.currentHp, status.FinalMaxHp);
            status.currentMental = Mathf.Min(status.currentMental, status.FinalMaxMental);
        }

        public bool CanSynthesize(int first, int second, out string reason)
        {
            if (!EquipmentLoadouts.CanConsumePair(Equipments, first, second, out reason)) return false;
            if (EquipmentDatabase.GetSynthesisCandidates(Equipments[first], Equipments[second]).Count == 0)
            { reason = "같은 등급·계열의 1~2성 장비 두 개가 필요합니다."; return false; }
            if (!CanAfford(SynthesisCost)) { reason = "합성 비용 50 G가 부족합니다."; return false; }
            return true;
        }

        public bool TrySynthesize(int first, int second, out EquipmentData result, out string reason)
        {
            result = null;
            if (!CanSynthesize(first, second, out reason)) return false;
            var candidates = EquipmentDatabase.GetSynthesisCandidates(Equipments[first], Equipments[second]);
            result = candidates[Random.Range(0, candidates.Count)];
            RunManager.Instance.State.gold -= SynthesisCost;
            Equipments.RemoveAt(Mathf.Max(first, second));
            Equipments.RemoveAt(Mathf.Min(first, second));
            Equipments.Add(result);
            OnGoldChanged?.Invoke(); OnEquipmentsChanged?.Invoke();
            return true;
        }
    }
}
