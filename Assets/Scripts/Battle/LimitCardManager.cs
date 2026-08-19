using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;

namespace TheLastArk.Battle
{
    public class LimitCardResult
    {
        public List<int> drawnCards = new List<int>();
        public int currentSum;
        public int threshold; // 21 (기본) 또는 28 (완전수 모듈)
        public bool isBust;
        public float ratio;
        public int baseCalculatedAP;
        public int bonusAP;
        public int totalGainedAP;
        public bool isExact21Triggered;
        public bool isPerfectNumberFreeSkillTriggered; // 합이 6 또는 28
        public bool isPerfectNumber6Triggered; // 합이 6 (+5 AP)
        public bool isInversionTriggered; // 합이 1 (+15 AP)
        public string summary;
    }

    public static class LimitCardManager
    {
        public static int GetThreshold(TrainCar nexusCar)
        {
            if (nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.LimitPerfectNumberModule))
            {
                return 28; // 완전수 모듈: 28
            }
            return 21; // 기본 21
        }

        public static float GetRatio(int level)
        {
            return level switch
            {
                0 => 0.20f,
                1 => 0.30f,
                2 => 0.40f,
                3 => 0.50f,
                _ => 0.60f // Lv.4 이상
            };
        }

        public static int DrawCard()
        {
            return UnityEngine.Random.Range(1, 11); // 1~10 무작위 숫자 카드
        }

        public static LimitCardResult Evaluate(TrainCar nexusCar, List<int> drawnCards)
        {
            int level = nexusCar != null ? nexusCar.level : 0;
            int threshold = GetThreshold(nexusCar);
            float ratio = GetRatio(level);

            bool hasMediator = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.LimitProbabilityMediator);
            bool hasPerfectMeter = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.LimitPerfectMeter);
            bool hasPerfectNumber = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.LimitPerfectNumberModule);
            bool hasInversion = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.LimitInversionConverter);

            int sum = 0;
            if (drawnCards != null)
            {
                foreach (int c in drawnCards) sum += c;
            }

            bool isBust = sum > threshold;

            var result = new LimitCardResult
            {
                drawnCards = drawnCards != null ? new List<int>(drawnCards) : new List<int>(),
                currentSum = sum,
                threshold = threshold,
                isBust = isBust,
                ratio = ratio
            };

            int calculatedAP = 0;
            int bonusAP = 0;

            if (isBust)
            {
                // 버스트 시 기본 행동력 1
                calculatedAP = 1;

                // +4 강화 효과: 21 초과 시 얻는 행동력 +1
                if (level >= 4)
                {
                    bonusAP += 1;
                }

                // [확률보정중재기] 초과 시 행동력 +2
                if (hasMediator)
                {
                    bonusAP += 2;
                }

                result.baseCalculatedAP = calculatedAP;
                result.bonusAP = bonusAP;
                result.totalGainedAP = Mathf.Max(1, calculatedAP + bonusAP);

                string bonusDesc = bonusAP > 0 ? $" <color=yellow>(버스트 보정 +{bonusAP}AP)</color>" : "";
                result.summary = $"<color=#FF4444>💥 버스트 초과! (합계 {sum} > 기준치 {threshold})</color> ➔ <b>총 {result.totalGainedAP} AP</b>{bonusDesc}";
            }
            else
            {
                // 비버스트 정상 계산: 반올림 적용
                calculatedAP = Mathf.RoundToInt(sum * ratio);

                // [완벽측정기] 정확히 21일 때 행동력 +1 및 이번 턴 스킬 유지
                if (hasPerfectMeter && sum == 21)
                {
                    bonusAP += 1;
                    result.isExact21Triggered = true;
                }

                // [완전수 모듈] 수치가 6 또는 28일 때 첫 스킬 비용 0, 수치가 6일 때 행동력 +5
                if (hasPerfectNumber)
                {
                    if (sum == 6 || sum == 28)
                    {
                        result.isPerfectNumberFreeSkillTriggered = true;
                    }
                    if (sum == 6)
                    {
                        bonusAP += 5;
                        result.isPerfectNumber6Triggered = true;
                    }
                }

                // [완전반전변환기] 수치가 1일 때 행동력 +15
                if (hasInversion && sum == 1)
                {
                    bonusAP += 15;
                    result.isInversionTriggered = true;
                }

                result.baseCalculatedAP = calculatedAP;
                result.bonusAP = bonusAP;
                result.totalGainedAP = Mathf.Max(1, calculatedAP + bonusAP);

                string cardList = string.Join(" + ", result.drawnCards);
                string bonusDesc = "";
                if (result.isExact21Triggered) bonusDesc += " <color=#00FFCC>[완벽측정기 +1AP & 유지]</color>";
                if (result.isPerfectNumber6Triggered) bonusDesc += " <color=#FFD700>[완전수 6! +5AP & 첫스킬 0코]</color>";
                else if (result.isPerfectNumberFreeSkillTriggered) bonusDesc += " <color=#FFD700>[완전수 28! 첫스킬 0코]</color>";
                if (result.isInversionTriggered) bonusDesc += " <color=#FF66FF>[완전반전 1! +15AP]</color>";

                result.summary = $"카드: {cardList} = <b>{sum}</b> (비율 {(ratio * 100):F0}%) ➔ <b>총 {result.totalGainedAP} AP</b>{bonusDesc}";
            }

            return result;
        }
    }
}
