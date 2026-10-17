using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TheLastArk.Editor
{
    public static class MirageEnemyContentUpdater
    {
        [MenuItem("Tools/Battle/Apply Mirage Enemy Specification")]
        public static void Apply()
        {
            var definitions = new List<CharacterData>(BasicEnemyAssetBootstrap.CreateDefinitions());
            definitions.AddRange(RemainingEnemyAssetBootstrap.Definitions());
            foreach (CharacterData definition in definitions)
            {
                string path = $"Assets/Resources/Characters/Enemy/Data/{definition.characterName}_Data.asset";
                CharacterData existing = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (existing == null) AssetDatabase.CreateAsset(definition, path);
                else
                {
                    CopyCombatDefinition(definition, existing);
                    EditorUtility.SetDirty(existing);
                    Object.DestroyImmediate(definition);
                }
            }
            // Update only the two formations whose companion count changed.
            foreach (string id in new[] { "late_supply", "late_fortress" })
            {
                var formation = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>($"Assets/Resources/Battle/Encounters/Content_{id}_Formation.asset");
                if (formation == null) continue;
                var serialized = new SerializedObject(formation);
                SerializedProperty slots = serialized.FindProperty("enemySlots");
                string companion = id == "late_supply" ? "흑철 쇠뇌수" : "가시 선인장";
                slots.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/Resources/Characters/Enemy/Data/{companion}_Data.asset");
                for (int i = 2; i < slots.arraySize; i++) slots.GetArrayElementAtIndex(i).objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            MirageMonsterArtImporter.BindAll();
            Debug.Log("[Mirage] Applied all 23 enemy combat definitions; illustration and icon references preserved.");
        }

        public static void CopyCombatDefinition(CharacterData source, CharacterData target)
        {
            foreach (var pattern in source.enemyPatterns)
            {
                var old = target.enemyPatterns.Find(p => p.patternName == pattern.patternName);
                if (old == null) continue;
                pattern.customVfxName = old.customVfxName;
                foreach (var action in pattern.actions)
                {
                    var previous = old.actions.Find(a => a.actionType == action.actionType && a.targetType == action.targetType
                        && a.effect?.type == action.effect?.type);
                    if (previous == null) continue;
                    action.intentIcon = previous.intentIcon;
                    if (action.effect == null || previous.effect == null) continue;
                    action.effect.statusIcon = previous.effect.statusIcon;
                    action.effect.customVfxName = previous.effect.customVfxName;
                }
            }
            foreach (var variable in source.enemyRuntimeVariables)
            {
                var previous = target.enemyRuntimeVariables.Find(v => v.id == variable.id);
                if (previous != null) variable.displayIcon = previous.displayIcon;
            }
            target.maxHp = source.maxHp;
            target.maxMental = source.maxMental;
            target.armor = source.armor;
            target.magicResist = source.magicResist;
            target.enemyPatterns = source.enemyPatterns;
            target.enemyRuntimeVariables = source.enemyRuntimeVariables;
            target.enemyRoleDescription = source.enemyRoleDescription;
            target.defeatGoldBonus = source.defeatGoldBonus;
            target.returnStolenGoldOnDefeat = source.returnStolenGoldOnDefeat;
            target.stolenGoldVariableId = source.stolenGoldVariableId;
        }
    }
}
