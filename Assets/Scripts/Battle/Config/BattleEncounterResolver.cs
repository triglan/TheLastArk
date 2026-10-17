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
        IReadOnlyCollection<string> appearedEncounterIds,
        out EnemyEncounterPool resolvedPool)
    {
        resolvedPool = null;
        if (table == null) table = BattleEncounterTable.LoadDefault();

        List<EnemyEncounterPool> matchedPools = new List<EnemyEncounterPool>();
        int highestPriority = int.MinValue;

        if (table != null)
        {
            foreach (EnemyEncounterPool pool in table.GetPoolsForRegion(regionId))
            {
                if (pool == null || !pool.Matches(regionId, nodeType, floor, combatCount)) continue;
                if (CollectValidEncounters(pool, appearedEncounterIds, true).Count == 0) continue;

                if (pool.Priority > highestPriority)
                {
                    highestPriority = pool.Priority;
                    matchedPools.Clear();
                }

                if (pool.Priority == highestPriority)
                    matchedPools.Add(pool);
            }
        }

        // 1. 정확히 매칭되는 인카운터 풀이 있는 경우
        if (matchedPools.Count > 0)
        {
            EnemyEncounterPool selectedPool = matchedPools[Random.Range(0, matchedPools.Count)];
            List<EnemyEncounterData> valid = CollectValidEncounters(selectedPool, appearedEncounterIds, false);

            if (valid.Count == 0)
                valid = CollectValidEncounters(selectedPool, appearedEncounterIds, true);

            if (valid.Count > 0)
            {
                resolvedPool = selectedPool;
                return valid[Random.Range(0, valid.Count)];
            }
        }

        // Never cross region, node type or floor boundaries when content is missing.
        return null;
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
