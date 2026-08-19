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

            if (RunManager.Instance != null && RunManager.Instance.State != null)
            {
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
            }

            // 시너지 증표 및 전설 유물 시너지 가산 반영
            if (ResourceManager.Instance != null && ResourceManager.Instance.Relics != null)
            {
                foreach (var relic in ResourceManager.Instance.Relics)
                {
                    if (relic == null) continue;

                    if (relic.effectType == RelicEffectType.SynergyBadge)
                    {
                        if (!counts.ContainsKey(relic.targetSynergy)) counts[relic.targetSynergy] = 0;
                        counts[relic.targetSynergy] += Mathf.RoundToInt(relic.effectValue > 0 ? relic.effectValue : 1);
                    }
                    else if (relic.effectType == RelicEffectType.HeartOfMagitech)
                    {
                        AddSynergyCount(counts, SynergyType.ArchiumUnion, 2);
                    }
                    else if (relic.effectType == RelicEffectType.BeastSlayer)
                    {
                        AddSynergyCount(counts, SynergyType.Lionheart, 2);
                    }
                    else if (relic.effectType == RelicEffectType.SkyCross)
                    {
                        AddSynergyCount(counts, SynergyType.Elysium, 2);
                    }
                    else if (relic.effectType == RelicEffectType.GrimoireOfStars)
                    {
                        AddSynergyCount(counts, SynergyType.BlueTower, 2);
                    }
                    else if (relic.effectType == RelicEffectType.WorldTreeBranch)
                    {
                        AddSynergyCount(counts, SynergyType.Elvenwood, 3);
                    }
                    else if (relic.effectType == RelicEffectType.AllianceCrest)
                    {
                        AddSynergyCount(counts, SynergyType.Mirage, 2);
                    }
                    else if (relic.effectType == RelicEffectType.CheongwoonRelic)
                    {
                        AddSynergyCount(counts, SynergyType.Cheongwoon, 2);
                    }
                }
            }

            // [특성 훈련소] 선택 칸 시너지 보너스 반영
            if (TrainManager.IsInitialized)
            {
                var trainingBonuses = TrainManager.Instance.GetTraitTrainingSynergies();
                foreach (var syn in trainingBonuses)
                {
                    AddSynergyCount(counts, syn, 1);
                }
            }

            return counts;
        }

        private static void AddSynergyCount(Dictionary<SynergyType, int> counts, SynergyType type, int amount)
        {
            if (!counts.ContainsKey(type)) counts[type] = 0;
            counts[type] += amount;
        }

        /// <summary>현재 활성화(1단계 이상 조건 달성)된 시너지의 개수를 반환합니다.</summary>
        public static int GetActiveSynergiesCount()
        {
            int activeCount = 0;
            var activeSynergies = CalculateActiveSynergies();
            foreach (var kvp in activeSynergies)
            {
                var info = SynergyDatabase.GetInfo(kvp.Key);
                if (info != null && info.GetCurrentActiveTier(kvp.Value) != null)
                {
                    activeCount++;
                }
            }
            return activeCount;
        }

        public static bool HasOtherActiveFactionSynergies(SynergyType targetFaction)
        {
            var activeSynergies = CalculateActiveSynergies();
            foreach (var kvp in activeSynergies)
            {
                if (kvp.Key == targetFaction) continue;
                var info = SynergyDatabase.GetInfo(kvp.Key);
                if (info != null && info.isFaction && info.GetCurrentActiveTier(kvp.Value) != null)
                {
                    return true;
                }
            }
            return false;
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

            // [훈장함] 유물: 활성 시너지 4개 이상 시 공격력 +20%
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.MedalBox))
            {
                if (GetActiveSynergiesCount() >= 4)
                {
                    totalBonus += ResourceManager.Instance.GetRelicBonus(RelicEffectType.MedalBox);
                }
            }

            // [특성 훈련소 파츠] 다중 재능 연합기 & 단일 특성 집중기 보너스
            totalBonus += GetTraitTrainingCampStatMultiplier();

            return totalBonus;
        }

        public static float GetTotalSynergySpellPowerMultiplier()
        {
            float totalBonus = 0f;

            // [훈장함] 유물: 활성 시너지 4개 이상 시 주문력 +20%
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.MedalBox))
            {
                if (GetActiveSynergiesCount() >= 4)
                {
                    totalBonus += ResourceManager.Instance.GetRelicBonus(RelicEffectType.MedalBox);
                }
            }

            // [하늘 십자가] 유물: 다른 세력 시너지가 없을 때 주문력 +25%
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.SkyCross))
            {
                var active = CalculateActiveSynergies();
                if (active.TryGetValue(SynergyType.Elysium, out int elysiumCount) && elysiumCount >= 2 && !HasOtherActiveFactionSynergies(SynergyType.Elysium))
                {
                    totalBonus += 0.25f;
                }
            }

            // [특성 훈련소 파츠] 다중 재능 연합기 & 단일 특성 집중기 보너스
            totalBonus += GetTraitTrainingCampStatMultiplier();

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

            // [훈장함] 유물: 활성 시너지 4개 이상 시 체력 +20%
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.MedalBox))
            {
                if (GetActiveSynergiesCount() >= 4)
                {
                    totalBonus += ResourceManager.Instance.GetRelicBonus(RelicEffectType.MedalBox);
                }
            }

            // [화합의 깃발] 유물: 활성 시너지 없을 때 체력 +40%
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.UnityBanner))
            {
                if (GetActiveSynergiesCount() == 0)
                {
                    totalBonus += ResourceManager.Instance.GetRelicBonus(RelicEffectType.UnityBanner);
                }
            }

            // [특성 훈련소 파츠] 다중 재능 연합기 & 단일 특성 집중기 보너스
            totalBonus += GetTraitTrainingCampStatMultiplier();

            return totalBonus;
        }

        public static float GetTotalSynergyMentalMultiplier()
        {
            float totalBonus = 0f;

            // [화합의 깃발] 유물: 활성 시너지 없을 때 정신력 +40%
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.UnityBanner))
            {
                if (GetActiveSynergiesCount() == 0)
                {
                    totalBonus += ResourceManager.Instance.GetRelicBonus(RelicEffectType.UnityBanner);
                }
            }

            // [하늘 십자가] 유물: 다른 세력 시너지가 없을 때 정신력 +25%
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.SkyCross))
            {
                var active = CalculateActiveSynergies();
                if (active.TryGetValue(SynergyType.Elysium, out int elysiumCount) && elysiumCount >= 2 && !HasOtherActiveFactionSynergies(SynergyType.Elysium))
                {
                    totalBonus += 0.25f;
                }
            }

            // [특성 훈련소 파츠] 다중 재능 연합기 & 단일 특성 집중기 보너스
            totalBonus += GetTraitTrainingCampStatMultiplier();

            return totalBonus;
        }

        public static float GetTraitTrainingCampStatMultiplier()
        {
            float bonus = 0f;
            if (TrainManager.IsInitialized)
            {
                // [다중 재능 연합기] 활성화된 시너지 갯수 하나당 +4%
                if (TrainManager.Instance.HasPartEffectInAnyCar(TrainPartEffectType.MultiTalentCombiner))
                {
                    bonus += GetActiveSynergiesCount() * 0.04f;
                }

                // [단일 특성 집중기] 활성화된 가장 높은 시너지 수치%만큼 증가
                if (TrainManager.Instance.HasPartEffectInAnyCar(TrainPartEffectType.SingleTraitFocuser))
                {
                    var active = CalculateActiveSynergies();
                    int maxTierCount = 0;
                    foreach (var kvp in active)
                    {
                        var info = SynergyDatabase.GetInfo(kvp.Key);
                        if (info != null && info.GetCurrentActiveTier(kvp.Value) != null)
                        {
                            if (kvp.Value > maxTierCount) maxTierCount = kvp.Value;
                        }
                    }
                    bonus += maxTierCount * 0.01f;
                }
            }
            return bonus;
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

            // [화합의 깃발] 유물: 활성 시너지 없을 때 AP +4
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.UnityBanner))
            {
                if (GetActiveSynergiesCount() == 0)
                {
                    totalAP += 4;
                }
            }

            return totalAP;
        }
    }
}
