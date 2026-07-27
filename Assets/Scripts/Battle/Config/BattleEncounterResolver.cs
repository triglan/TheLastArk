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
        if (table == null) table = BattleEncounterTable.LoadDefault();

        List<EnemyEncounterPool> matchedPools = new List<EnemyEncounterPool>();
        int highestPriority = int.MinValue;

        if (table != null && table.Pools != null)
        {
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
        }

        // 1. 정확히 매칭되는 인카운터 풀이 있는 경우
        if (matchedPools.Count > 0)
        {
            EnemyEncounterPool selectedPool = matchedPools[Random.Range(0, matchedPools.Count)];
            List<EnemyEncounterData> valid = CollectValidEncounters(selectedPool, appearedEncounterIds, false);

            if (valid.Count == 0)
                valid = CollectValidEncounters(selectedPool, appearedEncounterIds, true);

            if (valid.Count > 0)
                return valid[Random.Range(0, valid.Count)];
        }

        // 2. Fallback 1: regionId, floor 무시하고 nodeType에 맞는 임의의 풀 검색
        if (table != null && table.Pools != null)
        {
            List<EnemyEncounterData> fallbackEncounters = new List<EnemyEncounterData>();
            foreach (EnemyEncounterPool pool in table.Pools)
            {
                if (pool == null || pool.NodeType != nodeType) continue;
                var list = CollectValidEncounters(pool, appearedEncounterIds, true);
                fallbackEncounters.AddRange(list);
            }
            if (fallbackEncounters.Count > 0)
                return fallbackEncounters[Random.Range(0, fallbackEncounters.Count)];
        }

        // 3. Fallback 2: Resources에 존재하는 모든 EnemyEncounterData 에셋 검색
        var allEncounters = Resources.LoadAll<EnemyEncounterData>("");
        if (allEncounters != null && allEncounters.Length > 0)
        {
            List<EnemyEncounterData> validAll = new List<EnemyEncounterData>();
            foreach (var enc in allEncounters)
            {
                if (enc != null && enc.HasAnyEnemy) validAll.Add(enc);
            }
            if (validAll.Count > 0) return validAll[Random.Range(0, validAll.Count)];
        }

        // 4. Fallback 3: 등록된 적 캐릭터(Enemy CharacterData)로 런타임 인카운터 동적 생성
        var enemyChars = Resources.LoadAll<CharacterData>("Characters/Enemy");
        if (enemyChars == null || enemyChars.Length == 0)
        {
            enemyChars = Resources.LoadAll<CharacterData>("");
        }

        if (enemyChars != null && enemyChars.Length > 0)
        {
            List<CharacterData> validEnemies = new List<CharacterData>();
            foreach (var c in enemyChars)
            {
                if (c != null && c.isEnemy) validEnemies.Add(c);
            }
            if (validEnemies.Count == 0)
            {
                foreach (var c in enemyChars) if (c != null) validEnemies.Add(c);
            }

            if (validEnemies.Count > 0)
            {
                CharacterData selectedEnemy = validEnemies[Random.Range(0, validEnemies.Count)];
                return EnemyEncounterData.CreateRuntimeInstance("DynamicFallback", "야생의 적 연합", new CharacterData[4] { selectedEnemy, null, null, null });
            }
        }

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
