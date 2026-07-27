using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleEncounterTable", menuName = "TheLastArk/Battle/Battle Encounter Table")]
public class BattleEncounterTable : ScriptableObject
{
    public const string ResourcesPath = "Battle/BattleEncounterTable";

    [SerializeField] private List<EnemyEncounterRegionPools> regions = new List<EnemyEncounterRegionPools>();

    // Legacy flat list. Kept so existing assets do not lose references before they are grouped by region.
    [SerializeField] private List<EnemyEncounterPool> pools = new List<EnemyEncounterPool>();

    public IReadOnlyList<EnemyEncounterRegionPools> Regions => regions;
    public IReadOnlyList<EnemyEncounterPool> LegacyPools => pools;

    public static BattleEncounterTable LoadDefault()
    {
        return Resources.Load<BattleEncounterTable>(ResourcesPath);
    }

    public IEnumerable<EnemyEncounterPool> GetPoolsForRegion(string regionId)
    {
        string targetRegionId = NormalizeRegionId(regionId);
        HashSet<EnemyEncounterPool> emitted = new HashSet<EnemyEncounterPool>();

        if (regions != null)
        {
            foreach (EnemyEncounterRegionPools region in regions)
            {
                if (region == null || !region.Matches(targetRegionId)) continue;
                foreach (EnemyEncounterPool pool in region.Pools)
                {
                    if (pool == null || emitted.Contains(pool)) continue;
                    emitted.Add(pool);
                    yield return pool;
                }
            }
        }

        if (pools == null) yield break;

        foreach (EnemyEncounterPool pool in pools)
        {
            if (pool == null || emitted.Contains(pool) || !pool.MatchesRegion(targetRegionId)) continue;
            emitted.Add(pool);
            yield return pool;
        }
    }

    public IEnumerable<EnemyEncounterPool> GetAllPools()
    {
        HashSet<EnemyEncounterPool> emitted = new HashSet<EnemyEncounterPool>();

        if (regions != null)
        {
            foreach (EnemyEncounterRegionPools region in regions)
            {
                if (region == null) continue;
                foreach (EnemyEncounterPool pool in region.Pools)
                {
                    if (pool == null || emitted.Contains(pool)) continue;
                    emitted.Add(pool);
                    yield return pool;
                }
            }
        }

        if (pools == null) yield break;

        foreach (EnemyEncounterPool pool in pools)
        {
            if (pool == null || emitted.Contains(pool)) continue;
            emitted.Add(pool);
            yield return pool;
        }
    }

    public void RegisterPool(EnemyEncounterPool pool)
    {
        if (pool == null) return;
        if (regions == null) regions = new List<EnemyEncounterRegionPools>();

        string targetRegionId = NormalizeRegionId(pool.RegionId);
        EnemyEncounterRegionPools region = EnsureRegion(targetRegionId);

        region.AddPool(pool);
        RemovePoolFromOtherRegions(pool, targetRegionId);
    }

    public EnemyEncounterRegionPools EnsureRegion(string regionId)
    {
        if (regions == null) regions = new List<EnemyEncounterRegionPools>();

        string targetRegionId = NormalizeRegionId(regionId);
        EnemyEncounterRegionPools region = FindRegion(targetRegionId);
        if (region != null) return region;

        region = new EnemyEncounterRegionPools(targetRegionId);
        regions.Add(region);
        return region;
    }

    public void UnregisterPool(EnemyEncounterPool pool)
    {
        if (pool == null || regions == null) return;

        foreach (EnemyEncounterRegionPools region in regions)
        {
            region?.RemovePool(pool);
        }
    }

    public void SyncRegionsFromPools()
    {
        if (regions == null) regions = new List<EnemyEncounterRegionPools>();

        foreach (EnemyEncounterRegionPools region in regions)
        {
            region?.RemoveNullPools();
        }

        if (pools != null)
        {
            foreach (EnemyEncounterPool pool in pools)
            {
                RegisterPool(pool);
            }
        }

        List<EnemyEncounterRegionPools> existingRegions = new List<EnemyEncounterRegionPools>(regions);
        foreach (EnemyEncounterRegionPools region in existingRegions)
        {
            if (region == null) continue;
            List<EnemyEncounterPool> existingPools = new List<EnemyEncounterPool>(region.Pools);
            foreach (EnemyEncounterPool pool in existingPools)
            {
                if (pool == null) continue;
                RegisterPool(pool);
            }
        }
    }

    private EnemyEncounterRegionPools FindRegion(string regionId)
    {
        if (regions == null) return null;
        foreach (EnemyEncounterRegionPools region in regions)
        {
            if (region != null && region.Matches(regionId)) return region;
        }
        return null;
    }

    private void RemovePoolFromOtherRegions(EnemyEncounterPool pool, string owningRegionId)
    {
        if (regions == null) return;
        foreach (EnemyEncounterRegionPools region in regions)
        {
            if (region == null || region.Matches(owningRegionId)) continue;
            region.RemovePool(pool);
        }
    }

    private static string NormalizeRegionId(string regionId)
    {
        return string.IsNullOrWhiteSpace(regionId) ? EnemyEncounterPool.DefaultRegionId : regionId.Trim();
    }

    private void OnValidate()
    {
        if (regions == null) regions = new List<EnemyEncounterRegionPools>();
        if (pools == null) pools = new List<EnemyEncounterPool>();
        foreach (EnemyEncounterRegionPools region in regions)
        {
            region?.Normalize();
        }
    }
}

[System.Serializable]
public class EnemyEncounterRegionPools
{
    [SerializeField] private string regionId = EnemyEncounterPool.DefaultRegionId;
    [SerializeField] private List<EnemyEncounterPool> pools = new List<EnemyEncounterPool>();

    public string RegionId => string.IsNullOrWhiteSpace(regionId) ? EnemyEncounterPool.DefaultRegionId : regionId.Trim();
    public IReadOnlyList<EnemyEncounterPool> Pools => pools;

    public EnemyEncounterRegionPools()
    {
    }

    public EnemyEncounterRegionPools(string regionId)
    {
        this.regionId = string.IsNullOrWhiteSpace(regionId) ? EnemyEncounterPool.DefaultRegionId : regionId.Trim();
        pools = new List<EnemyEncounterPool>();
    }

    public bool Matches(string targetRegionId)
    {
        string target = string.IsNullOrWhiteSpace(targetRegionId) ? EnemyEncounterPool.DefaultRegionId : targetRegionId.Trim();
        return string.Equals(RegionId, target, System.StringComparison.OrdinalIgnoreCase);
    }

    public void AddPool(EnemyEncounterPool pool)
    {
        if (pool == null) return;
        if (pools == null) pools = new List<EnemyEncounterPool>();
        if (!pools.Contains(pool)) pools.Add(pool);
    }

    public void RemovePool(EnemyEncounterPool pool)
    {
        if (pools == null || pool == null) return;
        pools.Remove(pool);
    }

    public void RemoveNullPools()
    {
        if (pools == null) return;
        for (int i = pools.Count - 1; i >= 0; i--)
        {
            if (pools[i] == null) pools.RemoveAt(i);
        }
    }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(regionId)) regionId = EnemyEncounterPool.DefaultRegionId;
        else regionId = regionId.Trim();
        if (pools == null) pools = new List<EnemyEncounterPool>();
        RemoveNullPools();
    }
}
