using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Map.Events
{
    /// <summary>
    /// 게임 내 발생하는 이벤트를 관리하고, 무작위 이벤트를 뽑아주는 매니저 역할을 합니다.
    /// RunManager 등 전역 관리 객체에 붙여 사용합니다.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        private static EventManager instance;
        public static EventManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EventManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("EventManager");
                        instance = go.AddComponent<EventManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private List<GameEventData> commonEvents = new List<GameEventData>();

        // 이번 게임(런) 동안 한 번 발생한 이벤트 기록 (중복 발생 방지)
        private HashSet<string> seenEventIDs = new HashSet<string>();

        // 현재 진행 중인 이벤트 (씬 간 데이터 전달용)
        public GameEventData CurrentEvent { get; private set; }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadEventsFromResources();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Resources/Events/ 폴더에서 이벤트 에셋을 자동으로 로드합니다.
        /// - Resources/Events/Common/ → 공용 이벤트
        /// - Resources/Events/Stage{N}/ → 해당 스테이지 전용 이벤트
        /// </summary>
        private void LoadEventsFromResources()
        {
            commonEvents = new List<GameEventData>(Resources.LoadAll<GameEventData>("Events/Common"));
            stageEventMap.Clear();
            Debug.Log($"[EventManager] 공용 이벤트 {commonEvents.Count}개 로드 완료");
        }

        /// <summary>
        /// 새로운 게임 시작 시 발생 기록을 초기화합니다.
        /// </summary>
        public void ResetEventHistory()
        {
            seenEventIDs.Clear();
            CurrentEvent = null;
        }

        /// <summary>
        /// 현재 스테이지 상황에 맞는 무작위 이벤트를 1개 뽑아 반환합니다.
        /// (이미 발생한 이벤트는 제외됩니다.)
        /// </summary>
        public GameEventData GetRandomEvent(int currentStage = 1)
        {
            List<GameEventData> availablePool = new List<GameEventData>();

            // 1. 공용 이벤트 중 아직 보지 못한 이벤트 추가
            foreach (var ev in commonEvents)
            {
                if (ev != null && !seenEventIDs.Contains(ev.eventID))
                {
                    availablePool.Add(ev);
                }
            }

            // 2. 현재 스테이지 특화 이벤트 추가
            List<GameEventData> stagePool = GetStageEvents(currentStage);
            if (stagePool != null)
            {
                foreach (var ev in stagePool)
                {
                    if (ev != null && !seenEventIDs.Contains(ev.eventID))
                    {
                        availablePool.Add(ev);
                    }
                }
            }

            // 3. 남은 이벤트가 전혀 없다면 null 반환
            if (availablePool.Count == 0)
            {
                Debug.LogWarning("[EventManager] 발생 가능한 이벤트가 모두 소진되었습니다.");
                return null;
            }

            // 4. 무작위 선출
            int randomIndex = Random.Range(0, availablePool.Count);
            GameEventData selectedEvent = availablePool[randomIndex];

            // 5. 발생 기록 추가
            seenEventIDs.Add(selectedEvent.eventID);

            // 6. 현재 이벤트로 설정 (씬 전환 후 접근 가능)
            CurrentEvent = selectedEvent;

            return selectedEvent;
        }

        private Dictionary<int, List<GameEventData>> stageEventMap = new Dictionary<int, List<GameEventData>>();

        /// <summary>
        /// 스테이지 번호에 맞는 이벤트 풀을 반환합니다.
        /// Resources/Events/Stage{stage} 폴더에서 자동으로 이벤트를 로드합니다.
        /// </summary>
        private List<GameEventData> GetStageEvents(int stage)
        {
            if (!stageEventMap.ContainsKey(stage))
            {
                GameEventData[] loaded = Resources.LoadAll<GameEventData>($"Events/Stage{stage}");
                stageEventMap[stage] = new List<GameEventData>(loaded);
                Debug.Log($"[EventManager] Stage {stage} 전용 이벤트 {loaded.Length}개 로드 완료");
            }
            return stageEventMap[stage];
        }

        // ─────────────────────────────────────────────
        // 확률 판정
        // ─────────────────────────────────────────────

        /// <summary>
        /// 선택지의 결과 목록에서 확률에 따라 하나의 결과를 선택합니다.
        /// 확률의 합이 100이 아닌 경우에도 안전하게 동작합니다.
        /// </summary>
        public EventOutcome ResolveOutcome(EventOption option)
        {
            if (option.outcomes == null || option.outcomes.Count == 0)
            {
                Debug.LogWarning("[EventManager] 선택지에 결과가 없습니다.");
                return new EventOutcome
                {
                    outcomeText = "아무 일도 일어나지 않았다...",
                    probability = 100,
                    rewards = new System.Collections.Generic.List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.None }
                    }
                };
            }

            // 1개뿐이면 바로 반환
            if (option.outcomes.Count == 1)
            {
                return option.outcomes[0];
            }

            // 확률 합계 계산
            int totalWeight = 0;
            foreach (var outcome in option.outcomes)
            {
                totalWeight += outcome.probability;
            }

            if (totalWeight <= 0)
            {
                return option.outcomes[0];
            }

            // 가중치 기반 무작위 선택
            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var outcome in option.outcomes)
            {
                cumulative += outcome.probability;
                if (roll < cumulative)
                {
                    return outcome;
                }
            }

            // fallback
            return option.outcomes[option.outcomes.Count - 1];
        }

        // ─────────────────────────────────────────────
        // 보상/페널티 적용
        // ─────────────────────────────────────────────

        /// <summary>
        /// 결과에 따른 보상/페널티를 게임 상태에 적용합니다.
        /// 복수 보상을 지원합니다 (rewards 리스트 순회).
        /// 미구현 시스템은 로그만 출력합니다.
        /// </summary>
        public void ApplyRewards(EventOutcome outcome)
        {
            if (outcome.rewards == null || outcome.rewards.Count == 0)
            {
                Debug.Log("[EventManager] 보상 없음.");
                return;
            }

            foreach (var reward in outcome.rewards)
            {
                ApplySingleReward(reward);
            }
        }

        private void ApplySingleReward(EventReward reward)
        {
            switch (reward.rewardType)
            {
                case EventRewardType.None:
                    break;

                case EventRewardType.HealHP:
                    Debug.Log($"[EventManager] 모든 아군 HP +{reward.rewardValue} 회복!");
                    break;

                case EventRewardType.TakeDamage:
                    Debug.Log($"[EventManager] 모든 아군 HP -{reward.rewardValue} 피해!");
                    break;

                case EventRewardType.GainGold:
                    Debug.Log($"[EventManager] 골드 +{reward.rewardValue} 획득!");
                    TheLastArk.Managers.ResourceManager.Instance.AddGold(reward.rewardValue);
                    break;

                case EventRewardType.LoseGold:
                    Debug.Log($"[EventManager] 골드 -{reward.rewardValue} 소실!");
                    TheLastArk.Managers.ResourceManager.Instance.SpendGold(reward.rewardValue);
                    break;

                case EventRewardType.GainCard:
                    Debug.Log($"[EventManager] 카드 '{reward.rewardDataID}' x{reward.rewardValue} 획득!");
                    TheLastArk.Managers.ResourceManager.Instance.AddCharacterCard(reward.rewardDataID, reward.rewardValue);
                    break;

                case EventRewardType.GainRelic:
                    if (string.IsNullOrEmpty(reward.rewardDataID) || reward.rewardDataID == "Random" || reward.rewardDataID.Contains("무작위"))
                    {
                        var allRelics = Resources.LoadAll<TheLastArk.Data.RelicData>("Relics");
                        if (allRelics.Length > 0)
                        {
                            var randomRelic = allRelics[Random.Range(0, allRelics.Length)];
                            Debug.Log($"[EventManager] 무작위 유물 '{randomRelic.relicName}' 획득!");
                            TheLastArk.Managers.ResourceManager.Instance.AddRelic(randomRelic);
                        }
                    }
                    else
                    {
                        Debug.Log($"[EventManager] 유물 '{reward.rewardDataID}' 획득!");
                        var relic = Resources.Load<TheLastArk.Data.RelicData>($"Relics/{reward.rewardDataID}");
                        if (relic != null) TheLastArk.Managers.ResourceManager.Instance.AddRelic(relic);
                        else Debug.LogWarning($"[EventManager] 유물을 찾을 수 없습니다: Relics/{reward.rewardDataID}");
                    }
                    break;

                case EventRewardType.GainConsumable:
                    if (string.IsNullOrEmpty(reward.rewardDataID) || reward.rewardDataID == "Random" || reward.rewardDataID.Contains("무작위"))
                    {
                        var allConsumables = Resources.LoadAll<TheLastArk.Data.ConsumableData>("Consumables");
                        if (allConsumables.Length > 0)
                        {
                            var randomConsumable = allConsumables[Random.Range(0, allConsumables.Length)];
                            Debug.Log($"[EventManager] 무작위 소모품 '{randomConsumable.consumableName}' 획득!");
                            TheLastArk.Managers.ResourceManager.Instance.AddConsumable(randomConsumable);
                        }
                    }
                    else
                    {
                        Debug.Log($"[EventManager] 소모품 '{reward.rewardDataID}' 획득!");
                        var consumable = Resources.Load<TheLastArk.Data.ConsumableData>($"Consumables/{reward.rewardDataID}");
                        if (consumable != null) TheLastArk.Managers.ResourceManager.Instance.AddConsumable(consumable);
                        else Debug.LogWarning($"[EventManager] 소모품을 찾을 수 없습니다: Consumables/{reward.rewardDataID}");
                    }
                    break;

                case EventRewardType.UpgradeTrainCar:
                    Debug.Log($"[EventManager] 기차 칸 강화!");
                    break;

                case EventRewardType.TakeMentalDamage:
                    Debug.Log($"[EventManager] 모든 아군 정신력 -{reward.rewardValue}!");
                    break;

                case EventRewardType.UpgradeNextBattles:
                    Debug.Log($"[EventManager] 다음 {reward.rewardValue}회 전투가 강적 전투로 대체!");
                    break;

                case EventRewardType.LoseRelic:
                    Debug.Log($"[EventManager] 유물 '{reward.rewardDataID}' 소실!");
                    break;

                case EventRewardType.DamageTrainCar:
                    Debug.Log($"[EventManager] 기차 칸 파손!");
                    break;

                case EventRewardType.GainActionPoints:
                    Debug.Log($"[EventManager] 다음 {reward.rewardValue}회 전투 행동력 +{reward.rewardValue} 보너스!");
                    break;

                default:
                    Debug.LogWarning($"[EventManager] 알 수 없는 보상 타입: {reward.rewardType}");
                    break;
            }
        }
    }
}
