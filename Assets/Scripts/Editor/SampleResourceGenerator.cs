using UnityEngine;
using UnityEditor;
using TheLastArk.Data;
using System.IO;

namespace TheLastArk.EditorScripts
{
    public class SampleResourceGenerator
    {
        [MenuItem("TheLastArk/Generate Sample Resources")]
        public static void GenerateSamples()
        {
            EnsureDirectoryExists("Assets/Resources/Consumables");
            EnsureDirectoryExists("Assets/Resources/Relics");

            CreateConsumable("Consumables/불꽃 폭발의 마법서", "불꽃 폭발의 마법서", "대상 적 하나에게 피해를 20 줍니다.", ConsumableEffectType.DamageSingle, 20f);
            CreateConsumable("Consumables/화염 장벽의 마법서", "화염 장벽의 마법서", "모든 적에게 피해를 10 줍니다.", ConsumableEffectType.DamageAll, 10f);
            CreateConsumable("Consumables/생명의 나뭇잎", "생명의 나뭇잎", "대상의 체력을 10 회복합니다.", ConsumableEffectType.HealHP, 10f);
            CreateConsumable("Consumables/신경 안정제", "신경 안정제", "대상의 정신력을 10 회복합니다.", ConsumableEffectType.HealMental, 10f);

            CreateRelic("Relics/빛나는 당근", "빛나는 당근", "마을에서 휴식을 할 때 마다 모든 아군의 체력이 15% 추가로 회복됩니다.", RelicEffectType.RestBonusHeal, 15f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[샘플 데이터] 소모품과 유물 데이터가 Assets/Resources 하위에 생성되었습니다!");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void CreateConsumable(string path, string name, string desc, ConsumableEffectType type, float value)
        {
            string fullPath = $"Assets/Resources/{path}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ConsumableData>(fullPath);
            if (existing == null)
            {
                ConsumableData data = ScriptableObject.CreateInstance<ConsumableData>();
                data.consumableID = name;
                data.consumableName = name;
                data.description = desc;
                data.effectType = type;
                data.effectValue = value;
                AssetDatabase.CreateAsset(data, fullPath);
            }
        }

        private static void CreateRelic(string path, string name, string desc, RelicEffectType type, float value)
        {
            string fullPath = $"Assets/Resources/{path}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RelicData>(fullPath);
            if (existing == null)
            {
                RelicData data = ScriptableObject.CreateInstance<RelicData>();
                data.relicID = name;
                data.relicName = name;
                data.description = desc;
                data.effectType = type;
                data.effectValue = value;
                AssetDatabase.CreateAsset(data, fullPath);
            }
        }
    }
}
