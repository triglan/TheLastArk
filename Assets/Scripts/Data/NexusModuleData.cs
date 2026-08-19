using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Data
{
    [System.Serializable]
    public class NexusModuleData
    {
        public string moduleId;
        public string moduleName;
        public string description;
        public int maxLevel;
        public int baseUpgradeCost;
        public int basePartSlots;
        public Sprite icon;

        public NexusModuleData(string id, string name, string desc, int maxLevel, int upgradeCost, int partSlots, Sprite icon = null)
        {
            this.moduleId = id;
            this.moduleName = name;
            this.description = desc;
            this.maxLevel = maxLevel;
            this.baseUpgradeCost = upgradeCost;
            this.basePartSlots = partSlots;
            this.icon = icon;
        }
    }

    public static class NexusModuleDatabase
    {
        public const string OriginId = "Module_Origin";
        public const string GambleId = "Module_Gamble";
        public const string LimitId = "Module_Limit";
        public const string ClusterId = "Module_Cluster";
        public const string ArcanaId = "Module_Arcana";
        public const string SinId = "Module_Sin";

        private static readonly Dictionary<string, NexusModuleData> _allModules = new Dictionary<string, NexusModuleData>();

        static NexusModuleDatabase()
        {
            // 1. 오리진 모듈
            Register(new NexusModuleData(
                OriginId,
                "오리진",
                "기본 AP 공급 메커니즘.\n넥서스 칸 강화 시마다 매 턴 기본 행동력이 +1 증가합니다. (기본 파츠 슬롯 3개)",
                maxLevel: 4,
                upgradeCost: 140,
                partSlots: 3
            ));

            // 2. 갬블 모듈
            Register(new NexusModuleData(
                GambleId,
                "갬블",
                "매 턴 주사위를 굴려 나온 눈금 수의 합만큼 행동력을 획득합니다.\n확정 전 1회 재시도가 가능합니다. (기본 파츠 슬롯 2개)",
                maxLevel: 6,
                upgradeCost: 100,
                partSlots: 2
            ));

            // 3. 리미트 모듈 (블랙잭 방식)
            Register(new NexusModuleData(
                LimitId,
                "리미트",
                "매 턴 1~10의 숫자 카드를 뽑아 나온 숫자의 합의 일정 비율만큼 행동력을 증가시킵니다. (반올림)\n21 초과 시 카드 뽑기가 중단되고 행동력을 1만 얻습니다. (기본 파츠 슬롯 2개)",
                maxLevel: 4,
                upgradeCost: 150,
                partSlots: 2
            ));

            // 4. 클러스터 모듈 (포커 방식)
            Register(new NexusModuleData(
                ClusterId,
                "클러스터",
                "매 턴 4문양, 1~10의 카드 중 5장을 뽑아 포커 덱을 완성합니다.\n완성된 족보에 따라 행동력을 얻고, 각 카드의 문양에 따라 고유 전투 효과가 발동합니다. (기본 파츠 슬롯 2개)",
                maxLevel: 3,
                upgradeCost: 200,
                partSlots: 2
            ));

            // 5. 아르카나 모듈 (타로 카드 방식)
            Register(new NexusModuleData(
                ArcanaId,
                "아르카나",
                "매 턴 타로 카드를 뽑아 신비로운 효과와 행동력을 얻습니다.\n강화 수치(AC)에 따라 새로운 타로 카드가 해금되며, 1회 재시도가 가능합니다. (기본 파츠 슬롯 2개)",
                maxLevel: 3,
                upgradeCost: 200,
                partSlots: 2
            ));

            // 6. 씬 모듈 (7대 죄악 방식)
            Register(new NexusModuleData(
                SinId,
                "씬",
                "1턴에 한 번, 이후 3턴마다 무작위 7대 죄악이 발동되어 3턴간 유지됩니다.\n강력한 행동력과 혜택을 제공하지만 강제적인 패널티가 부여됩니다. (기본 파츠 슬롯 2개)",
                maxLevel: 4,
                upgradeCost: 200,
                partSlots: 2
            ));
        }

        public static void Register(NexusModuleData module)
        {
            if (module == null) return;
            _allModules[module.moduleId] = module;
        }

        public static NexusModuleData GetModule(string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId))
            {
                moduleId = OriginId;
            }
            _allModules.TryGetValue(moduleId, out var data);
            return data;
        }

        public static List<NexusModuleData> GetAllModules()
        {
            return new List<NexusModuleData>(_allModules.Values);
        }
    }
}
