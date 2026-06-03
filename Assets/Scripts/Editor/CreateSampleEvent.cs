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
            "회색빛 먼지가 휘날리는 선로 위, 정체불명의 화물차가 멈춰 서 있습니다. 누군가 급하게 도망친 듯 주위에는 주인 없는 신발과 알 수 없는 물건들이 널려 있습니다.";
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
                    outcomeText = "당신은 버려진 차량을 수상하게 생각해 주변을 먼저 수색하기로 했습니다.\n\n무기를 집어넣고 조심스럽게 차 주변을 탐색하기 시작합니다.\n\n그러다 운 좋게도 아직 마력이 깃든 작은 배낭 하나를 발견합니다.\n\n당신은 혹시 모를 기습을 경계하며 신속하게 자리를 뜹니다.",
                    probability = 100,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.GainConsumable, rewardValue = 1, rewardDataID = "무작위 소모품" }
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
                    outcomeText = "당신은 차량의 문을 열어보기로 했습니다.\n\n단단하게 닫힌 뒷문에 지렛대를 박아넣고 힘껏 젖힙니다. 그 순간, 문에 새겨진 보안 마법이 침입자를 감지하고 찢어지는 듯한 소리와 함께 폭발합니다!\n\n자욱한 연기가 걷히자, 피투성이가 된 당신의 손엔 맥동하는 고대의 유물 하나가 들려 있습니다.",
                    probability = 100,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.GainRelic, rewardValue = 1, rewardDataID = "무작위 유물" },
                        new EventReward { rewardType = EventRewardType.TakeDamage, rewardValue = 7 }
                    }
                }
            }
        };

        // ─── 선택지 3: 연료로 쓸만한 것을 찾는다 ───
        EventOption option3 = new EventOption
        {
            optionText = "차량에서 연료로 쓸만한 것을 찾는다.",
            requirementType = EventRequirementType.RequireSense,
            requirementValue = 8,
            outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeText = "당신의 승무원 중 한 명이 숨을 죽이고 차체에 손을 올립니다.\n\n혼탁한 공기 속에서 가느다랗게 진동하는 '마력의 선'을 찾아내는 것이죠. 집중력이 극도에 달한 순간, \"철컥\" 하는 경쾌한 소리와 함께 화물차의 비밀 격벽이 스르르 열립니다. 그 안에는 수많은 부품과 보물이 보관되어 있습니다. 당신은 열차의 엔진을 더욱 강화할 수 있는 귀중한 자원을 확보했습니다.",
                    probability = 100,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.GainGold, rewardValue = 30 },
                        new EventReward { rewardType = EventRewardType.UpgradeTrainCar, rewardValue = 1 }
                    }
                }
            }
        };

        // ─── 선택지 등록 ───
        eventData.options = new List<EventOption> { option1, option2, option3 };

        // ─── 에셋 저장 ───
        string path = "Assets/Resources/Events/Common/Evt_AbandonedCargo.asset";
        if (AssetDatabase.LoadAssetAtPath<GameEventData>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
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
