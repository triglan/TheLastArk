using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TheLastArk.Data;
using TheLastArk.Character;

namespace TheLastArk.Managers
{
    public class TrainManager : MonoBehaviour
    {
        private static TrainManager instance;
        public static bool IsInitialized => instance != null;
        public static TrainManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<TrainManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("TrainManager");
                        instance = go.AddComponent<TrainManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [Header("Train Cars (4 Slots)")]
        public TrainCar nexusCar;
        public TrainCar crewCar;
        public TrainCar optionalCar1;
        public TrainCar optionalCar2;

        // Legacy compatibility
        public TrainCar coreCar
        {
            get => nexusCar;
            set => nexusCar = value;
        }

        public List<TrainCar> additionalCars
        {
            get => new List<TrainCar> { crewCar, optionalCar1, optionalCar2 };
        }

        public int maxAdditionalCars = 3;

        // 통합 기차 체력
        public int maxTrainDurability = 100;
        public int currentTrainDurability = 100;

        public event Action OnDurabilityChanged;
        public event Action OnTrainCarsChanged;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDefaultTrain();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void InitializeDefaultTrain()
        {
            if (nexusCar == null || string.IsNullOrEmpty(nexusCar.carName))
            {
                nexusCar = new TrainCar("넥서스 칸", TrainCarType.Nexus, 0);
            }
            if (crewCar == null || string.IsNullOrEmpty(crewCar.carName))
            {
                crewCar = new TrainCar("승무원실", TrainCarType.CrewQuarters, 0);
            }
            if (optionalCar1 == null || string.IsNullOrEmpty(optionalCar1.carName))
            {
                optionalCar1 = new TrainCar("선택 칸 1 (미건설)", TrainCarType.Optional, 0);
            }
            if (optionalCar2 == null || string.IsNullOrEmpty(optionalCar2.carName))
            {
                optionalCar2 = new TrainCar("선택 칸 2 (미건설)", TrainCarType.Optional, 0);
            }

            currentTrainDurability = maxTrainDurability;
        }

        public List<TrainCar> GetAllCars()
        {
            return new List<TrainCar> { nexusCar, crewCar, optionalCar1, optionalCar2 };
        }

        public TrainCar GetCarOfType(TrainCarType carType)
        {
            if (nexusCar != null && nexusCar.carType == carType) return nexusCar;
            if (crewCar != null && crewCar.carType == carType) return crewCar;
            if (optionalCar1 != null && optionalCar1.carType == carType) return optionalCar1;
            if (optionalCar2 != null && optionalCar2.carType == carType) return optionalCar2;
            return null;
        }

        public bool HasPartEffectInAnyCar(TrainPartEffectType effectType)
        {
            if (nexusCar != null && nexusCar.HasPartEffect(effectType)) return true;
            if (crewCar != null && crewCar.HasPartEffect(effectType)) return true;
            if (optionalCar1 != null && optionalCar1.HasPartEffect(effectType)) return true;
            if (optionalCar2 != null && optionalCar2.HasPartEffect(effectType)) return true;
            return false;
        }

        // ── 넥서스 칸 모듈 & AP 계산 ──────────────────────────────────
        public bool TryChangeNexusModule(string newModuleId)
        {
            if (nexusCar == null) return false;
            var removedParts = nexusCar.ChangeNexusModule(newModuleId);
            if (removedParts != null && removedParts.Count > 0)
            {
                UI.NotificationManager.Instance?.ShowMessage($"모듈 변경으로 호환되지 않는 파츠 [{string.Join(", ", removedParts)}] 해제됨", Color.yellow);
            }
            return true;
        }

        public int GetNexusBaseAP()
        {
            if (nexusCar == null) return 4;

            if (nexusCar.installedModuleId == NexusModuleDatabase.GambleId)
            {
                bool hasChaos = nexusCar.HasPartEffect(TrainPartEffectType.GambleChaosDice);
                var config = Battle.GambleDiceManager.GetDiceConfig(nexusCar.level, hasChaos);
                float avgPerDie = (1f + config.sides) / 2f;
                return Mathf.RoundToInt(config.diceCount * avgPerDie);
            }

            if (nexusCar.installedModuleId == NexusModuleDatabase.LimitId)
            {
                float ratio = Battle.LimitCardManager.GetRatio(nexusCar.level);
                int threshold = Battle.LimitCardManager.GetThreshold(nexusCar);
                return Mathf.RoundToInt(threshold * 0.8f * ratio);
            }

            if (nexusCar.installedModuleId == NexusModuleDatabase.ClusterId)
            {
                return 4 + nexusCar.level; // 클러스터 기본 추정 AP (기본 4 + 강화 레벨)
            }

            if (nexusCar.installedModuleId == NexusModuleDatabase.ArcanaId)
            {
                return 4 + nexusCar.level; // 아르카나 기본 추정 AP
            }

            if (nexusCar.installedModuleId == NexusModuleDatabase.SinId)
            {
                return 6; // 씬 기본 추정 AP
            }

            int baseAP = 4 + nexusCar.level; // 오리진: 0강 4 ~ 4강 8
            return baseAP;
        }

        public int GetNexusTurnAP(int turnCount)
        {
            if (nexusCar == null) return 4;

            if (nexusCar.installedModuleId == NexusModuleDatabase.GambleId)
            {
                var roll = Battle.GambleDiceManager.Roll(nexusCar);
                return roll.totalGainedAP;
            }

            if (nexusCar.installedModuleId == NexusModuleDatabase.LimitId)
            {
                var cards = new List<int> { Battle.LimitCardManager.DrawCard(), Battle.LimitCardManager.DrawCard() };
                var eval = Battle.LimitCardManager.Evaluate(nexusCar, cards);
                return eval.totalGainedAP;
            }

            if (nexusCar.installedModuleId == NexusModuleDatabase.ClusterId)
            {
                // 가상 5장 드로우 평가
                var session = new Battle.ClusterDeckSession();
                session.StartNewTurnSession(nexusCar);
                var eval = Battle.ClusterCardManager.EvaluateHand(session.currentHand, nexusCar, simulateClover: true);
                return eval.totalGainedAP;
            }

            if (nexusCar.installedModuleId == NexusModuleDatabase.ArcanaId)
            {
                var pool = Battle.ArcanaCardManager.GetAvailableCardPool(nexusCar, null);
                var card = Battle.ArcanaCardManager.DrawRandomCard(pool, null);
                var eval = Battle.ArcanaCardManager.EvaluateDraw(card, nexusCar, null);
                return eval.gainedAP;
            }

            if (nexusCar.installedModuleId == NexusModuleDatabase.SinId)
            {
                var pool = Battle.SinModuleManager.GetAvailableSinPool(nexusCar);
                var sin = pool.Count > 0 ? pool[0] : Battle.SinType.Gluttony;
                return Battle.SinModuleManager.CalculateSinAP(sin, nexusCar);
            }

            int ap = GetNexusBaseAP();

            // [고에너지 모듈] 행동력 +2
            if (nexusCar.HasPartEffect(TrainPartEffectType.HighEnergyModule))
            {
                ap += 2;
            }

            // [에너지 가속 장치] 행동력 -2
            if (nexusCar.HasPartEffect(TrainPartEffectType.EnergyAccelerator))
            {
                ap -= 2;
            }

            // [기생형 생체 기관] 기본 -4, 매 턴 +1 (중첩)
            if (nexusCar.HasPartEffect(TrainPartEffectType.ParasiticOrgan))
            {
                ap -= 4;
                ap += Mathf.Max(0, turnCount - 1);
            }

            return Mathf.Max(1, ap);
        }

        // ── 승무원실 최대 인원 계산 ───────────────────────────────────
        public int GetMaxCrewCapacity()
        {
            int capacity = 8;
            if (crewCar != null)
            {
                capacity += crewCar.level; // +1 / +2 / +3 / +4 (최대 12명)

                // [개량형 침실] 최대 승무원 수 +4
                if (crewCar.HasPartEffect(TrainPartEffectType.ImprovedBedroom))
                {
                    capacity += 4;
                }

                // [개인 고급화 선실] 최대 승무원 수 -4
                if (crewCar.HasPartEffect(TrainPartEffectType.LuxuryCabin))
                {
                    capacity -= 4;
                }
            }
            return Mathf.Max(1, capacity);
        }

        public int GetCurrentCrewCount()
        {
            HashSet<string> uniqueCrew = new HashSet<string>();

            if (RunManager.Instance != null && RunManager.Instance.State != null && RunManager.Instance.State.partyDataIDs != null)
            {
                foreach (var id in RunManager.Instance.State.partyDataIDs)
                {
                    if (!string.IsNullOrEmpty(id)) uniqueCrew.Add(id);
                }
            }

            if (ResourceManager.Instance != null)
            {
                var allChars = Resources.LoadAll<CharacterData>("Characters");
                foreach (var c in allChars)
                {
                    if (c != null && !c.isEnemy && ResourceManager.Instance.GetCardCount(c.DataId) > 0)
                    {
                        uniqueCrew.Add(c.DataId);
                    }
                }
            }

            return uniqueCrew.Count;
        }

        public bool IsCrewAtMaxCapacity()
        {
            return GetCurrentCrewCount() >= GetMaxCrewCapacity();
        }

        // ── 선택 칸 1: 의무실 (전투 종료 후 체력/정신력 회복 & 소생) ──
        public void ApplyInfirmaryPostBattleEffect(List<BattleCharacter> party)
        {
            var infCar = GetCarOfType(TrainCarType.Infirmary);
            if (infCar == null || party == null) return;

            int baseHeal = 2 + infCar.level; // 0강: 2, 1강: 3, 2강: 4, 3강: 5
            if (infCar.HasPartEffect(TrainPartEffectType.NanobotProtocol))
            {
                baseHeal += 2; // [나노봇 프로토콜] 회복량 +2
            }

            bool hasMentalCare = infCar.HasPartEffect(TrainPartEffectType.MentalCareSystem);
            bool hasEmergencyRelief = infCar.HasPartEffect(TrainPartEffectType.EmergencyRelief);
            bool hasResuscitation = infCar.HasPartEffect(TrainPartEffectType.EmergencyResuscitation);

            foreach (var ally in party)
            {
                if (ally == null || ally.status == null) continue;

                // [긴급 소생술] 사망한 아군 체력 1로 소생
                if (hasResuscitation && ally.status.currentHp <= 0)
                {
                    ally.status.currentHp = 1f;
                    Debug.Log($"[의무실 - 긴급 소생술] {ally.characterName} 사망 상태에서 체력 1로 소생!");
                }

                if (ally.status.currentHp <= 0) continue;

                float finalHeal = baseHeal;
                // [긴급 구호소] 체력이 30% 이하인 아군에게 회복량 2배 적용
                if (hasEmergencyRelief && (ally.status.currentHp / ally.status.FinalMaxHp) <= 0.30f)
                {
                    finalHeal *= 2f;
                    Debug.Log($"[의무실 - 긴급 구호소] {ally.characterName} 체력 30% 이하 -> 회복량 2배 적용 ({finalHeal})");
                }

                // [멘탈 케어 시스템] 체력이 최대인 아군은 대신 정신력 회복
                if (hasMentalCare && ally.status.currentHp >= ally.status.FinalMaxHp)
                {
                    ally.status.currentMental = Mathf.Min(ally.status.FinalMaxMental, ally.status.currentMental + finalHeal);
                    Debug.Log($"[의무실 - 멘탈 케어] {ally.characterName} 체력 최대 -> 정신력 +{finalHeal} 회복!");
                }
                else
                {
                    ally.status.currentHp = Mathf.Min(ally.status.FinalMaxHp, ally.status.currentHp + finalHeal);
                    Debug.Log($"[의무실] {ally.characterName} 체력 +{finalHeal} 회복 (현재 HP: {ally.status.currentHp})");
                }

                if (ally.view != null) ally.view.UpdateVisual(ally.status);
            }
        }

        // ── 선택 칸 2: 전투 강화소 (공/주 버프 계산) ─────────────────
        public float GetCombatEnhancementBonus(BattleCharacter ally)
        {
            var combatCar = GetCarOfType(TrainCarType.CombatEnhancement);
            if (combatCar == null || combatCar.level <= 0) return 0f;

            float bonus = combatCar.level * 0.05f; // 1강: 5%, 2강: 10%, 3강: 15%, 4강: 20%, 5강: 25%

            if (ally != null && ally.status != null)
            {
                // [과적합 회로] 체력 50% 미만 시 효과 25% 추가 증가
                if (combatCar.HasPartEffect(TrainPartEffectType.OverfitCircuit) && (ally.status.currentHp / ally.status.FinalMaxHp) < 0.50f)
                {
                    bonus *= 1.25f;
                }

                // [완전한 준비] 체력 100% 시 효과 40% 추가 증가
                if (combatCar.HasPartEffect(TrainPartEffectType.FullPreparation) && ally.status.currentHp >= ally.status.FinalMaxHp)
                {
                    bonus *= 1.40f;
                }
            }

            return bonus;
        }

        // ── 선택 칸 3: 기도실 (치유량 증가 배율) ──────────────────────
        public float GetPrayerRoomHealMultiplier()
        {
            var prayerCar = GetCarOfType(TrainCarType.PrayerRoom);
            if (prayerCar == null) return 0f;

            return prayerCar.level switch
            {
                0 => 0.10f, // 0강: 10%
                1 => 0.13f, // 1강: 13% (+3%p)
                2 => 0.20f, // 2강: 20% (+7%p)
                3 => 0.35f, // 3강: 35% (+15%p)
                _ => 0.35f
            };
        }

        // ── 선택 칸 4: 특성 훈련소 (선택된 시너지 반환) ───────────────
        public List<SynergyType> GetTraitTrainingSynergies()
        {
            var traitCar = GetCarOfType(TrainCarType.TraitTrainingCamp);
            if (traitCar == null || traitCar.selectedSynergies == null) return new List<SynergyType>();
            return traitCar.selectedSynergies;
        }

        // ── 파츠 및 기차 칸에 의한 글로벌 스탯 배율 ──────────────────
        public float GetTrainBonusHpMultiplier()
        {
            float bonus = 0f;
            if (nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.EnergyAccelerator))
            {
                bonus += 0.25f; // [에너지 가속 장치] 체력 +25%
            }
            if (crewCar != null && crewCar.HasPartEffect(TrainPartEffectType.LuxuryCabin))
            {
                bonus += 0.20f; // [개인 고급화 선실] 체력 +20%
            }
            return bonus;
        }

        public float GetTrainBonusMentalMultiplier()
        {
            float bonus = 0f;
            if (crewCar != null && crewCar.HasPartEffect(TrainPartEffectType.LuxuryCabin))
            {
                bonus += 0.20f; // [개인 고급화 선실] 정신력 +20%
            }
            return bonus;
        }

        public float GetTrainBonusAttackMultiplier()
        {
            float bonus = 0f;
            if (nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.EnergyAccelerator))
            {
                bonus += 0.25f; // [에너지 가속 장치] 공격력 +25%
            }
            if (crewCar != null && crewCar.HasPartEffect(TrainPartEffectType.BondProof) && IsCrewAtMaxCapacity())
            {
                bonus += 0.10f; // [유대의 증표] 승무원 수 최대일 때 공 +10%
            }
            return bonus;
        }

        public float GetTrainBonusSpellPowerMultiplier()
        {
            float bonus = 0f;
            if (nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.EnergyAccelerator))
            {
                bonus += 0.25f; // [에너지 가속 장치] 주문력 +25%
            }
            if (crewCar != null && crewCar.HasPartEffect(TrainPartEffectType.BondProof) && IsCrewAtMaxCapacity())
            {
                bonus += 0.10f; // [유대의 증표] 승무원 수 최대일 때 주 +10%
            }
            return bonus;
        }

        // ── 선택 칸 건설 및 철거 ─────────────────────────────────────
        public bool TryBuildOptionalCar(int slotIndex, TrainCarType carType)
        {
            TrainCar targetCar = slotIndex == 1 ? optionalCar1 : (slotIndex == 2 ? optionalCar2 : null);
            if (targetCar == null) return false;

            int cost = GetDiscountedCost(TrainCar.OptionalCarBuildCost);
            if (ResourceManager.Instance != null && ResourceManager.Instance.TrySpendGold(cost))
            {
                targetCar.SetupCarDefaults(carType);
                OnTrainCarsChanged?.Invoke();
                Debug.Log($"[TrainManager] 선택 칸 {slotIndex}에 [{targetCar.carName}] 건설 완료! (-{cost}G)");
                return true;
            }

            return false;
        }

        public bool TryDismantleOptionalCar(int slotIndex)
        {
            TrainCar targetCar = slotIndex == 1 ? optionalCar1 : (slotIndex == 2 ? optionalCar2 : null);
            if (targetCar == null || !targetCar.IsBuiltOptionalCar) return false;

            int cost = GetDiscountedCost(TrainCar.OptionalCarDismantleCost);
            if (ResourceManager.Instance != null && ResourceManager.Instance.TrySpendGold(cost))
            {
                string oldName = targetCar.carName;
                targetCar.ResetCarToEmptyOptional();
                OnTrainCarsChanged?.Invoke();
                Debug.Log($"[TrainManager] 선택 칸 {slotIndex} [{oldName}] 철거 완료! (-{cost}G)");
                return true;
            }

            return false;
        }

        // ── 강화 및 파츠 장착 ────────────────────────────────────────
        public bool TryUpgradeCar(TrainCar car)
        {
            if (car == null || !car.CanUpgrade) return false;

            int cost = GetDiscountedCost(car.UpgradeCost);
            if (ResourceManager.Instance != null && ResourceManager.Instance.TrySpendGold(cost))
            {
                car.level++;
                OnTrainCarsChanged?.Invoke();
                Debug.Log($"[TrainManager] {car.carName} Lv.{car.level}로 강화 완료!");
                return true;
            }

            return false;
        }

        public bool TryBuyAndInstallPart(TrainCar car, string partId)
        {
            if (car == null || !car.CanInstallPart(partId)) return false;

            var partData = TrainPartDatabase.GetPart(partId);
            if (partData == null) return false;

            int cost = GetDiscountedCost(partData.cost);
            if (ResourceManager.Instance != null && ResourceManager.Instance.TrySpendGold(cost))
            {
                car.InstallPart(partId);
                OnTrainCarsChanged?.Invoke();
                Debug.Log($"[TrainManager] {car.carName}에 파츠 [{partData.partName}] 장착 완료!");
                return true;
            }

            return false;
        }

        public bool TryUninstallPart(TrainCar car, string partId)
        {
            if (car == null) return false;

            if (car.UninstallPart(partId))
            {
                OnTrainCarsChanged?.Invoke();
                Debug.Log($"[TrainManager] {car.carName}에서 파츠 [{partId}] 해제 완료!");
                return true;
            }

            return false;
        }

        // ── 내구도 ───────────────────────────────────────────────────
        public void DecreaseDurability(int amount)
        {
            if (amount <= 0) return;

            int old = currentTrainDurability;
            currentTrainDurability = Mathf.Max(0, currentTrainDurability - amount);

            if (old != currentTrainDurability)
            {
                OnDurabilityChanged?.Invoke();
            }
        }

        public void IncreaseDurability(int amount)
        {
            if (amount <= 0) return;

            int old = currentTrainDurability;
            currentTrainDurability = Mathf.Min(maxTrainDurability, currentTrainDurability + amount);

            if (old != currentTrainDurability)
            {
                OnDurabilityChanged?.Invoke();
            }
        }

        public int GetDiscountedCost(int baseCost)
        {
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.TrainDiscount))
            {
                float discount = ResourceManager.Instance.GetRelicBonus(RelicEffectType.TrainDiscount);
                return Mathf.RoundToInt(baseCost * (1f - discount));
            }
            return baseCost;
        }

        public int GetEffectiveCommLevel()
        {
            int baseLevel = nexusCar != null ? nexusCar.level : 0;
            if (ResourceManager.Instance != null && ResourceManager.Instance.HasRelicEffect(RelicEffectType.CommLevelBonus))
            {
                baseLevel += Mathf.RoundToInt(ResourceManager.Instance.GetRelicBonus(RelicEffectType.CommLevelBonus));
            }
            return baseLevel;
        }
    }
}
