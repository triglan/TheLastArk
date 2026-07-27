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
            return new SynergyInfo(type, type.ToString(), "✨", false, "기본 시너지 효과", new List<SynergyTierInfo> { new SynergyTierInfo(2, "기본 효과") });
        }

        private static void InitDatabase()
        {
            infoDict = new Dictionary<SynergyType, SynergyInfo>();

            // ── 세력 시너지 ───────────────────────────────────────────────

            // 1. 아르키움 유니온 (2/4/6/8)
            infoDict[SynergyType.ArchiumUnion] = new SynergyInfo(
                SynergyType.ArchiumUnion, "아르키움 유니온", "⚙️", true,
                "매 턴 마법공학 발명품을 순서대로 발동합니다 (다각도 프레임: 이번 턴 행동력 +1 / 연장 총열: 다음 스킬 피해량 +50% / 유해 화학품: 모든 적 화상·출혈·독 1).",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 100%의 효과"),
                    new SynergyTierInfo(4, "(4) 200%의 효과"),
                    new SynergyTierInfo(6, "(6) 4번째 발명품 추가 - '기계장치의 신': 모든 발명품 발동"),
                    new SynergyTierInfo(8, "(8) 모든 턴에 '기계장치의 신' 발동")
                }
            );

            // 2. 라이언하트 (3/5/7)
            infoDict[SynergyType.Lionheart] = new SynergyInfo(
                SynergyType.Lionheart, "라이언하트", "🦁", true,
                "라이언하트 소속 아군이 마수 상대로 강력한 피해증가 및 피해감소 효과를 얻습니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(3, "(3) 마수 상대 추가 피해 +25%, 받는 피해 -10%, 냉기 면역"),
                    new SynergyTierInfo(5, "(5) 마수 상대 추가 피해 +50%, 받는 피해 -20%, 냉기 면역"),
                    new SynergyTierInfo(7, "(7) 마수 상대 추가 피해 +100%, 받는 피해 -30%, 냉기 면역, 모든 공격 스킬 적에게 냉기 1 부여")
                }
            );

            // 3. 엘리시움 (2/4/6/8)
            infoDict[SynergyType.Elysium] = new SynergyInfo(
                SynergyType.Elysium, "엘리시움", "⚜️", true,
                "엘리시움 소속 아군의 주고 받는 치유량이 증가하며, 다른 세력 시너지가 없을 때 체력과 공격력이 증가합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 100%의 효과 (치유량 +25%, 체력/공격력 +25%)"),
                    new SynergyTierInfo(4, "(4) 150%의 효과"),
                    new SynergyTierInfo(6, "(6) 200%의 효과, 아군 체력 회복 시 50%만큼 무작위 적에게 체력 피해"),
                    new SynergyTierInfo(8, "(8) 300%의 효과, 초과 회복량의 50%만큼 보호막 변환")
                }
            );

            // 4. 푸른 마탑 (2/4/6)
            infoDict[SynergyType.BlueTower] = new SynergyInfo(
                SynergyType.BlueTower, "푸른 마탑", "🔮", true,
                "푸른 마탑 소속 아군이 스킬을 사용할 때마다 추가 효과를 얻습니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 스킬 3회 사용 시 무작위 적에게 화상 5, 냉기 1회 분할 부여"),
                    new SynergyTierInfo(4, "(4) 추가로 소속 아군 정신력 5% 회복"),
                    new SynergyTierInfo(6, "(6) 스킬 2회 사용 시 발동으로 변경, 약화 5, 취약 5 추가 부여")
                }
            );

            // 5. 엘븐우드 대삼림 (2/4/6)
            infoDict[SynergyType.Elvenwood] = new SynergyInfo(
                SynergyType.Elvenwood, "엘븐우드 대삼림", "🍃", true,
                "아군의 행동력이 크게 증가합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 이번 턴 행동력 +1"),
                    new SynergyTierInfo(4, "(4) 이번 턴 행동력 +2, 매 턴 아군의 첫 스킬 소모 행동력 -1"),
                    new SynergyTierInfo(6, "(6) 이번 턴 행동력 +3, 매 턴 아군의 첫 스킬 소모 행동력 0 고정")
                }
            );

            // 6. 신기루 (2/4/6/8)
            infoDict[SynergyType.Mirage] = new SynergyInfo(
                SynergyType.Mirage, "신기루", "🏜️", true,
                "직업별 추가 효과 (수호자/전사: 체력 3% 회복, 암살자/사수/마술사: 공격력 10% 증가, 지원가: 행동력 +1, 치유량 +20%).",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 100%의 효과"),
                    new SynergyTierInfo(4, "(4) 150%의 효과"),
                    new SynergyTierInfo(6, "(6) 200%의 효과, 신기루 영웅 100% 추가 효과"),
                    new SynergyTierInfo(8, "(8) 300%의 효과, 신기루 영웅 100% 추가 효과, 지도 정보 모두 개방")
                }
            );

            // 7. 속삭임 교단 (3/5/7)
            infoDict[SynergyType.WhisperCult] = new SynergyInfo(
                SynergyType.WhisperCult, "속삭임 교단", "👁️", true,
                "'리더 스킬'이 턴당 1회 사용할 수 있는 특수 강화 스킬로 변환됩니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(3, "(3) 1코스트, 적 1명에게 130% 공격력 체력/정신력 피해"),
                    new SynergyTierInfo(5, "(5) 피해량 +30%, 관통 효과 추가"),
                    new SynergyTierInfo(7, "(7) 피해량 +30%, 모든 적 대상으로 변경")
                }
            );

            // 8. 청운 (3/5/7)
            infoDict[SynergyType.Cheongwoon] = new SynergyInfo(
                SynergyType.Cheongwoon, "청운", "☁️", true,
                "청운 소속 아군이 행동력을 3 소모할 때마다 특수 효과가 발동합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(3, "(3) 무작위 적에게 최대 체력의 4% 피해"),
                    new SynergyTierInfo(5, "(5) 무작위 아군 체력/정신력 5% 회복 추가"),
                    new SynergyTierInfo(7, "(7) 행동력 1 회복 추가, 위 효과 100% 증가")
                }
            );

            // 9. 침묵의 아르카디아 (1/2/3)
            infoDict[SynergyType.SilentArcadia] = new SynergyInfo(
                SynergyType.SilentArcadia, "침묵의 아르카디아", "⏳", true,
                "아르카디아 소속 아군이 특수 효과를 얻습니다 (리더 보너스 미적용).",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(1, "(1) 매 턴 받는 체력/정신력 피해 5% 감소 (최대 10중첩)"),
                    new SynergyTierInfo(2, "(2) 3턴 뒤 아르카디아 아군 정신력 소모 50% 감소 & 공격력 +40%"),
                    new SynergyTierInfo(3, "(3) 5턴 뒤 턴마다 무작위 아르카디아 아군 소모 행동력 1 고정")
                }
            );

            // ── 직업 시너지 ───────────────────────────────────────────────

            // 10. 수호자 (2/4)
            infoDict[SynergyType.Guardian] = new SynergyInfo(
                SynergyType.Guardian, "수호자", "🛡️", false,
                "모든 아군이 받는 체력 피해가 감소합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 받는 피해 -10% (수호자는 -20%)"),
                    new SynergyTierInfo(4, "(4) 받는 피해 -15% (수호자는 -30%)")
                }
            );

            // 11. 전사 (2/4)
            infoDict[SynergyType.Warrior] = new SynergyInfo(
                SynergyType.Warrior, "전사", "⚔️", false,
                "전사 아군이 잃은 체력에 비례해 스탯이 증가합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 잃은 체력 비례 공격력 최대 +40% (체력 25%에서 최대)"),
                    new SynergyTierInfo(4, "(4) 잃은 체력 비례 공격력 최대 +60% (체력 25%에서 최대)")
                }
            );

            // 12. 암살자 (2/4)
            infoDict[SynergyType.Assassin] = new SynergyInfo(
                SynergyType.Assassin, "암살자", "🗡️", false,
                "암살자 아군의 공격력이 증가하고 매 턴 첫 스킬 소모 행동력이 감소합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 공격력 +10%, 첫 스킬 소모 행동력 -1"),
                    new SynergyTierInfo(4, "(4) 공격력 +20%, 첫 스킬 소모 행동력 -2")
                }
            );

            // 13. 사수 (2/4)
            infoDict[SynergyType.Ranger] = new SynergyInfo(
                SynergyType.Ranger, "사수", "🏹", false,
                "모든 아군의 치명타 확률과 피해량이 증가합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 치명타 확률 +10%, 치명타 피해량 +15%"),
                    new SynergyTierInfo(4, "(4) 치명타 확률 +20%, 치명타 피해량 +25%")
                }
            );

            // 14. 마술사 (2/4)
            infoDict[SynergyType.Mage] = new SynergyInfo(
                SynergyType.Mage, "마술사", "🔮", false,
                "모든 아군의 상태이상 배율이 증가합니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(2, "(2) 적에게 적용하는 상태이상 개수 +50%"),
                    new SynergyTierInfo(4, "(4) 적에게 적용하는 상태이상 개수 +100%")
                }
            );

            // 15. 지원가 (1/2/3)
            infoDict[SynergyType.Support] = new SynergyInfo(
                SynergyType.Support, "지원가", "🩹", false,
                "모든 아군이 전투 시 매 턴 이로운 효과를 얻습니다.",
                new List<SynergyTierInfo>
                {
                    new SynergyTierInfo(1, "(1) 매 턴 공격력 +1"),
                    new SynergyTierInfo(2, "(2) 매 턴 공격력 +1, 체력 1 회복 추가"),
                    new SynergyTierInfo(3, "(3) 매 턴 공격력 +1, 체력 1 회복, 정신력 1 회복 추가")
                }
            );
        }
    }
}
