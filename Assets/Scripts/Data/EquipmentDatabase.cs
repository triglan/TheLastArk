using System.Collections.Generic;
using UnityEngine;

namespace TheLastArk.Data
{
    public static class EquipmentDatabase
    {
        private static Dictionary<string, EquipmentData> equipDict;

        public static EquipmentData GetEquipment(string equipmentId)
        {
            if (equipDict == null) InitDatabase();
            if (equipDict.TryGetValue(equipmentId, out var data)) return data;
            return null;
        }

        public static List<EquipmentData> GetAllEquipments()
        {
            if (equipDict == null) InitDatabase();
            return new List<EquipmentData>(equipDict.Values);
        }

        public static List<EquipmentData> GetEquipmentsByStar(int starLevel)
        {
            if (equipDict == null) InitDatabase();
            List<EquipmentData> list = new List<EquipmentData>();
            foreach (var eq in equipDict.Values)
            {
                if (eq.starLevel == starLevel) list.Add(eq);
            }
            return list;
        }

        /// <summary>
        /// 두 장비를 합성하여 차세대 상위 장비 1종을 무작위 추출합니다.
        /// 동일 계열 합성 시 해당 계열 2종 중 무작위 1종 (50%/50%)
        /// </summary>
        public static EquipmentData SynthesizeEquipments(EquipmentData eq1, EquipmentData eq2)
        {
            if (eq1 == null || eq2 == null) return null;
            if (equipDict == null) InitDatabase();

            // 3성 장비는 합성 불가 (최고 등급)
            if (eq1.starLevel >= 3 || eq2.starLevel >= 3) return null;

            // 1. 같은 장비이거나 synthesisResultIDs가 존재하는 경우 우선 검색
            List<string> candidateIDs = new List<string>();
            if (eq1.synthesisResultIDs != null && eq1.synthesisResultIDs.Count > 0)
            {
                candidateIDs.AddRange(eq1.synthesisResultIDs);
            }
            if (eq2.synthesisResultIDs != null && eq2.synthesisResultIDs.Count > 0)
            {
                foreach (var id in eq2.synthesisResultIDs)
                {
                    if (!candidateIDs.Contains(id)) candidateIDs.Add(id);
                }
            }

            if (candidateIDs.Count > 0)
            {
                string chosenID = candidateIDs[Random.Range(0, candidateIDs.Count)];
                return GetEquipment(chosenID);
            }

            // 2. 계열이 같을 경우 해당 계열의 다음 등급 장비 목록 무작위
            int targetStar = Mathf.Min(eq1.starLevel, eq2.starLevel) + 1;
            List<EquipmentData> sameCategory = new List<EquipmentData>();
            foreach (var eq in equipDict.Values)
            {
                if (eq.starLevel == targetStar && (eq.category == eq1.category || eq.category == eq2.category))
                {
                    sameCategory.Add(eq);
                }
            }

            if (sameCategory.Count > 0)
            {
                return sameCategory[Random.Range(0, sameCategory.Count)];
            }

            // 3. Fallback: 임의의 다음 등급 장비
            var targetStarEquips = GetEquipmentsByStar(targetStar);
            if (targetStarEquips.Count > 0)
            {
                return targetStarEquips[Random.Range(0, targetStarEquips.Count)];
            }

            return null;
        }

        private static void InitDatabase()
        {
            equipDict = new Dictionary<string, EquipmentData>();

            // ─────────────────────────────────────────────────────────────
            // 1. 공격력 계열 (Attack Power Category)
            // ─────────────────────────────────────────────────────────────
            Register("Longsword", "롱소드", "공격력", 1, 3f, 0, 0, 0, 0, 0, 0, new List<string> { "Greatsword", "Hwando" });
            
            Register("Greatsword", "대검", "공격력", 2, 6f, 0, 20f, 0, 0, 0, 0, new List<string> { "ColossalSword", "RuneGreatsword" }, "Longsword");
            Register("Hwando", "환도", "공격력", 2, 8f, 0, 0, 0, 0, 0, 10f, new List<string> { "Odachi", "CheongwoonBlade" }, "Longsword");

            Register("ColossalSword", "특대검", "공격력", 3, 15f, 0, 40f, 0, 5f, 0, 0, null, "Greatsword", "거인 분쇄", "적의 체력에 물리 피해를 줄 때, 적 최대 체력의 4%만큼 추가 피해");
            Register("RuneGreatsword", "룬 대검", "공격력", 3, 9f, 9f, 20f, 20f, 0, 0, 0, null, "Greatsword", "마검사", "공격력과 주문력 중 더 높은 수치의 능력치 +9");
            Register("Odachi", "대태도", "공격력", 3, 18f, 0, 0, 0, 0, 0, 20f, null, "Hwando", "예리함", "치명타 피해량 +25%");
            Register("CheongwoonBlade", "청운도", "공격력", 3, 14f, 0, 20f, 0, 0, 0, 15f, null, "Hwando", "상처 찢기", "적의 체력에 물리 피해를 줄 때, 출혈 3 부여");

            // ─────────────────────────────────────────────────────────────
            // 2. 주문력 계열 (Spell Power Category)
            // ─────────────────────────────────────────────────────────────
            Register("Staff", "지팡이", "주문력", 1, 0, 3f, 0, 0, 0, 0, 0, new List<string> { "OldTreeStaff", "SapphireWand" });

            Register("OldTreeStaff", "고목나무 스태프", "주문력", 2, 0, 6f, 0, 0, 0, 4f, 0, new List<string> { "ArchmageGrimoire", "ElementalStaff" }, "Staff");
            Register("SapphireWand", "사파이어 완드", "주문력", 2, 0, 8f, 0, 15f, 0, 0, 0, new List<string> { "GrimoireOfTime", "StarWand" }, "Staff");

            Register("ArchmageGrimoire", "대마도서", "주문력", 3, 0, 30f, 0, 0, 0, 0, 0, null, "OldTreeStaff", "대마법의 편린", "주문력 +20%");
            Register("ElementalStaff", "원소의 스태프", "주문력", 3, 0, 12f, 7f, 14f, 0, 0, 0, null, "OldTreeStaff", "원소 회동", "적에게 부여하는 출혈, 독, 화상, 냉기 상태이상 수치 +50%");
            Register("GrimoireOfTime", "시간의 마도서", "주문력", 3, 0, 18f, 0, 30f, 0, 0, 0, null, "SapphireWand", "사고 가속", "장착한 캐릭터가 스킬 2회 사용 시 행동력 +1");
            Register("StarWand", "별의 완드", "주문력", 3, 0, 8f, 0, 60f, 0, 0, 0, null, "SapphireWand", "별의 울림", "최대 정신력의 8%만큼 주문력 증가");

            // ─────────────────────────────────────────────────────────────
            // 3. 방어력 계열 (Armor Category)
            // ─────────────────────────────────────────────────────────────
            Register("WoodenShield", "나무 방패", "방어력", 1, 0, 0, 0, 0, 2f, 0, 0, new List<string> { "Buckler", "KiteShield" });

            Register("Buckler", "버클러", "방어력", 2, 4f, 0, 0, 0, 4f, 0, 0, new List<string> { "ObsidianShield", "RuneShield" }, "WoodenShield");
            Register("KiteShield", "카이트 실드", "방어력", 2, 0, 0, 20f, 0, 6f, 0, 0, new List<string> { "TowerShield", "MegalithShield" }, "WoodenShield");

            Register("ObsidianShield", "흑요석 방패", "방어력", 3, 10f, 0, 0, 0, 8f, 0, 0, null, "Buckler", "최선의 방어는", "방어력의 40%만큼 공격력 증가");
            Register("RuneShield", "룬 실드", "방어력", 3, 0, 6f, 0, 0, 10f, 0, 0, null, "Buckler", "적응형 방패", "방어력과 마법저항력 중 더 낮은 수치의 능력치 +7");
            Register("TowerShield", "타워 실드", "방어력", 3, 0, 0, 20f, 0, 16f, 0, 0, null, "KiteShield", "철벽", "방어력 +20%");
            Register("MegalithShield", "거석 방패", "방어력", 3, 0, 0, 0, 0, 22f, 0, 0, null, "KiteShield", "부동", "체력이 20% 미만일 때, 받는 물리 피해 -50%");

            // ─────────────────────────────────────────────────────────────
            // 4. 마법 저항력 계열 (Magic Resist Category)
            // ─────────────────────────────────────────────────────────────
            Register("SilverRing", "은 반지", "마법 저항력", 1, 0, 0, 0, 0, 0, 2f, 0, new List<string> { "GuardianRing", "ManaStoneRing" });

            Register("GuardianRing", "수호 반지", "마법 저항력", 2, 0, 0, 0, 0, 2f, 4f, 0, new List<string> { "FortressRing", "PureManaRing" }, "SilverRing");
            Register("ManaStoneRing", "마석 반지", "마법 저항력", 2, 6f, 0, 0, 0, 0, 4f, 0, new List<string> { "ManaTechRing", "VampiricRing" }, "SilverRing");

            Register("FortressRing", "요새의 반지", "마법 저항력", 3, 0, 0, 0, 0, 10f, 10f, 0, null, "GuardianRing", "요새화", "25%의 확률로 상태이상에 저항");
            Register("PureManaRing", "순수한 마나의 반지", "마법 저항력", 3, 0, 0, 0, 0, 0, 22f, 0, null, "GuardianRing", "마나 파동", "매 턴마다 모든 적에게 착용자 마법 저항력의 25%만큼 정신 피해");
            Register("ManaTechRing", "마도공학 반지", "마법 저항력", 3, 0, 10f, 0, 0, 0, 8f, 0, null, "ManaStoneRing", "마력 환원기", "적에게 준 마법 피해의 20%만큼 착용자의 정신력 회복");
            Register("VampiricRing", "흡수의 반지", "마법 저항력", 3, 6f, 0, 0, 0, 0, 10f, 0, null, "ManaStoneRing", "영혼 흡혈", "적에게 준 물리 피해의 20%만큼 착용자의 체력 회복");

            // ─────────────────────────────────────────────────────────────
            // 5. 체력 계열 (Max HP Category)
            // ─────────────────────────────────────────────────────────────
            Register("LeatherArmor", "가죽 갑옷", "체력", 1, 0, 0, 15f, 0, 0, 0, 0, new List<string> { "PlateArmor", "AssassinGarb" });

            Register("PlateArmor", "판금 갑옷", "체력", 2, 0, 0, 40f, 0, 0, 0, 0, new List<string> { "DragonScaleArmor", "GiantArmor" }, "LeatherArmor");
            Register("AssassinGarb", "암살자의 도복", "체력", 2, 2f, 2f, 25f, 0, 0, 0, 0, new List<string> { "WindfeatherTunic", "AegisArmor" }, "LeatherArmor");

            Register("DragonScaleArmor", "용인의 갑주", "체력", 3, 0, 0, 60f, 0, 4f, 4f, 0, null, "PlateArmor", "용의 비늘", "턴 시작 시 잃은 체력의 10% 회복");
            Register("GiantArmor", "거인의 갑옷", "체력", 3, 0, 0, 80f, 0, 0, 0, 0, null, "PlateArmor", "거대한 힘", "최대 체력의 10%만큼 공격력 증가");
            Register("WindfeatherTunic", "바람깃 튜닉", "체력", 3, 8f, 0, 40f, 0, 0, 0, 10f, null, "AssassinGarb", "바람길", "착용자의 스킬로 치명타 발생 시 행동력 +1");
            Register("AegisArmor", "마법장 갑주", "체력", 3, 0, 12f, 35f, 0, 0, 0, 0, null, "AssassinGarb", "마법 보호장", "매 전투 시작 시 첫 피해를 무시함");

            // ─────────────────────────────────────────────────────────────
            // 6. 정신력 계열 (Max Mental Category)
            // ─────────────────────────────────────────────────────────────
            Register("GemNecklace", "보석 목걸이", "정신력", 1, 0, 0, 0, 15f, 0, 0, 0, new List<string> { "BlessedNecklace", "ManaStoneNecklace" });

            Register("BlessedNecklace", "축성 목걸이", "정신력", 2, 0, 0, 0, 40f, 0, 0, 0, new List<string> { "SaintNecklace", "JudicatorNecklace" }, "GemNecklace");
            Register("ManaStoneNecklace", "마석 목걸이", "정신력", 2, 0, 0, 0, 25f, 1f, 1f, 0, new List<string> { "EvilEyeNecklace", "MoonlightNecklace" }, "GemNecklace");

            Register("SaintNecklace", "성자의 목걸이", "정신력", 3, 0, 0, 45f, 45f, 0, 0, 0, null, "BlessedNecklace", "성자의 빛", "착용자가 주고 받는 회복량 +20%");
            Register("JudicatorNecklace", "심판자의 목걸이", "정신력", 3, 0, 8f, 0, 60f, 0, 0, 0, null, "BlessedNecklace", "하늘의 심판", "최대 체력의 10%만큼 주문력 증가");
            Register("EvilEyeNecklace", "마안의 목걸이", "정신력", 3, 8f, 0, 0, 50f, 0, 0, 0, null, "ManaStoneNecklace", "정신 침식", "착용자가 적에게 물리 피해를 줄 때, 준 피해의 30%만큼 정신력 피해");
            Register("MoonlightNecklace", "달의 목걸이", "정신력", 3, 0, 0, 0, 50f, 3f, 7f, 0, null, "ManaStoneNecklace", "달빛", "매 턴마다 착용자의 무작위 스킬 1개의 비용 -2");
        }

        private static void Register(string id, string name, string cat, int star, float atk, float spell, float hp, float mental, float armor, float mr, float crit, List<string> nextIDs = null, string parentID = "", string passName = "", string passDesc = "")
        {
            var data = ScriptableObject.CreateInstance<EquipmentData>();
            data.equipmentID = id;
            data.equipmentName = name;
            data.category = cat;
            data.starLevel = star;
            data.bonusAttack = atk;
            data.bonusSpellPower = spell;
            data.bonusHp = hp;
            data.bonusMental = mental;
            data.bonusArmor = armor;
            data.bonusMagicResist = mr;
            data.bonusCritRate = crit;
            data.synthesisResultIDs = nextIDs ?? new List<string>();
            data.parentEquipmentID = parentID;
            data.passiveSkillName = passName;
            data.passiveDescription = passDesc;

            equipDict[id] = data;
        }
    }
}
