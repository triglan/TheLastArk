using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Data
{
    public enum TrainPartEffectType
    {
        // ── 넥서스 파츠 (오리진) ──
        HighEnergyModule,             // 고에너지 모듈: 행동력 +2
        EnergyAccelerator,            // 에너지 가속 장치: 행동력 -2, 모든 아군 체/공/주 +25%
        EnergyStorage,                // 에너지 저장소: 미사용 행동력 최대 3 이월
        OverloadModule,               // 과부하 모듈: 다음 턴 행동력 최대 5 미리 사용
        ParasiticOrgan,               // 기생형 생체 기관: 행동력 -4, 매 턴 행동력 +1 (중첩)

        // ── 넥서스 파츠 (갬블) ──
        GambleForesight,              // 단편 미래예지 모듈: 주사위 재시도 횟수 +1
        GambleChaosDice,              // 혼돈의 주사위: 정이십면체의 주사위 1개로 변경, 강화 레벨당 주사위의 면 +1
        GambleEnergyPair,             // 에너지쌍 생성기: 주사위에서 같은 숫자 2개가 나오면, 얻는 행동력 +2
        GambleMisfortunePreventer,    // 불운 방지기: 주사위 눈금 1이 나왔을 때, 2로 재설정

        // ── 넥서스 파츠 (리미트) ──
        LimitProbabilityMediator,     // 확률보정중재기: 수치가 21을 초과했을 때 얻는 행동력 +2
        LimitPerfectMeter,            // 완벽측정기: 수치가 정확히 21일 때 얻는 행동력 +1, 이번 턴 모든 스킬에 유지 부여
        LimitPerfectNumberModule,     // 완전수 모듈: 기준치 28로 변경, 수치 6/28일 때 첫 스킬 비용 0, 수치 6일 때 행동력 +5
        LimitInversionConverter,      // 완전반전변환기: 수치가 1일 때 얻는 행동력 +15

        // ── 넥서스 파츠 (클러스터) ──
        ClusterWildCard,              // 와일드 카드: 어떤 문양/숫자로도 취급 가능한 와일드 카드 1장을 덱에 추가
        ClusterFateReset,             // 운명 재설정 모듈: 모든 카드를 다시 뽑을 수 있는 기능 해금 (턴당 1회)
        ClusterFateReverser,          // 운명 역행기: 숫자 10과 1을 연결되는 것으로 취급 (스트레이트)
        ClusterPatternAmplifier,      // 문양고정증폭기: 덱 완성 시 얻는 행동력 +1

        // ── 넥서스 파츠 (아르카나) ──
        ArcanaSun,                    // 아르카나: 태양 (카드 풀에 태양 추가: 행동력 +(5+AC), 이번 턴 모든 스킬에 유지 부여)
        ArcanaMoon,                   // 아르카나: 달 (카드 풀에 달 추가: 행동력 +(5+AC), 정신력 50% 이상 아군 수 비례 AP 추가 및 정신력 회복)
        ArcanaStar,                   // 아르카나: 별 (카드 풀에 별 추가: 행동력 +(4+AC), 별자리 효과 획득: 스킬 3회당 AP +1)
        ArcanaWorld,                  // 아르카나: 세계 (카드 풀에 세계 추가: 행동력 +10, 모든 아군 상태이상 완전 제거)

        // ── 넥서스 파츠 (씬) ──
        SinPride,                     // 대죄: 오만 (죄악 풀에 오만 추가: 0 AP, 모든 스킬 0코/유지, 스킬당 무작위 아군 정신력 -6)
        SinIndulgence,                // 면죄부 모듈 (전투당 1회 죄악 부가효과 정화 및 다음 턴 새로운 죄악 재추첨)
        SinGreaterEvil,               // 거악 프로토콜 (오만을 제외한 모든 죄악의 행동력과 부가효과 100% 증가)
        SinMartyrVow,                 // 순교자의 서약 (오만을 제외한 모든 죄악의 효과와 패널티를 극단적 순교자 형태로 변경)

        // ── 승무원실 파츠 ──
        BondProof,                    // 유대의 증표: 승무원 최대 시 모든 승무원 공/주 +10%
        ImprovedBedroom,              // 개량형 침실: 최대 승무원 수 +4
        LuxuryCabin,                  // 개인 고급화 선실: 최대 승무원 수 -4, 모든 승무원 체/정 +20%

        // ── 의무실 파츠 ──
        NanobotProtocol,              // 나노봇 프로토콜: 체력 회복량 +2
        MentalCareSystem,             // 멘탈 케어 시스템: 체력이 최대인 아군은 대신 정신력을 회복
        EmergencyRelief,              // 긴급 구호소: 체력이 30% 이하인 아군에게 회복량 2배 적용
        EmergencyResuscitation,       // 긴급 소생술: 사망한 아군이 있을 때, 체력 1로 소생

        // ── 전투 강화소 파츠 ──
        OverfitCircuit,               // 과적합 회로: 전투 시 체력이 50% 미만인 아군에게 효과 25% 증가
        FullPreparation,              // 완전한 준비: 전투 시 체력이 100%인 아군에게 효과 40% 증가
        AdaptiveShieldGenerator,      // 적응형 보호막 생성기: 전투 시작 시 모든 아군이 잃은 체력의 20%만큼 1턴간 보호막 생성
        ContinuousCombatCatalyst,     // 연속 전투 촉진기: 한 전투에서 스킬을 7번 사용할 때 마다, 행동력 +2

        // ── 기도실 파츠 ──
        FountainOfLight,              // 빛의 분수대: 단일 치유 시 50%만큼 최저 체력 아군 치유
        SkyBlessingProtocol,          // 하늘 가호 프로토콜: 치유로 체력 100% 달성 시 해당 턴 공/주 +20%
        CorruptionModule,             // 타락 모듈: 치유 스킬을 적에게 사용 가능 (50% 마법 피해)
        LoavesAndFishesModule,        // 물고기와 빵 모듈: 본인 치유 시 20%만큼 모든 아군 광역 치유

        // ── 특성 훈련소 파츠 ──
        FateStackingModule,           // 운명 중첩 모듈: 시너지를 중복해서 선택 가능
        TraitExpander,                // 특성 확장기: 선택 가능한 시너지 +1 (중복 불가)
        MultiTalentCombiner,          // 다중 재능 연합기: 활성화된 시너지 1개당 전 아군 체/정/공/주 +4%
        SingleTraitFocuser             // 단일 특성 집중기: (가장 높은 활성화 시너지 수치)%만큼 전 아군 체/정/공/주 증가
    }

    [System.Serializable]
    public class TrainPartData
    {
        public string partId;
        public string partName;
        public string description;
        public int cost;
        public TrainCarType targetCarType;
        public string targetModuleId = "";
        public TrainPartEffectType effectType;
        public Sprite icon;

        public TrainPartData(string id, string name, string desc, int cost, TrainCarType carType, TrainPartEffectType effect, string moduleId = "")
        {
            this.partId = id;
            this.partName = name;
            this.description = desc;
            this.cost = cost;
            this.targetCarType = carType;
            this.effectType = effect;
            this.targetModuleId = moduleId;
        }
    }

    public static class TrainPartDatabase
    {
        private static readonly Dictionary<string, TrainPartData> _allParts = new Dictionary<string, TrainPartData>();

        static TrainPartDatabase()
        {
            // ── 1. 넥서스 파츠 - 오리진 모듈 전용 (5종) ──
            Register(new TrainPartData(
                "Part_HighEnergy",
                "고에너지 모듈",
                "행동력이 +2 증가합니다.",
                80,
                TrainCarType.Nexus,
                TrainPartEffectType.HighEnergyModule,
                NexusModuleDatabase.OriginId
            ));

            Register(new TrainPartData(
                "Part_EnergyAccelerator",
                "에너지 가속 장치",
                "행동력이 -2 감소하지만, 모든 아군의 체력, 공격력, 주문력이 +25% 증가합니다.",
                80,
                TrainCarType.Nexus,
                TrainPartEffectType.EnergyAccelerator,
                NexusModuleDatabase.OriginId
            ));

            Register(new TrainPartData(
                "Part_EnergyStorage",
                "에너지 저장소",
                "턴 종료 시 사용하지 않은 행동력이 최대 3까지 다음 턴의 추가 행동력으로 이동됩니다.",
                50,
                TrainCarType.Nexus,
                TrainPartEffectType.EnergyStorage,
                NexusModuleDatabase.OriginId
            ));

            Register(new TrainPartData(
                "Part_Overload",
                "과부하 모듈",
                "다음 턴의 행동력을 최대 5까지 미리 사용 가능합니다. 사용한 만큼 다음 턴에 행동력이 잠기며, 해당 턴에는 과부하 사용이 불가합니다.",
                50,
                TrainCarType.Nexus,
                TrainPartEffectType.OverloadModule,
                NexusModuleDatabase.OriginId
            ));

            Register(new TrainPartData(
                "Part_ParasiticOrgan",
                "기생형 생체 기관",
                "기본 행동력이 -4 감소하지만, 전투 중 매 턴마다 행동력이 +1씩 영구히 중첩 증가합니다.",
                100,
                TrainCarType.Nexus,
                TrainPartEffectType.ParasiticOrgan,
                NexusModuleDatabase.OriginId
            ));

            // ── 1-2. 넥서스 파츠 - 갬블 모듈 전용 (4종) ──
            Register(new TrainPartData(
                "Part_Gamble_Foresight",
                "단편 미래예지 모듈",
                "주사위 재시도 횟수가 +1회 증가합니다. (턴당 총 2회 재시도)",
                80,
                TrainCarType.Nexus,
                TrainPartEffectType.GambleForesight,
                NexusModuleDatabase.GambleId
            ));

            Register(new TrainPartData(
                "Part_Gamble_ChaosDice",
                "혼돈의 주사위",
                "정이십면체(20면) 주사위 1개로 변경되며, 강화 레벨당 주사위의 면이 +1 추가됩니다.",
                100,
                TrainCarType.Nexus,
                TrainPartEffectType.GambleChaosDice,
                NexusModuleDatabase.GambleId
            ));

            Register(new TrainPartData(
                "Part_Gamble_EnergyPair",
                "에너지쌍 생성기",
                "주사위에서 같은 숫자 2개가 나오면, 획득하는 행동력이 +2 증가합니다.",
                80,
                TrainCarType.Nexus,
                TrainPartEffectType.GambleEnergyPair,
                NexusModuleDatabase.GambleId
            ));

            Register(new TrainPartData(
                "Part_Gamble_MisfortunePreventer",
                "불운 방지기",
                "주사위 눈금 1이 나왔을 때, 2로 재설정됩니다.",
                100,
                TrainCarType.Nexus,
                TrainPartEffectType.GambleMisfortunePreventer,
                NexusModuleDatabase.GambleId
            ));

            // ── 1-3. 넥서스 파츠 - 리미트 모듈 전용 (4종) ──
            Register(new TrainPartData(
                "Part_Limit_ProbabilityMediator",
                "확률보정중재기",
                "수치가 기준치(21/28)를 초과했을 때(버스트), 얻는 행동력이 +2 증가합니다.",
                100,
                TrainCarType.Nexus,
                TrainPartEffectType.LimitProbabilityMediator,
                NexusModuleDatabase.LimitId
            ));

            Register(new TrainPartData(
                "Part_Limit_PerfectMeter",
                "완벽측정기",
                "수치가 정확히 21일 때, 얻는 행동력 +1 및 이번 턴 모든 스킬에 유지 효과를 부여합니다.",
                100,
                TrainCarType.Nexus,
                TrainPartEffectType.LimitPerfectMeter,
                NexusModuleDatabase.LimitId
            ));

            Register(new TrainPartData(
                "Part_Limit_PerfectNumberModule",
                "완전수 모듈",
                "최대로 얻을 수 있는 수치 및 패널티 기준이 28로 변경됩니다.\n수치가 6 또는 28일 때 처음 사용하는 스킬의 비용이 0이 되며, 수치가 6일 때 행동력을 +5 추가 획득합니다.",
                70,
                TrainCarType.Nexus,
                TrainPartEffectType.LimitPerfectNumberModule,
                NexusModuleDatabase.LimitId
            ));

            Register(new TrainPartData(
                "Part_Limit_InversionConverter",
                "완전반전변환기",
                "수치가 1일 때(첫 카드 1 확정), 얻는 행동력이 +15 증가합니다.",
                70,
                TrainCarType.Nexus,
                TrainPartEffectType.LimitInversionConverter,
                NexusModuleDatabase.LimitId
            ));

            // ── 1-4. 넥서스 파츠 - 클러스터 모듈 전용 (4종) ──
            Register(new TrainPartData(
                "Part_Cluster_WildCard",
                "와일드 카드",
                "어떤 문양과 숫자로도 취급할 수 있는 와일드 카드 1장을 덱에 추가합니다.",
                70,
                TrainCarType.Nexus,
                TrainPartEffectType.ClusterWildCard,
                NexusModuleDatabase.ClusterId
            ));

            Register(new TrainPartData(
                "Part_Cluster_FateReset",
                "운명 재설정 모듈",
                "모든 카드를 다시 뽑을 수 있는 기능 해금, 턴마다 1번 사용 가능합니다.",
                120,
                TrainCarType.Nexus,
                TrainPartEffectType.ClusterFateReset,
                NexusModuleDatabase.ClusterId
            ));

            Register(new TrainPartData(
                "Part_Cluster_FateReverser",
                "운명 역행기",
                "숫자 10과 1을 연결되는 것으로 취급합니다. (스트레이트 완성 용이)",
                150,
                TrainCarType.Nexus,
                TrainPartEffectType.ClusterFateReverser,
                NexusModuleDatabase.ClusterId
            ));

            Register(new TrainPartData(
                "Part_Cluster_PatternAmplifier",
                "문양고정증폭기",
                "덱 완성 시 얻는 행동력이 +1 증가합니다.",
                100,
                TrainCarType.Nexus,
                TrainPartEffectType.ClusterPatternAmplifier,
                NexusModuleDatabase.ClusterId
            ));

            // ── 1-5. 넥서스 파츠 - 아르카나 모듈 전용 (4종) ──
            Register(new TrainPartData(
                "Part_Arcana_Sun",
                "아르카나: 태양",
                "타로 카드 풀에 [태양]을 추가합니다.\n(태양: 행동력 +(5+AC), 이번 턴 모든 스킬에 유지 부여)",
                70,
                TrainCarType.Nexus,
                TrainPartEffectType.ArcanaSun,
                NexusModuleDatabase.ArcanaId
            ));

            Register(new TrainPartData(
                "Part_Arcana_Moon",
                "아르카나: 달",
                "타로 카드 풀에 [달]을 추가합니다.\n(달: 행동력 +(5+AC), 정신력 50% 이상 아군 수만큼 AP 추가, 정신력 50% 미만 아군 정신력 15% 회복)",
                70,
                TrainCarType.Nexus,
                TrainPartEffectType.ArcanaMoon,
                NexusModuleDatabase.ArcanaId
            ));

            Register(new TrainPartData(
                "Part_Arcana_Star",
                "아르카나: 별",
                "타로 카드 풀에 [별]을 추가합니다.\n(별: 행동력 +(4+AC), 별자리 효과 획득: 스킬 3회 사용 시마다 행동력 +1)",
                70,
                TrainCarType.Nexus,
                TrainPartEffectType.ArcanaStar,
                NexusModuleDatabase.ArcanaId
            ));

            Register(new TrainPartData(
                "Part_Arcana_World",
                "아르카나: 세계",
                "타로 카드 풀에 [세계]를 추가합니다.\n(세계: 행동력 +10, 모든 아군의 모든 상태이상 제거)",
                70,
                TrainCarType.Nexus,
                TrainPartEffectType.ArcanaWorld,
                NexusModuleDatabase.ArcanaId
            ));

            // ── 1-6. 넥서스 파츠 - 씬 모듈 전용 (4종) ──
            Register(new TrainPartData(
                "Part_Sin_Pride",
                "대죄: 오만",
                "죄악 풀에 [오만의 죄]가 등장할 수 있게 됩니다.\n(오만: 행동력 +0, 이번 턴 모든 스킬 비용 0 고정/유지, 스킬당 무작위 아군 정신력 -6)",
                100,
                TrainCarType.Nexus,
                TrainPartEffectType.SinPride,
                NexusModuleDatabase.SinId
            ));

            Register(new TrainPartData(
                "Part_Sin_Indulgence",
                "면죄부 모듈",
                "죄악의 행동력을 제외한 부가 효과를 제거하고, 다음 턴에 죄악을 다시 뽑는 기능이 추가됩니다. (전투당 1회)",
                100,
                TrainCarType.Nexus,
                TrainPartEffectType.SinIndulgence,
                NexusModuleDatabase.SinId
            ));

            Register(new TrainPartData(
                "Part_Sin_GreaterEvil",
                "거악 프로토콜",
                "오만을 제외한 모든 죄악이 제공하는 행동력과 부가 효과를 100% 증가시킵니다. (수치 2배)",
                100,
                TrainCarType.Nexus,
                TrainPartEffectType.SinGreaterEvil,
                NexusModuleDatabase.SinId
            ));

            Register(new TrainPartData(
                "Part_Sin_MartyrVow",
                "순교자의 서약",
                "오만을 제외한 모든 죄악의 패널티와 효과를 극단적인 순교자 형태로 변경시킵니다.",
                300,
                TrainCarType.Nexus,
                TrainPartEffectType.SinMartyrVow,
                NexusModuleDatabase.SinId
            ));

            // ── 2. 승무원실 파츠 (3종) ──
            Register(new TrainPartData(
                "Part_BondProof",
                "유대의 증표",
                "승무원 수가 최대 수용량일 때, 모든 승무원의 공격력과 주문력이 +10% 증가합니다.",
                70,
                TrainCarType.CrewQuarters,
                TrainPartEffectType.BondProof
            ));

            Register(new TrainPartData(
                "Part_ImprovedBedroom",
                "개량형 침실",
                "최대 승무원 보유 수가 +4명 증가합니다.",
                60,
                TrainCarType.CrewQuarters,
                TrainPartEffectType.ImprovedBedroom
            ));

            Register(new TrainPartData(
                "Part_LuxuryCabin",
                "개인 고급화 선실",
                "최대 승무원 수가 -4명 감소하지만, 모든 승무원의 체력과 정신력이 +20% 증가합니다.",
                70,
                TrainCarType.CrewQuarters,
                TrainPartEffectType.LuxuryCabin
            ));

            // ── 3. 의무실 파츠 (4종) ──
            Register(new TrainPartData(
                "Part_NanobotProtocol",
                "나노봇 프로토콜",
                "전투 종료 후 체력 회복량이 +2 증가합니다.",
                70,
                TrainCarType.Infirmary,
                TrainPartEffectType.NanobotProtocol
            ));

            Register(new TrainPartData(
                "Part_MentalCareSystem",
                "멘탈 케어 시스템",
                "전투 종료 후 체력이 최대인 아군은 대신 정신력을 회복합니다.",
                100,
                TrainCarType.Infirmary,
                TrainPartEffectType.MentalCareSystem
            ));

            Register(new TrainPartData(
                "Part_EmergencyRelief",
                "긴급 구호소",
                "전투 종료 후 체력이 30% 이하인 아군에게 회복량이 2배로 적용됩니다.",
                120,
                TrainCarType.Infirmary,
                TrainPartEffectType.EmergencyRelief
            ));

            Register(new TrainPartData(
                "Part_EmergencyResuscitation",
                "긴급 소생술",
                "전투 종료 후 사망한 아군이 있을 때, 체력 1로 소생시킵니다.",
                150,
                TrainCarType.Infirmary,
                TrainPartEffectType.EmergencyResuscitation
            ));

            // ── 4. 전투 강화소 파츠 (4종) ──
            Register(new TrainPartData(
                "Part_OverfitCircuit",
                "과적합 회로",
                "전투 시 체력이 50% 미만인 아군에게 전투 강화소 효과가 25% 추가 증가합니다.",
                80,
                TrainCarType.CombatEnhancement,
                TrainPartEffectType.OverfitCircuit
            ));

            Register(new TrainPartData(
                "Part_FullPreparation",
                "완전한 준비",
                "전투 시 체력이 100%인 아군에게 전투 강화소 효과가 40% 추가 증가합니다.",
                100,
                TrainCarType.CombatEnhancement,
                TrainPartEffectType.FullPreparation
            ));

            Register(new TrainPartData(
                "Part_AdaptiveShieldGenerator",
                "적응형 보호막 생성기",
                "전투 시작 시, 모든 아군이 잃은 체력의 20%만큼 1턴간 유지되는 보호막을 생성합니다.",
                120,
                TrainCarType.CombatEnhancement,
                TrainPartEffectType.AdaptiveShieldGenerator
            ));

            Register(new TrainPartData(
                "Part_ContinuousCombatCatalyst",
                "연속 전투 촉진기",
                "한 전투에서 스킬을 7번 사용할 때마다, 행동력을 즉시 +2 획득합니다.",
                120,
                TrainCarType.CombatEnhancement,
                TrainPartEffectType.ContinuousCombatCatalyst
            ));

            // ── 5. 기도실 파츠 (4종) ──
            Register(new TrainPartData(
                "Part_FountainOfLight",
                "빛의 분수대",
                "단일 대상 치유 스킬 사용 시, 치유량의 50%만큼 가장 체력이 낮은 아군을 추가 치유합니다.",
                200,
                TrainCarType.PrayerRoom,
                TrainPartEffectType.FountainOfLight
            ));

            Register(new TrainPartData(
                "Part_SkyBlessingProtocol",
                "하늘 가호 프로토콜",
                "치유 스킬을 사용하여 아군 체력을 최대로 채울 경우, 해당 턴 동안 대상의 공격력과 주문력이 +20% 증가합니다.",
                120,
                TrainCarType.PrayerRoom,
                TrainPartEffectType.SkyBlessingProtocol
            ));

            Register(new TrainPartData(
                "Part_CorruptionModule",
                "타락 모듈",
                "치유 스킬의 대상을 적에게도 지정 가능하게 변경하며, 치유량의 50%만큼 마법 피해를 입힙니다.",
                100,
                TrainCarType.PrayerRoom,
                TrainPartEffectType.CorruptionModule
            ));

            Register(new TrainPartData(
                "Part_LoavesAndFishesModule",
                "물고기와 빵 모듈",
                "시전자 본인에게 치유 스킬 사용 시, 치유량의 20%만큼 모든 아군을 광역 치유합니다.",
                120,
                TrainCarType.PrayerRoom,
                TrainPartEffectType.LoavesAndFishesModule
            ));

            // ── 6. 특성 훈련소 파츠 (4종) ──
            Register(new TrainPartData(
                "Part_FateStackingModule",
                "운명 중첩 모듈",
                "동일한 시너지를 중복해서 선택할 수 있습니다.",
                150,
                TrainCarType.TraitTrainingCamp,
                TrainPartEffectType.FateStackingModule
            ));

            Register(new TrainPartData(
                "Part_TraitExpander",
                "특성 확장기",
                "선택 가능한 시너지 슬롯이 +1개 증가합니다. (중복 불가)",
                220,
                TrainCarType.TraitTrainingCamp,
                TrainPartEffectType.TraitExpander
            ));

            Register(new TrainPartData(
                "Part_MultiTalentCombiner",
                "다중 재능 연합기",
                "현재 활성화된 시너지 갯수 하나당 모든 아군의 체력, 정신력, 공격력, 주문력이 +4% 증가합니다.",
                120,
                TrainCarType.TraitTrainingCamp,
                TrainPartEffectType.MultiTalentCombiner
            ));

            Register(new TrainPartData(
                "Part_SingleTraitFocuser",
                "단일 특성 집중기",
                "모든 아군의 체력, 정신력, 공격력, 주문력이 (활성화된 가장 높은 시너지 수치)%만큼 증가합니다.",
                120,
                TrainCarType.TraitTrainingCamp,
                TrainPartEffectType.SingleTraitFocuser
            ));
        }

        private static void Register(TrainPartData part)
        {
            _allParts[part.partId] = part;
        }

        public static TrainPartData GetPart(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return null;
            _allParts.TryGetValue(partId, out var part);
            return part;
        }

        public static List<TrainPartData> GetPartsForCar(TrainCarType carType, string moduleId = "")
        {
            List<TrainPartData> list = new List<TrainPartData>();
            foreach (var kvp in _allParts)
            {
                if (kvp.Value.targetCarType == carType)
                {
                    if (carType == TrainCarType.Nexus && !string.IsNullOrEmpty(moduleId))
                    {
                        if (kvp.Value.targetModuleId == moduleId || string.IsNullOrEmpty(kvp.Value.targetModuleId))
                        {
                            list.Add(kvp.Value);
                        }
                    }
                    else
                    {
                        list.Add(kvp.Value);
                    }
                }
            }
            return list;
        }

        public static List<TrainPartData> GetPartsForNexusModule(string moduleId)
        {
            return GetPartsForCar(TrainCarType.Nexus, moduleId);
        }

        public static List<TrainPartData> GetAllParts()
        {
            return new List<TrainPartData>(_allParts.Values);
        }
    }
}
