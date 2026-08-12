using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleRewardStage
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private BattleRewardSettings reward = new BattleRewardSettings();

    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "보상 단계" : displayName;
    public BattleRewardSettings Reward => reward;

    public BattleRewardStage(string id, string displayName, int gold, int cards)
    {
        this.id = id;
        this.displayName = displayName;
        reward = new BattleRewardSettings(gold, cards);
    }

    public void Normalize(int index)
    {
        if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(displayName)) displayName = $"{index + 1}단계";
        if (reward == null) reward = new BattleRewardSettings();
        reward.Normalize();
    }
}

[CreateAssetMenu(fileName = "BattleRewardTable", menuName = "TheLastArk/Battle/Battle Reward Table")]
public class BattleRewardTable : ScriptableObject
{
    public const string ResourcesPath = "Battle/BattleRewardTable";

    [SerializeField] private List<BattleRewardStage> stages = new List<BattleRewardStage>();

    public IReadOnlyList<BattleRewardStage> Stages => stages;
    public string DefaultStageId => stages != null && stages.Count > 0 ? stages[0].Id : string.Empty;

    public static BattleRewardTable LoadDefault()
    {
        return Resources.Load<BattleRewardTable>(ResourcesPath);
    }

    public BattleRewardSettings GetReward(string stageId)
    {
        BattleRewardStage stage = GetStage(stageId);
        return stage != null ? stage.Reward : null;
    }

    public BattleRewardStage GetStage(string stageId)
    {
        if (stages == null || stages.Count == 0) return null;
        foreach (BattleRewardStage stage in stages)
        {
            if (stage != null && string.Equals(stage.Id, stageId, StringComparison.Ordinal)) return stage;
        }
        return stages[0];
    }

    public void EnsureDefaults()
    {
        if (stages == null) stages = new List<BattleRewardStage>();
        if (stages.Count == 0)
        {
            stages.Add(new BattleRewardStage("easy", "1단계", 50, 1));
            stages.Add(new BattleRewardStage("normal", "2단계", 100, 1));
            stages.Add(new BattleRewardStage("hard", "3단계", 150, 2));
            stages.Add(new BattleRewardStage("very-hard", "4단계", 300, 3));
        }

        for (int i = 0; i < stages.Count; i++) stages[i]?.Normalize(i);
    }

    private void OnValidate() => EnsureDefaults();
}
