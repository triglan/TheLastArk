using System;
using System.Collections.Generic;
using TheLastArk.Data;

namespace TheLastArk.Run
{
    [Serializable] public sealed class ArkNodeLevel { public string id; public int level; }

    /// <summary>Permanent progress is separate from the resettable run state.</summary>
    [Serializable]
    public sealed class ArkProgressData
    {
        public int memory;
        public List<ArkNodeLevel> levels = new List<ArkNodeLevel>();
        public string activeRunId;
        public int furthestFloor;
        public int normalWins, eliteWins, bossWins;
        public List<string> rewardedBattles = new List<string>();
        public int lastRunMemory;
        public int victoryMemoryBonus;

        public int Level(string id)
        {
            if (levels != null) foreach (var item in levels) if (item != null && item.id == id) return Math.Max(0, item.level);
            return 0;
        }

        public int Amount(ArkProgressionConfig config, ArkNodeKind kind)
        {
            int result = 0;
            foreach (var node in config.nodes)
                if (node != null && !node.placeholder && node.kind == kind)
                    result += Math.Min(Level(node.id), node.MaxLevel) * Math.Max(1, node.amountPerLevel);
            return result;
        }

        public bool PrerequisitesMet(ArkProgressionConfig config, ArkNodeDefinition node, out string missingId)
        {
            missingId = null;
            if (node == null) return false;
            // Keep already purchased upgrades usable when a later tree revision adds a prerequisite.
            if (Level(node.id) > 0 || node.prerequisites == null) return true;
            foreach (string id in node.prerequisites)
            {
                var parent = config.Find(id);
                if (parent == null || parent.placeholder || parent.MaxLevel == 0 || Level(id) < parent.MaxLevel)
                { missingId = id; return false; }
            }
            return true;
        }

        public bool TryPurchase(ArkProgressionConfig config, string id, out string message)
        {
            var node = config.Find(id);
            message = "";
            if (node == null || node.placeholder) { message = "아직 열리지 않은 별입니다."; return false; }
            int level = Level(node.id);
            if (level >= node.MaxLevel) { message = "최대 단계에 도달했습니다."; return false; }
            if (!PrerequisitesMet(config, node, out string missing))
            { message = $"‘{config.Find(missing)?.title ?? missing}’을 먼저 완성해야 합니다."; return false; }
            int cost = node.CostAt(level);
            if (memory < cost) { message = $"기억이 {cost - memory} 더 필요합니다."; return false; }
            if (levels == null) levels = new List<ArkNodeLevel>();
            var entry = levels.Find(item => item != null && item.id == node.id);
            if (entry == null) { entry = new ArkNodeLevel { id = node.id }; levels.Add(entry); }
            memory -= cost;
            entry.level = level + 1;
            return true;
        }

        public void AddMemory(int amount)
        {
            if (amount > 0) memory = (int)Math.Min(int.MaxValue, (long)Math.Max(0, memory) + amount);
        }

        public void BeginRun(ArkProgressionConfig config = null)
        {
            activeRunId = Guid.NewGuid().ToString("N"); furthestFloor = normalWins = eliteWins = bossWins = 0;
            rewardedBattles = new List<string>();
            victoryMemoryBonus = config != null ? Amount(config, ArkNodeKind.VictoryMemory) : 0;
        }
        public void RecordVisit(int floor) { if (!string.IsNullOrEmpty(activeRunId)) furthestFloor = Math.Max(furthestFloor, floor); }
        public bool RecordVictory(string nodeId, NodeType type)
        {
            if (string.IsNullOrEmpty(activeRunId) || string.IsNullOrEmpty(nodeId)) return false;
            if (rewardedBattles == null) rewardedBattles = new List<string>();
            if (rewardedBattles.Contains(nodeId)) return false;
            rewardedBattles.Add(nodeId);
            if (type == NodeType.Boss) bossWins++;
            else if (type == NodeType.Elite) eliteWins++;
            else normalWins++;
            return true;
        }

        public int Settle(ArkProgressionConfig config)
        {
            if (string.IsNullOrEmpty(activeRunId)) return 0;
            long reward = (long)Math.Max(0, furthestFloor - 1) * Math.Max(0, config.memoryPerFloor)
                + (long)normalWins * Math.Max(0, config.memoryPerBattle)
                + (long)eliteWins * Math.Max(0, config.memoryPerElite)
                + (long)bossWins * Math.Max(0, config.memoryPerBoss)
                + ((long)normalWins + eliteWins + bossWins) * Math.Max(0, victoryMemoryBonus);
            lastRunMemory = (int)Math.Min(int.MaxValue, Math.Max(0, reward));
            AddMemory(lastRunMemory);
            activeRunId = null;
            return lastRunMemory;
        }
    }
}
