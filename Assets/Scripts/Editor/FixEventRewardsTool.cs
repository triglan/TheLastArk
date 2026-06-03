using UnityEngine;
using UnityEditor;
using TheLastArk.Map.Events;
using System.Collections.Generic;
using System.Linq;

public class FixEventRewardsTool
{
    [MenuItem("TheLastArk/Fix Event Rewards")]
    public static void FixRewards()
    {
        string[] paths = {
            "Assets/Resources/Events/Common/Evt_AbandonedCargo.asset",
            "Assets/Resources/Events/Common/Evt_AbandonedEngine.asset",
            "Assets/Resources/Events/Common/Evt_LostWanderer.asset",
            "Assets/Resources/Events/Common/Evt_SuspiciousMerchant.asset"
        };
        
        foreach (string path in paths)
        {
            GameEventData data = AssetDatabase.LoadAssetAtPath<GameEventData>(path);
            if (data == null) continue;

            bool modified = false;

            // 1. Remove test options
            int removedCount = data.options.RemoveAll(o => o.optionText.StartsWith("[테스트]"));
            if (removedCount > 0) modified = true;

            // 2. Add Gold to any outcome that gives a random consumable
            for (int i = 0; i < data.options.Count; i++)
            {
                EventOption option = data.options[i];
                for (int j = 0; j < option.outcomes.Count; j++)
                {
                    EventOutcome outcome = option.outcomes[j];
                    bool givesConsumable = outcome.rewards.Any(r => r.rewardType == EventRewardType.GainConsumable);
                    bool givesGold = outcome.rewards.Any(r => r.rewardType == EventRewardType.GainGold);
                    
                    if (givesConsumable && !givesGold)
                    {
                        // Add 50 Gold reward
                        EventReward goldReward = new EventReward();
                        goldReward.rewardType = EventRewardType.GainGold;
                        goldReward.rewardValue = 50;
                        outcome.rewards.Add(goldReward);
                        
                        // Append text
                        if (!outcome.outcomeText.Contains("50 골드"))
                        {
                            outcome.outcomeText += "\n\n추가로 50 골드를 발견했습니다!";
                        }
                        
                        // Struct needs to be assigned back
                        option.outcomes[j] = outcome;
                        modified = true;
                    }
                }
                data.options[i] = option;
            }

            if (modified)
            {
                EditorUtility.SetDirty(data);
                Debug.Log($"Fixed rewards for {data.name}");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Event rewards fixed successfully.");
    }
}
