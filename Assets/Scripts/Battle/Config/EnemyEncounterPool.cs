using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum CharacterCardCandidateRule
{
    SameFactionAsOwnedCharacters,
    SameRegionAsOwnedCharacters,
    CompletelyRandom
}

[System.Serializable]
public class BattleRewardSettings
{
    public bool giveGold = true;
    [Min(0)] public int goldAmount = BattleConfig.DefaultVictoryGold;
    public bool giveCharacterCard = true;
    [Min(1)] public int cardAmount = 1;
    public CharacterCardCandidateRule card1Rule = CharacterCardCandidateRule.SameFactionAsOwnedCharacters;
    public CharacterCardCandidateRule card2Rule = CharacterCardCandidateRule.SameRegionAsOwnedCharacters;
    public CharacterCardCandidateRule card3Rule = CharacterCardCandidateRule.CompletelyRandom;

    public BattleRewardSettings() { }

    public BattleRewardSettings(int gold, int cards)
    {
        goldAmount = gold;
        cardAmount = cards;
    }

    public CharacterCardCandidateRule GetCardRule(int index)
    {
        if (index == 0) return card1Rule;
        if (index == 1) return card2Rule;
        return card3Rule;
    }

    public void Normalize()
    {
        goldAmount = Mathf.Max(0, goldAmount);
        cardAmount = Mathf.Max(1, cardAmount);
    }
}

[CreateAssetMenu(fileName = "EnemyEncounterPool", menuName = "TheLastArk/Battle/Enemy Encounter Pool")]
public class EnemyEncounterPool : ScriptableObject
{
    public const string DefaultRegionId = "temp";

    [SerializeField] private string poolId;
    [SerializeField] private string displayName = "New Pool";
    [SerializeField] private string regionId = DefaultRegionId;
    [SerializeField] private NodeType nodeType = NodeType.Combat;
    [SerializeField, Min(1)] private int minFloor = 1;
    [SerializeField, Min(1)] private int maxFloor = 15;
    [SerializeField, Min(0)] private int minCombatCount;
    [SerializeField, Min(0)] private int maxCombatCount = 99;
    [SerializeField] private int priority;
    [SerializeField] private List<EnemyEncounterData> encounters = new List<EnemyEncounterData>();
    [SerializeField] private string rewardStageId;
    [SerializeField, HideInInspector, FormerlySerializedAs("difficulty")] private int legacyRewardStage = 1;

    public string PoolId => poolId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string RegionId => string.IsNullOrWhiteSpace(regionId) ? DefaultRegionId : regionId;
    public NodeType NodeType => nodeType;
    public int MinFloor => minFloor;
    public int MaxFloor => maxFloor;
    public int MinCombatCount => minCombatCount;
    public int MaxCombatCount => maxCombatCount;
    public int Priority => priority;
    public IReadOnlyList<EnemyEncounterData> Encounters => encounters;
    public IReadOnlyList<EnemyEncounterData> Formations => encounters;
    public string RewardStageId => string.IsNullOrWhiteSpace(rewardStageId) ? LegacyStageId : rewardStageId;
    public BattleRewardSettings ActiveReward
    {
        get
        {
            BattleRewardSettings reward = BattleRewardTable.LoadDefault()?.GetReward(RewardStageId);
            return reward ?? new BattleRewardSettings(BattleConfig.DefaultVictoryGold, 1);
        }
    }

    private string LegacyStageId => legacyRewardStage == 0 ? "easy"
        : legacyRewardStage == 2 ? "hard"
        : legacyRewardStage == 3 ? "very-hard"
        : "normal";

    public bool MatchesRegion(string targetRegionId)
    {
        string target = string.IsNullOrWhiteSpace(targetRegionId) ? DefaultRegionId : targetRegionId.Trim();
        return string.Equals(RegionId, target, System.StringComparison.OrdinalIgnoreCase);
    }

    public bool Matches(string targetRegionId, NodeType targetNodeType, int floor, int combatCount)
    {
        return MatchesRegion(targetRegionId)
            && nodeType == targetNodeType
            && floor >= minFloor
            && floor <= maxFloor
            && combatCount >= minCombatCount
            && combatCount <= maxCombatCount;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(poolId))
            poolId = System.Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(regionId))
            regionId = DefaultRegionId;

        minFloor = Mathf.Max(1, minFloor);
        maxFloor = Mathf.Max(minFloor, maxFloor);
        minCombatCount = Mathf.Max(0, minCombatCount);
        maxCombatCount = Mathf.Max(minCombatCount, maxCombatCount);
        if (encounters == null) encounters = new List<EnemyEncounterData>();
        if (string.IsNullOrWhiteSpace(rewardStageId)) rewardStageId = LegacyStageId;
    }
}
