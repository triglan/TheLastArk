using UnityEngine;
using UnityEditor;
using TheLastArk.Data;
using System.IO;

namespace TheLastArk.EditorScripts
{
    public class GenerateRelics
    {
        [MenuItem("TheLastArk/Generate All Relics")]
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

            // ── 1. 일반 / 편의 / 경제 / 기차 유물 (12종) ──
            CreateRelic("Relic_MetalDetector", "낡은 금속탐지기", "전투 승리 보상에서 선택지가 등장할 때, 카드가 한 개 더 등장합니다.", RelicRarity.Common, RelicEffectType.ExtraCardChoice, 1f);
            CreateRelic("Relic_GnomeHammer", "노움의 만능 망치", "기차 칸 구매 및 강화 비용이 20% 감소합니다.", RelicRarity.Common, RelicEffectType.TrainDiscount, 0.2f);
            CreateRelic("Relic_Bed", "고급 침대", "마을에서 휴식을 할 때 마다 모든 아군의 체력이 10% 추가로 회복됩니다.", RelicRarity.Common, RelicEffectType.RestBonusHeal, 0.1f);
            CreateRelic("Relic_Carrot", "빛나는 당근", "휴식이 마을 선택지를 소모하지 않습니다.", RelicRarity.Common, RelicEffectType.FreeRest, 0);
            CreateRelic("Relic_Decoder", "암호 해독기", "기차의 통신소 레벨이 +1 증가합니다.", RelicRarity.Common, RelicEffectType.CommLevelBonus, 1);
            CreateRelic("Relic_VIP", "VIP 회원권", "상점가 구매 가격이 30% 감소합니다.", RelicRarity.Common, RelicEffectType.ShopDiscount, 0.3f);
            CreateRelic("Relic_CreditLedger", "외상 장부", "아이템 구매 시 골드가 -300까지 감소할 수 있습니다.", RelicRarity.Common, RelicEffectType.CreditLedger, 300f);
            CreateRelic("Relic_ThiefHand", "도적의 손길", "주점의 용병 고용 가격이 30% 감소합니다.", RelicRarity.Common, RelicEffectType.TavernDiscount, 0.3f);
            CreateRelic("Relic_CouragePotion", "용기의 물약", "주점 선택지에서 용병이 하나 더 등장합니다.", RelicRarity.Common, RelicEffectType.TavernExtraMerc, 1);
            CreateRelic("Relic_AlchemistJar", "연금술사의 황금 항아리", "전투 승리 보상 골드가 30% 증가합니다.", RelicRarity.Common, RelicEffectType.VictoryGoldBonus, 0.3f);
            CreateRelic("Relic_BountyHunter", "현상금 사냥", "엘리트 전투 시 보상 골드가 100% 증가합니다.", RelicRarity.Common, RelicEffectType.EliteGoldBonus, 1.0f);
            CreateRelic("Relic_AuthorityStaff", "권위자의 지팡이", "리더로 지정된 동료의 스킬이 전부 해금됩니다.", RelicRarity.Legendary, RelicEffectType.LeaderAllSkillsUnlocked, 1f);

            // ── [전설 유물 - 일반 4종] ──
            CreateRelic("Relic_ArchInvest", "고고학 투자 증서", "상점에서 등장하는 1번째 유물은 항상 전설 유물이 됩니다. (남은 전설 유물이 없으면 일반유물 등장)", RelicRarity.Legendary, RelicEffectType.ShopFirstLegendary, 0);
            CreateRelic("Relic_Dice", "전설의 주사위", "모든 새로고침 횟수가 +1 증가합니다.", RelicRarity.Legendary, RelicEffectType.ExtraRefresh, 1);
            CreateRelic("Relic_ArkCoin", "아크코인", "매 전투 종료 보상 골드가 -50%~+150%로 변동됩니다.", RelicRarity.Legendary, RelicEffectType.ArkCoin, 0);
            CreateRelic("Relic_GoldenPath", "황금의 길", "전투 종료 후, 보유한 골드의 10%만큼 추가 골드를 획득합니다. (보유 골드가 500일 때 최대)", RelicRarity.Legendary, RelicEffectType.GoldenPath, 0.1f);

            // ── [전설 유물 - 시너지 관련 14종] ──
            CreateRelic("Relic_HeartOfMagitech", "마도공학의 심장", "아르키움 유니온 시너지가 +2 증가합니다. 이번 전투동안 마법공학 발명품이 발동할 때 마다, 아군의 공격력, 주문력이 1 증가합니다.", RelicRarity.Legendary, RelicEffectType.HeartOfMagitech, 2f);
            CreateRelic("Relic_BeastSlayer", "마수살해자", "라이언하트 시너지가 +2 증가합니다. 라이언하트 시너지의 마수 대상 효과가 +50% 증가하며, 라이언하트 시너지의 효과가 모든 아군에게 적용됩니다.", RelicRarity.Legendary, RelicEffectType.BeastSlayer, 2f);
            CreateRelic("Relic_SkyCross", "하늘 십자가", "엘리시움 시너지가 +2 증가합니다. 다른 세력 시너지가 없을 때, 정신력과 주문력 +25% 효과가 추가됩니다.", RelicRarity.Legendary, RelicEffectType.SkyCross, 2f);
            CreateRelic("Relic_GrimoireOfStars", "별의 그리모어", "푸른 마탑 시너지가 +2 증가합니다. 푸른 마탑 시너지(8) 해금: 스킬 1회 사용 시 효과 적용으로 변경됩니다.", RelicRarity.Legendary, RelicEffectType.GrimoireOfStars, 2f);
            CreateRelic("Relic_WorldTreeBranch", "세계수의 가지", "엘븐우드 대삼림 시너지가 +3 증가합니다.", RelicRarity.Legendary, RelicEffectType.WorldTreeBranch, 3f);
            CreateRelic("Relic_AllianceCrest", "연합의 문장", "신기루 시너지가 +2 증가합니다. 아군이 직업에 따라 추가 효과를 얻습니다. (수호자, 전사, 지원가: 잃은 정신력의 3%만큼 회복 / 암살자, 사수, 마술사: 치명 +10)", RelicRarity.Legendary, RelicEffectType.AllianceCrest, 2f);
            CreateRelic("Relic_WhisperCult", "속삭임 교단", "속삭임 교단 시너지가 +2 증가합니다. 전용 스킬이 체력에도 동일한 피해를 줍니다.", RelicRarity.Legendary, RelicEffectType.WhisperCultRelic, 2f);
            CreateRelic("Relic_Cheongwoon", "청운", "청운 시너지가 +2 증가합니다. 청운 (7) 효과 추가: 행동력을 2 소모할 때 마다 효과가 발동합니다.", RelicRarity.Legendary, RelicEffectType.CheongwoonRelic, 2f);
            CreateRelic("Relic_MegalithShield", "거석 방패", "수호자가 최대 체력의 10%만큼, 공격력과 주문력 중 높은 수치를 얻습니다.", RelicRarity.Legendary, RelicEffectType.MegalithShield, 0.1f);
            CreateRelic("Relic_BerserkerAxe", "광전사의 도끼", "전사 아군이 적에게 주는 체력 피해의 15%만큼 체력을 회복합니다.", RelicRarity.Legendary, RelicEffectType.BerserkerAxe, 0.15f);
            CreateRelic("Relic_ShadowVeil", "그림자 베일", "암살자 아군이 적을 처치하면 행동력을 3 회복합니다.", RelicRarity.Legendary, RelicEffectType.ShadowVeil, 3f);
            CreateRelic("Relic_SniperEye", "저격수의 눈", "사수 아군의 치명타 피해량이 +25% 증가합니다.", RelicRarity.Legendary, RelicEffectType.SniperEye, 0.25f);
            CreateRelic("Relic_RuneOfCycle", "순환의 룬", "마술사 아군이 상태이상을 부여할 때 마다 무작위 아군의 정신력을 1 회복시킵니다.", RelicRarity.Legendary, RelicEffectType.RuneOfCycle, 1f);
            CreateRelic("Relic_HealingMallangi", "힐링 말랑이", "지원가 (4) 해금: 모든 아군 지원가의 스킬칸이 +1 증가합니다.", RelicRarity.Legendary, RelicEffectType.HealingMallangi, 1f);

            // ── [전설 유물 - 영웅 전용 2종 (알렉스 바스티온)] ──
            CreateRelic("Relic_Traitor", "배반자", "알렉스 바스티온 전용 유물. 회생 특성이 배반자 특성으로 변경됩니다. 스테이지당 1번, 체력 1이 되었을 때 아군들의 정신력을 흡수해 부활하며 공격력이 50% 증가합니다. (개화: 부활 시 무작위 적에게 가르기를 2번 시전)", RelicRarity.Legendary, RelicEffectType.Traitor, 0.5f);
            CreateRelic("Relic_EndlessBattle", "끊임없는 전투", "알렉스 바스티온 전용 유물. 재정비 스킬이 끊임없는 전투 스킬으로 변경됩니다. 1턴간 잃은 체력의 20%만큼 공격력을 얻고 즉시 무작위 적에게 가르기를 시전합니다. (일회성 / +1: 가르기 횟수 +1 / +2: 일회성 제거)", RelicRarity.Legendary, RelicEffectType.EndlessBattle, 0.2f);

            // ── 2. 기존 시너지 특수 유물 ──
            CreateRelic("Relic_MedalBox", "훈장함", "활성화된 시너지가 4개 이상일 때, 모든 아군의 체력, 공격력, 주문력 +20%", RelicRarity.Legendary, RelicEffectType.MedalBox, 0.2f);
            CreateRelic("Relic_UnityBanner", "화합의 깃발", "활성화된 시너지가 없을 때, 모든 아군의 체력, 정신력 +40%, 행동력 +4", RelicRarity.Legendary, RelicEffectType.UnityBanner, 0.4f);

            // 시너지 증표 (15종)
            CreateBadgeRelic("Relic_Badge_Union", "유니온의 증표", "아르키움 유니온 시너지가 1 증가합니다.", SynergyType.ArchiumUnion);
            CreateBadgeRelic("Relic_Badge_Lion", "라이언의 증표", "라이언하트 시너지가 1 증가합니다.", SynergyType.Lionheart);
            CreateBadgeRelic("Relic_Badge_Elysium", "엘리시움의 증표", "엘리시움 시너지가 1 증가합니다.", SynergyType.Elysium);
            CreateBadgeRelic("Relic_Badge_BlueTower", "마탑의 증표", "푸른 마탑 시너지가 1 증가합니다.", SynergyType.BlueTower);
            CreateBadgeRelic("Relic_Badge_Elvenwood", "엘븐우드의 증표", "엘븐우드 대삼림 시너지가 1 증가합니다.", SynergyType.Elvenwood);
            CreateBadgeRelic("Relic_Badge_Mirage", "신기루의 증표", "신기루 시너지가 1 증가합니다.", SynergyType.Mirage);
            CreateBadgeRelic("Relic_Badge_WhisperCult", "교단의 증표", "속삭임 교단 시너지가 1 증가합니다.", SynergyType.WhisperCult);
            CreateBadgeRelic("Relic_Badge_Cheongwoon", "청운의 증표", "청운 시너지가 1 증가합니다.", SynergyType.Cheongwoon);
            CreateBadgeRelic("Relic_Badge_Arcadia", "아르카디아의 증표", "침묵의 아르카디아 시너지가 1 증가합니다.", SynergyType.SilentArcadia);
            CreateBadgeRelic("Relic_Badge_Guardian", "수호자의 증표", "수호자 시너지가 1 증가합니다.", SynergyType.Guardian);
            CreateBadgeRelic("Relic_Badge_Warrior", "전사의 증표", "전사 시너지가 1 증가합니다.", SynergyType.Warrior);
            CreateBadgeRelic("Relic_Badge_Assassin", "암살자의 증표", "암살자 시너지가 1 증가합니다.", SynergyType.Assassin);
            CreateBadgeRelic("Relic_Badge_Ranger", "사수의 증표", "사수 시너지가 1 증가합니다.", SynergyType.Ranger);
            CreateBadgeRelic("Relic_Badge_Mage", "마술사의 증표", "마술사 시너지가 1 증가합니다.", SynergyType.Mage);
            CreateBadgeRelic("Relic_Badge_Support", "지원가의 증표", "지원가 시너지가 1 증가합니다.", SynergyType.Support);

            // ── 3. 전투 관련 유물 (19종) ──
            CreateRelic("Relic_SharpNail", "날카로운 못", "물리 피해를 입힐 때 추가로 1의 고정 피해를 더 입힙니다.", RelicRarity.Common, RelicEffectType.SharpNail, 1f);
            CreateRelic("Relic_MindFractureRune", "정신 분열의 룬", "마법 피해가 대상의 정신력에 10% 추가 피해를 줍니다.", RelicRarity.Common, RelicEffectType.MindFractureRune, 0.1f);
            CreateRelic("Relic_ShatterScroll", "파쇄 주문서", "정신 피해를 줄 때, 대상의 정신력이 10% 미만이면 즉시 패닉시킵니다.", RelicRarity.Common, RelicEffectType.ShatterScroll, 0.1f);
            CreateRelic("Relic_GlassBlade", "유리 칼날", "치명타 발동 시 대상에게 출혈을 3 부여합니다.", RelicRarity.Common, RelicEffectType.GlassBlade, 3f);
            CreateRelic("Relic_RedShoes", "빨간 구두", "턴 시작 시, 출혈을 보유하지 않은 모든 적에게 출혈을 2 부여합니다.", RelicRarity.Common, RelicEffectType.RedShoes, 2f);
            CreateRelic("Relic_BloodMist", "피안개", "출혈이 10 이상 중첩된 적은 즉시 출혈이 1회 발동합니다.", RelicRarity.Legendary, RelicEffectType.BloodMist, 10f);
            CreateRelic("Relic_PoisonMushroom", "맹독 버섯", "독을 부여할 때, 추가로 1 부여합니다.", RelicRarity.Common, RelicEffectType.PoisonMushroom, 1f);
            CreateRelic("Relic_MindLeech", "정신 흡혈 거머리", "독으로 10의 피해를 입힐 때 마다 무작위 아군의 정신력을 1 회복시킵니다.", RelicRarity.Common, RelicEffectType.MindLeech, 1f);
            CreateRelic("Relic_SwampLiquid", "늪지의 액체", "중독된 적에게 독을 부여할 때, 무작위 적 1명에게 대상 독 수치의 20%만큼 독을 부여합니다.", RelicRarity.Common, RelicEffectType.SwampLiquid, 0.2f);
            CreateRelic("Relic_FireMoth", "불나방", "화상이 시전자에게도 부여됩니다. 적에게 부여하는 화상 수치가 100% 증가합니다.", RelicRarity.Common, RelicEffectType.FireMoth, 1.0f);
            CreateRelic("Relic_FlameHammer", "화염 망치", "이번 턴에 화상을 3번 이상 같은 적에게 부여하면, 즉시 화상이 발동되며 수치가 감소하지 않습니다.", RelicRarity.Common, RelicEffectType.FlameHammer, 3f);
            CreateRelic("Relic_Lantern", "호롱불", "적이 화상 피해를 입을 때 피해량이 25% 증가하고, 아군이 화상 피해를 입을 때 피해량이 25% 감소합니다.", RelicRarity.Common, RelicEffectType.Lantern, 0.25f);
            CreateRelic("Relic_SealOfVengeance", "복수의 인장", "아군이 체력 피해를 입을 때 마다 힘 1을 얻습니다.", RelicRarity.Common, RelicEffectType.SealOfVengeance, 1f);
            CreateRelic("Relic_IronFortressFragment", "철벽 성채의 조각", "전투 시작 시 모든 아군에게 보호 20을 부여합니다.", RelicRarity.Common, RelicEffectType.IronFortressFragment, 20f);
            CreateRelic("Relic_ManaCrusher", "마나분쇄자", "적의 보호막에 주는 피해가 50% 증가합니다.", RelicRarity.Common, RelicEffectType.ManaCrusher, 0.5f);
            CreateRelic("Relic_OiledGear", "기름칠된 톱니바퀴", "각 전투에서 가장 처음으로 사용하는 스킬에 유지 효과를 부여합니다.", RelicRarity.Common, RelicEffectType.OiledGear, 1f);
            CreateRelic("Relic_UncertainScales", "불확실한 천칭", "전투 스킬 중, 확률적으로 발동되는 효과의 발동 확률 +25%", RelicRarity.Common, RelicEffectType.UncertainScales, 0.25f);
            CreateRelic("Relic_FlashOfTwilight", "회광반조", "아군이 잃은 체력에 비례해 주고 받는 치유량이 최대 50%까지 증가합니다. (체력 10%에서 최대)", RelicRarity.Common, RelicEffectType.FlashOfTwilight, 0.5f);
            CreateRelic("Relic_OneMoreDrink", "한 잔 더!", "비용이 0인 스킬을 사용할 때, 행동력을 1 회복합니다.", RelicRarity.Legendary, RelicEffectType.OneMoreDrink, 1f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GenerateRelics] All 35+ relics generated successfully.");
        }

        private static void CreateBadgeRelic(string id, string name, string desc, SynergyType synergyType)
        {
            string assetPath = $"Assets/Resources/Relics/{id}.asset";
            RelicData asset = AssetDatabase.LoadAssetAtPath<RelicData>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RelicData>();
                asset.relicID = id;
                asset.relicName = name;
                asset.description = desc;
                asset.rarity = RelicRarity.Common;
                asset.effectType = RelicEffectType.SynergyBadge;
                asset.effectValue = 1f;
                asset.targetSynergy = synergyType;

                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else
            {
                asset.relicName = name;
                asset.description = desc;
                asset.rarity = RelicRarity.Common;
                asset.effectType = RelicEffectType.SynergyBadge;
                asset.effectValue = 1f;
                asset.targetSynergy = synergyType;
                EditorUtility.SetDirty(asset);
            }
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
