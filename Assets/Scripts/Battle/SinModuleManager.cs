using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;

namespace TheLastArk.Battle
{
    public enum SinType
    {
        Gluttony = 0,   // 식탐
        Sloth = 1,      // 나태
        Lust = 2,       // 색욕
        Wrath = 3,      // 분노
        Envy = 4,       // 질투
        Greed = 5,      // 탐욕
        Pride = 6       // 오만 (파츠)
    }

    public class SinInfo
    {
        public SinType sinType;
        public string nameKorean;
        public string nameEnglish;
        public int baseAP;
        public string description;
        public Color themeColor;

        public SinInfo(SinType type, string ko, string en, int ap, string desc, Color color)
        {
            this.sinType = type;
            this.nameKorean = ko;
            this.nameEnglish = en;
            this.baseAP = ap;
            this.description = desc;
            this.themeColor = color;
        }

        public string FullTitle => $"[{nameKorean}의 죄 ({nameEnglish})]";
    }

    public class SinActiveState
    {
        public SinType? currentSin = null;
        public int remainingTurns = 0;              // 지속 턴 수 (최초 3턴)
        public int turnGainedAP = 0;
        public bool isIndulgenceUsedInBattle = false;// 면죄부 1회 사용 여부
        public bool isIndulgedCurrentSin = false;    // 현재 죄악 효과 제거(면죄) 여부
        public bool rerollSinNextTurn = false;       // 면죄부 사용 시 다음 턴 재추첨 플래그

        // 식탐 상태 추적
        public bool enemyKilledThisTurn = false;
        public int pendingGluttonyPenaltyNextTurn = 0;

        // 나태 상태 추적
        public int skillsUsedThisTurn = 0;

        // 색욕 매혹 대상 목록 (턴마다 갱신)
        public List<BattleCharacter> charmedCharacters = new List<BattleCharacter>();

        // 비복원 순환 추첨 Bag
        public List<SinType> sinBag = new List<SinType>();

        public void ResetTurnCounters()
        {
            enemyKilledThisTurn = false;
            skillsUsedThisTurn = 0;
            charmedCharacters.Clear();
        }

        public void ClearCurrentSin()
        {
            currentSin = null;
            remainingTurns = 0;
            turnGainedAP = 0;
            isIndulgedCurrentSin = false;
            ResetTurnCounters();
        }
    }

    public static class SinModuleManager
    {
        private static readonly Dictionary<SinType, SinInfo> _sinDatabase = new Dictionary<SinType, SinInfo>();

        static SinModuleManager()
        {
            Register(new SinInfo(
                SinType.Gluttony, "식탐", "Gluttony", 6,
                "행동력 +6\n적 처치 시 추가 행동력 +3 획득\n적을 처치하지 못하고 턴 종료 시 다음 턴 행동력 -3 감소",
                new Color(0.85f, 0.55f, 0.2f)
            ));

            Register(new SinInfo(
                SinType.Sloth, "나태", "Sloth", 12,
                "행동력 +12\n이번 턴에 스킬을 최대 3회까지만 사용할 수 있습니다.",
                new Color(0.45f, 0.6f, 0.85f)
            ));

            Register(new SinInfo(
                SinType.Lust, "색욕", "Lust", 6,
                "행동력 +6\n무작위 적 1명과 아군 1명을 '매혹' 상태로 만듭니다.\n(매혹: 턴 시작 시 무작위 스킬을 0코스트로 자동 시전하며, 해당 턴 동안 직접 조종 불가)",
                new Color(0.95f, 0.4f, 0.7f)
            ));

            Register(new SinInfo(
                SinType.Wrath, "분노", "Wrath", 6,
                "행동력 +6\n모든 적과 모든 아군에게 힘 +3, 취약 +3을 동시에 부여합니다.",
                new Color(0.95f, 0.25f, 0.25f)
            ));

            Register(new SinInfo(
                SinType.Envy, "질투", "Envy", 6,
                "행동력 +6\n모든 적의 버프(공격력/보호막 등)를 복사하여 무작위 아군에게 부여하며, 모든 아군의 치유량이 -50% 감소합니다.",
                new Color(0.3f, 0.85f, 0.45f)
            ));

            Register(new SinInfo(
                SinType.Greed, "탐욕", "Greed", 10,
                "행동력 +10\n턴 종료 시 남은(미사용) 행동력 1당 모든 아군의 정신력이 -3 감소합니다.",
                new Color(1.0f, 0.8f, 0.2f)
            ));

            Register(new SinInfo(
                SinType.Pride, "오만", "Pride", 0,
                "행동력 +0\n이번 턴에 사용하는 모든 스킬의 비용이 0으로 고정되고 유지(Retain) 효과가 부여되지만, 스킬 사용 시마다 무작위 아군의 정신력이 -6 감소합니다.",
                new Color(0.7f, 0.35f, 0.95f)
            ));
        }

        private static void Register(SinInfo info)
        {
            _sinDatabase[info.sinType] = info;
        }

        public static SinInfo GetSinInfo(SinType type)
        {
            _sinDatabase.TryGetValue(type, out var info);
            return info;
        }

        public static List<SinType> GetAvailableSinPool(TrainCar nexusCar)
        {
            int level = nexusCar != null ? nexusCar.level : 0;
            List<SinType> pool = new List<SinType>();

            // Lv.0 기본: 식탐, 나태
            pool.Add(SinType.Gluttony);
            pool.Add(SinType.Sloth);

            // Lv.1 이상: 색욕
            if (level >= 1) pool.Add(SinType.Lust);

            // Lv.2 이상: 분노
            if (level >= 2) pool.Add(SinType.Wrath);

            // Lv.3 이상: 질투
            if (level >= 3) pool.Add(SinType.Envy);

            // Lv.4 이상: 탐욕
            if (level >= 4) pool.Add(SinType.Greed);

            // 파츠 [대죄: 오만]
            if (nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.SinPride))
            {
                pool.Add(SinType.Pride);
            }

            return pool;
        }

        public static SinType DrawNextSin(TrainCar nexusCar, SinActiveState state)
        {
            if (state == null) state = new SinActiveState();

            var availablePool = GetAvailableSinPool(nexusCar);

            // Bag이 비었거나, Bag 내의 죄악이 현재 가용 풀과 다르면 새로 채움
            state.sinBag.RemoveAll(s => !availablePool.Contains(s));
            if (state.sinBag.Count == 0)
            {
                state.sinBag = new List<SinType>(availablePool);
                // 셔플
                for (int i = 0; i < state.sinBag.Count; i++)
                {
                    int rand = UnityEngine.Random.Range(i, state.sinBag.Count);
                    var temp = state.sinBag[i];
                    state.sinBag[i] = state.sinBag[rand];
                    state.sinBag[rand] = temp;
                }
            }

            SinType selected = state.sinBag[0];
            state.sinBag.RemoveAt(0);
            return selected;
        }

        public static int CalculateSinAP(SinType sin, TrainCar nexusCar)
        {
            bool hasGreaterEvil = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.SinGreaterEvil);
            bool hasMartyrVow = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);

            if (sin == SinType.Pride) return 0; // 오만은 항상 0 AP

            if (hasMartyrVow)
            {
                return sin switch
                {
                    SinType.Gluttony => 3,
                    SinType.Sloth => 15,
                    SinType.Lust => 4,
                    SinType.Wrath => 4,
                    SinType.Envy => 8,
                    SinType.Greed => 20,
                    _ => 6
                };
            }

            int baseAp = sin switch
            {
                SinType.Gluttony => 6,
                SinType.Sloth => 12,
                SinType.Lust => 6,
                SinType.Wrath => 6,
                SinType.Envy => 6,
                SinType.Greed => 10,
                _ => 6
            };

            if (hasGreaterEvil)
            {
                baseAp *= 2; // 거악 프로토콜: 100% 증가
            }

            return baseAp;
        }

        public static string GetSinDetailedDescription(SinType sin, TrainCar nexusCar)
        {
            bool hasGreaterEvil = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.SinGreaterEvil);
            bool hasMartyrVow = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.SinMartyrVow);

            if (sin == SinType.Pride)
            {
                return "행동력 +0\n이번 턴 모든 스킬 비용 0 고정 & 유지\n스킬 사용 시마다 무작위 아군 정신력 -6 감소";
            }

            if (hasMartyrVow)
            {
                return sin switch
                {
                    SinType.Gluttony => "<color=#FFD700>[순교자의 서약] 식탐</color>\n행동력 +3\n적 처치 시 추가 행동력 +6 획득\n적 미처치 시 다음 턴 행동력 -9 감소",
                    SinType.Sloth => "<color=#FFD700>[순교자의 서약] 나태</color>\n행동력 +15\n턴 시작 시 무작위 스킬 3개를 0코스트로 자동 시전",
                    SinType.Lust => "<color=#FFD700>[순교자의 서약] 색욕</color>\n행동력 +4\n모든 아군과 모든 적을 '매혹' (전원 자동 난투)",
                    SinType.Wrath => "<color=#FFD700>[순교자의 서약] 분노</color>\n행동력 +4\n모든 적과 아군 힘 +12, 취약 +12 동시 부여",
                    SinType.Envy => "<color=#FFD700>[순교자의 서약] 질투</color>\n행동력 +8\n턴 시작 시 최저 체력 아군이 최고 체력 적의 체력 12%를 강탈",
                    SinType.Greed => "<color=#FFD700>[순교자의 서약] 탐욕</color>\n행동력 +20\n남은 행동력 1당 모든 아군 최대 정신력의 20% 피해",
                    _ => ""
                };
            }

            if (hasGreaterEvil)
            {
                return sin switch
                {
                    SinType.Gluttony => "<color=#FF5555>[거악 프로토콜] 식탐</color>\n행동력 +12\n적 처치 시 추가 행동력 +6 획득\n적 미처치 시 다음 턴 행동력 -6 감소",
                    SinType.Sloth => "<color=#FF5555>[거악 프로토콜] 나태</color>\n행동력 +24\n이번 턴 스킬 최대 3회 사용 가능",
                    SinType.Lust => "<color=#FF5555>[거악 프로토콜] 색욕</color>\n행동력 +12\n무작위 적 2명과 아군 2명을 '매혹'",
                    SinType.Wrath => "<color=#FF5555>[거악 프로토콜] 분노</color>\n행동력 +12\n모든 적과 아군 힘 +6, 취약 +6 동시 부여",
                    SinType.Envy => "<color=#FF5555>[거악 프로토콜] 질투</color>\n행동력 +12\n적 버프 2배 복사 아군 부여, 아군 치유량 -100%",
                    SinType.Greed => "<color=#FF5555>[거악 프로토콜] 탐욕</color>\n행동력 +20\n남은 행동력 1당 모든 아군 정신력 -6 감소",
                    _ => ""
                };
            }

            var info = GetSinInfo(sin);
            return info != null ? info.description : "";
        }
    }
}
