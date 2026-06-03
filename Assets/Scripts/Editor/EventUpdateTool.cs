using UnityEngine;
using UnityEditor;
using TheLastArk.Map.Events;
using System.Collections.Generic;

public class EventUpdateTool
{
    [MenuItem("TheLastArk/Update Events With Consumables")]
    public static void UpdateEvents()
    {
        string[] paths = {
            "Assets/Resources/Events/Common/Evt_AbandonedCargo.asset",
            "Assets/Resources/Events/Common/Evt_AbandonedEngine.asset",
            "Assets/Resources/Events/Common/Evt_LostWanderer.asset",
            "Assets/Resources/Events/Common/Evt_SuspiciousMerchant.asset"
        };
        
        string[] consumableIDs = {
            "FlameBurstTome",
            "FlameWallTome",
            "LeafOfLife",
            "NerveStabilizer"
        };
        
        string[] consumableNames = {
            "불꽃 폭발의 마법서",
            "화염 장벽의 마법서",
            "생명의 나뭇잎",
            "신경 안정제"
        };

        for (int i = 0; i < paths.Length; i++)
        {
            GameEventData data = AssetDatabase.LoadAssetAtPath<GameEventData>(paths[i]);
            if (data != null)
            {
                // Check if already added
                bool alreadyHasConsumable = false;
                foreach (var opt in data.options)
                {
                    if (opt.optionText.Contains("마법서") || opt.optionText.Contains("나뭇잎") || opt.optionText.Contains("안정제"))
                    {
                        alreadyHasConsumable = true;
                        break;
                    }
                }
                
                if (!alreadyHasConsumable)
                {
                    EventOption newOption = new EventOption();
                    newOption.optionText = $"[테스트] {consumableNames[i]} 챙기기";
                    newOption.requirementType = EventRequirementType.None;
                    newOption.outcomes = new List<EventOutcome>();
                    
                    EventOutcome outcome = new EventOutcome();
                    outcome.outcomeText = $"{consumableNames[i]}를 획득했습니다!";
                    outcome.probability = 100;
                    outcome.rewards = new List<EventReward>();
                    
                    EventReward reward = new EventReward();
                    reward.rewardType = EventRewardType.GainConsumable;
                    reward.rewardValue = 1;
                    reward.rewardDataID = consumableIDs[i];
                    
                    outcome.rewards.Add(reward);
                    newOption.outcomes.Add(outcome);
                    
                    data.options.Add(newOption);
                    EditorUtility.SetDirty(data);
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Events updated with Consumable options.");
    }
}
