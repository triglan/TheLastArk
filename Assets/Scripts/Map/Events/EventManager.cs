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

        [Header("이벤트 풀 설정")]
        [Tooltip("모든 스테이지에서 공통으로 발생할 수 있는 이벤트 목록")]
        public List<GameEventData> commonEvents = new List<GameEventData>();
        
        [Tooltip("1스테이지 전용 이벤트 목록")]
        public List<GameEventData> stage1Events = new List<GameEventData>();

        // 이번 게임(런) 동안 한 번 발생한 이벤트 기록 (중복 발생 방지)
        private HashSet<string> seenEventIDs = new HashSet<string>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 새로운 게임 시작 시 발생 기록을 초기화합니다.
        /// </summary>
        public void ResetEventHistory()
        {
            seenEventIDs.Clear();
        }

        /// <summary>
        /// 현재 스테이지 상황에 맞는 무작위 이벤트를 1개 뽑아 반환합니다.
        /// (이미 발생한 이벤트는 제외됩니다.)
        /// </summary>
        public GameEventData GetRandomEvent(int currentStage = 1)
        {
            List<GameEventData> availablePool = new List<GameEventData>();

            // 1. 공용 이벤트 중 아직 보지 못한 이벤트 추가
            foreach(var ev in commonEvents)
            {
                if (ev != null && !seenEventIDs.Contains(ev.eventID))
                {
                    availablePool.Add(ev);
                }
            }

            // 2. 현재 스테이지 특화 이벤트 추가 (예: 1스테이지)
            if (currentStage == 1)
            {
                foreach(var ev in stage1Events)
                {
                    if (ev != null && !seenEventIDs.Contains(ev.eventID))
                    {
                        availablePool.Add(ev);
                    }
                }
            }

            // 3. 남은 이벤트가 전혀 없다면 null 반환 혹은 예비 이벤트
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

            return selectedEvent;
        }
    }
}
