using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyEncounter", menuName = "TheLastArk/Battle/Enemy Encounter")]
public class EnemyEncounterData : ScriptableObject
{
    public const int SlotCount = 4;

    [SerializeField] private string encounterId;
    [SerializeField] private string displayName = "New Encounter";
    [SerializeField] private CharacterData[] enemySlots = new CharacterData[SlotCount];

    public string EncounterId => encounterId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public CharacterData[] EnemySlots => enemySlots;

    public bool HasAnyEnemy
    {
        get
        {
            if (enemySlots == null) return false;
            foreach (CharacterData enemy in enemySlots)
            {
                if (enemy != null) return true;
            }
            return false;
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(encounterId))
            encounterId = Guid.NewGuid().ToString("N");

        if (enemySlots == null || enemySlots.Length != SlotCount)
        {
            CharacterData[] resized = new CharacterData[SlotCount];
            if (enemySlots != null)
                Array.Copy(enemySlots, resized, Mathf.Min(enemySlots.Length, SlotCount));
            enemySlots = resized;
        }

        for (int i = 0; i < enemySlots.Length; i++)
        {
            CharacterData enemy = enemySlots[i];
            if (enemy != null && !enemy.isEnemy)
                Debug.LogWarning($"[EnemyEncounterData] Slot {i} references non-enemy data: {enemy.name}", this);
        }
    }
}
