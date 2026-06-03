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
            "선로 한가운데, 온몸에 기름때와 굳은 피가 뒤섞인 한 남자가 위태롭게 서 있습니다. 그는 열차 앞에 무릎을 꿇으며, 품 안에서 빛나는 가방 하나를 꺼내 보입니다. \"제발...그놈들이 오고 있습니다! 저를 다음 구역까지만 태워다 주십시오. 여기서 잡히면 전 끝장입니다!\". 그 순간, 저 멀리 지평선 너머에서부터 추격대의 것으로 보이는 마법 엔진의 소리가 대기를 진동시키기 시작합니다.";
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
                    outcomeText = "당신은 남자를 열차의 화물칸에 밀어 넣습니다. 그는 약속대로 수많은 자원을 넘겨줍니다. 하지만 열차의 레이더에 수많은 적이 포착되기 시작합니다. 남자를 넘겨받기 위해 놈들이 수단과 방법을 가리지 않고 달려들 것이며, 당분간 선로는 피비린내 나는 격전지가 될 것입니다.",
                    probability = 100,
                    rewards = new List<EventReward>
                    {
                        new EventReward { rewardType = EventRewardType.GainGold, rewardValue = 30 },
                        new EventReward { rewardType = EventRewardType.GainRelic, rewardValue = 1, rewardDataID = "무작위 유물 1개" },
                        new EventReward { rewardType = EventRewardType.GainCard, rewardValue = 3, rewardDataID = "무작위 캐릭터 카드 3장" },
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
                    outcomeText = "당신은 냉정하게 거절하려 했으나, 그가 적을 끌어들일까 봐 우려됩니다. 당신은 비상용 자금 일부를 그에게 던져주며 선로 밖 거친 황무지를 가리킵니다. \"이걸로 어디 가서 흔적이라도 지워라. 우리 열차 근처에 얼씬도 하지 마.\" 남자는 절망적인 표정으로 돈을 챙겨 어둠 속으로 사라집니다. 당신의 주머니는 가벼워졌지만, 적어도 최악의 상황은 피했네요.",
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
                    outcomeText = "당신은 차갑게 열차의 문을 닫고 엔진을 가동합니다. 남자는 절규하며 열차 벽면을 두드리다 결국 뒤로 나자빠집니다. 멀어져 가는 열차 뒤로, 그가 내뱉는 처절한 저주와 원망이 통신기를 타고 대원들의 귓가에 울려 퍼집니다. \"너희도 똑같이 버림받을 것이다! 이 차가 지옥으로 가는 관이 되길 빌어주마!\" 비인도적인 선택을 내렸다는 죄책감과 남자의 섬뜩한 마지막 목소리가 대원들의 정신을 갉아먹습니다.",
                    probability = 100,
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
        if (AssetDatabase.LoadAssetAtPath<GameEventData>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
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
