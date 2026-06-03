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
        TakeMentalDamage,       // 정신력 감소
        GainCard,
        GainRelic,
        GainConsumable,
        UpgradeTrainCar,
        UpgradeNextBattles,     // 다음 N회 전투를 강적으로 대체
        LoseRelic,              // 유물 소실 (교환/소모)
        DamageTrainCar,         // 기차 칸 파손
        GainActionPoints        // 다음 N회 전투 행동력 보너스
    }

    public enum EventRequirementType
    {
        None,
        RequireGold,
        RequireHP,
        RequireSense,
        RequireRelic           // 특정 유물 보유 시 선택 가능
    }

    /// <summary>
    /// 개별 보상/페널티 하나를 나타냅니다.
    /// EventOutcome 안에 List로 들어가므로 복수 보상이 가능합니다.
    /// 예: 골드 +30, 유물 1개, 카드 3장을 한번에 지급
    /// </summary>
    [System.Serializable]
    public struct EventReward
    {
        public EventRewardType rewardType;
        public int rewardValue;           // 골드량, HP량, 수량 등
        public string rewardDataID;       // 유물/카드/소모품 ID (해당 시스템 구현 시 사용)
    }

    /// <summary>
    /// 하나의 확률 결과. 선택지 안에 여러 개가 들어감.
    /// 예: 성공(50%) → 유물 획득 / 실패(50%) → 체력 -5
    /// 보상이 여러 개일 수 있음: rewards 리스트에 복수 보상 등록.
    /// </summary>
    [System.Serializable]
    public struct EventOutcome
    {
        [Header("결과 텍스트")]
        [TextArea(2, 4)]
        public string outcomeText;        // "차량에서 자원을 획득했습니다!"

        [Header("확률 (0~100)")]
        [Range(0, 100)]
        public int probability;           // 이 결과가 발생할 확률

        [Header("보상/페널티 목록 (복수 가능)")]
        public List<EventReward> rewards;
    }

    /// <summary>
    /// 하나의 선택지. 결과가 여러 개(확률 분기)일 수 있음.
    /// 예: "조심스럽게 다가간다" → [성공 50%, 실패 50%]
    /// </summary>
    [System.Serializable]
    public struct EventOption
    {
        [Header("선택지 표시")]
        public string optionText;       // 유저에게 보여질 선택지 텍스트

        [Header("조건 (선택 시 필요/소모값)")]
        public EventRequirementType requirementType;
        public int requirementValue;    // 예: RequireGold 이고 값이 50이면 50골드 필요
        public string requirementDataID;  // RequireRelic일 때 유물 ID

        [Header("확률 결과 목록")]
        [Tooltip("확률의 합이 100이 되도록 설정하세요")]
        public List<EventOutcome> outcomes;
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

        [Tooltip("이미지 표시 위치 오프셋 (기본 0,0 - 원하는 위치로 조정 시 사용)")]
        public Vector2 imageOffset = Vector2.zero;

        [Tooltip("이미지 표시 배율 (기본 1.0 - 확대/축소 시 사용)")]
        public float imageScale = 1.0f;

        [Header("선택지 목록 (2~3개 권장)")]
        public List<EventOption> options = new List<EventOption>();
    }
}
