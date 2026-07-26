using System.Collections.Generic;
using UnityEngine;

public static class BattleEncounterResolver
{
    public static EnemyEncounterData Resolve(
        BattleEncounterTable table,
        string regionId,
        NodeType nodeType,
        int floor,
        int combatCount,
        IReadOnlyCollection<string> appearedEncounterIds)
    {
        if (table == null) return null;

        List<EnemyEncounterPool> matchedPools = new List<EnemyEncounterPool>();
        int highestPriority = int.MinValue;

        foreach (EnemyEncounterPool pool in table.Pools)
        {
            if (pool == null || !pool.Matches(regionId, nodeType, floor, combatCount)) continue;

            if (pool.Priority > highestPriority)
            {
                highestPriority = pool.Priority;
                matchedPools.Clear();
            }

            if (pool.Priority == highestPriority)
                matchedPools.Add(pool);
        }

        if (matchedPools.Count == 0) return null;

        EnemyEncounterPool selectedPool = matchedPools[Random.Range(0, matchedPools.Count)];
        List<EnemyEncounterData> valid = CollectValidEncounters(selectedPool, appearedEncounterIds, false);

        // Once every formation in this pool has appeared, duplicates become valid again.
        if (valid.Count == 0)
            valid = CollectValidEncounters(selectedPool, appearedEncounterIds, true);

        return valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : null;
    }

    private static List<EnemyEncounterData> CollectValidEncounters(
        EnemyEncounterPool pool,
        IReadOnlyCollection<string> appearedEncounterIds,
        bool allowDuplicates)
    {
        List<EnemyEncounterData> result = new List<EnemyEncounterData>();
        foreach (EnemyEncounterData encounter in pool.Encounters)
        {
            if (encounter == null || !encounter.HasAnyEnemy) continue;
            if (!allowDuplicates && HasAppeared(appearedEncounterIds, encounter.EncounterId)) continue;
            result.Add(encounter);
        }
        return result;
    }

    private static bool HasAppeared(IReadOnlyCollection<string> appearedEncounterIds, string encounterId)
    {
        if (appearedEncounterIds == null) return false;
        foreach (string appearedId in appearedEncounterIds)
        {
            if (string.Equals(appearedId, encounterId, System.StringComparison.Ordinal)) return true;
        }
        return false;
    }
}
