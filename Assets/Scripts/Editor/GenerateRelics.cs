using UnityEngine;
using UnityEditor;
using TheLastArk.Data;
using System.IO;

namespace TheLastArk.EditorScripts
{
    public class GenerateRelics
    {
        [MenuItem("TheLastArk/Generate Non-Combat Relics")]
        public static void Generate()
        {
            string folderPath = "Assets/Resources/Relics";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "Relics");
            }

            CreateRelic("Relic_Bed", "고급 침대", "마을에서 휴식을 할 때 마다 모든 아군의 체력이 10% 추가로 회복됩니다.", RelicRarity.Common, RelicEffectType.RestBonusHeal, 0.1f);
            CreateRelic("Relic_Carrot", "빛나는 당근", "휴식이 더이상 선택지를 소모하지 않습니다.", RelicRarity.Common, RelicEffectType.FreeRest, 0);
            CreateRelic("Relic_Decoder", "암호 해독기", "기차의 통신소 레벨이 +1 증가합니다.", RelicRarity.Common, RelicEffectType.CommLevelBonus, 1);
            CreateRelic("Relic_VIP", "VIP 회원권", "상점가 구매 가격이 30% 감소합니다.", RelicRarity.Common, RelicEffectType.ShopDiscount, 0.3f);
            CreateRelic("Relic_ThiefHand", "도적의 손길", "주점의 용병 고용 가격이 30% 감소합니다.", RelicRarity.Common, RelicEffectType.TavernDiscount, 0.3f);
            CreateRelic("Relic_CouragePotion", "용기의 물약", "주점 선택지에서 용병이 하나 더 등장합니다.", RelicRarity.Common, RelicEffectType.TavernExtraMerc, 1);

            CreateRelic("Relic_ArchInvest", "고고학 투자 증서", "상점에서 등장하는 1번째 유물은 항상 전설 유물이 됩니다.", RelicRarity.Legendary, RelicEffectType.ShopFirstLegendary, 0);
            CreateRelic("Relic_Dice", "전설의 주사위", "모든 새로고침 횟수가 +1 증가합니다.", RelicRarity.Legendary, RelicEffectType.ExtraRefresh, 1);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Non-combat relics generated successfully.");
        }

        private static void CreateRelic(string id, string name, string desc, RelicRarity rarity, RelicEffectType effectType, float effectValue)
        {
            string assetPath = $"Assets/Resources/Relics/{id}.asset";
            RelicData asset = AssetDatabase.LoadAssetAtPath<RelicData>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RelicData>();
                asset.relicID = id;
                asset.relicName = name;
                asset.description = desc;
                asset.rarity = rarity;
                asset.effectType = effectType;
                asset.effectValue = effectValue;

                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else
            {
                asset.relicName = name;
                asset.description = desc;
                asset.rarity = rarity;
                asset.effectType = effectType;
                asset.effectValue = effectValue;
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
