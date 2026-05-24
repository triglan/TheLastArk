using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TheLastArk.Map.Events;

/// <summary>
/// 예시 이벤트 에셋 "버려진 화물차"를 자동 생성하는 에디터 도구.
/// 메뉴: TheLastArk > Create Sample Event (버려진 화물차)
/// </summary>
public class CreateSampleEvent : MonoBehaviour
{
    [MenuItem("TheLastArk/Create Sample Event (버려진 화물차)")]
    public static void Create()
    {
        // 저장 폴더 확인/생성 (Resources/Events/Common/)
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Events"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Events");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Events/Common"))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Events", "Common");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Events/Stage1"))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Events", "Stage1");
        }

        // ScriptableObject 생성
        GameEventData eventData = ScriptableObject.CreateInstance<GameEventData>();

        // ─── 기본 정보 ───
        eventData.eventID = "evt_abandoned_cargo";
        eventData.eventTitle = "버려진 화물차";
        eventData.eventDescription = 
            "선로 위에 멈춰 선 정체불명의 화물차를 발견하고 속도를 줄입니다.\n" +
            "누군가 급하게 버리고 간 흔적이 역력합니다.";
        eventData.eventImage = null; // 이미지는 나중에 수동 설정

        // ─── 선택지 1: 조심스럽게 다가간다 ───
        EventOption option1 = new EventOption
        {
            optionText = "조심스럽게 다가간다.",
            requirementType = EventRequirementType.None,
            requirementValue = 0,
            outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeText = "버려진 차량 주위를 수색하다 떨어져 있던 (무작위 소모품)을 획득했습니다.",
                    probability = 50,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.GainConsumable, rewardValue = 1, rewardDataID = "random_consumable" }
                    }
                },
                new EventOutcome
                {
                    outcomeText = "수색을 이어갔지만 아무것도 얻지 못했습니다.",
                    probability = 50,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.None }
                    }
                }
            }
        };

        // ─── 선택지 2: 차량의 문을 강제로 연다 ───
        EventOption option2 = new EventOption
        {
            optionText = "차량의 문을 강제로 연다.",
            requirementType = EventRequirementType.None,
            requirementValue = 0,
            outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeText = "차량 내부에 숨겨져 있던 (무작위 유물 1개)를 획득했습니다!",
                    probability = 30,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.GainRelic, rewardValue = 1, rewardDataID = "random_relic" }
                    }
                },
                new EventOutcome
                {
                    outcomeText = "문을 부수다가 튀어나온 파편에 맞았습니다. 모든 아군의 체력 -5.",
                    probability = 70,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.TakeDamage, rewardValue = 5 }
                    }
                }
            }
        };

        // ─── 선택지 3: 연료로 쓸만한 것을 찾는다 ───
        EventOption option3 = new EventOption
        {
            optionText = "차량에서 연료로 쓸만한 것을 찾는다.",
            requirementType = EventRequirementType.None,
            requirementValue = 0,
            outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeText = "차량에서 대량의 자원과 연료를 획득했습니다! (기차의 무작위 1칸)을 강화합니다.",
                    probability = 40,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.UpgradeTrainCar, rewardValue = 1 }
                    }
                },
                new EventOutcome
                {
                    outcomeText = "차량은 텅 비어있었습니다…",
                    probability = 60,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.None }
                    }
                }
            }
        };

        // ─── 선택지 등록 ───
        eventData.options = new List<EventOption> { option1, option2, option3 };

        // ─── 에셋 저장 ───
        string path = "Assets/Resources/Events/Common/Evt_AbandonedCargo.asset";
        AssetDatabase.CreateAsset(eventData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 생성된 에셋 선택
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = eventData;

        Debug.Log($"[CreateSampleEvent] '버려진 화물차' 이벤트가 '{path}'에 생성되었습니다!");
        EditorUtility.DisplayDialog("이벤트 생성 완료",
            "\"버려진 화물차\" 이벤트 에셋이 생성되었습니다!\n\n" +
            "위치: Assets/Resources/Events/Common/Evt_AbandonedCargo.asset\n\n" +
            "EventManager가 자동으로 로드합니다. 수동 등록 불필요!",
            "확인");
    }
}
