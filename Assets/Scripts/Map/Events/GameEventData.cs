using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Map.Events
{
    public enum EventRewardType
    {
        None,
        GainGold,
        LoseGold,
        HealHP,
        TakeDamage,
        GainCard,
        GainRelic
    }

    public enum EventRequirementType
    {
        None,
        RequireGold,
        RequireHP
    }

    [System.Serializable]
    public struct EventOption
    {
        [Header("선택지 표시")]
        public string optionText;       // 유저에게 보여질 선택지 텍스트 (예: "싸운다", "도망친다")
        
        [Header("조건 (선택 시 필요/소모값)")]
        public EventRequirementType requirementType;
        public int requirementValue;    // 예: RequireGold 이고 값이 50이면 50골드 필요

        [Header("결과")]
        [TextArea(2, 4)]
        public string resultText;       // 선택 후 보여질 결과 텍스트 안내문
        public EventRewardType rewardType;
        public int rewardValue;         // 예: GainGold 이고 값이 100이면 100골드 획득
        // 필요에 따라 Card나 Relic ID 등 추가 데이터를 확장할 수 있습니다.
        public string rewardDataID;     
    }

    /// <summary>
    /// 무작위 발생 이벤트 하나의 정보를 담는 ScriptableObject 템플릿
    /// 우클릭 -> Create -> TheLastArk -> Map Event Data 메뉴로 생성 가능합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapEvent", menuName = "TheLastArk/Map Event Data", order = 1)]
    public class GameEventData : ScriptableObject
    {
        [Header("이벤트 기본 정보")]
        [Tooltip("중복 발생 방지에 사용할 고유 ID")]
        public string eventID;

        [Tooltip("이벤트 이름 (UI 상단 제목)")]
        public string eventTitle;

        [Tooltip("이벤트 상황 설명문")]
        [TextArea(5, 10)]
        public string eventDescription;

        [Tooltip("이벤트 상황 묘사용 이미지 (아이콘 등)")]
        public Sprite eventImage;

        [Header("선택지 목록 (2~3개 권장)")]
        public List<EventOption> options = new List<EventOption>();
    }
}
