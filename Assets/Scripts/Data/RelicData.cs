using UnityEngine;

namespace TheLastArk.Data
{
    public enum RelicRarity
    {
        Common,
        Legendary
    }

    public enum RelicEffectType
    {
        // 일반 / 마을 / 경제 / 기차
        RestBonusHeal,          // 고급 침대 (휴식 체력 추가 회복)
        BonusAttack,            // 공격력 보너스
        BonusMaxHP,             // 최대 체력 보너스
        BonusMaxMental,         // 최대 정신력 보너스
        BonusAP,                // 추가 AP
        FreeRest,               // 빛나는 당근 (휴식이 마을 선택지 미소모)
        CommLevelBonus,         // 암호 해독기 (통신소 레벨 +1)
        ShopDiscount,           // VIP 회원권 (상점 30% 할인)
        TavernDiscount,         // 도적의 손길 (주점 30% 할인)
        TavernExtraMerc,        // 용기의 물약 (주점 용병 +1명)
        ShopFirstLegendary,     // 고고학 투자 증서 (상점 1번째 전설 유물)
        ExtraRefresh,           // 전설의 주사위 (새로고침 +1)
        ExtraCardChoice,        // 낡은 금속탐지기 (승리 보상 카드 선택지 +1)
        TrainDiscount,          // 노움의 만능 망치 (기차 칸 구매/강화 비용 20% 감소)
        CreditLedger,           // 외상 장부 (골드 -300까지 구매 가능)
        VictoryGoldBonus,       // 연금술사의 황금 항아리 (승리 골드 +30%)
        EliteGoldBonus,         // 현상금 사냥 (엘리트 승리 골드 +100%)
        LeaderAllSkillsUnlocked,// 권위자의 지팡이 (리더 스킬 전부 해금)

        // 시너지 관련
        SynergyBadge,           // OOO의 증표 (해당 시너지 +1)
        MedalBox,               // 훈장함 (활성 시너지 4개 이상 시 체/공/주 +20%)
        UnityBanner,            // 화합의 깃발 (활성 시너지 없을 때 체/정 +40%, AP +4)

        // 전투 관련
        SharpNail,              // 날카로운 못 (물리 피해 시 +1 고정 피해)
        MindFractureRune,       // 정신 분열의 룬 (마법 피해 시 10% 정신력 피해)
        ShatterScroll,          // 파쇄 주문서 (정신 피해 줄 때 정신력 10% 미만 시 즉시 패닉)
        GlassBlade,             // 유리 칼날 (치명타 발동 시 출혈 3)
        RedShoes,               // 빨간 구두 (턴 시작 시 출혈 없는 적에게 출혈 2)
        BloodMist,              // 피안개 (출혈 10 중첩 시 즉시 출혈 1회 발동)
        PoisonMushroom,         // 맹독 버섯 (독 부여 시 추가 +1)
        MindLeech,              // 정신 흡혈 거머리 (독 10 피해마다 무작위 아군 정신력 +1)
        SwampLiquid,            // 늪지의 액체 (중독된 적에게 독 부여 시 무작위 적 20% 전이)
        FireMoth,               // 불나방 (화상 부여 100% 증가, 시전자도 화상)
        FlameHammer,            // 화염 망치 (동일 적 화상 3회 부여 시 즉시 발동)
        Lantern,                // 호롱불 (적 화상 +25%, 아군 화상 -25%)
        SealOfVengeance,        // 복수의 인장 (아군 체력 피해 시 힘 +1)
        IronFortressFragment,   // 철벽 성채의 조각 (전투 시작 시 아군 전체 보호막 20 부여)
        ManaCrusher,            // 마나분쇄자 (보호막 대상 피해 +50%)
        OiledGear,              // 기름칠된 톱니바퀴 (첫 사용 스킬 유지)
        UncertainScales,        // 불확실한 천칭 (확률 스킬 확률 +25%)
        FlashOfTwilight,        // 회광반조 (잃은 체력 비례 치유량 최대 +50%)
        OneMoreDrink,           // 한 잔 더! (비용 0 스킬 사용 시 AP 1 회복)

        // ── 전설 유물 ──
        ArkCoin,                // 아크코인 (보상 골드 -50%~+150% 변동)
        GoldenPath,             // 황금의 길 (보유 골드 10% 추가 획득, 최대 500골드)
        HeartOfMagitech,        // 마도공학의 심장 (아르키움 유니온 +2, 발명품 발동 시 공/주 +1)
        BeastSlayer,            // 마수살해자 (라이언하트 +2, 마수 대상 효과 +50%, 전 아군 적용)
        SkyCross,               // 하늘 십자가 (엘리시움 +2, 단독 세력 시 정신력/주문력 +25%)
        GrimoireOfStars,        // 별의 그리모어 (푸른 마탑 +2, 푸른 마탑 8단계 스킬 1회 발동 해금)
        WorldTreeBranch,        // 세계수의 가지 (엘븐우드 대삼림 +3)
        AllianceCrest,          // 연합의 문장 (신기루 +2, 수호/전사/지원 잃은 정신력 3% 회복, 암살/사수/마술 치명 +10)
        WhisperCultRelic,       // 속삭임 교단 (속삭임 교단 +2, 전용 스킬 체력 동일 피해)
        CheongwoonRelic,        // 청운 (청운 +2, 청운 7단계 행동력 2 소모 시 발동)
        MegalithShield,         // 거석 방패 (수호자 최대체력 10%만큼 공/주 중 높은 수치 획득)
        BerserkerAxe,           // 광전사의 도끼 (전사 적에게 입힌 체력 피해의 15% 흡혈)
        ShadowVeil,             // 그림자 베일 (암살자 적 처치 시 AP 3 회복)
        SniperEye,              // 저격수의 눈 (사수 치명타 피해량 +25%)
        RuneOfCycle,            // 순환의 룬 (마술사 상태이상 부여 시 무작위 아군 정신력 1 회복)
        HealingMallangi,        // 힐링 말랑이 (지원가 4단계 해금: 모든 아군 지원가 스킬칸 +1)
        Traitor,                // 배반자 (알렉스 바스티온: 회생->배반자, 체력1시 아군 정신력 흡수 부활, 공+50%, 개화시 가르기 2회)
        EndlessBattle           // 끊임없는 전투 (알렉스 바스티온: 재정비->끊임없는 전투, 잃은체력 20% 공격력+가르기)
    }

    [CreateAssetMenu(fileName = "NewRelic", menuName = "TheLastArk/Relic Data")]
    public class RelicData : ScriptableObject
    {
        public string relicID;
        public string relicName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public RelicRarity rarity = RelicRarity.Common;
        public RelicEffectType effectType;
        public float effectValue;
        public SynergyType targetSynergy; // 시너지 증표 유물용
    }
}
