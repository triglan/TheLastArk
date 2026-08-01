using System;
using UnityEngine;

namespace TheLastArk.Data
{
    public enum EquipmentType
    {
        Weapon,     // 무기
        Armor,      // 방어구
        Accessory   // 장신구
    }

    public enum EquipmentRarity
    {
        Common,     // 일반
        Rare,       // 희귀
        Epic,       // 영웅
        Legendary   // 전설
    }

    [CreateAssetMenu(fileName = "NewEquipment", menuName = "TheLastArk/Equipment Data")]
    public class EquipmentData : ScriptableObject
    {
        public string equipmentID;
        public string equipmentName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("장비 등급 및 합성 정보")]
        [Range(1, 3)] public int starLevel = 1; // 1성, 2성, 3성
        public string category = "공격력"; // 공격력, 주문력, 방어력, 마법 저항력, 체력, 정신력
        public string parentEquipmentID = ""; // 합성 전 상위 장비 ID
        public System.Collections.Generic.List<string> synthesisResultIDs = new System.Collections.Generic.List<string>(); // 합성 결과 라인업 (2종)

        [Header("기본 추가 능력치")]
        public float bonusAttack = 0f;
        public float bonusSpellPower = 0f;
        public float bonusHp = 0f;
        public float bonusMental = 0f;
        public float bonusArmor = 0f;
        public float bonusMagicResist = 0f;
        public float bonusCritRate = 0f; // % 단위 (10 -> 10%)
        public int bonusAP = 0;

        [Header("3성 장비 고유 패시브 효과")]
        public string passiveSkillName = "";
        [TextArea(2, 4)]
        public string passiveDescription = "";
    }

    [Serializable]
    public class EquipmentItem
    {
        public string instanceID;
        public EquipmentData data;
        public string equippedCharacterID = ""; // 빈 문자열이면 미장착

        public EquipmentItem(EquipmentData data)
        {
            this.instanceID = Guid.NewGuid().ToString("N");
            this.data = data;
            this.equippedCharacterID = "";
        }

        public float FinalAttack => data != null ? data.bonusAttack : 0f;
        public float FinalHp => data != null ? data.bonusHp : 0f;
        public float FinalMental => data != null ? data.bonusMental : 0f;
        public int FinalAP => data != null ? data.bonusAP : 0;
    }
}
