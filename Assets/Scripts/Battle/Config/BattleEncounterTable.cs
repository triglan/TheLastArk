using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleEncounterTable", menuName = "TheLastArk/Battle/Battle Encounter Table")]
public class BattleEncounterTable : ScriptableObject
{
    public const string ResourcesPath = "Battle/BattleEncounterTable";

    [SerializeField] private List<EnemyEncounterPool> pools = new List<EnemyEncounterPool>();

    public IReadOnlyList<EnemyEncounterPool> Pools => pools;

    public static BattleEncounterTable LoadDefault()
    {
        return Resources.Load<BattleEncounterTable>(ResourcesPath);
    }
}
