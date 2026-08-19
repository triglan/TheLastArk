using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;

namespace TheLastArk.Battle
{
    public enum TarotCardType
    {
        Fool = 0,               // 0. 광대
        Magician = 1,           // I. 마술사
        HighPriestess = 2,      // II. 여사제
        Empress = 3,            // III. 여제
        Emperor = 4,            // IV. 황제
        Hierophant = 5,         // V. 교황
        Lovers = 6,             // VI. 연인
        Chariot = 7,            // VII. 전차
        Strength = 8,           // VIII. 힘
        Hermit = 9,             // IX. 은자
        WheelOfFortune = 10,    // X. 운명
        Justice = 11,           // XI. 정의
        HangedMan = 12,         // XII. 매달린 사람
        Death = 13,             // XIII. 죽음
        Temperance = 14,        // XIV. 절제
        Devil = 15,             // XV. 악마
        Tower = 16,             // XVI. 탑
        Star = 17,              // XVII. 별 (파츠)
        Moon = 18,              // XVIII. 달 (파츠)
        Sun = 19,               // XIX. 태양 (파츠)
        Judgement = 20,         // XX. 심판
        World = 21              // XXI. 세계 (파츠)
    }

    public enum DevilContractType
    {
        None = 0,
        Option1_MoreAP_MoreDevil = 1,                       // 행동력 +6 (+선택 누적), 악마 등장 확률 +66% 누적
        Option2_EveryTurnAP_LoseMental_NoMoreDevil = 2,     // 매 턴 행동력 +6, 매 턴 아군 정신력 -6, 악마 재등장 불가
        Option3_EveryTurnAP_GainDebuffs = 3                 // 매 턴 행동력 +6, 매 턴 아군 약화/취약 +1
    }

    public class ArcanaCardInfo
    {
        public TarotCardType cardType;
        public int number;
        public string romanNumeral;
        public string cardNameKorean;
        public string cardNameEnglish;
        public string description;
        public string shortEffectSummary;
        public Color themeColor;

        public ArcanaCardInfo(TarotCardType type, int num, string roman, string nameKo, string nameEn, string desc, string shortSum, Color color)
        {
            this.cardType = type;
            this.number = num;
            this.romanNumeral = roman;
            this.cardNameKorean = nameKo;
            this.cardNameEnglish = nameEn;
            this.description = desc;
            this.shortEffectSummary = shortSum;
            this.themeColor = color;
        }

        public string FullTitle => $"[{romanNumeral}. {cardNameKorean}]";
    }

    public class ArcanaDrawResult
    {
        public ArcanaCardInfo drawnCard;
        public ArcanaCardInfo hermitChainedCard; // 은자 발동 시 추가로 드로우된 카드
        public int baseCardAP;
        public int gainedAP;
        public DevilContractType devilChoice = DevilContractType.None;
        public string summary;
        public List<string> detailLogs = new List<string>();
    }

    public class ArcanaBattleState
    {
        public int temperanceAccumulatedAP = 0;             // [절제] 매 턴 행동력 영구 누적
        public bool isWheelOfFortunePending = false;        // [운명] 다음 턴 행동력 2배
        public int hangedManPenaltyPending = 0;             // [매달린 사람] 다음 턴 행동력 -4
        public bool isConstellationActive = false;          // [별자리] 별 카드 발동 시 활성화
        public int constellationSkillCount = 0;             // [별자리] 스킬 사용 횟수 (3회마다 AP +1)
        public DevilContractType activeDevilContract = DevilContractType.None; // [악마] 영구 계약
        public int devilOption1PickCount = 0;               // [악마 1번] 선택 누적 횟수
        public HashSet<SkillInfo> emperorBannedSkillsNextTurn = new HashSet<SkillInfo>(); // [황제] 다음 턴 금지 스킬
        public HashSet<SkillInfo> emperorUsedSkillsThisTurn = new HashSet<SkillInfo>();   // [황제] 이번 턴 사용 스킬

        // 턴 한정 효과 플래그
        public int foolFreeSkillsRemaining = 0;             // [광대] 0코스트 스킬 남은 횟수 (2회)
        public bool isTowerDoubleCastActive = false;        // [탑] 첫 스킬 2회 발동
        public bool isEmperorActiveThisTurn = false;        // [황제] 0코스트 고정 및 1회 제한
        public bool isLoversActiveThisTurn = false;         // [연인] 힘/보호 아군 전체 공유
        public bool isStrengthActiveThisTurn = false;       // [힘] 공격 스킬 비용 +1, 힘 +3
        public bool isHierophantActiveThisTurn = false;     // [교황] 3코스트 이상 스킬 비용 -1
        public bool isSunRetainActiveThisTurn = false;      // [태양] 모든 스킬 유지
        public bool isDeathPendingThisTurn = false;         // [죽음] 턴 종료 시 체/정 4 감소

        public void ResetTurnFlags()
        {
            foolFreeSkillsRemaining = 0;
            isTowerDoubleCastActive = false;
            isEmperorActiveThisTurn = false;
            isLoversActiveThisTurn = false;
            isStrengthActiveThisTurn = false;
            isHierophantActiveThisTurn = false;
            isSunRetainActiveThisTurn = false;
            isDeathPendingThisTurn = false;
            emperorUsedSkillsThisTurn.Clear();
        }
    }

    public static class ArcanaCardManager
    {
        private static readonly Dictionary<TarotCardType, ArcanaCardInfo> _cardDatabase = new Dictionary<TarotCardType, ArcanaCardInfo>();

        static ArcanaCardManager()
        {
            Register(new ArcanaCardInfo(
                TarotCardType.Fool, 0, "0", "광대", "The Fool",
                "행동력 +0\n이번 턴에 사용하는 첫 스킬 2개의 비용이 0이 됨",
                "행동력 +0, 첫 스킬 2개 비용 0",
                new Color(0.9f, 0.7f, 0.2f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Magician, 1, "I", "마술사", "The Magician",
                "행동력 +(4+AC)\n막강한 마력으로 대량의 기본 행동력을 공급합니다.",
                "행동력 +(4+AC)",
                new Color(0.4f, 0.7f, 1.0f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.HighPriestess, 2, "II", "여사제", "The High Priestess",
                "행동력 +(2+AC)\n모든 아군에게 보호 1을 부여합니다.",
                "행동력 +(2+AC), 모든 아군 보호 1",
                new Color(0.5f, 0.85f, 0.95f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Empress, 3, "III", "여제", "The Empress",
                "행동력 +(4+AC)\n모든 아군의 체력과 정신력을 5 즉시 회복합니다.",
                "행동력 +(4+AC), 모든 아군 체/정 5 회복",
                new Color(0.4f, 0.9f, 0.5f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Emperor, 4, "IV", "황제", "The Emperor",
                "행동력 +0\n이번 턴 모든 스킬 비용이 0으로 고정되지만 스킬당 1회만 사용 가능하며, 이번 턴에 사용한 스킬은 다음 턴에 사용할 수 없습니다.",
                "행동력 +0, 모든 스킬 0코(1회 제한/다음턴 사용불가)",
                new Color(0.95f, 0.35f, 0.2f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Hierophant, 5, "V", "교황", "The Hierophant",
                "행동력 +(2+AC)\n스킬 비용이 3 이상인 고코스트 스킬들의 비용이 -1 감소합니다.",
                "행동력 +(2+AC), 3코 이상 스킬 비용 -1",
                new Color(0.9f, 0.8f, 0.4f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Lovers, 6, "VI", "연인", "The Lovers",
                "행동력 +(2+AC)\n이번 턴 '연인' 효과를 얻어, 아군이 얻는 힘 및 보호 효과가 모든 아군에게 동시 공유 적용됩니다.",
                "행동력 +(2+AC), 힘/보호 효과 전 아군 공유",
                new Color(1.0f, 0.45f, 0.65f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Chariot, 7, "VII", "전차", "The Chariot",
                "행동력 +(2+AC)\n모든 아군에게 힘(공격력) +1을 부여합니다.",
                "행동력 +(2+AC), 모든 아군 힘 +1",
                new Color(0.85f, 0.5f, 0.2f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Strength, 8, "VIII", "힘", "Strength",
                "행동력 +(2+AC)\n이번 턴 공격 스킬 비용이 +1 증가하지만, 모든 아군에게 힘 +3을 부여합니다.",
                "행동력 +(2+AC), 공격스킬 비용 +1, 모든 아군 힘 +3",
                new Color(0.95f, 0.3f, 0.3f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Hermit, 9, "IX", "은자", "The Hermit",
                "행동력 +1\n은자를 제외한 다른 타로 카드를 1회 더 추가로 연계 드로우합니다.",
                "행동력 +1, 추가 타로 카드 1회 연계 드로우",
                new Color(0.6f, 0.65f, 0.75f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.WheelOfFortune, 10, "X", "운명", "Wheel of Fortune",
                "행동력 +(AC)\n다음 턴에 얻는 총 행동력이 2배(*2)로 대폭 증폭됩니다.",
                "행동력 +(AC), 다음 턴 행동력 2배 증폭",
                new Color(1.0f, 0.8f, 0.2f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Justice, 11, "XI", "정의", "Justice",
                "행동력 +(2+AC)\n모든 적에게 약화 3 (공격력 감소 3턴) 및 취약 3 (받는 피해 증가 3턴)을 부여합니다.",
                "행동력 +(2+AC), 모든 적 약화 3 & 취약 3",
                new Color(0.3f, 0.75f, 0.9f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.HangedMan, 12, "XII", "매달린 사람", "The Hanged Man",
                "행동력 +(7+AC)\n이번 턴 막대한 행동력을 즉시 얻는 대신, 다음 턴에 얻는 행동력이 -4 감소합니다.",
                "행동력 +(7+AC), 다음 턴 행동력 -4",
                new Color(0.7f, 0.45f, 0.85f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Death, 13, "XIII", "죽음", "Death",
                "행동력 +(8+AC)\n강력한 행동력을 얻지만, 턴 종료 시 모든 아군의 체력과 정신력이 4 감소합니다.",
                "행동력 +(8+AC), 턴 종료 시 모든 아군 체/정 4 감소",
                new Color(0.35f, 0.35f, 0.4f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Temperance, 14, "XIV", "절제", "Temperance",
                "행동력 +(1+AC)\n이번 전투 동안 매 턴 행동력이 영구히 +1씩 누적 증가합니다.",
                "행동력 +(1+AC), 매 턴 행동력 +1 영구 누적",
                new Color(0.3f, 0.85f, 0.65f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Devil, 15, "XV", "악마", "The Devil",
                "3가지 악마의 계약 중 1가지를 선택하여 강력한 힘을 거래합니다.",
                "3가지 악마의 계약 중 1가지 선택",
                new Color(0.8f, 0.15f, 0.25f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Tower, 16, "XVI", "탑", "The Tower",
                "행동력 +(3+AC)\n이번 턴에 처음으로 사용하는 스킬이 2번 연속 발동(더블 캐스팅)됩니다.",
                "행동력 +(3+AC), 첫 스킬 2회 연속 발동",
                new Color(0.9f, 0.4f, 0.15f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Star, 17, "XVII", "별", "The Star",
                "행동력 +(4+AC)\n이번 전투 동안 '별자리' 효과를 얻습니다. (스킬 3회 사용 시마다 행동력 +1 충전, 별 카드 제외)",
                "행동력 +(4+AC), 별자리: 스킬 3회당 +1 AP",
                new Color(0.4f, 0.9f, 1.0f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Moon, 18, "XVIII", "달", "The Moon",
                "행동력 +(5+AC)\n정신력 50% 이상 아군 수만큼 행동력 +1 추가 획득, 정신력 50% 미만 아군은 정신력 15% 회복",
                "행동력 +(5+AC), 정신력 비례 추가 AP 및 회복",
                new Color(0.6f, 0.75f, 1.0f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Sun, 19, "XIX", "태양", "The Sun",
                "행동력 +(5+AC)\n이번 턴 모든 스킬에 유지(Retain) 효과를 부여합니다.",
                "행동력 +(5+AC), 이번 턴 모든 스킬 유지 부여",
                new Color(1.0f, 0.75f, 0.15f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.Judgement, 20, "XX", "심판", "Judgement",
                "행동력 +(5+AC)\n현재 체력이 가장 많은 적 1명에게 기절 1턴을 부여합니다.",
                "행동력 +(5+AC), 최고체력 적 1명 기절 1턴",
                new Color(0.85f, 0.85f, 0.95f)
            ));

            Register(new ArcanaCardInfo(
                TarotCardType.World, 21, "XXI", "세계", "The World",
                "행동력 +10\n모든 아군의 모든 상태이상(디버프)을 완전히 정화합니다.",
                "행동력 +10, 모든 아군 상태이상 정화",
                new Color(0.95f, 0.85f, 0.35f)
            ));
        }

        private static void Register(ArcanaCardInfo info)
        {
            _cardDatabase[info.cardType] = info;
        }

        public static ArcanaCardInfo GetCardInfo(TarotCardType type)
        {
            _cardDatabase.TryGetValue(type, out var info);
            return info;
        }

        public static List<TarotCardType> GetAvailableCardPool(TrainCar nexusCar, ArcanaBattleState state)
        {
            int ac = nexusCar != null ? nexusCar.level : 0;
            List<TarotCardType> pool = new List<TarotCardType>();

            // Lv.0 기본 (4종)
            pool.Add(TarotCardType.Fool);
            pool.Add(TarotCardType.Magician);
            pool.Add(TarotCardType.HighPriestess);
            pool.Add(TarotCardType.Lovers);

            // Lv.1 이상 (+4종)
            if (ac >= 1)
            {
                pool.Add(TarotCardType.Chariot);
                pool.Add(TarotCardType.Hermit);
                pool.Add(TarotCardType.Strength);
                pool.Add(TarotCardType.WheelOfFortune);
            }

            // Lv.2 이상 (+5종)
            if (ac >= 2)
            {
                pool.Add(TarotCardType.Justice);
                pool.Add(TarotCardType.Hierophant);
                pool.Add(TarotCardType.HangedMan);
                pool.Add(TarotCardType.Temperance);
                pool.Add(TarotCardType.Tower);
            }

            // Lv.3 이상 (+5종)
            if (ac >= 3)
            {
                pool.Add(TarotCardType.Empress);
                pool.Add(TarotCardType.Emperor);
                pool.Add(TarotCardType.Death);
                pool.Add(TarotCardType.Judgement);

                // 악마 2번 계약 활성 시 악마 카드 제외
                if (state == null || state.activeDevilContract != DevilContractType.Option2_EveryTurnAP_LoseMental_NoMoreDevil)
                {
                    pool.Add(TarotCardType.Devil);
                }
            }

            // 전용 파츠 장착 카드
            if (nexusCar != null)
            {
                if (nexusCar.HasPartEffect(TrainPartEffectType.ArcanaSun)) pool.Add(TarotCardType.Sun);
                if (nexusCar.HasPartEffect(TrainPartEffectType.ArcanaMoon)) pool.Add(TarotCardType.Moon);
                if (nexusCar.HasPartEffect(TrainPartEffectType.ArcanaWorld)) pool.Add(TarotCardType.World);
                if (nexusCar.HasPartEffect(TrainPartEffectType.ArcanaStar))
                {
                    // 별자리 활성화 시 별 카드 제외
                    if (state == null || !state.isConstellationActive)
                    {
                        pool.Add(TarotCardType.Star);
                    }
                }
            }

            return pool;
        }

        public static ArcanaCardInfo DrawRandomCard(List<TarotCardType> pool, ArcanaBattleState state, TarotCardType? excludeType = null)
        {
            if (pool == null || pool.Count == 0) return GetCardInfo(TarotCardType.Magician);

            List<TarotCardType> validPool = new List<TarotCardType>(pool);
            if (excludeType.HasValue) validPool.RemoveAll(t => t == excludeType.Value);

            if (validPool.Count == 0) validPool = new List<TarotCardType>(pool);

            // 악마 1번 옵션 선택 누적에 따른 악마 가중치 처리
            if (state != null && state.devilOption1PickCount > 0 && validPool.Contains(TarotCardType.Devil))
            {
                // 악마 1번 옵션 1회당 +66% 가중치
                int extraWeights = Mathf.RoundToInt(state.devilOption1PickCount * 2f);
                for (int i = 0; i < extraWeights; i++)
                {
                    validPool.Add(TarotCardType.Devil);
                }
            }

            TarotCardType selected = validPool[UnityEngine.Random.Range(0, validPool.Count)];
            return GetCardInfo(selected);
        }

        public static int CalculateBaseCardAP(TarotCardType type, int ac, ArcanaBattleState state, DevilContractType devilChoice = DevilContractType.None)
        {
            return type switch
            {
                TarotCardType.Fool => 0,
                TarotCardType.Magician => 4 + ac,
                TarotCardType.HighPriestess => 2 + ac,
                TarotCardType.Empress => 4 + ac,
                TarotCardType.Emperor => 0,
                TarotCardType.Hierophant => 2 + ac,
                TarotCardType.Lovers => 2 + ac,
                TarotCardType.Chariot => 2 + ac,
                TarotCardType.Strength => 2 + ac,
                TarotCardType.Hermit => 1,
                TarotCardType.WheelOfFortune => ac,
                TarotCardType.Justice => 2 + ac,
                TarotCardType.HangedMan => 7 + ac,
                TarotCardType.Death => 8 + ac,
                TarotCardType.Temperance => 1 + ac,
                TarotCardType.Devil => 6 + (devilChoice == DevilContractType.Option1_MoreAP_MoreDevil ? (state != null ? state.devilOption1PickCount : 0) : 0),
                TarotCardType.Tower => 3 + ac,
                TarotCardType.Star => 4 + ac,
                TarotCardType.Moon => 5 + ac,
                TarotCardType.Sun => 5 + ac,
                TarotCardType.Judgement => 5 + ac,
                TarotCardType.World => 10,
                _ => 4
            };
        }

        public static ArcanaDrawResult EvaluateDraw(ArcanaCardInfo card, TrainCar nexusCar, ArcanaBattleState state, DevilContractType devilChoice = DevilContractType.None)
        {
            int ac = nexusCar != null ? nexusCar.level : 0;
            var result = new ArcanaDrawResult
            {
                drawnCard = card,
                devilChoice = devilChoice
            };

            int baseAp = CalculateBaseCardAP(card.cardType, ac, state, devilChoice);
            result.baseCardAP = baseAp;

            // 은자 카드일 경우 1회 추가 연계 드로우
            if (card.cardType == TarotCardType.Hermit)
            {
                var pool = GetAvailableCardPool(nexusCar, state);
                result.hermitChainedCard = DrawRandomCard(pool, state, excludeType: TarotCardType.Hermit);
                int chainedAp = CalculateBaseCardAP(result.hermitChainedCard.cardType, ac, state, devilChoice);
                baseAp += chainedAp;
                result.detailLogs.Add($"[은자 연쇄 드로우] {result.hermitChainedCard.FullTitle} 추가 발동! (+{chainedAp} AP)");
            }

            // 악마 계약 상시 AP 또는 절제 상시 AP
            int extraTurnAP = 0;
            if (state != null)
            {
                if (state.temperanceAccumulatedAP > 0)
                {
                    extraTurnAP += state.temperanceAccumulatedAP;
                    result.detailLogs.Add($"[절제 누적] 매 턴 행동력 +{state.temperanceAccumulatedAP} AP");
                }
                if (state.activeDevilContract == DevilContractType.Option2_EveryTurnAP_LoseMental_NoMoreDevil ||
                    state.activeDevilContract == DevilContractType.Option3_EveryTurnAP_GainDebuffs)
                {
                    extraTurnAP += 6;
                    result.detailLogs.Add("[악마 계약] 매 턴 행동력 +6 AP");
                }
            }

            int finalAP = baseAp + extraTurnAP;

            // [운명] 이전 턴 2배 보너스
            if (state != null && state.isWheelOfFortunePending)
            {
                finalAP *= 2;
                result.detailLogs.Add("<color=#FFD700>[운명 2배] 이전 턴 운명 효과로 행동력 2배 증폭!</color>");
            }

            // [매달린 사람] 이전 턴 -4 페널티
            if (state != null && state.hangedManPenaltyPending > 0)
            {
                finalAP = Mathf.Max(1, finalAP - state.hangedManPenaltyPending);
                result.detailLogs.Add($"<color=#FF5555>[매달린 사람 페널티] 이전 턴 페널티로 행동력 -{state.hangedManPenaltyPending}</color>");
            }

            result.gainedAP = Mathf.Max(0, finalAP);

            string hermitExtra = result.hermitChainedCard != null ? $" + {result.hermitChainedCard.cardNameKorean}" : "";
            result.summary = $"{card.FullTitle}{hermitExtra} -> <b>총 {result.gainedAP} AP</b>";

            return result;
        }
    }
}
