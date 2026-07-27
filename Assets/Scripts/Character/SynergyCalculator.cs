using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;
using TheLastArk.Managers;

namespace TheLastArk.Character
{
    public static class SynergyCalculator
    {
        public static Dictionary<SynergyType, int> CalculateActiveSynergies()
        {
            Dictionary<SynergyType, int> counts = new Dictionary<SynergyType, int>();

            if (RunManager.Instance == null || RunManager.Instance.State == null)
                return counts;

            var partyIDs = RunManager.Instance.State.partyDataIDs;
            string leaderID = RunManager.Instance.State.leaderCharacterID;

            CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("Characters");
            Dictionary<string, CharacterData> dataDict = new Dictionary<string, CharacterData>();
            foreach (var c in allCharacters)
            {
                if (c != null) dataDict[c.DataId] = c;
            }

            foreach (string charId in partyIDs)
            {
                if (!dataDict.TryGetValue(charId, out CharacterData data) || data == null)
                    continue;

                bool isLeader = (charId == leaderID);

                if (data.synergies == null) continue;

                foreach (var syn in data.synergies)
                {
                    if (!counts.ContainsKey(syn)) counts[syn] = 0;

                    // 기본 파티원 +1 카운트
                    counts[syn] += 1;

                    // 리더 시스템 효과 1: 리더 캐릭터는 해당 시너지 카운트 +1 추가 부여!
                    if (isLeader)
                    {
                        counts[syn] += 1;
                    }
                }
            }

            return counts;
        }

        public static float GetTotalSynergyAttackMultiplier()
        {
            float totalBonus = 0f;
            var activeSynergies = CalculateActiveSynergies();
            SynergyData[] allSynergies = Resources.LoadAll<SynergyData>("Synergies");

            foreach (var synData in allSynergies)
            {
                if (synData != null && activeSynergies.TryGetValue(synData.synergyType, out int count))
                {
                    var bonus = synData.GetActiveLevelBonus(count);
                    if (bonus != null)
                    {
                        totalBonus += bonus.attackBonusPercent;
                    }
                }
            }

            return totalBonus;
        }

        public static float GetTotalSynergyHpMultiplier()
        {
            float totalBonus = 0f;
            var activeSynergies = CalculateActiveSynergies();
            SynergyData[] allSynergies = Resources.LoadAll<SynergyData>("Synergies");

            foreach (var synData in allSynergies)
            {
                if (synData != null && activeSynergies.TryGetValue(synData.synergyType, out int count))
                {
                    var bonus = synData.GetActiveLevelBonus(count);
                    if (bonus != null)
                    {
                        totalBonus += bonus.hpBonusPercent;
                    }
                }
            }

            return totalBonus;
        }

        public static int GetTotalSynergyBonusAP()
        {
            int totalAP = 0;
            var activeSynergies = CalculateActiveSynergies();
            SynergyData[] allSynergies = Resources.LoadAll<SynergyData>("Synergies");

            foreach (var synData in allSynergies)
            {
                if (synData != null && activeSynergies.TryGetValue(synData.synergyType, out int count))
                {
                    var bonus = synData.GetActiveLevelBonus(count);
                    if (bonus != null)
                    {
                        totalAP += bonus.bonusAP;
                    }
                }
            }

            return totalAP;
        }
    }
}
