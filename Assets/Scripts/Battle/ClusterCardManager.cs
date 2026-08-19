using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastArk.Data;

namespace TheLastArk.Battle
{
    public enum CardSuit
    {
        Spade,      // ♠ 스페이드
        Diamond,    // ♦ 다이아몬드
        Heart,      // ♥ 하트
        Clover      // ♣ 클로버
    }

    public enum PokerHandType
    {
        HighCard = 1,           // 하이 (+1 AP)
        OnePair = 2,            // 원 페어 (+2 AP)
        TwoPair = 3,            // 투 페어 (+3 AP)
        ThreeOfAKind = 4,       // 트리플 (+4 AP)
        Straight = 5,           // 스트레이트 (+5 AP)
        Flush = 6,              // 플러시 (+6 AP)
        FullHouse = 7,          // 풀하우스 (+7 AP)
        FourOfAKind = 8,        // 포카드 (+8 AP)
        StraightFlush = 9       // 스트레이트 플러쉬 (+9 AP)
    }

    [System.Serializable]
    public class ClusterCard
    {
        public int id;
        public int number; // 1 ~ 10
        public CardSuit suit;
        public bool isWildCard;

        // 와일드카드일 때 최적 평가로 결정된 수치/문양
        public int resolvedNumber;
        public CardSuit resolvedSuit;

        public ClusterCard(int id, int number, CardSuit suit, bool isWild = false)
        {
            this.id = id;
            this.number = number;
            this.suit = suit;
            this.isWildCard = isWild;
            this.resolvedNumber = number;
            this.resolvedSuit = suit;
        }

        public string SuitSymbol => isWildCard ? "★" : suit switch
        {
            CardSuit.Spade => "♠",
            CardSuit.Diamond => "♦",
            CardSuit.Heart => "♥",
            CardSuit.Clover => "♣",
            _ => "?"
        };

        public string SuitNameKorean => isWildCard ? "와일드" : suit switch
        {
            CardSuit.Spade => "스페이드",
            CardSuit.Diamond => "다이아몬드",
            CardSuit.Heart => "하트",
            CardSuit.Clover => "클로버",
            _ => "?"
        };

        public Color SuitColor => isWildCard ? new Color(1f, 0.85f, 0.2f, 1f) : suit switch
        {
            CardSuit.Spade => new Color(0.35f, 0.65f, 1f, 1f),       // 시안 블루
            CardSuit.Diamond => new Color(1f, 0.75f, 0.2f, 1f),      // 골드/오렌지
            CardSuit.Heart => new Color(1f, 0.35f, 0.45f, 1f),       // 루비 레드
            CardSuit.Clover => new Color(0.35f, 0.9f, 0.5f, 1f),      // 에메랄드 그린
            _ => Color.white
        };

        public string DisplayName => isWildCard ? "[와일드 카드]" : $"{SuitSymbol}{number}";
    }

    public class ClusterHandResult
    {
        public List<ClusterCard> cards = new List<ClusterCard>();
        public PokerHandType handType;
        public string handNameKorean;
        public int baseHandAP;
        public bool hasPatternAmplifier;
        public int amplifierAP;
        public int cloverBonusAP;
        public List<string> cloverTriggerDetails = new List<string>();
        public int totalGainedAP;

        // 문양 효과 상세
        public int spadeDamageTotal;
        public int diamondProtectionTotal;
        public int heartShieldTotal;

        public List<string> effectLogs = new List<string>();
        public string summary;
    }

    public static class ClusterCardManager
    {
        public static string GetHandNameKorean(PokerHandType handType)
        {
            return handType switch
            {
                PokerHandType.StraightFlush => "스트레이트 플러쉬",
                PokerHandType.FourOfAKind => "포카드",
                PokerHandType.FullHouse => "풀하우스",
                PokerHandType.Flush => "플러시",
                PokerHandType.Straight => "스트레이트",
                PokerHandType.ThreeOfAKind => "트리플",
                PokerHandType.TwoPair => "투 페어",
                PokerHandType.OnePair => "원 페어",
                _ => "하이 카드"
            };
        }

        public static int GetBaseHandAP(PokerHandType handType)
        {
            return (int)handType; // 1 ~ 9
        }

        public static int GetMaxRerolls(TrainCar nexusCar)
        {
            int baseRerolls = 2;
            int level = nexusCar != null ? nexusCar.level : 0;
            // Lv.0: 2회, Lv.1: 4회(+2), Lv.2: 6회(+4), Lv.3: 8회(+6)
            return baseRerolls + (level * 2);
        }

        public static int CalculateSuitValue(int number, int carLevel)
        {
            int divisor = Mathf.Max(1, 5 - carLevel); // 0강: 5, 1강: 4, 2강: 3, 3강: 2
            return number / divisor;
        }

        public static PokerHandType Evaluate5RawCards(List<(int number, CardSuit suit)> cards, bool canReverseFate)
        {
            if (cards == null || cards.Count != 5) return PokerHandType.HighCard;

            // 1. Flush 검사
            bool isFlush = true;
            CardSuit firstSuit = cards[0].suit;
            for (int i = 1; i < cards.Count; i++)
            {
                if (cards[i].suit != firstSuit)
                {
                    isFlush = false;
                    break;
                }
            }

            // 2. Straight 검사
            List<int> numbers = new List<int>();
            Dictionary<int, int> counts = new Dictionary<int, int>();
            foreach (var c in cards)
            {
                numbers.Add(c.number);
                if (!counts.ContainsKey(c.number)) counts[c.number] = 0;
                counts[c.number]++;
            }
            numbers.Sort();

            bool isStraight = false;
            // 중복 숫자가 없어야 스트레이트 가능
            if (counts.Count == 5)
            {
                // 기본 연속 검사
                if (numbers[4] - numbers[0] == 4 &&
                    numbers[1] == numbers[0] + 1 &&
                    numbers[2] == numbers[0] + 2 &&
                    numbers[3] == numbers[0] + 3)
                {
                    isStraight = true;
                }

                // [운명 역행기] 10과 1이 연결되는 Wrap-around 스트레이트 허용
                // 10-1-2-3-4 -> [1, 2, 3, 4, 10]
                // 9-10-1-2-3 -> [1, 2, 3, 9, 10]
                // 8-9-10-1-2 -> [1, 2, 8, 9, 10]
                // 7-8-9-10-1 -> [1, 7, 8, 9, 10]
                if (!isStraight && canReverseFate)
                {
                    if (numbers[0] == 1 && numbers[1] == 2 && numbers[2] == 3 && numbers[3] == 4 && numbers[4] == 10) isStraight = true;
                    else if (numbers[0] == 1 && numbers[1] == 2 && numbers[2] == 3 && numbers[3] == 9 && numbers[4] == 10) isStraight = true;
                    else if (numbers[0] == 1 && numbers[1] == 2 && numbers[2] == 8 && numbers[3] == 9 && numbers[4] == 10) isStraight = true;
                    else if (numbers[0] == 1 && numbers[1] == 7 && numbers[2] == 8 && numbers[3] == 9 && numbers[4] == 10) isStraight = true;
                }
            }

            // 3. 족보 매칭
            if (isStraight && isFlush) return PokerHandType.StraightFlush;

            List<int> countValues = new List<int>(counts.Values);
            countValues.Sort((a, b) => b.CompareTo(a)); // 내림차순 정렬

            if (countValues[0] == 4) return PokerHandType.FourOfAKind;
            if (countValues[0] == 3 && countValues[1] == 2) return PokerHandType.FullHouse;
            if (isFlush) return PokerHandType.Flush;
            if (isStraight) return PokerHandType.Straight;
            if (countValues[0] == 3) return PokerHandType.ThreeOfAKind;
            if (countValues[0] == 2 && countValues[1] == 2) return PokerHandType.TwoPair;
            if (countValues[0] == 2) return PokerHandType.OnePair;

            return PokerHandType.HighCard;
        }

        public static ClusterHandResult EvaluateHand(List<ClusterCard> currentCards, TrainCar nexusCar, bool simulateClover = true)
        {
            int level = nexusCar != null ? nexusCar.level : 0;
            bool canReverseFate = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.ClusterFateReverser);
            bool hasPatternAmp = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.ClusterPatternAmplifier);

            var result = new ClusterHandResult
            {
                cards = new List<ClusterCard>(currentCards),
                hasPatternAmplifier = hasPatternAmp
            };

            if (currentCards == null || currentCards.Count != 5)
            {
                result.handType = PokerHandType.HighCard;
                result.handNameKorean = GetHandNameKorean(PokerHandType.HighCard);
                result.baseHandAP = 1;
                result.totalGainedAP = 1;
                result.summary = "카드 수 부족";
                return result;
            }

            // 와일드카드 처리
            int wildIndex = currentCards.FindIndex(c => c.isWildCard);
            if (wildIndex >= 0)
            {
                // 와일드카드가 있는 경우: 40개 카드 중 최선의 족보 및 가장 유리한 문양/숫자 탐색
                PokerHandType bestHand = PokerHandType.HighCard;
                int bestNum = 10;
                CardSuit bestSuit = CardSuit.Spade;

                for (int s = 0; s < 4; s++)
                {
                    CardSuit testSuit = (CardSuit)s;
                    for (int num = 10; num >= 1; num--) // 높은 숫자 우선
                    {
                        var rawList = new List<(int, CardSuit)>();
                        for (int i = 0; i < 5; i++)
                        {
                            if (i == wildIndex) rawList.Add((num, testSuit));
                            else rawList.Add((currentCards[i].number, currentCards[i].suit));
                        }

                        PokerHandType eval = Evaluate5RawCards(rawList, canReverseFate);
                        if (eval > bestHand || (eval == bestHand && num > bestNum))
                        {
                            bestHand = eval;
                            bestNum = num;
                            bestSuit = testSuit;
                        }
                    }
                }

                currentCards[wildIndex].resolvedNumber = bestNum;
                currentCards[wildIndex].resolvedSuit = bestSuit;
                result.handType = bestHand;
            }
            else
            {
                var rawList = new List<(int, CardSuit)>();
                foreach (var c in currentCards)
                {
                    c.resolvedNumber = c.number;
                    c.resolvedSuit = c.suit;
                    rawList.Add((c.number, c.suit));
                }
                result.handType = Evaluate5RawCards(rawList, canReverseFate);
            }

            result.handNameKorean = GetHandNameKorean(result.handType);
            result.baseHandAP = GetBaseHandAP(result.handType);
            result.amplifierAP = hasPatternAmp ? 1 : 0;

            // 문양별 효과 계산
            int spadeDmg = 0;
            int diamondProt = 0;
            int heartShield = 0;
            int cloverBonusAP = 0;

            result.effectLogs.Clear();
            result.cloverTriggerDetails.Clear();

            for (int i = 0; i < currentCards.Count; i++)
            {
                var card = currentCards[i];
                int activeNum = card.resolvedNumber;
                CardSuit activeSuit = card.resolvedSuit;
                int suitVal = CalculateSuitValue(activeNum, level);

                string cardTag = card.isWildCard ? $"★와일드({card.resolvedSuit}{card.resolvedNumber})" : $"{card.SuitSymbol}{card.number}";

                switch (activeSuit)
                {
                    case CardSuit.Spade:
                        spadeDmg += suitVal;
                        result.effectLogs.Add($"{cardTag} -> 적 피해 {suitVal}");
                        break;

                    case CardSuit.Diamond:
                        diamondProt += suitVal;
                        result.effectLogs.Add($"{cardTag} -> 아군 보호 {suitVal}");
                        break;

                    case CardSuit.Heart:
                        heartShield += suitVal;
                        result.effectLogs.Add($"{cardTag} -> 아군 방어막 {suitVal}");
                        break;

                    case CardSuit.Clover:
                        int chance = activeNum * 7;
                        bool success = false;
                        if (simulateClover)
                        {
                            int roll = UnityEngine.Random.Range(0, 100);
                            success = roll < chance;
                        }
                        if (success)
                        {
                            cloverBonusAP += 1;
                            result.cloverTriggerDetails.Add($"[♣성공({chance}%)] +1 AP");
                            result.effectLogs.Add($"{cardTag} -> <color=#55FF77>♣ AP +1 획득! ({chance}%)</color>");
                        }
                        else
                        {
                            result.cloverTriggerDetails.Add($"[♣실패({chance}%)]");
                            result.effectLogs.Add($"{cardTag} -> ♣ AP 획득 실패 ({chance}%)");
                        }
                        break;
                }
            }

            result.spadeDamageTotal = spadeDmg;
            result.diamondProtectionTotal = diamondProt;
            result.heartShieldTotal = heartShield;
            result.cloverBonusAP = cloverBonusAP;

            result.totalGainedAP = Mathf.Max(1, result.baseHandAP + result.amplifierAP + result.cloverBonusAP);

            // Summary
            string bonusDesc = "";
            if (result.amplifierAP > 0) bonusDesc += " <color=#FFD700>[문양고정증폭 +1AP]</color>";
            if (result.cloverBonusAP > 0) bonusDesc += $" <color=#55FF77>[클로버 보너스 +{result.cloverBonusAP}AP]</color>";

            result.summary = $"족보: <b>{result.handNameKorean}</b> (+{result.baseHandAP}AP){bonusDesc} -> <b>총 {result.totalGainedAP} AP</b>";

            return result;
        }
    }

    public class ClusterDeckSession
    {
        public TrainCar nexusCar;
        public List<ClusterCard> deckPool = new List<ClusterCard>();
        public List<ClusterCard> discardedCards = new List<ClusterCard>();
        public List<ClusterCard> currentHand = new List<ClusterCard>();
        public int remainingRerolls;
        public bool hasFateResetPart;
        public bool hasFateResetUsed;

        public void StartNewTurnSession(TrainCar car)
        {
            nexusCar = car;
            deckPool.Clear();
            discardedCards.Clear();
            currentHand.Clear();

            // 1. 40장 기본 덱 생성 (1~10 숫자 x 4문양)
            int id = 1;
            for (int s = 0; s < 4; s++)
            {
                CardSuit suit = (CardSuit)s;
                for (int num = 1; num <= 10; num++)
                {
                    deckPool.Add(new ClusterCard(id++, num, suit, false));
                }
            }

            // 2. 와일드카드 파츠 확인
            if (nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.ClusterWildCard))
            {
                deckPool.Add(new ClusterCard(999, 10, CardSuit.Spade, true));
            }

            // 3. 셔플
            Shuffle(deckPool);

            // 4. 첫 5장 드로우
            for (int i = 0; i < 5 && deckPool.Count > 0; i++)
            {
                var card = deckPool[0];
                deckPool.RemoveAt(0);
                currentHand.Add(card);
            }

            // 5. 횟수 및 파츠 설정
            remainingRerolls = ClusterCardManager.GetMaxRerolls(nexusCar);
            hasFateResetPart = nexusCar != null && nexusCar.HasPartEffect(TrainPartEffectType.ClusterFateReset);
            hasFateResetUsed = false;
        }

        public bool CanReroll() => remainingRerolls > 0;

        public bool RerollSelected(List<int> selectedIndices)
        {
            if (selectedIndices == null || selectedIndices.Count == 0) return false;
            if (remainingRerolls <= 0) return false;

            // 선택된 인덱스의 카드를 discard에 넣고 새 카드로 교체
            foreach (int idx in selectedIndices)
            {
                if (idx < 0 || idx >= currentHand.Count) continue;
                var oldCard = currentHand[idx];
                discardedCards.Add(oldCard);

                if (deckPool.Count > 0)
                {
                    var newCard = deckPool[0];
                    deckPool.RemoveAt(0);
                    currentHand[idx] = newCard;
                }
            }

            remainingRerolls--;
            return true;
        }

        public bool FateReset()
        {
            if (!hasFateResetPart || hasFateResetUsed) return false;

            // 현재 핸드의 5장을 discard로 이동
            discardedCards.AddRange(currentHand);
            currentHand.Clear();

            // 새 5장 드로우
            for (int i = 0; i < 5 && deckPool.Count > 0; i++)
            {
                var card = deckPool[0];
                deckPool.RemoveAt(0);
                currentHand.Add(card);
            }

            hasFateResetUsed = true;
            return true;
        }

        private void Shuffle(List<ClusterCard> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, list.Count);
                var temp = list[i];
                list[i] = list[r];
                list[r] = temp;
            }
        }
    }
}
