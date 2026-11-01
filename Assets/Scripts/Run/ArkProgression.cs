using System;
using TheLastArk.Data;
using UnityEngine;

namespace TheLastArk.Run
{
    public static class ArkProgression
    {
        public const string SaveKey = "TheLastArk.Ark.v1";
        private static ArkProgressData data;
        public static event Action Changed;
        public static ArkProgressData Data
        {
            get
            {
                if (data != null) return data;
                try { data = JsonUtility.FromJson<ArkProgressData>(PlayerPrefs.GetString(SaveKey, "{}")); }
                catch (ArgumentException) { Debug.LogWarning("[Ark] 저장된 방주 기록을 읽을 수 없습니다."); }
                if (data == null) data = new ArkProgressData();
                data.memory = Mathf.Max(0, data.memory);
                return data;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() { data = null; Changed = null; }
        public static bool IsUnlocked(string id) => Data.Level(id) > 0;
        public static int Amount(ArkNodeKind kind) => Data.Amount(ArkProgressionConfig.Load(), kind);
        private static void Save() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data)); PlayerPrefs.Save(); Changed?.Invoke(); }
        public static bool TryPurchase(string id, out string message)
        {
            if (!Data.TryPurchase(ArkProgressionConfig.Load(), id, out message)) return false;
            Save(); return true;
        }
        public static void AddMemory(int amount) { Data.AddMemory(amount); Save(); }
        public static void BeginRun() { var config = ArkProgressionConfig.Load(); Data.Settle(config); Data.BeginRun(config); Save(); }
        public static void RecordVisit(int floor) { if (string.IsNullOrEmpty(Data.activeRunId)) return; Data.RecordVisit(floor); Save(); }
        public static void RecordVictory(string id, NodeType type) { if (Data.RecordVictory(id, type)) Save(); }
        public static int SettleRun()
        {
            if (string.IsNullOrEmpty(Data.activeRunId)) return 0;
            int reward = Data.Settle(ArkProgressionConfig.Load()); Save(); return reward;
        }
    }
}
