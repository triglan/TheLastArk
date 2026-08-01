using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;

namespace TheLastArk.Managers
{
    public class ResourceManager : MonoBehaviour
    {
        private static ResourceManager instance;
        public static bool IsInitialized => instance != null;
        public static ResourceManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ResourceManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ResourceManager");
                        instance = go.AddComponent<ResourceManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public int Gold { get; private set; }
        public event Action OnGoldChanged;

        [Header("Consumables (Max 3)")]
        public List<ConsumableData> Consumables = new List<ConsumableData>();
        public event Action OnConsumablesChanged;

        [Header("Relics")]
        public List<RelicData> Relics = new List<RelicData>();
        public event Action OnRelicsChanged;

        [Header("Equipments")]
        public List<EquipmentData> Equipments = new List<EquipmentData>();
        public event Action OnEquipmentsChanged;

        [Header("Character Cards")]
        public Dictionary<string, int> characterCards = new Dictionary<string, int>();
        public event Action<string, int> OnCharacterLevelChanged; // characterID, newLevel
        public event Action OnCardsChanged;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        // --- Gold ---
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            OnGoldChanged?.Invoke();
        }

        public bool SpendGold(int amount)
        {
            if (amount <= 0 || Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke();
            return true;
        }

        // --- Consumables ---
        public bool AddConsumable(ConsumableData data)
        {
            if (data == null || Consumables.Count >= 3) return false;
            Consumables.Add(data);
            OnConsumablesChanged?.Invoke();
            return true;
        }

        public void RemoveConsumable(int index)
        {
            if (index >= 0 && index < Consumables.Count)
            {
                Consumables.RemoveAt(index);
                OnConsumablesChanged?.Invoke();
            }
        }

        // --- Relics ---
        public void AddRelic(RelicData data)
        {
            if (data == null) return;
            if (!HasRelic(data.relicID))
            {
                Relics.Add(data);
                OnRelicsChanged?.Invoke();
            }
        }

        public void AddRelic(string relicID)
        {
            if (string.IsNullOrEmpty(relicID)) return;
            var allRelics = Resources.LoadAll<RelicData>("");
            foreach (var r in allRelics)
            {
                if (r != null && r.relicID == relicID)
                {
                    AddRelic(r);
                    break;
                }
            }
        }

        public bool HasRelic(string relicID)
        {
            return Relics.Exists(r => r.relicID == relicID);
        }

        // --- Equipments ---
        public void AddEquipment(EquipmentData data)
        {
            if (data == null) return;
            Equipments.Add(data);
            OnEquipmentsChanged?.Invoke();
        }

        public bool HasRelicEffect(RelicEffectType type)
        {
            return Relics.Exists(r => r.effectType == type);
        }

        public float GetRelicBonus(RelicEffectType type)
        {
            float total = 0f;
            foreach (var r in Relics)
            {
                if (r.effectType == type)
                    total += r.effectValue;
            }
            return total;
        }

        // --- Character Cards ---
        public void AddCharacterCard(string characterID, int amount)
        {
            if (!characterCards.ContainsKey(characterID))
                characterCards[characterID] = 0;

            int oldLevel = GetCharacterLevelFromCards(characterCards[characterID]);
            characterCards[characterID] += amount;
            int newLevel = GetCharacterLevelFromCards(characterCards[characterID]);

            OnCardsChanged?.Invoke();

            if (newLevel > oldLevel)
            {
                Debug.Log($"[ResourceManager] 캐릭터 {characterID} 자동 레벨업! {oldLevel}강 -> {newLevel}강");
                OnCharacterLevelChanged?.Invoke(characterID, newLevel);
            }
        }

        public int GetCardCount(string characterID)
        {
            return characterCards.ContainsKey(characterID) ? characterCards[characterID] : 0;
        }

        public int GetCharacterLevelFromCards(int cardCount)
        {
            if (cardCount >= 18) return 4;
            if (cardCount >= 9) return 3;
            if (cardCount >= 6) return 2;
            if (cardCount >= 3) return 1;
            if (cardCount >= 1) return 0; // 1장이면 명함(0강)
            return -1; // 미보유
        }
    }
}
