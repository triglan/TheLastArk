using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TheLastArk.Map.Events;

/// <summary>
/// 예시 이벤트 "길 잃은 방랑자" 자동 생성.
/// 메뉴: TheLastArk > Create Sample Event (길 잃은 방랑자)
/// </summary>
public class CreateSampleEvent_Wanderer : MonoBehaviour
{
    [MenuItem("TheLastArk/Create Sample Event (길 잃은 방랑자)")]
    public static void Create()
    {
        // 폴더 확인/생성
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Events"))
            AssetDatabase.CreateFolder("Assets/Resources", "Events");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Events/Common"))
            AssetDatabase.CreateFolder("Assets/Resources/Events", "Common");

        GameEventData eventData = ScriptableObject.CreateInstance<GameEventData>();

        // ─── 기본 정보 ───
        eventData.eventID = "evt_lost_wanderer";
        eventData.eventTitle = "길 잃은 방랑자";
        eventData.eventDescription =
            "어딘가 불안해 보이는 방랑자가 선로를 막고 있습니다.\n" +
            "그는 이 곳을 탈출하고 싶다며 모든 자원을 넘겨줄테니\n" +
            "자신을 다른 지역으로 데려다 달라고 합니다.";
        eventData.eventImage = null;

        // ─── 선택지 1: 제안을 수락한다 ───
        EventOption option1 = new EventOption
        {
            optionText = "제안을 수락한다.",
            requirementType = EventRequirementType.None,
            requirementValue = 0,
            outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeText = "확정: 30 골드, (무작위 유물 1개), (무작위 캐릭터 카드 3장)을 얻습니다. 다음 2회의 일반 전투가 강적 전투로 대체됩니다.",
                    probability = 100,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.GainGold, rewardValue = 30 },
                        new EventReward { rewardType = EventRewardType.GainRelic, rewardValue = 1 },
                        new EventReward { rewardType = EventRewardType.GainCard, rewardValue = 3 },
                        new EventReward { rewardType = EventRewardType.UpgradeNextBattles, rewardValue = 2 }
                    }
                }
            }
        };

        // ─── 선택지 2: 소량의 자원을 제공하고 돌려보낸다 ───
        EventOption option2 = new EventOption
        {
            optionText = "소량의 자원을 제공하고 돌려보낸다.",
            requirementType = EventRequirementType.None,
            requirementValue = 0,
            outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeText = "확정: 30골드를 잃습니다. 아무 일도 일어나지 않습니다.",
                    probability = 100,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.LoseGold, rewardValue = 30 }
                    }
                }
            }
        };

        // ─── 선택지 3: 제안을 거절한다 ───
        EventOption option3 = new EventOption
        {
            optionText = "제안을 거절한다.",
            requirementType = EventRequirementType.None,
            requirementValue = 0,
            outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeText = "아무 일도 일어나지 않습니다.",
                    probability = 50,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.None }
                    }
                },
                new EventOutcome
                {
                    outcomeText = "방랑자가 분노하여 저주의 말을 퍼붓습니다. 모든 아군의 정신력 -7.",
                    probability = 50,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.TakeMentalDamage, rewardValue = 7 }
                    }
                }
            }
        };

        eventData.options = new List<EventOption> { option1, option2, option3 };

        // ─── 에셋 저장 ───
        string path = "Assets/Resources/Events/Common/Evt_LostWanderer.asset";
        AssetDatabase.CreateAsset(eventData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = eventData;

        Debug.Log($"[CreateSampleEvent] '길 잃은 방랑자' 이벤트가 '{path}'에 생성되었습니다!");
        EditorUtility.DisplayDialog("이벤트 생성 완료",
            "\"길 잃은 방랑자\" 이벤트 에셋이 생성되었습니다!\n\n" +
            "위치: Assets/Resources/Events/Common/Evt_LostWanderer.asset\n\n" +
            "EventManager가 자동으로 로드합니다.",
            "확인");
    }
}
