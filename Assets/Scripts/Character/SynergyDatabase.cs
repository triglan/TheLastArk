using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;

namespace TheLastArk.Character
{
    public class SynergyTierInfo
    {
        public int threshold;
        public string description;

        public SynergyTierInfo(int threshold, string description)
        {
            this.threshold = threshold;
            this.description = description;
        }
    }

    public class SynergyInfo
    {
        public SynergyType type;
        public string displayName;
        public string iconEmoji;
        public bool isFaction; // true: 세력 시너지, false: 직업 시너지
        public string summaryDescription;
        public List<SynergyTierInfo> tiers;

        public SynergyInfo(SynergyType type, string displayName, string iconEmoji, bool isFaction, string summaryDescription, List<SynergyTierInfo> tiers)
        {
            this.type = type;
            this.displayName = displayName;
            this.iconEmoji = iconEmoji;
            this.isFaction = isFaction;
            this.summaryDescription = summaryDescription;
            this.tiers = tiers;
        }

        public int GetNextThreshold(int currentCount)
        {
            if (tiers == null || tiers.Count == 0) return 0;
            foreach (var tier in tiers)
            {
                if (currentCount < tier.threshold) return tier.threshold;
            }
            return tiers[tiers.Count - 1].threshold; // Max threshold reached
        }

        public SynergyTierInfo GetCurrentActiveTier(int currentCount)
        {
            if (tiers == null) return null;
            SynergyTierInfo highest = null;
            foreach (var tier in tiers)
            {
                if (currentCount >= tier.threshold)
                {
                    if (highest == null || tier.threshold > highest.threshold)
                    {
                        highest = tier;
                    }
                }
            }
            return highest;
        }
    }

    public static class SynergyDatabase
    {
        private static Dictionary<SynergyType, SynergyInfo> infoDict;

        public static SynergyInfo GetInfo(SynergyType type)
        {
            if (infoDict == null) InitDatabase();
            if (infoDict.TryGetValue(type, out SynergyInfo info)) return info;

            // Default Fallback
            return new SynergyInfo(type, type.ToString(), "시너지", false, "기본 시너지 효과", new List<SynergyTierInfo> { new SynergyTierInfo(2, "기본 효과") });
        }

        private static void InitDatabase()
        {
            infoDict = new Dictionary<SynergyType, SynergyInfo>();

            infoDict[SynergyType.ArchiumUnion] = new SynergyInfo(
                SynergyType.ArchiumUnion, "아르키움 유니온", "유니온", true,
                "턴 시작에 추가 D6을 굴리고, 새로 굴린 주사위 중 원래 눈금이 가장 높은 1개에 외부 보정을 줍니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 추가 D6 1개"),
                    new SynergyTierInfo(4, "(4) 추가 D6 1개, 최고 눈금 +2"),
                    new SynergyTierInfo(6, "(6) 추가 D6 2개, 최고 눈금 +2"),
                    new SynergyTierInfo(8, "(8) 추가 D6 3개, 최고 눈금 +4")
                }
            );
            infoDict[SynergyType.Lionheart] = new SynergyInfo(
                SynergyType.Lionheart, "라이언하트", "라이언", true,
                "턴 시작에 북방 D6을 얻습니다. 북방 주사위를 쓴 캐릭터는 효과 처리 후 소비 눈금만큼 보호막을 얻습니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(3, "(3) 북방 D6 1개, 보호막 D"),
                    new SynergyTierInfo(5, "(5) 북방 D6 2개, 보호막 D"),
                    new SynergyTierInfo(7, "(7) 북방 D6 3개, 보호막 D×2")
                }
            );
            infoDict[SynergyType.Elysium] = new SynergyInfo(
                SynergyType.Elysium, "성도 엘리시움", "엘리시움", true,
                "다른 진영 시너지가 없을 때만 발동. 턴 시작에 천상 D6을 즉시 굴려 생존 아군 전체 체력을 합÷2(버림, 최소 1)만큼 회복합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 천상 D6 1개"),
                    new SynergyTierInfo(4, "(4) 천상 D6 2개"),
                    new SynergyTierInfo(6, "(6) 천상 D6 2개, 초과 회복 50% 보호막"),
                    new SynergyTierInfo(8, "(8) 천상 D6 3개, 초과 회복 100% 보호막")
                }
            );
            infoDict[SynergyType.BlueTower] = new SynergyInfo(
                SynergyType.BlueTower, "푸른 마탑", "마탑", true,
                "마탑 소속 주문력 증가. 마탑 소속이 서로 다른 스킬을 실제로 발동하면 생존 마탑 소속 전체 정신력을 회복합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 주문력 +10%, 서로 다른 스킬 3종마다 정신력 +2 (턴당 1회)"),
                    new SynergyTierInfo(4, "(4) 주문력 +20%, 3종마다 정신력 +3 (턴당 1회)"),
                    new SynergyTierInfo(6, "(6) 주문력 +30%, 2종마다 정신력 +4 (턴당 2회)")
                }
            );
            infoDict[SynergyType.Elvenwood] = new SynergyInfo(
                SynergyType.Elvenwood, "엘븐우드 대삼림", "엘븐우드", true,
                "턴 시작에 큰 주사위를 추가로 얻습니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) D12 1개"),
                    new SynergyTierInfo(4, "(4) D12 + D18"),
                    new SynergyTierInfo(6, "(6) D12 + D18 + D24")
                }
            );
            infoDict[SynergyType.Mirage] = new SynergyInfo(
                SynergyType.Mirage, "미라주", "미라주", true,
                "골드 주사위 사용 가능(턴당 1회). 획득 골드 증가. 실제 파티 직업마다 생존 아군 전체를 지원합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 골드 +0%, 직업 지원 1단계"),
                    new SynergyTierInfo(4, "(4) 골드 +25%, 직업 지원 2단계"),
                    new SynergyTierInfo(6, "(6) 골드 +50%, 직업 지원 3단계"),
                    new SynergyTierInfo(8, "(8) 골드 +100%, 직업 지원 4단계")
                }
            );
            infoDict[SynergyType.WhisperCult] = new SynergyInfo(
                SynergyType.WhisperCult, "속삭이는 교단", "교단", true,
                "턴 시작에 꿈틀거리는 D6을 즉시 판정해 적에게 정신 피해와 공포를 줍니다. 정신력 50% 이하 적에게 정신 피해 증가.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(3, "(3) 1개: 무작위 적 max(1, D−1) 정신 피해 + 공포 1, 낮은 정신력 +25%"),
                    new SynergyTierInfo(5, "(5) 2개: 주사위마다 무작위 적 D 정신 피해 + 공포 2, +40%"),
                    new SynergyTierInfo(7, "(7) 2개: 합만큼 적 전체 정신 피해 + 공포 3, +50%")
                }
            );
            infoDict[SynergyType.Cheongwoon] = new SynergyInfo(
                SynergyType.Cheongwoon, "청운", "청운", true,
                "턴 시작에 눈금 고정 구름 D6을 얻습니다. 청운 소속이 0~1눈으로만 공격 스킬을 쓰면 공격받은 적에게 쇠약(진영 턴당 1회).",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(3, "(3) 구름 0눈 2개, 쇠약 1"),
                    new SynergyTierInfo(5, "(5) 구름 1눈 2개, 쇠약 2"),
                    new SynergyTierInfo(7, "(7) 구름 1눈 4개, 쇠약 3")
                }
            );
            infoDict[SynergyType.SilentArcadia] = new SynergyInfo(
                SynergyType.SilentArcadia, "침묵의 아르카디아", "아르카디아", true,
                "턴 종료에 미사용 주사위를 골라 다음 턴으로 이월합니다. 리더 보너스가 적용되지 않습니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(1, "(1) 이월 1개"),
                    new SynergyTierInfo(2, "(2) 이월 2개"),
                    new SynergyTierInfo(3, "(3) 이월 2개, 6번째 턴부터 매 턴 무작위 소속 1명 최대 눈금 효과")
                }
            );
            infoDict[SynergyType.BanditGang] = new SynergyInfo(
                SynergyType.BanditGang, "도적단", "도적단", true,
                "도적단 소속 적을 분류합니다.",
                new List<SynergyTierInfo>()
            );

            infoDict[SynergyType.Guardian] = new SynergyInfo(
                SynergyType.Guardian, "수호자", "수호자", false,
                "아군 전체가 받는 체력 피해 감소. 수호자가 보호·도발로 받는 피해 추가 감소.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 받는 체력 피해 −10%, 보호·도발 추가 −10%"),
                    new SynergyTierInfo(4, "(4) 받는 체력 피해 −15%, 보호·도발 추가 −20%")
                }
            );
            infoDict[SynergyType.Warrior] = new SynergyInfo(
                SynergyType.Warrior, "전사", "전사", false,
                "전사 소속의 그 턴 첫 물리 공격 스킬 피해 증가.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 첫 스킬 +35%"),
                    new SynergyTierInfo(4, "(4) 첫 스킬 +50%, 두 번째 +25%")
                }
            );
            infoDict[SynergyType.Assassin] = new SynergyInfo(
                SynergyType.Assassin, "암살자", "암살자", false,
                "암살자 소속 공격력·주문력 증가, 주사위 사용 시 효과 눈금 증가.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 공·주 +10%, 효과 눈금 +1"),
                    new SynergyTierInfo(4, "(4) 공·주 +20%, 효과 눈금 +2")
                }
            );
            infoDict[SynergyType.Ranger] = new SynergyInfo(
                SynergyType.Ranger, "사수", "사수", false,
                "아군 전체 치명 확률·치명 피해 증가.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 치명 +10%p, 치명 피해 +15%p"),
                    new SynergyTierInfo(4, "(4) 치명 +20%p, 치명 피해 +25%p")
                }
            );
            infoDict[SynergyType.Mage] = new SynergyInfo(
                SynergyType.Mage, "마법사", "마법사", false,
                "아군이 적에게 거는 수치형 디버프 증가.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 디버프 수치 ×1.5"),
                    new SynergyTierInfo(4, "(4) 디버프 수치 ×2")
                }
            );
            infoDict[SynergyType.Support] = new SynergyInfo(
                SynergyType.Support, "서포터", "서포터", false,
                "아군 전체가 받는 회복·보호막 증가. 서포터의 지원을 받은 아군 정신력 회복(대상별 턴당 1회).",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(1, "(1) 회복·보호막 +15%, 정신력 +1"),
                    new SynergyTierInfo(2, "(2) +25%, 정신력 +2"),
                    new SynergyTierInfo(3, "(3) +35%, 정신력 +3")
                }
            );
        }
    }
}
