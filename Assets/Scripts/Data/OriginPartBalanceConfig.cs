using System;
using UnityEngine;

namespace TheLastArk.Data
{
    /// <summary>
    /// Design numbers for the Origin module's five parts. Arrays indexed by part level
    /// (0 = 기본형 … 3 = 3강) hold the FINAL value at that level. Runtime state lives in TrainCar / BattleManager.
    /// </summary>
    [CreateAssetMenu(fileName = "OriginPartBalanceConfig", menuName = "TheLastArk/Train/Origin Part Balance")]
    public class OriginPartBalanceConfig : ScriptableObject
    {
        public const string ResourcePath = "Train/OriginPartBalanceConfig";
        public const int MaxPartLevel = 3;
        public const int LevelCount = MaxPartLevel + 1;

        [Serializable]
        public class PartCosts
        {
            public int craftCost;
            [Tooltip("Index 0 = 기본형→1강, 1 = 1강→2강, 2 = 2강→3강")]
            public int[] upgradeCosts = new int[MaxPartLevel];
        }

        [Header("Costs (gold)")]
        public PartCosts highEnergyCosts = new PartCosts();
        public PartCosts acceleratorCosts = new PartCosts();
        public PartCosts manipulatorCosts = new PartCosts();
        public PartCosts chanceCosts = new PartCosts();
        public PartCosts parasiticCosts = new PartCosts();

        [Header("고에너지 모듈")]
        public int[] highEnergyBonus = new int[LevelCount];

        [Header("에너지 가속 장치")]
        public int[] acceleratorDiceReduction = new int[LevelCount];
        public int[] acceleratorStatPercent = new int[LevelCount];

        [Header("숫자 조작 장치")]
        public int[] manipulatorUses = new int[LevelCount];

        [Header("기회 프로토콜")]
        public int[] chanceUses = new int[LevelCount];
        [Tooltip("true = 보관함의 사용 가능 주사위 전체 리롤, false = 선택한 주사위만 리롤")]
        public bool[] chanceRerollsAll = new bool[LevelCount];

        [Header("기생형 생체 기관")]
        public int[] parasiticDiceReduction = new int[LevelCount];
        public int[] parasiticStackCap = new int[LevelCount];

        public PartCosts GetCosts(string partId)
        {
            switch (partId)
            {
                case TrainPartDatabase.HighEnergyId: return highEnergyCosts;
                case TrainPartDatabase.EnergyAcceleratorId: return acceleratorCosts;
                case TrainPartDatabase.NumberManipulatorId: return manipulatorCosts;
                case TrainPartDatabase.ChanceProtocolId: return chanceCosts;
                case TrainPartDatabase.ParasiticOrganId: return parasiticCosts;
                default: return null;
            }
        }

        public int CraftCost(string partId) => GetCosts(partId)?.craftCost ?? 0;

        /// <summary>Cost to go from <paramref name="currentLevel"/> to the next level; -1 when already max.</summary>
        public int UpgradeCost(string partId, int currentLevel)
        {
            var costs = GetCosts(partId);
            if (costs?.upgradeCosts == null || currentLevel < 0 || currentLevel >= MaxPartLevel
                || currentLevel >= costs.upgradeCosts.Length) return -1;
            return costs.upgradeCosts[currentLevel];
        }

        public static int At(int[] values, int level)
        {
            if (values == null || values.Length == 0) return 0;
            return values[Mathf.Clamp(level, 0, values.Length - 1)];
        }

        public static bool At(bool[] values, int level)
        {
            if (values == null || values.Length == 0) return false;
            return values[Mathf.Clamp(level, 0, values.Length - 1)];
        }

        /// <summary>Effect text for a part at a level, built from the same numbers the game uses.</summary>
        public string Describe(string partId, int level)
        {
            switch (partId)
            {
                case TrainPartDatabase.HighEnergyId:
                    return $"턴 시작 시 처음 굴린 주사위 중 무작위 1개의 실제 값 +{At(highEnergyBonus, level)} (면 수 초과 가능)";
                case TrainPartDatabase.EnergyAcceleratorId:
                    return $"일반 주사위 -{At(acceleratorDiceReduction, level)}, 모든 아군 체력·공격력·주문력 +{At(acceleratorStatPercent, level)}%";
                case TrainPartDatabase.NumberManipulatorId:
                    return $"보관함 주사위 1개를 ±1 조정 (턴당 {At(manipulatorUses, level)}회, 주사위 소모 없음)";
                case TrainPartDatabase.ChanceProtocolId:
                    return At(chanceRerollsAll, level)
                        ? $"사용 가능한 주사위 전체 리롤 (턴당 {At(chanceUses, level)}회)"
                        : $"선택한 주사위 리롤 (턴당 {At(chanceUses, level)}회, 여러 개를 한 번에 1회로 처리)";
                case TrainPartDatabase.ParasiticOrganId:
                    return $"일반 주사위 -{At(parasiticDiceReduction, level)}, 첫 턴부터 매 턴 +1씩 누적 (최대 +{At(parasiticStackCap, level)}, 전투 종료 시 초기화)";
                default:
                    return "";
            }
        }
    }
}
