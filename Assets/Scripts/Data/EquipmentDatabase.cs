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
        public static List<EquipmentData> GetSynthesisCandidates(EquipmentData first, EquipmentData second)
        {
            var results = new List<EquipmentData>();
            if (first == null || second == null || first.isSpecial || second.isSpecial || first.starLevel < 1 || first.starLevel >= 3
                || !EquipmentLoadouts.Same(first, second)) return results;
            if (equipDict == null) InitDatabase();
            var ids = new HashSet<string>();
            if (first.synthesisResultIDs != null) ids.UnionWith(first.synthesisResultIDs);
            if (second.synthesisResultIDs != null) ids.UnionWith(second.synthesisResultIDs);
            foreach (string id in ids)
            {
                var candidate = GetEquipment(id);
                if (candidate != null && candidate.starLevel == first.starLevel + 1 && candidate.category == first.category)
                    results.Add(candidate);
            }
            return results;
        }

        public static EquipmentData SynthesizeEquipments(EquipmentData eq1, EquipmentData eq2)
        {
            var candidates = GetSynthesisCandidates(eq1, eq2);
            return candidates.Count == 0 ? null : candidates[Random.Range(0, candidates.Count)];
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

            Register("ColossalSword", "특대검", "공격력", 3, 15f, 0, 40f, 0, 5f, 0, 0, null, "Greatsword", "거인 분쇄", "물리 공격 스킬이 실제 체력 피해를 준 대상에게 최대 체력 4% 추가 물리 피해 (대상별 스킬당 1회, 착용자 공격력이 상한, 방어 적용·치명 제외)");
            Register("RuneGreatsword", "룬 대검", "공격력", 3, 9f, 9f, 20f, 20f, 0, 0, 0, null, "Greatsword", "마검사", "공격력·주문력 중 높은 쪽 +9 (자기 효과 제외, 동률은 공격력)");
            Register("Odachi", "대태도", "공격력", 3, 18f, 0, 0, 0, 0, 0, 20f, null, "Hwando", "예리함", "치명 피해 +25%p");
            Register("CheongwoonBlade", "청운도", "공격력", 3, 14f, 0, 20f, 0, 0, 0, 15f, null, "Hwando", "상처 찢기", "물리 공격 스킬이 실제 체력 피해를 준 대상에게 출혈 3·3라운드 (대상별 스킬당 1회)");

            // ─────────────────────────────────────────────────────────────
            // 2. 주문력 계열 (Spell Power Category)
            // ─────────────────────────────────────────────────────────────
            Register("Staff", "지팡이", "주문력", 1, 0, 3f, 0, 0, 0, 0, 0, new List<string> { "OldTreeStaff", "SapphireWand" });

            Register("OldTreeStaff", "고목나무 스태프", "주문력", 2, 0, 6f, 0, 0, 0, 4f, 0, new List<string> { "ArchmageGrimoire", "ElementalStaff" }, "Staff");
            Register("SapphireWand", "사파이어 완드", "주문력", 2, 0, 8f, 0, 15f, 0, 0, 0, new List<string> { "GrimoireOfTime", "StarWand" }, "Staff");

            Register("ArchmageGrimoire", "대마도서", "주문력", 3, 0, 30f, 0, 0, 0, 0, 0, null, "OldTreeStaff", "대마법의 편린", "주문력 +20%");
            Register("ElementalStaff", "원소의 스태프", "주문력", 3, 0, 12f, 7f, 14f, 0, 0, 0, null, "OldTreeStaff", "원소 증폭", "착용자 스킬이 적에게 부여하는 출혈·독·화상 수치 +50% (버림, 유물·장비 자동 부여 제외)");
            Register("GrimoireOfTime", "시간의 마도서", "주문력", 3, 0, 18f, 0, 30f, 0, 0, 0, null, "SapphireWand", "사고 가속", "실제 액티브 2회마다 일반 D6 1개 (착용자 아군 턴당 최대 2개)");
            Register("StarWand", "별의 완드", "주문력", 3, 0, 8f, 0, 60f, 0, 0, 0, null, "SapphireWand", "별의 울림", "최대 정신력 8%만큼 주문력 증가");

            // ─────────────────────────────────────────────────────────────
            // 3. 방어력 계열 (Armor Category)
            // ─────────────────────────────────────────────────────────────
            Register("WoodenShield", "나무 방패", "방어력", 1, 0, 0, 0, 0, 2f, 0, 0, new List<string> { "Buckler", "KiteShield" });

            Register("Buckler", "버클러", "방어력", 2, 4f, 0, 0, 0, 4f, 0, 0, new List<string> { "ObsidianShield", "RuneShield" }, "WoodenShield");
            Register("KiteShield", "카이트 실드", "방어력", 2, 0, 0, 20f, 0, 6f, 0, 0, new List<string> { "TowerShield", "MegalithShield" }, "WoodenShield");

            Register("ObsidianShield", "흑요석 방패", "방어력", 3, 10f, 0, 0, 0, 8f, 0, 0, null, "Buckler", "최선의 방어는", "방어력 40%만큼 공격력 증가");
            Register("RuneShield", "룬 실드", "방어력", 3, 0, 6f, 0, 0, 10f, 0, 0, null, "Buckler", "적응형", "방어력·마법저항력 중 낮은 쪽 +7 (자기 효과 제외, 동률은 방어력)");
            Register("TowerShield", "타워 실드", "방어력", 3, 0, 0, 20f, 0, 16f, 0, 0, null, "KiteShield", "철벽", "방어력 +20%");
            Register("MegalithShield", "거석 방패", "방어력", 3, 0, 0, 0, 0, 22f, 0, 0, null, "KiteShield", "부동", "물리 피해 판정 직전 체력이 20% 미만이면 받는 물리 피해 −50% (보호로 대신 받는 피해 포함)");

            // ─────────────────────────────────────────────────────────────
            // 4. 마법 저항력 계열 (Magic Resist Category)
            // ─────────────────────────────────────────────────────────────
            Register("SilverRing", "은 반지", "마법 저항력", 1, 0, 0, 0, 0, 0, 2f, 0, new List<string> { "GuardianRing", "ManaStoneRing" });

            Register("GuardianRing", "수호 반지", "마법 저항력", 2, 0, 0, 0, 0, 2f, 4f, 0, new List<string> { "FortressRing", "PureManaRing" }, "SilverRing");
            Register("ManaStoneRing", "마석 반지", "마법 저항력", 2, 6f, 0, 0, 0, 0, 4f, 0, new List<string> { "ManaTechRing", "VampiricRing" }, "SilverRing");

            Register("FortressRing", "요새의 반지", "마법 저항력", 3, 0, 0, 0, 0, 10f, 10f, 0, null, "GuardianRing", "요새화", "적의 부정적 상태 부여를 25% 확률로 저항 (상태 종류별 스킬당 1회 판정)");
            Register("PureManaRing", "순수한 마나의 반지", "마법 저항력", 3, 0, 0, 0, 0, 0, 22f, 0, null, "GuardianRing", "마나 파동", "아군 턴 시작에 모든 생존 적에게 착용자 마법저항력 25%만큼 정신 피해");
            Register("ManaTechRing", "마도공학 반지", "마법 저항력", 3, 0, 10f, 0, 0, 0, 8f, 0, null, "ManaStoneRing", "마력 환원기", "공격 스킬이 실제 준 마법 체력 피해 합의 20%만큼 자기 정신력 회복");
            Register("VampiricRing", "흡수의 반지", "마법 저항력", 3, 6f, 0, 0, 0, 0, 10f, 0, null, "ManaStoneRing", "영혼 흡혈", "공격 스킬이 실제 준 물리 체력 피해 합의 20%만큼 자기 체력 회복");

            // ─────────────────────────────────────────────────────────────
            // 5. 체력 계열 (Max HP Category)
            // ─────────────────────────────────────────────────────────────
            Register("LeatherArmor", "가죽 갑옷", "체력", 1, 0, 0, 15f, 0, 0, 0, 0, new List<string> { "PlateArmor", "AssassinGarb" });

            Register("PlateArmor", "판금 갑옷", "체력", 2, 0, 0, 40f, 0, 0, 0, 0, new List<string> { "DragonScaleArmor", "GiantArmor" }, "LeatherArmor");
            Register("AssassinGarb", "암살자의 도복", "체력", 2, 2f, 2f, 25f, 0, 0, 0, 0, new List<string> { "WindfeatherTunic", "AegisArmor" }, "LeatherArmor");

            Register("DragonScaleArmor", "용인의 갑주", "체력", 3, 0, 0, 60f, 0, 4f, 4f, 0, null, "PlateArmor", "용의 비늘", "아군 턴 시작에 잃은 체력 10% 회복");
            Register("GiantArmor", "거인의 갑옷", "체력", 3, 0, 0, 80f, 0, 0, 0, 0, null, "PlateArmor", "거대한 힘", "최대 체력 10%만큼 공격력 증가");
            Register("WindfeatherTunic", "바람깃 튜닉", "체력", 3, 8f, 0, 40f, 0, 0, 0, 10f, null, "AssassinGarb", "바람길", "착용자 공격 스킬이 치명타를 내면 일반 D6 1개 (착용자 아군 턴당 1회)");
            Register("AegisArmor", "마법장 갑주", "체력", 3, 0, 12f, 35f, 0, 0, 0, 0, null, "AssassinGarb", "마법 보호장", "전투당 첫 직접 공격 체력 피해 1회 무효화 (방어 계산 후, 보호막보다 먼저)");

            // ─────────────────────────────────────────────────────────────
            // 6. 정신력 계열 (Max Mental Category)
            // ─────────────────────────────────────────────────────────────
            Register("GemNecklace", "보석 목걸이", "정신력", 1, 0, 0, 0, 15f, 0, 0, 0, new List<string> { "BlessedNecklace", "ManaStoneNecklace" });

            Register("BlessedNecklace", "축성 목걸이", "정신력", 2, 0, 0, 0, 40f, 0, 0, 0, new List<string> { "SaintNecklace", "JudicatorNecklace" }, "GemNecklace");
            Register("ManaStoneNecklace", "마석 목걸이", "정신력", 2, 0, 0, 0, 25f, 1f, 1f, 0, new List<string> { "EvilEyeNecklace", "MoonlightNecklace" }, "GemNecklace");

            Register("SaintNecklace", "성자의 목걸이", "정신력", 3, 0, 0, 45f, 45f, 0, 0, 0, null, "BlessedNecklace", "성자의 빛", "착용자가 주고받는 체력·정신력 회복 +20% (자신 회복은 1회)");
            Register("JudicatorNecklace", "심판자의 목걸이", "정신력", 3, 0, 8f, 0, 60f, 0, 0, 0, null, "BlessedNecklace", "하늘의 심판", "최대 체력 10%만큼 주문력 증가");
            Register("EvilEyeNecklace", "마안의 목걸이", "정신력", 3, 8f, 0, 0, 50f, 0, 0, 0, null, "ManaStoneNecklace", "정신 침식", "공격 스킬이 실제 준 물리 체력 피해 30%를 같은 대상에게 정신 피해 (대상별 합산 후 1회)");
            // ─────────────────────────────────────────────────────────────
            // 7. 특수 장비 (강화·합성 불가, 파티 전체 1개)
            // ─────────────────────────────────────────────────────────────
            RegisterSpecial("BloodDagger", "피가 흐르는 단검", 35f, 0, 0, 0, 0, 0, 20f, "가시공", "적에게 주는 물리 피해 +30%. 실제 액티브 효과 처리 후 현재 체력 6% 물리 자해 1회", hpPercent: -0.25f);
            RegisterSpecial("GreedCrown", "탐욕의 관", 0, 0, 0, 50f, 5f, 5f, 0, "탐욕", "전투 보상 골드 +20%. 골드 100당 공격력·주문력 +1 (전투 시작 골드로 고정)");
            RegisterSpecial("HelperRobot", "자율주행 도우미 로봇", 0, 0, 0, 0, 4f, 4f, 0, "자율 지원", "아군 턴 시작 무작위 1회: 30% 적 기절 1턴 / 30% 일반 D6 2개 / 25% 착용자 공·주 +30% / 10% 모든 적 출혈 8·3라운드 / 5% 착용자 물리 피해 1");
            RegisterSpecial("CursedBlade", "저주받은 칼날", 0, 0, 0, 0, 0, 0, 100f, "저주", "공격력 −50%, 주문력 −50%, 치명 +100%p", attackPercent: -0.5f, spellPercent: -0.5f);
            RegisterSpecial("TwinStarSword", "쌍성의 검", 17f, 17f, 0, 0, 0, 0, 0, "파동 연결", "같은 주대상에게 연속 공격 스킬 시 피해 +10%씩 최대 +50% (다른 주대상·전체 공격·턴 종료 시 초기화)");
            RegisterSpecial("MiasmaCloud", "독기 구름", 0, 25f, 0, 50f, 0, 0, 0, "독기", "전투 시작 시 자신과 모든 생존 적에게 독 10·3라운드", hpPercent: -0.25f);
            RegisterSpecial("PerfectGear", "완벽한 장비", 10f, 10f, 25f, 25f, 5f, 5f, 0, "", "");
            RegisterSpecial("Polyhedron", "부등변다면체", 0, 0, 0, 0, 0, 0, 0, "가상 인원", "착용자의 모든 진영·직업 시너지 가상 인원 +1");

            Register("MoonlightNecklace", "달의 목걸이", "정신력", 3, 0, 0, 0, 50f, 3f, 7f, 0, null, "ManaStoneNecklace", "달빛", "아군 턴 시작에 다음 소비 주사위 1개에 사용 시 눈금 +2 (입력 조건 미적용, 턴 종료 소멸)");
        }

        private static void RegisterSpecial(string id, string name, float atk, float spell, float hp, float mental, float armor, float mr, float crit,
            string passName, string passDesc, float attackPercent = 0f, float spellPercent = 0f, float hpPercent = 0f)
        {
            Register(id, name, "특수", 3, atk, spell, hp, mental, armor, mr, crit, null, "", passName, passDesc);
            var data = equipDict[id];
            data.isSpecial = true;
            data.bonusAttackPercent = attackPercent;
            data.bonusSpellPowerPercent = spellPercent;
            data.bonusHpPercent = hpPercent;
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

            if (id == "ArchmageGrimoire") data.bonusSpellPowerPercent = 0.2f;
            if (id == "TowerShield") data.bonusArmorPercent = 0.2f;
            equipDict[id] = data;
        }
    }
}
