using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;

namespace TheLastArk.Battle
{
    public struct GambleDieResult
    {
        public int dieIndex;
        public int rolledValue;
        public int finalValue;
        public int sides;
        public bool wasAdjustedByMisfortune;
    }

    public class GambleRollResult
    {
        public List<GambleDieResult> dice = new List<GambleDieResult>();
        public int baseSum;
        public int finalDiceSum;
        public bool isPairBonusTriggered;
        public int pairBonusAP;
        public int totalGainedAP;
        public bool hasChaosDice;
        public bool hasMisfortunePreventer;
        public bool hasEnergyPair;
        public string summary;
    }

    public static class GambleDiceManager
    {
        public static int GetMaxRerolls(TrainCar nexusCar)
        {
            int rerolls = 1; // 기본 1회 재시도
            if (nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.GambleForesight))
            {
                rerolls += 1; // 단편 미래예지 모듈: +1회 (총 2회)
            }
            return rerolls;
        }

        public static (int diceCount, int sides) GetDiceConfig(int carLevel, bool hasChaosDice)
        {
            if (hasChaosDice)
            {
                // 혼돈의 주사위: 정이십면체(20면) 주사위 1개, 레벨당 +1면
                return (1, 20 + carLevel);
            }

            // 일반 주사위 구성
            // +1: 4면 1개, +2: 5면 1개, +3: 6면 1개
            // +4: 4면 2개, +5: 5면 2개, +6: 6면 2개
            // +0: 3면 1개 (기본)
            return carLevel switch
            {
                0 => (1, 3),
                1 => (1, 4),
                2 => (1, 5),
                3 => (1, 6),
                4 => (2, 4),
                5 => (2, 5),
                _ => (2, 6) // Lv.6 이상
            };
        }

        public static string GetDiceDescription(int carLevel, bool hasChaosDice)
        {
            var config = GetDiceConfig(carLevel, hasChaosDice);
            if (hasChaosDice)
            {
                return $"혼돈의 주사위 1개 ({config.sides}면)";
            }
            return $"{config.sides}면 주사위 {config.diceCount}개 ({config.diceCount}d{config.sides})";
        }

        public static GambleRollResult Roll(TrainCar nexusCar)
        {
            int level = nexusCar != null ? nexusCar.level : 0;
            bool hasChaos = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.GambleChaosDice);
            bool hasMisfortune = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.GambleMisfortunePreventer);
            bool hasEnergyPair = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.GambleEnergyPair);

            var config = GetDiceConfig(level, hasChaos);
            var result = new GambleRollResult
            {
                hasChaosDice = hasChaos,
                hasMisfortunePreventer = hasMisfortune,
                hasEnergyPair = hasEnergyPair
            };

            int baseSum = 0;
            int finalSum = 0;
            Dictionary<int, int> valueCounts = new Dictionary<int, int>();

            for (int i = 0; i < config.diceCount; i++)
            {
                int rawRoll = UnityEngine.Random.Range(1, config.sides + 1);
                int finalVal = rawRoll;
                bool adjusted = false;

                // [불운 방지기] 주사위 눈금 1이 나왔을 때 2로 재설정
                if (hasMisfortune && rawRoll == 1)
                {
                    finalVal = 2;
                    adjusted = true;
                }

                baseSum += rawRoll;
                finalSum += finalVal;

                if (!valueCounts.ContainsKey(finalVal)) valueCounts[finalVal] = 0;
                valueCounts[finalVal]++;

                result.dice.Add(new GambleDieResult
                {
                    dieIndex = i + 1,
                    rolledValue = rawRoll,
                    finalValue = finalVal,
                    sides = config.sides,
                    wasAdjustedByMisfortune = adjusted
                });
            }

            result.baseSum = baseSum;
            result.finalDiceSum = finalSum;

            // [에너지쌍 생성기] 같은 숫자 2개(더블)가 나오면 AP +2
            bool pairTriggered = false;
            if (hasEnergyPair && config.diceCount >= 2)
            {
                foreach (var kvp in valueCounts)
                {
                    if (kvp.Value >= 2)
                    {
                        pairTriggered = true;
                        break;
                    }
                }
            }

            result.isPairBonusTriggered = pairTriggered;
            result.pairBonusAP = pairTriggered ? 2 : 0;
            result.totalGainedAP = Mathf.Max(1, finalSum + result.pairBonusAP);

            // Summary text
            List<string> rollStrings = new List<string>();
            foreach (var d in result.dice)
            {
                if (d.wasAdjustedByMisfortune)
                {
                    rollStrings.Add($"[{d.rolledValue}→<b>{d.finalValue}</b>]");
                }
                else
                {
                    rollStrings.Add($"[<b>{d.finalValue}</b>]");
                }
            }

            string sumStr = string.Join(" + ", rollStrings);
            if (result.dice.Count > 1) sumStr += $" = {result.finalDiceSum}";
            if (pairTriggered) sumStr += $" <color=yellow>(+에너지쌍 2AP)</color>";
            sumStr += $" ➔ <b>총 {result.totalGainedAP} AP</b>";

            result.summary = sumStr;
            return result;
        }
    }
}
