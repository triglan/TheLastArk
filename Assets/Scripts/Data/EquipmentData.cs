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
        public EquipmentType equipmentType = EquipmentType.Weapon;
        public EquipmentRarity rarity = EquipmentRarity.Common;

        [Header("기본 추가 능력치")]
        public float bonusAttack = 10f;
        public float bonusHp = 50f;
        public float bonusMental = 0f;
        public int bonusAP = 0;
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
